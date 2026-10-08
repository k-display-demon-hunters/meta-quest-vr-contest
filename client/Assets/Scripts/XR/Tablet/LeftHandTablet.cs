using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace XR
{
    public sealed class LeftHandTablet : MonoBehaviour
    {
        [SerializeField] private Text _message;

        private InputAction _xButton;
        private InputAction _yButton;
        private InputAction _trigger;
        private int _pressCount;

        private void Awake()
        {
            _xButton = new InputAction("Tablet X", InputActionType.Button,
                "<XRController>{LeftHand}/primaryButton");
            _yButton = new InputAction("Tablet Y", InputActionType.Button,
                "<XRController>{LeftHand}/secondaryButton");
            _trigger = new InputAction("Tablet Trigger", InputActionType.Button,
                "<XRController>{LeftHand}/triggerPressed");
        }

        private void OnEnable()
        {
            _xButton.Enable();
            _yButton.Enable();
            _trigger.Enable();
        }

        private void Update()
        {
            if (_xButton.WasPressedThisFrame())
            {
                ShowButton("X");
            }
            if (_yButton.WasPressedThisFrame())
            {
                ShowButton("Y");
            }
            if (_trigger.WasPressedThisFrame())
            {
                ShowButton("TRIGGER");
            }
        }

        private void ShowButton(string button)
        {
            _pressCount++;
            _message.text = $"TABLET\n\n{button} PRESSED\n\nINPUT {_pressCount}";
        }

        private void OnDisable()
        {
            _xButton?.Disable();
            _yButton?.Disable();
            _trigger?.Disable();
        }

        private void OnDestroy()
        {
            _xButton?.Dispose();
            _yButton?.Dispose();
            _trigger?.Dispose();
        }
    }
}
