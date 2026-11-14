using Core;
using Learning;
using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Owns stage order, session lifetime and progress; practice internals stay behind one entry point.
    public sealed class CNNConvolutionLabManager : MonoBehaviour, ILearningExperience
    {
        [SerializeField] private LessonUIController _ui;
        [SerializeField] private LessonNarrationController _narration;
        [SerializeField] private WorkbenchBuilder _spatial;
        [SerializeField] private Font _font;
        private ConvolutionPracticeManager _practice;
        private bool _started;
        private bool _completed;

        public LearningStageId StageId => LearningStageId.ConvolutionLab;
        public float Progress { get; private set; }
        public bool CanExit => isActiveAndEnabled && _completed;
        public event Action<bool> ExitAvailabilityChanged;
        public event Action<float> ProgressChanged;

        private void Awake()
        {
            _practice = new ConvolutionPracticeManager(_ui, _narration, _spatial, _font);
        }

        private void OnEnable()
        {
            _practice.FinishRequested += FinishPractice;
            _practice.Subscribe();
            _practice.RestartRequested += RestartPractice;
            if (_started && !_completed)
            {
                BeginPractice();
            }

            ExitAvailabilityChanged?.Invoke(CanExit);
        }

        private void Start()
        {
            _started = true;
            BeginPractice();
        }

        private void BeginPractice()
        {
            _practice.Prepare();
            StartCoroutine(RunLesson());
        }

        private void RestartPractice()
        {
            StopPractice();
            Progress = 0f;
            _completed = false;
            ExitAvailabilityChanged?.Invoke(CanExit);
            ProgressChanged?.Invoke(Progress);
            BeginPractice();
        }

        public IEnumerator EndSession()
        {
            StopPractice();
            yield break;
        }

        private void StopPractice()
        {
            StopAllCoroutines();
            _practice.Cancel();
        }

        private IEnumerator RunLesson()
        {
            yield return _practice.PlaceFilter();
            SetProgress(0.2f);
            yield return _practice.CalculateFirstOutput();
            SetProgress(0.45f);
            yield return _practice.SweepPicture();
            SetProgress(0.9f);
            yield return _practice.ExplainOutput();
        }

        private void FinishPractice()
        {
            _completed = true;
            SetProgress(1f);
            ExitAvailabilityChanged?.Invoke(CanExit);
        }

        private void SetProgress(float value)
        {
            Progress = Mathf.Max(Progress, value);
            ProgressChanged?.Invoke(Progress);
        }

        private void OnDisable()
        {
            _practice.FinishRequested -= FinishPractice;
            _practice.Unsubscribe();
            _practice.RestartRequested -= RestartPractice;
            StopPractice();
            ExitAvailabilityChanged?.Invoke(false);
        }
    }
}
