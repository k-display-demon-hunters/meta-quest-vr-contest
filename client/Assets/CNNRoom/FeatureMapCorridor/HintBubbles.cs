using TMPro;
using UnityEngine;

namespace FeatureMapCorridor
{
    internal sealed class HintBubbles
    {
        private const int RecapCount = 5;
        private readonly Transform _root;
        private readonly CorridorVisuals _visuals;
        private readonly Transform _hint;
        private readonly TMP_Text _caption;
        private readonly Renderer _dot;
        private readonly Transform _offscreenChip;
        private readonly TMP_Text _offscreen;
        private readonly Transform[] _recaps = new Transform[RecapCount];
        private readonly TMP_Text[,] _chips = new TMP_Text[RecapCount,6];
        private readonly Renderer[,] _chipFrames = new Renderer[RecapCount,6];
        private readonly float[] _expiration = new float[RecapCount];
        private int _next;
        private int _direction = -1;
        private static readonly string[] Directions = { "→ "+CorridorDialogue.Arrow,"↗ "+CorridorDialogue.Arrow,"↑ "+CorridorDialogue.Arrow,"↖ "+CorridorDialogue.Arrow,"← "+CorridorDialogue.Arrow,"↙ "+CorridorDialogue.Arrow,"↓ "+CorridorDialogue.Arrow,"↘ "+CorridorDialogue.Arrow };

        internal HintBubbles(CorridorVisuals visuals, Transform root)
        {
            _visuals = visuals;
            _root = root;
            _hint = CorridorVisuals.Group(root,"Guide bubble",Vector3.zero);
            visuals.FaceCamera(_hint);
            visuals.Shape(_hint,"Cyan border",new Vector3(0,0,.009f),new Vector3(2.88f,.51f,.02f),visuals.Theme.Cyan);
            visuals.Shape(_hint,"Bubble background",Vector3.zero,new Vector3(2.84f,.47f,.021f),
                new Color(8/255f,16/255f,26/255f,.94f),transparent:true);
            var tail = visuals.Shape(_hint,"Tail",new Vector3(0,-.25f,.006f),new Vector3(.13f,.13f,.018f),visuals.Theme.Cyan);
            tail.transform.localRotation = Quaternion.Euler(0,0,45);
            _dot = visuals.Shape(_hint,"Pulse",new Vector3(-1.27f,.14f,-.035f),Vector3.one*.075f,visuals.Theme.Cyan,PrimitiveType.Sphere);
            _caption = visuals.Label(_hint,"",new Vector3(0,0,-.042f),.17f,2.68f,visuals.Theme.Ink,false);
            _offscreenChip = CorridorVisuals.Group(root,"Offscreen hint",Vector3.zero);
            visuals.Shape(_offscreenChip,"Edge",new Vector3(0,0,.011f),new Vector3(.4f,.132f,.017f),visuals.Theme.Cyan);
            visuals.Shape(_offscreenChip,"Fill",Vector3.zero,new Vector3(.382f,.114f,.015f),
                new Color32(8,16,26,255));
            _offscreen = visuals.Label(_offscreenChip,"",new Vector3(0,0,-.016f),.096f,.36f,visuals.Theme.Cyan,false);
            for (int i = 0; i < RecapCount; i++)
            {
                _recaps[i] = CorridorVisuals.Group(root,"Calculation chips",Vector3.zero);
                visuals.FaceCamera(_recaps[i]);
                for (int j = 0; j < 6; j++)
                {
                    _chipFrames[i,j] = visuals.Shape(_recaps[i],"Chip",Vector3.zero,new Vector3(.66f,.27f,.02f),visuals.Theme.Amber);
                    visuals.Shape(_chipFrames[i,j].transform,"Chip background",new Vector3(0,0,-.18f),new Vector3(.93f,.82f,1),
                        new Color32(22,32,46,255));
                    _chips[i,j] = visuals.Label(_recaps[i],"",Vector3.zero,.1f,.66f,visuals.Theme.Ink,false);
                }
                _recaps[i].gameObject.SetActive(false);
            }
            Hide();
        }

        internal void Show(string text, Vector3 position)
        {
            _hint.localPosition = position;
            _caption.text = text;
            _hint.gameObject.SetActive(true);
        }

        internal void Hide()
        {
            _hint.gameObject.SetActive(false);
            _offscreenChip.gameObject.SetActive(false);
        }

        internal void AddRecap(Vector3 position, string[] sources, string result, Color32[] colors)
        {
            int index = _next++ % RecapCount;
            _recaps[index].localPosition = position;
            _recaps[index].gameObject.SetActive(true);
            int count = sources.Length+1;
            for (int i = 0; i < 6; i++)
            {
                bool visible = i < count;
                _chipFrames[index,i].gameObject.SetActive(visible);
                _chips[index,i].gameObject.SetActive(visible);
                if (!visible) { continue; }
                float x = (i-(count-1)*.5f)*.73f;
                _chipFrames[index,i].transform.localPosition = new Vector3(x,0,0);
                _chips[index,i].transform.localPosition = new Vector3(x,0,-.03f);
                _chips[index,i].text = i < sources.Length ? sources[i]+(i < sources.Length-1 ? " +" : " →") : result;
                _visuals.Paint(_chipFrames[index,i], i < sources.Length ? colors[i] : _visuals.Theme.Amber);
            }
            _expiration[index] = Time.unscaledTime+20;
        }

        internal void Tick(Camera camera, float now)
        {
            for (int i = 0; i < RecapCount; i++)
                if (_recaps[i].gameObject.activeSelf && now >= _expiration[i]) { _recaps[i].gameObject.SetActive(false); }
            if (!_hint.gameObject.activeSelf) { return; }
            _dot.transform.localScale = Vector3.one*(.064f+.014f*Mathf.Sin(now*4));
            Vector3 viewport = camera.WorldToViewportPoint(_hint.position);
            bool outside = viewport.z <= 0 || viewport.x < .08f || viewport.x > .92f || viewport.y < .1f || viewport.y > .9f;
            _offscreenChip.gameObject.SetActive(outside);
            if (!outside) { return; }
            Vector2 relative = new Vector2(viewport.x-.5f,viewport.y-.5f);
            if (viewport.z <= 0) { relative = -relative; }
            if (relative.sqrMagnitude < .0001f) { relative = Vector2.right; }
            float border = Mathf.Max(Mathf.Abs(relative.x)/.36f,Mathf.Abs(relative.y)/.36f);
            Vector2 edge = relative/border+Vector2.one*.5f;
            _offscreenChip.SetPositionAndRotation(camera.ViewportToWorldPoint(new Vector3(edge.x,edge.y,1.4f)),camera.transform.rotation);
            int direction = Mathf.RoundToInt(Mathf.Atan2(relative.y,relative.x)*4/Mathf.PI+8)%8;
            if (_direction == direction) { return; }
            _direction = direction;
            _offscreen.text = Directions[direction];
        }
    }
}
