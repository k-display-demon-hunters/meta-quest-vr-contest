using System.Collections;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace FactoryIntro
{
    public sealed class FactoryIntroManager : MonoBehaviour
    {
        private enum IntroStage { Title, Story, Loading, Retry }
        [SerializeField] private FactoryIntroPresentationController _presentation;
        [SerializeField] private FactoryIntroCinematicController _cinematic;
        [SerializeField] private Transform _exteriorStart;
        [SerializeField] private string _nextSceneName = "FactoryEnvironment";
        private IntroStage _stage;
        private XROrigin _rig;
        private FactoryIntroInputController _input;
        private SceneTransitionController _transition;
        private float _ambientTime;
        private bool _isReady;

        private void Start()
        {
            _rig = FindFirstObjectByType<XROrigin>();
            if (_rig == null || _rig.Camera == null)
            {
                _presentation.Show("XR 리그를 찾을 수 없습니다", "공통 XRPlayerRig 연결을 확인해 주세요.", "", false);
                return;
            }
            float height = Vector3.Dot(_rig.Camera.transform.position - _rig.Origin.transform.position, _rig.Origin.transform.up);
            _rig.MatchOriginUpCameraForward(Vector3.up, _exteriorStart.forward);
            _rig.MoveCameraToWorldLocation(_exteriorStart.position + Vector3.up * height);
            _input = GetComponent<FactoryIntroInputController>();
            if (_input == null)
            {
                _input = gameObject.AddComponent<FactoryIntroInputController>();
            }
            _input.Bind(_rig);
            _presentation.BindPlayer(_rig);
            if (_cinematic == null || !_cinematic.IsReady)
            {
                _presentation.Show("오프닝 연결을 확인해 주세요", "마을과 공장 연출이 연결되지 않았습니다.", "", false);
                return;
            }
            _cinematic.BindCamera(_rig.Camera);
            _cinematic.ResetOpening();
            _isReady = true;
            _stage = IntroStage.Title;
            _presentation.Show("뭉개진 세상을 구해라!", "선명한 일상을 지키는 CNN 화질 개선 공장\n\n시작하기를 누르면 마을의 이야기가 펼쳐집니다.", "시작하기", true);
        }

        private void Update()
        {
            if (_isReady && _stage == IntroStage.Title)
            {
                _ambientTime += Time.deltaTime;
                _cinematic.EvaluateIdle(_ambientTime);
            }
        }

        public void Continue()
        {
            if (!_isReady)
            {
                return;
            }
            if (_stage == IntroStage.Title)
            {
                _stage = IntroStage.Story;
                StartCoroutine(PlayStory());
            }
            else if (_stage == IntroStage.Retry)
            {
                StartCoroutine(EnterFactory());
            }
        }

        private IEnumerator PlayStory()
        {
            _presentation.BeginCinematic();
            _input.SetUIVisible(false);
            _cinematic.BeginOpening(_ambientTime);
            float elapsed = 0f;
            while (elapsed < FactoryIntroCinematicController.Duration)
            {
                _cinematic.Evaluate(elapsed);
                yield return null;
                elapsed += Time.deltaTime;
            }
            _cinematic.Evaluate(FactoryIntroCinematicController.Duration);
            yield return EnterFactory();
        }

        private IEnumerator EnterFactory()
        {
            _stage = IntroStage.Loading;
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
            // The transition lives on the persistent rig across the Single scene load.
            while (_transition != null && _transition.IsLoading)
            {
                yield return null;
            }
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
            _presentation.Show("공장을 열지 못했습니다", "씬 등록 상태를 확인한 뒤 다시 시도해 주세요.", "다시 시도", true);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _presentation.ReleasePlayer();
            _input?.Release();
        }
    }
}
