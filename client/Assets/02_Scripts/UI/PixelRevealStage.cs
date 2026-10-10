using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UI
{
    public sealed class PixelRevealStage : MonoBehaviour
    {
        public enum RevealPhase { Inactive, Enlarging, AwaitingClick, Revealing, Revealed }

        [SerializeField] private Transform _board;
        [SerializeField] private Transform _controls;
        [SerializeField] private Camera _camera;
        [SerializeField] private Font _font;
        [SerializeField] private Vector2 _boardSize = new Vector2(2.4f, 1.35f);
        [SerializeField] private float _enlargement = 2f;
        [SerializeField] private float _enlargeDuration = 0.65f;
        [SerializeField] private float _revealDuration = 2f;
        [SerializeField] private UnityEvent _onPixelsRevealed = new UnityEvent();
        private Vector3 _originalScale;
        private Vector3 _originalControlsPosition;
        private CanvasGroup[] _columns;
        private GameObject _pixelCanvas;
        private BoxCollider _clickCollider;
        private XRSimpleInteractable _clickTarget;
        private bool _initialized;

        public RevealPhase Phase { get; private set; }
        public bool IsReadyForConvolution => Phase == RevealPhase.Revealed;

        private void Update()
        {
            if (Phase != RevealPhase.AwaitingClick || XRSettings.isDeviceActive || Mouse.current == null)
            {
                return;
            }
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 20) && hit.collider == _clickCollider)
                {
                    RevealPixels();
                }
            }
        }

        public void ShowEnlargedImage(Color32[] pixels, int columns, int rows)
        {
            if (Phase != RevealPhase.Inactive) { return; }
            if (!_initialized)
            {
                _originalScale = _board.localScale;
                _originalControlsPosition = _controls.localPosition;
                BuildPixels(pixels, columns, rows);
                // The collider becomes interactive only after enlargement, so it cannot intercept filter dragging.
                _clickCollider = _board.gameObject.AddComponent<BoxCollider>();
                _clickCollider.size = new Vector3(_boardSize.x, _boardSize.y, 0.04f);
                _clickCollider.enabled = false;
                _clickTarget = _board.gameObject.AddComponent<XRSimpleInteractable>();
                _clickTarget.colliders.Clear();
                _clickTarget.colliders.Add(_clickCollider);
                _clickTarget.selectEntered.AddListener(HandleImageSelected);
                _clickTarget.enabled = false;
                _initialized = true;
            }
            _pixelCanvas.SetActive(true);
            foreach (CanvasGroup column in _columns) { column.alpha = 0; }
            Phase = RevealPhase.Enlarging;
            StartCoroutine(EnlargeImage());
        }

        public void RevealPixels()
        {
            if (Phase != RevealPhase.AwaitingClick) { return; }
            Phase = RevealPhase.Revealing;
            StartCoroutine(RevealColumns());
        }

        public void ResetStage()
        {
            StopAllCoroutines();
            Phase = RevealPhase.Inactive;
            if (!_initialized) { return; }
            _clickTarget.enabled = false;
            _clickCollider.enabled = false;
            _pixelCanvas.SetActive(false);
            _board.localScale = _originalScale;
            _controls.localPosition = _originalControlsPosition;
        }

        private IEnumerator EnlargeImage()
        {
            float elapsed = 0;
            Vector3 controlsPosition = new Vector3(_originalControlsPosition.x, 0.18f, 1.05f);
            while (elapsed < _enlargeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, _enlargeDuration)));
                _board.localScale = Vector3.Lerp(_originalScale, _originalScale * _enlargement, progress);
                _controls.localPosition = Vector3.Lerp(_originalControlsPosition, controlsPosition, progress);
                yield return null;
            }
            _board.localScale = _originalScale * _enlargement;
            _controls.localPosition = controlsPosition;
            _clickCollider.enabled = true;
            _clickTarget.enabled = true;
            Phase = RevealPhase.AwaitingClick;
        }

        private IEnumerator RevealColumns()
        {
            float elapsed = 0;
            const float FadeWidth = 0.2f;
            while (elapsed < _revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, _revealDuration));
                for (int x = 0; x < _columns.Length; x++)
                {
                    float start = x / (float)Mathf.Max(1, _columns.Length - 1) * (1 - FadeWidth);
                    _columns[x].alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01((progress - start) / FadeWidth));
                }
                yield return null;
            }
            foreach (CanvasGroup column in _columns) { column.alpha = 1; }
            Phase = RevealPhase.Revealed;
            _onPixelsRevealed.Invoke();
        }

        private void BuildPixels(Color32[] pixels, int columns, int rows)
        {
            const float CellSize = 96;
            _pixelCanvas = new GameObject("Image pixels and values", typeof(RectTransform), typeof(Canvas));
            RectTransform canvasRect = (RectTransform)_pixelCanvas.transform;
            canvasRect.SetParent(_board, false);
            canvasRect.localPosition = Vector3.back * 0.045f;
            canvasRect.sizeDelta = new Vector2(columns * CellSize, rows * CellSize);
            canvasRect.localScale = new Vector3(_boardSize.x / (columns * CellSize), _boardSize.y / (rows * CellSize), 1);
            Canvas canvas = _pixelCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _camera;
            _columns = new CanvasGroup[columns];
            for (int x = 0; x < columns; x++)
            {
                RectTransform columnRect = Rect(canvasRect, "Column " + x, (x + 0.5f - columns / 2f) * CellSize, 0, CellSize, rows * CellSize);
                CanvasGroup column = columnRect.gameObject.AddComponent<CanvasGroup>();
                column.alpha = 0;
                column.blocksRaycasts = false;
                column.interactable = false;
                _columns[x] = column;
                for (int y = 0; y < rows; y++)
                {
                    Color32 pixel = pixels[y * columns + x];
                    RectTransform cell = Rect(columnRect, "Pixel " + x + "," + (rows - 1 - y), 0, (y + 0.5f - rows / 2f) * CellSize, CellSize, CellSize);
                    Image fill = cell.gameObject.AddComponent<Image>();
                    fill.color = pixel;
                    fill.raycastTarget = false;
                    Text text = Rect(cell, "Value", 0, 0, CellSize - 4, CellSize - 4).gameObject.AddComponent<Text>();
                    text.font = _font;
                    text.fontSize = 30;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = ((Color)pixel).grayscale > 0.55f ? Color.black : Color.white;
                    text.text = pixel.r.ToString();
                    text.raycastTarget = false;
                    text.supportRichText = false;
                }
            }
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private void HandleImageSelected(SelectEnterEventArgs args)
        {
            RevealPixels();
        }

        private void OnDisable()
        {
            ResetStage();
        }
    }
}
