using System;
using System.Collections;
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

        public bool ConvolutionCompleted => _completed && !_switching && !_ending;
        public bool WaitingForFactoryConvolution { get; private set; }
        public event Action ScreenCompleted;
        public event Action ConvolutionStarted;

        public void ConfigureFactoryConvolution(Pose pose)
        {
            _factoryHosted = true;
            _factoryConvolutionPose = pose;
        }

        public IEnumerator StartFactoryConvolution()
        {
            if (!_factoryHosted || !WaitingForFactoryConvolution || _switching || _ending) { yield break; }
            WaitingForFactoryConvolution = false;
            _switching = true;
            yield return SwitchStage(true, false);
            if (_ending) { yield break; }
            if (_sceneLoader.LastError != null)
            {
                WaitingForFactoryConvolution = true;
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
        }

        private IEnumerator Start()
        {
            _switching = true;
            yield return SwitchStage(false, false);
        }

        public IEnumerator EndSession()
        {
            _ending = true;
            WaitingForFactoryConvolution = false;
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
                _switching = false;
                yield break;
            }
            if (_ending)
            {
                yield return _sceneLoader.CancelPreparedStage();
                _switching = false;
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
            _switching = false;
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
                WaitingForFactoryConvolution = true;
                ScreenCompleted?.Invoke();
                return;
            }
            _switching = true;
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
            _switching = true;
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
            WaitingForFactoryConvolution = false;
        }

        private void HandleCompleted()
        {
            _completed = true;
        }

        private void OnDisable()
        {
            UnsubscribeStage();
        }
    }
}
