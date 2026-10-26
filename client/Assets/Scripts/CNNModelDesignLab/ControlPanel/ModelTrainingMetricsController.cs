using System;
using System.Collections.Generic;
using System.Globalization;
using ModelDesignScene.Execution.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene.ControlPanel
{
    public sealed class ModelTrainingMetricsController : MonoBehaviour
    {
        [SerializeField] private Text _epochText;
        [SerializeField] private Text _trainingMetrics;
        [SerializeField] private Text _validationMetrics;
        [SerializeField] private Text _evaluationMetrics;
        [SerializeField] private Text _accuracyAxis;
        [SerializeField] private Text _lossAxis;
        [SerializeField] private Text _learningHint;
        [SerializeField] private Image _epochBar;
        [SerializeField] private ModelMetricGraph _accuracyGraph;
        [SerializeField] private ModelMetricGraph _lossGraph;
        [SerializeField] private Color _trainingColor = new Color(0.27f, 0.81f, 0.91f);
        [SerializeField] private Color _validationColor = new Color(1f, 0.72f, 0.29f);

        internal void Initialize()
        {
            _accuracyGraph.Initialize(true, _trainingColor, _validationColor);
            _lossGraph.Initialize(false, _trainingColor, _validationColor);
            ResetTraining();
        }

        internal void ResetTraining()
        {
            _accuracyGraph.Clear();
            _lossGraph.Clear();
            _epochText.text = "에포크 — / — · 학습 준비 중";
            SetProgress(_epochBar, 0f);
            _trainingMetrics.text = "정확도 —\n손실 —";
            _validationMetrics.text = "정확도 —\n손실 —";
            _evaluationMetrics.text = "정확도 —\n손실 —";
            _accuracyAxis.text = "0–100% · 에포크 —";
            _lossAxis.text = "손실 범위 — · 에포크 —";
            _learningHint.text = "정확도는 맞힌 비율, 손실은 예측 오차를 나타냅니다. 학습과 검증 곡선의 변화를 함께 살펴보세요.";
        }

        internal void ShowProgress(TrainingProgress progress)
        {
            if (progress == null)
            {
                return;
            }
            ShowEpoch(progress.Epoch, progress.Epochs, progress.Logs);
        }

        internal void ShowCompleted(TrainingResult result)
        {
            if (result == null)
            {
                return;
            }
            // Completion includes the last epoch and the held-out test evaluation.
            ShowEpoch(result.Epoch, result.Epoch, result.Logs);
            _evaluationMetrics.text = "정확도 " + FormatMetric(result.Evaluation, "accuracy", true)
                + "\n손실 " + FormatMetric(result.Evaluation, "loss", false);
            double? accuracy = ReadMetric(result.Evaluation, "accuracy");
            _learningHint.text = accuracy.HasValue
                ? "테스트 이미지에서 " + FormatPercentage(accuracy.Value) + "를 맞혔습니다. 샘플 예측도 확인하고, 필터 수나 뉴런 수를 한 항목씩 바꿔 재학습 결과를 비교해 보세요."
                : "학습이 끝났지만 테스트 정확도를 받지 못했습니다. 수신한 학습·검증 곡선과 샘플 예측을 확인하세요.";
        }

        private void ShowEpoch(int epoch, int totalEpochs, IReadOnlyDictionary<string, double> logs)
        {
            _epochText.text = "에포크 " + epoch + " / " + totalEpochs;
            SetProgress(_epochBar, totalEpochs > 0 ? (float)epoch / totalEpochs : 0f);
            _trainingMetrics.text = "정확도 " + FormatMetric(logs, "accuracy", true)
                + "\n손실 " + FormatMetric(logs, "loss", false);
            _validationMetrics.text = "정확도 " + FormatMetric(logs, "val_accuracy", true)
                + "\n손실 " + FormatMetric(logs, "val_loss", false);
            double? training = ReadMetric(logs, "accuracy");
            double? validation = ReadMetric(logs, "val_accuracy");
            _accuracyGraph.AddPoint(epoch, totalEpochs, training, validation);
            _lossGraph.AddPoint(epoch, totalEpochs, ReadMetric(logs, "loss"), ReadMetric(logs, "val_loss"));
            _accuracyAxis.text = "0–100% · 가로축 에포크 0–" + totalEpochs;
            _lossAxis.text = FormatNumber(_lossGraph.Minimum) + "–" + FormatNumber(_lossGraph.Maximum)
                + " · 가로축 에포크 0–" + totalEpochs;
            if (training.HasValue && validation.HasValue)
            {
                double gap = (training.Value - validation.Value) * 100;
                string comparison = gap == 0 ? "학습과 검증 정확도가 같습니다."
                    : "학습 정확도가 검증보다 " + Math.Abs(gap).ToString("0.0", CultureInfo.InvariantCulture)
                        + "%p " + (gap > 0 ? "높습니다." : "낮습니다.");
                _learningHint.text = "현재 에포크: " + comparison
                    + " 한 시점의 차이만으로 원인을 단정할 수 없습니다. 검증 정확도와 손실의 추이를 함께 비교하세요.";
            }
            else
            {
                _learningHint.text = "수신한 지표만 표시합니다. 학습과 검증 지표가 함께 있어야 두 결과를 비교할 수 있습니다.";
            }
        }

        private static void SetProgress(Image bar, float value)
        {
            bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
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
    }
}
