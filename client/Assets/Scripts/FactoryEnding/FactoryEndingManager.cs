using System.Collections;
using FactoryIntro;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace FactoryEnding
{
    public sealed class FactoryEndingManager : MonoBehaviour
    {
        [SerializeField] private FactoryRestorationController _restoration;
        [SerializeField] private FactoryEndingPanelController _panel;
        [SerializeField] private Transform _entryPoint;
        [SerializeField] private string _restartSceneName = "FactoryIntro";

        private enum EndingStage { Restoration, Complete, Leaving }
        private EndingStage _stage;
        private XROrigin _rig;
        private FactoryIntroInputController _input;
        private SceneTransitionController _transition;
        private float _elapsed;
        private bool _isReady;

        private void OnEnable()
        {
            _panel.OnRestartRequested += HandleRestartRequested;
            _panel.OnExitRequested += HandleExitRequested;
        }

        private void Start()
        {
            _panel.ShowRestoration();
            _rig = FindFirstObjectByType<XROrigin>();
            if (_rig == null || _rig.Camera == null || _entryPoint == null || _restoration == null || !_restoration.IsReady)
            {
                Debug.LogError("[FactoryEnding] The village, sky, XR rig and arrival point must be assigned.", this);
                return;
            }

            // Keep head tracking; only the origin is placed at the familiar opening viewpoint.
            float height = Vector3.Dot(_rig.Camera.transform.position - _rig.Origin.transform.position,
                _rig.Origin.transform.up);
            _rig.MatchOriginUpCameraForward(Vector3.up, _entryPoint.forward);
            _rig.MoveCameraToWorldLocation(_entryPoint.position + Vector3.up * height);
            _input = GetComponent<FactoryIntroInputController>();
            if (_input == null) _input = gameObject.AddComponent<FactoryIntroInputController>();
            _input.Bind(_rig);
            _input.SetUIVisible(false);
            _panel.BindCamera(_rig.Camera);
            _restoration.BindCamera(_rig.Camera);
            _restoration.BeginRestoration();
            _stage = EndingStage.Restoration;
            _elapsed = 0f;
            _isReady = true;
        }

        private void Update()
        {
            if (!_isReady || _stage == EndingStage.Leaving) return;
            _elapsed += Time.deltaTime;
            _restoration.Evaluate(_elapsed);
            if (_stage == EndingStage.Restoration && _elapsed >= FactoryRestorationController.Duration)
            {
                _stage = EndingStage.Complete;
                _panel.ShowCompletion();
                _input.SetUIVisible(true);
            }
        }

        private void HandleRestartRequested()
        {
            if (_stage != EndingStage.Complete) return;
            if (!SceneTransitionController.CanLoad(_restartSceneName))
            {
                _panel.ShowMessage("시작 화면을 열지 못했습니다. 다시 시도해 주세요.");
                return;
            }
            _stage = EndingStage.Leaving;
            _panel.SetNavigationAvailable(false);
            StartCoroutine(Restart());
        }

        private IEnumerator Restart()
        {
            // Leave the XR button callback before unloading its input and scene.
            yield return null;
            GameObject overlay = new GameObject("Ending scene transition", typeof(RectTransform));
            overlay.transform.SetParent(_rig.transform, false);
            _transition = overlay.AddComponent<SceneTransitionController>();
            _transition.Initialize(_rig.Camera);
            _input.Release();
            _transition.Load(_restartSceneName);
            while (_transition != null && _transition.IsLoading) yield return null;
            if (_transition != null)
            {
                Destroy(_transition.gameObject);
                _input.Bind(_rig);
                _input.SetUIVisible(true);
                _stage = EndingStage.Complete;
                _panel.SetNavigationAvailable(true);
                _panel.ShowMessage("시작 화면을 열지 못했습니다. 다시 시도해 주세요.");
            }
        }

        private void HandleExitRequested()
        {
            if (_stage != EndingStage.Complete) return;
#if UNITY_EDITOR
            _panel.ShowMessage("에디터에서는 Play를 종료하면 체험이 끝납니다.");
#else
            Application.Quit();
#endif
        }

        private void OnDisable()
        {
            _panel.OnRestartRequested -= HandleRestartRequested;
            _panel.OnExitRequested -= HandleExitRequested;
            StopAllCoroutines();
            _input?.Release();
        }
    }
}
