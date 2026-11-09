using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class CinematicWorldController : MonoBehaviour
    {
        [SerializeField] private Transform _world, _room, _portal, _timeline, _envelope, _digit, _factory, _leftDoor, _rightDoor, _network;
        [SerializeField] private Transform[] _letters, _boxes, _pixels, _eraProps, _networkLayers, _fragments;
        [SerializeField] private Transform _digitBox;
        [SerializeField] private Text _year, _caption, _networkNote;
        [SerializeField] private Text[] _layerLabels;
        [SerializeField] private Image _flash;
        [SerializeField] private CinematicArchiveController _archive;
        private CinematicText _content;
        private bool _ending;
        private int _era = -1;
        private int _recognized;
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

        public void Evaluate(float time)
        {
            if (_ending) { Ending(time); } else { Opening(time); }
            _last = time;
        }

        private void Opening(float t)
        {
            _room.gameObject.SetActive(t >= 7f && t < 15f);
            _portal.gameObject.SetActive(t >= 5.2f && t < 15f);
            _timeline.gameObject.SetActive(t >= 3.2f && t < 7f);
            _year.gameObject.SetActive(t >= 3.2f && t < 12f);
            _caption.gameObject.SetActive(t >= 7f && t < 12f);
            _factory.gameObject.SetActive(t >= 17.2f);
            _network.gameObject.SetActive(false);
            foreach (Transform prop in _eraProps) { prop.gameObject.SetActive(false); }
            if (t >= 3.2f && t < 4.5f)
            {
                float e = t - 3.2f;
                _year.text = e < .3f ? "2026".Substring(0, Mathf.Clamp((int)(e / .055f) + 1, 0, 4))
                    : e < .65f ? "2026" : e < .9f ? "2026".Substring(0, Mathf.Clamp(4 - (int)((e - .65f) / .06f), 0, 4))
                    : "1989".Substring(0, Mathf.Clamp((int)((e - .9f) / .055f), 0, 4));
            }
            else { _year.text = "1989"; }
            _year.transform.parent.localPosition = Vector3.Lerp(new Vector3(0, 1.9f, 3), new Vector3(0, 3.2f, 4.5f), Mathf.Clamp01((t - 5.2f) / .6f));
            _timeline.localPosition = new Vector3(Mathf.Lerp(0, 12, Mathf.Clamp01((t - 3.2f) / 3.5f)), .28f, 3.3f);
            float expand = Smooth((t - 7f) / .6f);
            _portal.localPosition = Vector3.Lerp(new Vector3(0, 1.85f, 3), new Vector3(0, 2.5f, 6), expand);
            _portal.localScale = Vector3.Lerp(new Vector3(1.6f, 1.067f, 1), new Vector3(10, 6.667f, 1), expand);
            _archive.SetTone(1 - Smooth((t - 8f) / 1.6f), t < 12f ? 1 : Mathf.Lerp(1, .35f, Smooth((t - 12f) / .5f)));
            _flash.color = new Color(1, .97f, .88f, t >= 7f && t < 7.22f ? .62f * (1 - (t - 7) / .22f) : 0);
            _envelope.gameObject.SetActive(t >= 12f && t < 13.6f);
            _envelope.localPosition = Vector3.Lerp(new Vector3(.3f, 1.5f, 5.8f), new Vector3(0, 1.4f, 1.1f), Smooth((t - 12f) / .6f));
            _envelope.localScale = Vector3.one * (1 - Smooth((t - 13.0f) / .6f));
            _digit.gameObject.SetActive(t >= 12.6f && t < 14.6f);
            _digitBox.gameObject.SetActive(t >= 13.6f && t < 14.6f);
            _digit.localScale = Vector3.one * Mathf.Lerp(.16f, 1, Smooth((t - 12.6f) / 1.4f));
            for (int i = 0; i < _letters.Length; i++)
            {
                _letters[i].gameObject.SetActive(t >= 7f && t < 12f);
                _boxes[i].gameObject.SetActive(t >= 7.6f + i * .42f && t < 12f);
                float age = t - (7.6f + i * .42f);
                _boxes[i].localScale = Vector3.one * Mathf.Lerp(1.35f, 1, Smooth(age / .35f));
                if (i >= _recognized && age >= 0 && t < 12f) { _recognized = i + 1; Recognized?.Invoke(_letters[i].position); }
            }
            AnimatePixels(t);
            for (int i = 0; i < _fragments.Length; i++)
            {
                _fragments[i].gameObject.SetActive(t >= 14.95f && t < 15.6f);
                float age = Mathf.Clamp01((t - 14.95f) / .65f);
                float angle = i * 2.39996f;
                float radius = 4 + age * 3;
                _fragments[i].localPosition = new Vector3(Mathf.Sin(angle) * radius, .2f + (i % 11) * .3f, Mathf.Cos(angle) * radius);
                _fragments[i].localScale = Vector3.one * .22f * (1 - age);
                _fragments[i].localRotation = Quaternion.Euler(age * i * 5, i * 31, age * 120);
            }
            _leftDoor.localPosition = new Vector3(-.7f - 1.4f * Smooth((t - 18f) / 1.4f), 1.5f, 0);
            _rightDoor.localPosition = new Vector3(.7f + 1.4f * Smooth((t - 18f) / 1.4f), 1.5f, 0);
            foreach (float cue in new[] { 3f, 12f, 15f, 17.2f })
            { if (Crossed(cue, t)) { Impact?.Invoke(cue == 17.2f); } }
            if (Crossed(7f, t)) { Impact?.Invoke(true); }
            if (Crossed(13.6f, t)) { Recognized?.Invoke(_digit.position); }
        }

        private void AnimatePixels(float t)
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i].gameObject.SetActive(t >= 14.6f && t < 18f);
                float angle = i * 2.39996f + (t - 15) * .18f;
                float radius = 1.2f + (i % 7) * .22f;
                Vector3 orbit = new Vector3(Mathf.Sin(angle) * radius, .45f + (i % 13) * .18f, Mathf.Cos(angle) * radius);
                Vector3 point = Vector3.Lerp(_pixelOrigins[i], orbit, Smooth((t - 15f) / 1.1f));
                _pixels[i].localPosition = Vector3.Lerp(point, new Vector3((i % 9 - 4) * .15f, 1.5f, 12), Smooth((t - 17.2f) / .7f));
                _pixels[i].localScale = Vector3.one * Mathf.Lerp(.085f, .025f, Smooth((t - 17.2f) / .7f));
            }
        }

        private void Ending(float t)
        {
            _room.gameObject.SetActive(false); _factory.gameObject.SetActive(false);
            _envelope.gameObject.SetActive(false); _digit.gameObject.SetActive(false); _timeline.gameObject.SetActive(false);
            foreach (Transform letter in _letters) { letter.gameObject.SetActive(false); }
            foreach (Transform pixel in _pixels) { pixel.gameObject.SetActive(false); }
            foreach (Transform fragment in _fragments) { fragment.gameObject.SetActive(false); }
            _flash.color = Color.clear;
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
            foreach (Transform target in new[] { _room, _portal, _timeline, _envelope, _digit, _factory, _network })
            { target.gameObject.SetActive(false); }
            _year.gameObject.SetActive(false); _caption.gameObject.SetActive(false); _flash.color = Color.clear;
            foreach (Transform target in _letters) { target.gameObject.SetActive(false); }
            foreach (Transform target in _boxes) { target.gameObject.SetActive(false); }
            foreach (Transform target in _pixels) { target.gameObject.SetActive(false); }
            foreach (Transform target in _eraProps) { target.gameObject.SetActive(false); }
            foreach (Transform target in _fragments) { target.gameObject.SetActive(false); }
        }
    }
}
