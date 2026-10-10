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
        private RgbConvolutionView _rgbView;
        private Text _instruction;
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
            _rgbView = _detail.AddComponent<RgbConvolutionView>();
            _rgbView.Initialize(this, _values[0].font);
            RectTransform hint = new GameObject("Interaction hint", typeof(RectTransform)).GetComponent<RectTransform>();
            hint.SetParent(_detailButton.transform.parent, false);
            hint.anchorMin = hint.anchorMax = hint.pivot = Vector2.one * 0.5f;
            hint.anchoredPosition = new Vector2(0, -70);
            hint.sizeDelta = new Vector2(1200, 70);
            _instruction = hint.gameObject.AddComponent<Text>();
            _instruction.font = _values[0].font;
            _instruction.fontSize = 26;
            _instruction.alignment = TextAnchor.MiddleCenter;
            _instruction.color = Color.white;
            _instruction.raycastTarget = false;
            if (_revealStage != null) { _revealStage.PixelsRevealed += HandlePixelsRevealed; }
            ResetLab();
        }

        private void Update()
        {
            if (_isRevealStage) { return; }
            if (!XRSettings.isDeviceActive)
            {
                UpdateMouseGrab();
            }
        }

        private void LateUpdate()
        {
            if (_isRevealStage) { return; }
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
                UpdateInstruction();
            }
        }

        public void ShowDetails()
        {
            if (_isRevealStage || !_hasSelection || _filter.isSelected || _mouseHolding) { return; }
            _rgbView.Show(_pixels, Columns, Rows, _selected, false);
            _detail.SetActive(true);
        }

        public void BeginConvolution()
        {
            if (_revealStage == null || !_revealStage.IsReadyForConvolution || !_hasSelection
                || _filter.isSelected || _mouseHolding) { return; }
            _rgbView.Show(_pixels, Columns, Rows, _selected, true);
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
            if (_nextButton != null)
            {
                _nextButton.interactable = true;
                _nextButton.GetComponentInChildren<Text>().text = "다음으로";
            }
            _filter.gameObject.SetActive(true);
            _mouseHolding = false;
            _snapPending = false;
            _hasSelection = false;
            _previewValid = false;
            _detail.SetActive(false);
            _detailButton.interactable = false;
            _detailButton.GetComponentInChildren<Text>().text = "더 자세히 보기";
            PlaceFilter();
            UpdateInstruction();
        }

        public void MoveSelection(int dx, int dy)
        {
            if (_isRevealStage || !_hasSelection || _filter.isSelected || _mouseHolding) { return; }
            _selected = new Vector2Int(Mathf.Clamp(_selected.x + dx, 0, Columns - 3),
                Mathf.Clamp(_selected.y + dy, 0, Rows - 3));
            PlaceFilter();
            _rgbView.Show(_pixels, Columns, Rows, _selected, _revealStage != null && _revealStage.IsReadyForConvolution);
        }

        private void HandlePixelsRevealed()
        {
            _isRevealStage = false;
            if (!_hasSelection)
            {
                _selected = new Vector2Int((Columns - 3) / 2, (Rows - 3) / 2);
                _hasSelection = true;
            }
            _filter.gameObject.SetActive(true);
            PlaceFilter();
            _detailButton.interactable = _hasSelection;
            _nextButton.interactable = true;
            _nextButton.GetComponentInChildren<Text>().text = "합성곱 학습 시작";
            UpdateInstruction();
        }
        public void NextStage()
        {
            if (_revealStage != null && _revealStage.IsReadyForConvolution)
            {
                BeginConvolution();
                return;
            }
            if (_isRevealStage || _filter.isSelected || _mouseHolding || _revealStage == null
                || _revealStage.Phase != PixelRevealStage.RevealPhase.Inactive) { return; }
            _isRevealStage = true;
            _instruction.gameObject.SetActive(false);
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
                _instruction.text = "원하는 부분에서 필터를 놓으세요";
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
                    colors[y * Width + x] = blurred;
                }
            }
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(colors);
            texture.Apply();
            return texture;
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
            _instruction.text = "원하는 부분에서 트리거를 놓으세요";
            _rayInteractor = args.interactorObject as XRRayInteractor;
            _filter.trackPosition = _rayInteractor == null;
            _filter.trackRotation = _rayInteractor == null;
            _detail.SetActive(false);
            _detailButton.interactable = false;
        }

        private void UpdateInstruction()
        {
            _instruction.gameObject.SetActive(true);
            if (_revealStage != null && _revealStage.IsReadyForConvolution)
            {
                _instruction.text = "R/G/B를 골라 픽셀 값을 살펴보세요\n관찰을 마치면 ‘합성곱 학습 시작’을 누르세요";
                return;
            }
            _instruction.text = _hasSelection
                ? "‘더 자세히 보기’를 눌러 RGB와 계산을 살펴보세요"
                : "필터를 잡아 이미지 위에 놓으세요\n컨트롤러: 가리킨 뒤 트리거로 드래그 · PC: 마우스로 드래그";
        }

        private void OnDestroy()
        {
            _filter.selectExited.RemoveListener(HandleReleased);
            _filter.selectEntered.RemoveListener(HandleGrabbed);
            if (_revealStage != null) { _revealStage.PixelsRevealed -= HandlePixelsRevealed; }
            Destroy(_smoothTexture);
            Destroy(_pixelTexture);
            Destroy(_boardMaterial);
            Destroy(_lensMaterial);
        }
    }
}
