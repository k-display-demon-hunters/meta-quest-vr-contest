"""Canonical MNIST test IDs use the unshuffled official test split."""

import hashlib
import os
import re
import tempfile
import urllib.request
from pathlib import Path

import numpy as np

MNIST_URL = "https://storage.googleapis.com/tensorflow/tf-keras-datasets/mnist.npz"
# Published by keras.datasets.mnist; verify both cached and downloaded data.
MNIST_SHA256 = "731c5ac602752760c8e48fbffcf8c3b850d9dc2a2aedcf2cc48468fc17b673d1"
IMAGE_ID = re.compile(r"mnist-test-(0|[1-9][0-9]*)\Z")


def ensure_dataset(data_dir: Path) -> Path:
    data_dir.mkdir(parents=True, exist_ok=True)
    path = data_dir / "mnist.npz"
    if path.exists():
        verify_checksum(path)
        return path
    fd, temporary = tempfile.mkstemp(dir=data_dir, suffix=".download")
    try:
        with os.fdopen(fd, "wb") as output:
            with urllib.request.urlopen(MNIST_URL, timeout=60) as response:
                while chunk := response.read(1024 * 1024):
                    output.write(chunk)
        verify_checksum(Path(temporary))
        os.replace(temporary, path)
    finally:
        Path(temporary).unlink(missing_ok=True)
    return path


def verify_checksum(path: Path) -> None:
    with path.open("rb") as source:
        digest = hashlib.file_digest(source, "sha256").hexdigest()
    if digest != MNIST_SHA256:
        raise ValueError(f"MNIST checksum mismatch: {path}; remove it and download again")


class Dataset:
    def __init__(self, path: Path):
        with np.load(path, allow_pickle=False) as data:
            self.images = data["x_test"]
            self.labels = data["y_test"]
        if self.images.shape != (10000, 28, 28) or self.labels.shape != (10000,):
            raise ValueError("Unexpected MNIST test split shape")

    def index(self, image_id: str) -> int:
        match = IMAGE_ID.fullmatch(image_id)
        if not match or len(match[1]) > 5 or int(match[1]) >= len(self.images):
            raise KeyError(image_id)
        return int(match[1])


if __name__ == "__main__":
    from app.config import Settings

    print(ensure_dataset(Settings.from_env().data_dir))
