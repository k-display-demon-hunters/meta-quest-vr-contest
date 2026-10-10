import math
import multiprocessing
from dataclasses import replace
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from app.main import create_app

pytestmark = pytest.mark.integration


@pytest.mark.parametrize("model_name", ["mlp", "cnn"])
def test_real_keras_training_and_inference(settings, request, model_name):
    model = request.getfixturevalue(model_name)
    configured = replace(
        settings, data_dir=Path(".data").resolve(), train_samples=256, default_epochs=2
    )
    with TestClient(create_app(configured)) as client:
        with client.websocket_connect("/ws") as socket:
            socket.send_json({"type": "train", "model": model})
            assert socket.receive_json() == {"type": "training_started", "epochs": 2}
            progress = socket.receive_json()
            assert progress["type"] == "training_progress", progress
            assert progress["epoch"] == 1 and progress["epochs"] == 2
            assert set(progress["logs"]) == {"loss", "accuracy", "val_loss", "val_accuracy"}
            completed = socket.receive_json()
            assert completed["type"] == "training_completed", completed
            assert completed["epoch"] == 2
            assert set(completed["logs"]) == {"loss", "accuracy", "val_loss", "val_accuracy"}
            assert set(completed["evaluation"]) == {"loss", "accuracy"}
            for metrics in (progress["logs"], completed["logs"], completed["evaluation"]):
                assert all(math.isfinite(value) for value in metrics.values())
                assert 0 <= metrics["accuracy"] <= 1
                if "val_accuracy" in metrics:
                    assert 0 <= metrics["val_accuracy"] <= 1
            socket.send_json({"type": "infer", "image_id": "mnist-test-42"})
            result = socket.receive_json()
            assert result["type"] == "inference_result", result
            assert result["image_id"] == "mnist-test-42" and len(result["output"]) == 10
            assert sum(result["output"]) == pytest.approx(1, abs=0.00001)
            socket.send_json({"type": "cancel_training"})
            assert socket.receive_json()["code"] == "no_training"
        with client.websocket_connect("/ws") as socket:
            socket.send_json({"type": "infer", "image_id": "mnist-test-42"})
            assert socket.receive_json()["code"] == "no_model"
    assert not multiprocessing.active_children()


def test_real_cancellation_and_disconnect(settings, mlp):
    configured = replace(settings, data_dir=Path(".data").resolve(), train_samples=256)
    with TestClient(create_app(configured)) as client:
        with client.websocket_connect("/ws") as socket:
            socket.send_json({"type": "train", "model": mlp, "epoch": 100})
            assert socket.receive_json() == {"type": "training_started", "epochs": 100}
            assert socket.receive_json()["type"] == "training_progress"
            socket.send_json({"type": "cancel_training"})
            while True:
                result = socket.receive_json()
                if result["type"] != "training_progress":
                    break
            assert result == {"type": "training_cancelled"}
            socket.send_json({"type": "infer", "image_id": "mnist-test-42"})
            assert socket.receive_json()["code"] == "no_model"
        with client.websocket_connect("/ws") as socket:
            socket.send_json({"type": "train", "model": mlp, "epoch": 10})
            # Busy proves the request was accepted before disconnecting.
            socket.send_json({"type": "infer"})
            assert socket.receive_json()["code"] == "busy"
    assert not multiprocessing.active_children()
