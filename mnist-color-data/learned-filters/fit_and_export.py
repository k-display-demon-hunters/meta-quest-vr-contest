"""Export genuine digit-classification filters and recompute the existing room data.

Training is in train_classifiers.py. Export alone uses NumPy/Pillow and has no
network access. The scene retains its existing prototype output calculation.
"""
import argparse
from pathlib import Path
import re

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
DATA = HERE.parent
CS = ROOT / 'client/Assets/Scripts/CNNFeatureMapLab/ExecutionData'


def current_inputs():
    blocks, inputs = [], []
    for digit in (1, 2, 3):
        text = (CS/f'Digit{digit}ExecutionData.cs').read_text()
        arrays, chunks = [], []
        for channel in 'RGB':
            match = re.search(r'            var input'+channel+r' = new float\[\]\s*\{(.*?)\n            \};', text, re.S)
            arrays.append(np.array([float(v.rstrip('f')) for v in match[1].replace('\n', '').split(',') if v.strip()], np.float32).reshape(16, 16))
            chunks.append(match[0])
        inputs.append(np.stack(arrays, axis=-1))
        blocks.append('\n'.join(chunks))
    return np.stack(inputs), blocks


def patches(x):
    padded = np.pad(x, ((0, 0), (1, 1), (1, 1), (0, 0)))
    return np.stack([padded[:, y:y+x.shape[1], z:z+x.shape[2]]
                     for y in range(3) for z in range(3)], axis=-2)


def conv(x, weights, bias):
    # float32, input-channel partial sums matching the C# lesson contract.
    p = patches(x)
    contributions = np.zeros(x.shape + (weights.shape[-1],), np.float32)
    for k in range(9):
        contributions += p[..., k, :, None] * weights[k]
    pre = contributions.sum(axis=-2) + bias
    return np.maximum(pre, 0), pre, contributions


