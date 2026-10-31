using System.Collections;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace OpeningEnding.Ending
{
    public sealed class FactoryEndingManager : MonoBehaviour
    {
        [SerializeField] private FactoryRestorationController _restoration;
        [SerializeField] private FactoryEndingPanelController _panel;
        [SerializeField] private Transform _entryPoint;
        [SerializeField] private string _restartSceneName = "FactoryIntro";

        private enum EndingStage
        {
            FactoryRepair, Focus, AwaitingUpscaling, Extraction, Convolution, FeatureMaps,
            Prediction, ReturnToScreen, Result, Celebration, ResidentArrival,
            FirstThanks, SecondThanks, ResidentDeparture, Panorama, Complete, Leaving
        }
        private EndingStage _stage;
        private XROrigin _rig;
        private FactoryIntroInputController _input;
        private SceneTransitionController _transition;
        private float _elapsed;
        private bool _isReady;

        private void OnEnable()
        {
            _panel.OnUpscalingRequested += HandleUpscalingRequested;
            _panel.OnDialogueContinueRequested += HandleDialogueContinueRequested;
            _panel.OnRestartRequested += HandleRestartRequested;
            _panel.OnExitRequested += HandleExitRequested;
        }

        private void Start()
        {
            _rig = FindFirstObjectByType<XROrigin>();

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
            _restoration.BeginFactoryRepair();
            _panel.ShowRestoration();
            _stage = EndingStage.FactoryRepair;
            _elapsed = 0f;
            _isReady = true;
        }

        private void Update()
        {
            if (!_isReady || _stage == EndingStage.Leaving)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            switch (_stage)
            {
                case EndingStage.FactoryRepair:
                    _restoration.EvaluateFactoryRepair(_elapsed);
                    if (_elapsed >= FactoryRestorationController.FactoryRepairDuration)
                    {
                        _restoration.PrepareUpscaling();
                        EnterStage(EndingStage.Focus);
                    }
                    break;
                case EndingStage.Focus:
                    _restoration.EvaluateFocus(Mathf.Clamp01(_elapsed / 2.5f));
                    if (_elapsed >= 2.5f)
                    {
                        _panel.ShowUpscalingInvitation();
                        _input.SetUIVisible(true);
                        EnterStage(EndingStage.AwaitingUpscaling);
                    }
                    break;
                case EndingStage.AwaitingUpscaling:
                    _restoration.EvaluateProcessing(_elapsed);
                    break;
                case EndingStage.Extraction:
                    _restoration.EvaluateProcessing(_elapsed);
                    _panel.ShowExtraction(Mathf.Clamp01(_elapsed / 4f));
                    AdvanceAfter(4f, EndingStage.Convolution);
                    break;
                case EndingStage.Convolution:
                    _restoration.EvaluateProcessing(_elapsed);
                    _panel.ShowConvolution(Mathf.Clamp01(_elapsed / 12f));
                    AdvanceAfter(12f, EndingStage.FeatureMaps);
                    break;
                case EndingStage.FeatureMaps:
                    _restoration.EvaluateProcessing(_elapsed);
                    _panel.ShowFeatureMaps(Mathf.Clamp01(_elapsed / 8f));
                    AdvanceAfter(8f, EndingStage.Prediction);
                    break;
                case EndingStage.Prediction:
                    _restoration.EvaluateProcessing(_elapsed);
                    _panel.ShowPrediction(Mathf.Clamp01(_elapsed / 8f));
                    AdvanceAfter(8f, EndingStage.ReturnToScreen);
                    break;
                case EndingStage.ReturnToScreen:
                    _restoration.EvaluateProcessing(_elapsed);
                    _panel.ShowReturn(Mathf.Clamp01(_elapsed / 5f));
                    _restoration.EvaluateUpscaling(Mathf.SmoothStep(0f, 1f, _elapsed / 5f));
                    if (_elapsed >= 5f)
                    {
                        _panel.ShowReveal();
                        EnterStage(EndingStage.Result);
                    }
                    break;
                case EndingStage.Result:
                    _restoration.EvaluateProcessing(_elapsed);
                    if (_elapsed >= 2f)
                    {
                        _restoration.BeginCelebration();
                        EnterStage(EndingStage.Celebration);
                    }
                    break;
                case EndingStage.Celebration:
                    _restoration.EvaluateCelebration(_elapsed);
                    if (_elapsed >= FactoryRestorationController.CelebrationDuration)
                    {
                        _restoration.BeginResidentThanks();
                        EnterStage(EndingStage.ResidentArrival);
                    }
                    break;
                case EndingStage.ResidentArrival:
                    _restoration.EvaluateResidentThanks(_elapsed, false);
                    if (_elapsed >= 3f)
                    {
                        _panel.ShowResidentThanks(false);
                        _input.SetUIVisible(true);
                        EnterStage(EndingStage.FirstThanks);
                    }
                    break;
                case EndingStage.FirstThanks:
                case EndingStage.SecondThanks:
                    // The approach has already finished; keep the speaker in place while reading.
                    _restoration.EvaluateResidentThanks(3f + _elapsed, false);
                    break;
                case EndingStage.ResidentDeparture:
                    _restoration.EvaluateResidentThanks(_elapsed, true);
                    AdvanceAfter(2.5f, EndingStage.Panorama);
                    break;
                case EndingStage.Panorama:
                    _restoration.EvaluatePanorama(_elapsed);
                    if (_elapsed >= 6f)
                    {
                        _panel.ShowCompletion();
                        _input.SetUIVisible(true);
                        EnterStage(EndingStage.Complete);
                    }
                    break;
                case EndingStage.Complete:
                    _restoration.EvaluatePanorama(6f + _elapsed);
                    break;
            }
        }

        private void HandleUpscalingRequested()
        {
            if (!_isReady || _stage != EndingStage.AwaitingUpscaling)
            {
                return;
            }

            EnterStage(EndingStage.Extraction);
            _panel.ShowExtraction(0f);
            _input.SetUIVisible(false);
        }

        private void HandleDialogueContinueRequested()
        {
            if (_stage == EndingStage.FirstThanks)
            {
                _panel.ShowResidentThanks(true);
                // Page changes must not restart the resident's talking animation.
                _stage = EndingStage.SecondThanks;
            }
            else if (_stage == EndingStage.SecondThanks)
            {
                _panel.ShowRestoration();
                _input.SetUIVisible(false);
                EnterStage(EndingStage.ResidentDeparture);
            }
        }

        private void EnterStage(EndingStage next)
        {
            _stage = next;
            _elapsed = 0f;
        }

        private void AdvanceAfter(float duration, EndingStage next)
        {
            if (_elapsed >= duration)
            {
                EnterStage(next);
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
            _panel.OnUpscalingRequested -= HandleUpscalingRequested;
            _panel.OnDialogueContinueRequested -= HandleDialogueContinueRequested;
            _panel.OnRestartRequested -= HandleRestartRequested;
            _panel.OnExitRequested -= HandleExitRequested;
            StopAllCoroutines();
            _input?.Release();
        }
    }
}
