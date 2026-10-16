using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

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
        private bool _snapPending;
        private bool _hasSelection;
        private bool _previewValid;
        private Vector2Int _selected;
        private Vector2Int _preview;
        private XRRayInteractor _rayInteractor;
        private bool _isRevealStage;
        private GameObject _ownedEventSystem;
        private XRUIInputModule _addedUIInputModule;
        private bool _initialized;
        private bool _backgroundHidden;
        private readonly bool[] _backgroundStates = new bool[3];

        public Vector3 SelectionWorldPosition => _board.TransformPoint(SelectionCenter(_selected));
        public bool HasSelection => _hasSelection;
        public bool ConvolutionCompleted { get; private set; }
        public void MarkConvolutionCompleted() { ConvolutionCompleted = true; }
        public void RebaseDock(Vector3 offset) { _dockPosition += offset; }
        public Vector2Int SelectedPixel => _selected;

#if UNITY_EDITOR
        public bool EditorDetailsVisible => _detail.activeSelf;
        public Vector3 EditorPreviewFocus => _detail.activeSelf ? _detail.transform.position + Vector3.right * 0.35f : _board.position;

        public bool EditorCanDragFilter(XRGrabInteractable target)
        {
            return target == _filter && !_isRevealStage && !_detail.activeSelf;
        }

        public void EditorDragFilter(Ray ray)
        {
            Plane plane = new Plane(_board.forward, _board.position - _board.forward * 0.3f);
            if (!plane.Raycast(ray, out float distance)) { return; }
            _filter.transform.SetPositionAndRotation(ray.GetPoint(distance), _board.rotation);
            Rigidbody body = _filter.GetComponent<Rigidbody>();
            body.position = _filter.transform.position;
            body.rotation = _filter.transform.rotation;
            UpdatePreview();
        }

        public void EditorReleaseFilter()
        {
            _snapPending = true;
        }
#endif

        private void Awake()
        {
            // Resolve the surviving rig, since the scene's duplicate may have been removed.
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("[CNNPixelLab] An active XRPlayerRig camera is required.", this);
                enabled = false;
                return;
            }
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
            {
                canvas.worldCamera = _camera;
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
            _initialized = true;
            ResetLab();
        }

        private void OnEnable()
        {
            if (!_initialized) { return; }
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("CNN Pixel Lab UI Input", typeof(EventSystem), typeof(XRUIInputModule));
                _ownedEventSystem.transform.SetParent(transform, false);
            }
            else if (EventSystem.current.GetComponent<XRUIInputModule>() == null)
            {
                _addedUIInputModule = EventSystem.current.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private void LateUpdate()
        {
            if (!_initialized || _isRevealStage) { return; }
            if (_rayInteractor != null && _filter.isSelected)
            {
                Transform origin = _rayInteractor.rayOriginTransform;
                Ray ray = new Ray(origin.position, origin.forward);
                Plane plane = new Plane(_board.forward, _board.position - _board.forward * 0.3f);
                if (plane.Raycast(ray, out float distance) && distance < 5)
                {
                    _filter.transform.SetPositionAndRotation(ray.GetPoint(distance), _board.rotation);
                    Rigidbody body = _filter.GetComponent<Rigidbody>();
                    body.position = _filter.transform.position;
                    body.rotation = _filter.transform.rotation;
                }
            }
            if (_filter.isSelected)
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
            if (_isRevealStage || !_hasSelection || _filter.isSelected) { return; }
            _rgbView.Show(_pixels, Columns, Rows, _selected, false);
            _detail.SetActive(true);
        }

        public void BeginConvolution()
        {
            if (_revealStage == null || !_revealStage.IsReadyForConvolution || !_hasSelection
                || _filter.isSelected) { return; }
            _rgbView.Show(_pixels, Columns, Rows, _selected, true);
            SetBackgroundHidden(true);
        }

        public void CloseDetails()
        {
            foreach (SpatialConvolutionController spatial in _detail.GetComponentsInChildren<SpatialConvolutionController>(true))
            {
                spatial.ReleaseInteractions();
            }
            _detail.SetActive(false);
            SetBackgroundHidden(false);
        }

        public void SetBackgroundHidden(bool hidden)
        {
            if (_backgroundHidden == hidden) { return; }
            GameObject[] background = { _board.gameObject, _filter.gameObject, _detailButton.transform.parent.gameObject };
            for (int i = 0; i < background.Length; i++)
            {
                if (hidden) { _backgroundStates[i] = background[i].activeSelf; }
                background[i].SetActive(hidden ? false : _backgroundStates[i]);
            }
            _backgroundHidden = hidden;
        }

        public void ResetLab()
        {
            if (_filter.isSelected) { return; }
            ConvolutionCompleted = false;
            CloseDetails();
            _isRevealStage = false;
            if (_revealStage != null) { _revealStage.ResetStage(); }
            if (_nextButton != null)
            {
                _nextButton.interactable = true;
                _nextButton.GetComponentInChildren<Text>().text = "다음으로";
            }
            _filter.gameObject.SetActive(true);
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
            if (_isRevealStage || !_hasSelection || _filter.isSelected) { return; }
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
            Text lessonLabel = _nextButton.GetComponentInChildren<Text>();
            lessonLabel.text = "합성곱 연산\n원리 알아보기";
            lessonLabel.fontSize = 24;
            UpdateInstruction();
        }
        public void NextStage()
        {
            if (_revealStage != null && _revealStage.IsReadyForConvolution)
            {
                BeginConvolution();
                return;
            }
            if (_isRevealStage || _filter.isSelected || _revealStage == null
                || _revealStage.Phase != PixelRevealStage.RevealPhase.Inactive) { return; }
            CloseDetails();
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
            _instruction.text = "시선으로 필터를 옮기고 원하는 부분에서 Grip을 놓으세요";
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
                _instruction.text = "R/G/B를 골라 픽셀 값을 살펴보세요\n관찰을 마치면 ‘합성곱 연산 원리 알아보기’를 누르세요";
                return;
            }
            _instruction.text = _hasSelection
                ? "‘더 자세히 보기’를 눌러 RGB 값을 살펴보세요"
                : "필터를 잡아 이미지 위에 놓으세요\n오른손: Grip을 누른 채 시선으로 이동 · 클릭: Trigger";
        }

        private void OnDisable()
        {
            if (_ownedEventSystem != null)
            {
                _ownedEventSystem.SetActive(false);
                Destroy(_ownedEventSystem);
                _ownedEventSystem = null;
            }
            if (_addedUIInputModule != null)
            {
                Destroy(_addedUIInputModule);
                _addedUIInputModule = null;
            }
            if (_initialized && _filter != null && _filter.isSelected)
            {
                // Keep a held filter in the content scene when the persistent rig survives.
                if (_filter.interactionManager != null)
                {
                    _filter.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_filter);
                }
                _filter.transform.SetParent(transform, true);
            }
        }

        private void OnDestroy()
        {
            if (_filter != null)
            {
                _filter.selectExited.RemoveListener(HandleReleased);
                _filter.selectEntered.RemoveListener(HandleGrabbed);
            }
            if (_revealStage != null) { _revealStage.PixelsRevealed -= HandlePixelsRevealed; }
            Destroy(_smoothTexture);
            Destroy(_pixelTexture);
            Destroy(_boardMaterial);
            Destroy(_lensMaterial);
        }
    }
}
