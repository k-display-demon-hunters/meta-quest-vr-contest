using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryIntro
{
    public sealed class FactoryIntroManager : MonoBehaviour
    {
        private enum IntroStage { Title, Introduction, Attack, Mission, Tutorial, Ready, Loading }

        [SerializeField] private FactoryIntroPresentationController _presentation;
        [SerializeField] private FactoryTutorialController _tutorial;
        [SerializeField] private Transform _exteriorStart;
        [SerializeField] private Transform _interiorStart;
        [SerializeField] private string _nextSceneName = "FactoryEnvironment";

        private IntroStage _stage;
        private XROrigin _rig;
        private FactoryIntroInputController _input;

        private void OnEnable()
        {
            _tutorial.OnStepChanged += HandleTutorialStep;
            _tutorial.OnInstructionChanged += HandleInstruction;
            _tutorial.OnCompleted += HandleTutorialCompleted;
            _presentation.OnSkipRequested += PauseTutorialForSkip;
            _presentation.OnSkipCancelled += ResumeTutorialAfterSkip;
            _presentation.OnSkipConfirmed += SkipTutorial;
        }

        private void Start()
        {
            // PersistentXRPlayerRig removes duplicate rigs in Awake, before this lookup.
            _rig = FindFirstObjectByType<XROrigin>();
            if (_rig == null || _rig.Camera == null)
            {
                _presentation.Show("XR 리그를 찾을 수 없습니다", "공통 XRPlayerRig 프리팹 연결을 확인해 주세요.", "", false);
                return;
            }
            PlacePlayer(_exteriorStart);
            _input = GetComponent<FactoryIntroInputController>();
            if (_input == null)
            {
                _input = gameObject.AddComponent<FactoryIntroInputController>();
            }
            _input.Bind(_rig);
            _presentation.BindPlayer(_rig);
            _presentation.ResetFactory();
            _presentation.ShowTutorialChrome(false);
            _stage = IntroStage.Title;
            _presentation.Show("뭉개진 세상을 구해라!", "세상의 해상도 높이기\n\n오른손 레이로 버튼을 가리키고 Trigger를 눌러 시작하세요.", "시작하기", true);
        }

        public void Continue()
        {
            if (_rig == null)
            {
                return;
            }
            switch (_stage)
            {
                case IntroStage.Title:
                    _stage = IntroStage.Introduction;
                    _presentation.Show("CNN 화질 개선 공장", "이곳은 사진과 영상을 선명하게 만드는 공장입니다.\n\n오늘은 공장 속 장치를 살펴보고 CNN의 원리를 배워 볼 거예요.", "공장 둘러보기", true);
                    break;
                case IntroStage.Introduction:
                    _stage = IntroStage.Attack;
                    StartCoroutine(PlayAttack());
                    break;
                case IntroStage.Mission:
                    BeginTutorial();
                    break;
                case IntroStage.Tutorial:
                    if (_tutorial.CurrentStep == TutorialStep.Selection)
                    {
                        _tutorial.ConfirmSelection();
                    }
                    else if (_tutorial.CurrentStep == TutorialStep.Return)
                    {
                        _tutorial.ReturnToStart();
                    }
                    break;
                case IntroStage.Ready:
                    StartCoroutine(EnterFactory());
                    break;
            }
        }

        public void ResetPracticeObject()
        {
            if (_stage == IntroStage.Tutorial && _tutorial.CurrentStep == TutorialStep.Grab)
            {
                _presentation.ResetPracticeObject();
            }
        }

        private IEnumerator PlayAttack()
        {
            _presentation.Show("긴급 상황!", "정체불명의 악령이 공장의 핵심 장치를 공격하고 있습니다!", "", false);
            yield return _presentation.PlayAttack();
            _stage = IntroStage.Mission;
            _presentation.Show("공장을 복구해 주세요", "공장이 멈추면서 주변 풍경이 뭉개졌어요.\nCNN 장치를 복구해 세상을 다시 선명하게 되돌려 주세요!\n\n먼저 공장 안에서 기본 조작을 익혀 봅시다.", "공장 입장", true);
        }

        private void BeginTutorial()
        {
            _stage = IntroStage.Tutorial;
            _presentation.EnterInterior();
            PlacePlayer(_interiorStart);
            _presentation.ShowTutorialChrome(true);
            _tutorial.BeginTutorial(_rig);
            XR.LeftHandTablet tablet = _rig.GetComponentInChildren<XR.LeftHandTablet>(true);
            _presentation.PlayHologramReveal(tablet != null ? tablet.transform : null);
        }

        private void PlacePlayer(Transform destination)
        {
            float height = Vector3.Dot(_rig.Camera.transform.position - _rig.Origin.transform.position, _rig.Origin.transform.up);
            Vector3 forward = Vector3.ProjectOnPlane(destination.forward, Vector3.up).normalized;
            _rig.MatchOriginUpCameraForward(Vector3.up, forward);
            _rig.MoveCameraToWorldLocation(destination.position + Vector3.up * height);
        }

        private void HandleTutorialStep(TutorialStep step)
        {
            _input.SetTutorialStep(step);
            _presentation.SetTutorialProgress(step);
            if (step == TutorialStep.Complete)
            {
                return;
            }
            string button = step == TutorialStep.Selection ? "선택 연습" : step == TutorialStep.Return ? "원위치로 돌아가기" : "연습 진행 중";
            bool canPress = step == TutorialStep.Selection || step == TutorialStep.Return;
            _presentation.Show($"조작 연습 {(int)step + 1} / 5", _tutorial.CurrentInstruction, button, canPress);
            _presentation.ShowPractice(step);
        }

        private void HandleInstruction(string instruction)
        {
            _presentation.SetInstruction(instruction);
        }

        private void HandleTutorialCompleted()
        {
            _stage = IntroStage.Ready;
            _input.SetTutorialStep(TutorialStep.Complete);
            _presentation.ShowTutorialChrome(false);
            _presentation.HidePractice();
            _presentation.Show("조작 준비 완료", "왼손 스틱으로 이동하고, 오른손 Trigger로 선택하세요.\n물체를 잡을 때는 오른손 Grip을 사용합니다.\n\n이제 공장 탐색을 시작해 보세요.", "공장 탐색 시작", true);
        }

        private void SkipTutorial()
        {
            if (_stage != IntroStage.Tutorial)
            {
                return;
            }
            _tutorial.StopTutorial();
            HandleTutorialCompleted();
        }

        private void PauseTutorialForSkip()
        {
            if (_stage != IntroStage.Tutorial)
            {
                return;
            }
            _tutorial.SetPaused(true);
            _input.SetTutorialStep(TutorialStep.Selection);
        }

        private void ResumeTutorialAfterSkip()
        {
            if (_stage != IntroStage.Tutorial)
            {
                return;
            }
            _input.SetTutorialStep(_tutorial.CurrentStep);
            _tutorial.SetPaused(false);
        }

        private IEnumerator EnterFactory()
        {
            // Defer unloading UI until its pointer callback has returned.
            _stage = IntroStage.Loading;
            _presentation.Show("공장으로 이동합니다", "잠시만 기다려 주세요.", "", false);
            yield return null;
            if (!Application.CanStreamedLevelBeLoaded(_nextSceneName))
            {
                _stage = IntroStage.Ready;
                _presentation.Show("다음 씬을 열 수 없습니다", $"Build Settings에 {_nextSceneName} 씬을 등록해 주세요.", "다시 시도", true);
                yield break;
            }
            // Restore persistent rig input before the next scene configures its controls.
            _presentation.ReleasePlayer();
            _input.Release();
            AsyncOperation loading = SceneManager.LoadSceneAsync(_nextSceneName, LoadSceneMode.Single);
            if (loading != null)
            {
                yield return loading;
            }
            else
            {
                _input.Bind(_rig);
                _input.SetTutorialStep(TutorialStep.Complete);
                _presentation.BindPlayer(_rig);
                _stage = IntroStage.Ready;
                _presentation.Show("다음 씬을 열지 못했습니다", "다시 시도해 주세요.", "다시 시도", true);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _tutorial.OnStepChanged -= HandleTutorialStep;
            _tutorial.OnInstructionChanged -= HandleInstruction;
            _tutorial.OnCompleted -= HandleTutorialCompleted;
            _presentation.OnSkipRequested -= PauseTutorialForSkip;
            _presentation.OnSkipCancelled -= ResumeTutorialAfterSkip;
            _presentation.OnSkipConfirmed -= SkipTutorial;
            _tutorial.StopTutorial();
            _presentation.ReleasePlayer();
            _input?.Release();
        }
    }
}
