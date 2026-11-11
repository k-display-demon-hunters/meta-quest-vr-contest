using System;
using UnityEngine;

namespace Cinematic
{
    internal sealed class EndingSequence
    {
        private readonly EndingSequenceData _schedule;
        private readonly EndingVisualController _view;
        private readonly CinematicArchiveController _archive;
        private readonly CinematicAudioController _audio;
        private readonly Action<Vector3> _recognize;
        private readonly Action<float, float> _pulse;
        private readonly float[] _montageStarts;
        private readonly float _montageEnd;
        private float _previous = -1f;
        private int _era = -1;

        internal EndingSequence(EndingSequenceData schedule, EndingVisualController view, CinematicArchiveController archive,
            CinematicAudioController audio, Action<Vector3> recognize, Action<float, float> pulse)
        {
            _schedule = schedule;
            _view = view;
            _archive = archive;
            _audio = audio;
            _recognize = recognize;
            _pulse = pulse;

            _montageStarts = new float[schedule.Montage.Length];
            double start = schedule.MontageStart;
            for (int i = 0; i < _montageStarts.Length; i++)
            {
                _montageStarts[i] = (float)start;
                start += schedule.Montage[i].Duration;
            }
            _montageEnd = (float)start;
        }

        internal bool IsShowingModel(float time) => time >= (float)_schedule.ModelStart && time < (float)_schedule.ModelEnd;

        internal void Evaluate(float time)
        {
            if (Crossed(_montageEnd, time)) { _audio.StopAll(); }

            bool montage = time >= _schedule.MontageStart && time < _montageEnd;
            _view.SetMontageVisible(montage);
            int propIndex = -1;
            float elapsed = 0f;
            if (montage)
            {
                int era = 0;
                for (int i = 1; i < _montageStarts.Length && time >= _montageStarts[i]; i++) { era = i; }

                EndingSequenceData.MontageEntry entry = _schedule.Montage[era];
                if (era != _era)
                {
                    _era = era;
                    _archive.Show(entry.Clip);
                    _audio.Hit(false);
                    _view.SetLabels(entry.Year, entry.Label);
                    _recognize(_view.ArchivePosition);
                }
                elapsed = time - _montageStarts[era];
                propIndex = entry.PropIndex;
                _view.FrameArchive(elapsed);
                _archive.SetTone(era == 0 ? .35f : 0, .8f);
            }

            _view.ShowEraProp(propIndex, elapsed);
            _view.ShowRecognition(time < (float)_schedule.ModelStart, time - (float)_schedule.RecognitionStart);
            _view.ShowModel(IsShowingModel(time), time - (float)_schedule.ModelStart);

            if (Crossed(_schedule.IntroImpact, time) || Crossed((float)_schedule.ModelEnd, time)) { _audio.Hit(false); }
            if (Crossed((float)_schedule.ModelStart, time))
            {
                _audio.Hit(true);
                _pulse(.5f, .12f);
            }
            _previous = time;
        }

        private bool Crossed(float cue, float time) => _previous < cue && time >= cue;
    }
}
