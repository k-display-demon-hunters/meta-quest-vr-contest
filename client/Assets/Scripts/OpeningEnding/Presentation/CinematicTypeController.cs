using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class CinematicTypeController : MonoBehaviour
    {
        [SerializeField] private Text _line;
        [SerializeField] private Image _backing;
        private CinematicText.Line _active;
        private int _visible;
        private string _rich;
        private string _plain;
        private float[] _times;
        private bool _cursor;
        private Vector3 _captionScale;
        public event Action CharacterTyped;

        internal void Bind()
        {
            _captionScale = _line.transform.parent.localScale;
            ClearLine();
        }

        internal void Show(CinematicText.Line line, float elapsed, float remaining, float height, bool foreground, bool backing)
        {
            Transform caption = _line.transform.parent;
            float distance = foreground ? .85f : 2.5f;
            caption.localPosition = new Vector3(0, height, distance);
            caption.localScale = foreground ? _captionScale * (.85f / 2.5f) : _captionScale;

            if (line != _active)
            {
                _active = line;
                _visible = -1;
                if (line != null)
                {
                    _rich = Regex.Replace(line.Text, @"\{C:([^}]+)\}", "<color=#62E4D9>$1</color>");
                    _rich = Regex.Replace(_rich, @"\{A:([^}]+)\}", "<color=#FFB24A>$1</color>");
                    _plain = Regex.Replace(_rich, "<[^>]+>", "");
                    _times = new float[_plain.Length];
                    float duration = 0;
                    for (int i = 0; i < _plain.Length; i++)
                    {
                        duration += char.IsWhiteSpace(_plain[i]) ? .03f : .055f;
                        _times[i] = duration;
                    }
                }
            }
            if (line == null) { ClearLine(); return; }
            int count = 0;
            while (count < _times.Length && elapsed >= _times[count]) { count++; }
            float typedDuration = _times.Length == 0 ? 0 : _times[_times.Length - 1];
            bool cursor = count == _plain.Length && elapsed - typedDuration < .8f
                && Mathf.FloorToInt((elapsed - typedDuration) / .2f) % 2 == 0;
            float alpha = Mathf.Clamp01(remaining / .25f);
            _line.color = new Color(.96f, .96f, .94f, alpha);
            _backing.enabled = backing;
            _backing.color = new Color(0, 0, 0, .45f * alpha);
            if (count == _visible && cursor == _cursor) { return; }
            _cursor = cursor;
            if (count != _visible)
            {
                if (count > _visible && count > 0 && !char.IsWhiteSpace(_plain[count - 1])) { CharacterTyped?.Invoke(); }
                _visible = count;
            }
            // Balance color tags when revealing only part of a highlighted phrase.
            string result = "";
            int shown = 0;
            bool colored = false;
            for (int i = 0; i < _rich.Length && shown < count; i++)
            {
                if (_rich[i] == '<')
                {
                    int end = _rich.IndexOf('>', i);
                    string tag = _rich.Substring(i, end - i + 1);
                    result += tag; colored = !tag.StartsWith("</"); i = end;
                }
                else { result += _rich[i]; shown++; }
            }
            if (colored) { result += "</color>"; }
            _line.text = result + (cursor ? "▌" : "");
        }

        public void ClearLine()
        {
            _line.text = string.Empty;
            _backing.enabled = false;
        }
    }
}
