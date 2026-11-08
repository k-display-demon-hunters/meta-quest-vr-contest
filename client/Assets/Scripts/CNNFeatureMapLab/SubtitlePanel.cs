using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class SubtitlePanel
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private TMP_Text _header;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private WorldUIButtonController _previous;
        [SerializeField] private WorldUIButtonController _next;
        private Vector3 _targetHeading;
        private Vector3 _fromHeading;
        private Vector3 _displayHeading;
        private float _motion;
        private bool _placed;
        private Vector3 _displayScale;


        internal void Bind(CorridorVisuals visuals, Action previous, Action next)
        {
            _displayScale = _root.localScale;
            visuals.Listen(_previous.GetComponent<Button>(), previous);
            visuals.Listen(_next.GetComponent<Button>(), next);
        }

        private SubtitlePanel() { }

#if UNITY_EDITOR
        internal SubtitlePanel(CorridorVisuals visuals, Transform parent, Action previous, Action next)
        {
            var canvasObject = new GameObject("Corridor subtitles",typeof(RectTransform),typeof(Canvas),
                typeof(TrackedDeviceGraphicRaycaster));
            _root = (RectTransform)canvasObject.transform;
            _root.SetParent(parent,false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _root.sizeDelta = new Vector2(1300,480);
            _root.localScale = Vector3.one * .001f;
            _root.localPosition = new Vector3(0, 1.2f, -9);
            Color cyan = visuals.Theme.Cyan;
            Color edge = cyan;
            edge.a = .22f;
            AddBackdrop(_root,visuals.RoundedSprite(),_root.sizeDelta,edge);
            AddBackdrop(_root,visuals.RoundedSprite(),_root.sizeDelta-new Vector2(5,5),
                new Color(5/255f,8/255f,15/255f,.86f));
            _header = visuals.CanvasText(_root,"Heading",new Vector2(0,193),new Vector2(1180,54),32,visuals.Theme.Amber);
            _body = visuals.CanvasText(_root,"Body",new Vector2(0,18),new Vector2(1190,285),40,visuals.Theme.Ink);
            _previous = visuals.Button(_root,CorridorDialogue.Previous,new Vector3(-250,-178,0),new Vector2(310,74),previous,nested:true);
            _next = visuals.Button(_root,CorridorDialogue.Next,new Vector3(250,-178,0),new Vector2(310,74),next,primary:true,nested:true);
            Color ink = visuals.Theme.Void;
            _next.ApplyPalette(cyan,Color.Lerp(cyan,Color.white,.28f),Color.Lerp(cyan,ink,.14f),ink,cyan);
            // The previous button retains a transparent fill and a permanent cyan edge.
            _previous.ApplyPalette(new Color(.06f,.14f,.18f,.12f),new Color(.09f,.28f,.29f,.42f),
                new Color(.12f,.4f,.4f,.65f),visuals.Theme.Ink,cyan,true);
        }
#endif

#if UNITY_EDITOR
        private static void AddBackdrop(RectTransform parent, Sprite sprite, Vector2 size, Color color)
        {
            var panel = new GameObject("Backdrop",typeof(RectTransform),typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(parent,false);
            rect.sizeDelta = size;
            Image image = panel.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
        }
#endif

        internal void Show(string header, string body, bool previous, string next)
        {
            _header.text = header;
            _body.text = body;
            _previous.gameObject.SetActive(previous);
            _next.gameObject.SetActive(next != null);
            if (next != null) { _next.SetText(next); }
        }

        internal void Track(Camera camera, float delta)
        {
            Transform view = camera.transform;
            Vector3 heading = view.forward;
            if (!_placed)
            {
                _targetHeading = heading;
                _fromHeading = heading;
                _displayHeading = heading;
                _motion = 1;
                _placed = true;
            }
            else if (Vector3.Angle(heading,_targetHeading) > 35)
            {
                _fromHeading = _displayHeading;
                _targetHeading = heading;
                _motion = 0;
            }

            _motion = Mathf.Min(1,_motion+delta*2);
            _displayHeading = Vector3.Slerp(_fromHeading,_targetHeading,Mathf.SmoothStep(0,1,_motion));
            float downwardAngle = camera.stereoEnabled ? 25 : Mathf.Clamp(camera.fieldOfView*.5f-11,10,25);
            Vector3 right = Vector3.Cross(Vector3.up,_displayHeading).normalized;
            if (right.sqrMagnitude < .01f) { right = view.right; }
            Vector3 offset = Quaternion.AngleAxis(downwardAngle,right)*_displayHeading*1.6f;
            _root.SetPositionAndRotation(view.position+offset,Quaternion.LookRotation(offset,Vector3.up));
            Vector3 parentScale = _root.parent.lossyScale;
            _root.localScale = new Vector3(_displayScale.x/parentScale.x,_displayScale.y/parentScale.y,_displayScale.z/parentScale.z);
        }

        internal void SetVisible(bool visible) => _root.gameObject.SetActive(visible);
    }
}
