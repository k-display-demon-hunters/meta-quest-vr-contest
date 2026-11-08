using TMPro;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class CorridorDoor
    {
        internal static readonly Vector3 Position = new Vector3(0,-2.4f,22.5f);
        private CorridorVisuals _visuals;
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

        internal void Bind(CorridorVisuals visuals)
        {
            _visuals = visuals;
            _leftClosed = _left.localPosition;
            _rightClosed = _right.localPosition;
        }

        private CorridorDoor() { }

#if UNITY_EDITOR
        internal CorridorDoor(CorridorVisuals visuals, Transform root)
        {
            _visuals = visuals;
            Transform frame = CorridorVisuals.Group(root,"Corridor door",Position);
            _frame = frame;
            Color32 door = new Color32(34,56,69,255);
            _left = visuals.Shape(frame,"Left leaf",new Vector3(-.75f,1.8f,0),new Vector3(1.5f,3.6f,.16f),door,collider:true).transform;
            _right = visuals.Shape(frame,"Right leaf",new Vector3(.75f,1.8f,0),new Vector3(1.5f,3.6f,.16f),door,collider:true).transform;
            _glow = visuals.Shape(frame,"Threshold glow",new Vector3(0,.065f,.18f),
                new Vector3(3,.13f,.32f),(Color)visuals.Theme.Amber*.3f);
            _label = visuals.Label(frame,CorridorDialogue.DoorClosed,new Vector3(0,3.94f,0),.2f,4,visuals.Theme.Dim);
        }
#endif

        internal void Open()
        {
            _open = true;
            _label.text = CorridorDialogue.DoorOpen;
            _label.color = _visuals.Theme.Amber;
        }

        internal void Tick(float delta)
        {
            if (!_open || _progress >= 1) { return; }
            _progress = Mathf.Min(1,_progress+delta*1.45f);
            float offset = Mathf.SmoothStep(0,1,_progress);
            _left.localPosition = _leftClosed + Vector3.left * (_slideDistance * offset);
            _right.localPosition = _rightClosed + Vector3.right * (_slideDistance * offset);
            _visuals.Paint(_glow,(Color)_visuals.Theme.Amber*Mathf.Lerp(.3f,2.5f,offset));
        }
    }
}
