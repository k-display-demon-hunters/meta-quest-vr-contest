using UnityEngine;

namespace FeatureMapCorridor
{
    [CreateAssetMenu(menuName = "CNN/Feature Map Corridor Theme")]
    public sealed class CorridorTheme : ScriptableObject
    {
        [SerializeField] internal Color32 Void = new Color32(7, 11, 20, 255);
        [SerializeField] internal Color32 SlabTop = new Color32(20, 36, 58, 255);
        [SerializeField] internal Color32 SlabSide = new Color32(8, 15, 24, 255);
        [SerializeField] internal Color32 Edge = new Color32(36, 93, 104, 255);
        [SerializeField] internal Color32 Cyan = new Color32(98, 228, 217, 255);
        [SerializeField] internal Color32 Amber = new Color32(255, 178, 74, 255);
        [SerializeField] internal Color32 Yellow = new Color32(255, 225, 74, 255);
        [SerializeField] internal Color32 Negative = new Color32(255, 122, 107, 255);
        [SerializeField] internal Color32 Ink = new Color32(230, 238, 246, 255);
        [SerializeField] internal Color32 Dim = new Color32(143, 163, 184, 255);
        [SerializeField] internal float PanelSize = 1.9f;
        [SerializeField] internal Font Korean;
        [SerializeField] internal Font Mono;
        [SerializeField] internal Shader Unlit;
        [SerializeField] internal Shader Lit;
        internal static readonly Color32[] Pairs =
        {
            new Color32(255, 111, 181, 255), new Color32(111, 168, 255, 255), new Color32(166, 232, 90, 255), new Color32(185, 140, 255, 255), new Color32(242, 242, 242, 255)
        };
        internal static readonly Color32[] Rgb =
        {
            new Color32(255, 92, 92, 255), new Color32(79, 220, 106, 255), new Color32(90, 140, 255, 255)
        };
        internal static Color MapColor(float value)
        {
            float v = Mathf.Clamp(value, -1, 1);
            if (v < 0)
            {
                return new Color(Mathf.Lerp(16, 255, -v)/255, Mathf.Lerp(14, 110, -v)/255, Mathf.Lerp(22, 95, -v)/255);
            }
            return new Color(Mathf.Lerp(10, 170, v*v)/255, Mathf.Lerp(18, 236, v)/255, Mathf.Lerp(32, 226, Mathf.Pow(v, .8f))/255);
        }
    }
}
