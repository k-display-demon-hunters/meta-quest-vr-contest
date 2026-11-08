using TMPro;
using UnityEngine;

namespace FeatureMapCorridor
{
    internal sealed class CorridorDoor
    {
        internal static readonly Vector3 Position = new Vector3(0,-2.4f,22.5f);
        private readonly CorridorVisuals _visuals;
        private readonly Transform _left;
        private readonly Transform _right;
        private readonly Renderer _glow;
        private readonly TMP_Text _label;
        private float _progress;
        private bool _open;

        internal CorridorDoor(CorridorVisuals visuals, Transform root)
        {
            _visuals = visuals;
            Transform frame = CorridorVisuals.Group(root,"Corridor door",Position);
            Color32 door = new Color32(34,56,69,255);
            _left = visuals.Shape(frame,"Left leaf",new Vector3(-.75f,1.8f,0),new Vector3(1.5f,3.6f,.16f),door,collider:true).transform;
            _right = visuals.Shape(frame,"Right leaf",new Vector3(.75f,1.8f,0),new Vector3(1.5f,3.6f,.16f),door,collider:true).transform;
            _glow = visuals.Shape(frame,"Threshold glow",new Vector3(0,.065f,.18f),
                new Vector3(3,.13f,.32f),(Color)visuals.Theme.Amber*.3f);
            _label = visuals.Label(frame,CorridorDialogue.DoorClosed,new Vector3(0,3.94f,0),.2f,4,visuals.Theme.Dim);
        }

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
            _left.localPosition = new Vector3(-.75f-1.45f*offset,1.8f,0);
            _right.localPosition = new Vector3(.75f+1.45f*offset,1.8f,0);
            _visuals.Paint(_glow,(Color)_visuals.Theme.Amber*Mathf.Lerp(.3f,2.5f,offset));
        }
    }
}
