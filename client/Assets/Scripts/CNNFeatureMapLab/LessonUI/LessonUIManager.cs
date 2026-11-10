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
            _subtitle.SetVisible(visible);
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
            switch (context.Phase)
            {
                case LessonPhase.Pooling:
                _subtitle.Show(LessonContent.RoundHeader(2), LessonContent.PoolingBody(context.Stage, context.Map, context.Pooling),
                    true, context.Stage == 2 ? "전체 적용 ▶" : LessonContent.Next);
                break;
                case LessonPhase.AwaitRound:
                _subtitle.Show("", LessonContent.WaitingBody(context.Layer, context.Map, context.Digit), context.CanGoBack, null);
                _hints.Show(LessonContent.WaitingHint(context.Layer, context.Map, context.Digit), context.HintPosition);
                break;
                case LessonPhase.Round:
                if (context.WaitingForFilter)
                {
                    _hints.Show(LessonContent.FilterHint, context.HintPosition);
                }
                string next = context.WaitingForFilter || context.FilterPending ? null : context.Stage == (context.Layer == 4 ? 8 : 5) ? LessonContent.Understood : LessonContent.Next;
                _subtitle.Show(LessonContent.RoundHeader(context.Layer), context.FilterPending ? "" : RoundBody(context), true, next);
                break;
                case LessonPhase.Recap:
                _subtitle.Show("", LessonContent.Recap(context.Layer, context.Map, context.Mandatory), true, LessonContent.Next);
                break;
                case LessonPhase.AwaitOff:
                _hints.Show(LessonContent.SwitchHint, context.HintPosition);
                _subtitle.Show("", LessonContent.SwitchBody, true, null);
                break;
                case LessonPhase.OffResult:
                _subtitle.Show("", LessonContent.FirstOff(context.Switch.Layer, context.Switch.Map), true, LessonContent.Next);
                break;
                case LessonPhase.AwaitOn:
                _hints.Show(LessonContent.RestoreSwitchHint, context.HintPosition);
                _subtitle.Show("", LessonContent.RestoreSwitchBody, true, null);
                break;
                case LessonPhase.OnResult:
                _subtitle.Show("", LessonContent.RestoredSwitch, true, LessonContent.Next);
                break;
                case LessonPhase.Complete:
                _subtitle.Show("", LessonContent.CompleteBody, false, null);
                _hints.Show(LessonContent.DoorOpenHint, context.HintPosition);
                break;
            }
        }

        private static string RoundBody(LessonContext context)
        {
            var data = context.Calculation;
            switch (context.Stage)
            {
                case 0: return LessonContent.FilterIntro(data.Layer, data.Map, data.Digit);
                case 1: return LessonContent.FilterOpen(data.Layer, data.Count, data.Size);
                case 2: return LessonContent.Scan(data.Layer);
                case 3: return LessonContent.Explain(data.Layer, data.Digit);
                case 4: return LessonContent.Compare(data.Layer, data.Terms, data.Pre, data.Bias, data.Disabled);
                case 5: return LessonContent.Merge(data.Layer, data.Map, data.Digit, data.Count, context.Winner);
                case 6: return LessonContent.MatchTemplates(data.Count);
                case 7: return LessonContent.ShareLight(context.Winner, context.WinnerPercent);
                default: return LessonContent.FlyAnswer(context.Winner);
            }
        }

        internal void AddRecaps(System.Collections.Generic.IReadOnlyList<RecapInfo> recaps)
        {
            foreach (var recap in recaps)
            {
                var names = new string[recap.SourceCount];
                for (int i = 0;i<names.Length;i++)
                {
                    names[i] = recap.Layer == 1 ? FeatureMapLabels.InputChannels[i] : FeatureMapLabels.MapId(recap.Layer-1, i);
                }
                _hints.AddRecap(recap.Position, names, recap.Layer == 4 ? LessonContent.OutputScore(recap.Digit) : FeatureMapLabels.MapId(recap.Layer, recap.Map), recap.Layer == 1 ? CorridorTheme.Rgb : CorridorTheme.Pairs);
            }
        }

        internal void ShowDebug(string state, string hover, string cell, string click) => _debug.Show(state, hover, cell, click, _camera, _visuals.DebugFont);
        internal void DrawDebug() => _debug.Draw();
        internal void Dispose() => _debug.Dispose();
    }
}
