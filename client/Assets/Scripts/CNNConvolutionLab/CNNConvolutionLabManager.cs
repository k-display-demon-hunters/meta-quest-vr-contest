using Core;
using UI;
using System;
using System.Collections;
using Learning;
using UnityEngine;

namespace CNNConvolution
{
    public sealed class CNNConvolutionLabManager : MonoBehaviour, ILearningExperience
    {
        [SerializeField] private ConvolutionLessonManager _lesson;
        private PixelPatchData _patch;
        private bool _completed;
        private readonly PixelUIInputController _input = new PixelUIInputController();
        public LearningStageId StageId => LearningStageId.ConvolutionLab;
        public float Progress { get; private set; }
        public bool CanExit => isActiveAndEnabled && _completed;
        public event Action<float> ProgressChanged;
        public event Action<bool> ExitAvailabilityChanged;
        public void SetInput(PixelPatchData patch) { _patch = patch; }
        private void OnEnable()
        {
            _input.Enable(transform);
            _lesson.ProgressChanged += HandleProgress;
            _lesson.Completed += HandleCompleted;
        }
        private void Start()
        {
            // Direct entry has an explicit practice example instead of an all-zero patch.
            bool example = _patch == null;
            if (example)
            {
                _patch = new PixelPatchData(new Color32[] {
                    new Color32(85,170,255,255), new Color32(170,85,0,255), new Color32(255,170,85,255),
                    new Color32(0,85,170,255), new Color32(170,255,85,255), new Color32(85,0,255,255),
                    new Color32(255,85,170,255), new Color32(85,170,0,255), new Color32(170,85,255,255) });
            }
            _lesson.BeginPractice(_patch, example);
        }
        private void HandleProgress(float value) { Progress = value; ProgressChanged?.Invoke(value); }
        private void HandleCompleted() { _completed = true; ExitAvailabilityChanged?.Invoke(CanExit); }
        public IEnumerator EndSession() { _lesson.StopPractice(); _input.Dispose(); yield break; }
        private void OnDisable()
        {
            _lesson.ProgressChanged -= HandleProgress;
            _lesson.Completed -= HandleCompleted;
            _input.Dispose();
            ExitAvailabilityChanged?.Invoke(false);
        }
    }
}
