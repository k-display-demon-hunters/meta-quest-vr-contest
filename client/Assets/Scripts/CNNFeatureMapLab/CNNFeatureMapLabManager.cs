using System.Collections;
using CNNFeatureMaps.Activation;
using CNNFeatureMaps.FeatureMaps;
using CNNFeatureMaps.Lesson;
using UnityEngine;

namespace CNNFeatureMaps
{
    [DefaultExecutionOrder(-100)]
    public sealed class CNNFeatureMapLabManager : MonoBehaviour
    {
        [SerializeField] private FeatureMapsManager _featureMaps;
        [SerializeField] private LessonUIManager _lessonUI;
        [SerializeField] private FilterComparisonManager _filterComparison;
        [SerializeField] private Transform[] _featureMapVisuals;
        [SerializeField] private ActivationLessonManager _activationLesson;
        private bool _activationLessonComplete;
        private bool _filterLessonComplete;
        private bool _ready;
        private LessonStep _step = LessonStep.ExploreFeatureMaps;
        private bool _mapLessonComplete;
        private int _channel;
        private bool _lastStepSkipped;

        public bool ReadyForModelDesign => _mapLessonComplete && _activationLessonComplete && !_featureMaps.HasHeldTargets;

        private void Awake()
        {
            // Wire modules before child grab components begin receiving XR events.
            _featureMaps.Initialize();
            _activationLesson.gameObject.SetActive(false);
            foreach (Transform visual in _featureMapVisuals) { visual.gameObject.SetActive(false); }
        }

        private void OnEnable()
        {
            _filterComparison.OnCompleted += FinishFilterLesson;
            _activationLesson.OnCompleted += HandleActivationCompleted;
            _lessonUI.OnPreviousRequested += HandlePreviousRequested;
            _lessonUI.OnNextRequested += HandleNextRequested;
            _lessonUI.OnScanRequested += HandleScanRequested;
            _lessonUI.OnFilterRequested += HandleFilterRequested;
            _lessonUI.OnSpreadRequested += HandleSpreadRequested;
            _lessonUI.OnRestoreRequested += HandleRestoreRequested;
            _lessonUI.OnEnlargeRequested += HandleEnlargeRequested;
            _lessonUI.OnShrinkRequested += HandleShrinkRequested;
            _featureMaps.OnStepApplied += _lessonUI.ShowLesson;
            _featureMaps.OnScanningChanged += UpdateButtons;
            _featureMaps.OnPatchEvaluated += _lessonUI.ShowPatch;
            _featureMaps.OnAffectedPixelSelected += _lessonUI.ShowAffectedPixel;
            _featureMaps.OnInputEdited += _lessonUI.ShowInputEdited;
        }

        private void FinishFilterLesson()
        {
            if (!_filterLessonComplete) { StartCoroutine(BeginFeatureMapLesson()); }
        }

        private IEnumerator BeginFeatureMapLesson()
        {
            _filterLessonComplete = true;
            // Finish the uGUI click before disabling its entire spatial lesson.
            yield return null;
            _filterComparison.gameObject.SetActive(false);

            _ready = _featureMaps.TryStart();
            if (_ready)
            {
                foreach (Transform visual in _featureMapVisuals) { visual.gameObject.SetActive(true); }
                ApplyStep();
                _lessonUI.ShowInfo("피쳐맵을 살펴보세요!\n\n입력 픽셀을 누르면 연결된 칸이 빛나요.\n맵의 테두리는 잡아서 옮길 수 있고,\n맵이나 칸을 누르면 채널이 합쳐지는 계산을 볼 수 있어요.");
            }
        }

        private IEnumerator BeginActivationLesson()
        {
            _mapLessonComplete = true;
            _ready = false;
            // Finish the button event before hiding all of parchment 4.
            yield return null;
            _featureMaps.CloseCalculation();
            _lessonUI.HideInfo();
            foreach (Transform visual in _featureMapVisuals) { visual.gameObject.SetActive(false); }
            _activationLesson.BeginLesson(_featureMaps.CopyFirstLayerMap(0), _featureMaps.CopyFirstLayerMap(1));
        }

        private void HandleActivationCompleted()
        {
            StartCoroutine(FinishActivationLesson());
        }

        private IEnumerator FinishActivationLesson()
        {
            // The final explanation is confirmed before the factory exit condition becomes true.
            yield return null;
            _activationLesson.gameObject.SetActive(false);
            _activationLessonComplete = true;
        }

