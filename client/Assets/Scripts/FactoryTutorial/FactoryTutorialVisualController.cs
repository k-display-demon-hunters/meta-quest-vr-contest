using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using XR;

namespace FactoryTutorial
{
    public sealed class FactoryTutorialVisualController : MonoBehaviour
    {
        private TouchPlusHandController _left;
        private TouchPlusHandController _right;
        private Text _status;
        private Text _tabletTitle;
        private Text[] _rows;
        private int _page;
        private int _selection;
        private bool _stickCentered = true;
        private bool _lastX;
        private bool _lastY;
        private bool _lastMenu;

        public void Initialize(PersistentXRPlayerRig rig, Font font)
        {
            Transform leftRoot = new GameObject("Left controller enlarged").transform;
            leftRoot.SetParent(transform, false);
            leftRoot.localPosition = new Vector3(-0.95f, -0.1f, -0.7f);
            leftRoot.localRotation = Quaternion.Euler(-25, 0, 0);
            leftRoot.localScale = Vector3.one * 2.8f;
            Transform rightRoot = new GameObject("Right controller enlarged").transform;
            rightRoot.SetParent(transform, false);
            rightRoot.localPosition = new Vector3(-0.45f, -0.1f, -0.7f);
            rightRoot.localRotation = Quaternion.Euler(-25, 0, 0);
            rightRoot.localScale = Vector3.one * 2.8f;
            if (rig != null)
            {
                _left = rig.CreateControllerVisual(leftRoot, true);
                _right = rig.CreateControllerVisual(rightRoot, false);
            }
            GameObject tablet = new GameObject("Tablet tutorial graphic", typeof(RectTransform), typeof(Canvas));
            tablet.transform.SetParent(transform, false);
            tablet.transform.localPosition = new Vector3(-0.75f, 0.32f, -0.05f);
            tablet.transform.localScale = Vector3.one * 0.001f;
            tablet.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            tablet.GetComponent<Canvas>().worldCamera = Camera.main;
            tablet.GetComponent<Canvas>().overrideSorting = true;
            tablet.GetComponent<Canvas>().sortingOrder = 10;
            GameObject face = new GameObject("Tablet screen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            face.transform.SetParent(tablet.transform, false);
            face.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 340);
            face.GetComponent<Image>().color = new Color(.035f, .055f, .08f);
            face.GetComponent<Image>().raycastTarget = false;
            _tabletTitle = Label(tablet.transform, font, 125, "태블릿 · 미션 보조 도구");
            _rows = new[] { Label(tablet.transform, font, 45, "현재 미션 · 무엇을 할까요?"),
                Label(tablet.transform, font, -25, "지도 · 어디로 갈까요?"),
                Label(tablet.transform, font, -95, "양피지 · 배운 원리 다시 보기") };
            _status = Label(tablet.transform, font, -820, "");
            _status.rectTransform.sizeDelta = new Vector2(1100, 100);
        }

        public void ShowPage(int page)
        {
            _page = page;
            int cue = page == 4 || page == 9 ? 5 : page == 5 || page == 8 ? 4
                : page == 6 ? 3 : page == 7 || page == 10 ? 0 : page == 11 ? 1 : page == 12 ? 2 : -1;
            bool left = page == 4 || page >= 8;
            _left?.SetCue(left ? cue : -1);
            _right?.SetCue(left ? -1 : cue);
            if (_left != null) { _left.transform.parent.localRotation = Quaternion.Euler(-25, 0, 0); }
            if (_right != null) { _right.transform.parent.localRotation = Quaternion.Euler(-25, page == 6 ? 65 : 0, 0); }
            _status.text = cue < 0 ? "왼손                 오른손" : "노란 표시의 버튼을 눌러보세요";
            _tabletTitle.text = "태블릿 · 조작 연습 그림";
            _selection = page == 2 ? 1 : page == 3 ? 2 : 0;
            RefreshRows();
        }

        private void Update()
        {
            if (_rows == null) { return; }
            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            device.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
            device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick);
            device.TryGetFeatureValue(CommonUsages.primaryButton, out bool x);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool y);
            device.TryGetFeatureValue(CommonUsages.menuButton, out bool menu);
            if (stick.magnitude < 0.2f) { _stickCentered = true; }
            if (_page >= 8 && trigger > 0.5f && stick.magnitude > 0.5f && _stickCentered)
            {
                _selection = (_selection + (stick.y > 0 ? 2 : 1)) % _rows.Length;
                _stickCentered = false;
                RefreshRows();
                _status.text = "항목이 이동했어요. X로 선택할 수 있어요.";
            }
            if (_page >= 8 && x && !_lastX) { _tabletTitle.text = "선택: " + _rows[_selection].text; _status.text = "X · 선택했어요"; }
            if (_page >= 8 && y && !_lastY) { _tabletTitle.text = "태블릿 · 조작 연습 그림"; _status.text = "Y · 뒤로 돌아왔어요"; }
            if (_page >= 8 && menu && !_lastMenu) { _tabletTitle.text = "설정 · 밝기 / 음량"; _status.text = "Menu · 실제 태블릿에서도 설정이 열려요"; }
            _lastX = x; _lastY = y; _lastMenu = menu;
            if (_page >= 4 && _page <= 8 && ((_left != null && _left.CuePerformed) || (_right != null && _right.CuePerformed)))
            {
                _status.text = "입력이 확인됐어요! 다음으로 진행하세요.";
            }
        }

        private void RefreshRows()
        {
            for (int i = 0; i < _rows.Length; i++) { _rows[i].color = i == _selection ? new Color(1, .8f, .2f) : Color.white; }
        }

        private static Text Label(Transform parent, Font font, float y, string caption)
        {
            GameObject obj = new GameObject(caption, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.rectTransform.anchoredPosition = new Vector2(0, y);
            text.rectTransform.sizeDelta = new Vector2(650, 70);
            text.font = font; text.fontSize = 35; text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white; text.raycastTarget = false; text.text = caption;
            return text;
        }
    }
}
