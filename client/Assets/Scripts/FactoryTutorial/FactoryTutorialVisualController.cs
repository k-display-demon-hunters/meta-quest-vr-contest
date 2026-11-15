using System;
using UnityEngine;
using UnityEngine.UI;
using XR;
using static FactoryTutorial.TutorialControl;

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
        private int _page;

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
            face.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 160);
            face.GetComponent<Image>().color = new Color(.035f, .055f, .08f);
            face.GetComponent<Image>().raycastTarget = false;
            Label(tablet.transform, font, 36, "태블릿 목차");
            Label(tablet.transform, font, -32, "지도 · 학습 단계 · 양피지");
            _status = Label(tablet.transform, font, -180, "");
            _status.rectTransform.sizeDelta = new Vector2(1100, 100);
            _status.fontSize = 44;
        }

        public void ShowPage(int page)
        {
            _page = page;
            _left?.SetCues(page switch
            {
                1 => new[] { Stick },
                3 => new[] { PrimaryButton, MenuButton },
                4 => new[] { Trigger, Stick, PrimaryButton, SecondaryButton },
                _ => Array.Empty<TutorialControl>()
            });
            _right?.SetCues(page switch
            {
                0 => new[] { PrimaryButton, Trigger },
                1 => new[] { Stick, PrimaryButton },
                2 => new[] { Grip },
                5 => new[] { SecondaryButton },
                _ => Array.Empty<TutorialControl>()
            });
            _leftRoot.localRotation = Quaternion.Euler(-25, 0, 0);
            _rightRoot.localRotation = Quaternion.Euler(-25, page == 2 ? 65 : 0, 0);
            _status.text = page switch
            {
                0 => "A 버튼으로 조작 배우기",
                1 or 4 => "노란 표시의 스틱과 버튼을 조작해 보세요",
                _ => "노란 표시의 버튼을 눌러보세요"
            };
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
            if ((_page is 1 or 2) && ((_left != null && _left.CuePerformed) || (_right != null && _right.CuePerformed)))
            {
                _status.text = "입력이 확인됐어요! 다음으로 진행하세요.";
            }
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
