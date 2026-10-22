using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryFlowPortalController : MonoBehaviour
    {
        private Material _material;

        public void Initialize(Font font)
        {
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Factory route instruction";
            face.transform.SetParent(transform, false);
            face.transform.localScale = new Vector3(3.6f, 0.45f, 0.05f);
            // This sign displays instructions without blocking movement or accepting input.
            Destroy(face.GetComponent<Collider>());
            _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _material.color = new Color(0.16f, 0.22f, 0.28f);
            face.GetComponent<Renderer>().sharedMaterial = _material;
            TextMesh label = new GameObject("Instruction").AddComponent<TextMesh>();
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            label.font = font;
            label.fontSize = 64;
            label.characterSize = 0.01f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            label.text = "바닥의 안내선을 따라 이동하세요.";
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
