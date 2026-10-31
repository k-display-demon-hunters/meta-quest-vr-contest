using System;
using System.Collections;
using Learning;
using FactoryEnvironment;
using Parchment;
using UnityEngine;

namespace UI
{
    public sealed class CNNPixelLabManager : MonoBehaviour, ILearningExperience
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
        private Func<bool> _isPassageInside;
        private Action<bool> _setPassageAvailable;
        private Func<bool> _hasRequiredDocuments;
        private Coroutine _hostedTransition;
        private bool _passageAvailable;

        private bool _progressionAvailable;
        public LearningStageId StageId => LearningStageId.PixelLab;
        public float Progress { get; private set; }
        public event System.Action<float> ProgressChanged;
        public event System.Action<bool> ExitAvailabilityChanged;

        private void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (Progress == value) { return; }
            Progress = value;
            ProgressChanged?.Invoke(value);
        }

        public bool CanExit => isActiveAndEnabled && _completed && !_switching && !_ending;

        private void ConfigureEnvironment()
        {
            // The persistent factory is optional when this experience runs on its own.
            FactoryEnvironmentManager factory = FindFirstObjectByType<FactoryEnvironmentManager>();
            if (factory == null) { return; }
            ParchmentManager parchment = FindFirstObjectByType<ParchmentManager>();
            _factoryHosted = true;
            _factoryConvolutionPose = factory.PixelLabContentPose;
            _isPassageInside = () => factory.IsPixelPassageInside;
            _setPassageAvailable = factory.SetPixelPassageAvailable;
            _hasRequiredDocuments = () => parchment.HasCollectedAll(LearningStageCatalog.Get(StageId).ParchmentIds);
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
            if (_ending) { yield break; }
            ConfigureEnvironment();
            do
            {
                SetSwitching(true);
                yield return SwitchStage(false, false);
                if (_ending || _sceneLoader.LastError == null) { yield break; }
                yield return new WaitForSeconds(3f);
            } while (!_ending);
        }

        public IEnumerator EndSession()
        {
            _ending = true;
            PublishProgressionState();
            if (_factoryHosted) { SetPassageAvailable(false); }
            while (_switching || _closingFactoryDisplay)
            {
                yield return null;
            }
            CancelHostedTransition();
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
            if (_switching || _ending || _hostedTransition != null) { return; }
            _patch = patch;
            _selectedPixel = selectedPixel;
            SetProgress(0.5f);
            if (_factoryHosted)
            {
                _hostedTransition = StartCoroutine(RunHostedTransition());
                return;
            }
            SetSwitching(true);
            StartCoroutine(SwitchStage(true, false));
        }

        private IEnumerator RunHostedTransition()
        {
            // Finish the screen's request callback before a loaded scene can be replaced.
            yield return null;
            while (!_ending)
            {
                bool available = _hasRequiredDocuments();
                SetPassageAvailable(available);
                if (!available || !_isPassageInside())
                {
                    yield return null;
                    continue;
                }
                SetSwitching(true);
                yield return SwitchStage(true, false);
                if (_ending || !isActiveAndEnabled) { break; }
                if (_sceneLoader.LastError != null)
                {
                    yield return new WaitForSeconds(3f);
                    continue;
                }
                SetPassageAvailable(false);
                break;
            }
            _hostedTransition = null;
        }

        private void SetPassageAvailable(bool available)
        {
            if (_passageAvailable == available) { return; }
            _passageAvailable = available;
            _setPassageAvailable(available);
        }

        private void CancelHostedTransition()
        {
            if (_hostedTransition != null)
            {
                StopCoroutine(_hostedTransition);
                _hostedTransition = null;
            }
            if (_factoryHosted) { SetPassageAvailable(false); }
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
            SetProgress(0f);
            _completed = false;
            CancelHostedTransition();
            PublishProgressionState();
        }

        private void HandleCompleted()
        {
            _completed = true;
            SetProgress(1f);
            PublishProgressionState();
        }

        private void SetSwitching(bool switching)
        {
            _switching = switching;
            PublishProgressionState();
        }

        private void PublishProgressionState()
        {
            bool available = CanExit;
            if (_progressionAvailable == available) { return; }
            _progressionAvailable = available;
            ExitAvailabilityChanged?.Invoke(available);
        }

        private void OnDisable()
        {
            UnsubscribeStage();
            if (!_switching) { CancelHostedTransition(); }
            else if (_factoryHosted) { SetPassageAvailable(false); }
            PublishProgressionState();
        }
    }
}
