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
        private readonly PixelUIInputController _input = new PixelUIInputController();
        public event Action ReturnRequested;
        public event Action Completed;

        private void Awake()
        {
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
            _lesson = _lessonRoot.AddComponent<PixelLessonManager>();
            _lesson.Initialize(_font);
        }
        private void OnEnable()
        {
            _lesson.CloseRequested += HandleReturn;
            _lesson.AdvanceRequested += HandleReturn;
            _lesson.Completed += HandleCompleted;
            _input.Enable(transform);
        }
        public void Begin(PixelPatchData patch) { _lesson.Show(patch); }
        private void HandleReturn() { ReturnRequested?.Invoke(); }
        private void HandleCompleted() { Completed?.Invoke(); }
        private void OnDisable()
        {
            _lesson.CloseRequested -= HandleReturn;
            _lesson.AdvanceRequested -= HandleReturn;
            _lesson.Completed -= HandleCompleted;
            _lesson.Close();
            _input.Dispose();
        }
    }
}
