from dataclasses import replace

import pytest
from fastapi.testclient import TestClient
from starlette.websockets import WebSocketDisconnect


from app.main import create_app
from app.worker import finite_logs


@pytest.mark.parametrize(
    "raw",
    [
        "not json",
        "[]",
        "null",
        '{"type":"infer","type":"train"}',
        '{"type":"train","model":{"input":{"shape":[NaN]}}}',
        '{"type":5}',
        '{"type":"unknown"}',
    ],
)
def test_invalid_json_and_unknown_messages(settings, dataset, raw):
    with TestClient(create_app(settings, dataset)) as client:
        with client.websocket_connect("/ws") as socket:
            socket.send_text(raw)
            result = socket.receive_json()
            assert result["type"] == "error" and result["code"] == "invalid_request"
            assert isinstance(result["request_type"], str)


def test_binary_rejected_and_new_session_has_no_model(settings, dataset):
    with TestClient(create_app(settings, dataset)) as client:
        for _ in range(2):
            with client.websocket_connect("/ws") as socket:
                socket.send_bytes(b"{}")
                assert socket.receive_json()["code"] == "invalid_request"
                socket.send_json({"type": "infer", "image_id": "mnist-test-0"})
                assert socket.receive_json()["code"] == "no_model"
                socket.send_json({"type": "cancel_training"})
                assert socket.receive_json()["code"] == "no_training"


@pytest.mark.parametrize("value", [float("nan"), float("inf"), float("-inf")])
def test_non_finite_keras_metrics_fail(value):
    with pytest.raises(ValueError):
        finite_logs({"loss": value})


def test_only_actual_keras_metrics_are_kept():
    assert finite_logs({"loss": 0.5}) == {"loss": 0.5}


def test_session_limit_and_slot_release(settings, dataset):
    with TestClient(create_app(replace(settings, max_sessions=1), dataset)) as client:
        with client.websocket_connect("/ws"):
            with client.websocket_connect("/ws") as rejected:
                with pytest.raises(WebSocketDisconnect) as error:
                    rejected.receive_json()
                assert error.value.code == 1013
        with client.websocket_connect("/ws") as accepted:
            accepted.send_json({"type": "infer", "image_id": "mnist-test-0"})
            assert accepted.receive_json()["code"] == "no_model"


def test_oversized_message_closes_connection(settings, dataset):
    with TestClient(create_app(replace(settings, max_message_bytes=64), dataset)) as client:
        with client.websocket_connect("/ws") as socket:
            socket.send_text("x" * 65)
            with pytest.raises(WebSocketDisconnect) as error:
                socket.receive_json()
            assert error.value.code == 1009
