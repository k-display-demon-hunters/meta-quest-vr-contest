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
        private bool _ready;
        private LessonStep _step = LessonStep.BuildFeatureMap;
        private int _channel;
        private bool _lastStepSkipped;

        public bool ReadyForModelDesign => _ready && _step == LessonStep.EditInput
            && (_featureMaps.EditCount > 0 || _lastStepSkipped) && !_featureMaps.HasHeldTargets;

        private void Awake()
        {
            // Wire modules before child grab components begin receiving XR events.
            _featureMaps.Initialize();
        }

        private void OnEnable()
        {
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

        private void Start()
        {
            _ready = _featureMaps.TryStart();
            if (_ready)
            {
                ApplyStep();
            }
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
            ApplyStep();
        }

        private void UpdateButtons()
        {
            _lessonUI.UpdateButtons(_step, _featureMaps.NextSpreadLayer, _featureMaps.IsScanning);
        }

        private void OnDisable()
        {
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
