using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UI
{
    public sealed class SpatialPixelLab : MonoBehaviour
    {
        private const int Columns = 32;
        private const int Rows = 18;
        [SerializeField] private Texture2D _sourceImage;
        [SerializeField] private Transform _board;
        [SerializeField] private MeshRenderer _boardRenderer;
        [SerializeField] private Vector2 _boardSize = new Vector2(2.4f, 1.35f);
        [SerializeField] private XRGrabInteractable _filter;
        [SerializeField] private MeshRenderer _lensRenderer;
        [SerializeField] private Transform _selectionFrame;
        [SerializeField] private Camera _camera;
        [SerializeField] private Button _detailButton;
        [SerializeField] private GameObject _detail;
        [SerializeField] private Image[] _cells;
        [SerializeField] private Text[] _values;
        [SerializeField] private PixelRevealStage _revealStage;
        [SerializeField] private Button _nextButton;
        [SerializeField] private float _introHoldDuration = 0.8f;
        [SerializeField] private float _collapseDuration = 2.2f;
        private Texture2D _colorTexture;
        private Texture2D _introTexture;
        private Coroutine _introRoutine;
        private bool _introPlaying;
        private Color32[] _pixels;
        private Texture2D _smoothTexture;
        private Texture2D _pixelTexture;
        private Material _boardMaterial;
        private Material _lensMaterial;
        private Vector3 _dockPosition;
        private Quaternion _dockRotation;
        private bool _mouseHolding;
        private bool _snapPending;
        private bool _hasSelection;
        private bool _previewValid;
        private Vector2Int _selected;
        private Vector2Int _preview;
        private Plane _dragPlane;
        private XRRayInteractor _rayInteractor;
        private bool _isRevealStage;

        public bool HasSelection => _hasSelection;
        public Vector2Int SelectedPixel => _selected;

        private void Awake()
        {
            if (!XRSettings.isDeviceActive)
            {
                UnityEngine.InputSystem.XR.TrackedPoseDriver tracking = _camera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                if (tracking != null) { tracking.enabled = false; }
                _camera.transform.localPosition = new Vector3(0, 0.08f, -0.9f);
                _camera.transform.localRotation = Quaternion.Euler(4, 0, 0);
                foreach (XRRayInteractor ray in _camera.transform.parent.GetComponentsInChildren<XRRayInteractor>())
                {
                    ray.gameObject.SetActive(false);
                }
            }
            _dockPosition = _filter.transform.position;
            _dockRotation = _filter.transform.rotation;
            _smoothTexture = CreateSmoothPhoto();
            _pixels = new Color32[Columns * Rows];
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    _pixels[y * Columns + x] = _smoothTexture.GetPixelBilinear((x + 0.5f) / Columns, (y + 0.5f) / Rows);
                }
            }
            _pixelTexture = CreateTexture(FilterMode.Point);
            _boardMaterial = _boardRenderer.material;
            _boardMaterial.mainTexture = _smoothTexture;
            _lensMaterial = _lensRenderer.material;
            _lensMaterial.mainTexture = _pixelTexture;
            _filter.selectExited.AddListener(HandleReleased);
            _filter.selectEntered.AddListener(HandleGrabbed);
            ResetLab();
        }

        private void Update()
        {
            if (_introPlaying || _isRevealStage) { return; }
            if (!XRSettings.isDeviceActive)
            {
                UpdateMouseGrab();
            }
        }

        private void LateUpdate()
        {
            if (_introPlaying || _isRevealStage) { return; }
            if (_rayInteractor != null && _filter.isSelected)
            {
                Ray ray = new Ray(_rayInteractor.transform.position, _rayInteractor.transform.forward);
                Plane plane = new Plane(_board.forward, _board.position - _board.forward * 0.3f);
                if (plane.Raycast(ray, out float distance) && distance < 5)
                {
                    _filter.transform.SetPositionAndRotation(ray.GetPoint(distance), _board.rotation);
                    Rigidbody body = _filter.GetComponent<Rigidbody>();
                    body.position = _filter.transform.position;
                    body.rotation = _filter.transform.rotation;
                }
            }
            if (_mouseHolding || _filter.isSelected)
            {
                UpdatePreview();
            }
            if (_snapPending)
            {
                _snapPending = false;
                UpdatePreview();
                if (_previewValid)
                {
                    _selected = _preview;
                    _hasSelection = true;
                }
                PlaceFilter();
                _detailButton.interactable = _hasSelection;
            }
        }

        public void ShowDetails()
        {
            if (_introPlaying || _isRevealStage || !_hasSelection || _filter.isSelected || _mouseHolding) { return; }
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    int x = _selected.x + column;
                    int y = _selected.y + 2 - row;
                    Color32 pixel = _pixels[y * Columns + x];
                    int index = row * 3 + column;
                    _cells[index].color = pixel;
                    _values[index].color = ((Color)pixel).grayscale > 0.55f ? Color.black : Color.white;
                    _values[index].text = pixel.r.ToString();
                }
            }
            _detail.SetActive(true);
        }

        public void CloseDetails()
        {
            _detail.SetActive(false);
        }

        public void ResetLab()
        {
            if (_filter.isSelected) { return; }
            _isRevealStage = false;
            if (_revealStage != null) { _revealStage.ResetStage(); }
            if (_nextButton != null) { _nextButton.interactable = true; }
            _filter.gameObject.SetActive(true);
            _mouseHolding = false;
            _snapPending = false;
            _hasSelection = false;
            _previewValid = false;
            _detail.SetActive(false);
            _detailButton.interactable = false;
            PlaceFilter();
            if (_introRoutine != null) { StopCoroutine(_introRoutine); }
            _introRoutine = StartCoroutine(PlayCollapseIntro());
        }

        public void NextStage()
        {
            if (_introPlaying || _isRevealStage || _filter.isSelected || _mouseHolding || _revealStage == null) { return; }
            _isRevealStage = true;
            _snapPending = false;
            _detail.SetActive(false);
            _detailButton.interactable = false;
            _filter.gameObject.SetActive(false);
            _selectionFrame.gameObject.SetActive(false);
            _nextButton.interactable = false;
            _revealStage.ShowEnlargedImage(_pixels, Columns, Rows);
        }

        private void UpdateMouseGrab()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || _filter.isSelected || _detail.activeSelf) { return; }
            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            if (mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out RaycastHit hit, 15)
                && hit.collider.GetComponentInParent<XRGrabInteractable>() == _filter)
            {
                _mouseHolding = true;
                _detail.SetActive(false);
                _detailButton.interactable = false;
                _dragPlane = new Plane(_board.forward, _board.position - _board.forward * 0.3f);
            }
            if (_mouseHolding && _dragPlane.Raycast(ray, out float distance))
            {
                _filter.transform.SetPositionAndRotation(ray.GetPoint(distance), _board.rotation);
            }
            if (_mouseHolding && (!mouse.leftButton.isPressed || !Application.isFocused))
            {
                _mouseHolding = false;
                _snapPending = true;
            }
        }

        private void UpdatePreview()
        {
            Vector3 local = _board.InverseTransformPoint(_filter.transform.position);
            _previewValid = Mathf.Abs(local.x) <= _boardSize.x * 0.5f
                && Mathf.Abs(local.y) <= _boardSize.y * 0.5f
                && local.z >= -0.85f && local.z <= 0.1f
                && Mathf.Abs(Vector3.Dot(_filter.transform.forward, _board.forward)) > 0.65f;
            _lensRenderer.gameObject.SetActive(_previewValid);
            _selectionFrame.gameObject.SetActive(_previewValid);
            if (!_previewValid) { return; }
            _preview = new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt((local.x / _boardSize.x + 0.5f) * Columns) - 1, 0, Columns - 3),
                Mathf.Clamp(Mathf.FloorToInt((local.y / _boardSize.y + 0.5f) * Rows) - 1, 0, Rows - 3));
            SetPreview(_preview);
        }

        private void SetPreview(Vector2Int origin)
        {
            _lensMaterial.mainTextureScale = new Vector2(3f / Columns, 3f / Rows);
            _lensMaterial.mainTextureOffset = new Vector2((float)origin.x / Columns, (float)origin.y / Rows);
            _selectionFrame.localPosition = SelectionCenter(origin) + Vector3.back * 0.015f;
            _lensRenderer.gameObject.SetActive(true);
            _selectionFrame.gameObject.SetActive(true);
        }

        private Vector3 SelectionCenter(Vector2Int origin)
        {
            return new Vector3(((origin.x + 1.5f) / Columns - 0.5f) * _boardSize.x,
                ((origin.y + 1.5f) / Rows - 0.5f) * _boardSize.y, 0);
        }

        private void PlaceFilter()
        {
            Vector3 position = _hasSelection
                ? _board.TransformPoint(SelectionCenter(_selected) + Vector3.back * 0.22f) : _dockPosition;
            Quaternion rotation = _hasSelection ? _board.rotation : _dockRotation;
            _filter.transform.SetPositionAndRotation(position, rotation);
            Rigidbody body = _filter.GetComponent<Rigidbody>();
            body.position = position;
            body.rotation = rotation;
            if (_hasSelection) { SetPreview(_selected); }
            else
            {
                _lensRenderer.gameObject.SetActive(false);
                _selectionFrame.gameObject.SetActive(false);
            }
        }

        private Texture2D CreateTexture(FilterMode filterMode)
        {
            Texture2D texture = new Texture2D(Columns, Rows, TextureFormat.RGBA32, false);
            texture.filterMode = filterMode;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(_pixels);
            texture.Apply();
            return texture;
        }

        private Texture2D CreateSmoothPhoto()
        {
            const int Width = 128;
            const int Height = 72;
            const int Radius = 2;
            Color[] source = new Color[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color color = _sourceImage.GetPixelBilinear((x + 0.5f) / Width, (y + 0.5f) / Height);
                    source[y * Width + x] = color;
                }
            }
            Color32[] colors = new Color32[source.Length];
            Color32[] colorPixels = new Color32[source.Length];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color sum = Color.clear;
                    for (int dy = -Radius; dy <= Radius; dy++)
                    {
                        for (int dx = -Radius; dx <= Radius; dx++)
                        {
                            sum += source[Mathf.Clamp(y + dy, 0, Height - 1) * Width + Mathf.Clamp(x + dx, 0, Width - 1)];
                        }
                    }
                    Color blurred = sum / 25f;
                    colorPixels[y * Width + x] = blurred;
                    byte value = (byte)Mathf.RoundToInt((0.299f * blurred.r + 0.587f * blurred.g + 0.114f * blurred.b) * 255);
                    colors[y * Width + x] = new Color32(value, value, value, 255);
                }
            }
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(colors);
            texture.Apply();
            _colorTexture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            _colorTexture.filterMode = FilterMode.Bilinear;
            _colorTexture.wrapMode = TextureWrapMode.Clamp;
            _colorTexture.SetPixels32(colorPixels);
            _colorTexture.Apply();
            return texture;
        }

        private IEnumerator PlayCollapseIntro()
        {
            _introPlaying = true;
            _filter.gameObject.SetActive(false);
            _detailButton.transform.parent.gameObject.SetActive(false);
            _boardMaterial.mainTexture = _colorTexture;
            yield return new WaitForSecondsRealtime(Mathf.Max(0, _introHoldDuration));

            int width = _colorTexture.width;
            int height = _colorTexture.height;
            Color32[] color = _colorTexture.GetPixels32();
            Color32[] gray = _smoothTexture.GetPixels32();
            Color32[] frame = new Color32[color.Length];
            if (_introTexture == null)
            {
                _introTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                _introTexture.filterMode = FilterMode.Bilinear;
                _introTexture.wrapMode = TextureWrapMode.Clamp;
            }
            _boardMaterial.mainTexture = _introTexture;
            float elapsed = 0;
            while (elapsed < Mathf.Max(0.01f, _collapseDuration))
            {
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, _collapseDuration));
                System.Array.Copy(gray, frame, gray.Length);
                // Nine strips lose color and slip downward, revealing the final grayscale image.
                // The camera and physical board stay still in VR.
                for (int strip = 0; strip < 9; strip++)
                {
                    float delay = ((strip * 5) % 9) * 0.025f;
                    float t = Mathf.Clamp01((progress - delay) / (1 - delay));
                    float fade = Mathf.SmoothStep(0, 1, t);
                    int shiftX = Mathf.RoundToInt(Mathf.Sin(strip * 2.3f) * width * 0.08f * Mathf.Sin(t * Mathf.PI));
                    int drop = Mathf.RoundToInt(t * t * height * (0.18f + strip * 0.018f));
                    for (int y = strip * height / 9; y < (strip + 1) * height / 9; y++)
                    {
                        int targetY = y - drop;
                        if (targetY < 0) { continue; }
                        for (int x = 0; x < width; x++)
                        {
                            int targetX = x + shiftX;
                            if (targetX < 0 || targetX >= width) { continue; }
                            int sourceIndex = y * width + x;
                            int targetIndex = targetY * width + targetX;
                            Color desaturated = Color.Lerp(color[sourceIndex], gray[sourceIndex], fade);
                            frame[targetIndex] = Color.Lerp(desaturated, gray[targetIndex], fade);
                        }
                    }
                }
                _introTexture.SetPixels32(frame);
                _introTexture.Apply(false);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            FinishIntro();
        }

        private void FinishIntro()
        {
            _boardMaterial.mainTexture = _smoothTexture;
            _introPlaying = false;
            _introRoutine = null;
            _filter.gameObject.SetActive(true);
            _detailButton.transform.parent.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (_introRoutine == null) { return; }
            StopCoroutine(_introRoutine);
            FinishIntro();
        }

        private void HandleReleased(SelectExitEventArgs args)
        {
            _rayInteractor = null;
            _filter.trackPosition = true;
            _filter.trackRotation = true;
            _snapPending = true;
        }

        private void HandleGrabbed(SelectEnterEventArgs args)
        {
            _rayInteractor = args.interactorObject as XRRayInteractor;
            _filter.trackPosition = _rayInteractor == null;
            _filter.trackRotation = _rayInteractor == null;
            _detail.SetActive(false);
            _detailButton.interactable = false;
        }

        private void OnDestroy()
        {
            _filter.selectExited.RemoveListener(HandleReleased);
            _filter.selectEntered.RemoveListener(HandleGrabbed);
            Destroy(_smoothTexture);
            Destroy(_colorTexture);
            Destroy(_introTexture);
            Destroy(_pixelTexture);
            Destroy(_boardMaterial);
            Destroy(_lensMaterial);
        }
    }
}
