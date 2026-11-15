"""Fit small teaching filters, then rebake the existing three corridor inputs.

NumPy least-squares feature regression, NOT end-to-end classification training.
The first room keeps R-only, nonnegative sum-one weights. The corridor keeps
3->2->pool->4 channels and its existing prototype-score calculation.
"""
import argparse
import gzip
from pathlib import Path
import re
import struct
from urllib.request import urlopen

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
DATA = HERE.parent
CS = ROOT / 'client/Assets/Scripts/CNNFeatureMapLab/ExecutionData'
SEED = 20261115
BASE = 'https://storage.googleapis.com/cvdf-datasets/mnist/'
SMOOTH = np.array([[1, 2, 1], [2, 4, 2], [1, 2, 1]], np.float32)/16
DIRECTIONS = np.array([
    [[-.5, 1, -.5]]*3,
    [[-.5]*3, [1]*3, [-.5]*3],
    [[1, 2, 1], [2, 4, 2], [1, 2, 1]],
    [[-.5, -.5, 1], [-.5, 1, -.5], [1, -.5, -.5]],
], np.float32)
DIRECTIONS[2] /= 16


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


def read_masks():
    raw = (DATA/'MnistTraining.bytes').read_bytes()
    if struct.unpack_from('<4s5i', raw) != (b'CMNT', 2, 28, 28, 3, 480):
        raise ValueError('Expected existing training dataset')
    dt = np.dtype([('index', '<i4'), ('label', 'u1'), ('image', 'u1', (28, 28, 3))])
    records = np.frombuffer(raw, dtype=dt, offset=24)
    records = records[records['label'] != 0]
    wanted = dict(zip(records['index'].tolist(), records['label'].tolist()))
    masks, labels, indices = [], [], []
    with urlopen(BASE+'train-images-idx3-ubyte.gz', timeout=60) as ir, urlopen(BASE+'train-labels-idx1-ubyte.gz', timeout=60) as lr, gzip.GzipFile(fileobj=ir) as images, gzip.GzipFile(fileobj=lr) as truth:
        if struct.unpack('>4I', images.read(16)) != (2051, 60000, 28, 28) or struct.unpack('>2I', truth.read(8)) != (2049, 60000):
            raise ValueError('Unexpected IDX headers')
        for index in range(max(wanted)+1):
            pixels, label = images.read(784), truth.read(1)[0]
            if index in wanted:
                if label != wanted[index]: raise ValueError('Source label mismatch')
                mask = np.asarray(Image.frombytes('L', (28, 28), pixels).resize((16, 16), Image.Resampling.BILINEAR), np.float32)/255
                masks.append(mask); labels.append(label); indices.append(index)
    if len(masks) != 360: raise ValueError('Missing source masks')
    return np.stack(masks), np.array(labels), np.array(indices)


def regress(x, target, regularization=.001):
    a = patches(x).reshape(-1, 9*x.shape[-1]).astype(np.float64)
    a = np.column_stack([a, np.ones(len(a))])
    y = target.reshape(-1, target.shape[-1]).astype(np.float64)
    gram = a.T @ a / len(a)
    penalty = np.eye(gram.shape[0])*regularization
    penalty[-1, -1] = 0
    coef = np.linalg.solve(gram+penalty, a.T @ y / len(a))
    return coef[:-1].reshape(9, x.shape[-1], target.shape[-1]).astype(np.float32), coef[-1].astype(np.float32)


def simplex(v):
    ordered = np.sort(v)[::-1]
    cumulative = np.cumsum(ordered)-1
    rho = np.flatnonzero(ordered-cumulative/np.arange(1, len(v)+1)>0)[-1]
    return np.maximum(v-cumulative[rho]/(rho+1), 0)


