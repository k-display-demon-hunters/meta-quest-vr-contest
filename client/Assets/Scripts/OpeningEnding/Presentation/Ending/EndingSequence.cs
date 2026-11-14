using System;
using UnityEngine;

namespace Cinematic
{
    internal sealed class EndingSequence
    {
        private const float Release = .15f;
        private const float Flight = .7f;
        private const float PhotoFade = .4f;
        private const float SweepStart = .65f;
        private const float SweepLength = 1.8f;
        // Starts during the outgoing cut so the 0.15 s lock finishes at the unchanged sweep cue.
        private const float BaseballLead = Release + Flight + EndingVisualController.FocusLockDuration - SweepStart;

        private readonly EndingSequenceData _schedule;
        private readonly EndingVisualController _view;
        private readonly CinematicArchiveController _archive;
        private readonly CinematicAudioController _audio;
        private readonly Action<Vector3> _recognize;
        private readonly Action<float, float> _pulse;
        private readonly float[] _montageStarts;
        private readonly Rect[] _crops, _focus;
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

            int count = schedule.Montage.Length;
            _montageStarts = new float[count];
            _crops = new Rect[count];
            _focus = new Rect[count];
            _view.PrepareTrail(count);
            double start = schedule.IntroDuration;
            for (int i = 0; i < count; i++)
            {
                _montageStarts[i] = (float)start;
                _crops[i] = _view.FrameArchive(schedule.Montage[i], out _focus[i]);
                EndingSequenceData.MontageEntry entry = schedule.Montage[i];
                _view.SetTrailPhoto(i, entry.Clip.Still, ZoomedCrop(i, 1.04f), entry.Year);
                start += entry.Duration;
            }
            _montageEnd = (float)start;
        }

        internal float OutroStart => _montageEnd;
        private float QuietStart => OutroStart + (float)_schedule.OutroDuration;
        internal float Duration => QuietStart + (float)_schedule.QuietDuration;
        internal float ScoreGain(float time) => 1f - Mathf.SmoothStep(0f, 1f, (time - (QuietStart - 2.2f)) / 2.2f);

        internal void Evaluate(float time)
        {
            bool intro = time < (float)_schedule.IntroDuration;
            bool montage = !intro && time < OutroStart;
            bool outro = time >= OutroStart && time < QuietStart;
            float outroElapsed = time - OutroStart;

            if (Crossed((float)_schedule.IntroDuration, time)) { _audio.Voice(null); }
            if (Crossed(QuietStart, time))
            {
                _audio.StopAll();
                _view.SetQuiet(true);
            }

            _view.SetMontageVisible(montage);
            _view.ShowModel(intro, time);
            _view.EvaluateTrail(time, _montageStarts, OutroStart, QuietStart);

            if (montage)
            {
                int era = 0;
                while (era + 1 < _montageStarts.Length && time >= _montageStarts[era + 1]) { era++; }
                ShowPhoto(era, time - _montageStarts[era]);
                ShowMatchedFrame(era, time);
            }
            else if (intro)
            {
                _view.HideYear();
                if (time >= _montageStarts[0] - Release) { ShowMatchedFrame(0, time); }
                else { _view.ShowModelFocus(time); }
            }
            else if (outro)
            {
                int last = _montageStarts.Length - 1;
                _view.ShowYear(_schedule.Montage[last].Year, _schedule.Montage[last].Year,
                    1f, 1f - Mathf.Clamp01(outroElapsed / .4f));
                _view.HideArchiveFocus();
            }
            else
            {
                _view.HideYear();
                _view.HideArchiveFocus();
            }

            if (Crossed(0f, time))
            {
                _audio.Hit(true);
                _pulse(.5f, .12f);
            }
            _previous = time;
        }

        private void ShowPhoto(int era, float elapsed)
        {
            EndingSequenceData.MontageEntry entry = _schedule.Montage[era];
            if (era != _era)
            {
                _era = era;
                _archive.Show(entry.Clip);
                _archive.SetTone(0, 1);
                if (era > 0)
                {
                    _archive.SetPreviousPhoto(_schedule.Montage[era - 1].Clip.Still, ZoomedCrop(era - 1, 1.04f));
                }
            }

            float zoom = PhotoZoom(era, elapsed);
            Rect crop = ZoomedCrop(era, zoom);
            _archive.SetFraming(crop);
            _archive.SetPhotoBlend(Mathf.SmoothStep(0f, 1f, elapsed / PhotoFade), era > 0);

            if (entry.ShowUpscaleComparison)
            {
                float sweep = Mathf.SmoothStep(0f, 1f, (elapsed - SweepStart) / SweepLength);
                _archive.SetUpscaleSweep(Mathf.Lerp(-.02f, 1.02f, sweep));
                _archive.SetUpscaleFraming(_crops[era]);
            }

            _view.ShowYear(era == 0 ? null : _schedule.Montage[era - 1].Year, entry.Year, elapsed, 1f);
        }

        private void ShowMatchedFrame(int era, float time)
        {
            int next = era + 1;
            if (next < _montageStarts.Length && time >= MoveStart(next)) { era = next; }

            float elapsed = time - _montageStarts[era];
            bool baseball = _schedule.Montage[era].ShowUpscaleComparison;
            if (baseball && elapsed >= SweepStart && elapsed < SweepStart + SweepLength)
            {
                _view.HideArchiveFocus();
                return;
            }

            float moving = time - MoveStart(era);
            var target = PhotoTarget(era, elapsed);
            if (moving < Release)
            {
                var source = era == 0 ? _view.ModelTarget : PhotoTarget(era - 1, time - _montageStarts[era - 1]);
                _view.ShowReleasedFocus(source, moving / Release);
            }
            else if (moving < Release + Flight)
            {
                var source = era == 0 ? _view.ModelTarget : PhotoTarget(era - 1, MoveStart(era) + Release - _montageStarts[era - 1]);
                _view.ShowFlyingFocus(source, target, (moving - Release) / Flight);
            }
            else
            {
                float lockTime = MoveStart(era) + Release + Flight;
                _view.ShowLockedFocus(target, time - lockTime);
                if (Crossed(lockTime, time)) { _recognize(_view.FocusPosition); }
            }
        }


        private float MoveStart(int era) =>
            _montageStarts[era] - (_schedule.Montage[era].ShowUpscaleComparison ? BaseballLead : Release);

        private EndingFocusTarget PhotoTarget(int era, float elapsed) =>
            _view.ArchiveTarget(_focus[era], PhotoZoom(era, elapsed));

        private float PhotoZoom(int era, float elapsed) =>
            Mathf.Lerp(1f, 1.04f, Mathf.Clamp01(elapsed / (float)_schedule.Montage[era].Duration));

        private Rect ZoomedCrop(int era, float zoom)
        {
            Rect crop = _crops[era];
            Vector2 size = crop.size / zoom;
            return new Rect(crop.center - size * .5f, size);
        }

        private bool Crossed(float cue, float time) => _previous < cue && time >= cue;
    }
}
