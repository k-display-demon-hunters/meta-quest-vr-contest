"""Stream distinct MNIST handwriting and synthesize a single RGB dataset."""
import colorsys
import gzip
from pathlib import Path
import struct
from urllib.request import urlopen

import numpy as np
from PIL import Image, ImageDraw

from export_bytes import export_bytes

SEED = 20261107
BASE = 'https://storage.googleapis.com/cvdf-datasets/mnist/'
OUT = Path(__file__).resolve().parent


def read_exact(stream, size):
    data = stream.read(size)
    if len(data) != size:
        raise ValueError('Truncated MNIST stream')
    return data


def acquire():
    counts = [0] * 4
    seen = set()
    records = []
    with urlopen(BASE + 'train-images-idx3-ubyte.gz', timeout=60) as ir, \
         urlopen(BASE + 'train-labels-idx1-ubyte.gz', timeout=60) as lr, \
         gzip.GzipFile(fileobj=ir) as images, gzip.GzipFile(fileobj=lr) as labels:
        magic, count, h, w = struct.unpack('>4I', read_exact(images, 16))
        label_magic, label_count = struct.unpack('>2I', read_exact(labels, 8))
        if (magic, label_magic, h, w) != (2051, 2049, 28, 28) or count != label_count:
            raise ValueError('Unexpected MNIST IDX headers')
        for index in range(count):
            raw = read_exact(images, 784)
            label = read_exact(labels, 1)[0]
            if label < 4 and counts[label] < 150 and raw not in seen:
                seen.add(raw)
                records.append((index, label, np.frombuffer(raw, np.uint8).reshape(28, 28)))
                counts[label] += 1
            if counts == [150] * 4:
                break
    if counts != [150] * 4:
        raise ValueError(f'Insufficient unique originals: {counts}')
    return records, index + 1


def texture(rng, hue):
    # Colored base and subpixel circles cover the square, with no plate outline.
    yy, xx = np.mgrid[:28, :28]
    light = np.full((28, 28), 0.73)
    for y in np.arange(-1, 30, 2.3):
        for x in np.arange(-1, 30, 2.3):
            cx, cy = np.array([x, y]) + rng.uniform(-0.65, 0.65, 2)
            radius = rng.uniform(0.55, 1.15)
            circle = np.clip(radius + 0.5 - np.hypot(xx-cx, yy-cy), 0, 1)
            light += circle * rng.uniform(-0.17, 0.17)
    rgb = np.array(colorsys.hsv_to_rgb(hue, 0.64, 1.0))
    return light[..., None] * rgb * 255


def main():
    records, consumed = acquire()
    rng = np.random.default_rng(SEED)
    # Every class receives the exact same 150 style seeds in independent order.
    style_seeds = rng.integers(0, 2**32, 150, dtype=np.uint64)
    assignments = [rng.permutation(style_seeds) for _ in range(4)]
    ranks = [0] * 4
    result, seeds = [], []
    for _, label, gray in records:
        seed = int(assignments[label][ranks[label]])
        ranks[label] += 1
        style = np.random.default_rng(seed)
        hue = style.uniform(0, 1)
        other = (hue + style.uniform(0.38, 0.62)) % 1
        foreground, background = texture(style, hue), texture(style, other)
        mask = gray.astype(np.float64)[..., None] / 255
        result.append(np.rint(mask*foreground + (1-mask)*background).clip(0, 255).astype(np.uint8))
        seeds.append(seed)
    images = np.stack(result)
    labels = np.array([r[1] for r in records], dtype=np.uint8)
    arrays = dict(images=images, labels=labels,
                  source_indices=np.array([r[0] for r in records], dtype=np.int32),
                  style_seeds=np.array(seeds, dtype=np.uint64))
    for name, array in arrays.items():
        np.save(OUT / f'{name}.npy', array, allow_pickle=False)
    export_bytes(images, labels, arrays['source_indices'])
    # All 600 samples, 20 columns, nearest-neighbor 4x enlargement.
    preview = Image.new('RGB', (20*116, 30*128), '#dddddd')
    draw = ImageDraw.Draw(preview)
    for i, (pixels, label) in enumerate(zip(images, labels)):
        x, y = (i % 20)*116, (i // 20)*128
        preview.paste(Image.fromarray(pixels).resize((112, 112), Image.Resampling.NEAREST), (x, y))
        draw.text((x, y+113), f'{i}: {label}', fill='black')
    preview.save(OUT / 'preview.png')
    report = (f'images: {images.shape}, {images.dtype}, range {images.min()}..{images.max()}\n'
              f'labels: {labels.shape}, {labels.dtype}\n'
              f'class counts: {np.bincount(labels).tolist()}\n'
              f'unique source indices / original pixel arrays: 600 / 600\n'
              f'source records consumed: {consumed}\n'
              f'array bytes: images={images.nbytes}, labels={labels.nbytes}\n'
              f'seed: {SEED}\n')
    (OUT / 'generation-report.txt').write_text(report, encoding='utf-8')
    print(report)


if __name__ == '__main__':
    main()
