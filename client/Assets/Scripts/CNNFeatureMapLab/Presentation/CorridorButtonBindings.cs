using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace FeatureMapCorridor
{
    internal sealed class CorridorButtonBindings
    {
        private readonly List<Action> _enable = new List<Action>();
        private readonly List<Action> _disable = new List<Action>();
        internal event Action<string> Pressed;
        internal void Add(Button button, Action action)
        {
            UnityEngine.Events.UnityAction listener = () =>
            {
                Pressed?.Invoke(button.GetComponentInChildren<Text>(true).text);
                action();
            };
            _enable.Add(() => button.onClick.AddListener(listener));
            _disable.Add(() => button.onClick.RemoveListener(listener));
        }

        internal void Enable()
        {
            foreach (Action action in _enable)
            {
                action();
            }
        }

        internal void Disable()
        {
            foreach (Action action in _disable)
            {
                action();
            }
        }
    }
}
