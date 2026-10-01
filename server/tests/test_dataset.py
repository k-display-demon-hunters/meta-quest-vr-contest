import pytest

from app.dataset import ensure_dataset


def test_corrupt_cached_dataset_is_rejected(tmp_path):
    path = tmp_path / "mnist.npz"
    path.write_bytes(b"corrupt dataset")
    with pytest.raises(ValueError, match="checksum mismatch"):
        ensure_dataset(tmp_path)
    assert path.read_bytes() == b"corrupt dataset"


@pytest.mark.parametrize(
    "image_id",
    [
        "mnist-train-42",
        "mnist-test--1",
        "mnist-test-01",
        "mnist-test-10000",
        "mnist-test-42x",
        "mnist-test-42\n",
    ],
)
def test_image_ids_are_exact_test_split_ids(dataset, image_id):
    with pytest.raises(KeyError):
        dataset.index(image_id)
