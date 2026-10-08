using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace XR
{
    public sealed class LeftHandTablet : MonoBehaviour
    {
        [SerializeField] private Transform _screen;

        private InputAction _xButton;
        private InputAction _yButton;
        private InputAction _trigger;
        private Text _message;
        private int _pressCount;

        private void Awake()
        {
            CreateDisplay();
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

        private void CreateDisplay()
        {
            GameObject display = new GameObject("Tablet Display", typeof(RectTransform), typeof(Canvas));
            display.transform.SetParent(_screen, false);
            // The screen is an unscaled anchor; UI pixels map to 0.5 mm in world space.
            display.transform.localScale = Vector3.one * 0.0005f;
            RectTransform displayRect = display.GetComponent<RectTransform>();
            displayRect.sizeDelta = new Vector2(500f, 340f);
            display.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            GameObject label = new GameObject("Button Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(display.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _message = label.GetComponent<Text>();
            _message.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _message.fontSize = 32;
            _message.alignment = TextAnchor.MiddleCenter;
            _message.color = Color.white;
            _message.raycastTarget = false;
            _message.text = "TABLET READY\n\nX / Y / TRIGGER";
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
