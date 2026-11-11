using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class SubtitlePanelController
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private TMP_Text _header;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private WorldUIButtonController _previous;
        [SerializeField] private WorldUIButtonController _next;
        private float _targetYaw;
        private float _fromYaw;
        private float _displayYaw;
        private float _motion;
        private bool _placed;
        private Vector3 _displayScale;
        internal void Bind(Action previous, Action next)
        {
            _displayScale = _root.localScale;
            _buttons.Add(_previous.GetComponent<Button>(), previous);
            _buttons.Add(_next.GetComponent<Button>(), next);
        }

        private readonly CorridorButtonBindings _buttons = new CorridorButtonBindings();
        internal event Action<string> ButtonPressed
        {
            add => _buttons.Pressed += value;
            remove => _buttons.Pressed -= value;
        }

        internal void Enable() => _buttons.Enable();
        internal void Disable() => _buttons.Disable();
        internal void SetCamera(Camera camera)
        {
            foreach (var canvas in _root.GetComponentsInChildren<Canvas>(true))
            {
                canvas.worldCamera = camera;
            }
        }

        private SubtitlePanelController()
        {
        }

        internal void Show(string header, string body, bool previous, string next)
        {
            _header.text = header;
            _body.text = body;
            _previous.gameObject.SetActive(previous);
            _next.gameObject.SetActive(next != null);
            if (next != null)
            {
                _next.SetText(next);
            }
        }

        internal void Track(Camera camera, float delta)
        {
            Transform view = camera.transform;
            Vector3 heading = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            // Keep the last horizontal direction when looking almost straight up or down.
            float yaw = heading.sqrMagnitude >= .01f
                ? Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg
                : _placed ? _targetYaw : view.eulerAngles.y;
            if (!_placed)
            {
                _targetYaw = yaw;
                _fromYaw = yaw;
                _displayYaw = yaw;
                _motion = 1;
                _placed = true;
            }
            else if (Mathf.Abs(Mathf.DeltaAngle(_targetYaw, yaw)) > 35f)
            {
                _fromYaw = _displayYaw;
                _targetYaw = yaw;
                _motion = 0;
            }

            _motion = Mathf.Min(1f, _motion + delta * 2f);
            _displayYaw = Mathf.LerpAngle(_fromYaw, _targetYaw, Mathf.SmoothStep(0f, 1f, _motion));
            float downwardAngle = camera.stereoEnabled ? 18f : Mathf.Clamp(camera.fieldOfView * .5f - 11f, 10f, 18f);
            Quaternion placement = Quaternion.Euler(downwardAngle, _displayYaw, 0f);
            Vector3 offset = placement * Vector3.forward * 1.6f;
            _root.SetPositionAndRotation(view.position+offset, Quaternion.LookRotation(offset, Vector3.up));
            Vector3 parentScale = _root.parent.lossyScale;
            _root.localScale = new Vector3(_displayScale.x/parentScale.x, _displayScale.y/parentScale.y, _displayScale.z/parentScale.z);
        }

        internal void SetVisible(bool visible)
        {
            if (!visible)
            {
                _placed = false;
            }

            _root.gameObject.SetActive(visible);
        }
    }
}
