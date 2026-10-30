using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class CNNConvolutionLabManager : MonoBehaviour
    {
        [SerializeField] private GameObject _lessonRoot;
        [SerializeField] private Font _font;
        private PixelPatchData _patch;
        private LessonUIController _ui;
        private RgbVolumeController _spatial;
        private FilterAlignmentController _filter;
        private ResultObservationController _result;
        private ConvolutionQuizController _quiz;
        private ConvolutionCalculation _calculation;
        private bool _initialized, _calculating;
        private bool _factoryHosted;
        private readonly PixelUIInputController _input = new PixelUIInputController();
        public event Action ReturnRequested;
        public event Action Completed;

        private void OnEnable()
        {
            _input.Enable(transform);
            if (_initialized) { Subscribe(); }
        }
        private void Start()
        {
            _ui = _lessonRoot.AddComponent<LessonUIController>();
            _ui.Initialize(_font);
            _ui.SetReturnAvailable(!_factoryHosted);
            GameObject spatial = new GameObject("Spatial calculation");
            spatial.transform.SetParent(_lessonRoot.transform, false);
            _spatial = spatial.AddComponent<RgbVolumeController>();
            _spatial.Initialize(_font);
            _filter = spatial.AddComponent<FilterAlignmentController>();
            _filter.Initialize(_spatial, _ui);
            _result = _lessonRoot.AddComponent<ResultObservationController>();
            _result.Initialize(_spatial, _ui);
            _calculation = new ConvolutionCalculation(_patch ?? new PixelPatchData(new Color32[9]));
            _initialized = true;
            Subscribe();
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
            BeginCalculation();
        }
        public void SetInput(PixelPatchData patch) { _patch = patch; }
        public void SetFactoryHosted(bool hosted) { _factoryHosted = hosted; }
        private void Subscribe()
        {
            _ui.CloseRequested += HandleReturn;
            _ui.RestartRequested += BeginCalculation;
            _ui.AdvanceRequested += _result.Advance;
            _ui.CombineRequested += _result.Combine;
            _ui.AnswerRequested += HandleAnswer;
            _ui.SkipRequested += HandleSkip;
            _result.ReturnRequested += HandleReturn;
        }
        private void BeginCalculation()
        {
            StopAllCoroutines();
            _result.Close();
            _quiz?.Cancel();
            _quiz = null;
            _filter.StopInteraction();
            _spatial.ReleaseInteractions();
            _spatial.transform.SetParent(_lessonRoot.transform, false);
            _spatial.transform.localScale = Vector3.one * 500f;
            _spatial.transform.localRotation = Quaternion.Euler(0, -55, 0);
            _spatial.transform.localPosition = new Vector3(0, 0, -180);
            _spatial.SetData(_calculation);
            _ui.BeginCalculation();
            _lessonRoot.SetActive(true);
            _calculating = true;
            StartCoroutine(Run());
        }
        private IEnumerator Run()
        {
            yield return _filter.Run();
            yield return new RedCalculationController(_spatial, _ui, _calculation).Run();
            _quiz = new ConvolutionQuizController(_spatial, _ui, _calculation);
            yield return _quiz.Run();
            _quiz.Cancel();
            _spatial.ReleaseInteractions();
            _calculating = false;
            Completed?.Invoke();
            _result.Show(_calculation.ChannelSums());
        }
        private void Update()
        {
            if (_initialized && _calculating) { _ui.ShowSkip(_filter.HasPendingAction || _spatial.HasPendingAction, _quiz != null && _quiz.Waiting); }
        }
        private void HandleAnswer(int option) { _quiz?.Choose(option); }
        private void HandleSkip()
        {
            if (!_filter.CompletePendingAction() && !_spatial.CompletePendingAction()) { _quiz?.SkipCurrentStep(); }
        }
        private void HandleReturn() { ReturnRequested?.Invoke(); }
        private void OnDisable()
        {
            StopAllCoroutines();
            _input.Dispose();
            if (!_initialized) { return; }
            _ui.CloseRequested -= HandleReturn;
            _ui.RestartRequested -= BeginCalculation;
            _ui.AdvanceRequested -= _result.Advance;
            _ui.CombineRequested -= _result.Combine;
            _ui.AnswerRequested -= HandleAnswer;
            _ui.SkipRequested -= HandleSkip;
            _result.ReturnRequested -= HandleReturn;
            _quiz?.Cancel();
            _filter.StopInteraction();
            _result.Close();
            _lessonRoot.SetActive(false);
        }
    }
}