        private bool CanPerformAction()
        {
            if (!_ready)
            {
                return false;
            }
            if (_featureMaps.HasHeldTargets)
            {
                _lessonUI.ShowReleaseBeforeAction();
                return false;
            }
            return true;
        }

        private void HandlePreviousRequested()
        {
            if (CanPerformAction())
            {
                MoveToPreviousStep();
            }
        }

        private void HandleNextRequested()
        {
            if (CanPerformAction())
            {
                MoveToNextStep();
            }
        }

        private void HandleScanRequested()
        {
            if (CanPerformAction())
            {
                _featureMaps.ToggleScanning();
            }
        }

        private void HandleFilterRequested()
        {
            if (CanPerformAction())
            {
                _channel = 1 - _channel;
                ApplyStep();
            }
        }

        private void HandleSpreadRequested()
        {
            if (CanPerformAction())
            {
                _featureMaps.SpreadNextLayer();
                UpdateButtons();
            }
        }

        private void HandleRestoreRequested()
        {
            if (CanPerformAction())
            {
                RestoreLesson();
            }
        }

        private void HandleEnlargeRequested()
        {
            ScaleSelectedTarget(1.2f);
        }

        private void HandleShrinkRequested()
        {
            ScaleSelectedTarget(1f / 1.2f);
        }

        private void ScaleSelectedTarget(float multiplier)
        {
            if (!CanPerformAction())
            {
                return;
            }
            if (!_featureMaps.ScaleSelectedTarget(multiplier))
            {
                _lessonUI.ShowSelectScaleTarget();
            }
        }

        private void MoveToPreviousStep()
        {
            switch (_step)
            {
                case LessonStep.CompareFilters:
                    _step = LessonStep.BuildFeatureMap;
                    break;
                case LessonStep.TraceReceptiveField:
                    _step = LessonStep.CompareFilters;
                    break;
                case LessonStep.EditInput:
                    _step = LessonStep.TraceReceptiveField;
                    break;
            }
            ApplyStep();
        }

        private void MoveToNextStep()
        {
            if (_step == LessonStep.ExploreFeatureMaps)
            {
                if (!_mapLessonComplete) { StartCoroutine(BeginActivationLesson()); }
                return;
            }
            switch (_step)
            {
                case LessonStep.BuildFeatureMap:
                    _step = LessonStep.CompareFilters;
                    break;
                case LessonStep.CompareFilters:
                    _step = LessonStep.TraceReceptiveField;
                    break;
                case LessonStep.TraceReceptiveField:
                    _step = LessonStep.EditInput;
                    break;
                case LessonStep.EditInput:
                    _lastStepSkipped = _featureMaps.EditCount == 0;
                    _lessonUI.ShowStepFinished(_lastStepSkipped);
                    return;
            }
            ApplyStep();
        }

        private void ApplyStep()
        {
            _featureMaps.ShowStep(_step, _channel);
        }

        private void RestoreLesson()
        {
            if (!_featureMaps.TryRestore())
            {
                return;
            }
            _lastStepSkipped = false;
            _mapLessonComplete = false;
            ApplyStep();
        }

        private void UpdateButtons()
        {
            _lessonUI.UpdateButtons(_step, _featureMaps.NextSpreadLayer, _featureMaps.IsScanning);
        }

        private void OnDisable()
        {
            _filterComparison.OnCompleted -= FinishFilterLesson;
            _activationLesson.OnCompleted -= HandleActivationCompleted;
            _lessonUI.OnPreviousRequested -= HandlePreviousRequested;
            _lessonUI.OnNextRequested -= HandleNextRequested;
            _lessonUI.OnScanRequested -= HandleScanRequested;
            _lessonUI.OnFilterRequested -= HandleFilterRequested;
            _lessonUI.OnSpreadRequested -= HandleSpreadRequested;
            _lessonUI.OnRestoreRequested -= HandleRestoreRequested;
            _lessonUI.OnEnlargeRequested -= HandleEnlargeRequested;
            _lessonUI.OnShrinkRequested -= HandleShrinkRequested;
            _featureMaps.OnStepApplied -= _lessonUI.ShowLesson;
            _featureMaps.OnScanningChanged -= UpdateButtons;
            _featureMaps.OnPatchEvaluated -= _lessonUI.ShowPatch;
            _featureMaps.OnAffectedPixelSelected -= _lessonUI.ShowAffectedPixel;
            _featureMaps.OnInputEdited -= _lessonUI.ShowInputEdited;
        }
    }
}
