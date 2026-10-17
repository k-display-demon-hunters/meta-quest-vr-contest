using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using static Tablet.TabletUIBuilder;

namespace Tablet
{
    internal sealed class TabletInputController : IDisposable
    {
        private readonly InputAction _xButton;
        private readonly InputAction _yButton;
        private readonly InputAction _trigger;
        private readonly InputAction _menuButton;
        private readonly InputAction _stick;
        private readonly ContinuousMoveProvider _moveProvider;
        private XRInputValueReader<Vector2> _previousMoveInput;
        private readonly XRInputValueReader<Vector2> _blockedMoveInput =
            new XRInputValueReader<Vector2>("Tablet Focus", XRInputValueReader.InputSourceMode.ManualValue);
        private readonly List<FocusTarget> _mainTargets = new List<FocusTarget>();
        private readonly List<FocusTarget> _settingsTargets = new List<FocusTarget>();
        private readonly List<FocusTarget> _stageTargets = new List<FocusTarget>();
        private readonly List<FocusTarget> _detailTargets = new List<FocusTarget>();
        private bool _movementCaptured;
        private bool _isInteracting;
        private bool _isAdjustingSlider;
        private bool _stickMustCenter;
        private Vector2 _lastDirection;
        private float _nextRepeatTime;
        private int _focusIndex;

        private readonly Func<FocusGroup> _activeGroup;
        private readonly Func<RectTransform> _canvasRect;
        private readonly Action<bool> _interactionRequested;
        private readonly Action _toggleSettings;
        private readonly Action _goBack;
        private readonly Action<Selectable, bool> _focusChanged;

        internal enum FocusGroup { Main, Settings, Stage, Detail }

        public bool IsInteracting => _isInteracting;
        public bool IsAdjustingSlider => _isAdjustingSlider;
        public int FocusIndex => _focusIndex;
        private List<FocusTarget> ActiveTargets => Targets(_activeGroup());

        public TabletInputController(ContinuousMoveProvider moveProvider, Func<FocusGroup> activeGroup,
            Func<RectTransform> canvasRect, Action<bool> interactionRequested, Action toggleSettings,
            Action goBack, Action<Selectable, bool> focusChanged)
        {
            _moveProvider = moveProvider;
            _activeGroup = activeGroup;
            _canvasRect = canvasRect;
            _interactionRequested = interactionRequested;
            _toggleSettings = toggleSettings;
            _goBack = goBack;
            _focusChanged = focusChanged;
            _xButton = new InputAction("Tablet X", InputActionType.Button,
                "<XRController>{LeftHand}/primaryButton");
            _yButton = new InputAction("Tablet Y", InputActionType.Button,
                "<XRController>{LeftHand}/secondaryButton");
            _trigger = new InputAction("Tablet Trigger", InputActionType.Button,
                "<XRController>{LeftHand}/triggerPressed");
            _menuButton = new InputAction("Tablet Settings", InputActionType.Button,
                "<XRController>{LeftHand}/menuButton");
            _stick = new InputAction("Tablet Navigation", InputActionType.Value,
                "<XRController>{LeftHand}/primary2DAxis");
        }

        private List<FocusTarget> Targets(FocusGroup group)
        {
            switch (group)
            {
                case FocusGroup.Settings: return _settingsTargets;
                case FocusGroup.Stage: return _stageTargets;
                case FocusGroup.Detail: return _detailTargets;
                default: return _mainTargets;
            }
        }

        public void RegisterControl(Selectable control, FocusGroup group)
        {
            control.navigation = new Navigation { mode = Navigation.Mode.None };
            Outline outline = control.GetComponent<Image>().gameObject.AddComponent<Outline>();
            outline.effectColor = AccentColor;
            outline.effectDistance = new Vector2(3f, 3f);
            outline.enabled = false;
            Targets(group).Add(new FocusTarget(control, outline));
        }

        public void RemoveControls(List<Button> buttons)
        {
            _mainTargets.RemoveAll(target => buttons.Contains(target.Control as Button));
        }

        public int IndexOfMainControl(Selectable control, int fallback)
        {
            int index = _mainTargets.FindIndex(target => target.Control == control);
            return index >= 0 ? index : fallback;
        }

        public void SetInteraction(bool active, int initialIndex)
        {
            _isInteracting = active;
            ResetFocus(initialIndex);
        }

        public void StopAdjustingSlider()
        {
            _isAdjustingSlider = false;
            _stickMustCenter = true;
            _lastDirection = Vector2.zero;
            RefreshFocus();
        }

        public void Disable()
        {
            _xButton.Disable();
            _yButton.Disable();
            _trigger.Disable();
            _menuButton.Disable();
            _stick.Disable();
            ReleaseMovement();
        }

        public void Dispose()
        {
            Disable();
            _xButton.Dispose();
            _yButton.Dispose();
            _trigger.Dispose();
            _menuButton.Dispose();
            _stick.Dispose();
        }

        public void Enable()
        {
            _xButton.Enable();
            _yButton.Enable();
            _trigger.Enable();
            _menuButton.Enable();
            _stick.Enable();
        }

