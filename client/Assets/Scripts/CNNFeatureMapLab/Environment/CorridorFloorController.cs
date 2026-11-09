using System;
using TMPro;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class CorridorFloorController
    {
        private static readonly float[] Starts =
        {
            -9.5f, -5.2f, -.2f, 6.4f, 16.4f
        };
        private static readonly float[] Ends =
        {
            -5.6f, -1.2f, 5.4f, 15.2f, 25.5f
        };
        private const float Width = 6.8f;
        private CorridorRenderUtility _visuals;
        [SerializeField] private Transform _root;
        [SerializeField] private Mesh _cube;
        private Matrix4x4[] _edgeLights = new Matrix4x4[70];
        private Matrix4x4[] _stepLights = new Matrix4x4[108];
        [SerializeField] private Renderer[] _stepGlow = new Renderer[4];
        [SerializeField] private Renderer[] _zoneOverlay = new Renderer[5];
        [SerializeField] private Transform[] _zoneEdges = new Transform[5];
        [SerializeField] private TMP_Text[] _names = new TMP_Text[3*2];
        private Vector3[] _nameScales = new Vector3[6];
        private int _highlight = -1;
        internal void Bind(CorridorRenderUtility visuals)
        {
            _visuals = visuals;
            for (int i = 0; i < _names.Length; i++)
            {
                _nameScales[i] = _names[i].transform.localScale;
            }
            Highlight(-1);
        }

        private CorridorFloorController()
        {
        }

        internal int HighlightedZone => _highlight;
        internal void Highlight(int zone)
        {
            _highlight = zone;
            for (int i = 0; i < _zoneOverlay.Length; i++)
            {
                _zoneOverlay[i].enabled = zone >= 0;
                Color wash = _visuals.Theme.Cyan;
                wash.a = i == zone ? .14f : .035f;
                _visuals.Paint(_zoneOverlay[i], wash);
                _zoneEdges[i].gameObject.SetActive(i == zone);
                if (i == 0 || i == 4)
                {
                    continue;
                }
                for (int line = 0; line < 2; line++)
                {
                    TMP_Text label = _names[(i-1)*2+line];
                    label.color = i == zone ? _visuals.Theme.Cyan : new Color32(111, 133, 153, 255);
                    label.transform.localScale = _nameScales[(i-1)*2+line] * (i == zone ? 1.08f : 1);
                }
            }
        }

        internal void DrawFlow(float time)
        {
            Matrix4x4 world = _root.localToWorldMatrix;
            for (int i = 0; i < _edgeLights.Length; i++)
            {
                float z = Mathf.Lerp(Starts[0], Ends[4], Mathf.Repeat(time/22+i/35f, 1));
                float x = i < 35 ? -3.1f : 3.1f;
                _edgeLights[i] = world * Matrix4x4.TRS(new Vector3(x, Height(z)+.027f, z), Quaternion.identity, new Vector3(.07f, .03f, .32f));
            }
            for (int zone = 0; zone < 4; zone++)
            {
                Color glow = _visuals.Theme.Cyan;
                glow.a = .095f + .025f*Mathf.Sin(time*2.1f+zone);
                _visuals.Paint(_stepGlow[zone], glow);
                for (int i = 0; i < 27; i++)
                {
                    float t = Mathf.Repeat(time/1.8f+i/9/3f+(i%9)*.019f, 1);
                    float z = Mathf.Lerp(Ends[zone], Starts[zone+1], t);
                    float x = Mathf.Lerp(-2.95f, 2.95f, (i%9)/8f);
                    _stepLights[zone*27+i] = world * Matrix4x4.TRS(new Vector3(x, Height(z)+.042f, z), Quaternion.identity, new Vector3(.09f, .03f, .13f));
                }
            }
            _visuals.InstanceLights(_cube, _edgeLights, _edgeLights.Length, (Color)_visuals.Theme.Cyan*1.7f, _root.gameObject.layer);
            _visuals.InstanceLights(_cube, _stepLights, _stepLights.Length, new Color(.65f, 1.12f, 1.07f), _root.gameObject.layer);
        }

        private static float Height(float z)
        {
            for (int i = 0; i < Ends.Length-1; i++)
            {
                if (z <= Ends[i])
                {
                    return -.6f*i;
                }
                if (z < Starts[i+1])
                {
                    return -.6f*i-.2f*Mathf.Ceil(3*Mathf.InverseLerp(Ends[i], Starts[i+1], z));
                }
            }
            return -2.4f;
        }
    }
}
