using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace KdyIntro
{
    public sealed class KdyIntroManager : MonoBehaviour
    {
        [SerializeField, Range(1, 3)] private int _sceneNumber = 1;
        [SerializeField] private Text _heading;
        [SerializeField] private Text _subtitle;
        [SerializeField] private Text _continueLabel;
        [SerializeField] private KdyRigController _rig;
        [SerializeField] private KdyTabletController _tablet;
        [SerializeField] private KdyWraithController _wraith;
        [SerializeField] private GameObject _corruption;
        private KdyTransitionController _transition;
        private int _page;
        private bool _tabletPracticed;
        private bool _completed;
        private bool _ready;
        private float _storyStarted;
        public bool IsTransitioning => _transition != null && _transition.IsLoading;

        private void Awake()
        {
            foreach (KdyActionController action in GetComponentsInChildren<KdyActionController>(true))
            {
                action.Initialize(this);
            }
            _tablet.Initialize(_sceneNumber);
            _tablet.TabChanged += HandleTabChanged;
            if (_wraith != null)
            {
                _wraith.BindCorruption(_corruption);
            }
            RefreshNarrative();
        }

        private IEnumerator Start()
        {
            // Let the persistent rig discard duplicates and the simulator initialize its poses first.
            yield return null;
            _rig.BindRig(_sceneNumber == 1);
            if (!_rig.Ready || _rig.ViewCamera == null)
            {
                Debug.LogError("[KdyIntro] XRPlayerRig is missing.", this);
                yield break;
            }
            Transform rigRoot = _rig.ViewCamera.transform.root;
            _transition = rigRoot.GetComponentInChildren<KdyTransitionController>();
            if (_transition == null)
            {
                GameObject fade = new GameObject("Kdy Scene Transition", typeof(RectTransform));
                fade.transform.SetParent(rigRoot, false);
                _transition = fade.AddComponent<KdyTransitionController>();
                _transition.Initialize(_rig.ViewCamera);
            }
            _storyStarted = Time.time;
            _ready = true;
        }

        public void Activate(KdyIntroAction action)
        {
            if (!_ready || IsTransitioning)
            {
                return;
            }
            switch (action)
            {
                case KdyIntroAction.PreviousTab:
                    _tablet.ChangeTab(-1);
                    break;
                case KdyIntroAction.NextTab:
                    _tablet.ChangeTab(1);
                    break;
                case KdyIntroAction.RestartStory:
                    _page = 0;
                    _storyStarted = Time.time;
                    _wraith?.Restart();
                    RefreshNarrative();
                    break;
                case KdyIntroAction.RestartIntro:
                    _transition.Load("kdy_scene1");
                    break;
                case KdyIntroAction.EnterCnn:
                    EnterCnn();
                    break;
                default:
                    Continue();
                    break;
            }
        }

        private void Continue()
        {
            if (_sceneNumber == 1)
            {
                _transition.Load("kdy_scene2");
            }
            else if (_sceneNumber == 2)
            {
                if (Time.time - _storyStarted < 7f)
                {
                    _subtitle.text = "악령이 핵심 장치를 파괴하고 있어요! 잠시 공장을 살펴보세요.";
                    return;
                }
                if (_page == 0)
                {
                    _page = 1;
                    RefreshNarrative();
                }
                else
                {
                    _transition.Load("kdy_scene3");
                }
            }
            else
            {
                EnterCnn();
            }
        }

        private void EnterCnn()
        {
            if (!_tabletPracticed)
            {
                _subtitle.text = "먼저 태블릿 탭을 한 번 바꿔 보세요. 오른손으로 화살표를 선택하거나 왼손 X / Y 버튼을 눌러요.";
                return;
            }
            if (KdyTransitionController.CanLoad("kdy_scene4"))
            {
                _transition.Load("kdy_scene4");
                return;
            }
            _completed = true;
            _heading.text = "조작 준비 완료";
            _subtitle.text = "이동 · 선택 · 태블릿 사용을 익혔어요.\n다음 체험은 CNN실에서 이어집니다.";
            _continueLabel.text = "조작 준비 완료";
        }

        private void RefreshNarrative()
        {
            if (_sceneNumber == 1)
            {
                _heading.text = "뭉개진 세상을 구해라!";
                _subtitle.text = "세상의 해상도 높이기\nCNN 화질 개선 공장에서 시작하는 헌터스의 임무";
                _continueLabel.text = "GAME START";
            }
            else if (_sceneNumber == 2)
            {
                _heading.text = "긴급 상황 발생!";
                _subtitle.text = _page == 0 ?
                    "세상의 사진과 영상을 선명하게 만들어 주던 AI 화질 개선 공장이 정체불명의 악령에게 습격당했습니다." :
                    "핵심 장치가 고장 나면서 세상의 이미지가 뭉개져 버렸습니다! 헌터스, 공장을 복구해 세상을 다시 선명하게 되돌려 주세요!";
                _continueLabel.text = _page == 0 ? "다음" : "임무 시작";
            }
            else
            {
                _heading.text = "공장 · 조작 안내";
                _subtitle.text = "왼쪽 스틱으로 이동하고, 오른손으로 선택하거나 잡아요.\n왼손의 태블릿에는 진행도 · 공장 맵 · 양피지가 표시됩니다. 탭을 한 번 바꿔 보세요!";
                _continueLabel.text = "CNN실 입장";
            }
        }

        public void ReportGrabPractice(bool released)
        {
            if (_sceneNumber == 3 && !_completed)
            {
                _subtitle.text = released ?
                    "잡기 성공! Grip을 놓으면 큐브가 연습대로 돌아옵니다.\n태블릿 탭도 바꾸고 정면의 CNN실 문을 선택해 보세요." :
                    "오른손을 움직이면 큐브도 함께 움직여요.\n손목을 돌려 보고, Grip을 놓아 큐브를 내려놓으세요.";
            }
        }

        private void HandleTabChanged()
        {
            if (_sceneNumber == 3 && !_completed)
            {
                _tabletPracticed = true;
                _subtitle.text = "좋아요! 정면의 CNN실 문을 오른손으로 가리키고 Trigger를 눌러요.\n옆의 큐브는 Grip으로 잡아 움직여 볼 수 있어요.";
            }
        }

        private void OnDestroy()
        {
            if (_tablet != null)
            {
                _tablet.TabChanged -= HandleTabChanged;
            }
        }
    }
}
