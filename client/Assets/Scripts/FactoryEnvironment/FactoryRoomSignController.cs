using UI;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryEnvironment
{
    public sealed class FactoryRoomSignController : MonoBehaviour
    {
        public void Initialize(Font font)
        {
            foreach (MeshRenderer face in GetComponentsInChildren<MeshRenderer>(true))
            {
                string title = face.name == "Convolution Room printed face" ? "Pixel Lab"
                    : face.name == "CNN Room printed face" ? "Feature Map Lab"
                    : face.name == "MLP Room printed face" ? "Model Design Lab" : null;
                if (title == null)
                {
                    continue;
                }
                // Replace the old printed lettering without duplicating or changing shared textures.
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                face.GetPropertyBlock(properties);
                properties.SetTexture("_BaseMap", Texture2D.whiteTexture);
                properties.SetTexture("_MainTex", Texture2D.whiteTexture);
                properties.SetColor("_BaseColor", SceneUIStyle.Surface);
                properties.SetColor("_Color", SceneUIStyle.Surface);
                face.SetPropertyBlock(properties);

                GameObject panel = new GameObject(title + " sign", typeof(RectTransform), typeof(Canvas));
                panel.transform.SetParent(face.transform.parent, false);
                panel.transform.SetPositionAndRotation(face.transform.position - face.transform.forward * 0.01f,
                    face.transform.rotation);
                panel.transform.localScale = Vector3.one * 0.003f;
                RectTransform rect = panel.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1180f, 300f);
                panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                GameObject labelObject = new GameObject("Room name", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(panel.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                Text label = labelObject.GetComponent<Text>();
                label.font = font;
                label.text = title;
                label.fontSize = 90;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 50;
                label.resizeTextMaxSize = 90;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = SceneUIStyle.Accent;
                label.raycastTarget = false;
            }
        }
    }
}
