using UnityEngine;

namespace FactoryEnvironment
{
    // The panel and collider travel together into the housing above the doorway.
    public sealed class FactoryShutterController : MonoBehaviour
    {
        [SerializeField] private Transform _panel;
        [SerializeField] private float _travel = 3.3f;
        [SerializeField] private float _duration = 1.6f;
        [SerializeField] private float _doorHalfWidth = 1.6f;
        private Transform _camera;
        private Vector3 _closedPosition;
        private float _amount;
        private bool _open;

        private void Awake() => _closedPosition = _panel.localPosition;
        public void Bind(Transform camera) => _camera = camera;
        public void Open() => _open = true;
        public void Close() => _open = false;

        private void Update()
        {
            float target = _open ? 1f : 0f;
            if (_amount == target) { return; }
            if (!_open)
            {
                Vector3 local = transform.InverseTransformPoint(_camera.position);
                // Wait if the player backs into the threshold while the panel is descending.
                if (Mathf.Abs(local.x) < _doorHalfWidth + .45f && Mathf.Abs(local.z) < .85f)
                {
                    return;
                }
            }
            _amount = Mathf.MoveTowards(_amount, target, Time.deltaTime / _duration);
            _panel.localPosition = _closedPosition + Vector3.up * (_travel * Mathf.SmoothStep(0f, 1f, _amount));
        }
    }
}
