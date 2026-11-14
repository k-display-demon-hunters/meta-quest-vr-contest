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
        }

        internal void Enable()
        {
            _subtitle.ButtonPressed += RecordButton;
            _subtitle.Enable();
        }

        internal void Disable()
        {
            _subtitle.Disable();
            _subtitle.ButtonPressed -= RecordButton;
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
            }
            else
            {
                _debug.Hide();
            }
        }

        internal void Tick(float now) => _hints.Tick(now);

        internal void Show(LessonContext context)
        {
            _hints.Hide();
            _completed = context.Phase == LessonPhase.Complete;
            bool firstConv = context.Layer == 1;
            _hints.SetRecapVisibility(_completed ? -1 : context.Layer);
            if (!_completed)
            {
                float scale = context.Layer switch
                {
                    2 when context.Phase == LessonPhase.Round => 1.12f,
                    3 when context.OfferAdditionalFilters => 1.3f,
                    4 => 1.4f,
                    _ => 1f
                };
                _subtitle.SetPose(context.LessonPanelPose, scale);
            }
            switch (context.Phase)
            {
                case LessonPhase.AwaitInput:
                _subtitle.Show("", LessonContent.InputPictureBody, false, null);
                break;
                case LessonPhase.AwaitRound:
                if (context.ExploringAdditionalFilters)
                {
                    _subtitle.Show("", LessonContent.AdditionalFiltersBody, false, LessonContent.GoToVerdict);
                }
                else
                {
                    string header = context.Layer == 2 ? FeatureMapLabels.Operation(2, context.Map)
                        : FeatureMapLabels.MapId(context.Layer, context.Map);
                    _subtitle.Show(header, LessonContent.WaitingBody(context.Layer, context.Map),
                        context.CanGoBack, null, emphasizeHeading: firstConv);
                }
                break;
                case LessonPhase.Round:
                ShowRound(context);
                break;
                case LessonPhase.Recap:
                if (context.OfferAdditionalFilters)
                {
                    _subtitle.Show("", LessonContent.AdditionalFiltersChoice, true,
                        LessonContent.GoToVerdict, LessonContent.TryAdditionalFilters);
                    break;
                }
                string recap = LessonContent.Recap(context.Layer, context.Map, context.Mandatory);
                _subtitle.Show(firstConv ? FeatureMapLabels.MapId(1, context.Map) : "",
                    recap, true,
                    context.Layer == 4 ? LessonContent.Complete : LessonContent.Next,
                    context.Layer == 4 ? LessonContent.ViewScore : LessonContent.Previous);
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
                _subtitle.Show(FeatureMapLabels.Operation(2, context.Map),
                    LessonContent.PoolingBody(context.Step),
                    true, context.Step switch
                    {
                        LessonStep.PoolingMaximum => "채널 전체 적용 ▶",
                        LessonStep.PoolingApply => "채널 완료",
                        _ => LessonContent.Next
                    });
                return;
            }

            if (context.WaitingForFilter)
            {
                _hints.Show(LessonContent.FilterHint, context.HintPosition);
            }
            string next = context.WaitingForFilter || context.FilterPending
                ? null
                : context.IsLastStep
                    ? (context.ShowingOutputDetails ? LessonContent.BackToVerdict : LessonContent.Understood)
                    : LessonContent.Next;
            bool mapCard = context.Layer == 1 || (context.Layer == 3 && context.Map < 2);
            if (mapCard && next != null)
            {
                next = context.Step switch
                {
                    LessonStep.FilterOpened => "필터 적용",
                    LessonStep.Scan => LessonContent.Next,
                    _ => "확인"
                };
            }
            string previous = context.ShowingOutputDetails && context.WaitingForFilter
                ? LessonContent.BackToVerdict : LessonContent.Previous;
            string body = context.FilterPending && !mapCard ? "" : RoundBody(context);
            string header = mapCard ? FeatureMapLabels.MapId(context.Layer, context.Map)
                : LessonContent.RoundHeader(context.Layer);
            _subtitle.Show(header, body, true, next, previous,
                separateHeader: mapCard && context.Layer == 3,
                enlargeButtons: context.Layer == 1 && (context.Step is LessonStep.FilterOpened or LessonStep.Scan));
        }

        private static string RoundBody(LessonContext context)
        {
            var data = context.Calculation;
            switch (context.Step)
            {
                case LessonStep.FilterIntro: return LessonContent.FilterIntro(data.Layer, data.Map, data.Digit);
                case LessonStep.FilterOpened: return LessonContent.FilterOpen(data.Layer, data.Map, data.Count, data.Size);
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
            _hints.AddRecap(recap.Layer, recap.Position, names,
                recap.Layer == 4 ? LessonContent.OutputScore(recap.Digit) : FeatureMapLabels.MapId(recap.Layer, recap.Map),
                recap.Layer == 1 ? CorridorTheme.Rgb : CorridorTheme.Pairs);
        }

        internal void ShowDebug(string state, string hover, string cell, string click) => _debug.Show(state, hover, cell, click, _camera, _visuals.DebugFont);
        internal void DrawDebug() => _debug.Draw();
        internal void Dispose() => _debug.Dispose();
    }
}
