using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Closing stage: shows in about half a minute that a filter detects a pattern, an output value
    // scores that pattern at one place, and the whole output is a map handed to the next stage.
    internal sealed class FeatureMeaningController
    {
        private const float StepDuration = 0.2f;
        private const float HandOffDuration = 0.9f;
        private readonly FeatureMeaningDisplayController _display;
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private readonly LessonPlayback _playback;

        public FeatureMeaningController(WorkbenchPrimitiveFactory factory, Transform anchor, LessonUIController ui,
            LessonNarrationController narration, LessonPlayback playback)
        {
            _display = new FeatureMeaningDisplayController(factory, anchor);
            _ui = ui;
            _narration = narration;
            _playback = playback;
        }

        // Call before the scene is shown so it appears in its starting state.
        public void Prepare() => _display.Prepare();

        public IEnumerator Run()
        {
            yield return Sweep(0, "필터는 특정 무늬를 찾는 탐지기예요.", NarrationCue.MeaningDetector);
            yield return Sweep(1, "출력값은 그 자리에 무늬가 얼마나 있는지 알려 주는 점수예요.", NarrationCue.MeaningScore);

            _ui.Caption = "출력판은 무늬의 위치를 담은 특징 맵이에요. 다음 단계에서 그림을 맞히는 데 쓰여요.";
            _narration.Play(NarrationCue.MeaningMap);
            yield return _playback.Wait(1.5f);
            for (float time = 0f; time < HandOffDuration; time += _playback.DeltaTime)
            {
                _display.SendMapsToNextStage(Mathf.SmoothStep(0f, 1f, time / HandOffDuration));
                yield return null;
            }
            _display.ShowVerdict();
        }

        private IEnumerator Sweep(int filter, string caption, NarrationCue cue)
        {
            _ui.Caption = caption;
            _narration.Play(cue);
            _display.ShowFilter(filter);
            yield return _playback.Wait(0.6f);
            int size = FeatureMeaningExample.MapSize;
            for (int index = 0; index < size * size; index++)
            {
                _display.MoveWindow(index / size, index % size);
                yield return _playback.Wait(StepDuration);
                _display.RaiseBar(filter, index, FeatureMeaningExample.Response(filter, index / size, index % size));
            }
            _display.HideWindow();
            yield return _narration.WaitUntilDone();
        }
    }
}
