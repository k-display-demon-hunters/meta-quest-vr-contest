import asyncio

from app.session import Session


class Socket:
    def __init__(self):
        self.messages = []

    async def send_json(self, message):
        self.messages.append(message)


class FakeWorker:
    instances = None

    def __init__(self, model, epochs, settings):
        self.model = model
        self.epochs = epochs
        self.queue = asyncio.Queue()
        self.closed = False
        self.cancelled = False
        self.inference = None
        self.instances.append(self)

    async def receive(self):
        result = await self.queue.get()
        if isinstance(result, Exception):
            raise result
        return result

    def cancel(self):
        self.cancelled = True

    def infer(self, image_id, index):
        self.inference = (image_id, index)

    async def close(self):
        self.closed = True


def make_session(settings, dataset):
    class Worker(FakeWorker):
        instances = []

    socket = Socket()
    return Session(socket, settings, dataset, Worker), socket, Worker.instances


async def start(session, mlp, **kwargs):
    await session.handle({"type": "train", "model": mlp, **kwargs})
    await asyncio.sleep(0)
    return session.worker


async def complete(session, epoch=1):
    session.worker.queue.put_nowait(
        {
            "type": "training_completed",
            "epoch": epoch,
            "logs": {"loss": 0.5, "accuracy": 0.8, "val_loss": 0.6, "val_accuracy": 0.7},
            "evaluation": {"loss": 0.6, "accuracy": 0.7},
        }
    )
    await asyncio.wait_for(session.task, 2)


def test_progress_completion_inference_and_retraining(settings, dataset, mlp):
    async def scenario():
        session, socket, workers = make_session(settings, dataset)
        worker = await start(session, mlp, epoch=2)
        worker.queue.put_nowait({"type": "training_started", "epochs": 2})
        await asyncio.sleep(0)
        assert socket.messages == [{"type": "training_started", "epochs": 2}]
        assert session.operation == "train" and not worker.closed
        worker.queue.put_nowait(
            {
                "type": "training_progress",
                "epoch": 1,
                "epochs": 2,
                "logs": {"loss": 0.6, "accuracy": 0.7, "val_loss": 0.7, "val_accuracy": 0.6},
            }
        )
        await complete(session, 2)
        assert [m["type"] for m in socket.messages] == [
            "training_started", "training_progress", "training_completed"
        ]
        assert session.has_model
        await session.handle({"type": "infer", "image_id": "mnist-test-42"})
        await asyncio.sleep(0)
        assert worker.inference == ("mnist-test-42", 42)
        worker.queue.put_nowait(
            {"type": "inference_result", "image_id": "mnist-test-42", "output": [2.0] * 10}
        )
        await session.task
        assert socket.messages[-1]["output"] == [2.0] * 10
        assert session.has_model
        await start(session, mlp)
        assert worker.closed and not session.has_model and len(workers) == 2
        await session.close()
        assert workers[-1].closed

    asyncio.run(scenario())


def test_busy_precedes_invalid_fields_and_does_not_end_training(settings, dataset, mlp):
    async def scenario():
        session, socket, _ = make_session(settings, dataset)
        await start(session, mlp)
        await session.handle({"type": "train", "model": {}})
        await session.handle({"type": "infer"})
        assert [m["code"] for m in socket.messages] == ["busy", "busy"]
        assert session.operation == "train"
        await complete(session)
        assert socket.messages[-1]["type"] == "training_completed"
        await session.close()

    asyncio.run(scenario())


def test_cancel_ack_waits_for_worker_and_deletes_model(settings, dataset, mlp):
    async def scenario():
        session, socket, _ = make_session(settings, dataset)
        worker = await start(session, mlp)
        await session.handle({"type": "cancel_training"})
        assert worker.cancelled and socket.messages == []
        await session.handle({"type": "infer"})
        assert socket.messages[-1]["code"] == "busy"
        # A completion not yet confirmed by the session loses to accepted cancellation.
        await complete(session)
        assert socket.messages[-1] == {"type": "training_cancelled"}
        assert worker.closed and not session.has_model
        await session.handle({"type": "infer", "image_id": "mnist-test-42"})
        assert socket.messages[-1]["code"] == "no_model"
        await session.close()

    asyncio.run(scenario())


