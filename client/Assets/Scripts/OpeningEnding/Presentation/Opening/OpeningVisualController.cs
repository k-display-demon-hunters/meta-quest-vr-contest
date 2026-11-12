using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class OpeningVisualController : MonoBehaviour
    {
        [SerializeField] private Transform _world, _portal, _timeline, _digit, _factory;
        [SerializeField] private Transform _postOffice;
        [SerializeField] private Transform _postalEnvelope, _postalStamp, _postmark, _postalOutline;
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
            _portal.gameObject.SetActive(false);
            foreach (Transform item in _boxes) { item.gameObject.SetActive(false); }
            foreach (Transform item in _eraProps) { item.gameObject.SetActive(false); }
        }

        internal void ShowPostOffice(bool visible) => _postOffice.gameObject.SetActive(visible);

        internal void ShowPostalProps(bool visible, float lift, float stampElapsed,
            float contactOffset, bool inspecting, float zoom)
        {
            _postalEnvelope.gameObject.SetActive(visible);
            if (!visible) { return; }

            Vector3 counterPosition = new Vector3(.25f, .73f, 1.85f);
            Vector3 inspectionPosition = new Vector3(0f, 1.30f, 1.65f);
            const float closeupScale = 4.5f;
            // The first ZIP digit meets the existing digit stage; the headset never moves.
            Vector3 digitAnchor = new Vector3(.04f, -.12f, -.018f);
            // Keep the envelope in front of the counter posts while matching the digit's viewing direction.
            const float envelopeDepth = 1.65f;
            Vector3 eyePosition = new Vector3(0f, 1.15f, 0f);
            Vector3 closeupDigitPosition = Vector3.Lerp(eyePosition, _digit.localPosition,
                envelopeDepth / _digit.localPosition.z);
            Vector3 closeupPosition = closeupDigitPosition - digitAnchor * closeupScale;
            _postalEnvelope.localPosition = Vector3.Lerp(
                Vector3.Lerp(counterPosition, inspectionPosition, lift), closeupPosition, zoom);
            _postalEnvelope.localRotation = Quaternion.Euler(
                Mathf.Lerp(Mathf.Lerp(90f, 20f, lift), 0f, zoom), 0f, 0f);
            _postalEnvelope.localScale = Vector3.one * Mathf.Lerp(Mathf.Lerp(.55f, 1.5f, lift), closeupScale, zoom);

            _postalStamp.gameObject.SetActive(!inspecting);
            float press = Smooth(Mathf.InverseLerp(contactOffset - .55f, contactOffset, stampElapsed));
            float release = Smooth(Mathf.InverseLerp(contactOffset + .12f, contactOffset + .7f, stampElapsed));
            _postalStamp.localPosition = new Vector3(-.28f, .10f,
                Mathf.Lerp(-.38f, -.036f, press * (1f - release)));
            _postalStamp.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-12f, 0f, press), 0f);
            _postmark.gameObject.SetActive(stampElapsed >= contactOffset);
            _postalOutline.gameObject.SetActive(inspecting);
        }

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
            foreach (Transform target in new[] { _portal, _postOffice, _postalEnvelope, _timeline, _digit, _factory, _network })
            { target.gameObject.SetActive(false); }
            _year.gameObject.SetActive(false); _caption.transform.parent.gameObject.SetActive(false);
            foreach (Transform target in _boxes) { target.gameObject.SetActive(false); }
            foreach (Transform target in _pixels) { target.gameObject.SetActive(false); }
            foreach (Transform target in _eraProps) { target.gameObject.SetActive(false); }
        }
    }
}
