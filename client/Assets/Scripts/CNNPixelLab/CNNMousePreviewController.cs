#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UI
{
    // Editor-only adapter. It never writes the shared XR rig, scene, or project settings.
    public sealed class CNNMousePreviewController : MonoBehaviour
    {
        public static string PreferenceKey => "CNNPixelLab.MousePreview." + Application.dataPath;
        private readonly Dictionary<GameObject, bool> _hiddenObjects = new Dictionary<GameObject, bool>();
        private readonly List<SpatialVolumeInteractionController> _volumes = new List<SpatialVolumeInteractionController>();
        private readonly List<BaseInputModule> _disabledModules = new List<BaseInputModule>();
        private CNNPixelLabManager _lab;
        private Camera _camera;
        private XRGrabInteractable _drag;
        private SpatialConvolutionController _calculation;
        private Button _pressedButton;
        private Plane _dragPlane;
        private Vector3 _dragOffset;
        private Vector3 _pivot;
        private float _distance = 4.5f;
        private float _yaw;
        private float _pitch;
        private bool _details;
        private bool _ready;
        private const float ToolbarHeight = 90;

        private void Start()
        {
            // The factory flow owns the shared XR rig and its entrance-facing view.
            if (UnityEngine.SceneManagement.SceneManager.GetSceneByName("FactoryEnvironment").isLoaded)
            {
                Destroy(this);
                return;
            }
            _lab = GetComponent<CNNPixelLabManager>();
            Camera original = Camera.main;
            if (original == null || UnityEngine.XR.XRSettings.isDeviceActive)
            {
                Destroy(this);
                return;
            }
            GameObject cameraObject = new GameObject("CNN local mouse preview camera", typeof(Camera));
            cameraObject.hideFlags = HideFlags.DontSave;
            cameraObject.transform.SetParent(transform, false);
            _camera = cameraObject.GetComponent<Camera>();
            _camera.CopyFrom(original);
            _camera.targetTexture = null;
            _camera.fieldOfView = 60;
            _camera.nearClipPlane = 0.03f;
            _camera.enabled = true;
            cameraObject.tag = "MainCamera";
            Hide(original.transform.root.gameObject);
            foreach (XR.XREditorSimulator simulator in FindObjectsByType<XR.XREditorSimulator>(FindObjectsSortMode.None))
            {
                Hide(simulator.gameObject);
            }
            foreach (BaseInputModule module in FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None))
            {
                if (module.enabled)
                {
                    _disabledModules.Add(module);
                    module.enabled = false;
                }
            }
            _lab.GetComponentsInChildren(true, _volumes);
            _ready = true;
            _details = _lab.EditorDetailsVisible;
            ResetView();
        }

        private void Hide(GameObject target)
        {
            if (_hiddenObjects.ContainsKey(target)) { return; }
            _hiddenObjects.Add(target, target.activeSelf);
            target.SetActive(false);
        }

        private void ResetView()
        {
            _pivot = _lab.EditorPreviewFocus;
            _distance = _details ? 3.5f : 2.8f;
            _yaw = _lab.transform.eulerAngles.y;
            _pitch = 0;
            PositionCamera();
        }

        private void Update()
        {
            if (!_ready) { return; }
            if (UnityEngine.XR.XRSettings.isDeviceActive)
            {
                Destroy(this);
                return;
            }
            if (_details != _lab.EditorDetailsVisible)
            {
                ReleaseDrag();
                _details = _lab.EditorDetailsVisible;
                    ResetView();
            }
            Mouse mouse = Mouse.current;
            if (mouse == null) { return; }
            Vector2 point = mouse.position.ReadValue();
            if (_drag != null && (!mouse.leftButton.isPressed || !_drag.isActiveAndEnabled)) { ReleaseDrag(); }
            bool inView = point.x >= 0 && point.x < Screen.width && point.y >= 0 && point.y < Screen.height - ToolbarHeight;
            if (!inView) { return; }

            Vector2 delta = mouse.delta.ReadValue();
            if (_drag == null && mouse.rightButton.isPressed)
            {
                _yaw += delta.x * 0.2f;
                _pitch = Mathf.Clamp(_pitch - delta.y * 0.2f, -75, 75);
            }
            if (_drag == null && mouse.middleButton.isPressed)
            {
                _pivot -= (_camera.transform.right * delta.x + _camera.transform.up * delta.y) * (_distance * 0.0015f);
            }
            float wheel = mouse.scroll.ReadValue().y;
            if (_drag == null) { _distance = Mathf.Clamp(_distance * Mathf.Exp(-wheel * 0.001f), 0.6f, 12); }
            PositionCamera();
            Ray ray = _camera.ScreenPointToRay(point);
            foreach (SpatialVolumeInteractionController volume in _volumes)
            {
                if (volume != null && volume.isActiveAndEnabled) { volume.Highlight(ray); }
            }
            if (mouse.leftButton.wasPressedThisFrame) { Press(point, ray); }
            if (_drag != null)
            {
                if (_lab.EditorCanDragFilter(_drag)) { _lab.EditorDragFilter(ray); }
                else
                {
                    if (wheel != 0)
                    {
                        Vector3 position = _drag.transform.position + _camera.transform.forward * wheel * 0.0008f;
                        _dragPlane = new Plane(_camera.transform.forward, position);
                    }
                    if (_dragPlane.Raycast(ray, out float distance))
                    {
                        _drag.transform.position = ray.GetPoint(distance) + _dragOffset;
                        _drag.GetComponent<Rigidbody>().position = _drag.transform.position;
                    }
                }
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                Button released = FindButton(point, ray, out _);
                if (_pressedButton != null && released == _pressedButton && released.IsInteractable())
                {
                    released.onClick.Invoke();
                }
                _pressedButton = null;
                ReleaseDrag();
            }
        }

        private void Press(Vector2 point, Ray ray)
        {
            _pressedButton = FindButton(point, ray, out float buttonDistance);
            bool hitObject = Physics.Raycast(ray, out RaycastHit hit, 30, ~0, QueryTriggerInteraction.Collide);
            if (_pressedButton != null && (!hitObject || buttonDistance <= hit.distance + 0.02f)) { return; }
            _pressedButton = null;
            if (!hitObject) { return; }
            XRGrabInteractable grab = hit.collider.GetComponentInParent<XRGrabInteractable>();
            if (grab != null && grab.isActiveAndEnabled)
            {
                SpatialConvolutionController calculation = grab.GetComponentInParent<SpatialConvolutionController>();
                if (_lab.EditorCanDragFilter(grab) || (calculation != null && calculation.EditorCanDrag(grab)))
                {
                    _drag = grab;
                    _calculation = calculation;
                    _dragPlane = new Plane(_camera.transform.forward, hit.point);
                    _dragOffset = grab.transform.position - hit.point;
                    return;
                }
            }
            PixelRevealStage reveal = _lab.GetComponent<PixelRevealStage>();
            if (reveal == null) { reveal = _lab.GetComponentInChildren<PixelRevealStage>(); }
            if (reveal != null && hit.collider.GetComponent<XRSimpleInteractable>() != null)
            {
                reveal.RevealPixels();
            }
        }

        private Button FindButton(Vector2 point, Ray ray, out float nearest)
        {
            nearest = float.PositiveInfinity;
            Button selected = null;
            // Only on click/release: no per-frame scene search or synthetic XR input.
            foreach (Button button in _lab.GetComponentsInChildren<Button>())
            {
                if (!button.isActiveAndEnabled || !button.IsInteractable()) { continue; }
                RectTransform rect = button.transform as RectTransform;
                if (rect == null || !RectTransformUtility.RectangleContainsScreenPoint(rect, point, _camera)) { continue; }
                Plane plane = new Plane(rect.forward, rect.position);
                if (plane.Raycast(ray, out float distance) && distance < nearest)
                {
                    nearest = distance;
                    selected = button;
                }
            }
            return selected;
        }

        private void ReleaseDrag()
        {
            if (_drag != null && _drag.gameObject.activeInHierarchy)
            {
                if (_lab.EditorCanDragFilter(_drag)) { _lab.EditorReleaseFilter(); }
                else if (_calculation != null) { _calculation.EditorRelease(_drag); }
            }
            _drag = null;
            _calculation = null;
        }

        private void PositionCamera()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            _camera.transform.SetPositionAndRotation(_pivot - rotation * Vector3.forward * _distance, rotation);
        }

        private void OnGUI()
        {
            if (!_ready) { return; }
            GUI.Box(new Rect(8, 8, Screen.width - 16, 76), "마우스 미리보기 · 이 컴퓨터에서만 적용");
            GUI.Label(new Rect(20, 30, Screen.width - 40, 24), "왼쪽: 클릭 / 물체 드래그   오른쪽 드래그: 돌아보기   휠: 확대·축소   휠 버튼 드래그: 화면 이동");
            if (GUI.Button(new Rect(20, 55, 140, 24), "정면으로 돌아가기")) { ResetView(); }
            if (GUI.Button(new Rect(170, 55, 150, 24), "Quest 조작으로 복귀"))
            {
                EditorPrefs.SetBool(PreferenceKey, false);
                Destroy(this);
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) { ReleaseDrag(); _pressedButton = null; }
        }

        private void OnDisable()
        {
            ReleaseDrag();
            if (_camera != null) { _camera.gameObject.SetActive(false); Destroy(_camera.gameObject); }
            foreach (KeyValuePair<GameObject, bool> item in _hiddenObjects)
            {
                if (item.Key != null) { item.Key.SetActive(item.Value); }
            }
            foreach (BaseInputModule module in _disabledModules)
            {
                if (module != null) { module.enabled = true; }
            }
            _ready = false;
        }
    }
}
#endif
