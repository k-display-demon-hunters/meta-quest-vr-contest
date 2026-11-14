using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Shows the convolution at one filter position as a formula built in the air: each overlapped cell
    // becomes a "value × weight" term, and the terms add up to one output value.
    internal sealed class FormulaController
    {
        // The first terms move slowly so the pairing is readable; the rest follow quickly.
        private const int SlowTerms = 2;
        private const float SumFlightDuration = 0.9f;
        private readonly FormulaDisplayController _display;
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private readonly LessonPlayback _playback;

        public FormulaController(WorkbenchPrimitiveFactory factory, WorkbenchKernelController kernel,
            Transform anchor, LessonUIController ui, LessonNarrationController narration, LessonPlayback playback)
        {
            _display = new FormulaDisplayController(factory, kernel, anchor);
            _ui = ui;
            _narration = narration;
            _playback = playback;
        }

        public void Cancel() => _display.Clear();

        // Ends with only the sum left in the air, while the voice says where it goes.
        public IEnumerator Run(PictureConvolution convolution, int row, int column)
        {
            _display.Clear();
            _ui.Caption = "겹친 칸끼리 곱해서 모두 더합니다.";
            _narration.Play(NarrationCue.FormulaTerms);
            yield return _playback.Wait(0.8f);
            for (int index = 0; index < 9; index++)
            {
                bool slow = index < SlowTerms;
                _display.ShowTerm(index, convolution.Input(0, row, column, index), convolution.WeightLabel(0));
                yield return _playback.Wait(slow ? 0.5f : 0.1f);
                Vector3 start = _display.TermStart(index);
                Vector3 end = _display.TermSlot(index);
                float duration = slow ? 0.6f : 0.3f;
                for (float time = 0f; time < duration; time += _playback.DeltaTime)
                {
                    _display.MoveTerm(index, Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, time / duration)));
                    yield return null;
                }
                _display.MoveTerm(index, end);
                yield return _playback.Wait(slow ? 0.5f : 0.08f);
            }
            yield return _narration.WaitUntilDone();

            _display.ShowSum(convolution.Output(row, column));
            _ui.Caption = "더한 값이 특징 맵의 같은 자리에 들어갑니다.";
            _narration.Play(NarrationCue.OutputValue);
            yield return _playback.Wait(4.2f);
            _display.HideTerms();
        }

        // Carries the sum to its cell of the feature map (a workbench-local point).
        public IEnumerator SendSum(Vector3 destination)
        {
            Vector3 start = _display.SumSlot;
            for (float time = 0f; time < SumFlightDuration; time += _playback.DeltaTime)
            {
                _display.MoveSum(Vector3.Lerp(start, destination, Mathf.SmoothStep(0f, 1f, time / SumFlightDuration)));
                yield return null;
            }
            _display.Clear();
        }
    }
}
