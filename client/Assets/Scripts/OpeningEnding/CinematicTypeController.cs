using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class CinematicTypeController : MonoBehaviour
    {
        [SerializeField] private Text _line;
        [SerializeField] private Text _hint;
        [SerializeField] private Image _backing;
        [SerializeField] private Image _holdBar;
        private CinematicText.Line[] _lines;
        private int _active = -1;
        private int _visible;
        private string _rich;
        private string _plain;
        private float[] _times;
        private bool _cursor;
        private bool _ending;
        private Vector3 _captionScale;
        public event Action<AudioClip> VoiceRequested;
        public event Action CharacterTyped;

        public void Bind(CinematicText.Line[] lines, bool ending)
        {
            _lines = lines;
            _ending = ending;
            _captionScale = _line.transform.parent.localScale;
            _line.text = "";
        }

        public void Evaluate(float time, bool inWorld)
        {
            Transform caption = _line.transform.parent;
            bool showInFront = !_ending && time >= OpeningTiming.Digit && time < OpeningTiming.Doors;
            if (showInFront)
            {
                // Keep the subtitle in front of the number and the moving pixels at the same apparent size.
                const float foregroundDistance = .85f;
                const float subtitleDistance = 2.5f;
                caption.localPosition = new Vector3(0, 1.36f, foregroundDistance);
                caption.localScale = _captionScale * (foregroundDistance / subtitleDistance);
            }
            else
            {
                float height = inWorld ? (time >= 12.6f ? .85f : .99f) : 1.6f;
                if (_ending && time >= 16.6f && time < 18f) { height = .95f; }
                caption.localPosition = new Vector3(0, height, 2.5f);
                caption.localScale = _captionScale;
            }
            int next = -1;
            for (int i = 0; i < _lines.Length; i++)
            {
                if (time >= _lines[i].Start && time < _lines[i].End) { next = i; break; }
            }
            if (next != _active)
            {
                _active = next;
                _visible = -1;
                if (next >= 0)
                {
                    _rich = Regex.Replace(_lines[next].Text, @"\{C:([^}]+)\}", "<color=#62E4D9>$1</color>");
                    _rich = Regex.Replace(_rich, @"\{A:([^}]+)\}", "<color=#FFB24A>$1</color>");
                    _plain = Regex.Replace(_rich, "<[^>]+>", "");
                    _times = new float[_plain.Length];
                    float duration = 0;
                    for (int i = 0; i < _plain.Length; i++)
                    { duration += char.IsWhiteSpace(_plain[i]) ? .03f : .055f; _times[i] = duration; }
                    VoiceRequested?.Invoke(_lines[next].Voice);
                }
            }
            if (next < 0) { _line.text = ""; _backing.enabled = false; return; }
            float elapsed = time - _lines[next].Start;
            int count = 0;
            while (count < _times.Length && elapsed >= _times[count]) { count++; }
            float typedDuration = _times.Length == 0 ? 0 : _times[_times.Length - 1];
            bool cursor = count == _plain.Length && elapsed - typedDuration < .8f
                && Mathf.FloorToInt((elapsed - typedDuration) / .2f) % 2 == 0;
            float alpha = Mathf.Clamp01((_lines[next].End - time) / .25f);
            _line.color = new Color(.96f, .96f, .94f, alpha);
            _backing.enabled = inWorld || showInFront;
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

        public void SetHint(string text, float progress) { _hint.text = text; _holdBar.fillAmount = progress; }
        public void ShowCredits(string text)
        {
            _line.text = text; _line.fontSize = 26; _line.color = Color.white; _backing.enabled = false;
            _line.rectTransform.anchoredPosition = new Vector2(0, 220);
            _line.rectTransform.sizeDelta = new Vector2(1450, 550);
        }
    }
}
