using System;
using System.Collections;
using UnityEngine;

namespace UI
{
    public sealed class CNNConvolutionLabManager : MonoBehaviour
    {
        [SerializeField] private GameObject _lessonRoot;
        [SerializeField] private Font _font;
        [SerializeField] private LessonUIController _ui;
        [SerializeField] private RgbVolumeController _spatial;
        private PixelPatchData _patch;
        private ConvolutionQuizController _quiz;
        private ConvolutionCalculation _calculation;
        private bool _initialized;
        private bool _completed;
        private bool _factoryHosted;
        private readonly PixelUIInputController _input = new PixelUIInputController();
        public event Action ReturnRequested;
        public event Action Completed;

        private void OnEnable()
        {
            _input.Enable(transform);
            _ui.AnswerRequested += HandleAnswer;
            _ui.SkipRequested += HandleAssist;
            _ui.AdvanceRequested += HandleReturn;
            _ui.CloseRequested += HandleReturn;
        }
        private void Start()
        {
            _lessonRoot.SetActive(true);
            _ui.Initialize(_font);
            // Leaving the B exercise is only possible after solving it.
            _ui.SetReturnAvailable(false);
            _spatial.Initialize(_font);
            _calculation = new ConvolutionCalculation(_patch ?? new PixelPatchData(new Color32[9]));
            _initialized = true;
            _spatial.SetData(_calculation);
            _ui.BeginCalculation();
            StartCoroutine(Run());
        }
        public void SetInput(PixelPatchData patch) { _patch = patch; }
        public void SetFactoryHosted(bool hosted) { _factoryHosted = hosted; }
        private IEnumerator Run()
        {
            _ui.Title = "B 퀴즈 · 배운 방법을 써 보세요";
            _ui.Explanation = "앞에서 쓴 연습용 값과 같은 가중치예요.\n이번에는 B의 합을 구해요.";
            _ui.Guide = "연습용 값 0~3\n\n× 1 → 그대로\n× 0 → 0";
            yield return _spatial.ArrangePractice();
            yield return _spatial.RevealWeights();
            yield return _ui.WaitForNext("B 퀴즈 시작");
            _quiz = new ConvolutionQuizController(_spatial, _ui, _calculation);
            yield return _quiz.Run();
            _quiz.Cancel();
            _spatial.ReleaseInteractions();
            _completed = true;
            _ui.ShowCompleted(_calculation.ChannelSums());
            _ui.SetCompletionDestination(_factoryHosted);
            Completed?.Invoke();
        }
        private void Update()
        {
            if (_initialized && !_completed) { _ui.ShowSkip(_spatial.HasPendingAction || _spatial.WaitingForKernel, false); }
        }
        private void HandleAnswer(int option) { _quiz?.Choose(option); }
        private void HandleAssist()
        {
            if (!_spatial.CompletePendingAction()) { _spatial.AlignKernel(); }
        }
        private void HandleReturn()
        {
            if (_completed) { ReturnRequested?.Invoke(); }
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            _input.Dispose();
            _ui.AnswerRequested -= HandleAnswer;
            _ui.SkipRequested -= HandleAssist;
            _ui.AdvanceRequested -= HandleReturn;
            _ui.CloseRequested -= HandleReturn;
            _quiz?.Cancel();
            if (_initialized) { _spatial.ReleaseInteractions(); }
        }
    }
}
