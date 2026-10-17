using System;
using Networking;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class ModelResultsController : MonoBehaviour
    {
        [Serializable]
        internal sealed class SampleImage
        {
            [SerializeField] private Texture2D _image;
            [SerializeField] private int _label;

            public Texture2D Image => _image;
            public int Label => _label;
            public string Id => _image.name;
        }

        [SerializeField] private Font _font;
        [SerializeField] private SampleImage[] _samples = Array.Empty<SampleImage>();

        private Text _stateText;
        private Text _errorText;
        private Text _modelChangedText;
        private ModelTrainingMetricsController _metrics;
        private ModelInferenceResultsController _inference;

        public event Action<string> OnInferenceRequested;

        private void Awake()
        {
            ModelResultsUIBuilder ui = new ModelResultsUIBuilder(_font);
            RectTransform root = ui.BuildRoot(transform);
            ui.Label(root, "Title", "모델 학습 결과", 30, 20, 900, 46, 34);
            _stateText = ui.Label(root, "State", string.Empty, 30, 78, 1140, 32, 24);
            _errorText = ui.Label(root, "Error", string.Empty, 30, 115, 1140, 48, 19);
            _errorText.color = new Color(1, 0.55f, 0.49f);
            _errorText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _errorText.resizeTextForBestFit = true;
            _errorText.resizeTextMinSize = 14;
            _errorText.resizeTextMaxSize = 19;
            _modelChangedText = ui.Label(root, "Model changed", string.Empty, 30, 168, 1140, 30, 19);
            _modelChangedText.color = ModelResultsUIBuilder.ValidationColor;
            _metrics = new ModelTrainingMetricsController(root, ui);
            ui.Box(root, "Section divider", 30, 625, 1140, 2, ModelResultsUIBuilder.ButtonColor);
            _inference = new ModelInferenceResultsController(root, ui, _samples, () => isActiveAndEnabled);
            _inference.OnInferenceRequested += HandleInferenceRequested;
            _inference.OnError += ShowError;
            ResetSessionUI();
        }

        public void ResetTraining()
        {
            _metrics.ResetTraining();
            _errorText.text = string.Empty;
            _inference.ResetTraining();
        }

        public void ResetSessionUI()
        {
            ResetTraining();
            _inference.ResetSession();
            _stateText.text = "대기 · 컨베이어 왼쪽에서 학습을 시작하세요.";
            SetModelChanged(false);
        }

        public void ShowProgress(TrainingProgress progress)
        {
            _metrics.ShowProgress(progress);
        }

        public void ShowCompleted(TrainingResult result)
        {
            if (result == null)
            {
                return;
            }
            _metrics.ShowCompleted(result);
            _stateText.text = "학습 완료 · MNIST 샘플을 선택해 예측을 확인하세요.";
            _errorText.text = string.Empty;
        }

        public void ShowInference(InferenceResult result)
        {
            if (_inference.ShowInference(result))
            {
                _errorText.text = string.Empty;
            }
        }

        public void ShowState(ModelExecutionState state, bool hasTrainedModel, bool canInfer)
        {
            switch (state)
            {
                case ModelExecutionState.Connecting:
                    _stateText.text = "학습 서버에 연결하는 중…";
                    break;
                case ModelExecutionState.StartingTraining:
                    _stateText.text = "모델을 전달했습니다. 학습 시작을 기다리는 중…";
                    break;
                case ModelExecutionState.Training:
                    _stateText.text = "학습 중 · 수신한 에포크의 지표를 표시합니다.";
                    break;
                case ModelExecutionState.CancellingTraining:
                    _stateText.text = "학습 취소를 요청했습니다. 서버 응답을 기다리는 중…";
                    break;
                case ModelExecutionState.Completed:
                    _stateText.text = "학습 완료 · MNIST 샘플을 선택해 예측을 확인하세요.";
                    break;
                case ModelExecutionState.Inferring:
                    _stateText.text = "선택한 이미지의 예측을 기다리는 중…";
                    break;
                case ModelExecutionState.Cancelled:
                    _stateText.text = "학습이 취소되었습니다. 다시 학습을 시작할 수 있습니다.";
                    break;
                case ModelExecutionState.Failed:
                    _stateText.text = "요청을 완료하지 못했습니다. 아래 안내를 확인하세요.";
                    break;
                default:
                    _stateText.text = hasTrainedModel ? "학습 모델 준비됨 · 샘플을 선택하세요."
                        : "대기 · 컨베이어 왼쪽에서 학습을 시작하세요.";
                    break;
            }
            _inference.SetAvailability(hasTrainedModel, canInfer);
        }

        public void ShowError(string message)
        {
            _errorText.text = string.IsNullOrWhiteSpace(message) ? "요청을 완료하지 못했습니다." : message;
            _inference.ClearPendingPrediction();
        }

        public void ShowCancelled()
        {
            _stateText.text = "학습 취소됨 · 그래프에는 취소 전 수신한 지표만 남아 있습니다.";
            _errorText.text = string.Empty;
            _inference.ResetTraining();
        }

        public void SetModelChanged(bool changed)
        {
            _modelChangedText.text = changed
                ? "구성이 변경되었습니다. 표시된 결과는 마지막 학습을 시작했을 때의 모델 기준입니다."
                : string.Empty;
        }

        private void HandleInferenceRequested(string imageId)
        {
            _errorText.text = string.Empty;
            OnInferenceRequested?.Invoke(imageId);
        }

        private void OnDestroy()
        {
            if (_inference != null)
            {
                _inference.OnInferenceRequested -= HandleInferenceRequested;
                _inference.OnError -= ShowError;
            }
        }
    }
}
