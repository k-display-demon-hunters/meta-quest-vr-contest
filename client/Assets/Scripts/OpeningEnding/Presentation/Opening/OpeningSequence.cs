using System;
using UnityEngine;

namespace Cinematic
{
    internal sealed class OpeningSequence
    {
        private readonly CinematicText _content;
        private readonly OpeningSequenceData _schedule;
        private readonly OpeningVisualController _view;
        private readonly CinematicAudioController _audio;
        private readonly Action<Vector3> _recognize;
        private readonly int _runId;
        private float _previous = -1f;
        private string _section;

        internal OpeningSequence(CinematicText content, OpeningVisualController view,
            CinematicAudioController audio, Action<Vector3> recognize, int runId)
        {
            _content = content;
            _schedule = content.OpeningSchedule;
            _view = view;
            _audio = audio;
            _recognize = recognize;
            _runId = runId;
        }

        internal void Begin()
        {
            _audio.BeginOpening();
        }

        internal void PrepareStage() => EvaluateVisuals(0f);

        internal void Evaluate(float time)
        {
            LogSection(time);
            EvaluateAudio(time);
            EvaluateVisuals(time);
            _previous = time;
        }

        private void EvaluateVisuals(float time)
        {
            _view.ShowPostOffice(time >= _schedule.Arrival && time < _schedule.Digit, time - _schedule.Arrival);
            _view.ShowPostalProps(time >= _schedule.Arrival && time < _schedule.Digit,
                Smooth(Mathf.InverseLerp(_schedule.Zoom, _schedule.StampShot, time)),
                time - _schedule.StampShot, _schedule.StampContactOffset,
                time >= _schedule.Envelope,
                Smooth(Mathf.InverseLerp(_schedule.Envelope, _schedule.Digit, time)));
            _view.ShowRewind(time >= _schedule.Rewind && time < _schedule.Arrival,
                time >= _schedule.Rewind && time < _schedule.Arrival + .65f,
                Mathf.InverseLerp(_schedule.Rewind, _schedule.Arrival, time));
            _view.ShowLocation(time >= _schedule.Arrival && time < _schedule.Zoom,
                time - _schedule.Arrival, _content.OpeningLocation, _content.OpeningResearch);

            float transitionStart = _schedule.Pixels - .65f;
            _view.ShowDigit(time >= _schedule.Digit && time < _schedule.Pixels, time - _schedule.Digit,
                Smooth(Mathf.InverseLerp(transitionStart, _schedule.Pixels, time)));
            if (Crossed(_schedule.Digit, time)) { _recognize(_view.DigitPosition); }

            _view.AnimatePixels(time < _schedule.Doors, time - transitionStart, time - _schedule.Pixels,
                time - (_schedule.Pixels + _schedule.PixelOrbitOffset),
                time - (_schedule.Pixels + _schedule.PixelGatherOffset));
            _view.ShowFactory(time >= _schedule.Factory, time - _schedule.Doors);
            if (Crossed(_schedule.Factory, time)) { _audio.Hit(false); }
        }

        internal void GetCaptionLayout(float time, out float height, out bool foreground, out bool backing)
        {
            foreground = false;
            bool inDigitAndPixels = time >= _schedule.Digit && time < _schedule.Doors;
            bool inArchive = time >= _schedule.Arrival && time < _schedule.Digit;
            bool inFactory = time >= _schedule.Doors;
            backing = inDigitAndPixels || inArchive || inFactory;
            // Eye height is .75: captions sit only 25 cm below it, in a separate band from the props.
            height = inDigitAndPixels || inArchive || inFactory ? .50f : .75f;
        }

        private void EvaluateAudio(float time)
        {
            if (Crossed(_schedule.Rewind, time)) { _audio.StartRewind(); }
            if (Crossed(_schedule.Arrival, time)) { _audio.StartCrowd(); }
            if (Crossed(_schedule.StampShot + _schedule.StampContactOffset, time)) { _audio.PlayStamp(); }
            if (Crossed(_schedule.Pixels, time)) { _audio.StopCrowd(); }

            bool quietCue = (time >= _schedule.Rewind && time < _schedule.Arrival)
                || (time >= _schedule.StampShot && time < _schedule.Envelope);
            _audio.MixOpening(1f - Mathf.InverseLerp(_schedule.Digit, _schedule.Pixels, time), quietCue);
        }

        private void LogSection(float time)
        {
            string section;
            if (time >= _schedule.Doors) { section = "Doors"; }
            else if (time >= _schedule.Factory) { section = "Factory"; }
            else if (time >= _schedule.Pixels) { section = "Pixels"; }
            else if (time >= _schedule.Digit) { section = "Digit"; }
            else if (time >= _schedule.Envelope) { section = "Envelope"; }
            else if (time >= _schedule.StampShot) { section = "Stamp"; }
            else if (time >= _schedule.Zoom) { section = "PostOfficeCounter"; }
            else if (time >= _schedule.Arrival) { section = "PostOffice"; }
            else if (time >= _schedule.Rewind) { section = "Rewind"; }
            else { section = "Question"; }

            if (section == _section) { return; }
            _section = section;
            Debug.Log($"[Opening][run={_runId}] Section {section} elapsed={time:F2}s", _view);
        }

        private bool Crossed(float cue, float time) => _previous < cue && time >= cue;
        private static float Smooth(float value) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));
    }
}
