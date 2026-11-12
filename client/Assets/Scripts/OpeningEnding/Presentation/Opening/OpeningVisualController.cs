using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class OpeningVisualController : MonoBehaviour
    {
        [SerializeField] private Transform _world, _portal, _timeline, _digit, _factory;
        [SerializeField] private Transform[] _boxes, _pixels, _eraProps;
        [SerializeField] private Transform _network;
        [SerializeField] private Text _year, _caption;
        [SerializeField] private Transform _leftDoor, _rightDoor, _digitBox;
        [SerializeField] private Text _numeral;
        private Vector3[] _pixelOrigins;

        internal Vector3 DigitPosition => _digit.position;

        internal void Bind(string provenance, Transform head)
        {
            // Move the stage around the viewer once; never move the camera or chase head rotation.
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            _world.position = new Vector3(head.position.x, head.position.y - 1.15f, head.position.z);
            _world.rotation = Quaternion.LookRotation(forward.sqrMagnitude > .01f ? forward : Vector3.forward);
            _pixelOrigins = new Vector3[_pixels.Length];
            for (int i = 0; i < _pixels.Length; i++) { _pixelOrigins[i] = _pixels[i].localPosition; }
            _caption.text = provenance;
            _network.gameObject.SetActive(false);
            foreach (Transform item in _boxes) { item.gameObject.SetActive(false); }
            foreach (Transform item in _eraProps) { item.gameObject.SetActive(false); }
        }

        internal void ShowArchive(bool visible) => _portal.gameObject.SetActive(visible);

        internal void ShowRewind(bool timeline, bool year, float rewind)
        {
            _timeline.gameObject.SetActive(timeline);
            _year.gameObject.SetActive(year);
            float yearsElapsed = 37 * (1 - Mathf.Pow(1 - rewind, 1.55f));
            _year.text = (2026 - Mathf.FloorToInt(yearsElapsed)).ToString();
            _timeline.localPosition = new Vector3(Mathf.Lerp(0, 12, rewind), 1f, 3.3f);
            _year.transform.parent.localPosition = new Vector3(0, 2.25f, 2.9f);
        }

        internal void ShowLocation(bool visible, float elapsed, string location, string research)
        {
            _caption.transform.parent.gameObject.SetActive(visible);
            if (!visible) { return; }
            int characters = Mathf.Min(location.Length, (int)(elapsed * 20f) + 1);
            _caption.text = location.Substring(0, characters);
            if (characters == location.Length) { _caption.text += "\n" + research; }
        }

        internal void ShowDigit(bool visible, float elapsed, float separation)
        {
            _digit.gameObject.SetActive(visible);
            _digitBox.gameObject.SetActive(visible);
            float digitReveal = Smooth(elapsed / .65f);
            _digit.localScale = Vector3.one * Mathf.Lerp(.5f, 1, digitReveal);
            Color digitColor = Color.Lerp(Color.white, new Color(.39f, .89f, .85f), separation);
            digitColor.a = 1f - separation;
            _numeral.color = digitColor;
        }

        internal void ShowFactory(bool visible, float elapsed)
        {
            _factory.gameObject.SetActive(visible);
            float doorOpen = Smooth(elapsed / 1.4f);
            _leftDoor.localPosition = new Vector3(-.8f - 1.65f * doorOpen, 1.3f, .12f);
            _rightDoor.localPosition = new Vector3(.8f + 1.65f * doorOpen, 1.3f, .12f);
        }

        internal void FrameArchive(float expansion)
        {
            _portal.localPosition = Vector3.Lerp(new Vector3(0, 1.85f, 3), new Vector3(0, 2.2f, 4.6f), expansion);
            _portal.localScale = Vector3.Lerp(new Vector3(1.6f, .9f, 1), new Vector3(6.8f, 3.825f, 1), expansion);
        }

        internal void AnimatePixels(bool visible, float transitionElapsed, float pixelElapsed, float orbitElapsed, float gatherElapsed)
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                Vector3 origin = _pixelOrigins[i];
                float rowDelay = .17f * Mathf.InverseLerp(.65f, -.65f, origin.y - _digit.localPosition.y);
                float separation = Smooth(Mathf.InverseLerp(rowDelay, .65f, transitionElapsed));
                _pixels[i].gameObject.SetActive(separation > 0f && visible);
                float angle = i * 2.39996f + pixelElapsed * .18f;
                float radius = 1.2f + (i % 7) * .22f;
                Vector3 orbit = new Vector3(Mathf.Sin(angle) * radius, .45f + (i % 13) * .18f, Mathf.Cos(angle) * radius);
                Vector3 assembled = Vector3.Lerp(_digit.localPosition, origin, Mathf.Lerp(.82f, 1f, separation));
                Vector3 point = Vector3.Lerp(assembled, orbit, Smooth(orbitElapsed / 1.1f));
                float gather = Smooth(gatherElapsed / 1.2f);
                _pixels[i].localPosition = Vector3.Lerp(point, new Vector3((i % 9 - 4) * .15f, 1.5f, 12), gather);
                _pixels[i].localScale = Vector3.one * (Mathf.Lerp(.085f, .025f, gather) * separation);
            }
        }

        private static float Smooth(float t) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
        internal void Finish()
        {
            foreach (Transform target in new[] { _portal, _timeline, _digit, _factory, _network })
            { target.gameObject.SetActive(false); }
            _year.gameObject.SetActive(false); _caption.transform.parent.gameObject.SetActive(false);
            foreach (Transform target in _boxes) { target.gameObject.SetActive(false); }
            foreach (Transform target in _pixels) { target.gameObject.SetActive(false); }
            foreach (Transform target in _eraProps) { target.gameObject.SetActive(false); }
        }
    }
}
