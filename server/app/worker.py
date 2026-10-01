"""Each connection owns a process so disconnect can stop native TensorFlow work."""

import asyncio
import logging
import math
import multiprocessing
import os
from multiprocessing.connection import Connection

from app.config import Settings

logger = logging.getLogger(__name__)


def finite_logs(logs: dict) -> dict[str, float]:
    result = {name: float(value) for name, value in logs.items()}
    if not all(math.isfinite(value) for value in result.values()):
        raise ValueError("Keras returned a non-finite metric")
    return result


def build_model(definition: dict):
    import keras

    layers = [keras.Input(shape=(28, 28, 1))]
    if definition["layers"][0]["type"] == "dense":
        layers.append(keras.layers.Flatten())
    for layer in definition["layers"]:
        match layer["type"]:
            case "conv2d":
                layers.append(
                    keras.layers.Conv2D(
                        layer["filters"],
                        layer["kernelSize"],
                        activation=layer["activation"],
                        padding="valid",
                        strides=1,
                    )
                )
            case "max_pooling2d":
                layers.append(keras.layers.MaxPooling2D(layer["poolSize"], padding="valid"))
            case "flatten":
                layers.append(keras.layers.Flatten())
            case "dense":
                layers.append(keras.layers.Dense(layer["units"], activation=layer["activation"]))
    model = keras.Sequential(layers)
    model.compile(optimizer="adam", loss="sparse_categorical_crossentropy", metrics=["accuracy"])
    return model


def run_worker(pipe: Connection, cancelled, definition: dict, epochs: int, settings: Settings):
    os.environ["KERAS_BACKEND"] = "tensorflow"
    os.environ["TF_CPP_MIN_LOG_LEVEL"] = "2"
    os.environ["OMP_NUM_THREADS"] = str(settings.worker_threads)
    os.environ["TF_NUM_INTRAOP_THREADS"] = str(settings.worker_threads)
    os.environ["TF_NUM_INTEROP_THREADS"] = "1"
    try:
        import keras
        import numpy as np
        import tensorflow as tf

        tf.config.threading.set_intra_op_parallelism_threads(settings.worker_threads)
        tf.config.threading.set_inter_op_parallelism_threads(1)
        if cancelled.is_set():
            pipe.send({"type": "training_cancelled"})
            return
        model = build_model(definition)
        with np.load(settings.data_dir / "mnist.npz", allow_pickle=False) as data:
            x_train = data["x_train"][: settings.train_samples, ..., None].astype("float32") / 255.0
            y_train = data["y_train"][: settings.train_samples]
            x_test = data["x_test"][..., None].astype("float32") / 255.0
        options = tf.data.Options()
        options.threading.private_threadpool_size = 1
        options.threading.max_intra_op_parallelism = settings.worker_threads
        training_data = tf.data.Dataset.from_tensor_slices((x_train, y_train))
        training_data = training_data.shuffle(len(x_train)).batch(settings.batch_size)
        training_data = training_data.with_options(options)

        class Progress(keras.callbacks.Callback):
            final_logs = None
            completed_epoch = 0

            def on_train_batch_begin(self, batch, logs=None):
                if cancelled.is_set():
                    self.model.stop_training = True

            def on_train_batch_end(self, batch, logs=None):
                # Detect divergence even if a cancellation is waiting.
                finite_logs(logs or {})
                if cancelled.is_set():
                    self.model.stop_training = True

            def on_epoch_end(self, epoch, logs=None):
                self.final_logs = finite_logs(logs or {})
                self.completed_epoch = epoch + 1
                if cancelled.is_set():
                    self.model.stop_training = True
                elif epoch + 1 < epochs:
                    pipe.send(
                        {
                            "type": "training_progress",
                            "epoch": epoch + 1,
                            "epochs": epochs,
                            "logs": self.final_logs,
                        }
                    )

        progress = Progress()
        model.fit(training_data, epochs=epochs, callbacks=[progress], verbose=0)
        if cancelled.is_set():
            del model
            pipe.send({"type": "training_cancelled"})
            return
        if progress.completed_epoch != epochs or progress.final_logs is None:
            raise RuntimeError("Training ended before the final epoch")
        pipe.send({"type": "training_completed", "epoch": epochs, "logs": progress.final_logs})
    except Exception:
        logger.exception("Model training failed")
        pipe.send(
            {
                "type": "error",
                "request_type": "train",
                "code": "training_failed",
                "message": "Model training failed. Check the server log.",
            }
        )
        pipe.close()
        return

    try:
        while True:
            request = pipe.recv()
            try:
                image = x_test[request["index"] : request["index"] + 1]
                prediction_data = tf.data.Dataset.from_tensor_slices(image).batch(1)
                output = model.predict(prediction_data.with_options(options), verbose=0)[0]
                if output.shape != (10,) or not np.isfinite(output).all():
                    raise ValueError("Expected 10 finite model outputs")
                pipe.send(
                    {
                        "type": "inference_result",
                        "image_id": request["image_id"],
                        "output": output.tolist(),
                    }
                )
            except Exception:
                logger.exception("Model inference failed")
                pipe.send(
                    {
                        "type": "error",
                        "request_type": "infer",
                        "code": "inference_failed",
                        "message": "Model inference failed. Check the server log.",
                    }
                )
    except (EOFError, BrokenPipeError):
        pass
    finally:
        pipe.close()


class Worker:
    def __init__(self, definition: dict, epochs: int, settings: Settings):
        context = multiprocessing.get_context("spawn")
        self.pipe, child = context.Pipe()
        self.cancelled = context.Event()
        self.close_task = None
        self.process = context.Process(
            target=run_worker,
            args=(child, self.cancelled, definition, epochs, settings),
            daemon=True,
            name="mnist-session",
        )
        try:
            self.process.start()
        except BaseException:
            self.pipe.close()
            raise
        finally:
            child.close()

    async def receive(self) -> dict:
        while True:
            if await asyncio.to_thread(self.pipe.poll, 0.1):
                return self.pipe.recv()
            if not self.process.is_alive():
                raise EOFError("Training worker exited")

    def infer(self, image_id: str, index: int) -> None:
        self.pipe.send({"image_id": image_id, "index": index})

    def cancel(self) -> None:
        self.cancelled.set()

    async def close(self) -> None:
        if self.close_task is None:
            self.close_task = asyncio.create_task(self._close())
        try:
            await asyncio.shield(self.close_task)
        except asyncio.CancelledError:
            # A disconnect during model replacement must still reap the old process.
            await asyncio.shield(self.close_task)
            raise

    async def _close(self) -> None:
        if self.process.is_alive():
            self.process.terminate()
        await asyncio.to_thread(self.process.join, 5)
        if self.process.is_alive():
            self.process.kill()
            await asyncio.to_thread(self.process.join)
        self.pipe.close()
        self.process.close()
