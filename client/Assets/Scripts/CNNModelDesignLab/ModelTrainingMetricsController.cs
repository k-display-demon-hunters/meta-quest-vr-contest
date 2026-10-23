using System;
using System.Collections.Generic;
using System.Globalization;
using CNNTraining;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene
{
    internal sealed class ModelTrainingMetricsController
    {
        private readonly ModelResultsUIBuilder _ui;
        private Text _epochText;
        private Text _trainingMetrics;
        private Text _validationMetrics;
        private Text _evaluationMetrics;
        private Text _accuracyAxis;
        private Text _lossAxis;
        private RectTransform _epochBar;
        private ModelMetricGraph _accuracyGraph;
        private ModelMetricGraph _lossGraph;

        public ModelTrainingMetricsController(RectTransform root, ModelResultsUIBuilder ui)
        {
            _ui = ui;
            _epochText = _ui.Label(root, "Epoch", string.Empty, 30, 204, 1140, 28, 22);
            Image progressTrack = _ui.Box(root, "Epoch progress", 30, 242, 1140, 10, ModelResultsUIBuilder.ButtonColor);
            _epochBar = _ui.Box(progressTrack.transform, "Progress", 0, 0, 0, 10, ModelResultsUIBuilder.TrainingColor).rectTransform;
            _trainingMetrics = _ui.Label(root, "Training metrics", string.Empty, 30, 270, 555, 36, 24);
            _trainingMetrics.color = ModelResultsUIBuilder.TrainingColor;
            _validationMetrics = _ui.Label(root, "Validation metrics", string.Empty, 615, 270, 555, 36, 24);
            _validationMetrics.color = ModelResultsUIBuilder.ValidationColor;
            _evaluationMetrics = _ui.Label(root, "Final test metrics", string.Empty, 30, 320, 1140, 40, 27);
            _ui.Label(root, "Accuracy title", "정확도", 30, 376, 280, 28, 23);
            _ui.Label(root, "Loss title", "손실", 618, 376, 280, 28, 23);
            _ui.Label(root, "Training legend", "━ 학습", 877, 376, 130, 28, 21).color = ModelResultsUIBuilder.TrainingColor;
            _ui.Label(root, "Validation legend", "━ 검증", 1020, 376, 150, 28, 21).color = ModelResultsUIBuilder.ValidationColor;
            _accuracyGraph = CreateGraph(root, "Accuracy chart", 30, true);
            _lossGraph = CreateGraph(root, "Loss chart", 618, false);
            _accuracyAxis = _ui.Label(root, "Accuracy axis", string.Empty, 30, 582, 555, 28, 18);
            _lossAxis = _ui.Label(root, "Loss axis", string.Empty, 618, 582, 552, 28, 18);
            _accuracyAxis.color = _lossAxis.color = ModelResultsUIBuilder.MutedColor;
        }

        public void ResetTraining()
        {
            _accuracyGraph.Clear();
            _lossGraph.Clear();
            _epochText.text = "에포크 — / — · 학습 준비 중";
            SetBarWidth(_epochBar, 0);
            _trainingMetrics.text = "학습  정확도 —   손실 —";
            _validationMetrics.text = "검증  정확도 —   손실 —";
            _evaluationMetrics.text = "최종 테스트  정확도 —   손실 —";
            _accuracyAxis.text = "0–100% · 에포크 —";
            _lossAxis.text = "손실 범위 — · 에포크 —";
        }

        public void ShowProgress(TrainingProgress progress)
        {
            if (progress == null)
            {
                return;
            }
            ShowEpoch(progress.Epoch, progress.Epochs, progress.Logs);
        }

        public void ShowCompleted(TrainingResult result)
        {
            if (result == null)
            {
                return;
            }
            // Completion includes the last epoch and the held-out test evaluation.
            ShowEpoch(result.Epoch, result.Epoch, result.Logs);
            _evaluationMetrics.text = "최종 테스트  정확도 " + FormatMetric(result.Evaluation, "accuracy", true)
                + "   손실 " + FormatMetric(result.Evaluation, "loss", false);
        }

        private void ShowEpoch(int epoch, int totalEpochs, IReadOnlyDictionary<string, double> logs)
        {
            _epochText.text = "에포크 " + epoch + " / " + totalEpochs;
            SetBarWidth(_epochBar, totalEpochs > 0 ? 1140 * Mathf.Clamp01((float)epoch / totalEpochs) : 0);
            _trainingMetrics.text = "학습  정확도 " + FormatMetric(logs, "accuracy", true)
                + "   손실 " + FormatMetric(logs, "loss", false);
            _validationMetrics.text = "검증  정확도 " + FormatMetric(logs, "val_accuracy", true)
                + "   손실 " + FormatMetric(logs, "val_loss", false);
            _accuracyGraph.AddPoint(epoch, totalEpochs, ReadMetric(logs, "accuracy"), ReadMetric(logs, "val_accuracy"));
            _lossGraph.AddPoint(epoch, totalEpochs, ReadMetric(logs, "loss"), ReadMetric(logs, "val_loss"));
            _accuracyAxis.text = "0–100% · 가로축 에포크 0–" + totalEpochs;
            _lossAxis.text = FormatNumber(_lossGraph.Minimum) + "–" + FormatNumber(_lossGraph.Maximum)
                + " · 가로축 에포크 0–" + totalEpochs;
        }

        private ModelMetricGraph CreateGraph(Transform parent, string name, float x, bool accuracy)
        {
            RectTransform rect = _ui.Rect(parent, name, x, 414, 552, 160);
            ModelMetricGraph graph = rect.gameObject.AddComponent<ModelMetricGraph>();
            graph.Initialize(accuracy, ModelResultsUIBuilder.TrainingColor, ModelResultsUIBuilder.ValidationColor);
            return graph;
        }

        private static double? ReadMetric(IReadOnlyDictionary<string, double> metrics, string key)
        {
            if (metrics != null && metrics.TryGetValue(key, out double value)
                && !double.IsNaN(value) && !double.IsInfinity(value))
            {
                return value;
            }
            return null;
        }

        private static string FormatMetric(IReadOnlyDictionary<string, double> metrics, string key, bool percentage)
        {
            double? value = ReadMetric(metrics, key);
            return value.HasValue ? (percentage ? FormatPercentage(value.Value) : FormatNumber(value.Value)) : "—";
        }

        private static string FormatPercentage(double value)
        {
            return (value * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatNumber(double value)
        {
            double magnitude = Math.Abs(value);
            string format = magnitude > 0 && (magnitude < 0.0001 || magnitude >= 10000) ? "G5" : "0.####";
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static void SetBarWidth(RectTransform bar, float width)
        {
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
    }
}
