using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UI
{
    // Physical actions for the RGB volume. Lesson arithmetic remains in ConvolutionExercise.
    public sealed class SpatialVolumeInteractionController : MonoBehaviour
    {
        private Transform[] _layers;
        private Transform[] _weights;
        private Transform _patch;
        private Transform _filter;
        private XRGrabInteractable _patchGrab;
        private XRGrabInteractable _filterGrab;
        private readonly XRGrabInteractable[] _layerGrabs = new XRGrabInteractable[3];
        private readonly Vector3[] _layerHomes = new Vector3[3];
        private readonly LineRenderer[] _pairs = new LineRenderer[3];
        private LineRenderer _depthLine;
        private Text _instruction;
        private SpatialConvolutionController _owner;
        private XRRayInteractor _ray;
        private Vector3 _patchSource;
        private bool _extracted;
        private bool _aligned;
        private bool _closing;
        private bool _exploring;
        private int _expected = -1;
        private bool _pulled;
        private enum PendingAction { None, Extract, Align, Pull }
        private PendingAction _pending;
        public bool HasPendingAction => _pending != PendingAction.None;
        private static readonly Vector3 WeightOffset = new Vector3(0.08f, 0.055f, -0.3f);
        private static readonly Vector3 FilterTarget = new Vector3(-0.38f, 0, 0.15f);

        public void Initialize(SpatialConvolutionController owner, Transform[] layers, Transform[] weights,
            Transform patch, Text instruction, Material material)
        {
            _owner = owner;
            _layers = layers;
            _weights = weights;
            _patch = patch;
            _instruction = instruction;
            _patchGrab = Grab(_patch, new Vector3(0.58f, 0.5f, 0.06f), Vector3.zero);
            _patchGrab.selectExited.AddListener(_ => Released(_patchGrab));
            _filter = new GameObject("One 3x3x3 filter").transform;
            _filter.SetParent(transform, false);
            _filterGrab = Grab(_filter, new Vector3(0.6f, 0.52f, 1.5f), new Vector3(0.08f, 0.055f, 0.55f));
            _filterGrab.selectExited.AddListener(_ => Released(_filterGrab));
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

        public IEnumerator FitWholeFilter()
        {
            StopInteraction();
            _aligned = false;
            _patch.gameObject.SetActive(false);
            _filter.gameObject.SetActive(true);
            _filter.localPosition = new Vector3(-1.65f, 0, 0.15f);
            _filter.localRotation = Quaternion.identity;
            for (int c = 0; c < 3; c++)
            {
                _layers[c].gameObject.SetActive(true);
                _layers[c].localPosition = FilterTarget + Vector3.forward * (c * 0.65f);
                _weights[c].gameObject.SetActive(true);
                _weights[c].SetParent(_filter, false);
                _weights[c].localPosition = WeightOffset + Vector3.forward * (c * 0.65f);
            }
            EnableGrab(_filterGrab);
            _pending = PendingAction.Align;
            _exploring = true;
            _instruction.text = "왼쪽의 3층 가중치 묶음 전체가 필터 하나예요.\n묶음을 잡아 오른쪽 RGB 입력에 맞춰 놓으세요. 정면과 옆면에서 위치를 확인하세요.";
            yield return new WaitUntil(() => _aligned);
            _pending = PendingAction.None;
            StopGrab(_filterGrab);
            for (int c = 0; c < 3; c++) { _weights[c].SetParent(transform, true); }
            _instruction.text = "R에는 R 가중치, G에는 G, B에는 B가 대응해요.\n3 × 3 × 3 = 27쌍의 곱을 더해 출력 하나를 만듭니다.";
        }

        public IEnumerator PullLayer(int channel)
        {
            StopInteraction();
            _expected = channel;
            _pulled = false;
            _pending = PendingAction.Pull;
            _exploring = true;
            for (int c = 0; c < 3; c++)
            {
                _layers[c].gameObject.SetActive(true);
                _layerHomes[c] = FilterTarget + new Vector3(c * 0.22f, c * 0.05f, c * 0.65f);
                _layers[c].localPosition = _layerHomes[c];
                _weights[c].gameObject.SetActive(true);
                _weights[c].localPosition = _layers[c].localPosition + WeightOffset;
                if (c == channel) { EnableGrab(_layerGrabs[c]); }
            }
            _instruction.text = "RGB"[channel] + " 층을 직접 잡아 앞으로 또는 옆으로 꺼내 놓으세요.\n같은 색의 가중치도 함께 따라옵니다. 값은 바뀌지 않아요.";
            yield return new WaitUntil(() => _pulled);
            StopInteraction();
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
            else if (target == _filterGrab)
            {
                // Compare visible RGB slices, not the offset parent pivot. Accept
                // overlap or a small gap in front so both sets of values remain readable.
                _aligned = true;
                for (int c = 0; c < 3; c++)
                {
                    Vector3 offset = _layers[c].InverseTransformPoint(_weights[c].position);
                    _aligned &= Mathf.Abs(offset.x) <= 0.22f && Mathf.Abs(offset.y) <= 0.20f
                        && Mathf.Abs(offset.z) <= 0.48f
                        && Quaternion.Angle(_weights[c].rotation, _layers[c].rotation) <= 30f;
                }
                _filter.localPosition = _aligned ? FilterTarget : new Vector3(-1.65f, 0, 0.15f);
                _filter.localRotation = Quaternion.identity;
                if (!_aligned) { _instruction.text = "세 색의 순서와 가로·세로 위치를 맞춰 다시 놓으세요."; }
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
            if (_expected >= 0)
            {
                for (int c = 0; c < 3; c++) { _weights[c].position = _layers[c].TransformPoint(WeightOffset); }
            }
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
            _owner.HighlightSpatialPair(index);
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
            StopGrab(_filterGrab);
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
            if (action == PendingAction.Align)
            {
                StopGrab(_filterGrab);
                _filter.localPosition = FilterTarget;
                _filter.localRotation = Quaternion.identity;
                _aligned = true;
            }
            else if (action == PendingAction.Extract)
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
            DestroyDetached(_filterGrab);
            foreach (XRGrabInteractable grab in _layerGrabs) { DestroyDetached(grab); }
        }

        private void DestroyDetached(XRGrabInteractable grab)
        {
            if (grab != null && !grab.transform.IsChildOf(transform)) { Destroy(grab.gameObject); }
        }
    }
}
