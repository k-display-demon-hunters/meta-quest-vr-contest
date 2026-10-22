using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FactoryEnvironment
{
    public sealed class FactoryFlowPortalController : MonoBehaviour
    {
        private TextMesh _label;
        private TextMesh _hint;
        private XRSimpleInteractable _button;
        private Material _material;
        private Action _advance;

        public void Initialize(Font font, Action advance)
        {
            _advance = advance;
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Next experience";
            face.transform.SetParent(transform, false);
            face.transform.localScale = new Vector3(1.7f, .3f, .05f);
            _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _material.color = new Color(.16f, .22f, .28f);
            face.GetComponent<Renderer>().sharedMaterial = _material;
            _button = face.AddComponent<XRSimpleInteractable>();
            _button.activated.AddListener(Activate);
            _label = Label(font, new Vector3(0, 0, -.04f), .013f);
            _hint = Label(font, new Vector3(0, -.35f, -.04f), .008f);
        }

        private TextMesh Label(Font font, Vector3 position, float size)
        {
            TextMesh label = new GameObject("Label").AddComponent<TextMesh>();
            label.transform.SetParent(transform, false);
            label.transform.localPosition = position;
            label.font = font;
            label.fontSize = 64;
            label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return label;
        }

        public void Show(string title, string hint, bool ready)
        {
            if (_label.text != title) { _label.text = title; }
            if (_hint.text != hint) { _hint.text = hint; }
            _label.color = ready ? Color.white : Color.gray;
            if (_button.enabled != ready) { _button.enabled = ready; }
        }

        private void Activate(ActivateEventArgs args) { _advance?.Invoke(); }
        private void OnDestroy() { if (_material != null) { Destroy(_material); } }
    }
}
