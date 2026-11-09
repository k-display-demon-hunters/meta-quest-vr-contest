using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    internal sealed class FlyingNumberController
    {
        private readonly Transform _root;
        private readonly Text _label;

        public FlyingNumberController(WorkbenchPrimitiveFactory factory, string name, float width)
        {
            _root = factory.Root(name);
            _label = factory.Caption(_root, "", Vector3.zero, width);
            Hide();
        }

        public Vector3 WorldPosition => _root.position;
        public void Move(Vector3 position) => _root.localPosition = position;

        public void Show(string value, Color color, int size)
        {
            _label.text = value;
            _label.color = color;
            _label.fontSize = size;
            _root.gameObject.SetActive(true);
        }

        public void Hide() => _root.gameObject.SetActive(false);
    }
}
