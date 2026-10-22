using System.Collections;
using KdyIntro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace KdyLearning
{
    public enum KdyLessonAction
    {
        EnterPixels,
        CollectPixel,
        CollectConvolution,
        PreviousTab,
        NextTab,
        FoldRgb,
        ToggleNumbers,
        Answer,
        AddPadding,
        Replay,
        ReturnRoom,
        RestartIntro,
        CollectFilter,
        CollectFeatureMaps,
        CollectActivation,
        ToggleFeature,
        EnterDesign,
        StartTraining,
        ContinueTraining,
        ResetDesign,
        RetryLastDesign,
        ShowHints,
        ContinueToEnding,
        FinishGame,
        ReturnInspection
    }

    public sealed class KdyLessonManager : MonoBehaviour
    {
        [SerializeField, Range(4, 17)] private int _sceneNumber = 4;
        [SerializeField] private Text _heading;
        [SerializeField] private Text _subtitle;
        [SerializeField] private Text _collectLabel;
        [SerializeField] private KdyRigController _rig;
        [SerializeField] private KdyLessonTabletController _tablet;
        [SerializeField] private KdyRgbImageController _pixels;
        [SerializeField] private KdyConvolutionController _convolution;
        [SerializeField] private KdyFilterComparisonController _filters;
        [SerializeField] private KdyFeatureMapGalleryController _featureMaps;
        [SerializeField] private KdyActivationLeverController _activation;
        [SerializeField] private KdyLessonSurfaceController _collect;
        [SerializeField] private KdyLessonSurfaceController _paddingAction;
        [SerializeField] private KdyLessonSurfaceController[] _answerActions;
        [SerializeField] private Text[] _answerLabels;
        [SerializeField] private KdyScrollBeaconController[] _beacons;
        [SerializeField] private KdyLessonSurfaceController[] _featureSwitches;
        [SerializeField] private KdyQuestUnlockController _quest;
        [SerializeField] private KdyNetworkDesignerController _designer;
        [SerializeField] private KdyTrainingPlaybackController _training;
        [SerializeField] private KdyLessonSurfaceController _primaryAction;
        [SerializeField] private Text _primaryLabel;
        [SerializeField] private KdyLessonSurfaceController _replayAction;
        [SerializeField] private KdyLessonSurfaceController _redesignAction;
        [SerializeField] private KdyFailureReviewController _failure;
        [SerializeField] private KdySuccessController _success;
        [SerializeField] private KdyFactoryInspectionController _inspection;
        [SerializeField] private KdySignComparisonController _comparison;
        [SerializeField] private KdyFactoryEndingController _ending;
        private UI.SceneTransitionController _transition;
        private KdyLessonProgressController _progress;
        private bool _ready;

        public bool IsTransitioning => !_ready || (_transition != null && _transition.IsLoading);

        private void Awake()
        {
            foreach (KdyLessonSurfaceController surface in GetComponentsInChildren<KdyLessonSurfaceController>(true))
            {
                surface.Initialize(this);
            }
            foreach (KdyLessonGrabController grab in GetComponentsInChildren<KdyLessonGrabController>(true))
            {
                grab.Initialize(this);
            }
        }

        private IEnumerator Start()
        {
            yield return null;
            _rig.BindRig(false);
            if (!_rig.Ready || _rig.ViewCamera == null)
            {
                Debug.LogError("[KdyLearning] The shared XR player is missing.", this);
                yield break;
            }
            XROrigin origin = _rig.ViewCamera.GetComponentInParent<XROrigin>();
            if (origin != null)
            {
                float height = XRSettings.isDeviceActive ? Mathf.Clamp(_rig.ViewCamera.transform.position.y - origin.transform.position.y, .75f, 2.2f) : 1.65f;
                origin.MoveCameraToWorldLocation(new Vector3(0f, height, 0f));
                origin.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
            }
            Transform player = _rig.ViewCamera.transform.root;
            _progress = player.GetComponent<KdyLessonProgressController>();
            if (_progress == null)
            {
                _progress = player.gameObject.AddComponent<KdyLessonProgressController>();
            }
            _transition = player.GetComponentInChildren<UI.SceneTransitionController>();
            if (_transition == null)
            {
                GameObject fade = new GameObject("Kdy Scene Transition", typeof(RectTransform));
                fade.transform.SetParent(player, false);
                _transition = fade.AddComponent<UI.SceneTransitionController>();
                _transition.Initialize(_rig.ViewCamera);
            }
            _tablet.Initialize(_progress, _rig.ViewCamera, _sceneNumber);
            if (_pixels != null)
            {
                _pixels.Changed += RefreshLesson;
                _pixels.Initialize(_rig.ViewCamera);
            }
            if (_convolution != null)
            {
                _convolution.Changed += RefreshLesson;
                _convolution.Initialize(_rig.ViewCamera);
            }
            if (_filters != null)
            {
                _filters.Changed += RefreshLesson;
                _filters.Initialize(_rig.ViewCamera);
            }
            if (_featureMaps != null)
            {
                _featureMaps.Changed += RefreshLesson;
                _featureMaps.Initialize(_rig.ViewCamera);
            }
            if (_activation != null)
            {
                _activation.Changed += RefreshLesson;
                _activation.Initialize(_rig.ViewCamera);
            }
            if (_quest != null)
            {
                _quest.Changed += RefreshLesson;
                _quest.Initialize(_rig.ViewCamera, _progress);
            }
            if (_designer != null)
            {
                _designer.Changed += RefreshLesson;
                _designer.Initialize(_rig.ViewCamera, _progress.Network);
            }
            if (_training != null)
            {
                _training.Changed += RefreshLesson;
                _training.Initialize(_rig.ViewCamera, _progress);
            }
            if (_failure != null)
            {
                _failure.Changed += RefreshLesson;
                _failure.Initialize(_rig.ViewCamera, _progress);
            }
            if (_success != null)
            {
                _success.Changed += RefreshLesson;
                _success.Initialize(_rig.ViewCamera, _progress);
            }
            if (_inspection != null)
            {
                _inspection.Changed += RefreshLesson;
                _inspection.Initialize(_rig.ViewCamera, _progress);
            }
            if (_ending != null)
            {
                _ending.Changed += RefreshLesson;
                _ending.Initialize(_rig.ViewCamera, _progress);
            }
            if (_sceneNumber >= 10)
            {
                _progress.Tab = 2;
            }
            if (_sceneNumber == 13 || _sceneNumber == 14)
            {
                _tablet.EmphasizeHints();
            }
            _ready = true;
            RefreshLesson();
        }

        public void Activate(KdyLessonAction action, int argument)
        {
            if (IsTransitioning)
            {
                return;
            }
            switch (action)
            {
                case KdyLessonAction.EnterPixels:
                    if (_progress.ScrollCount == 5 && argument == 4)
                    {
                        _transition.Load("kdy_scene10");
                    }
                    else if (argument == _progress.NextScroll && argument >= 0 && argument < 5)
                    {
                        _transition.Load("kdy_scene" + (argument + 5));
                    }
                    else
                    {
                        _subtitle.text = "태블릿 맵이 가리키는 다음 양피지를 선택해 보세요.";
                    }
                    break;
                case KdyLessonAction.CollectPixel:
                    if (_pixels != null && _pixels.ReadyToCollect)
                    {
                        CollectAndContinue(0);
                    }
                    break;
                case KdyLessonAction.CollectConvolution:
                    if (_convolution != null && _convolution.ReadyToCollect)
                    {
                        CollectAndContinue(1);
                    }
                    break;
                case KdyLessonAction.CollectFilter:
                    if (_filters != null && _filters.ReadyToCollect)
                    {
                        CollectAndContinue(2);
                    }
                    break;
                case KdyLessonAction.CollectFeatureMaps:
                    if (_featureMaps != null && _featureMaps.ReadyToCollect)
                    {
                        CollectAndContinue(3);
                    }
                    break;
                case KdyLessonAction.CollectActivation:
                    if (_activation != null && _activation.ReadyToCollect)
                    {
                        CollectAndContinue(4);
                    }
                    break;
                case KdyLessonAction.PreviousTab:
                    _tablet.ChangeTab(-1);
                    break;
                case KdyLessonAction.NextTab:
                    _tablet.ChangeTab(1);
                    break;
                case KdyLessonAction.FoldRgb:
                    _pixels?.Fold();
                    break;
                case KdyLessonAction.ToggleNumbers:
                    _pixels?.ToggleNumbers();
                    break;
                case KdyLessonAction.Answer:
                    if (_convolution != null && argument >= 0 && argument < _convolution.AnswerOptions.Length)
                    {
                        _convolution.SubmitAnswer(_convolution.AnswerOptions[argument]);
                    }
                    break;
                case KdyLessonAction.AddPadding:
                    _convolution?.BeginPadding();
                    break;
                case KdyLessonAction.ToggleFeature:
                    _featureMaps?.ToggleFeature(argument);
                    break;
                case KdyLessonAction.Replay:
                    _convolution?.Replay();
                    _filters?.Replay();
                    _featureMaps?.Replay();
                    _activation?.ResetLever();
                    _inspection?.Replay();
                    if (_training != null && _training.CanReplay)
                    {
                        _training.Replay();
                    }
                    break;
                case KdyLessonAction.EnterDesign:
                    if ((_quest != null && _quest.ReadyToDesign) ||
                        (_training != null && (_training.IsComplete || !_progress.Network.IsComplete)) ||
                        _failure != null || _success != null || _inspection != null || _ending != null)
                    {
                        _transition.Load("kdy_scene11");
                    }
                    break;
                case KdyLessonAction.StartTraining:
                    if (_designer != null && _designer.ReadyToTrain && _progress.Network.IsComplete)
                    {
                        _transition.Load("kdy_scene12");
                    }
                    break;
                case KdyLessonAction.ContinueTraining:
                    ContinueTrainingResult();
                    break;
                case KdyLessonAction.RetryLastDesign:
                    if (_progress.HasTrainingResult && _progress.LastTrainedNetwork.IsComplete &&
                        ((_failure != null && _failure.CanRetry) || (_success != null && _success.CanRetry)))
                    {
                        _progress.Network.CopyFrom(_progress.LastTrainedNetwork);
                        _transition.Load("kdy_scene12");
                    }
                    break;
                case KdyLessonAction.ContinueToEnding:
                    if (_inspection != null && _inspection.ReadyToContinue)
                    {
                        _progress.MarkFactorySignInspected(KdyFactorySignExampleData.GetImprovement(_progress.LastImprovement));
                        _transition.Load("kdy_scene17");
                    }
                    break;
                case KdyLessonAction.FinishGame:
                    if (_ending != null && _ending.ReadyToFinish)
                    {
                        _ending.CompleteExperience();
                    }
                    break;
                case KdyLessonAction.ReturnInspection:
                    _transition.Load("kdy_scene16");
                    break;
                case KdyLessonAction.ShowHints:
                    _tablet.EmphasizeHints();
                    break;
                case KdyLessonAction.ResetDesign:
                    _designer?.ResetDesign();
                    break;
                case KdyLessonAction.RestartIntro:
                    _progress.ClearProgress();
                    _transition.Load("kdy_scene1");
                    break;
                default:
                    _transition.Load("kdy_scene4");
                    break;
            }
        }

        public void SelectPixel(RaycastHit hit)
        {
            if (IsTransitioning)
            {
                return;
            }
            _pixels?.SelectPixel(hit);
            _featureMaps?.SelectMap(hit);
            _activation?.SelectCell(hit);
            _comparison?.SelectPixel(hit);
        }

        private void CollectAndContinue(int index)
        {
            _progress.Collect(index);
            _tablet.Refresh();
            string nextScene = "kdy_scene" + (index + 6);
            if (UI.SceneTransitionController.CanLoad(nextScene))
            {
                _transition.Load(nextScene);
                return;
            }
            RefreshLesson();
            _heading.text = index == 4 ? "활성화함수 양피지 획득" : "양피지 획득";
            _subtitle.text = _progress.ScrollCount == 5
                ? "양피지 5개를 모두 모았어요! 다음 신경망 설계 구역은 다음 제작 묶음에서 이어집니다. CNN실로 돌아가 복습해 보세요."
                : "체험을 마쳤어요. CNN실로 돌아가 태블릿에서 모은 양피지를 확인해 보세요.";
            _tablet.SetHint(_subtitle.text);
        }

        private void RefreshLesson()
        {
            if (_progress == null)
            {
                return;
            }
            if (_sceneNumber == 4)
            {
                _heading.text = "CNN실";
                _subtitle.text = _progress.ScrollCount == 5
                    ? "양피지 5개를 모두 모았어요. 빛나는 ⑤번 양피지를 선택하면 신경망 설계 구역으로 이동합니다."
                    : "CNN은 이미지의 특징을 찾는 인공신경망이에요. 양피지 다섯 개를 순서대로 모아 보세요. 다음 위치는 태블릿 맵에 표시돼요.";
                for (int i = 0; i < _beacons.Length; i++)
                {
                    bool questDestination = _progress.ScrollCount == 5 && i == 4;
                    _beacons[i].Configure((i == _progress.NextScroll && i < 5) || questDestination, _progress.HasScroll(i));
                    if (questDestination)
                    {
                        _beacons[i].ShowQuestDestination();
                    }
                }
            }
            else if (_pixels != null)
            {
                ShowLesson(_pixels.Hint, _pixels.ReadyToCollect, 0, "RGB를 펼치고 픽셀 3개 선택");
            }
            else if (_convolution != null)
            {
                ShowLesson(_convolution.Hint, _convolution.ReadyToCollect, 1, "합성곱과 패딩 체험 중");
                _paddingAction.SetAvailable(_convolution.CanPad);
                for (int i = 0; i < _answerActions.Length; i++)
                {
                    _answerActions[i].SetAvailable(_convolution.CanAnswer);
                    if (i < _convolution.AnswerOptions.Length)
                    {
                        _answerLabels[i].text = _convolution.AnswerOptions[i].ToString();
                    }
                }
            }
            else if (_filters != null)
            {
                ShowLesson(_filters.Hint, _filters.ReadyToCollect, 2, "서로 다른 필터 2개 사용");
            }
            else if (_featureMaps != null)
            {
                ShowLesson(_featureMaps.Hint, _featureMaps.ReadyToCollect, 3, "피쳐맵 3장 선택 후 껐다 켜기");
                if (_featureSwitches != null)
                {
                    foreach (KdyLessonSurfaceController featureSwitch in _featureSwitches)
                    {
                        featureSwitch.SetAvailable(_featureMaps.CanToggleFeatures);
                    }
                }
            }
            else if (_activation != null)
            {
                ShowLesson(_activation.Hint, _activation.ReadyToCollect, 4, "레버를 양쪽으로 움직이기");
            }
            else if (_quest != null)
            {
                _subtitle.text = _quest.Hint;
                _primaryAction.SetAvailable(_quest.ReadyToDesign);
                _primaryLabel.text = _quest.ReadyToDesign ? "설계 시작" : "양피지 5개 모으면 해금";
                _tablet.SetHint(_quest.Hint);
            }
            else if (_designer != null)
            {
                _subtitle.text = _designer.Hint;
                _primaryAction.SetAvailable(_designer.ReadyToTrain);
                _primaryLabel.text = _designer.ReadyToTrain ? "학습 시작" : "네 가지 부품 설정 필요";
                _tablet.SetHint(_designer.Hint);
            }
            else if (_training != null)
            {
                _subtitle.text = _training.Hint;
                string resultScene = _training.DidPass ? "kdy_scene15" :
                    (_progress.FailureCount >= 3 ? "kdy_scene14" : "kdy_scene13");
                bool resultSceneExists = UI.SceneTransitionController.CanLoad(resultScene);
                _primaryAction.SetAvailable(_training.IsComplete && resultSceneExists);
                _primaryLabel.text = resultSceneExists ? "결과 확인" : "다음 구역 제작 예정";
                _replayAction.SetAvailable(_training.CanReplay);
                _redesignAction.SetAvailable(_training.IsComplete || !_progress.Network.IsComplete);
                _tablet.SetHint(_training.Hint);
            }
            else if (_failure != null)
            {
                _subtitle.text = _failure.Hint;
                _replayAction.SetAvailable(_failure.CanRetry);
                _redesignAction.SetAvailable(true);
                _tablet.SetHint(_failure.Hint);
            }
            else if (_success != null)
            {
                _subtitle.text = _success.Hint;
                _replayAction.SetAvailable(_success.CanRetry);
                _redesignAction.SetAvailable(!_success.IsSignGrabbed);
                _tablet.SetHint(_success.Hint);
                if (_ready && _success.HasPassedResult && _success.IsSignGrabbed &&
                    UI.SceneTransitionController.CanLoad("kdy_scene16") && !IsTransitioning)
                {
                    _transition.Load("kdy_scene16");
                }
            }
            else if (_inspection != null)
            {
                _subtitle.text = _inspection.Hint;
                _primaryAction.SetAvailable(_inspection.ReadyToContinue);
                _primaryLabel.text = _inspection.ReadyToContinue ? "공장 복구 엔딩으로" : "간판 투입 후 슬라이더 양쪽 비교";
                _tablet.SetHint(_inspection.Hint);
            }
            else if (_ending != null)
            {
                _subtitle.text = _ending.Hint;
                _primaryAction.SetAvailable(_ending.ReadyToFinish && !_ending.IsFinished);
                _primaryLabel.text = _ending.IsFinished ? "체험 완료" : (_ending.ReadyToFinish ? "게임 완료" : (_progress.HasInspectedFactorySign ? "공장 복구 중" : "16번 비교 먼저 완료"));
                _tablet.SetHint(_ending.Hint);
            }
        }

        private void ContinueTrainingResult()
        {
            if (_training == null || !_training.IsComplete)
            {
                return;
            }
            string destination = _training.DidPass ? "kdy_scene15" :
                (_progress.FailureCount >= 3 ? "kdy_scene14" : "kdy_scene13");
            if (UI.SceneTransitionController.CanLoad(destination))
            {
                _transition.Load(destination);
            }
        }

        private void ShowLesson(string hint, bool complete, int scrollIndex, string instruction)
        {
            _subtitle.text = hint;
            _collect.SetAvailable(complete && !_progress.HasScroll(scrollIndex));
            string[] symbols = { "①", "②", "③", "④", "⑤" };
            _collectLabel.text = _progress.HasScroll(scrollIndex) ? "획득 완료" :
                (complete ? "양피지 " + symbols[scrollIndex] + " 획득" : instruction);
            _tablet.SetHint(hint);
        }

        private void OnDestroy()
        {
            if (_pixels != null)
            {
                _pixels.Changed -= RefreshLesson;
            }
            if (_convolution != null)
            {
                _convolution.Changed -= RefreshLesson;
            }
            if (_filters != null)
            {
                _filters.Changed -= RefreshLesson;
            }
            if (_featureMaps != null)
            {
                _featureMaps.Changed -= RefreshLesson;
            }
            if (_activation != null)
            {
                _activation.Changed -= RefreshLesson;
            }
            if (_quest != null)
            {
                _quest.Changed -= RefreshLesson;
            }
            if (_designer != null)
            {
                _designer.Changed -= RefreshLesson;
            }
            if (_training != null)
            {
                _training.Changed -= RefreshLesson;
            }
            if (_failure != null)
            {
                _failure.Changed -= RefreshLesson;
            }
            if (_success != null)
            {
                _success.Changed -= RefreshLesson;
            }
            if (_inspection != null)
            {
                _inspection.Changed -= RefreshLesson;
            }
            if (_ending != null)
            {
                _ending.Changed -= RefreshLesson;
            }
        }
    }
}
