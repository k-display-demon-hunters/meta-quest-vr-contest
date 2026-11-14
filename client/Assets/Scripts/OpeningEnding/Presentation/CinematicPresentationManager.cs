using System;
using UnityEngine;

namespace Cinematic
{
    public sealed class CinematicPresentationManager : MonoBehaviour
    {
        [SerializeField] private OpeningVisualController _openingView;
        [SerializeField] private EndingVisualController _endingView;
        [SerializeField] private CinematicArchiveController _archive;
        [SerializeField] private CinematicAudioController _audio;
        [SerializeField] private CinematicTypeController _type;

        private OpeningSequence _opening;
        private EndingSequence _ending;
        private (CinematicText.Line Line, float Start, float End)[] _lines;
        private CinematicText.Line _activeLine;
        private float _duration;
        private float _elapsed;

        internal float Elapsed => _elapsed;
        internal event Action OnPlaybackCompleted;
        internal event Action<float, float> OnHapticRequested;

        private void OnEnable() => _type.CharacterTyped += _audio.Type;
        private void OnDisable() => _type.CharacterTyped -= _audio.Type;

        internal void Bind(CinematicText content, CinematicModelResult model, bool ending, Transform head, int runId)
        {
            _archive.SetOpeningRun(ending ? (int?)null : runId);
            _audio.Bind(content, ending);
            if (ending) { _archive.Show(content.PostOffice); }
            _type.Bind();
            CinematicText.Line[] lines = ending ? content.Ending : content.Opening;
            _lines = new (CinematicText.Line, float, float)[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                CinematicText.Line line = lines[i];
                double origin = content.GetCaptionOrigin(line.Anchor);
                _lines[i] = (line, (float)(origin + line.Start), (float)(origin + line.End));
            }

            if (ending)
            {
                _duration = content.EndingSchedule.Duration;
                _endingView.Bind(model, content.Example, content.Provenance, head);
                _ending = new EndingSequence(content.EndingSchedule, _endingView, _archive, _audio, Recognize, Pulse);
                _endingView.Finish();
            }
            else
            {
                _duration = content.OpeningSchedule.Duration;
                _openingView.Bind(content.Provenance, head);
                _opening = new OpeningSequence(content, _openingView, _audio, Recognize, runId);
                _opening.PrepareStage();
            }
        }

        internal void Begin()
        {
            if (_opening != null) { _openingView.AlignToViewer(); }
            _opening?.Begin();
            if (_ending != null) { _audio.BeginEnding(); }
            Evaluate(0f);
        }

        internal void AlignOpeningToViewer() => _openingView.AlignToViewer();

        internal void Tick(float deltaTime)
        {
            _elapsed = Mathf.Min(_duration, _elapsed + deltaTime);
            Evaluate(_elapsed);
            if (_elapsed >= _duration) { OnPlaybackCompleted?.Invoke(); }
        }

        internal void Finish()
        {
            _audio.StopAll();
            _archive.Stop();
            if (_opening != null) { _openingView.Finish(); }
            else { _endingView.Finish(); }
            _type.ClearLine();
        }

        private void Evaluate(float time)
        {
            float height;
            bool foreground;
            bool backing;
            if (_opening != null)
            {
                _opening.Evaluate(time);
                _opening.GetCaptionLayout(time, out height, out foreground, out backing);
            }
            else
            {
                _ending.Evaluate(time);
                height = _ending.IsShowingModel(time) ? .95f : 1.6f;
                foreground = false;
                backing = false;
            }

            CinematicText.Line line = null;
            float elapsed = 0f;
            float remaining = 0f;
            foreach (var candidate in _lines)
            {
                if (time < candidate.Start || time >= candidate.End) { continue; }
                line = candidate.Line;
                elapsed = time - candidate.Start;
                remaining = candidate.End - time;
                break;
            }

            if (line != _activeLine)
            {
                _activeLine = line;
                if (line != null) { _audio.Voice(line.Voice); }
            }
            _type.Show(line, elapsed, remaining, height, foreground, backing);
        }

        private void Recognize(Vector3 location)
        {
            _audio.Beep(location);
            Pulse(.3f, .04f);
        }

        private void Pulse(float amplitude, float duration) => OnHapticRequested?.Invoke(amplitude, duration);
    }
}
