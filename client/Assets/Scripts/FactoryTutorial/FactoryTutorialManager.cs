using System.Collections;
using Learning;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using XR;

namespace FactoryTutorial
{
    public sealed class FactoryTutorialManager : MonoBehaviour, ILearningExperience
    {
        private static readonly (string Title, string Body)[] Pages =
        {
            ("조작 시작", "오른손 A로 조작 안내를 시작해요.\n버튼은 오른손으로 가리키고\n트리거로 선택해요."),
            ("이동과 시점", "왼손 스틱으로 이동하고,\n오른손 스틱으로 시점을 돌려요.\n기울어진 시점은 오른손 A로 복원해요."),
            ("물체 잡기", "오른손으로 물체를 가리켜\n그립을 누른 채 옮기고,\n그립을 놓아 내려놓아요."),
            ("나의 미션 도우미", "왼손 X로 태블릿을 켜고 숨겨요.\n지도·진행률·양피지를 살펴보고,\n태블릿을 연 채 왼손 메뉴로 설정을 열어요."),
            ("왼손으로 태블릿 조작", "트리거+스틱: 항목 이동\n트리거+X: 선택 · 트리거+Y: 뒤로\n트리거와 스틱을 놓으면 다시 걸을 수 있어요."),
            ("체험실로 이동", "태블릿 학습 단계에서 체험실로 이동해요.\n오른손 B는 조건을 채운 지점마다 한 번 써요."),
            ("CNN 공장 복구 미션", "양피지는 마지막 [학습 완료]까지 읽어요.\n픽셀 관찰·합성곱·특징 맵·모델 제작을 마쳐요.")
        };
        private FactoryTutorialVisualController _visuals;
        [SerializeField] private FactoryTutorialPanelController _panel;
        [SerializeField] private Font _font;

        private XRPlayerInputController _playerInput;
        private bool _initialized;
        private bool _ownsRay;
        private bool _previousInputSuppressed;
        private InputAction _startGuide;
        private int _page;
        private bool _finishing;
        private int _advancedFrame = -1;
        private bool _exitAvailable;

        public bool IsComplete { get; private set; }
        public LearningStageId StageId => LearningStageId.Tutorial;
        public float Progress => IsComplete ? 1f : 0f;
        public bool CanExit => isActiveAndEnabled && IsComplete && !_ownsRay;
        public event System.Action<float> ProgressChanged;
        public event System.Action<bool> ExitAvailabilityChanged;

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
            PublishExitAvailability();
        }

        private void Start()
        {
            XROrigin rig = FindFirstObjectByType<XROrigin>();
            _playerInput = rig.GetComponent<XRPlayerInputController>();

            _panel.Initialize(_font, rig.Camera);
            _panel.ConfigureVisualGuide();
            _visuals = _panel.gameObject.AddComponent<FactoryTutorialVisualController>();
            _visuals.Initialize(rig.GetComponent<PersistentXRPlayerRig>(), _font);
            _initialized = true;
            if (isActiveAndEnabled)
            {
                SubscribePanel();
                HoldGuideRay();
            }
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
            if (_page == Pages.Length - 1)
            {
                Finish();
                return;
            }
            _page++;
            Refresh();
        }

        private void Refresh()
        {
            (string title, string body) = Pages[_page];
            if (_page == Pages.Length - 1)
            {
                _visuals.SetVisible(false);
                _panel.ShowBriefing(title, body);
                return;
            }

            string caption = _page == 0 ? "조작 배우기" : _page == Pages.Length - 2 ? "미션 시작" : "다음";
            _panel.ShowPage(_page, Pages.Length, title, body, caption);
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
            ReleaseInput();
            _panel.gameObject.SetActive(false);
            IsComplete = true;
            ProgressChanged?.Invoke(Progress);
            PublishExitAvailability();
        }

        private void PublishExitAvailability()
        {
            bool available = CanExit;
            if (_exitAvailable == available) { return; }
            _exitAvailable = available;
            ExitAvailabilityChanged?.Invoke(available);
        }

        private void HoldGuideRay()
        {
            if (_ownsRay)
            {
                return;
            }
            _playerInput.EnsureRayReady();
            _previousInputSuppressed = _playerInput.TutorialInputSuppressed;
            _playerInput.TutorialInputSuppressed = _page == 0 || _previousInputSuppressed;
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
                _playerInput.TutorialInputSuppressed = _previousInputSuppressed;
            }
            _ownsRay = false;
        }

        private void SubscribePanel()
        {
            _panel.OnPrevious += Previous;
            _panel.OnNext += Next;
        }

        public IEnumerator EndSession()
        {
            StopAllCoroutines();
            _finishing = true;
            ReleaseInput();
            _visuals.SetVisible(false);
            _panel.gameObject.SetActive(false);
            yield break;
        }

        private void ReleaseInput()
        {
            _startGuide.Disable();
            if (_initialized)
            {
                _panel.OnPrevious -= Previous;
                _panel.OnNext -= Next;
            }
            ReleaseGuideRay();
        }

        private void OnDisable()
        {
            ReleaseInput();
            if (_finishing && !IsComplete)
            {
                StopAllCoroutines();
                _finishing = false;
            }
            PublishExitAvailability();
        }

        private void OnDestroy()
        {
            _startGuide.Dispose();
        }
    }
}
