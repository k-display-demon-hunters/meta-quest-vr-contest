"""Strict wire schema and shape validation, before allocating a Keras model."""

from typing import Annotated, Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

Activation = Literal["linear", "relu", "sigmoid", "tanh", "softmax"]
PositiveInt = Annotated[int, Field(strict=True, gt=0)]
Pair = Annotated[list[PositiveInt], Field(min_length=2, max_length=2)]


class StrictModel(BaseModel):
    model_config = ConfigDict(extra="forbid", strict=True)


class Input(StrictModel):
    shape: Annotated[list[PositiveInt], Field(min_length=3, max_length=3)]


class Conv2D(StrictModel):
    type: Literal["conv2d"]
    filters: Annotated[PositiveInt, Field(le=256)]
    kernelSize: Pair
    activation: Activation


class MaxPooling2D(StrictModel):
    type: Literal["max_pooling2d"]
    poolSize: Pair


class Flatten(StrictModel):
    type: Literal["flatten"]


class Dense(StrictModel):
    type: Literal["dense"]
    units: Annotated[PositiveInt, Field(le=4096)]
    activation: Activation


Layer = Annotated[Conv2D | MaxPooling2D | Flatten | Dense, Field(discriminator="type")]


class ModelDefinition(StrictModel):
    modelType: Literal["mlp", "cnn"]
    input: Input
    layers: Annotated[list[Layer], Field(min_length=1, max_length=32)]

    @model_validator(mode="after")
    def validate_architecture(self) -> "ModelDefinition":
        if self.input.shape != [28, 28, 1]:
            raise ValueError("input.shape must be [28, 28, 1]")
        is_cnn = any(isinstance(layer, (Conv2D, MaxPooling2D)) for layer in self.layers)
        if self.modelType != ("cnn" if is_cnn else "mlp"):
            raise ValueError("modelType must match the presence of Conv2D or Pooling")
        final = self.layers[-1]
        if not isinstance(final, Dense) or final.units != 10 or final.activation != "softmax":
            raise ValueError("The final layer must be Dense(10, softmax)")

        height, width, channels = self.input.shape
        features = 784 if isinstance(self.layers[0], Dense) else None
        parameters = 0
        for layer in self.layers:
            if isinstance(layer, Dense):
                if features is None:
                    raise ValueError("Insert Flatten between spatial layers and Dense")
                parameters += (features + 1) * layer.units
                features = layer.units
            else:
                if features is not None:
                    raise ValueError("Only Dense may follow Dense or Flatten")
                if isinstance(layer, Flatten):
                    features = height * width * channels
                elif isinstance(layer, Conv2D):
                    kh, kw = layer.kernelSize
                    parameters += (kh * kw * channels + 1) * layer.filters
                    height, width = height - kh + 1, width - kw + 1
                    channels = layer.filters
                else:
                    ph, pw = layer.poolSize
                    height, width = height // ph, width // pw
                if height < 1 or width < 1:
                    raise ValueError("Kernel/pool size produces an empty spatial dimension")
                if height * width * channels > 200000:
                    raise ValueError("A layer exceeds the 200000 activation limit")
            if parameters > 2000000:
                raise ValueError("Model exceeds the 2000000 parameter limit")
        return self


class TrainRequest(StrictModel):
    type: Literal["train"]
    model: ModelDefinition
    epoch: PositiveInt | None = None


class InferRequest(StrictModel):
    type: Literal["infer"]
    image_id: Annotated[str, Field(min_length=1, max_length=128)]


class CancelRequest(StrictModel):
    type: Literal["cancel_training"]
