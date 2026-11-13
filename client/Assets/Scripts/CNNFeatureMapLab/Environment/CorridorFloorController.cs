using System;
using TMPro;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class CorridorFloorController
    {
        private static readonly Color32 ActiveLabelColor = new Color32(20, 34, 54, 255);
        private static readonly Color32 IdleLabelColor = new Color32(170, 176, 178, 255);
        [SerializeField] private Renderer[] _zoneSurfaces;
        [SerializeField] private Material _activeSurface;
        private Material _idleSurface;
        [SerializeField] private TMP_Text[] _names = new TMP_Text[3*2];
        [SerializeField] private TMP_Text _outputLabel;
        private Vector3[] _nameScales = new Vector3[6];
        internal void Bind()
        {
            _idleSurface = _zoneSurfaces[0].sharedMaterial;
            for (int i = 0; i < _names.Length; i++)
            {
                _nameScales[i] = _names[i].transform.localScale;
            }
            Highlight(-1);
        }

        private CorridorFloorController()
        {
        }

        internal void Highlight(int zone)
        {
            _outputLabel.gameObject.SetActive(zone < 0 || zone >= 4);
            for (int i = 0; i < _zoneSurfaces.Length; i++)
            {
                _zoneSurfaces[i].sharedMaterial = i == zone ? _activeSurface : _idleSurface;
                if (i == 0 || i == 4)
                {
                    continue;
                }
                for (int line = 0; line < 2; line++)
                {
                    TMP_Text label = _names[(i-1)*2+line];
                    label.gameObject.SetActive(i == 1 ? zone == 1 : zone < 0 || i <= zone);
                    label.color = i == zone ? ActiveLabelColor : IdleLabelColor;
                    label.transform.localScale = _nameScales[(i-1)*2+line] * (i == zone ? 1.08f : 1);
                }
            }
        }

    }
}