def fit(inputs):
    masks, labels, indices = read_masks()
    rng = np.random.default_rng(SEED)
    valid = np.zeros(len(masks), bool)
    for label in (1, 2, 3): valid[rng.permutation(np.flatnonzero(labels==label))[:12]] = True
    # Use the existing corridor's color domain without changing its inputs.
    flat = inputs.reshape(-1, 3)
    fg, bg = flat[flat[:, 0]>.7].mean(axis=0), flat[flat[:, 0]<.6].mean(axis=0)
    clean = bg + masks[..., None]*(fg-bg)
    noisy = np.clip(clean+rng.normal(0, .10, clean.shape), 0, 1).astype(np.float32)
    smooth_mask = conv(masks[..., None], SMOOTH.reshape(9, 1, 1), np.zeros(1))[0][..., 0]
    target1 = np.stack([smooth_mask, 1-smooth_mask], axis=-1)
    w1, b1 = regress(noisy[~valid], target1[~valid])
    first = conv(noisy, w1, b1)[0]
    pooled, _ = pool(first)
    clean_pool, _ = pool(masks[..., None])
    _, target2, _ = conv(clean_pool, DIRECTIONS.reshape(4, 9).T[:, None, :], np.zeros(4))
    w2, b2 = regress(pooled[~valid], target2[~valid], .003)
    # A convex denoising filter for room 1-2: zero bias, no negative weights,
    # no G/B contribution, sum one. The current voice and 0..255 display stay valid.
    a = patches(noisy[~valid, :, :, :1])[:, 1:-1, 1:-1].reshape(-1, 9).astype(np.float64)
    y = clean[~valid, 1:-1, 1:-1, 0].ravel().astype(np.float64)
    gram, rhs = a.T@a/len(a), a.T@y/len(a)
    red = np.ones(9)/9
    step = 1/(2*np.linalg.eigvalsh(gram).max())
    for _ in range(1500): red = simplex(red-step*2*(gram@red-rhs))
    # Two decimal labels and exact same quantized weights in runtime arithmetic.
    scaled = red*100
    units = np.floor(scaled).astype(int)
    units[np.argsort(-(scaled-units), kind='stable')[:100-units.sum()]] += 1
    red = (units/100).astype(np.float32)
    a_valid = patches(noisy[valid, :, :, :1])[:, 1:-1, 1:-1].reshape(-1, 9)
    y_valid = clean[valid, 1:-1, 1:-1, 0].ravel()
    second = conv(pooled, w2, b2)[0]
    metrics = [
        f'Seed: {SEED}; fitting 324 images; validation 36 images (12/class); test not read.',
        f'Room 1-2 validation denoising MSE: equal 1/9 = {np.mean((a_valid.mean(axis=1)-y_valid)**2):.8f}, fitted/quantized = {np.mean((a_valid@red-y_valid)**2):.8f}',
        f'Room 2 validation Conv1 target MSE: {np.mean((first[valid]-target1[valid])**2):.8f}',
        f'Room 2 validation Conv2 target MSE: {np.mean((second[valid]-np.maximum(target2[valid],0))**2):.8f}',
        f'R-only 3x3 weights: {units.reshape(3,3).tolist()} / 100; sum = {units.sum()}/100.',
        f'Recolor foreground RGB: {fg.tolist()}; background RGB: {bg.tolist()}.',
        'These are supervised feature-regression objectives, not classification accuracy.',
    ]
    for name, value in dict(red=red, conv1=w1, bias1=b1, conv2=w2, bias2=b2,
                            training_indices=indices[~valid], validation_indices=indices[valid]).items():
        np.save(HERE/f'{name}.npy', value, allow_pickle=False)
    (HERE/'fit-report.md').write_text('# Filter fitting\n\n'+'\n\n'.join(metrics)+'\n')
    print('\n'.join(metrics), flush=True)


def lit(v):
    return format(float(v), '.9g')+'f'


def arr(a):
    return 'new[] { '+', '.join(lit(v) for v in np.ravel(a))+' }'


def kernel_block(name, w):
    return '        private static readonly float[][][] '+name+' =\n        {\n'+',\n'.join('            new[] { '+', '.join(arr(w[:, c, m]) for c in range(w.shape[1]))+' }' for m in range(w.shape[2]))+'\n        };\n'


