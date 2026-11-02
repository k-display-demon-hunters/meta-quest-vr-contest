using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CNNConvolution
{
    // Physical actions for the RGB volume. Arithmetic belongs to ConvolutionCalculation.
    public sealed class RgbLayerInteractionController : MonoBehaviour
    {
        private Transform[] _layers;
        private Transform[] _weights;
        private Transform _patch;
        private XRGrabInteractable _patchGrab;
        private readonly XRGrabInteractable[] _layerGrabs = new XRGrabInteractable[3];
        private readonly Vector3[] _layerHomes = new Vector3[3];
        private readonly LineRenderer[] _pairs = new LineRenderer[3];
        private LineRenderer _depthLine;
        private Text _instruction;
        public event System.Action<int> PairHighlighted;
        private XRRayInteractor _ray;
        private Vector3 _patchSource;
        private bool _extracted;
        private bool _closing;
        private bool _exploring;
        private int _expected = -1;
        private bool _pulled;
        private enum PendingAction { None, Extract, Pull }
        private PendingAction _pending;
        public bool HasPendingAction => _pending != PendingAction.None;
        private static readonly Vector3 WeightOffset = new Vector3(0.08f, 0.055f, -0.3f);
        private static readonly Vector3 FilterTarget = new Vector3(-0.38f, 0, 0.15f);

        public void Initialize(Transform[] layers, Transform[] weights,
            Transform patch, Text instruction, Material material)
        {
            _layers = layers;
            _weights = weights;
            _patch = patch;
            _instruction = instruction;
            _patchGrab = Grab(_patch, new Vector3(0.58f, 0.5f, 0.06f), Vector3.zero);
            _patchGrab.selectExited.AddListener(_ => Released(_patchGrab));
            for (int c = 0; c < 3; c++)
            {
                int colour = c;
                // The entire slice is grabbed directly; no separate handle is introduced.
                _layerGrabs[c] = Grab(layers[c], new Vector3(0.58f, 0.5f, 0.14f), Vector3.zero);
                _layerGrabs[c].selectExited.AddListener(_ => Released(_layerGrabs[colour]));
                _pairs[c] = Line("Input to weight " + c, material, 2);
            }
            _depthLine = Line("Same pixel through RGB depth", material, 3);
            foreach (XRRayInteractor ray in FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None))
            {
                if (ray.handedness == InteractorHandedness.Right) { _ray = ray; break; }
            }
            StopInteraction();
        }

        public IEnumerator Extract(Vector3 source)
        {
            StopInteraction();
            _extracted = false;
            foreach (Transform layer in _layers) { layer.gameObject.SetActive(false); }
            foreach (Transform weight in _weights) { weight.gameObject.SetActive(false); }
            _patch.gameObject.SetActive(true);
            _patch.localScale = Vector3.one * 0.5f;
            _patch.position = source - transform.forward * 0.06f;
            _patchSource = _patch.position;
            EnableGrab(_patchGrab);
            _pending = PendingAction.Extract;
            _instruction.text = "선택한 작은 이미지 영역을 직접 잡아 밖으로 꺼낸 뒤 놓으세요.\nGrip으로 잡기 · 마우스 미리보기에서는 왼쪽 드래그";
            yield return new WaitUntil(() => _extracted);
            _pending = PendingAction.None;
            StopGrab(_patchGrab);
            Vector3 start = _patch.localPosition;
            for (float time = 0; time < 0.7f; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / 0.7f);
                _patch.localPosition = Vector3.Lerp(start, Vector3.zero, t);
                _patch.localScale = Vector3.one * Mathf.Lerp(0.5f, 1, t);
                yield return null;
            }
            _patch.localPosition = Vector3.zero;
            _patch.localScale = Vector3.one;
            _instruction.text = "RGB 층을 직접 잡아 벌려 보세요. 옆으로 이동하면 세 층의 깊이가 보여요.\n이 깊이는 사진의 두께가 아니라 색 정보를 펼친 표현이에요.";
        }

        public void ExplorePairs() { _exploring = true; _expected = -1; }

        public void Explore()
        {
            _exploring = true;
            _expected = -1;
            for (int c = 0; c < 3; c++)
            {
                _layerHomes[c] = _layers[c].localPosition;
                if (_layers[c].gameObject.activeSelf) { EnableGrab(_layerGrabs[c]); }
            }
        }


        public IEnumerator PullLayer(int channel)
        {
            StopInteraction();
            _expected = channel;
            _pulled = false;
            _instruction.text = "RGB"[channel] + " 층을 준비하고 있어요.";
            yield return ArrangeForPull();
            _pending = PendingAction.Pull;
            _exploring = true;
            EnableGrab(_layerGrabs[channel]);
            _instruction.text = "RGB"[channel] + " 층을 Grip으로 잡아 앞으로 꺼내세요.";
            yield return new WaitUntil(() => _pulled);
            StopInteraction();
        }

        private IEnumerator ArrangeForPull()
        {
            const float duration = 0.55f;
            Vector3[] layerStarts = new Vector3[3];
            Vector3[] weightStarts = new Vector3[3];
            Vector3[] weightHomes = new Vector3[3];
            for (int c = 0; c < 3; c++)
            {
                _layerHomes[c] = FilterTarget + new Vector3(c * 0.24f, c * 0.1f, c * 0.14f);
                weightHomes[c] = new Vector3(0.65f + c * 0.14f, c * 0.1f, 0.15f + c * 0.14f);
                // Hidden slices re-enter near the workbench, not from their offstage positions.
                layerStarts[c] = _layers[c].gameObject.activeSelf
                    ? _layers[c].localPosition : _layerHomes[c] + Vector3.up * 0.12f;
                weightStarts[c] = _weights[c].gameObject.activeSelf
                    ? _weights[c].localPosition : weightHomes[c] + Vector3.up * 0.12f;
                _layers[c].localPosition = layerStarts[c];
                _weights[c].localPosition = weightStarts[c];
                _layers[c].gameObject.SetActive(true);
                _weights[c].gameObject.SetActive(true);
            }
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / duration);
                for (int c = 0; c < 3; c++)
                {
                    _layers[c].localPosition = Vector3.Lerp(layerStarts[c], _layerHomes[c], t);
                    _weights[c].localPosition = Vector3.Lerp(weightStarts[c], weightHomes[c], t);
                }
                yield return null;
            }
            for (int c = 0; c < 3; c++)
            {
                _layers[c].localPosition = _layerHomes[c];
                _weights[c].localPosition = weightHomes[c];
            }
        }

        public void Released(XRGrabInteractable target)
        {
            if (_closing) { return; }
            target.transform.SetParent(transform, true);
            if (target == _patchGrab)
            {
                _extracted = Vector3.Distance(_patch.position, _patchSource) >= 0.15f;
                if (!_extracted) { _patch.position = _patchSource; }
            }
            else
            {
                int c = System.Array.IndexOf(_layerGrabs, target);
                if (c < 0) { return; }
                _layers[c].localRotation = Quaternion.identity;
                Vector3 displacement = _layers[c].localPosition - _layerHomes[c];
                _layers[c].localPosition = _layerHomes[c] + Vector3.ClampMagnitude(displacement, 1.2f);
                if (c == _expected) { _pulled = displacement.magnitude >= 0.16f; }
            }
            Rigidbody body = target.GetComponent<Rigidbody>();
            body.position = target.transform.position;
            body.rotation = target.transform.rotation;
        }

        private void LateUpdate()
        {
            if (!_exploring) { return; }
            if (_ray != null && _ray.isActiveAndEnabled)
            {
                Highlight(new Ray(_ray.transform.position, _ray.transform.forward));
            }
        }

        public void Highlight(Ray ray)
        {
            if (!_exploring) { return; }
            int index = -1;
            float nearest = float.PositiveInfinity;
            for (int c = 0; c < 3; c++)
            {
                Plane plane = new Plane(_layers[c].forward, _layers[c].position);
                if (!plane.Raycast(ray, out float distance) || distance > nearest) { continue; }
                Vector3 point = _layers[c].InverseTransformPoint(ray.GetPoint(distance));
                if (Mathf.Abs(point.x) > 0.28f || Mathf.Abs(point.y) > 0.25f) { continue; }
                int col = Mathf.Clamp(Mathf.FloorToInt((point.x + 0.285f) / 0.19f), 0, 2);
                int row = Mathf.Clamp(Mathf.FloorToInt((0.255f - point.y) / 0.17f), 0, 2);
                index = row * 3 + col;
                nearest = distance;
            }
            PairHighlighted?.Invoke(index);
            _depthLine.enabled = index >= 0;
            for (int c = 0; c < 3; c++)
            {
                _pairs[c].enabled = index >= 0 && _weights[c].gameObject.activeInHierarchy;
                if (index < 0) { continue; }
                Vector3 cell = new Vector3((index % 3 - 1) * 0.19f, (1 - index / 3) * 0.17f, -0.02f);
                Vector3 input = _layers[c].TransformPoint(cell);
                _depthLine.SetPosition(c, input);
                _pairs[c].SetPosition(0, input);
                _pairs[c].SetPosition(1, _weights[c].TransformPoint(cell));
            }
        }

        public void StopInteraction()
        {
            _pending = PendingAction.None;
            _closing = true;
            _exploring = false;
            _expected = -1;
            StopGrab(_patchGrab);
            foreach (XRGrabInteractable grab in _layerGrabs) { StopGrab(grab); }
            foreach (LineRenderer pair in _pairs) { if (pair != null) { pair.enabled = false; } }
            if (_depthLine != null) { _depthLine.enabled = false; }
            _closing = false;
        }

        public bool CompletePendingAction()
        {
            PendingAction action = _pending;
            if (action == PendingAction.None) { return false; }
            _pending = PendingAction.None;
            _closing = true;
            if (action == PendingAction.Extract)
            {
                StopGrab(_patchGrab);
                _patch.position = _patchSource - transform.forward * 0.2f;
                _extracted = true;
            }
            else if (_expected >= 0)
            {
                StopGrab(_layerGrabs[_expected]);
                _layers[_expected].localPosition = _layerHomes[_expected] + Vector3.left * 0.3f;
                _pulled = true;
            }
            _closing = false;
            return true;
        }

        private void StopGrab(XRGrabInteractable grab)
        {
            if (grab == null) { return; }
            if (grab.isSelected && grab.interactionManager != null)
            {
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            }
            if (grab.transform.parent != transform)
            {
                if (gameObject.activeInHierarchy) { grab.transform.SetParent(transform, true); }
                else { grab.gameObject.SetActive(false); }
            }
            grab.enabled = false;
            foreach (Collider collider in grab.colliders) { collider.enabled = false; }
        }

        private static void EnableGrab(XRGrabInteractable grab)
        {
            foreach (Collider collider in grab.colliders) { collider.enabled = true; }
            grab.enabled = true;
        }

        private static XRGrabInteractable Grab(Transform target, Vector3 size, Vector3 center)
        {
            BoxCollider collider = target.gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = center;
            Rigidbody body = target.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            XRGrabInteractable grab = target.gameObject.AddComponent<XRGrabInteractable>();
            grab.colliders.Clear();
            grab.colliders.Add(collider);
            grab.throwOnDetach = false;
            grab.retainTransformParent = false;
            grab.trackRotation = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            return grab;
        }

        private LineRenderer Line(string label, Material material, int count)
        {
            GameObject lineObject = new GameObject(label);
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = count;
            line.startWidth = line.endWidth = 0.004f;
            line.enabled = false;
            return line;
        }

        private void OnDisable() { StopInteraction(); }

        private void OnDestroy()
        {
            DestroyDetached(_patchGrab);
            foreach (XRGrabInteractable grab in _layerGrabs) { DestroyDetached(grab); }
        }

        private void DestroyDetached(XRGrabInteractable grab)
        {
            if (grab != null && !grab.transform.IsChildOf(transform)) { Destroy(grab.gameObject); }
        }
    }
}
