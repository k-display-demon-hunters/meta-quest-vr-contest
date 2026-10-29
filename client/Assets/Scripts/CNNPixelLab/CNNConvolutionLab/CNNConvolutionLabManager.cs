using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class CNNConvolutionLabManager : MonoBehaviour
    {
        [SerializeField] private GameObject _lessonRoot;
        [SerializeField] private Font _font;
        private PixelLessonManager _lesson;
        private PixelPatchData _patch;
        private bool _initialized;
        private readonly PixelUIInputController _input = new PixelUIInputController();
        public event Action ReturnRequested;
        public event Action Completed;

        private void Awake()
        {
            _lesson = _lessonRoot.AddComponent<PixelLessonManager>();
        }
        private void Start()
        {
            _lesson.Initialize(_font);
            _initialized = true;
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
            _lesson.Show(_patch ?? new PixelPatchData(new Color32[9]));
        }
        private void OnEnable()
        {
            _lesson.CloseRequested += HandleReturn;
            _lesson.AdvanceRequested += HandleReturn;
            _lesson.Completed += HandleCompleted;
            _input.Enable(transform);
        }
        public void SetInput(PixelPatchData patch)
        {
            _patch = patch;
        }
        private void HandleReturn() { ReturnRequested?.Invoke(); }
        private void HandleCompleted() { Completed?.Invoke(); }
        private void OnDisable()
        {
            _lesson.CloseRequested -= HandleReturn;
            _lesson.AdvanceRequested -= HandleReturn;
            _lesson.Completed -= HandleCompleted;
            if (_initialized) { _lesson.Close(); }
            _input.Dispose();
        }
    }
}
