using UnityEngine;

namespace FactoryEnvironment
{
    // The panel and collider travel together into the housing above the doorway.
    [DefaultExecutionOrder(100)]
    public sealed class FactoryShutterController : MonoBehaviour
    {
        [SerializeField] private FactoryShutterId _id;
        [SerializeField] private Transform _panel;
        [SerializeField] private float _travel = 3.3f;
        [SerializeField] private float _duration = 1.6f;
        private Vector3 _closedPosition;
        private float _amount;
        private bool _open;
        private bool _isNear;

        public FactoryShutterId Id => _id;

        private void Awake()
        {
            _closedPosition = _panel.localPosition;
        }

        public void Open() => _open = true;
        public void Close() => _open = false;
        public void SetNear(bool isNear) => _isNear = isNear;

        // Run after the area controller's LateUpdate so closing uses this frame's proximity.
        private void LateUpdate()
        {
            float target = _open ? 1f : 0f;
            if (_amount == target) { return; }
            if (!_open && _isNear) { return; }
            _amount = Mathf.MoveTowards(_amount, target, Time.deltaTime / _duration);
            _panel.localPosition = _closedPosition + Vector3.up * (_travel * Mathf.SmoothStep(0f, 1f, _amount));
        }
    }
}
