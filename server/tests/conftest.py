import json
from pathlib import Path

import numpy as np
import pytest

from app.config import Settings
from app.dataset import Dataset


@pytest.fixture
def mlp():
    return json.loads(Path("examples/mlp.json").read_text())


@pytest.fixture
def cnn():
    return json.loads(Path("examples/cnn.json").read_text())


@pytest.fixture
def settings(tmp_path):
    return Settings(data_dir=tmp_path)


@pytest.fixture
def dataset():
    data = Dataset.__new__(Dataset)
    data.images = np.zeros((10000, 28, 28), dtype="uint8")
    data.labels = np.arange(10000, dtype="uint16") % 10
    return data
