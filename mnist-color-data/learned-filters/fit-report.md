# Digit-label classification training

Python / TensorFlow 2.20.0 / Keras 3.15.1 / NumPy 2.3.5.

Seeds: (20261115, 20261116, 20261117); split seed 20261115; 200 epochs per candidate.

Existing colored RGB data, Pillow bilinear 28 -> 16, float32 /255; no recoloring or augmentation.

Digits 1/2/3: 324 training, 36 validation (12/class), 90 held-out test (30/class).

Adam, batch 32, learning rate .003 (epochs 1-120), .001 (121-200).

Only sparse categorical cross-entropy against digit labels. Random Glorot kernels, zero initial biases.

Select checkpoint/seed by lowest validation loss; ties keep the earlier candidate.

Test is opened only after both models and the R filter have been selected.

- corridor: selected seed 20261117, epoch 197; training loss/accuracy (0.028398685157299042, 0.9969135802469136); validation loss/accuracy (0.07488390803337097, 0.9722222222222222).

- red-classifier: selected seed 20261116, epoch 77; training loss/accuracy (0.13706640899181366, 0.9382716049382716); validation loss/accuracy (0.23613964021205902, 0.9166666666666666).

Room 1-2 selected conv1 channel (zero-based): 7.

Mean spatial standard deviation on validation per R channel: [0.0851997435092926, 0.04729805141687393, 0.04559251666069031, 0.0, 0.10075387358665466, 0.0, 0.04557697847485542, 0.10400840640068054].

R kernel (row-major): [[0.1284749060869217, 0.5557138323783875, -0.16575877368450165], [0.6202117800712585, 0.2748258113861084, -0.7774329781532288], [0.2052554041147232, -0.35644376277923584, -0.5069782137870789]].

Saved/exported kernels are the exact float32 checkpoint tensors: no scaling, rounding, sign flip, or hand correction.

Room 1-2 shows the selected zero-bias pre-ReLU convolution on raw R pixels (0..255), valid padding.

Room 2 preserves the entire 2 -> pool -> 4 convolution trunk; the scene retains its existing prototype output head.

- Held-out corridor: loss 0.16826227; accuracy 0.92222222 (83/90); confusion rows=true, columns=predicted 1/2/3: [[30, 0, 0], [5, 25, 0], [0, 2, 28]].

- Held-out red-classifier: loss 0.22207144; accuracy 0.90000000 (81/90); confusion rows=true, columns=predicted 1/2/3: [[30, 0, 0], [4, 23, 3], [0, 2, 28]].
