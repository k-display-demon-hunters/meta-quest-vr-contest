"""Train solely against digit labels; export unmodified float32 convolution weights."""
import csv
import os
from pathlib import Path
import struct

for key, value in {
    'TF_DETERMINISTIC_OPS': '1', 'TF_ENABLE_ONEDNN_OPTS': '0',
    'TF_CPP_MIN_LOG_LEVEL': '2', 'OPENBLAS_NUM_THREADS': '1',
    'OMP_NUM_THREADS': '1', 'MPLCONFIGDIR': '/tmp/mnist-filter-mpl',
    'XDG_CACHE_HOME': '/tmp/mnist-filter-cache',
}.items():
    os.environ[key] = value

import numpy as np
from PIL import Image
import tensorflow as tf

HERE = Path(__file__).resolve().parent
SEEDS = (20261115, 20261116, 20261117)
EPOCHS = 200


def read_dataset(name):
    raw = (HERE.parent / name).read_bytes()
    magic, version, h, w, channels, count = struct.unpack_from('<4s5i', raw)
    if (magic, version, h, w, channels) != (b'CMNT', 2, 28, 28, 3):
        raise ValueError('Expected the existing RGB dataset')
    dtype = np.dtype([('index', '<i4'), ('label', 'u1'), ('image', 'u1', (28, 28, 3))])
    if len(raw) != 24 + count * dtype.itemsize:
        raise ValueError('Invalid record count')
    records = np.frombuffer(raw, dtype=dtype, offset=24)
    records = np.sort(records[np.isin(records['label'], (1, 2, 3))], order='index')
    images = np.stack([np.asarray(Image.fromarray(row['image']).resize(
        (16, 16), Image.Resampling.BILINEAR)) for row in records]).astype(np.float32) / 255
    return images, records['label'].astype(np.int32) - 1, records['index']


def make_model(red_only):
    # No hand-set kernel, target map, mask, denoising loss, or directional loss.
    return tf.keras.Sequential([
        tf.keras.layers.Input((16, 16, 1 if red_only else 3)),
        tf.keras.layers.Conv2D(8 if red_only else 2, 3, padding='same',
                               activation='relu', use_bias=not red_only, name='conv1'),
        tf.keras.layers.MaxPooling2D(2, name='pool'),
        tf.keras.layers.Conv2D(8 if red_only else 4, 3, padding='same',
                               activation='relu', name='conv2'),
        tf.keras.layers.Flatten(),
        tf.keras.layers.Dense(3, name='logits'),
    ])


def metrics(model, x, labels):
    logits = model(x, training=False)
    loss = float(tf.reduce_mean(tf.keras.losses.sparse_categorical_crossentropy(
        labels, logits, from_logits=True)))
    accuracy = float(np.mean(np.argmax(logits.numpy(), axis=1) == labels))
    return loss, accuracy


