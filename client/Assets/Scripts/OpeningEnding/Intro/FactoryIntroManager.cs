using System.Collections;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace OpeningEnding.Intro
{
    public sealed class FactoryIntroManager : MonoBehaviour
    {
        private enum IntroStage { Title, World, Technology, Story, Mission, Door, Entry, Loading, Retry }
        [SerializeField] private FactoryIntroPresentationController _presentation;
        [SerializeField] private FactoryIntroCinematicController _cinematic;
        [SerializeField] private FactoryIntroEntryController _entry;
        [SerializeField] private FactoryIntroInputController _input;
        [SerializeField] private Transform _exteriorStart;
        [SerializeField] private string _nextSceneName = "FactoryEnvironment";
        private IntroStage _stage;
        private XROrigin _rig;
        private SceneTransitionController _transition;
        private float _ambientTime;
        private int _continuedFrame = -1;
        private bool _isReady;

        private void OnEnable()
        {
            _input.OnContinueRequested += Continue;
            _presentation.OnContinueRequested += Continue;
        }

        private void Start()
        {
            _rig = FindFirstObjectByType<XROrigin>();
            float height = Vector3.Dot(_rig.Camera.transform.position - _rig.Origin.transform.position, _rig.Origin.transform.up);
            _rig.MatchOriginUpCameraForward(Vector3.up, _exteriorStart.forward);
            _rig.MoveCameraToWorldLocation(_exteriorStart.position + Vector3.up * height);
            _input.Bind(_rig);
            _presentation.BindPlayer(_rig);
            _entry.BindPlayer(_rig);
            _cinematic.BindCamera(_rig.Camera);
            _cinematic.ResetOpening();
            _isReady = true;
            _stage = IntroStage.Title;
            _input.SetUIVisible(false);
            _input.EnableContinueInput(true);
            _presentation.Show("CNN 공장에 오신 것을 환영합니다",
                "오른손 <b>A 버튼</b>을 눌러 시작하세요.", "", false);
        }

        private void Update()
        {
            if (!_isReady) return;
            if (_stage == IntroStage.Title || _stage == IntroStage.World || _stage == IntroStage.Technology)
            {
                _ambientTime += Time.deltaTime;
                _cinematic.EvaluateIdle(_ambientTime);
            }
            else if (_stage == IntroStage.Entry && _entry.HasEntered)
            {
                StartCoroutine(EnterFactory());
            }
        }

        public void Continue()
        {
            // A and a UI click can arrive together; advance at most one page per frame.
            if (!_isReady || _continuedFrame == Time.frameCount) return;
            _continuedFrame = Time.frameCount;
            switch (_stage)
            {
                case IntroStage.Title:
                    _stage = IntroStage.World;
                    _input.SetUIVisible(true);
                    _presentation.Show("선명한 화면의 비밀",
                        "이 마을의 사진과 영상은\n<b>CNN 공장의 AI 업스케일링</b>으로 선명해져요.", "다음", true);
                    break;
                case IntroStage.World:
                    _stage = IntroStage.Technology;
                    _presentation.Show("작은 이미지를 더 선명하게",
                        "CNN은 학습한 이미지의 특징을 활용해\n저해상도 이미지를 고해상도로 바꿔요.", "계속", true);
                    break;
                case IntroStage.Technology:
                    _stage = IntroStage.Story;
                    _input.EnableContinueInput(false);
                    StartCoroutine(PlayStory());
                    break;
                case IntroStage.Mission:
                    _stage = IntroStage.Door;
                    _input.EnableContinueInput(false);
                    StartCoroutine(OpenEntrance());
                    break;
                case IntroStage.Retry:
                    StartCoroutine(EnterFactory());
                    break;
            }
        }

        private IEnumerator PlayStory()
        {
            _presentation.BeginCinematic();
            _input.SetUIVisible(false);
            _cinematic.BeginOpening(_ambientTime);
            float elapsed = 0f;
            bool warningShown = false;
            bool warningHidden = false;
            while (elapsed < FactoryIntroCinematicController.Duration)
            {
                _cinematic.Evaluate(elapsed);
                if (!warningShown && elapsed >= 4.3f)
                {
                    warningShown = true;
                    _presentation.ShowWarning();
                }
                if (warningShown && !warningHidden && elapsed >= 12f)
                {
                    warningHidden = true;
                    _presentation.BeginCinematic();
                }
                yield return null;
                elapsed += Time.deltaTime;
            }
            _cinematic.Evaluate(FactoryIntroCinematicController.Duration);
            _stage = IntroStage.Mission;
            _input.SetUIVisible(true);
            _input.EnableContinueInput(true);
            _presentation.Show("마을에 선명함을 되찾아 주세요",
                "공장 안의 미션을 완료해\n멈춰 버린 CNN 공장을 복구해 주세요!", "공장 수리하기", true);
        }

        private IEnumerator OpenEntrance()
        {
            _presentation.BeginCinematic();
            _input.SetUIVisible(false);
            _cinematic.PlayDoorSound();
            yield return _entry.OpenDoor();
            _stage = IntroStage.Entry;
            _entry.BeginEntry();
            _input.SetWalkingEnabled(true);
            _presentation.ShowEntryGuide();
        }

        private IEnumerator EnterFactory()
        {
            _stage = IntroStage.Loading;
            _input.EnableContinueInput(false);
            _input.SetWalkingEnabled(false);
            _entry.EndEntry();
            _presentation.BeginCinematic();
            yield return null;
            if (!SceneTransitionController.CanLoad(_nextSceneName))
            {
                ShowRetry();
                yield break;
            }
            GameObject overlay = new GameObject("Story scene transition", typeof(RectTransform));
            overlay.transform.SetParent(_rig.transform, false);
            _transition = overlay.AddComponent<SceneTransitionController>();
            _transition.Initialize(_rig.Camera);
            _presentation.ReleasePlayer();
            _input.Release();
            _transition.Load(_nextSceneName);
            while (_transition != null && _transition.IsLoading) yield return null;
            if (_transition != null)
            {
                Destroy(_transition.gameObject);
                _input.Bind(_rig);
                _presentation.BindPlayer(_rig);
                ShowRetry();
            }
        }

        private void ShowRetry()
        {
            _stage = IntroStage.Retry;
            _input.SetUIVisible(true);
            _input.EnableContinueInput(true);
            _presentation.Show("공장을 열지 못했습니다",
                "잠시 후 다시 시도해 주세요.", "다시 시도", true);
        }

        private void OnDisable()
        {
            _input.OnContinueRequested -= Continue;
            _presentation.OnContinueRequested -= Continue;
            StopAllCoroutines();
            _entry.EndEntry();
            _presentation.ReleasePlayer();
            _input.Release();
        }
    }
}
