using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNScreen
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public sealed class MagnifierIdleFloat : MonoBehaviour
    {
        public float rotateSpeed = 30f;
        [Min(0f)] public float bobAmplitude = 0.03f;
        [Min(0f)] public float bobFrequency = 0.5f;

        [SerializeField] private Transform _dock;
        [SerializeField] private ScreenLabUIController _ui;

        private XRGrabInteractable _grab;
        private Rigidbody _body;
        private bool _idle = true;
        private float _yaw;
        private float _phase;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _body = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            _grab.selectEntered.AddListener(StopFloating);
            _ui.ResetRequested += ResumeFloating;
        }

        private void OnDisable()
        {
            _grab.selectEntered.RemoveListener(StopFloating);
            _ui.ResetRequested -= ResumeFloating;
        }

        private void StopFloating(SelectEnterEventArgs args) => _idle = false;

        private void ResumeFloating()
        {
            // The existing reset is ignored while the filter is held.
            if (_grab.isSelected) return;

            _yaw = 0f;
            _phase = 0f;
            _idle = true;
        }

        private void FixedUpdate()
        {
            if (!_idle || _grab.isSelected) return;

            _yaw = Mathf.Repeat(_yaw + rotateSpeed * Time.fixedDeltaTime, 360f);
            _phase = Mathf.Repeat(_phase + bobFrequency * Time.fixedDeltaTime, 1f);
            float height = Mathf.Sin(_phase * (2f * Mathf.PI)) * bobAmplitude;
            _body.MovePosition(_dock.position + Vector3.up * height);
            _body.MoveRotation(Quaternion.AngleAxis(_yaw, Vector3.up) * _dock.rotation);
        }
    }
}
