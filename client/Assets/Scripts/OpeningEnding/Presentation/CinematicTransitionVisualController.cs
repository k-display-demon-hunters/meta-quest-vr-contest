using System.Collections;
using UnityEngine;
namespace Cinematic
{
    public sealed class CinematicTransitionVisualController : MonoBehaviour
    {
        [SerializeField] private Material _coverMaterial;
        [SerializeField] private Transform _space, _cover;
        [SerializeField] private Transform[] _frames;
        private Transform _head;
        private Material _coverInstance;
        private float _time;
        private bool _running;
        internal void Bind(Transform head)
        {
            _head = head;
            _coverInstance = new Material(_coverMaterial);
            _cover.GetComponent<Renderer>().sharedMaterial = _coverInstance;
            SetCover(0);
            _space.gameObject.SetActive(false);
            _cover.gameObject.SetActive(false);
        }
        internal IEnumerator Enter()
        {
            _space.position = _head.position;
            Vector3 forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up);
            _space.rotation = Quaternion.LookRotation(forward.sqrMagnitude > .01f ? forward : Vector3.forward);
            _space.gameObject.SetActive(true);
            _cover.gameObject.SetActive(true);
            _running = true;
            float elapsed = 0;
            while (elapsed < 1f)
            {
                elapsed += Time.unscaledDeltaTime;
                _space.localScale = Vector3.one * Mathf.SmoothStep(.05f, 1f, Mathf.Clamp01(elapsed / .4f));
                SetCover(Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, 1f, elapsed)));
                yield return null;
            }
            SetCover(1);
        }
        internal IEnumerator Exit()
        {
            float elapsed = 0;
            while (elapsed < .6f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / .6f);
                SetCover(1 - t);
                _space.localScale = Vector3.one * Mathf.Lerp(1, 4, t);
                yield return null;
            }
            _running = false;
            _space.gameObject.SetActive(false);
            _cover.gameObject.SetActive(false);
        }
        private void LateUpdate()
        {
            if (!_running) { return; }
            _cover.position = _head.position;
            _time += Time.unscaledDeltaTime;
            for (int i = 0; i < _frames.Length; i++)
                _frames[i].localPosition = new Vector3(0, 0, 6f - Mathf.Repeat(_time * 2f + i * 1.4f, 5.6f));
        }
        private void SetCover(float alpha) => _coverInstance.SetColor("_Color", new Color(.018f, .045f, .065f, alpha));
        private void OnDestroy()
        {
            if (_coverInstance != null) { Destroy(_coverInstance); }
        }
    }
}