def train():
    tf.config.threading.set_inter_op_parallelism_threads(1)
    tf.config.threading.set_intra_op_parallelism_threads(1)
    tf.config.experimental.enable_op_determinism()
    x, y, indices = read_dataset('MnistTraining.bytes')
    rng = np.random.default_rng(SEEDS[0])
    valid = np.zeros(len(y), bool)
    for label in range(3):
        valid[rng.permutation(np.flatnonzero(y == label))[:12]] = True
    np.save(HERE / 'training_indices.npy', indices[~valid], allow_pickle=False)
    np.save(HERE / 'validation_indices.npy', indices[valid], allow_pickle=False)
    report = [
        '# Digit-label classification training', '',
        f'Python / TensorFlow {tf.__version__} / Keras {tf.keras.__version__} / NumPy {np.__version__}.',
        f'Seeds: {SEEDS}; split seed {SEEDS[0]}; {EPOCHS} epochs per candidate.',
        'Existing colored RGB data, Pillow bilinear 28 -> 16, float32 /255; no recoloring or augmentation.',
        'Digits 1/2/3: 324 training, 36 validation (12/class), 90 held-out test (30/class).',
        'Adam, batch 32, learning rate .003 (epochs 1-120), .001 (121-200).',
        'Only sparse categorical cross-entropy against digit labels. Random Glorot kernels, zero initial biases.',
        'Select checkpoint/seed by lowest validation loss; ties keep the earlier candidate.',
        'Test is opened only after both models and the R filter have been selected.', '',
    ]
    selected = {}
    with (HERE / 'training-history.csv').open('w', newline='') as stream:
        writer = csv.writer(stream)
        writer.writerow(('model', 'seed', 'epoch', 'training_loss', 'training_accuracy',
                         'validation_loss', 'validation_accuracy'))
        for name, red_only in (('corridor', False), ('red-classifier', True)):
            xx = x[..., :1] if red_only else x
            best_loss, best = float('inf'), None
            for seed in SEEDS:
                tf.keras.backend.clear_session()
                tf.keras.utils.set_random_seed(seed)
                model = make_model(red_only)
                optimizer = tf.keras.optimizers.Adam(.003)
                model.compile(optimizer=optimizer, loss=tf.keras.losses.SparseCategoricalCrossentropy(from_logits=True))
                shuffle = np.random.default_rng(seed)
                training_rows = np.flatnonzero(~valid)
                for epoch in range(1, EPOCHS + 1):
                    if epoch == 121:
                        optimizer.learning_rate.assign(.001)
                    order = shuffle.permutation(training_rows)
                    for start in range(0, len(order), 32):
                        batch = order[start:start + 32]
                        model.train_on_batch(xx[batch], y[batch])
                    tr = metrics(model, xx[~valid], y[~valid])
                    va = metrics(model, xx[valid], y[valid])
                    writer.writerow((name, seed, epoch, *tr, *va))
                    if va[0] < best_loss:
                        best_loss, best = va[0], (seed, epoch, tr, va)
                        model.save(HERE / f'{name}.keras')
                    if epoch % 50 == 0:
                        print(f'{name} seed={seed} epoch={epoch}: train={tr}, validation={va}', flush=True)
            selected[name] = tf.keras.models.load_model(HERE / f'{name}.keras')
            report.append(f'- {name}: selected seed {best[0]}, epoch {best[1]}; training loss/accuracy {best[2]}; validation loss/accuracy {best[3]}.')

    corridor = selected['corridor']
    w1, b1 = corridor.get_layer('conv1').get_weights()
    w2, b2 = corridor.get_layer('conv2').get_weights()
    red_model = selected['red-classifier']
    red_kernels = red_model.get_layer('conv1').get_weights()[0]
    # Pick a naturally active, spatially varying channel on validation data.
    # No directional target and no use of the room's three display examples.
    activations = red_model.get_layer('conv1')(x[valid, ..., :1]).numpy()[:, 1:-1, 1:-1]
    spread = activations.std(axis=(1, 2)).mean(axis=0)
    channel = int(np.argmax(spread))
    red = red_kernels[:, :, 0, channel].reshape(9)
    for name, values in dict(red=red, conv1=w1.reshape(9, 3, 2), bias1=b1,
                             conv2=w2.reshape(9, 2, 4), bias2=b2).items():
        np.save(HERE / f'{name}.npy', values, allow_pickle=False)
    report += ['', f'Room 1-2 selected conv1 channel (zero-based): {channel}.',
               f'Mean spatial standard deviation on validation per R channel: {spread.tolist()}.',
               f'R kernel (row-major): {red.reshape(3, 3).tolist()}.',
               'Saved/exported kernels are the exact float32 checkpoint tensors: no scaling, rounding, sign flip, or hand correction.',
               'Room 1-2 shows the selected zero-bias pre-ReLU convolution on raw R pixels (0..255), valid padding.',
               'Room 2 preserves the entire 2 -> pool -> 4 convolution trunk; the scene retains its existing prototype output head.', '']

    tx, ty, test_indices = read_dataset('MnistTest.bytes')
    if np.intersect1d(indices, test_indices).size:
        raise ValueError('Train/test source overlap')
    for name, model in selected.items():
        data = tx[..., :1] if name == 'red-classifier' else tx
        loss, accuracy = metrics(model, data, ty)
        prediction = np.argmax(model(data, training=False).numpy(), axis=1)
        confusion = np.zeros((3, 3), dtype=int)
        np.add.at(confusion, (ty, prediction), 1)
        report.append(f'- Held-out {name}: loss {loss:.8f}; accuracy {accuracy:.8f} ({int(np.sum(prediction == ty))}/90); confusion rows=true, columns=predicted 1/2/3: {confusion.tolist()}.')
    (HERE / 'fit-report.md').write_text('\n\n'.join(line for line in report if line) + '\n')
    print('\n'.join(report), flush=True)


if __name__ == '__main__':
    train()
