using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using XR;

namespace FactoryTutorial
{
    public sealed class FactoryTutorialManager : MonoBehaviour
    {
        private static readonly string[] Titles =
        {
            "수리 준비부터 해볼까요?", "버튼 선택", "이동과 방향 전환", "물체 잡기", "정면 맞추기",
            "나의 미션 도우미", "태블릿 항목 이동", "X · 선택", "Y · 뒤로", "설정 열기",
            "이제 준비됐어요!"
        };
        private static readonly string[] Instructions =
        {
            "컨트롤러와 태블릿 사용법을\n먼저 알아보세요.",
            "오른손으로 버튼을 가리키고\n검지 버튼(트리거)을 누르세요.",
            "왼쪽 조이스틱으로 이동해요.\n오른쪽 조이스틱으로 돌아보세요.",
            "오른손 옆면 버튼(그립)을\n눌러 잡고, 놓아 내려놓으세요.",
            "오른손 A 버튼으로\n정면을 다시 맞추세요.",
            "태블릿에서 진행률·지도를 보고,\n학습 단계 이동과 양피지 재열람을 해요.",
            "왼손 트리거를 누른 채\n조이스틱으로 항목을 고르세요.",
            "왼손 X 버튼을 눌러\n선택한 항목을 여세요.",
            "왼손 Y 버튼을 눌러\n이전 화면으로 돌아가세요.",
            "왼손 메뉴 버튼으로\n밝기와 음량을 조절하세요.",
            "배운 조작으로\n공장 수리를 시작해 볼까요?"
        };
        private FactoryTutorialVisualController _visuals;
        [SerializeField] private FactoryTutorialPanelController _panel;
        [SerializeField] private Font _font;

        private XRPlayerInputController _playerInput;
        private bool _initialized;
        private bool _ownsRay;
        private bool _previousForcedRay;
        private bool _previousInputSuppressed;
        private InputAction _startGuide;
        private int _page;
        private bool _finishing;
        private bool _showingMission;
        private int _advancedFrame = -1;

        public bool IsComplete { get; private set; }

        private void Awake()
        {
            _startGuide = new InputAction("Start control guide", InputActionType.Button,
                "<XRController>{RightHand}/primaryButton");
        }

        private void OnEnable()
        {
            _startGuide.Enable();
            if (_initialized && !IsComplete)
            {
                SubscribePanel();
                HoldGuideRay();
            }
        }

        public void Initialize(XROrigin rig, Transform entrance)
        {
            if (_initialized)
            {
                return;
            }
            _playerInput = rig.GetComponent<XRPlayerInputController>();

            _panel.transform.SetPositionAndRotation(
                entrance.position + entrance.forward * 3f + Vector3.up * 1.65f, entrance.rotation);
            _panel.Initialize(_font, rig.Camera);
            _panel.ConfigureVisualGuide();
            _visuals = _panel.gameObject.AddComponent<FactoryTutorialVisualController>();
            _visuals.Initialize(rig.GetComponent<PersistentXRPlayerRig>(), _font);
            _initialized = true;
            SubscribePanel();
            HoldGuideRay();
            Refresh();
        }

        private void Update()
        {
            if (_initialized && !_finishing && !IsComplete && _page == 0 &&
                _startGuide.WasPressedThisFrame())
            {
                Next();
            }
        }

        private void Previous()
        {
            if (_finishing || IsComplete)
            {
                return;
            }
            _page = Mathf.Max(0, _page - 1);
            Refresh();
        }

        private void Next()
        {
            if (_finishing || IsComplete || _advancedFrame == Time.frameCount)
            {
                return;
            }
            _advancedFrame = Time.frameCount;
            if (_showingMission)
            {
                Finish();
                return;
            }
            if (_page == Titles.Length - 1)
            {
                _showingMission = true;
                _visuals.SetVisible(false);
                _panel.ShowBriefing("CNN 공장 복구 미션",
                    "양피지에서 원리를 배우고,\n세 실습실의 미션을 완료하세요!");
                return;
            }
            _page++;
            Refresh();
        }

        private void Refresh()
        {
            string caption = _page == 0 ? "조작 배우기" : _page == Titles.Length - 1 ? "미션 시작" : "다음";
            _panel.ShowPage(_page, Titles.Length, Titles[_page], Instructions[_page], caption);
            _visuals.ShowPage(_page);
        }

        private void LateUpdate()
        {
            // Do not let the A press that dismisses the first page also recenter this frame.
            if (_ownsRay)
                _playerInput.TutorialInputSuppressed = _page == 0 || _previousInputSuppressed;
        }

        private void Finish()
        {
            if (_finishing || IsComplete)
            {
                return;
            }
            _finishing = true;
            StartCoroutine(FinishAfterClick());
        }

        private IEnumerator FinishAfterClick()
        {
            // Let the XR UI callback finish before the factory unloads this scene.
            yield return null;
            ReleaseGuideRay();
            _panel.gameObject.SetActive(false);
            IsComplete = true;
        }

        private void HoldGuideRay()
        {
            if (_ownsRay)
            {
                return;
            }
            _playerInput.EnsureRayReady();
            _previousForcedRay = _playerInput.TutorialRayForcedOn;
            _previousInputSuppressed = _playerInput.TutorialInputSuppressed;
            _playerInput.TutorialInputSuppressed = _page == 0 || _previousInputSuppressed;
            _playerInput.TutorialRayForcedOn = true;
            _ownsRay = true;
        }

        private void ReleaseGuideRay()
        {
            if (!_ownsRay)
            {
                return;
            }
            if (_playerInput != null)
            {
                _playerInput.TutorialRayForcedOn = _previousForcedRay;
                _playerInput.TutorialInputSuppressed = _previousInputSuppressed;
            }
            _ownsRay = false;
        }

        private void SubscribePanel()
        {
            _panel.OnPrevious += Previous;
            _panel.OnNext += Next;
        }

        private void OnDisable()
        {
            _startGuide.Disable();
            if (_initialized)
            {
                _panel.OnPrevious -= Previous;
                _panel.OnNext -= Next;
            }
            ReleaseGuideRay();
            if (_finishing && !IsComplete)
            {
                StopAllCoroutines();
                _finishing = false;
            }
        }

        private void OnDestroy()
        {
            _startGuide.Dispose();
        }
    }
}
