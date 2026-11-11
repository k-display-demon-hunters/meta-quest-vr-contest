using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class CinematicWorldController : MonoBehaviour
    {
        [SerializeField] private Transform _world, _portal, _timeline, _digit, _factory, _leftDoor, _rightDoor, _network;
        [SerializeField] private Transform[] _boxes, _pixels, _eraProps, _networkLayers;
        [SerializeField] private Transform _digitBox;
        [SerializeField] private Text _year, _caption, _networkNote;
        [SerializeField] private Text[] _layerLabels;
        [SerializeField] private CinematicArchiveController _archive;
        private CinematicText _content;
        private bool _ending;
        private int _era = -1;
        private int _openingShot;
        private Vector3[] _pixelOrigins;
        public event Action<Vector3> Recognized;
        public event Action<bool> Impact;
        private float _last = -1;
        public static readonly float[] EraStarts = { 3f, 4.8f, 6.4f, 7.8f, 9.1f, 10.3f, 11.5f, 12.7f };

        public void Bind(CinematicText content, CinematicModelResult result, bool ending, Transform head)
        {
            _content = content; _ending = ending;
            // Move the stage around the viewer once; never move the camera or chase head rotation.
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            _world.position = new Vector3(head.position.x, head.position.y - 1.6f, head.position.z);
            _world.rotation = Quaternion.LookRotation(forward.sqrMagnitude > .01f ? forward : Vector3.forward);
            _pixelOrigins = new Vector3[_pixels.Length];
            for (int i = 0; i < _pixels.Length; i++) { _pixelOrigins[i] = _pixels[i].localPosition; }
            _archive.Show(content.PostOffice);
            _caption.text = content.Provenance;
            if (!ending)
            {
                _network.gameObject.SetActive(false);
                foreach (Transform item in _boxes) { item.gameObject.SetActive(false); }
                foreach (Transform item in _eraProps) { item.gameObject.SetActive(false); }
            }
            int count = result.Layers.Count;
            _networkNote.text = count == 0 ? content.Example : "";
            for (int i = 0; i < _networkLayers.Length; i++)
            {
                bool visible = i < (count == 0 ? 5 : count);
                _networkLayers[i].gameObject.SetActive(visible);
                _networkLayers[i].localPosition = new Vector3((i - ((count == 0 ? 5 : count) - 1) * .5f) * .22f, 0, 0);
                if (visible) { _layerLabels[i].text = count == 0 ? new[] { "INPUT", "CONV", "POOL", "DENSE", "OUTPUT" }[i] : result.Layers[i]; }
            }
        }

        public void PrepareOpeningMedia() => _archive.PrepareVideo(_content.PostOfficeStamp.Video);

        public void Evaluate(float time)
        {
            if (_ending) { Ending(time); } else { Opening(time); }
            _last = time;
        }

        private void Opening(float t)
        {
            bool showArchive = t >= OpeningTiming.Arrival && t < OpeningTiming.Digit;
            bool showLocation = t >= OpeningTiming.Arrival && t < OpeningTiming.Zoom;
            _portal.gameObject.SetActive(showArchive);
            _timeline.gameObject.SetActive(t >= OpeningTiming.Rewind && t < OpeningTiming.Arrival);
            _year.gameObject.SetActive(t >= OpeningTiming.Rewind && t < OpeningTiming.Arrival + .65f);
            _caption.gameObject.SetActive(showLocation);
            _factory.gameObject.SetActive(t >= OpeningTiming.Factory);

            float rewind = Mathf.InverseLerp(OpeningTiming.Rewind, OpeningTiming.Arrival, t);
            float yearsElapsed = 37 * (1 - Mathf.Pow(1 - rewind, 1.55f));
            _year.text = (2026 - Mathf.FloorToInt(yearsElapsed)).ToString();
            _timeline.localPosition = new Vector3(Mathf.Lerp(0, 12, rewind), .28f, 3.3f);
            _year.transform.parent.localPosition = new Vector3(0, 2.45f, 2.9f);

            if (showLocation)
            {
                string location = _content.OpeningLocation;
                int characters = Mathf.Min(location.Length, (int)((t - OpeningTiming.Arrival) * 20f) + 1);
                _caption.text = location.Substring(0, characters);
                if (characters == location.Length)
                {
                    _caption.text += "\n" + _content.OpeningResearch;
                }
            }

            if (showArchive) { EvaluateArchive(t); }
            else if (Crossed(OpeningTiming.Digit, t)) { _archive.Stop(); }

            _digit.gameObject.SetActive(t >= OpeningTiming.Digit && t < OpeningTiming.Pixels);
            _digitBox.gameObject.SetActive(t >= OpeningTiming.Digit && t < OpeningTiming.Pixels);
            float digitReveal = Smooth((t - OpeningTiming.Digit) / .65f);
            _digit.localScale = Vector3.one * Mathf.Lerp(.5f, 1, digitReveal);
            if (Crossed(OpeningTiming.Digit, t)) { Recognized?.Invoke(_digit.position); }

            AnimatePixels(t);
            float doorOpen = Smooth((t - OpeningTiming.Doors) / 1.4f);
            _leftDoor.localPosition = new Vector3(-.7f - 1.4f * doorOpen, 1.5f, 0);
            _rightDoor.localPosition = new Vector3(.7f + 1.4f * doorOpen, 1.5f, 0);
            if (Crossed(OpeningTiming.Factory, t)) { Impact?.Invoke(false); }
        }

        private void EvaluateArchive(float t)
        {
            int shot = t < OpeningTiming.StampShot ? 0 : t < OpeningTiming.Envelope ? 1 : 2;
            if (shot != _openingShot)
            {
                _openingShot = shot;
                _archive.Show(shot == 1 ? _content.PostOfficeStamp : _content.PostOfficeEnvelope);
            }

            float expand = Smooth((t - OpeningTiming.Arrival - .18f) / .65f);
            _portal.localPosition = Vector3.Lerp(new Vector3(0, 1.85f, 3), new Vector3(0, 2.2f, 4.6f), expand);
            _portal.localScale = Vector3.Lerp(new Vector3(1.6f, .9f, 1), new Vector3(6.8f, 3.825f, 1), expand);
            _archive.SetTone(shot == 0 ? .55f * (1 - expand) : 0, 1);
            if (shot == 0)
            {
                float zoom = Smooth(Mathf.InverseLerp(OpeningTiming.Zoom, OpeningTiming.StampShot, t));
                _archive.SetFraming(new Rect(.57f * zoom, .28f * zoom, 1 - .57f * zoom, 1 - .57f * zoom));
            }
            else if (shot == 2)
            {
                float zoom = Smooth(Mathf.InverseLerp(OpeningTiming.Digit - 1.15f, OpeningTiming.Digit, t));
                _archive.SetFraming(new Rect(.64f * zoom, .33f * zoom, 1 - .78f * zoom, 1 - .78f * zoom));
            }
        }

        private void AnimatePixels(float t)
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i].gameObject.SetActive(t >= OpeningTiming.Pixels && t < OpeningTiming.Doors);
                float angle = i * 2.39996f + (t - OpeningTiming.Pixels) * .18f;
                float radius = 1.2f + (i % 7) * .22f;
                Vector3 orbit = new Vector3(Mathf.Sin(angle) * radius, .45f + (i % 13) * .18f, Mathf.Cos(angle) * radius);
                Vector3 point = Vector3.Lerp(_pixelOrigins[i], orbit, Smooth((t - 24.2f) / 1.1f));
                float gather = Smooth((t - 29.8f) / 1.2f);
                _pixels[i].localPosition = Vector3.Lerp(point, new Vector3((i % 9 - 4) * .15f, 1.5f, 12), gather);
                _pixels[i].localScale = Vector3.one * Mathf.Lerp(.085f, .025f, gather);
            }
        }

        private void Ending(float t)
        {
            _factory.gameObject.SetActive(false);
            _digit.gameObject.SetActive(false); _timeline.gameObject.SetActive(false);
            foreach (Transform pixel in _pixels) { pixel.gameObject.SetActive(false); }
            bool montage = t >= 3f && t < 14f;
            _portal.gameObject.SetActive(montage);
            _year.gameObject.SetActive(montage);
            _caption.gameObject.SetActive(montage);
            if (montage)
            {
                int era = 0;
                for (int i = 0; i < EraStarts.Length; i++) { if (t >= EraStarts[i]) { era = i; } }
                if (era != _era)
                {
                    _era = era; _archive.Show(_content.Montage[era]); Impact?.Invoke(false);
                    _year.text = _content.Years[era]; _caption.text = _content.Labels[era];
                    Recognized?.Invoke(_portal.position);
                }
                float local = t - EraStarts[era];
                float stretch = Smooth(local / .3f);
                _portal.localPosition = new Vector3(0, 2.1f, Mathf.Lerp(3, 4.5f, stretch));
                _portal.localScale = new Vector3(Mathf.Lerp(.9f, 3.6f, stretch), Mathf.Lerp(1.2f, 4.8f, stretch), 1);
                _year.transform.parent.localPosition = new Vector3(-1.7f, 3.15f, 3.5f);
                _archive.SetTone(era == 0 ? .35f : 0, .8f);
            }
            for (int i = 0; i < _eraProps.Length; i++)
            {
                _eraProps[i].gameObject.SetActive(montage && i == _era);
                if (montage && i == _era)
                { _eraProps[i].localRotation = Quaternion.Euler(0, Mathf.Sin((t - EraStarts[i]) * 2) * 8, 0); }
            }
            for (int i = 0; i < _boxes.Length; i++)
            {
                bool visible = i < 8 && t >= 15 + i * .11f && t < 16.6f;
                _boxes[i].gameObject.SetActive(visible);
                float angle = i * Mathf.PI / 4;
                _boxes[i].localPosition = Vector3.Lerp(new Vector3(Mathf.Sin(angle) * 3, 1.7f, Mathf.Cos(angle) * 3),
                    new Vector3(0, 1.7f, 1.8f), Smooth((t - 15 - i * .11f) / .7f));
                _boxes[i].localScale = Vector3.one;
            }
            _network.gameObject.SetActive(t >= 16.6f && t < 18f);
            _network.localScale = Vector3.one * Mathf.Lerp(.7f, 1, Smooth((t - 16.6f) / .25f));
            if (Crossed(2.8f, t) || Crossed(18f, t)) { Impact?.Invoke(false); }
            if (Crossed(16.6f, t)) { Impact?.Invoke(true); }
        }

        private bool Crossed(float cue, float time) => _last < cue && time >= cue;
        private static float Smooth(float t) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
        public void Finish()
        {
            _archive.Stop();
            foreach (Transform target in new[] { _portal, _timeline, _digit, _factory, _network })
            { target.gameObject.SetActive(false); }
            _year.gameObject.SetActive(false); _caption.gameObject.SetActive(false);
            foreach (Transform target in _boxes) { target.gameObject.SetActive(false); }
            foreach (Transform target in _pixels) { target.gameObject.SetActive(false); }
            foreach (Transform target in _eraProps) { target.gameObject.SetActive(false); }
        }
    }
}
