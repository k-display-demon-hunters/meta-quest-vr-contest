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
        internal void Bind(Action previous, Action next)
        {
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

        internal void Show(string header, string body, bool previous, string next, string previousLabel = LessonContent.Previous)
        {
            _header.text = header;
            _body.text = body;
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

        internal void SetPosition(Vector3 position) => _root.position = position;
        internal void SetVisible(bool visible) => _root.gameObject.SetActive(visible);
    }
}
