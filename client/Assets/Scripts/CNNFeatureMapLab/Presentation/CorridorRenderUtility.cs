using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class CorridorRenderUtility
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private MaterialPropertyBlock _properties;
        [SerializeField] private List<Transform> _billboards = new List<Transform>();
        [SerializeField] private Material _opaque;
        [SerializeField] private TMP_FontAsset _korean;
        [SerializeField] private CorridorTheme _theme;
        internal CorridorTheme Theme => _theme;
        internal TMP_FontAsset DebugFont => _korean;
        private CorridorRenderUtility()
        {
        }

        internal void InitializeRuntime()
        {
            _properties = new MaterialPropertyBlock();
        }

        internal void Paint(Renderer target, Color color)
        {
            target.GetPropertyBlock(_properties);
            _properties.SetColor(BaseColor, color);
            target.SetPropertyBlock(_properties);
        }

        internal void Texture(Renderer target, Texture2D texture)
        {
            target.GetPropertyBlock(_properties);
            _properties.SetTexture(BaseMap, texture);
            target.SetPropertyBlock(_properties);
        }

        internal void UpdateBillboards(Camera camera)
        {
            for (int i = _billboards.Count - 1; i >= 0; i--)
            {
                Transform billboard = _billboards[i];
                if (billboard == null)
                {
                    _billboards.RemoveAt(i);
                    continue;
                }
                if (!billboard.gameObject.activeInHierarchy)
                {
                    continue;
                }
                Vector3 forward = billboard.position - camera.transform.position;
                if (forward.sqrMagnitude > .0001f)
                billboard.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
        }

        internal void InstanceLights(Mesh mesh, Matrix4x4[] transforms, int count, Color color, int layer)
        {
            _properties.Clear();
            _properties.SetColor(BaseColor, color);
            if (!SystemInfo.supportsInstancing)
            {
                return;
            }
            Graphics.DrawMeshInstanced(mesh, 0, _opaque, transforms, count, _properties, ShadowCastingMode.Off, false, layer);
        }
    }
}
