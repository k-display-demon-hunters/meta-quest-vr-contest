using System;
using UnityEngine;

namespace Cinematic
{
    internal sealed class OpeningSequence
    {
        private readonly CinematicText _content;
        private readonly OpeningSequenceData _schedule;
        private readonly OpeningVisualController _view;
        private readonly CinematicArchiveController _archive;
        private readonly CinematicAudioController _audio;
        private readonly Action<Vector3> _recognize;
        private readonly int _runId;
        private float _previous = -1f;
        private int _shot;
        private string _section;

        internal OpeningSequence(CinematicText content, OpeningVisualController view,
            CinematicArchiveController archive, CinematicAudioController audio, Action<Vector3> recognize, int runId)
        {
            _content = content;
            _schedule = content.OpeningSchedule;
            _view = view;
            _archive = archive;
            _audio = audio;
            _recognize = recognize;
            _runId = runId;
        }

        internal void Begin()
        {
            _archive.PrepareVideo(_content.PostOfficeStamp.Video);
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
            bool archiveVisible = time >= _schedule.Arrival && time < _schedule.Digit;
            _view.ShowArchive(archiveVisible);
            _view.ShowRewind(time >= _schedule.Rewind && time < _schedule.Arrival,
                time >= _schedule.Rewind && time < _schedule.Arrival + .65f,
                Mathf.InverseLerp(_schedule.Rewind, _schedule.Arrival, time));
            _view.ShowLocation(time >= _schedule.Arrival && time < _schedule.Zoom,
                time - _schedule.Arrival, _content.OpeningLocation, _content.OpeningResearch);

            if (archiveVisible) { EvaluateArchive(time); }
            else if (Crossed(_schedule.Digit, time)) { _archive.Stop(); }

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
            foreground = time >= _schedule.Digit && time < _schedule.Doors;
            bool inArchive = time >= _schedule.Arrival && time < _schedule.Digit;
            backing = foreground || inArchive;
            height = foreground ? 1.36f : inArchive ? (time >= _schedule.Zoom ? .85f : .99f) : 1.6f;
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

        private void EvaluateArchive(float time)
        {
            int shot = time < _schedule.StampShot ? 0 : time < _schedule.Envelope ? 1 : 2;
            if (shot != _shot)
            {
                _shot = shot;
                _archive.Show(shot == 1 ? _content.PostOfficeStamp : _content.PostOfficeEnvelope, frameVideo: shot == 1);
            }

            float expansion = Smooth((time - _schedule.Arrival - .18f) / .65f);
            _view.FrameArchive(expansion);
            _archive.SetTone(shot == 0 ? .55f * (1 - expansion) : 0, 1);
            _archive.SetPeopleMotion(shot == 0 ? time - _schedule.Arrival : 0f);
            if (shot == 0)
            {
                float zoom = Smooth(Mathf.InverseLerp(_schedule.Zoom, _schedule.StampShot, time));
                _archive.SetFraming(new Rect(.57f * zoom, .28f * zoom, 1 - .57f * zoom, 1 - .57f * zoom));
            }
            else if (shot == 2)
            {
                float zoom = Smooth(Mathf.InverseLerp(_schedule.Digit - 1.15f, _schedule.Digit, time));
                Rect stampFraming = _content.PostOfficeStamp.UV;
                Vector2 offset = Vector2.Lerp(stampFraming.position, new Vector2(.64f, .33f), zoom);
                Vector2 size = Vector2.Lerp(stampFraming.size, Vector2.one * .22f, zoom);
                _archive.SetFraming(new Rect(offset, size));
            }
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
            else if (time >= _schedule.Zoom) { section = "WindowZoom"; }
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
