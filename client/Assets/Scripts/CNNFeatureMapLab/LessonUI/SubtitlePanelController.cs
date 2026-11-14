using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

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
        private Vector3 _panelScale;
        private Vector2 _headerSize;
        private float _headerFontSizeMax;
        private Vector3 _previousScale;
        private Vector3 _nextScale;
        internal void Bind(Action previous, Action next)
        {
            _panelScale = _root.localScale;
            _headerSize = _header.rectTransform.sizeDelta;
            _headerFontSizeMax = _header.fontSizeMax;
            _previousScale = _previous.transform.localScale;
            _nextScale = _next.transform.localScale;
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

        internal void Show(string header, string body, bool previous, string next, string previousLabel = LessonContent.Previous,
            bool separateHeader = false, bool emphasizeHeading = false, bool enlargeButtons = false)
        {
            _header.text = header;
            _body.text = body;
            bool showButtons = previous || next != null;
            _header.rectTransform.sizeDelta = _headerSize + (emphasizeHeading ? new Vector2(0, 22) : Vector2.zero);
            _header.fontSizeMax = _headerFontSizeMax + (emphasizeHeading ? 20 : 0);
            _header.rectTransform.anchoredPosition = new Vector2(0, showButtons ? 177 : 103);
            _body.rectTransform.anchoredPosition = new Vector2(0,
                showButtons ? (header.Length > 0 ? 12 : 26) : (header.Length > 0 ? -34 : 0));
            if (emphasizeHeading)
            {
                _header.rectTransform.anchoredPosition = new Vector2(0, showButtons ? 155 : 115);
                _body.rectTransform.anchoredPosition = new Vector2(0, showButtons ? 24 : -20);
            }
            else if (separateHeader)
            {
                _header.rectTransform.anchoredPosition = new Vector2(0, 170);
                _body.rectTransform.anchoredPosition = new Vector2(0, -12);
            }
            float buttonScale = enlargeButtons ? 1.2f : 1f;
            _previous.transform.localScale = _previousScale * buttonScale;
            _next.transform.localScale = _nextScale * buttonScale;
            _previous.gameObject.SetActive(previous);
            if (previous)
            {
                _previous.SetText(previousLabel);
            }
            _next.gameObject.SetActive(next != null);
            if (next != null)
            {
                _next.SetText(next);
            }
        }

        internal void SetPose(Pose pose, float scale)
        {
            _root.SetPositionAndRotation(pose.position, pose.rotation);
            _root.localScale = _panelScale * scale;
        }
        internal void SetVisible(bool visible) => _root.gameObject.SetActive(visible);
    }
}
