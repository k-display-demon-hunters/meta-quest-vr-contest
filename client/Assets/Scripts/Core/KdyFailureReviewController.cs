using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyFailureReviewController : MonoBehaviour
    {
        [SerializeField] private bool _showDirectHint;
        [SerializeField] private Text _status;
        [SerializeField] private Text _scoreLabel;
        [SerializeField] private LineRenderer _lossGraph;
        [SerializeField] private GameObject _plateauHighlight;
        [SerializeField] private Renderer _inputSurface;
        [SerializeField] private Renderer _outputSurface;
        [SerializeField] private GameObject _recommendation;
        [SerializeField] private Vector2 _graphSize = new Vector2(2.4f, 1.1f);

        private KdyLessonProgressController _progress;
        private Material _originalInputMaterial;
        private Material _originalOutputMaterial;
        private Material _inputMaterial;
        private Material _outputMaterial;
        private Texture2D _inputTexture;
        private Texture2D _outputTexture;
        private bool _initialized;
        private string _hint;

        public bool CanRetry { get; private set; }
        public bool HasFailureResult { get; private set; }
        public string Hint => _hint;
        public event Action Changed;

        public void Initialize(Camera viewCamera, KdyLessonProgressController progress)
        {
            if (_initialized)
            {
                return;
            }
            if (_status == null || _scoreLabel == null || _lossGraph == null)
            {
                Debug.LogError("[KdyLearning] Failure review needs status, score and loss graph references.", this);
                return;
            }
            _progress = progress;
            _lossGraph.useWorldSpace = false;
            _lossGraph.positionCount = 0;
            _plateauHighlight?.SetActive(false);
            _recommendation?.SetActive(false);
            bool hasRecordedResult = _progress != null && _progress.HasTrainingResult;
            float improvement = hasRecordedResult ? _progress.LastImprovement : 0f;
            HasFailureResult = hasRecordedResult && improvement < _progress.TargetImprovement;
            CreatePreview(_inputSurface, KdyTrainingExampleData.CreateBlurredImage(), "Kdy Failure Blurred Input",
                out _originalInputMaterial, out _inputMaterial, out _inputTexture);
            CreatePreview(_outputSurface, KdyTrainingExampleData.BlendForImprovement(improvement), "Kdy Failure Previous Output",
                out _originalOutputMaterial, out _outputMaterial, out _outputTexture);
            _initialized = true;
            if (!hasRecordedResult)
            {
                _scoreLabel.text = "학습 결과 기록 없음";
                SetMessage("검토할 실패 기록이 없습니다.",
                    "설계실에서 신경망을 설계하고 먼저 학습해 보세요. 기록이 없을 때 실패 결과나 손실 곡선을 만들어 내지 않습니다.");
                return;
            }
            ShowRecordedScore();
            if (!HasFailureResult)
            {
                SetMessage("직전 학습은 목표 개선율을 달성한 기록입니다.",
                    "직전 결과가 통과 기준을 넘었으므로 실패로 표시하지 않습니다. 설계실에서 다른 구조를 비교할 수 있습니다.");
                return;
            }
            CanRetry = _progress.LastTrainedNetwork != null && _progress.LastTrainedNetwork.IsComplete &&
                _progress.TrainingAttempts > 0;
            if (!CanRetry)
            {
                SetMessage("직전 결과는 목표 미달이지만, 재현할 설계 기록이 없습니다.",
                    "이전 설계 정보가 없어 곡선을 임의로 만들지 않습니다. 설계실에서 구조를 다시 완성해 주세요.");
                return;
            }
            DrawRecordedCurve();
            bool directHint = _showDirectHint && _progress.FailureCount >= 3;
            _recommendation?.SetActive(directHint);
            if (directHint)
            {
                SetMessage("이 간판 예제에서는 은닉층 3개 · 피쳐맵 32개 · ReLU 조합을 비교해 보세요.",
                    "이 교육용 간판에서는 3층·32장·ReLU 조합이 목표를 넘습니다. 필터는 3×3과 9×9 모두 비교할 수 있어요. 설계실에서 직접 바꿔 보세요.");
            }
            else
            {
                SetMessage("직전 학습이 목표 개선율에 도달하지 못했어요. 강조한 후반 구간의 오차 변화를 살펴보세요.",
                    "태블릿의 힌트에서 은닉층·피쳐맵·필터·활성화 함수를 다시 살펴보세요. 같은 설계로 재학습하거나 설계를 바꿔 결과를 비교할 수 있습니다.");
            }
        }

        private void DrawRecordedCurve()
        {
            float[] curve = KdyTrainingExampleData.CreateLossCurve(_progress.LastTrainedNetwork, _progress.TrainingAttempts);
            float baseline = Mathf.Max(.000001f, KdyTrainingExampleData.BaselineError);
            _lossGraph.positionCount = curve.Length;
            for (int index = 0; index < curve.Length; index++)
            {
                float x = index / (float)(curve.Length - 1) * _graphSize.x;
                float y = Mathf.Clamp01(curve[index] / baseline) * _graphSize.y;
                _lossGraph.SetPosition(index, new Vector3(x, y, 0f));
            }
            if (_plateauHighlight != null)
            {
                // This band marks the authored curve's final region; it does not diagnose an optimizer problem.
                Vector3 localCenter = new Vector3(_graphSize.x * .9f,
                    Mathf.Clamp01(curve[curve.Length - 1] / baseline) * _graphSize.y, .035f);
                _plateauHighlight.transform.position = _lossGraph.transform.TransformPoint(localCenter);
                _plateauHighlight.SetActive(true);
            }
        }

        private void ShowRecordedScore()
        {
            float baseline = KdyTrainingExampleData.BaselineError;
            float finalError = baseline * (1f - _progress.LastImprovement / 100f);
            string score = _progress.LastImprovement.ToString("0.0", CultureInfo.InvariantCulture);
            string target = _progress.TargetImprovement.ToString("0.#", CultureInfo.InvariantCulture);
            string before = baseline.ToString("0.0000", CultureInfo.InvariantCulture);
            string after = finalError.ToString("0.0000", CultureInfo.InvariantCulture);
            _scoreLabel.text = $"직전 개선율 {score}% / 목표 {target}%\nRGB 평균 제곱 오차 {before} → {after}";
            KdyNetworkConfiguration configuration = _progress.LastTrainedNetwork;
            if (configuration != null && configuration.IsComplete)
            {
                string activation = configuration.UseSigmoid ? "Sigmoid" : "ReLU";
                _scoreLabel.text += $"\n직전 설계: {configuration.LayerCount}층 · 피쳐맵 {configuration.FeatureMaps}개 · {configuration.KernelSize}×{configuration.KernelSize} · {activation}";
            }
            _scoreLabel.text += $"\n학습 {_progress.TrainingAttempts}회 / 누적 목표 미달 {_progress.FailureCount}회";
        }

        private void CreatePreview(Renderer surface, Color[] pixels, string assetName,
            out Material original, out Material material, out Texture2D texture)
        {
            original = null;
            material = null;
            texture = null;
            if (surface == null || surface.sharedMaterial == null)
            {
                return;
            }
            original = surface.sharedMaterial;
            int width = KdySignExampleData.Width;
            int height = KdySignExampleData.Height;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = assetName;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, height - 1 - y, pixels[y * width + x]);
                }
            }
            texture.Apply(false, false);
            material = new Material(original);
            material.name = assetName + " Material";
            material.mainTexture = texture;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }
            surface.sharedMaterial = material;
        }

        private void SetMessage(string message, string hint)
        {
            _status.text = "교육용 학습 시뮬레이션 · 결과 검토\n" + message;
            _hint = hint;
            Changed?.Invoke();
        }

        private void ReleasePreview(Renderer surface, Material original, Material material, Texture2D texture)
        {
            if (surface != null && material != null && surface.sharedMaterial == material)
            {
                surface.sharedMaterial = original;
            }
            if (material != null)
            {
                Destroy(material);
            }
            if (texture != null)
            {
                Destroy(texture);
            }
        }

        private void OnDestroy()
        {
            ReleasePreview(_inputSurface, _originalInputMaterial, _inputMaterial, _inputTexture);
            ReleasePreview(_outputSurface, _originalOutputMaterial, _outputMaterial, _outputTexture);
        }
    }
}
