using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UI
{
    // Geometry is in metres; the parent is the existing world-space UI canvas.
    public sealed class SpatialConvolutionController : MonoBehaviour
    {
        private readonly Transform[] _layers = new Transform[3];
        private readonly Text[,] _values = new Text[3, 9];
        private readonly Text[,] _volumeWeightLabels = new Text[3, 9];
        private readonly Text[] _weights = new Text[9];
        private readonly Text[] _products = new Text[9];
        private readonly Transform[] _productTiles = new Transform[9];
        private readonly XRGrabInteractable[] _answers = new XRGrabInteractable[3];
        private readonly Text[] _answerLabels = new Text[3];
        private readonly Vector3[] _answerHomes = new Vector3[3];
        private Transform _kernel;
        private readonly Transform[] _parkedKernels = new Transform[3];
        private Transform _colourPatch;
        private readonly MeshRenderer[] _colourTiles = new MeshRenderer[9];
        private Transform _result;
        private Transform _slot;
        private Transform _flying;
        private Text _flyingLabel;
        private Transform _weightCopy;
        private Text _weightCopyLabel;
        private Text _sumLabel;
        private Text _kernelTitle;
        private XRGrabInteractable _kernelGrab;
        private Material _material;
        private Font _font;
        private Action<int> _choose;
        private int _channel;
        private bool _aligned;
        private bool _closing;
        private int[,] _inputs;
        private int[,] _fixedWeights;
        private static readonly string[] Names = { "R", "G", "B" };
        public bool IsAligned => _aligned;
        private SpatialVolumeInteractionController _volume;
        private Text _spatialGuide;
        public SpatialVolumeInteractionController Volume => _volume;

        public void HighlightSpatialPair(int index)
        {
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    _values[c, i].fontStyle = i == index ? FontStyle.Bold : FontStyle.Normal;
                    _volumeWeightLabels[c, i].fontStyle = i == index ? FontStyle.Bold : FontStyle.Normal;
                    _volumeWeightLabels[c, i].color = i == index ? new Color(1, 0.85f, 0.3f) : Color.white;
                    _values[c, i].transform.parent.localScale = Vector3.one * (i == index ? 0.00125f : 0.001f);
                }
            }
        }

        public IEnumerator PrepareVolume()
        {
            _kernel.gameObject.SetActive(false);
            _result.gameObject.SetActive(false);
            _slot.gameObject.SetActive(false);
            _spatialGuide.gameObject.SetActive(true);
            yield return _volume.FitWholeFilter();
        }

        public IEnumerator PullColour(int channel)
        {
            _kernel.gameObject.SetActive(false);
            _result.gameObject.SetActive(false);
            _slot.gameObject.SetActive(false);
            _spatialGuide.gameObject.SetActive(true);
            yield return _volume.PullLayer(channel);
            _spatialGuide.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        public bool EditorCanDrag(XRGrabInteractable target)
        {
            return _volume.CanDrag(target) || (target.isActiveAndEnabled && (target == _kernelGrab || Array.IndexOf(_answers, target) >= 0));
        }

        public void EditorRelease(XRGrabInteractable target)
        {
            if (_volume.CanDrag(target)) { _volume.Released(target); }
            else if (target == _kernelGrab) { AlignReleasedKernel(); }
            else
            {
                int index = Array.IndexOf(_answers, target);
                if (index >= 0) { ReleaseAnswer(index); }
            }
        }
#endif

        public void Initialize(Font font)
        {
            _font = font;
            transform.localScale = Vector3.one * 500f;
            _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            for (int c = 0; c < 3; c++)
            {
                _layers[c] = Root(Names[c] + " input slice");
                Caption(_layers[c], Names[c] + " 색 성분", new Vector3(0, 0.32f, 0), 0.65f);
                Text leftSide = Caption(_layers[c], Names[c], new Vector3(-0.31f, 0.28f, 0), 0.16f);
                leftSide.transform.parent.localRotation = Quaternion.Euler(0, 90, 0);
                Text rightSide = Caption(_layers[c], Names[c], new Vector3(0.31f, 0.28f, 0), 0.16f);
                rightSide.transform.parent.localRotation = Quaternion.Euler(0, -90, 0);
                for (int i = 0; i < 9; i++)
                {
                    Transform tile = Tile(_layers[c], Cell(i), new Vector3(0.17f, 0.15f, 0.025f), Color.gray);
                    _values[c, i] = Caption(tile, "", new Vector3(0, 0, -0.016f), 0.17f);
                }
                _parkedKernels[c] = Root(Names[c] + " fixed filter slice");
                Caption(_parkedKernels[c], Names[c] + " 가중치", new Vector3(0, 0.3f, 0), 0.6f);
                for (int i = 0; i < 9; i++)
                {
                    Transform tile = Tile(_parkedKernels[c], Cell(i), new Vector3(0.09f, 0.065f, 0.02f), new Color(0.3f, 0.31f, 0.34f));
                    _volumeWeightLabels[c, i] = Caption(tile, "", new Vector3(0, 0, -0.016f), 0.12f);
                }
            }
            _colourPatch = Root("Reconstructed colour patch");
            for (int i = 0; i < 9; i++)
            {
                Transform tile = Tile(_colourPatch, Cell(i), new Vector3(0.17f, 0.15f, 0.025f), Color.white);
                _colourTiles[i] = tile.Find("Tile mesh").GetComponent<MeshRenderer>();
            }
            _colourPatch.gameObject.SetActive(false);
            _kernel = Root("Fixed weight slice");
            _kernelTitle = Caption(_kernel, "고정 가중치", new Vector3(0, 0.32f, 0), 0.7f);
            for (int i = 0; i < 9; i++)
            {
                Transform tile = Tile(_kernel, Cell(i), new Vector3(0.09f, 0.065f, 0.025f), new Color(0.3f, 0.31f, 0.34f));
                _weights[i] = Caption(tile, "", new Vector3(0, 0, -0.016f), 0.12f);
            }
            _kernelGrab = MakeGrab(_kernel, new Vector3(0.6f, 0.52f, 0.04f));
            _kernelGrab.selectEntered.AddListener(_ => _aligned = false);
            _kernelGrab.selectExited.AddListener(_ => AlignReleasedKernel());
            _result = Root("Products at matching positions");
            _result.localPosition = new Vector3(0.75f, 0, -0.05f);
            Caption(_result, "같은 위치의 곱", new Vector3(0, 0.32f, 0), 0.7f);
            for (int i = 0; i < 9; i++)
            {
                _productTiles[i] = Tile(_result, Cell(i), new Vector3(0.17f, 0.15f, 0.025f), new Color(0.16f, 0.18f, 0.22f));
                _products[i] = Caption(_productTiles[i], "?", new Vector3(0, 0, -0.016f), 0.17f);
            }
            _slot = Root("Answer destination");
            _slot.localPosition = new Vector3(0.7f, -0.4f, -0.12f);
            Tile(_slot, Vector3.zero, new Vector3(0.36f, 0.14f, 0.035f), new Color(0.26f, 0.29f, 0.34f));
            _sumLabel = Caption(_slot, "합: ?", new Vector3(0, 0, -0.025f), 0.34f);
            _flying = Root("Animated calculation copy");
            _flyingLabel = Caption(_flying, "", Vector3.zero, 0.9f);
            _flying.gameObject.SetActive(false);
            _weightCopy = Root("Animated weight copy");
            _weightCopyLabel = Caption(_weightCopy, "", Vector3.zero, 0.3f);
            _weightCopy.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                int option = i;
                Transform answer = Root("Numeric answer " + i);
                _answerHomes[i] = new Vector3(-0.6f + i * 0.38f, -0.58f, -0.15f);
                Tile(answer, Vector3.zero, new Vector3(0.3f, 0.13f, 0.04f), new Color(0.3f, 0.34f, 0.4f));
                _answerLabels[i] = Caption(answer, "", new Vector3(0, 0, -0.025f), 0.29f);
                _answers[i] = MakeGrab(answer, new Vector3(0.32f, 0.15f, 0.06f));
                _answers[i].selectExited.AddListener(_ => ReleaseAnswer(option));
                answer.gameObject.SetActive(false);
            }
            _spatialGuide = Caption(transform, "", new Vector3(0, -0.55f, -0.15f), 1.9f);
            _spatialGuide.rectTransform.sizeDelta = new Vector2(1900, 180);
            _volume = gameObject.AddComponent<SpatialVolumeInteractionController>();
            _volume.Initialize(this, _layers, _parkedKernels, _colourPatch, _spatialGuide, _material);
        }

        public void SetData(int[,] inputs, int[,] weights)
        {
            _inputs = inputs;
            _fixedWeights = weights;
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    _values[c, i].text = inputs[c, i].ToString();
                    float strength = inputs[c, i] / 255f;
                    Color color = c == 0 ? new Color(strength, 0, 0) : c == 1 ? new Color(0, strength, 0) : new Color(0, 0, strength);
                    Tint(_values[c, i].transform.parent.parent.Find("Tile mesh").GetComponent<MeshRenderer>(), color);
                    _values[c, i].color = c == 1 && strength > 0.65f ? Color.black : Color.white;
                }
                for (int i = 0; i < 9; i++) { _volumeWeightLabels[c, i].text = weights[c, i].ToString(); }
            }
            for (int i = 0; i < 9; i++) { Tint(_colourTiles[i], new Color(inputs[0, i] / 255f, inputs[1, i] / 255f, inputs[2, i] / 255f)); }
        }

        public IEnumerator Observe(Vector3 source, bool extract = true)
        {
            HideAnswers();
            _kernel.gameObject.SetActive(false);
            _result.gameObject.SetActive(false);
            _slot.gameObject.SetActive(false);
            _flying.gameObject.SetActive(false);
            _weightCopy.gameObject.SetActive(false);
            foreach (Transform slice in _parkedKernels) { slice.gameObject.SetActive(false); }
            _spatialGuide.gameObject.SetActive(extract);
            if (extract) { yield return _volume.Extract(source); }
            for (int c = 0; c < 3; c++) { _layers[c].localPosition = Vector3.zero; }
            yield return SpreadColours(false);
        }

        public IEnumerator SpreadColours(bool combine)
        {
            _volume.StopInteraction();
            Vector3[] starts = new Vector3[3];
            _colourPatch.gameObject.SetActive(false);
            for (int c = 0; c < 3; c++)
            {
                starts[c] = _layers[c].localPosition;
                _layers[c].gameObject.SetActive(true);
            }
            for (float time = 0; time < 1.2f; time += Time.unscaledDeltaTime)
            {
                for (int c = 0; c < 3; c++)
                {
                    Vector3 target = combine ? new Vector3(0, 0, 0.2f + c * 0.035f) : new Vector3((c - 1) * 0.65f, c * 0.06f, c * 0.3f);
                    _layers[c].localPosition = Vector3.Lerp(starts[c], target, Mathf.SmoothStep(0, 1, time / 1.2f));
                }
                yield return null;
            }
            for (int c = 0; c < 3; c++)
            {
                _layers[c].localPosition = combine ? new Vector3(0, 0, 0.2f + c * 0.035f) : new Vector3((c - 1) * 0.65f, c * 0.06f, c * 0.3f);
            }
            if (combine)
            {
                foreach (Transform layer in _layers) { layer.gameObject.SetActive(false); }
                _colourPatch.localPosition = new Vector3(0, 0, 0.2f);
                _colourPatch.gameObject.SetActive(true);
            }
            else { _volume.Explore(); }
        }

        public IEnumerator Focus(int channel, Vector3 source)
        {
            _volume.StopInteraction();
            _spatialGuide.gameObject.SetActive(false);
            _channel = channel;
            _colourPatch.gameObject.SetActive(false);
            _flying.gameObject.SetActive(false);
            _weightCopy.gameObject.SetActive(false);
            _aligned = false;
            _kernelGrab.enabled = false;
            _kernel.gameObject.SetActive(true);
            _result.gameObject.SetActive(true);
            _slot.gameObject.SetActive(true);
            _sumLabel.text = "합: ?";
            _kernelTitle.text = Names[channel] + " 고정 가중치 · Grip";
            for (int i = 0; i < 9; i++)
            {
                _weights[i].text = _fixedWeights[channel, i].ToString();
                _products[i].text = "?";
            }
            Vector3[] starts = new Vector3[3];
            for (int c = 0; c < 3; c++)
            {
                starts[c] = _layers[c].localPosition;
            }
            for (float time = 0; time < 1; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time);
                for (int c = 0; c < 3; c++)
                {
                    _layers[c].gameObject.SetActive(true);
                    _layers[c].localPosition = Vector3.Lerp(starts[c], LayerPosition(c, channel), t);
                    _parkedKernels[c].gameObject.SetActive(c != channel);
                    _parkedKernels[c].localPosition = _layers[c].localPosition + new Vector3(0.08f, 0.055f, -0.14f);
                }
                _kernel.localPosition = Vector3.Lerp(new Vector3(-0.38f, 0, 0.6f), KernelHome, t);
                yield return null;
            }
            for (int c = 0; c < 3; c++)
            {
                _layers[c].localPosition = LayerPosition(c, channel);
                _parkedKernels[c].localPosition = _layers[c].localPosition + new Vector3(0.08f, 0.055f, -0.14f);
            }
            ResetGrab(_kernelGrab, KernelHome);
            _kernelGrab.enabled = true;
        }

        public void AlignKernel()
        {
            if (_kernelGrab.isSelected) { return; }
            ResetGrab(_kernelGrab, KernelTarget);
            _aligned = true;
        }

        private void AlignReleasedKernel()
        {
            if (_closing) { return; }
            // The target is in front of the input. Both numbers remain visible in depth.
            if (Vector3.Distance(_kernel.position, transform.TransformPoint(KernelTarget)) < 0.3f)
            {
                AlignKernel();
            }
            else { ResetGrab(_kernelGrab, KernelHome); }
        }

        public IEnumerator MultiplyAndAccumulate(int channel, int index, int accumulated)
        {
            _closing = true;
            CancelGrab(_kernelGrab);
            _closing = false;
            AlignKernel();
            _kernelGrab.enabled = false;
            int product = _inputs[channel, index] * _fixedWeights[channel, index];
            _flying.gameObject.SetActive(true);
            Vector3 start = _layers[channel].localPosition + Cell(index);
            Vector3 meeting = new Vector3(0.1f, 0.42f, -0.32f);
            _flyingLabel.text = _inputs[channel, index].ToString();
            _weightCopyLabel.text = "× (" + _fixedWeights[channel, index] + ")";
            _weightCopy.gameObject.SetActive(true);
            Vector3 weightStart = _kernel.localPosition + Cell(index);
            HighlightRow(index / 3);
            for (float time = 0; time < 0.7f; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / 0.7f);
                _flying.localPosition = Vector3.Lerp(start, meeting + Vector3.left * 0.13f, t);
                _weightCopy.localPosition = Vector3.Lerp(weightStart, meeting + Vector3.right * 0.13f, t);
                yield return null;
            }
            _weightCopy.gameObject.SetActive(false);
            _flying.localPosition = meeting;
            _flyingLabel.text = $"{_inputs[channel, index]} × ({_fixedWeights[channel, index]}) = {product}";
            yield return new WaitForSecondsRealtime(0.4f);
            Vector3 result = _result.localPosition + Cell(index);
            yield return Fly(meeting, result, 0.45f);
            _products[index].text = product.ToString();
            _flyingLabel.text = "(" + product + ")";
            yield return Fly(result, _slot.localPosition, 0.4f);
            _sumLabel.text = "합: " + accumulated;
            _flying.gameObject.SetActive(false);
        }

        public void ShowProducts(int channel, int count)
        {
            for (int i = 0; i < count; i++) { _products[i].text = (_inputs[channel, i] * _fixedWeights[channel, i]).ToString(); }
        }

        public void ShowAnswer(int value)
        {
            _sumLabel.text = value.ToString();
        }

        public void HighlightRow(int row)
        {
            for (int i = 0; i < 9; i++)
            {
                bool selected = i / 3 == row;
                _weights[i].color = selected ? new Color(1, 0.83f, 0.3f) : Color.white;
                _products[i].color = selected ? new Color(1, 0.83f, 0.3f) : Color.white;
                _values[_channel, i].fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        public void ShowAnswers(int[] options, Action<int> choose)
        {
            _choose = choose;
            _sumLabel.text = "여기에 답 놓기";
            for (int i = 0; i < 3; i++)
            {
                _answerLabels[i].text = options[i].ToString();
                _answers[i].gameObject.SetActive(true);
                ResetGrab(_answers[i], _answerHomes[i]);
            }
        }

        public void HideAnswers()
        {
            _choose = null;
            _closing = true;
            foreach (XRGrabInteractable answer in _answers)
            {
                CancelGrab(answer);
                answer.gameObject.SetActive(false);
            }
            _closing = false;
        }

        private void ReleaseAnswer(int option)
        {
            if (_closing || _choose == null) { return; }
            XRGrabInteractable answer = _answers[option];
            bool inside = Vector3.Distance(answer.transform.position, _slot.position) < 0.24f;
            ResetGrab(answer, _answerHomes[option]);
            if (inside) { _choose.Invoke(option); }
        }

        public IEnumerator CombineSums(int[] sums)
        {
            _closing = true;
            CancelGrab(_kernelGrab);
            _closing = false;
            _kernelGrab.enabled = false;
            _kernel.gameObject.SetActive(false);
            _result.gameObject.SetActive(false);
            Vector3[] starts = new Vector3[3];
            for (int c = 0; c < 3; c++)
            {
                starts[c] = _layers[c].localPosition;
                _parkedKernels[c].gameObject.SetActive(true);
            }
            for (float time = 0; time < 1.2f; time += Time.unscaledDeltaTime)
            {
                for (int c = 0; c < 3; c++)
                {
                    Vector3 target = new Vector3(-0.38f, 0, 0.12f + c * 0.3f);
                    _layers[c].localPosition = Vector3.Lerp(starts[c], target, Mathf.SmoothStep(0, 1, time / 1.2f));
                    _parkedKernels[c].localPosition = _layers[c].localPosition + new Vector3(0.08f, 0.055f, -0.14f);
                }
                yield return null;
            }
            _flying.gameObject.SetActive(true);
            int total = 0;
            for (int c = 0; c < 3; c++)
            {
                _flyingLabel.text = Names[c] + " 합 (" + sums[c] + ")";
                yield return Fly(_layers[c].localPosition, _slot.localPosition, 0.85f);
                total += sums[c];
                _sumLabel.text = total.ToString();
            }
            _flyingLabel.text = "편향 0은 마지막에 한 번 더해요";
            _flying.localPosition = new Vector3(0, 0.4f, -0.1f);
        }

        private IEnumerator Fly(Vector3 start, Vector3 end, float duration)
        {
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                _flying.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, time / duration));
                yield return null;
            }
            _flying.localPosition = end;
        }

        private static Vector3 KernelHome => new Vector3(-1.05f, 0, -0.22f);
        private static Vector3 KernelTarget => new Vector3(-0.3f, 0.055f, -0.14f);
        private static Vector3 LayerPosition(int c, int focus) => c == focus ? new Vector3(-0.38f, 0, 0.1f) : new Vector3(-0.38f + (c + 1) * 0.22f, 0.1f + c * 0.08f, 0.65f + c * 0.25f);
        private static Vector3 Cell(int i) => new Vector3((i % 3 - 1) * 0.19f, (1 - i / 3) * 0.17f, 0);

        private Transform Root(string name)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(transform, false);
            return root;
        }

        private Transform Tile(Transform parent, Vector3 position, Vector3 size, Color color)
        {
            GameObject tile = new GameObject("Number tile");
            tile.transform.SetParent(parent, false);
            tile.transform.localPosition = position;
            // Keep label and collider coordinates independent from mesh dimensions.
            Transform mesh = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            mesh.name = "Tile mesh";
            mesh.SetParent(tile.transform, false);
            MeshRenderer renderer = mesh.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            mesh.localScale = size;
            Tint(renderer, color);
            Collider collider = mesh.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            return tile.transform;
        }

        private static void Tint(MeshRenderer renderer, Color color)
        {
            if (renderer == null) { return; }
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(properties);
        }

        private Text Caption(Transform parent, string value, Vector3 position, float width)
        {
            GameObject canvasObject = new GameObject("Label", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = position;
            canvasObject.transform.localScale = Vector3.one * 0.001f;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            Text label = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(canvasObject.transform, false);
            label.rectTransform.sizeDelta = new Vector2(width * 1000, 100);
            label.font = _font;
            label.fontSize = 27;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = value;
            return label;
        }

        private static XRGrabInteractable MakeGrab(Transform target, Vector3 size)
        {
            target.gameObject.AddComponent<BoxCollider>().size = size;
            Rigidbody body = target.gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            XRGrabInteractable grab = target.gameObject.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.trackRotation = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            return grab;
        }

        private void ResetGrab(XRGrabInteractable grab, Vector3 position)
        {
            grab.transform.SetParent(transform, false);
            grab.transform.localPosition = position;
            grab.transform.localRotation = Quaternion.identity;
            Rigidbody body = grab.GetComponent<Rigidbody>();
            body.position = grab.transform.position;
            body.rotation = grab.transform.rotation;
        }

        private void CancelGrab(XRGrabInteractable grab)
        {
            if (grab == null) { return; }
            if (grab.isSelected && grab.interactionManager != null) { grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab); }
            grab.transform.SetParent(transform, true);
        }

        private void OnDisable()
        {
            _closing = true;
            CancelGrab(_kernelGrab);
            foreach (XRGrabInteractable answer in _answers) { CancelGrab(answer); }
            _choose = null;
            _closing = false;
        }

        private void OnDestroy()
        {
            if (_material != null) { Destroy(_material); }
        }
    }
}
