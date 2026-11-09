using TMPro;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class CorridorDoorController
    {
        internal static readonly Vector3 Position = new Vector3(0, -2.4f, 22.5f);
        private CorridorRenderUtility _visuals;
        [SerializeField] private Transform _left;
        [SerializeField] private Transform _right;
        [SerializeField] private Renderer _glow;
        [SerializeField] private TMP_Text _label;
        private float _progress;
        private bool _open;
        [SerializeField] private float _slideDistance = 1.45f;
        [SerializeField] private Transform _frame;
        internal Vector3 GuidePosition => _frame.TransformPoint(Vector3.up * 4.45f);
        private Vector3 _leftClosed;
        private Vector3 _rightClosed;
        internal void Bind(CorridorRenderUtility visuals)
        {
            _visuals = visuals;
            _leftClosed = _left.localPosition;
            _rightClosed = _right.localPosition;
        }

        private CorridorDoorController()
        {
        }

        internal void Open()
        {
            _open = true;
            _label.text = FeatureMapLabels.DoorOpen;
            _label.color = _visuals.Theme.Amber;
        }

        internal void Tick(float delta)
        {
            if (!_open || _progress >= 1)
            {
                return;
            }
            _progress = Mathf.Min(1, _progress+delta*1.45f);
            float offset = Mathf.SmoothStep(0, 1, _progress);
            _left.localPosition = _leftClosed + Vector3.left * (_slideDistance * offset);
            _right.localPosition = _rightClosed + Vector3.right * (_slideDistance * offset);
            _visuals.Paint(_glow, (Color)_visuals.Theme.Amber*Mathf.Lerp(.3f, 2.5f, offset));
        }
    }
}
