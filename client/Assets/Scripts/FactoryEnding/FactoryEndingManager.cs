using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryEnding
{
    public sealed class FactoryEndingManager : MonoBehaviour
    {
        [SerializeField] private FactoryRestorationController _restoration;
        [SerializeField] private FactoryEndingPanelController _panel;
        [SerializeField] private Transform _entryPoint;
        [SerializeField] private string _restartSceneName = "FactoryIntro";

        private enum EndingStage { Restoration, Recap, Complete, Leaving }

        private EndingStage _stage;
        private Coroutine _sequence;

        private void OnEnable()
        {
            _panel.OnCompleteRequested += HandleCompleteRequested;
            _panel.OnRestartRequested += HandleRestartRequested;
            _panel.OnExitRequested += HandleExitRequested;
        }

        private void Start()
        {
            XROrigin rig = FindFirstObjectByType<XROrigin>();
            if (rig != null && rig.Camera != null)
            {
                float height = Vector3.Dot(rig.Camera.transform.position - rig.Origin.transform.position, rig.Origin.transform.up);
                rig.MatchOriginUpCameraForward(Vector3.up, _entryPoint.forward);
                rig.MoveCameraToWorldLocation(_entryPoint.position + Vector3.up * height);
            }
            _sequence = StartCoroutine(PlayEnding());
        }

        private IEnumerator PlayEnding()
        {
            _stage = EndingStage.Restoration;
            _panel.ShowRestoration();
            yield return _restoration.RestoreFactory();
            _stage = EndingStage.Recap;
            _panel.ShowRecap();
            _sequence = null;
        }

        private void HandleCompleteRequested()
        {
            if (_stage != EndingStage.Recap)
            {
                return;
            }

            _stage = EndingStage.Complete;
            _panel.ShowCompletion();
        }

        private void HandleRestartRequested()
        {
            if (_stage != EndingStage.Complete)
            {
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(_restartSceneName))
            {
                _panel.ShowMessage("시작 씬이 빌드 목록에 없습니다. FactoryIntro 씬을 등록한 뒤 다시 시작해 주세요.");
                return;
            }

            _stage = EndingStage.Leaving;
            _panel.SetNavigationAvailable(false);
            _sequence = StartCoroutine(Restart());
        }

        private IEnumerator Restart()
        {
            // Finish the XR UI callback before unloading its button and restoring ray input.
            yield return null;
            _panel.enabled = false;
            AsyncOperation loading = SceneManager.LoadSceneAsync(_restartSceneName, LoadSceneMode.Single);
            if (loading == null)
            {
                _stage = EndingStage.Complete;
                _panel.enabled = true;
                _panel.SetNavigationAvailable(true);
                _panel.ShowMessage("시작 씬을 열지 못했습니다. 다시 시도해 주세요.");
            }
            _sequence = null;
        }

        private void HandleExitRequested()
        {
            if (_stage != EndingStage.Complete)
            {
                return;
            }
#if UNITY_EDITOR
            _panel.ShowMessage("에디터에서는 앱을 종료하지 않습니다. Play를 종료하면 체험이 끝납니다.");
#else
            Application.Quit();
#endif
        }

        private void OnDisable()
        {
            _panel.OnCompleteRequested -= HandleCompleteRequested;
            _panel.OnRestartRequested -= HandleRestartRequested;
            _panel.OnExitRequested -= HandleExitRequested;
            if (_sequence != null)
            {
                StopCoroutine(_sequence);
                _sequence = null;
            }
        }
    }
}
