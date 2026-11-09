using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    internal sealed class SideNumberController
    {
        private readonly List<KeyValuePair<Text, Text>> _labels = new List<KeyValuePair<Text, Text>>();

        public void Register(Text front, Text side) => _labels.Add(new KeyValuePair<Text, Text>(front, side));

        public void Refresh()
        {
            Camera viewer = Camera.main;
            foreach (KeyValuePair<Text, Text> pair in _labels)
            {
                if (pair.Value.text != pair.Key.text) { pair.Value.text = pair.Key.text; }
                pair.Value.color = pair.Key.color;
                if (viewer != null)
                {
                    Vector3 towardViewer = (viewer.transform.position - pair.Value.transform.position).normalized;
                    pair.Value.enabled = Vector3.Dot(pair.Value.transform.forward, towardViewer) < -0.25f;
                }
            }
        }
    }
}