def bake(inputs, input_blocks, apply):
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
                 '            // Recomputed from fitted weights; original input pixels preserved.', input_blocks[d]]
        def variable(name, values):
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
        (export/f'Digit{d+1}ExecutionData.cs').write_text('\n'.join(lines)+'\n')
    shared=(CS/'DigitExecutionData.cs').read_text()
    start=shared.index('        // The saved convolution results') if '        // The saved convolution results' in shared else shared.index('        // Fitted feature-regression')
    end=shared.index('        private static readonly DigitExecutionData[] Digits', start)
    shared=shared[:start]+'        // Fitted feature-regression kernels; see mnist-color-data/learned-filters.\n'+kernel_block('FirstKernels',weights['conv1'])+kernel_block('SecondKernels',weights['conv2'])+'\n'+shared[end:]
    (export/'DigitExecutionData.cs').write_text(shared)
    scales='namespace FeatureMapCorridor\n{\n    internal sealed partial class DigitExecutionData\n    {\n        private static readonly float[][] DisplayScales =\n        {\n            '+',\n            '.join(arr(a) for a in (scale1,scale1,scale2))+'\n        };\n\n        private static readonly float[] OutputScales = '+arr(np.maximum(output_parts.max(axis=(1,2,3)),.001))+';\n    }\n}\n'
    (export/'ExecutionDisplayScales.cs').write_text(scales)
    if apply:
        for path in export.glob('*.cs'): (CS/path.name).write_text(path.read_text())
        picture=ROOT/'client/Assets/Scripts/CNNConvolutionLab/Picture/PictureConvolution.cs'
        text=picture.read_text()
        red_literal = 'new[] { '+', '.join(f'{v:.2f}f' for v in weights['red'])+' }'
        text=re.sub(r'private static readonly float\[\] RedWeights = .*?;', 'private static readonly float[] RedWeights = '+red_literal+';', text)
        picture.write_text(text)
    preview(inputs, first, pooled, second, scale1, scale2)
    report=['# Recomputed corridor results', '', 'Existing inputs, 2/2/4 maps, template scoring and softmax multiplier 8 preserved.',
            'Convolution / pooling / output contribution consistency checked during export. Unity not run.', '']
    report += [f'- Digit {d+1}: scores {scores[d].tolist()}, probabilities {probabilities[d].tolist()}' for d in range(3)]
    (HERE/'export-report.md').write_text('\n'.join(report)+'\n')
    print('\n'.join(report),flush=True)


def preview(inputs, first, pooled, second, s1, s2):
    canvas=Image.new('RGB',(1350,650),'#172333'); draw=ImageDraw.Draw(canvas)
    font=ImageFont.load_default(size=16)
    for col,title in enumerate(['Original','Foreground','Background','Pool FG','Pool BG','Vertical','Horizontal','Shape','Diagonal']):
        draw.text((col*150+5,10),title,font=font,fill='white')
    for d in range(3):
        maps=[inputs[d]]+[first[d,:,:,c]/s1[c] for c in range(2)]+[pooled[d,:,:,c]/s1[c] for c in range(2)]+[second[d,:,:,c]/s2[c] for c in range(4)]
        for col,a in enumerate(maps):
            if a.ndim==2: a=np.stack([a*.2,a*.85,a],axis=-1)
            tile=Image.fromarray(np.rint(np.clip(a,0,1)*255).astype(np.uint8))
            size=64 if col in (3,4) else 128
            canvas.paste(tile.resize((size,size),Image.Resampling.NEAREST),(col*150+5,40+d*200))
        draw.text((5,175+d*200),f'Digit {d+1}: 16x16 -> max pool 8x8; shared fitted weights',font=font,fill='white')
    canvas.save(HERE/'preview.png')


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--fit',action='store_true',help='Fetch selected source masks and fit weights')
    parser.add_argument('--apply',action='store_true',help='Write generated arrays and red weights to existing Unity sources')
    args=parser.parse_args()
    inputs, blocks=current_inputs()
    if args.fit: fit(inputs)
    bake(inputs,blocks,args.apply)


if __name__=='__main__': main()
