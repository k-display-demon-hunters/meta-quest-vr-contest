using UnityEngine;
using UnityEngine.Rendering;

namespace ModelDesignScene.ModelBuilder
{
    internal sealed class ModelBlockOutline
    {
        internal static readonly Color Amber = new Color(0.73f, 0.48f, 0.21f);
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private readonly LineRenderer _line;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();

        internal ModelBlockOutline(Transform parent, string name, Material material)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            _line = visual.AddComponent<LineRenderer>();
            _line.sharedMaterial = material;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.useWorldSpace = true;
            _line.positionCount = 4;
            _line.loop = true;
            _line.numCornerVertices = 4;
            SetAppearance(Amber, 0.022f);
        }

        internal void Show(Transform target, Bounds bounds, bool horizontal = false)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 offset = horizontal ? target.up * 0.018f : -target.forward * 0.018f;
            if (horizontal)
            {
                _line.SetPosition(0, target.TransformPoint(new Vector3(min.x, max.y, min.z)) + offset);
                _line.SetPosition(1, target.TransformPoint(new Vector3(max.x, max.y, min.z)) + offset);
                _line.SetPosition(2, target.TransformPoint(new Vector3(max.x, max.y, max.z)) + offset);
                _line.SetPosition(3, target.TransformPoint(new Vector3(min.x, max.y, max.z)) + offset);
            }
            else
            {
                _line.SetPosition(0, target.TransformPoint(new Vector3(min.x, min.y, min.z)) + offset);
                _line.SetPosition(1, target.TransformPoint(new Vector3(max.x, min.y, min.z)) + offset);
                _line.SetPosition(2, target.TransformPoint(new Vector3(max.x, max.y, min.z)) + offset);
                _line.SetPosition(3, target.TransformPoint(new Vector3(min.x, max.y, min.z)) + offset);
            }
        }

        internal void SetAppearance(Color color, float width)
        {
            _properties.SetColor(BaseColor, color);
            _line.SetPropertyBlock(_properties);
            _line.widthMultiplier = width;
        }

        internal void Remove()
        {
            _line.gameObject.SetActive(false);
            Object.Destroy(_line.gameObject);
        }
    }
}
