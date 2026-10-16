using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
#endif

//유니티 에디터에서만 작동하는 코드. 

namespace XR
{
    public sealed class XREditorSimulator : MonoBehaviour
    {
#if UNITY_EDITOR
        private readonly List<InputAction> _actions = new List<InputAction>();
        private GameObject _devices;
        private XRInteractionSimulator _simulator;

        private void Awake()
        {
            _devices = new GameObject("Simulated XR Devices");
            _devices.SetActive(false);
            _devices.transform.SetParent(transform, false);
            SimulatedDeviceLifecycleManager lifecycle = _devices.AddComponent<SimulatedDeviceLifecycleManager>();
            lifecycle.handTrackingCapability = false;
            _simulator = _devices.AddComponent<XRInteractionSimulator>();
            _simulator.deviceLifecycleManager = lifecycle;
            ConfigureMovement();
            ConfigureButtons();
        }

        private void OnEnable()
        {
            foreach (InputAction action in _actions)
            {
                action.Enable();
            }
            _devices.SetActive(true);
        }

        private void ConfigureMovement()
        {
            _simulator.translateXInput = Axis("Move X", "a", "d");
            _simulator.translateYInput = Axis("Move Y", "q", "e");
            _simulator.translateZInput = Axis("Move Z", "s", "w");
            _simulator.axis2DInput = Vector("Stick", "i", "k", "j", "l");
            _simulator.keyboardRotationDeltaInput = Vector("Rotate", "upArrow", "downArrow", "leftArrow", "rightArrow");
            _simulator.mouseRotationDeltaInput = Value<Vector2>(Action("Mouse Rotation", InputActionType.Value, "<Mouse>/delta"));
            _simulator.mouseScrollInput = Value<Vector2>(Action("Mouse Scroll", InputActionType.Value, "<Mouse>/scroll"));
            _simulator.toggleMouseInput = Button("Mouse Look", "<Mouse>/rightButton");
            _simulator.toggleManipulateLeftInput = Key("Left Device", "leftBracket");
            _simulator.toggleManipulateRightInput = Key("Right Device", "rightBracket");
            _simulator.leftDeviceActionsInput = Key("Left Actions", "shift");
            _simulator.cycleDevicesInput = Key("Cycle Devices", "tab");
            _simulator.toggleManipulateHeadInput = Key("Head", "h");
            _simulator.resetInput = Key("Reset Hands", "r");
            _simulator.xConstraintInput = Key("X Constraint", "v");
            _simulator.yConstraintInput = Key("Y Constraint", "c");
            _simulator.zConstraintInput = Key("Z Constraint", "z");
        }

        private void ConfigureButtons()
        {
            _simulator.triggerInput = Key("Trigger", "t");
            _simulator.gripInput = Key("Grip", "g");
            _simulator.primaryButtonInput = Key("Primary", "1");
            _simulator.secondaryButtonInput = Key("Secondary", "2");
            _simulator.menuInput = Key("Menu", "m");
            _simulator.primary2DAxisClickInput = Key("Stick Click", "3");
            _simulator.secondary2DAxisClickInput = Key("Secondary Stick Click", "4");
            _simulator.primary2DAxisTouchInput = Key("Stick Touch", "5");
            _simulator.secondary2DAxisTouchInput = Key("Secondary Stick Touch", "6");
            _simulator.primaryTouchInput = Key("Primary Touch", "7");
            _simulator.secondaryTouchInput = Key("Secondary Touch", "8");
            _simulator.togglePrimary2DAxisTargetInput = Key("Primary Stick", "9");
            _simulator.toggleSecondary2DAxisTargetInput = Key("Secondary Stick", "0");
            _simulator.cycleQuickActionInput = Key("Cycle Quick Action", "backquote");
            _simulator.togglePerformQuickActionInput = Key("Quick Action", "space");
        }

        private InputAction Action(string name, InputActionType type, string binding = null)
        {
            InputAction action = new InputAction(name, type, binding);
            _actions.Add(action);
            return action;
        }

        private XRInputButtonReader Key(string name, string key)
        {
            return Button(name, "<Keyboard>/" + key);
        }

        private XRInputButtonReader Button(string name, string binding)
        {
            InputAction action = Action(name, InputActionType.Button, binding);
            return new XRInputButtonReader(name, name, true, XRInputButtonReader.InputSourceMode.InputAction)
            {
                inputActionPerformed = action,
                inputActionValue = action
            };
        }

        private XRInputValueReader<float> Axis(string name, string negative, string positive)
        {
            InputAction action = Action(name, InputActionType.Value);
            action.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/" + negative)
                .With("Positive", "<Keyboard>/" + positive);
            return Value<float>(action);
        }

        private XRInputValueReader<Vector2> Vector(string name, string up, string down, string left, string right)
        {
            InputAction action = Action(name, InputActionType.Value);
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/" + up)
                .With("Down", "<Keyboard>/" + down)
                .With("Left", "<Keyboard>/" + left)
                .With("Right", "<Keyboard>/" + right);
            return Value<Vector2>(action);
        }

        private static XRInputValueReader<TValue> Value<TValue>(InputAction action) where TValue : struct
        {
            return new XRInputValueReader<TValue>(action.name, XRInputValueReader.InputSourceMode.InputAction)
            {
                inputAction = action
            };
        }

        private void OnDisable()
        {
            if (_devices != null)
            {
                _devices.SetActive(false);
            }
            foreach (InputAction action in _actions)
            {
                action.Disable();
            }
        }

        private void OnDestroy()
        {
            foreach (InputAction action in _actions)
            {
                action.Dispose();
            }
        }
#endif
    }
}
