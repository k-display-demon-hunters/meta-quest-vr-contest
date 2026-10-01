import asyncio
import json
import logging

from fastapi import WebSocket, WebSocketDisconnect
from pydantic import ValidationError

from app.config import Settings
from app.dataset import Dataset
from app.models import CancelRequest, InferRequest, TrainRequest
from app.worker import Worker

logger = logging.getLogger(__name__)


def unique_object(pairs: list[tuple]) -> dict:
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"Duplicate JSON key: {key}")
        result[key] = value
    return result


def reject_constant(value: str):
    raise ValueError(f"Non-finite JSON number: {value}")


class Session:
    def __init__(
        self, socket: WebSocket, settings: Settings, dataset: Dataset, worker_factory=Worker
    ):
        self.socket = socket
        self.settings = settings
        self.dataset = dataset
        self.worker_factory = worker_factory
        self.worker = None
        self.task = None
        self.operation = None
        self.has_model = False
        self.cancelling = False
        self.closed = False
        self.send_lock = asyncio.Lock()

    async def send(self, message: dict) -> None:
        async with self.send_lock:
            if not self.closed:
                await self.socket.send_json(message)

    async def error(self, request_type: str, code: str, message: str) -> None:
        await self.send(
            {"type": "error", "request_type": request_type, "code": code, "message": message}
        )

    async def serve(self) -> None:
        try:
            while True:
                message = await self.socket.receive()
                if message["type"] == "websocket.disconnect":
                    break
                raw = message.get("text")
                if raw is None:
                    await self.error("unknown", "invalid_request", "Send a UTF-8 JSON text object.")
                    continue
                if len(raw.encode("utf-8")) > self.settings.max_message_bytes:
                    await self.socket.close(code=1009)
                    break
                try:
                    request = json.loads(
                        raw, object_pairs_hook=unique_object, parse_constant=reject_constant
                    )
                    if not isinstance(request, dict):
                        raise ValueError("Expected a JSON object")
                except (ValueError, RecursionError):
                    await self.error("unknown", "invalid_request", "Invalid JSON object.")
                    continue
                await self.handle(request)
        except WebSocketDisconnect:
            pass
        finally:
            await self.close()

    async def handle(self, request: dict) -> None:
        kind = request.get("type")
        if not isinstance(kind, str):
            kind = "unknown"
        if kind in ("train", "infer") and self.operation is not None:
            await self.error(kind, "busy", "Another operation is running.")
            return
        try:
            if kind == "train":
                parsed = TrainRequest.model_validate(request)
                epochs = parsed.epoch or self.settings.default_epochs
                if epochs > self.settings.max_epochs:
                    raise ValueError(f"epoch must not exceed {self.settings.max_epochs}")
                self.operation = "train"
                self.has_model = False
                self.cancelling = False
                self.task = asyncio.create_task(self.train(parsed.model.model_dump(), epochs))
            elif kind == "infer":
                parsed = InferRequest.model_validate(request)
                if not self.has_model:
                    await self.error(kind, "no_model", "Train a model on this connection first.")
                    return
                try:
                    index = self.dataset.index(parsed.image_id)
                except KeyError:
                    await self.error(kind, "image_not_found", "Unknown MNIST test image ID.")
                    return
                self.operation = "infer"
                self.task = asyncio.create_task(self.infer(parsed.image_id, index))
            elif kind == "cancel_training":
                CancelRequest.model_validate(request)
                if self.operation != "train":
                    await self.error(kind, "no_training", "No training is running.")
                    return
                try:
                    if self.worker:
                        self.worker.cancel()
                    self.cancelling = True
                except Exception:
                    logger.exception("Could not signal cancellation")
                    await self.error(kind, "cancellation_failed", "Could not cancel training.")
            else:
                await self.error(kind, "invalid_request", "Unknown message type.")
        except (ValidationError, ValueError) as exception:
            # Never include the full input/model in a protocol error or server log.
            message = (
                exception.errors(include_input=False)[0]["msg"]
                if isinstance(exception, ValidationError)
                else str(exception)
            )
            await self.error(kind, "invalid_request", message)

    async def dispose_model(self) -> None:
        self.has_model = False
        if self.worker:
            worker, self.worker = self.worker, None
            await worker.close()

    async def train(self, model: dict, epochs: int) -> None:
        try:
            await self.dispose_model()
            self.worker = self.worker_factory(model, epochs, self.settings)
            if self.cancelling:
                self.worker.cancel()
            while True:
                result = await self.worker.receive()
                if result["type"] == "training_progress":
                    await self.send(result)
                    continue
                if result["type"] == "training_completed" and self.cancelling:
                    result = {"type": "training_cancelled"}
                if result["type"] != "training_completed":
                    await self.dispose_model()
                else:
                    self.has_model = True
                self.operation = None
                self.cancelling = False
                await self.send(result)
                return
        except asyncio.CancelledError:
            raise
        except Exception:
            logger.exception("Training operation failed")
            await self.dispose_model()
            self.operation = None
            self.cancelling = False
            await self.error("train", "training_failed", "Training worker failed.")

    async def infer(self, image_id: str, index: int) -> None:
        try:
            self.worker.infer(image_id, index)
            result = await self.worker.receive()
            self.operation = None
            await self.send(result)
        except asyncio.CancelledError:
            raise
        except Exception:
            logger.exception("Inference operation failed")
            # A dead native process cannot retain its model. Recover by retraining.
            await self.dispose_model()
            self.operation = None
            await self.error(
                "infer", "inference_failed", "Inference worker exited; retrain the model."
            )

    async def close(self) -> None:
        self.closed = True
        if self.task:
            self.task.cancel()
            await asyncio.gather(self.task, return_exceptions=True)
        await self.dispose_model()
