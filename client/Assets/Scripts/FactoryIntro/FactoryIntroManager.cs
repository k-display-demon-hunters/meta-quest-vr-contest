using System.Collections;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryIntro
{
    public sealed class FactoryIntroManager : MonoBehaviour
    {
        private enum IntroStage { Title, Story, Loading, Retry }
        [SerializeField] private FactoryIntroPresentationController _presentation;
        [SerializeField] private Transform _exteriorStart;
        [SerializeField] private string _nextSceneName = "FactoryEnvironment";
        private IntroStage _stage;
        private XROrigin _rig;
        private FactoryIntroInputController _input;
        private SceneTransitionController _transition;

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
            _presentation.ResetFactory();
            _stage = IntroStage.Title;
            _presentation.Show("뭉개진 세상을 구해라!", "CNN 화질 개선 공장\n\n오른손 ray로 시작하기를 가리키고 Trigger를 누르세요.", "시작하기", true);
        }

        public void Continue()
        {
            if (_rig == null)
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
            _presentation.Show("CNN 화질 개선 공장", "사진과 영상을 선명하게 만들던 공장에\n정체불명의 악령이 나타났습니다.", "", false);
            yield return _presentation.PlayAttack();
            _presentation.Show("공장을 복구해 주세요", "핵심 장치가 멈추면서 세상의 이미지가 뭉개졌습니다.\n공장 속 CNN 장치를 복구해 선명한 세상을 되찾아 주세요!", "", false);
            yield return new WaitForSeconds(2.5f);
            yield return EnterFactory();
        }

        private IEnumerator EnterFactory()
        {
            _stage = IntroStage.Loading;
            yield return null;
            if (!Application.CanStreamedLevelBeLoaded(_nextSceneName))
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
