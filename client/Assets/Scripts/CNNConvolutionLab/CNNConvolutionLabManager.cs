using Core;
using System;
using System.Collections;
using Learning;
using UI;
using UnityEngine;

namespace CNNConvolution
{
    // Owns the two consecutive parts of ROOM 1-2 and its single completion contract.
    public sealed class CNNConvolutionLabManager : MonoBehaviour, ILearningExperience
    {
        private enum ExperiencePhase { Waiting, Convolution, Inspection, Finished }
        [SerializeField] private ConvolutionLessonManager _lesson;
        [SerializeField] private FeatureInspectionManager _inspection;
        private readonly PixelUIInputController _input = new PixelUIInputController();
        private ConvolutionInputData _inputData;
        private ExperiencePhase _phase;
        private bool _sessionManaged;
        private bool _stageActive;
        private bool _started;
        private bool _completed;

        public LearningStageId StageId => LearningStageId.ConvolutionLab;
        public float Progress { get; private set; }
        public bool CanExit => isActiveAndEnabled && _completed;
        public event Action<float> ProgressChanged;
        public event Action<bool> ExitAvailabilityChanged;

        private void OnEnable()
        {
            _input.Enable(transform);
            _lesson.ProgressChanged += HandleConvolutionProgress;
            _lesson.Completed += HandleConvolutionCompleted;
            _lesson.ContinueRequested += HandleInspectionRequested;
            _inspection.Completed += HandleComparisonCompleted;
            _inspection.ExitRequested += HandleFinishRequested;
            if (_started && _stageActive)
            {
                if (_phase == ExperiencePhase.Inspection || _phase == ExperiencePhase.Finished)
                {
                    _phase = ExperiencePhase.Inspection;
                    _inspection.BeginInspection();
                }
                else { BeginWhenReady(); }
            }
            ExitAvailabilityChanged?.Invoke(CanExit);
        }

        private void Start()
        {
            _started = true;
            if (!_sessionManaged) { _stageActive = true; }
            BeginWhenReady();
        }

        // Called from sceneLoaded before Start. A preload does not fix a demo input
        // or start the lesson before the visitor has finished the observation.
        public void PrepareForSession()
        {
            _sessionManaged = true;
            _stageActive = false;
            _lesson.StopPractice();
            _inspection.StopInspection();
        }

        public void SetStageActive(bool active)
        {
            if (_stageActive == active) { return; }
            _stageActive = active;
            if (!active)
            {
                if (_phase == ExperiencePhase.Convolution)
                {
                    _lesson.StopPractice();
                    _phase = ExperiencePhase.Waiting;
                }
                _inspection.StopInspection();
            }
            else if (_phase == ExperiencePhase.Inspection || _phase == ExperiencePhase.Finished)
            {
                // Return to the same loaded desk with a clear comparison starting
                // point. Previously earned session completion remains recorded.
                _phase = ExperiencePhase.Inspection;
                _inspection.BeginInspection();
            }
            else { BeginWhenReady(); }
        }

        public void SetInput(ConvolutionInputData input)
        {
            if (ReferenceEquals(_inputData, input)) { return; }
            ResetExperience();
            _inputData = input;
            BeginWhenReady();
        }

        public void ClearInput()
        {
            // Resetting the previous observation invalidates its selected snapshot.
            _inputData = null;
            ResetExperience();
        }

        private void BeginWhenReady()
        {
            if (!_started || !_stageActive || _phase != ExperiencePhase.Waiting) { return; }
            _phase = ExperiencePhase.Convolution;
            if (_inputData == null)
            {
                // Standalone entry and direct tablet travel have no completed
                // observation. The fixed fixture is visibly labelled in the lesson.
                _lesson.BeginPractice();
            }
            else { _lesson.BeginPractice(_inputData); }
        }

        private void HandleConvolutionProgress(float value) { SetProgress(value * 0.75f); }
        private void HandleConvolutionCompleted() { SetProgress(0.75f); }

        private void HandleInspectionRequested()
        {
            if (_phase != ExperiencePhase.Convolution) { return; }
            _lesson.StopPractice();
            _phase = ExperiencePhase.Inspection;
            _inspection.BeginInspection();
            SetProgress(0.8f);
        }

        private void HandleComparisonCompleted() { SetProgress(0.95f); }

        private void HandleFinishRequested()
        {
            if (_phase != ExperiencePhase.Inspection) { return; }
            _phase = ExperiencePhase.Finished;
            _completed = true;
            SetProgress(1f);
            ExitAvailabilityChanged?.Invoke(CanExit);
        }

        private void SetProgress(float progress)
        {
            Progress = Mathf.Max(Progress, progress);
            ProgressChanged?.Invoke(Progress);
        }

        private void ResetExperience()
        {
            _lesson.StopPractice();
            _inspection.StopInspection();
            _phase = ExperiencePhase.Waiting;
            _completed = false;
            Progress = 0f;
            ProgressChanged?.Invoke(Progress);
            ExitAvailabilityChanged?.Invoke(false);
        }

        public IEnumerator EndSession()
        {
            _stageActive = false;
            _lesson.StopPractice();
            _inspection.StopInspection();
            _input.Dispose();
            yield break;
        }

        private void OnDisable()
        {
            _lesson.ProgressChanged -= HandleConvolutionProgress;
            _lesson.Completed -= HandleConvolutionCompleted;
            _lesson.ContinueRequested -= HandleInspectionRequested;
            _inspection.Completed -= HandleComparisonCompleted;
            _inspection.ExitRequested -= HandleFinishRequested;
            _lesson.StopPractice();
            _inspection.StopInspection();
            if (_phase == ExperiencePhase.Convolution) { _phase = ExperiencePhase.Waiting; }
            _input.Dispose();
            ExitAvailabilityChanged?.Invoke(false);
        }
    }
}
