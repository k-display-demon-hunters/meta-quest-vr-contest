"""Export existing RGB arrays as an uncompressed CMNT version 2 .bytes file."""
from pathlib import Path
import struct

import numpy as np

OUT = Path(__file__).resolve().parent
SPLIT_SEED = 20261108


def export_bytes(images, labels, source_indices):
    if images.shape != (600, 28, 28, 3) or images.dtype != np.uint8:
        raise ValueError('Expected 600 RGB uint8 images of size 28x28')
    if labels.shape != (600,) or labels.dtype != np.uint8:
        raise ValueError('Expected 600 uint8 labels')
    if not np.array_equal(np.bincount(labels, minlength=4), [150] * 4):
        raise ValueError('Expected 150 labels per class, 0 through 3')
    if (source_indices.shape != (600,)
            or not np.issubdtype(source_indices.dtype, np.integer)
            or np.any(source_indices < 0) or np.any(source_indices >= 60000)
            or len(np.unique(source_indices)) != 600):
        raise ValueError('Expected 600 distinct MNIST source indices')

    paths = [write_bytes('MnistColor.bytes', images, labels, source_indices)]
    rng = np.random.default_rng(SPLIT_SEED)
    training_rows, test_rows = [], []
    for label in range(4):
        rows = rng.permutation(np.flatnonzero(labels == label))
        training_rows.extend(rows[:120])
        test_rows.extend(rows[120:])
    for name, rows in (('MnistTraining.bytes', training_rows),
                       ('MnistTest.bytes', test_rows)):
        rows = np.sort(rows)
        paths.append(write_bytes(name, images[rows], labels[rows], source_indices[rows]))
    return paths


def write_bytes(name, images, labels, source_indices):
    count, height, width, channels = images.shape
    path = OUT / name
    with path.open('wb') as stream:
        stream.write(struct.pack('<4s5i', b'CMNT', 2, height, width, channels, count))
        for pixels, label, source_index in zip(images, labels, source_indices):
            stream.write(struct.pack('<iB', int(source_index), int(label)))
            stream.write(pixels.tobytes(order='C'))
    return path


def main():
    arrays = [np.load(OUT / f'{name}.npy', allow_pickle=False)
              for name in ('images', 'labels', 'source_indices')]
    for path in export_bytes(*arrays):
        print(f'{path.name}: {path.stat().st_size:,} bytes, CMNT v2')


if __name__ == '__main__':
    main()
