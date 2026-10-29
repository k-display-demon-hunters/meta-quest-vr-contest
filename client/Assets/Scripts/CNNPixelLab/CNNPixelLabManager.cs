using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UI
{
    public sealed class CNNPixelLabManager : MonoBehaviour
    {
        // Keep the scene's serialized bindings; compose runtime modules from them in Awake.
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

        private PixelImageManager _image;
        private PixelLessonManager _lesson;
        private PixelLabUIController _ui;
        private readonly PixelUIInputController _input = new PixelUIInputController();

        public Vector3 SelectionWorldPosition => _image.SelectionWorldPosition;
        public bool HasSelection => _image != null && _image.HasSelection;
        public Vector2Int SelectedPixel => _image.SelectedPixel;
        public bool ConvolutionCompleted { get; private set; }

        private void Awake()
        {
            // Additively loaded scenes use the surviving persistent rig camera.
            _camera = Camera.main;
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
            {
                canvas.worldCamera = _camera;
            }
            _image = new PixelImageManager(_sourceImage, _board, _boardRenderer, _boardSize,
                _filter, _lensRenderer, _selectionFrame, _revealStage);
            _lesson = _detail.AddComponent<PixelLessonManager>();
            _lesson.Initialize(_values[0].font);
            _ui = new PixelLabUIController(_detailButton, _nextButton, _values[0].font);
            ResetLab();
        }

        private void OnEnable()
        {
            _image.SelectionChanged += HandleSelectionChanged;
            _image.FilterGrabbed += HandleFilterGrabbed;
            _image.PixelsRevealed += HandlePixelsRevealed;
            _lesson.CloseRequested += CloseDetails;
            _lesson.AdvanceRequested += HandleAdvanceRequested;
            _lesson.BackgroundHiddenChanged += SetBackgroundHidden;
            _lesson.SelectionMoveRequested += MoveSelection;
            _lesson.Completed += MarkConvolutionCompleted;
            _image.EnableInput();
            _input.Enable(transform);
        }

        private void LateUpdate()
        {
            _image.UpdateSelection();
        }

        public void ShowDetails()
        {
            if (!_image.CanBeginReveal || !_image.HasSelection)
            {
                return;
            }
            _ui.ShowRevealStarted();
            _image.BeginReveal();
        }

        public void BeginConvolution()
        {
            if (!_image.IsReadyForConvolution || !_image.HasSelection || _image.IsFilterHeld)
            {
                return;
            }
            ShowLesson(true);
            SetBackgroundHidden(true);
        }

        public void CloseDetails()
        {
            _lesson.Close();
            SetBackgroundHidden(false);
        }

        public void SetBackgroundHidden(bool hidden)
        {
            _image.SetBackgroundHidden(hidden);
            _ui.SetBackgroundHidden(hidden);
        }

        public void MarkConvolutionCompleted()
        {
            ConvolutionCompleted = true;
        }

        public void ResetLab()
        {
            if (_image.IsFilterHeld)
            {
                return;
            }
            ConvolutionCompleted = false;
            CloseDetails();
            _image.Reset();
            _ui.ResetControls();
            UpdateInstruction();
        }

        public void MoveSelection(int dx, int dy)
        {
            if (_image.IsRevealing || !_image.HasSelection || _image.IsFilterHeld)
            {
                return;
            }
            _image.MoveSelection(dx, dy);
            ShowLesson(_image.IsReadyForConvolution);
        }

        public void NextStage()
        {
            if (_image.IsRevealing)
            {
                return;
            }
            if (_image.IsReadyForConvolution)
            {
                BeginConvolution();
                return;
            }
            ShowDetails();
        }

        private void ShowLesson(bool calculationAvailable)
        {
            _lesson.Show(_image.Pixels, PixelImageManager.Columns, PixelImageManager.Rows,
                _image.SelectedPixel, _image.SelectionWorldPosition, calculationAvailable);
        }

        private void UpdateInstruction()
        {
            _ui.UpdateInstruction(_image.IsReadyForConvolution, _image.HasSelection);
        }

        private void HandleSelectionChanged()
        {
            _ui.SetSelectionAvailable(_image.HasSelection);
            UpdateInstruction();
        }

        private void HandleFilterGrabbed()
        {
            _ui.ShowFilterGrabbed();
            CloseDetails();
        }

        private void HandlePixelsRevealed()
        {
            _ui.ShowPixelsRevealed(_image.HasSelection);
            UpdateInstruction();
        }

        private void HandleAdvanceRequested()
        {
            CloseDetails();
            if (!ConvolutionCompleted)
            {
                NextStage();
            }
        }

#if UNITY_EDITOR
        public bool EditorDetailsVisible => _lesson.IsVisible;
        public Vector3 EditorPreviewFocus => _lesson.IsVisible ? _lesson.PreviewFocus : _image.BoardPosition;

        public bool EditorCanDragFilter(XRGrabInteractable target)
        {
            return _image.IsFilter(target) && !_image.IsRevealing && !_lesson.IsVisible;
        }

        public void EditorDragFilter(Ray ray) => _image.EditorDragFilter(ray);
        public void EditorReleaseFilter() => _image.EditorReleaseFilter();
#endif

        private void OnDisable()
        {
            _image.SelectionChanged -= HandleSelectionChanged;
            _image.FilterGrabbed -= HandleFilterGrabbed;
            _image.PixelsRevealed -= HandlePixelsRevealed;
            _lesson.CloseRequested -= CloseDetails;
            _lesson.AdvanceRequested -= HandleAdvanceRequested;
            _lesson.BackgroundHiddenChanged -= SetBackgroundHidden;
            _lesson.SelectionMoveRequested -= MoveSelection;
            _lesson.Completed -= MarkConvolutionCompleted;
            _image.DisableInput(transform);
            _input.Dispose();
        }

        private void OnDestroy()
        {
            _image?.Dispose();
        }
    }
}
