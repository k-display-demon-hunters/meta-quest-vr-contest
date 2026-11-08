using UI;
using UnityEngine;
using UnityEngine.UI;

namespace FeatureMapCorridor
{
    internal sealed class CompletionPopup
    {
        private readonly WorldUIPopupController _popup;
        private readonly Transform _parent;
        private float _time;

        internal CompletionPopup(GameObject prefab, Transform parent, CorridorVisuals visuals)
        {
            _parent = parent;
            CorridorTheme theme = visuals.Theme;
            var instance = Object.Instantiate(prefab,parent);
            foreach (Text label in instance.GetComponentsInChildren<Text>(true)) { label.font = theme.Korean; }
            _popup = instance.GetComponent<WorldUIPopupController>();
            var button = instance.GetComponentInChildren<WorldUIButtonController>();
            CorridorVisuals.EnableNestedButton(button.gameObject,3);
            var buttonRect = (RectTransform)button.transform;
            buttonRect.anchoredPosition = new Vector2(0,-122);
            buttonRect.sizeDelta = new Vector2(230,72);
            Color amber = theme.Amber;
            Color ink = theme.Void;
            button.ApplyPalette(amber,Color.Lerp(amber,Color.white,.25f),Color.Lerp(amber,ink,.15f),ink,amber);
            _popup.SetText(CorridorDialogue.Complete,CorridorDialogue.CompleteTitle,CorridorDialogue.Confirm,"");
            Image image = instance.GetComponent<Image>();
            image.sprite = visuals.RoundedSprite();
            image.type = Image.Type.Sliced;
            Text title = instance.transform.Find("Title").GetComponent<Text>();
            title.color = theme.Amber;

            image.color = new Color(10/255f,18/255f,30/255f,.96f);
            title.alignment = TextAnchor.MiddleCenter;
            title.rectTransform.anchoredPosition = new Vector2(0,126);
            title.rectTransform.sizeDelta = new Vector2(710,64);
            Text message = instance.transform.Find("Body").GetComponent<Text>();
            message.alignment = TextAnchor.MiddleCenter;
            message.fontSize = 58;
            message.fontStyle = FontStyle.Bold;
            message.rectTransform.anchoredPosition = new Vector2(0,24);
            message.rectTransform.sizeDelta = new Vector2(710,110);
            var canvas = instance.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2;
            var edge = new GameObject("Edge",typeof(RectTransform),typeof(Image));
            var edgeRect = (RectTransform)edge.transform;
            edgeRect.SetParent(instance.transform,false);
            edgeRect.SetAsFirstSibling();
            edgeRect.sizeDelta = new Vector2(800,420);
            Image border = edge.GetComponent<Image>();
            border.sprite = visuals.RoundedSprite();
            border.type = Image.Type.Sliced;
            border.color = theme.Amber;
            border.raycastTarget = false;
            var fill = new GameObject("Fill",typeof(RectTransform),typeof(Image));
            var fillRect = (RectTransform)fill.transform;
            fillRect.SetParent(instance.transform,false);
            fillRect.SetSiblingIndex(1);
            fillRect.sizeDelta = new Vector2(795,415);
            Image inset = fill.GetComponent<Image>();
            inset.sprite = visuals.RoundedSprite();
            inset.type = Image.Type.Sliced;
            inset.color = new Color(10/255f,18/255f,30/255f,.96f);
            inset.raycastTarget = false;
            image.color = Color.clear;
            _popup.Hide();
        }

        internal void Open(Camera camera)
        {
            Transform view = camera.transform;
            _popup.transform.SetPositionAndRotation(view.position+view.forward*1.5f,view.rotation);
            _time = 0;
            _popup.Show();
            Tick(0);
        }

        internal void Tick(float delta)
        {
            if (!_popup.gameObject.activeSelf || _time >= .45f) { return; }
            _time = Mathf.Min(.45f,_time+delta);
            float t = _time/.45f;
            float scale = Mathf.Lerp(.7f,1,Mathf.SmoothStep(0,1,t))+.05f*Mathf.Sin(Mathf.PI*t);
            Vector3 parent = _parent.lossyScale;
            const float width = .0009375f;
            _popup.transform.localScale = new Vector3(scale*width/parent.x,scale*width/parent.y,scale*width/parent.z);
        }
    }
}
