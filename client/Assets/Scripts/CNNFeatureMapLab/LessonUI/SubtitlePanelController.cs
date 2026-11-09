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
        private Vector3 _targetHeading;
        private Vector3 _fromHeading;
        private Vector3 _displayHeading;
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
            Vector3 heading = view.forward;
            if (!_placed)
            {
                _targetHeading = heading;
                _fromHeading = heading;
                _displayHeading = heading;
                _motion = 1;
                _placed = true;
            }
            else if (Vector3.Angle(heading, _targetHeading) > 35)
            {
                _fromHeading = _displayHeading;
                _targetHeading = heading;
                _motion = 0;
            }
            _motion = Mathf.Min(1, _motion+delta*2);
            _displayHeading = Vector3.Slerp(_fromHeading, _targetHeading, Mathf.SmoothStep(0, 1, _motion));
            float downwardAngle = camera.stereoEnabled ? 25 : Mathf.Clamp(camera.fieldOfView*.5f-11, 10, 25);
            Vector3 right = Vector3.Cross(Vector3.up, _displayHeading).normalized;
            if (right.sqrMagnitude < .01f)
            {
                right = view.right;
            }
            Vector3 offset = Quaternion.AngleAxis(downwardAngle, right)*_displayHeading*1.6f;
            _root.SetPositionAndRotation(view.position+offset, Quaternion.LookRotation(offset, Vector3.up));
            Vector3 parentScale = _root.parent.lossyScale;
            _root.localScale = new Vector3(_displayScale.x/parentScale.x, _displayScale.y/parentScale.y, _displayScale.z/parentScale.z);
        }

        internal void SetVisible(bool visible) => _root.gameObject.SetActive(visible);
    }
}