def pool(x):
    n, h, w, c = x.shape
    windows = x.reshape(n, h//2, 2, w//2, 2, c).transpose(0, 1, 3, 2, 4, 5).reshape(n, h//2, w//2, 4, c)
    return windows.max(axis=-2), windows


def lit(v):
    return format(float(v), '.9g')+'f'


def arr(a):
    return 'new[] { '+', '.join(lit(v) for v in np.ravel(a))+' }'


def kernel_block(name, w):
    return '        private static readonly float[][][] '+name+' =\n        {\n'+',\n'.join('            new[] { '+', '.join(arr(w[:, c, m]) for c in range(w.shape[1]))+' }' for m in range(w.shape[2]))+'\n        };\n'


def check_model(inputs):
    # Independent TensorFlow forward pass checks the NumPy export arithmetic.
    from train_classifiers import tf
    tf.config.threading.set_inter_op_parallelism_threads(1)
    tf.config.threading.set_intra_op_parallelism_threads(1)
    model = tf.keras.models.load_model(HERE/'corridor.keras')
    weights = {name: np.load(HERE/f'{name}.npy', allow_pickle=False)
               for name in ('conv1', 'bias1', 'conv2', 'bias2', 'red')}
    for layer, bias in (('conv1', 'bias1'), ('conv2', 'bias2')):
        w, b = model.get_layer(layer).get_weights()
        if not np.array_equal(w.reshape(weights[layer].shape), weights[layer]) or not np.array_equal(b, weights[bias]):
            raise ValueError(f'{layer} differs from the trained checkpoint')
    red_model = tf.keras.models.load_model(HERE/'red-classifier.keras')
    red = red_model.get_layer('conv1').get_weights()[0].reshape(9, 8)
    channels = np.flatnonzero(np.all(red == weights['red'][:, None], axis=0))
    if len(channels) != 1:
        raise ValueError('R kernel must be one unchanged checkpoint channel')
    a, _, _ = conv(inputs, weights['conv1'], weights['bias1'])
    b, _ = pool(a)
    c, _, _ = conv(b, weights['conv2'], weights['bias2'])
    ta = model.get_layer('conv1')(inputs)
    tb = model.get_layer('pool')(ta)
    tc = model.get_layer('conv2')(tb)
    report = [f'Exact checkpoint tensors confirmed, including R channel {int(channels[0])} (zero-based).']
    for name, actual, expected in (('Conv1', a, ta), ('Pool', b, tb), ('Conv2', c, tc)):
        expected = expected.numpy()
        if not np.allclose(actual, expected, rtol=2e-5, atol=2e-6):
            raise ValueError(f'{name} differs from TensorFlow forward pass')
        report.append(f'{name} export vs TensorFlow maximum absolute error: {np.max(np.abs(actual-expected)):.9g}.')
    return report


def bake(inputs, input_blocks, apply, checks):
    weights = {name: np.load(HERE/f'{name}.npy', allow_pickle=False) for name in ('red', 'conv1', 'bias1', 'conv2', 'bias2')}
    first, pre1, part1 = conv(inputs, weights['conv1'], weights['bias1'])
    pooled, windows = pool(first)
    second, pre2, part2 = conv(pooled, weights['conv2'], weights['bias2'])
    scale1 = np.maximum(first.max(axis=(0,1,2)), .001)
    scale2 = np.maximum(second.max(axis=(0,1,2)), .001)
    templates = second/scale2
    scores = np.zeros((3,3), np.float32)
    for candidate in range(3):
        denom = max(.001, float(np.sum(templates[candidate]**2)))
        scores[:, candidate] = np.sum(second/scale2*templates[candidate], axis=(1,2,3))/denom
    exp = np.exp(8*(scores-scores.max(axis=1,keepdims=True)))
    probabilities = exp/exp.sum(axis=1,keepdims=True)
    output_parts = np.stack([second[d]*templates[d]/(scale2*max(.001,float(np.sum(templates[d]**2)))) for d in range(3)])
    if not np.allclose(output_parts.sum(axis=(1,2,3)), np.diag(scores), atol=1e-6): raise ValueError('Output contribution mismatch')
    if not np.array_equal(pooled, windows.max(axis=-2)): raise ValueError('Pooling mismatch')
    if not np.isfinite(second).all() or not np.allclose(first, np.maximum(part1.sum(axis=-2)+weights['bias1'],0)): raise ValueError('Invalid convolution')
    export = HERE/'export'
    export.mkdir(exist_ok=True)
    for d in range(3):
        lines = ['using System;', '', 'namespace FeatureMapCorridor', '{', '    internal sealed partial class DigitExecutionData', '    {', f'        private static DigitExecutionData CreateDigit{d+1}()', '        {',
                 '            // Recomputed from digit-classification checkpoint weights; original input pixels preserved.', input_blocks[d]]
        expected_arrays = {}
        def variable(name, values):
            expected_arrays[name] = values.ravel()
            flat = values.ravel()
            lines.extend(['            var '+name+' = new float[]', '            {'])
            lines.extend('                '+', '.join(lit(v) for v in flat[k:k+16])+',' for k in range(0,len(flat),16))
            lines.append('            };')
        for m in range(2):
            variable(f'first{m}', first[d,:,:,m]); variable(f'pool{m}', pooled[d,:,:,m])
            for c in range(3): variable(f'first{m}Source{c}', part1[d,:,:,c,m])
        for m in range(4):
            variable(f'second{m}', second[d,:,:,m]); variable(f'template{m}', templates[d,:,:,m]); variable(f'outputSource{m}', output_parts[d,:,:,m])
            for c in range(2): variable(f'second{m}Source{c}', part2[d,:,:,c,m])
        def saved(prefix, layer, acts, pres, parts, biases, kernels):
            result=[]
            for m in range(acts.shape[-1]):
                idx=int(np.argmax(acts[d,:,:,m])); y,x=divmod(idx,acts.shape[2])
                scales=np.maximum(np.max(np.abs(parts[d,:,:,:,m]),axis=(0,1)),.001)
                names=', '.join(f'{prefix}{m}Source{c}' for c in range(parts.shape[-2]))
                result.append(f'                    new SavedConvMap({prefix}{m}, new RoundCalculationResult({layer}, {m}, {d+1}, {idx}, {acts.shape[1]}, new[] {{ {names} }}, Array.Empty<float[]>(), {arr(scales)}, {arr(parts[d,y,x,:,m])}, {lit(pres[d,y,x,m])}, {lit(biases[m])}, {kernels}[{m}]))')
            return ',\n'.join(result)
        pool_rows=[]
        for m in range(2):
            idx=int(np.argmax(pooled[d,:,:,m])); y,x=divmod(idx,8); vals=windows[d,y,x,:,m]
            pool_rows.append(f'new PoolingCalculationResult({2*y*16+2*x}, {idx}, {int(vals.argmax())}, '+', '.join(lit(v) for v in vals)+', '+lit(vals.max())+')')
        lines += ['            return new DigitExecutionData(', '                new[] { inputR, inputG, inputB },', '                new[]', '                {',
                  saved('first',1,first,pre1,part1,weights['bias1'],'FirstKernels'), '                },', '                new[] { pool0, pool1 },', '                new[] { '+', '.join(pool_rows)+' },', '                new[]', '                {',
                  saved('second',3,second,pre2,part2,weights['bias2'],'SecondKernels'), '                },', '                new[] { template0, template1, template2, template3 },',
                  f'                new SavedOutput({arr(scores[d])}, {arr(probabilities[d])}, new[] {{ outputSource0, outputSource1, outputSource2, outputSource3 }}));', '        }', '    }', '}']
        path = export/f'Digit{d+1}ExecutionData.cs'
        path.write_text('\n'.join(lines)+'\n')
        # Verify decimal C# serialization preserves every saved float32 map value.
        saved_text = path.read_text()
        for name, expected in expected_arrays.items():
            body = re.search(r'var '+name+r' = new float\[\]\s*\{(.*?)\};', saved_text, re.S)[1]
            values = np.array([float(v.rstrip('f')) for v in body.replace('\n', '').split(',') if v.strip()], np.float32)
            if not np.array_equal(values, expected):
                raise ValueError(f'C# round-trip mismatch: digit {d+1} {name}')
    shared=(CS/'DigitExecutionData.cs').read_text()
    start=shared.rfind('        //', 0, shared.index('        private static readonly float[][][] FirstKernels'))
    end=shared.index('        private static readonly DigitExecutionData[] Digits', start)
    shared=shared[:start]+'        // Digit-label classification kernels; see mnist-color-data/learned-filters.\n'+kernel_block('FirstKernels',weights['conv1'])+kernel_block('SecondKernels',weights['conv2'])+'\n'+shared[end:]
    (export/'DigitExecutionData.cs').write_text(shared)
    scales='namespace FeatureMapCorridor\n{\n    internal sealed partial class DigitExecutionData\n    {\n        private static readonly float[][] DisplayScales =\n        {\n            '+',\n            '.join(arr(a) for a in (scale1,scale1,scale2))+'\n        };\n\n        private static readonly float[] OutputScales = '+arr(np.maximum(output_parts.max(axis=(1,2,3)),.001))+';\n    }\n}\n'
    (export/'ExecutionDisplayScales.cs').write_text(scales)
    if apply:
        for path in export.glob('*.cs'): (CS/path.name).write_text(path.read_text())
        picture=ROOT/'client/Assets/Scripts/CNNConvolutionLab/Picture/PictureConvolution.cs'
        text=picture.read_text()
        red_literal = arr(weights['red'])
        text=re.sub(r'private static readonly float\[\] RedWeights = .*?;', 'private static readonly float[] RedWeights = '+red_literal+';', text)
        picture.write_text(text)
    preview(inputs, first, pooled, second, scale1, scale2)
    report=['# Recomputed corridor results', '', 'Existing inputs, 2/2/4 maps, template scoring and softmax multiplier 8 preserved.',
            'Convolution / pooling / output contribution consistency and C# float32 map serialization checked during export. Unity not run.',
            'Scene probabilities below use the existing prototype head, not the trained Dense classifier or held-out evaluation.', '']
    report += checks + ['']
    report += [f'- Digit {d+1}: scores {scores[d].tolist()}, probabilities {probabilities[d].tolist()}' for d in range(3)]
    (HERE/'export-report.md').write_text('\n'.join(report)+'\n')
    print('\n'.join(report),flush=True)


def preview(inputs, first, pooled, second, s1, s2):
    canvas=Image.new('RGB',(1350,650),'#172333'); draw=ImageDraw.Draw(canvas)
    font=ImageFont.load_default(size=16)
    for col,title in enumerate(['Input','Conv1 #1','Conv1 #2','Pool #1','Pool #2','Conv2 #1','Conv2 #2','Conv2 #3','Conv2 #4']):
        draw.text((col*150+5,10),title,font=font,fill='white')
    for d in range(3):
        maps=[inputs[d]]+[first[d,:,:,c]/s1[c] for c in range(2)]+[pooled[d,:,:,c]/s1[c] for c in range(2)]+[second[d,:,:,c]/s2[c] for c in range(4)]
        for col,a in enumerate(maps):
            if a.ndim==2: a=np.stack([a*.2,a*.85,a],axis=-1)
            tile=Image.fromarray(np.rint(np.clip(a,0,1)*255).astype(np.uint8))
            size=64 if col in (3,4) else 128
            canvas.paste(tile.resize((size,size),Image.Resampling.NEAREST),(col*150+5,40+d*200))
        draw.text((5,175+d*200),f'Digit {d+1}: 16x16 -> max pool 8x8; shared classification-trained weights',font=font,fill='white')
    canvas.save(HERE/'preview.png')


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--train', action='store_true', help='Train classifiers against digit labels; requires TensorFlow')
    parser.add_argument('--apply',action='store_true',help='Write generated arrays and red weights to existing Unity sources')
    parser.add_argument('--check-model', action='store_true', help='Compare weights and all three forward passes against saved TensorFlow models')
    args=parser.parse_args()
    inputs, blocks=current_inputs()
    if args.train:
        from train_classifiers import train
        train()
    checks = check_model(inputs) if args.check_model else []
    bake(inputs,blocks,args.apply,checks)


if __name__=='__main__': main()
