using UI;
using UnityEngine;
using UnityEngine.UI;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class CompletionPopupController
    {
        [SerializeField] private WorldUIPopupController _popup;
        [SerializeField] private Transform _parent;
        private float _time;
        private Vector3 _displayScale;
        internal void Bind()
        {
            foreach (Button button in _popup.GetComponentsInChildren<Button>(true))
            _buttons.Add(button, () =>
            {
            }
            );
            // The common popup owns confirmation/close; record the event once.
            _displayScale = _popup.transform.localScale;
            _popup.Hide();
        }

        private readonly CorridorButtonBindings _buttons = new CorridorButtonBindings();
        internal event System.Action<string> ButtonPressed
        {
            add => _buttons.Pressed += value;
            remove => _buttons.Pressed -= value;
        }

        internal void Enable() => _buttons.Enable();
        internal void Disable() => _buttons.Disable();
        internal void SetCamera(Camera camera)
        {
            foreach (var canvas in _popup.GetComponentsInChildren<Canvas>(true))
            {
                canvas.worldCamera = camera;
            }
        }

        private CompletionPopupController()
        {
        }

        internal void Open(Camera camera)
        {
            Transform view = camera.transform;
            _popup.transform.SetPositionAndRotation(view.position+view.forward*1.5f, view.rotation);
            _time = 0;
            _popup.Show();
            Tick(0);
        }

        internal void Tick(float delta)
        {
            if (!_popup.gameObject.activeSelf || _time >= .45f)
            {
                return;
            }
            _time = Mathf.Min(.45f, _time+delta);
            float t = _time/.45f;
            float scale = Mathf.Lerp(.7f, 1, Mathf.SmoothStep(0, 1, t))+.05f*Mathf.Sin(Mathf.PI*t);
            Vector3 parent = _parent.lossyScale;
            _popup.transform.localScale = new Vector3(scale*_displayScale.x/parent.x, scale*_displayScale.y/parent.y, scale*_displayScale.z/parent.z);
        }
    }
}
