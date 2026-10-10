from copy import deepcopy

import pytest
from pydantic import ValidationError

from app.models import ModelDefinition, TrainRequest


def test_valid_architectures(mlp, cnn):
    assert ModelDefinition.model_validate(mlp).modelType == "mlp"
    assert ModelDefinition.model_validate(cnn).modelType == "cnn"
    mlp["layers"].insert(0, {"type": "flatten"})
    assert ModelDefinition.model_validate(mlp).modelType == "mlp"


@pytest.mark.parametrize(
    "mutation",
    [
        lambda m: m.update(modelType="cnn"),
        lambda m: m["input"].update(shape=[784]),
        lambda m: m["input"].update(shape=[28, 28, 3]),
        lambda m: m.update(unknown=1),
        lambda m: m["layers"][0].update(units=True),
        lambda m: m["layers"][0].update(units=2.5),
        lambda m: m["layers"][0].update(units=0),
        lambda m: m["layers"][0].update(activation="elu"),
        lambda m: m["layers"][0].update(connections=[]),
        lambda m: m["layers"][-1].update(units=9),
        lambda m: m["layers"][-1].update(activation="linear"),
        lambda m: m["layers"].append({"type": "flatten"}),
        lambda m: m.update(layers=[]),
        lambda m: m.update(
            layers=[
                {"type": "dense", "units": 4096, "activation": "relu"},
                {"type": "dense", "units": 10, "activation": "softmax"},
            ]
        ),
    ],
)
def test_invalid_mlp_rejected(mlp, mutation):
    invalid = deepcopy(mlp)
    mutation(invalid)
    with pytest.raises(ValidationError):
        ModelDefinition.model_validate(invalid)


@pytest.mark.parametrize(
    "mutation",
    [
        lambda m: m["layers"].pop(2),
        lambda m: m["layers"][0].update(kernelSize=[29, 3]),
        lambda m: m["layers"][0].update(kernelSize=[3]),
        lambda m: m["layers"][1].update(poolSize=[100, 100]),
        lambda m: m["layers"][1].update(activation="relu"),
        lambda m: m["layers"].insert(3, {"type": "max_pooling2d", "poolSize": [2, 2]}),
    ],
)
def test_invalid_cnn_rejected(cnn, mutation):
    mutation(cnn)
    with pytest.raises(ValidationError):
        ModelDefinition.model_validate(cnn)


@pytest.mark.parametrize("epoch", [True, 0, -1, 1.5, "5"])
def test_epoch_is_a_positive_integer(mlp, epoch):
    with pytest.raises(ValidationError):
        TrainRequest.model_validate({"type": "train", "model": mlp, "epoch": epoch})


def test_epoch_null_and_omitted(mlp):
    assert TrainRequest.model_validate({"type": "train", "model": mlp}).epoch is None
    assert TrainRequest.model_validate({"type": "train", "model": mlp, "epoch": None}).epoch is None
