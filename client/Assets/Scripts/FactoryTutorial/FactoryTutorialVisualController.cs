using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using XR;

namespace FactoryTutorial
{
    public sealed class FactoryTutorialVisualController : MonoBehaviour
    {
        private Transform _leftRoot;
        private Transform _rightRoot;
        private GameObject _tablet;
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
            _leftRoot = leftRoot;
            leftRoot.SetParent(transform, false);
            leftRoot.localPosition = new Vector3(-0.95f, -0.1f, -0.7f);
            leftRoot.localRotation = Quaternion.Euler(-25, 0, 0);
            leftRoot.localScale = Vector3.one * 2.8f;
            Transform rightRoot = new GameObject("Right controller enlarged").transform;
            _rightRoot = rightRoot;
            rightRoot.SetParent(transform, false);
            rightRoot.localPosition = new Vector3(-0.45f, -0.1f, -0.7f);
            rightRoot.localRotation = Quaternion.Euler(-25, 0, 0);
            rightRoot.localScale = Vector3.one * 2.8f;
            if (rig != null)
            {
                OVRControllerHelper leftVisual = rig.CreateControllerVisual(leftRoot, true);
                _left = leftVisual.gameObject.AddComponent<TouchPlusHandController>();
                _left.Initialize(true, leftVisual.m_modelMetaTouchPlusLeftController.transform);
                OVRControllerHelper rightVisual = rig.CreateControllerVisual(rightRoot, false);
                _right = rightVisual.gameObject.AddComponent<TouchPlusHandController>();
                _right.Initialize(false, rightVisual.m_modelMetaTouchPlusRightController.transform);
            }
            GameObject tablet = new GameObject("Tablet tutorial graphic", typeof(RectTransform), typeof(Canvas));
            _tablet = tablet;
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
            _tabletTitle = Label(tablet.transform, font, 125, "태블릿 · 진행률과 앱");
            _rows = new[] { Label(tablet.transform, font, 45, "지도 · 내 주변 보기"),
                Label(tablet.transform, font, -25, "학습 단계 · 네 체험으로 이동"),
                Label(tablet.transform, font, -95, "양피지 · 배운 원리 다시 보기") };
            _status = Label(tablet.transform, font, -640, "");
            _status.rectTransform.sizeDelta = new Vector2(1100, 100);
        }

        public void ShowPage(int page)
        {
            _page = page;
            int cue = page == 2 || page == 6 ? 5 : page == 1 ? 4
                : page == 3 ? 3 : page == 0 || page == 4 || page == 7 ? 0
                : page == 8 ? 1 : page == 9 ? 2 : -1;
            bool left = page >= 6 && page <= 9;
            _left?.SetCue(page == 2 || left ? cue : -1);
            _right?.SetCue(left ? -1 : cue);
            _leftRoot.localRotation = Quaternion.Euler(-25, 0, 0);
            _rightRoot.localRotation = Quaternion.Euler(-25, page == 3 ? 65 : 0, 0);
            _status.text = page == 0 ? "A 버튼으로 조작 배우기" : cue < 0 ? "왼손                 오른손"
                : cue == 5 ? "노란 표시의 조이스틱을 움직여 보세요" : "노란 표시의 버튼을 눌러보세요";
            _tabletTitle.text = "태블릿 · 조작 연습";
            _selection = 0;
            RefreshRows();
        }

        public void SetVisible(bool visible)
        {
            _leftRoot.gameObject.SetActive(visible);
            _rightRoot.gameObject.SetActive(visible);
            _tablet.SetActive(visible);
            enabled = visible;
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
            if (_page >= 6 && _page <= 9 && trigger > 0.5f && stick.magnitude > 0.5f && _stickCentered)
            {
                _selection = (_selection + (stick.y > 0 ? 2 : 1)) % _rows.Length;
                _stickCentered = false;
                RefreshRows();
                _status.text = "항목이 이동했어요. X로 선택할 수 있어요.";
            }
            if (_page >= 6 && _page <= 9 && x && !_lastX) { _tabletTitle.text = "선택: " + _rows[_selection].text; _status.text = "X · 선택했어요"; }
            if (_page >= 6 && _page <= 9 && y && !_lastY) { _tabletTitle.text = "태블릿 · 조작 연습 그림"; _status.text = "Y · 뒤로 돌아왔어요"; }
            if (_page >= 6 && _page <= 9 && menu && !_lastMenu) { _tabletTitle.text = "설정 · 밝기 / 음량"; _status.text = "Menu · 실제 태블릿에서도 설정이 열려요"; }
            _lastX = x; _lastY = y; _lastMenu = menu;
            if (_page >= 1 && _page <= 4 && ((_left != null && _left.CuePerformed) || (_right != null && _right.CuePerformed)))
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
