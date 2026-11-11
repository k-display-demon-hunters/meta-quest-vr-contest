using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class EndingVisualController : MonoBehaviour
    {
        [SerializeField] private Transform _world, _portal, _timeline, _digit, _factory;
        [SerializeField] private Transform[] _boxes, _pixels, _eraProps;
        [SerializeField] private Transform _network;
        [SerializeField] private Text _year, _caption;
        [SerializeField] private Transform[] _networkLayers;
        [SerializeField] private Text _networkNote;
        [SerializeField] private Text[] _layerLabels;
        private static readonly string[] ExampleLayers = { "INPUT", "CONV", "POOL", "DENSE", "OUTPUT" };

        internal Vector3 ArchivePosition => _portal.position;

        internal void Bind(CinematicModelResult result, string example, string provenance, Transform head)
        {
            // Move the stage around the viewer once; never move the camera or chase head rotation.
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            _world.position = new Vector3(head.position.x, head.position.y - 1.6f, head.position.z);
            _world.rotation = Quaternion.LookRotation(forward.sqrMagnitude > .01f ? forward : Vector3.forward);
            _caption.text = provenance;
            int count = result.Layers.Count;
            _networkNote.text = count == 0 ? example : "";
            for (int i = 0; i < _networkLayers.Length; i++)
            {
                bool visible = i < (count == 0 ? 5 : count);
                _networkLayers[i].gameObject.SetActive(visible);
                _networkLayers[i].localPosition = new Vector3((i - ((count == 0 ? 5 : count) - 1) * .5f) * .22f, 0, 0);
                if (visible) { _layerLabels[i].text = count == 0 ? ExampleLayers[i] : result.Layers[i]; }
            }
        }

        internal void SetMontageVisible(bool visible)
        {
            _factory.gameObject.SetActive(false);
            _digit.gameObject.SetActive(false);
            _timeline.gameObject.SetActive(false);
            foreach (Transform pixel in _pixels) { pixel.gameObject.SetActive(false); }
            _portal.gameObject.SetActive(visible);
            _year.gameObject.SetActive(visible);
            _caption.gameObject.SetActive(visible);
        }

        internal void FrameArchive(float elapsed)
        {
            float stretch = Smooth(elapsed / .3f);
            _portal.localPosition = new Vector3(0, 2.1f, Mathf.Lerp(3, 4.5f, stretch));
            _portal.localScale = new Vector3(Mathf.Lerp(.9f, 3.6f, stretch), Mathf.Lerp(1.2f, 4.8f, stretch), 1);
            _year.transform.parent.localPosition = new Vector3(-1.7f, 3.15f, 3.5f);
        }

        internal void ShowEraProp(int propIndex, float elapsed)
        {
            for (int i = 0; i < _eraProps.Length; i++)
            {
                bool showProp = i == propIndex;
                _eraProps[i].gameObject.SetActive(showProp);
                if (showProp)
                {
                    _eraProps[i].localRotation = Quaternion.Euler(0, Mathf.Sin(elapsed * 2) * 8, 0);
                }
            }
        }

        internal void SetLabels(string year, string label)
        {
            _year.text = year;
            _caption.text = label;
        }

        internal void ShowRecognition(bool beforeModel, float elapsed)
        {
            for (int i = 0; i < _boxes.Length; i++)
            {
                bool visible = i < 8 && elapsed >= i * .11f && beforeModel;
                _boxes[i].gameObject.SetActive(visible);
                float angle = i * Mathf.PI / 4;
                _boxes[i].localPosition = Vector3.Lerp(new Vector3(Mathf.Sin(angle) * 3, 1.7f, Mathf.Cos(angle) * 3),
                    new Vector3(0, 1.7f, 1.8f), Smooth((elapsed - i * .11f) / .7f));
                _boxes[i].localScale = Vector3.one;
            }
        }

        internal void ShowModel(bool visible, float elapsed)
        {
            _network.gameObject.SetActive(visible);
            _network.localScale = Vector3.one * Mathf.Lerp(.7f, 1, Smooth(elapsed / .25f));
        }

        private static float Smooth(float t) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
        internal void Finish()
        {
            foreach (Transform target in new[] { _portal, _timeline, _digit, _factory, _network })
            { target.gameObject.SetActive(false); }
            _year.gameObject.SetActive(false); _caption.gameObject.SetActive(false);
            foreach (Transform target in _boxes) { target.gameObject.SetActive(false); }
            foreach (Transform target in _pixels) { target.gameObject.SetActive(false); }
            foreach (Transform target in _eraProps) { target.gameObject.SetActive(false); }
        }
    }
}
