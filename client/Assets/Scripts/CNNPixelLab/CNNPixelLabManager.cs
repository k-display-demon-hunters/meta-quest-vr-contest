using System;
using System.Collections;
using Learning;
using UnityEngine;

namespace UI
{
    public sealed class CNNPixelLabManager : MonoBehaviour
    {
        private PixelLabSceneLoadController _sceneLoader;
        private CNNScreenLabManager _screen;
        private CNNConvolutionLabManager _convolution;
        private PixelPatchData _patch;
        private Vector2Int _selectedPixel;
        private bool _completed;
        private bool _switching;
        private bool _ending;
        private bool _factoryHosted;
        private bool _closingFactoryDisplay;
        private Pose _factoryConvolutionPose;
        private bool _startWithConvolution;
        public bool IsSwitching => _switching;
        public string LoadError => _sceneLoader.LastError;

        private bool _pixelCompleted;
        private bool _convolutionCompleted;
        private bool _pixelAvailable;
        private bool _convolutionAvailable;
        public event Action<LearningStageId> StageCompleted;
        public event Action<LearningStageId, bool> ProgressionAvailabilityChanged;
        public bool WaitingForFactoryConvolution { get; private set; }
        public event Action ConvolutionStarted;

        public bool HasCompleted(LearningStageId stage) => stage switch
        {
            LearningStageId.PixelObservation => _pixelCompleted,
            LearningStageId.Convolution => _convolutionCompleted,
            _ => false
        };

        public bool CanProceed(LearningStageId stage)
        {
            if (!isActiveAndEnabled || _switching || _ending) { return false; }
            return stage switch
            {
                LearningStageId.PixelObservation => WaitingForFactoryConvolution,
                LearningStageId.Convolution => _completed,
                _ => false
            };
        }

        public void ConfigureHostedStages(Pose pose, bool startWithConvolution = false)
        {
            _startWithConvolution = startWithConvolution;
            _factoryHosted = true;
            _factoryConvolutionPose = pose;
        }

        public IEnumerator StartConvolution()
        {
            if (!_factoryHosted || !WaitingForFactoryConvolution || _switching || _ending) { yield break; }
            SetWaitingForConvolution(false);
            SetSwitching(true);
            yield return SwitchStage(true, false);
            if (_ending) { yield break; }
            if (_sceneLoader.LastError != null)
            {
                SetWaitingForConvolution(true);
                yield break;
            }
            ConvolutionStarted?.Invoke();
        }

        private void Awake()
        {
            _sceneLoader = GetComponent<PixelLabSceneLoadController>();
        }

        private void OnEnable()
        {
            SubscribeStage();
            PublishProgressionState();
        }

        private IEnumerator Start()
        {
            SetSwitching(true);
            yield return SwitchStage(false, false);
            if (_startWithConvolution && !_ending && _sceneLoader.LastError == null)
            {
                _patch = _screen.CreateConvolutionInput();
                SetSwitching(true);
                yield return SwitchStage(true, false);
                if (!_ending && _sceneLoader.LastError == null) { ConvolutionStarted?.Invoke(); }
            }
        }

        public IEnumerator EndSession()
        {
            _ending = true;
            SetWaitingForConvolution(false);
            while (_switching || _closingFactoryDisplay)
            {
                yield return null;
            }
            UnbindStage();
            yield return _sceneLoader.UnloadStages();
        }

        private IEnumerator SwitchStage(bool calculation, bool restoreSelection)
        {
            if (calculation && _factoryHosted)
            {
                _sceneLoader.SetFactoryConvolutionPose(_factoryConvolutionPose);
            }
            yield return _sceneLoader.PrepareStage(calculation);
            if (_sceneLoader.LastError != null)
            {
                Debug.LogError("[PixelLab] " + _sceneLoader.LastError, this);
                SetSwitching(false);
                yield break;
            }
            if (_ending)
            {
                yield return _sceneLoader.CancelPreparedStage();
                SetSwitching(false);
                yield break;
            }
            UnbindStage();
            _screen = _sceneLoader.PreparedScreen;
            _convolution = _sceneLoader.PreparedConvolution;
            SubscribeStage();
            _completed = false;
            if (calculation)
            {
                _convolution.SetInput(_patch);
                _convolution.SetFactoryHosted(_factoryHosted);
            }
            else if (restoreSelection)
            {
                _screen.SetInitialSelection(_selectedPixel);
            }
            yield return _sceneLoader.CommitStage();
            SetSwitching(false);
        }

        private void SubscribeStage()
        {
            if (_screen != null)
            {
                _screen.ConvolutionRequested += HandleConvolutionRequested;
                _screen.ResetRequested += HandleResetRequested;
            }
            if (_convolution != null)
            {
                _convolution.ReturnRequested += HandleReturnRequested;
                _convolution.Completed += HandleCompleted;
            }
        }

        private void UnsubscribeStage()
        {
            if (_screen != null)
            {
                _screen.ConvolutionRequested -= HandleConvolutionRequested;
                _screen.ResetRequested -= HandleResetRequested;
            }
            if (_convolution != null)
            {
                _convolution.ReturnRequested -= HandleReturnRequested;
                _convolution.Completed -= HandleCompleted;
            }
        }

        private void UnbindStage()
        {
            UnsubscribeStage();
            _screen = null;
            _convolution = null;
        }

        private void HandleConvolutionRequested(PixelPatchData patch, Vector2Int selectedPixel)
        {
            if (_switching || _ending) { return; }
            _patch = patch;
            _selectedPixel = selectedPixel;
            if (_factoryHosted)
            {
                SetWaitingForConvolution(true);
                return;
            }
            SetSwitching(true);
            StartCoroutine(SwitchStage(true, false));
        }

        private void HandleReturnRequested()
        {
            if (_switching || _ending || _closingFactoryDisplay) { return; }
            if (_factoryHosted)
            {
                if (!_completed) { return; }
                _closingFactoryDisplay = true;
                StartCoroutine(FinishFactoryDisplay());
                return;
            }
            SetSwitching(true);
            StartCoroutine(SwitchStage(false, true));
        }

        private IEnumerator FinishFactoryDisplay()
        {
            // Leave the UI callback before unloading its scene; completion remains available to the factory.
            yield return null;
            UnbindStage();
            yield return _sceneLoader.UnloadStages();
            _closingFactoryDisplay = false;
        }

        private void HandleResetRequested()
        {
            if (_switching || _ending) { return; }
            _patch = null;
            _completed = false;
            SetWaitingForConvolution(false);
        }

        private void HandleCompleted()
        {
            _completed = true;
            PublishProgressionState();
        }

        private void SetSwitching(bool switching)
        {
            _switching = switching;
            PublishProgressionState();
        }

        private void SetWaitingForConvolution(bool waiting)
        {
            WaitingForFactoryConvolution = waiting;
            PublishProgressionState();
        }

        private void PublishProgressionState()
        {
            PublishProgressionState(LearningStageId.PixelObservation, ref _pixelCompleted, ref _pixelAvailable);
            PublishProgressionState(LearningStageId.Convolution, ref _convolutionCompleted, ref _convolutionAvailable);
        }

        private void PublishProgressionState(LearningStageId stage, ref bool completed, ref bool wasAvailable)
        {
            if (!completed && CanProceed(stage))
            {
                completed = true;
                StageCompleted?.Invoke(stage);
            }
            bool available = CanProceed(stage);
            if (wasAvailable == available) { return; }
            wasAvailable = available;
            ProgressionAvailabilityChanged?.Invoke(stage, available);
        }

        private void OnDisable()
        {
            UnsubscribeStage();
            PublishProgressionState();
        }
    }
}
