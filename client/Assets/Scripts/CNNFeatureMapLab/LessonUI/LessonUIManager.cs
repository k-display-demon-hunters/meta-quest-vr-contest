using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class LessonUIManager
    {
        [SerializeField] private CorridorRenderUtility _visuals;
        [SerializeField] private SubtitlePanelController _subtitle;
        [SerializeField] private HintBubblesController _hints;
        [SerializeField] private CompletionPopupController _completion;
        [SerializeField] private CorridorDebugController _debug;
        private Camera _camera;
        private bool _completed;
        internal event Action PreviousRequested;
        internal event Action NextRequested;
        internal void Initialize()
        {
            _visuals.InitializeRuntime();
            _subtitle.Bind(() => PreviousRequested?.Invoke(), () => NextRequested?.Invoke());
            _hints.Bind(_visuals);
            _completion.Bind();
        }

        internal void Enable()
        {
            _subtitle.ButtonPressed += RecordButton;
            _completion.ButtonPressed += RecordButton;
            _subtitle.Enable();
            _completion.Enable();
        }

        internal void Disable()
        {
            _subtitle.Disable();
            _completion.Disable();
            _subtitle.ButtonPressed -= RecordButton;
            _completion.ButtonPressed -= RecordButton;
            _debug.Hide();
        }

        internal void RecordButton(string caption)
        {
            _debug.RecordButton(caption);
        }

        internal void SetVisible(bool visible)
        {
            _subtitle.SetVisible(visible && !_completed);
            if (visible)
            {
                _camera = Camera.main;
                _subtitle.SetCamera(_camera);
                _completion.SetCamera(_camera);
            }
            else
            {
                _debug.Hide();
            }
        }

        internal void Tick(float now, float delta)
        {
            _hints.Tick(_camera, now);
            _subtitle.Track(_camera, delta);
            _completion.Tick(delta);
            _visuals.UpdateBillboards(_camera);
        }

        internal void OpenCompletion() => _completion.Open(_camera);
        internal void Show(LessonContext context)
        {
            _hints.Hide();
            _completed = context.Phase == LessonPhase.Complete;
            switch (context.Phase)
            {
                case LessonPhase.AwaitRound:
                _subtitle.Show("", LessonContent.WaitingBody(context.Layer, context.Map, context.Digit), context.CanGoBack, null);
                _hints.Show(LessonContent.WaitingHint(context.Layer, context.Map, context.Digit), context.HintPosition);
                break;
                case LessonPhase.Round:
                ShowRound(context);
                break;
                case LessonPhase.Recap:
                _subtitle.Show("", LessonContent.Recap(context.Layer, context.Map, context.Mandatory), true,
                    context.Layer == 4 ? LessonContent.Complete : LessonContent.Next);
                break;
                case LessonPhase.Complete:
                _subtitle.SetVisible(false);
                break;
            }
        }

        private void ShowRound(LessonContext context)
        {
            if (context.Layer == 2)
            {
                _subtitle.Show(LessonContent.RoundHeader(2), LessonContent.PoolingBody(context.Step, context.Map, context.Pooling),
                    true, context.Step == LessonStep.PoolingMaximum ? "전체 적용 ▶" : LessonContent.Next);
                return;
            }

            if (context.WaitingForFilter)
            {
                _hints.Show(LessonContent.FilterHint, context.HintPosition);
            }
            string next = context.WaitingForFilter || context.FilterPending
                ? null
                : context.IsLastStep ? LessonContent.Understood : LessonContent.Next;
            _subtitle.Show(LessonContent.RoundHeader(context.Layer), context.FilterPending ? "" : RoundBody(context), true, next);
        }

        private static string RoundBody(LessonContext context)
        {
            var data = context.Calculation;
            switch (context.Step)
            {
                case LessonStep.FilterIntro: return LessonContent.FilterIntro(data.Layer, data.Map, data.Digit);
                case LessonStep.FilterOpened: return LessonContent.FilterOpen(data.Layer, data.Count, data.Size);
                case LessonStep.Scan: return LessonContent.Scan(data.Layer);
                case LessonStep.Explain: return LessonContent.Explain(data.Layer, data.Digit);
                case LessonStep.Merge: return LessonContent.Merge(data.Layer, data.Map, data.Digit, data.Count, context.Winner);
                case LessonStep.CompareTemplates: return LessonContent.MatchTemplates(data.Count);
                case LessonStep.FillProbabilities: return LessonContent.ShareLight(context.Winner, context.WinnerPercent);
                default: return LessonContent.FlyAnswer(context.Winner);
            }
        }

        internal void AddRecap(RecapInfo recap)
        {
            var names = new string[recap.SourceCount];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = recap.Layer == 1 ? FeatureMapLabels.InputChannels[i] : FeatureMapLabels.MapId(recap.Layer-1, i);
            }
            _hints.AddRecap(recap.Position, names, recap.Layer == 4 ? LessonContent.OutputScore(recap.Digit) : FeatureMapLabels.MapId(recap.Layer, recap.Map), recap.Layer == 1 ? CorridorTheme.Rgb : CorridorTheme.Pairs);
        }

        internal void ShowDebug(string state, string hover, string cell, string click) => _debug.Show(state, hover, cell, click, _camera, _visuals.DebugFont);
        internal void DrawDebug() => _debug.Draw();
        internal void Dispose() => _debug.Dispose();
    }
}
