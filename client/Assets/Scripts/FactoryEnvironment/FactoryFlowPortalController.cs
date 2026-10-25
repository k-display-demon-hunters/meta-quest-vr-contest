using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryFlowPortalController : MonoBehaviour
    {
        private Material _material;
        private TextMesh _label;

        public void Initialize(Font font)
        {
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Factory route instruction";
            face.transform.SetParent(transform, false);
            face.transform.localScale = new Vector3(4.8f, 1.1f, 0.05f);
            // This sign displays instructions without blocking movement or accepting input.
            Destroy(face.GetComponent<Collider>());
            _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _material.color = UI.SceneUIStyle.Surface;
            face.GetComponent<Renderer>().sharedMaterial = _material;
            TextMesh label = new GameObject("Instruction").AddComponent<TextMesh>();
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            _label = label;
            label.font = font;
            label.fontSize = 64;
            label.characterSize = 0.006f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = UI.SceneUIStyle.Accent;
            label.text = "바닥의 안내선을 따라 이동하세요.";
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        public void SetInstruction(string message) { if (_label != null) { _label.text = message; } }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