        public void Tick()
        {
            bool navigating = _trigger.IsPressed();
            Vector2 stick = _stick.ReadValue<Vector2>();
            if (navigating)
            {
                if (!_isInteracting)
                {
                    _interactionRequested(true);
                }
                CaptureMovement();
            }
            else if (stick.magnitude < 0.25f)
            {
                // Do not turn a tablet gesture into walking until the stick returns to neutral.
                ReleaseMovement();
            }

            if (_menuButton.WasPressedThisFrame())
            {
                _toggleSettings();
            }
            else if (_yButton.WasPressedThisFrame())
            {
                _goBack();
            }
            else if (_xButton.WasPressedThisFrame())
            {
                if (!_isInteracting)
                {
                    _interactionRequested(true);
                }
                if (ActiveTargets[_focusIndex].Control is Button button && button.IsInteractable())
                {
                    button.onClick.Invoke();
                }
                else if (ActiveTargets[_focusIndex].Control is Slider slider && slider.IsInteractable())
                {
                    _isAdjustingSlider = !_isAdjustingSlider;
                    _stickMustCenter = true;
                    RefreshFocus();
                }
            }

            if (navigating && _isInteracting)
            {
                Navigate(stick);
            }
        }

        private void CaptureMovement()
        {
            if (!_movementCaptured && _moveProvider != null)
            {
                _previousMoveInput = _moveProvider.leftHandMoveInput;
                _moveProvider.leftHandMoveInput = _blockedMoveInput;
                _movementCaptured = true;
            }
        }

        private void ReleaseMovement()
        {
            if (_movementCaptured && _moveProvider != null && _moveProvider.leftHandMoveInput == _blockedMoveInput)
            {
                _moveProvider.leftHandMoveInput = _previousMoveInput;
            }
            _movementCaptured = false;
            _previousMoveInput = null;
        }

        public void ResetFocus(int index)
        {
            _focusIndex = Mathf.Clamp(index, 0, ActiveTargets.Count - 1);
            if (!ActiveTargets[_focusIndex].Control.IsActive() || !ActiveTargets[_focusIndex].Control.IsInteractable())
            {
                for (int candidate = 0; candidate < ActiveTargets.Count; candidate++)
                {
                    if (ActiveTargets[candidate].Control.IsActive() && ActiveTargets[candidate].Control.IsInteractable())
                    {
                        _focusIndex = candidate;
                        break;
                    }
                }
            }
            _isAdjustingSlider = false;
            _stickMustCenter = true;
            _lastDirection = Vector2.zero;
            RefreshFocus();
        }

        private void RefreshFocus()
        {
            foreach (FocusTarget target in _mainTargets)
            {
                target.Outline.enabled = false;
            }
            foreach (FocusTarget target in _settingsTargets)
            {
                target.Outline.enabled = false;
            }
            foreach (FocusTarget target in _stageTargets)
            {
                target.Outline.enabled = false;
            }
            foreach (FocusTarget target in _detailTargets)
            {
                target.Outline.enabled = false;
            }
            _focusChanged(_isInteracting ? ActiveTargets[_focusIndex].Control : null, _activeGroup() == FocusGroup.Main);
            if (_isInteracting)
            {
                Outline outline = ActiveTargets[_focusIndex].Outline;
                outline.effectColor = _isAdjustingSlider ? WarningColor : AccentColor;
                outline.enabled = true;
            }
        }

        private void Navigate(Vector2 stick)
        {
            if (stick.magnitude < 0.25f)
            {
                _stickMustCenter = false;
                _lastDirection = Vector2.zero;
                return;
            }
            if (_stickMustCenter || stick.magnitude < 0.6f)
            {
                return;
            }
            Vector2 direction = Mathf.Abs(stick.x) > Mathf.Abs(stick.y)
                ? new Vector2(Mathf.Sign(stick.x), 0f) : new Vector2(0f, Mathf.Sign(stick.y));
            bool newDirection = direction != _lastDirection;
            if (!newDirection && Time.unscaledTime < _nextRepeatTime)
            {
                return;
            }
            _lastDirection = direction;
            _nextRepeatTime = Time.unscaledTime + (newDirection ? 0.4f : 0.16f);
            List<FocusTarget> targets = ActiveTargets;
            if (_isAdjustingSlider)
            {
                if (direction.x != 0f && targets[_focusIndex].Control is Slider slider)
                {
                    slider.value = Mathf.Clamp01(slider.value + direction.x * 0.05f);
                }
                return;
            }

            Vector2 current = _canvasRect().InverseTransformPoint(targets[_focusIndex].Control.transform.position);
            float bestScore = float.PositiveInfinity;
            int nextIndex = _focusIndex;
            for (int index = 0; index < targets.Count; index++)
            {
                if (index == _focusIndex || !targets[index].Control.IsActive() || !targets[index].Control.IsInteractable())
                {
                    continue;
                }
                Vector2 position = _canvasRect().InverseTransformPoint(targets[index].Control.transform.position);
                Vector2 delta = position - current;
                float forward = Vector2.Dot(delta, direction);
                if (forward <= 1f)
                {
                    continue;
                }
                RectTransform targetRect = (RectTransform)targets[index].Control.transform;
                Vector2 halfSize = targetRect.rect.size * 0.5f;
                float perpendicularExtent = direction.x == 0f ? halfSize.x : halfSize.y;
                float sideways = Mathf.Max(0f,
                    Mathf.Abs(delta.x * direction.y - delta.y * direction.x) - perpendicularExtent);
                float score = forward + sideways * 2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    nextIndex = index;
                }
            }
            _focusIndex = nextIndex;
            RefreshFocus();
        }

        private sealed class FocusTarget
        {
            public FocusTarget(Selectable control, Outline outline)
            {
                Control = control;
                Outline = outline;
            }

            public Selectable Control { get; }
            public Outline Outline { get; }
        }
    }
}
