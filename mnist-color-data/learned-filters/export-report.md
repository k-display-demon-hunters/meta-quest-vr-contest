# Recomputed corridor results

Existing inputs, 2/2/4 maps, template scoring and softmax multiplier 8 preserved.
Convolution / pooling / output contribution consistency and C# float32 map serialization checked during export. Unity not run.
Scene probabilities below use the existing prototype head, not the trained Dense classifier or held-out evaluation.

Exact checkpoint tensors confirmed, including R channel 7 (zero-based).
Conv1 export vs TensorFlow maximum absolute error: 1.1920929e-06.
Pool export vs TensorFlow maximum absolute error: 1.1920929e-06.
Conv2 export vs TensorFlow maximum absolute error: 1.69873238e-06.

- Digit 1: scores [1.0, 0.6822416186332703, 0.5455721020698547], probabilities [0.9049148559570312, 0.07122001051902771, 0.0238651130348444]
- Digit 2: scores [0.7251421213150024, 1.0, 0.6238488554954529], probabilities [0.0956071987748146, 0.8618758320808411, 0.04251691326498985]
- Digit 3: scores [0.7808963656425476, 0.84010910987854, 1.0], probabilities [0.11937684565782547, 0.19171063601970673, 0.6889125108718872]
