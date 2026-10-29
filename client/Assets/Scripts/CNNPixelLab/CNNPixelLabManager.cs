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

        public bool ConvolutionCompleted => _completed && !_switching && !_ending;

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
            while (_switching)
            {
                yield return null;
            }
            UnbindStage();
            yield return _sceneLoader.UnloadStages();
        }

        private IEnumerator SwitchStage(bool calculation, bool restoreSelection)
        {
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
            _switching = true;
            StartCoroutine(SwitchStage(true, false));
        }

        private void HandleReturnRequested()
        {
            if (_switching || _ending) { return; }
            _switching = true;
            StartCoroutine(SwitchStage(false, true));
        }

        private void HandleResetRequested()
        {
            if (_switching || _ending) { return; }
            _patch = null;
            _completed = false;
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
