using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UI
{
    // Geometry is in metres; the parent is the existing world-space UI canvas.
    public sealed class SpatialConvolutionController : MonoBehaviour
    {
        private SpatialConvolutionViewBuilder _builder;
        private SpatialConvolutionViewBuilder.View _view;
        private SpatialConvolutionInteractionController _interaction;
        private SpatialVolumeInteractionController _volume;
        private int _channel;
        private int[,] _inputs;
        private int[,] _fixedWeights;
        private static readonly string[] Names = { "R", "G", "B" };

        public bool IsAligned => _interaction != null && _interaction.IsAligned;
        public SpatialVolumeInteractionController Volume => _volume;

        public void HighlightSpatialPair(int index)
        {
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    _view.Values[c, i].fontStyle = i == index ? FontStyle.Bold : FontStyle.Normal;
                    _view.VolumeWeightLabels[c, i].fontStyle = i == index ? FontStyle.Bold : FontStyle.Normal;
                    _view.VolumeWeightLabels[c, i].color = i == index ? new Color(1, 0.85f, 0.3f) : Color.white;
                    _view.Values[c, i].transform.parent.localScale = Vector3.one * (i == index ? 0.00125f : 0.001f);
                }
            }
        }

        public IEnumerator PrepareVolume()
        {
            _view.Kernel.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Slot.gameObject.SetActive(false);
            _view.SpatialGuide.gameObject.SetActive(true);
            yield return _volume.FitWholeFilter();
        }

        public IEnumerator PullColour(int channel)
        {
            _view.Kernel.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Slot.gameObject.SetActive(false);
            _view.SpatialGuide.gameObject.SetActive(true);
            yield return _volume.PullLayer(channel);
            _view.SpatialGuide.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        public bool EditorCanDrag(XRGrabInteractable target)
        {
            return _volume.CanDrag(target) || _interaction.CanDrag(target);
        }

        public void EditorRelease(XRGrabInteractable target)
        {
            if (_volume.CanDrag(target)) { _volume.Released(target); }
            else { _interaction.Release(target); }
        }
#endif

        public void Initialize(Font font)
        {
            transform.localScale = Vector3.one * 500f;
            transform.localRotation = Quaternion.Euler(0, -55, 0);
            transform.localPosition = new Vector3(0, 0, -180);
            _builder = new SpatialConvolutionViewBuilder(transform, font);
            _view = _builder.Build();
            _interaction = new SpatialConvolutionInteractionController(transform, _view);
            _volume = gameObject.AddComponent<SpatialVolumeInteractionController>();
            _volume.Initialize(this, _view.Layers, _view.ParkedKernels, _view.ColourPatch,
                _view.SpatialGuide, _builder.Material);
        }

        public void SetData(int[,] inputs, int[,] weights)
        {
            _inputs = inputs;
            _fixedWeights = weights;
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    _view.Values[c, i].text = inputs[c, i].ToString();
                    float strength = inputs[c, i] / 255f;
                    Color color = c == 0 ? new Color(strength, 0, 0) : c == 1 ? new Color(0, strength, 0) : new Color(0, 0, strength);
                    SpatialConvolutionViewBuilder.Tint(_view.Values[c, i].transform.parent.parent.Find("Tile mesh").GetComponent<MeshRenderer>(), color);
                    _view.Values[c, i].color = c == 1 && strength > 0.65f ? Color.black : Color.white;
                }
                for (int i = 0; i < 9; i++) { _view.VolumeWeightLabels[c, i].text = weights[c, i].ToString(); }
            }
            for (int i = 0; i < 9; i++) { SpatialConvolutionViewBuilder.Tint(_view.ColourTiles[i], new Color(inputs[0, i] / 255f, inputs[1, i] / 255f, inputs[2, i] / 255f)); }
        }

        public IEnumerator Observe(Vector3 source, bool extract = true)
        {
            HideAnswers();
            _view.Kernel.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Slot.gameObject.SetActive(false);
            _view.Flying.gameObject.SetActive(false);
            _view.WeightCopy.gameObject.SetActive(false);
            foreach (Transform slice in _view.ParkedKernels) { slice.gameObject.SetActive(false); }
            _view.SpatialGuide.gameObject.SetActive(extract);
            if (extract) { yield return _volume.Extract(source); }
            for (int c = 0; c < 3; c++) { _view.Layers[c].localPosition = Vector3.zero; }
            yield return SpreadColours(false);
        }

        public IEnumerator SpreadColours(bool combine)
        {
            _volume.StopInteraction();
            Vector3[] starts = new Vector3[3];
            _view.ColourPatch.gameObject.SetActive(false);
            for (int c = 0; c < 3; c++)
            {
                starts[c] = _view.Layers[c].localPosition;
                _view.Layers[c].gameObject.SetActive(true);
            }
            for (float time = 0; time < 1.2f; time += Time.unscaledDeltaTime)
            {
                for (int c = 0; c < 3; c++)
                {
                    Vector3 target = combine ? new Vector3(0, 0, 0.2f + c * 0.035f) : new Vector3((c - 1) * 0.65f, c * 0.06f, c * 0.6f);
                    _view.Layers[c].localPosition = Vector3.Lerp(starts[c], target, Mathf.SmoothStep(0, 1, time / 1.2f));
                }
                yield return null;
            }
            for (int c = 0; c < 3; c++)
            {
                _view.Layers[c].localPosition = combine ? new Vector3(0, 0, 0.2f + c * 0.035f) : new Vector3((c - 1) * 0.65f, c * 0.06f, c * 0.6f);
            }
            if (combine)
            {
                foreach (Transform layer in _view.Layers) { layer.gameObject.SetActive(false); }
                _view.ColourPatch.localPosition = new Vector3(0, 0, 0.2f);
                _view.ColourPatch.gameObject.SetActive(true);
            }
            else { _volume.Explore(); }
        }

        public IEnumerator Focus(int channel, Vector3 source)
        {
            _volume.StopInteraction();
            _view.SpatialGuide.gameObject.SetActive(false);
            _channel = channel;
            _view.ColourPatch.gameObject.SetActive(false);
            _view.Flying.gameObject.SetActive(false);
            _view.WeightCopy.gameObject.SetActive(false);
            _interaction.PrepareKernel();
            _view.Kernel.gameObject.SetActive(true);
            _view.Result.gameObject.SetActive(true);
            _view.Slot.gameObject.SetActive(true);
            _view.SumLabel.text = "합: ?";
            _view.KernelTitle.text = Names[channel] + " 고정 가중치 · Grip";
            for (int i = 0; i < 9; i++)
            {
                _view.Weights[i].text = _fixedWeights[channel, i].ToString();
                _view.Products[i].text = "?";
            }
            Vector3[] starts = new Vector3[3];
            for (int c = 0; c < 3; c++)
            {
                starts[c] = _view.Layers[c].localPosition;
            }
            for (float time = 0; time < 1; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time);
                for (int c = 0; c < 3; c++)
                {
                    _view.Layers[c].gameObject.SetActive(true);
                    _view.Layers[c].localPosition = Vector3.Lerp(starts[c], LayerPosition(c, channel), t);
                    _view.ParkedKernels[c].gameObject.SetActive(c != channel);
                    _view.ParkedKernels[c].localPosition = _view.Layers[c].localPosition + new Vector3(0.08f, 0.055f, -0.3f);
                }
                _view.Kernel.localPosition = Vector3.Lerp(new Vector3(-0.38f, 0, 0.6f), SpatialConvolutionInteractionController.KernelHome, t);
                yield return null;
            }
            for (int c = 0; c < 3; c++)
            {
                _view.Layers[c].localPosition = LayerPosition(c, channel);
                _view.ParkedKernels[c].localPosition = _view.Layers[c].localPosition + new Vector3(0.08f, 0.055f, -0.3f);
            }
            _interaction.EnableKernel();
        }

        public void AlignKernel()
        {
            _interaction.AlignKernel();
        }

        public IEnumerator MultiplyAndAccumulate(int channel, int index, int accumulated)
        {
            _interaction.LockAlignedKernel();
            int product = _inputs[channel, index] * _fixedWeights[channel, index];
            _view.Flying.gameObject.SetActive(true);
            Vector3 start = _view.Layers[channel].localPosition + Cell(index);
            Vector3 meeting = new Vector3(0.1f, 0.42f, -0.65f);
            _view.CalculationPath.enabled = true;
            _view.CalculationPath.SetPosition(0, _view.Layers[channel].TransformPoint(Cell(index)));
            _view.CalculationPath.SetPosition(1, _view.Kernel.TransformPoint(Cell(index)));
            _view.CalculationPath.SetPosition(2, _view.Result.TransformPoint(Cell(index)));
            _view.FlyingLabel.text = _inputs[channel, index].ToString();
            _view.WeightCopyLabel.text = "× (" + _fixedWeights[channel, index] + ")";
            _view.WeightCopy.gameObject.SetActive(true);
            Vector3 weightStart = _view.Kernel.localPosition + Cell(index);
            HighlightRow(index / 3);
            for (float time = 0; time < 0.7f; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / 0.7f);
                _view.Flying.localPosition = Vector3.Lerp(start, meeting + Vector3.left * 0.13f, t);
                _view.WeightCopy.localPosition = Vector3.Lerp(weightStart, meeting + Vector3.right * 0.13f, t);
                yield return null;
            }
            _view.WeightCopy.gameObject.SetActive(false);
            _view.Flying.localPosition = meeting;
            _view.FlyingLabel.text = $"{_inputs[channel, index]} × ({_fixedWeights[channel, index]}) = {product}";
            yield return new WaitForSecondsRealtime(0.4f);
            Vector3 result = _view.Result.localPosition + Cell(index);
            yield return Fly(meeting, result, 0.45f);
            _view.Products[index].text = product.ToString();
            _view.FlyingLabel.text = "(" + product + ")";
            yield return Fly(result, _view.Slot.localPosition, 0.4f);
            _view.SumLabel.text = "합: " + accumulated;
            _view.CalculationPath.enabled = false;
            _view.Flying.gameObject.SetActive(false);
        }

        public void ShowProducts(int channel, int count)
        {
            for (int i = 0; i < count; i++) { _view.Products[i].text = (_inputs[channel, i] * _fixedWeights[channel, i]).ToString(); }
        }

        public void ShowAnswer(int value)
        {
            _view.SumLabel.text = value.ToString();
        }

        public void HighlightRow(int row)
        {
            for (int i = 0; i < 9; i++)
            {
                bool selected = i / 3 == row;
                _view.Weights[i].color = selected ? new Color(1, 0.83f, 0.3f) : Color.white;
                _view.Products[i].color = selected ? new Color(1, 0.83f, 0.3f) : Color.white;
                _view.Values[_channel, i].fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        public void ShowAnswers(int[] options, Action<int> choose)
        {
            _view.SumLabel.text = "여기에 답 놓기";
            _interaction.ShowAnswers(options, choose);
        }

        public void HideAnswers()
        {
            _interaction.HideAnswers();
        }

        public IEnumerator CombineSums(int[] sums)
        {
            _interaction.DisableKernel();
            _view.Kernel.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            Vector3[] starts = new Vector3[3];
            for (int c = 0; c < 3; c++)
            {
                starts[c] = _view.Layers[c].localPosition;
                _view.ParkedKernels[c].gameObject.SetActive(true);
            }
            for (float time = 0; time < 1.2f; time += Time.unscaledDeltaTime)
            {
                for (int c = 0; c < 3; c++)
                {
                    Vector3 target = new Vector3(-0.38f, 0, 0.12f + c * 0.6f);
                    _view.Layers[c].localPosition = Vector3.Lerp(starts[c], target, Mathf.SmoothStep(0, 1, time / 1.2f));
                    _view.ParkedKernels[c].localPosition = _view.Layers[c].localPosition + new Vector3(0.08f, 0.055f, -0.3f);
                }
                yield return null;
            }
            _view.Flying.gameObject.SetActive(true);
            int total = 0;
            for (int c = 0; c < 3; c++)
            {
                _view.FlyingLabel.text = Names[c] + " 합 (" + sums[c] + ")";
                yield return Fly(_view.Layers[c].localPosition, _view.Slot.localPosition, 0.85f);
                total += sums[c];
                _view.SumLabel.text = total.ToString();
            }
            _view.FlyingLabel.text = "편향 0은 마지막에 한 번 더해요";
            _view.Flying.localPosition = new Vector3(0, 0.4f, -0.1f);
        }

        private IEnumerator Fly(Vector3 start, Vector3 end, float duration)
        {
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                _view.Flying.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, time / duration));
                yield return null;
            }
            _view.Flying.localPosition = end;
        }

        private static Vector3 LayerPosition(int c, int focus) => c == focus ? new Vector3(-0.38f, 0, 0.1f) : new Vector3(-0.38f + (c + 1) * 0.22f, 0.1f + c * 0.08f, 0.65f + c * 0.55f);
        private static Vector3 Cell(int index) => SpatialConvolutionViewBuilder.Cell(index);

        public void ReleaseInteractions()
        {
            if (_volume != null) { _volume.StopInteraction(); }
            _interaction?.ReleaseInteractions();
        }

        private void LateUpdate()
        {
            if (_view == null) { return; }
            Camera viewer = Camera.main;
            foreach (KeyValuePair<Text, Text> pair in _view.SideLabels)
            {
                if (pair.Value.text != pair.Key.text) { pair.Value.text = pair.Key.text; }
                pair.Value.color = pair.Key.color;
                if (viewer != null)
                {
                    Vector3 towardViewer = (viewer.transform.position - pair.Value.transform.position).normalized;
                    pair.Value.enabled = Vector3.Dot(pair.Value.transform.forward, towardViewer) < -0.25f;
                }
            }
        }

        private void OnDisable()
        {
            if (_view != null && _view.CalculationPath != null) { _view.CalculationPath.enabled = false; }
            ReleaseInteractions();
        }

        private void OnDestroy()
        {
            _interaction?.Dispose();
            _builder?.Dispose();
        }
    }
}