def test_late_cancel_preserves_completed_model(settings, dataset, mlp):
    async def scenario():
        session, socket, _ = make_session(settings, dataset)
        await start(session, mlp)
        await complete(session)
        await session.handle({"type": "cancel_training"})
        assert socket.messages[-1]["code"] == "no_training"
        assert session.has_model
        await session.close()

    asyncio.run(scenario())


def test_training_failure_takes_priority_over_pending_cancel(settings, dataset, mlp):
    async def scenario():
        session, socket, _ = make_session(settings, dataset)
        worker = await start(session, mlp)
        await session.handle({"type": "cancel_training"})
        worker.queue.put_nowait(
            {
                "type": "error",
                "request_type": "train",
                "code": "training_failed",
                "message": "failed",
            }
        )
        await session.task
        assert socket.messages[-1]["code"] == "training_failed"
        assert worker.closed and not session.has_model
        await session.close()

    asyncio.run(scenario())


def test_image_and_inference_errors_preserve_model(settings, dataset, mlp):
    async def scenario():
        session, socket, _ = make_session(settings, dataset)
        worker = await start(session, mlp)
        await complete(session)
        await session.handle({"type": "infer", "image_id": "mnist-test-10000"})
        assert socket.messages[-1]["code"] == "image_not_found" and session.has_model
        await session.handle({"type": "infer", "image_id": "mnist-test-0"})
        await asyncio.sleep(0)
        worker.queue.put_nowait(
            {
                "type": "error",
                "request_type": "infer",
                "code": "inference_failed",
                "message": "failed",
            }
        )
        await session.task
        assert session.has_model and not worker.closed
        await session.close()

    asyncio.run(scenario())


def test_disconnect_drops_pending_results_and_closes_worker(settings, dataset, mlp):
    async def scenario():
        session, socket, _ = make_session(settings, dataset)
        worker = await start(session, mlp)
        await session.close()
        worker.queue.put_nowait({"type": "training_completed", "epoch": 5, "logs": {}})
        assert worker.closed and not session.has_model and socket.messages == []
        await session.close()

    asyncio.run(scenario())


def test_null_and_omitted_epochs_use_server_default(settings, dataset, mlp):
    async def scenario():
        session, _, _ = make_session(settings, dataset)
        for kwargs in ({}, {"epoch": None}):
            worker = await start(session, mlp, **kwargs)
            assert worker.epochs == settings.default_epochs
            await complete(session, settings.default_epochs)
        await session.close()

    asyncio.run(scenario())


def test_invalid_retraining_keeps_existing_model(settings, dataset, mlp):
    async def scenario():
        session, socket, _ = make_session(settings, dataset)
        worker = await start(session, mlp)
        await complete(session)
        await session.handle({"type": "train", "model": {}, "epoch": 1})
        await session.handle({"type": "train", "model": mlp, "epoch": settings.max_epochs + 1})
        assert all(message["code"] == "invalid_request" for message in socket.messages[1:])
        assert session.has_model and not worker.closed
        await session.close()

    asyncio.run(scenario())


def test_cancellation_failure_leaves_training_running(settings, dataset, mlp):
    async def scenario():
        session, socket, _ = make_session(settings, dataset)
        worker = await start(session, mlp)

        def fail():
            raise OSError("cancel unavailable")

        worker.cancel = fail
        await session.handle({"type": "cancel_training"})
        assert socket.messages[-1]["code"] == "cancellation_failed"
        assert session.operation == "train" and not session.cancelling
        await complete(session)
        assert session.has_model
        await session.close()

    asyncio.run(scenario())
