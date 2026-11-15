# Recomputed corridor results

Existing inputs, 2/2/4 maps, template scoring and softmax multiplier 8 preserved.
Convolution / pooling / output contribution consistency checked during export. Unity not run.

- Digit 1: scores [1.0, 0.6254851222038269, 0.5000247359275818], probabilities [0.936066746711731, 0.04678523540496826, 0.017148053273558617]
- Digit 2: scores [0.675479531288147, 1.0, 0.7204709649085999], probabilities [0.06310971826314926, 0.8464394211769104, 0.09045082330703735]
- Digit 3: scores [0.6147812604904175, 0.820257842540741, 1.0], probabilities [0.03575083613395691, 0.18500566482543945, 0.779243528842926]

## Export read-back

Digit 1: original RGB arrays identical; exported Conv/Pool/channel contributions agree with independent direct calculation (absolute tolerance 3e-6).

Digit 2: original RGB arrays identical; exported Conv/Pool/channel contributions agree with independent direct calculation (absolute tolerance 3e-6).

Digit 3: original RGB arrays identical; exported Conv/Pool/channel contributions agree with independent direct calculation (absolute tolerance 3e-6).

Room 1-2: C# literals match quantized fitted weights; nonnegative, sum one; zero G/B and bias preserved.
