using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyTrainingPlaybackController : MonoBehaviour
    {
        private const string SimulationLabel = "교육용 학습 시뮬레이션";
        private const int PhaseCount = 5;
        private const float OptimizationStart = .6f;
        private const float OptimizationEnd = .8f;

        [SerializeField] private Renderer _inputSurface;
        [SerializeField] private Renderer _outputSurface;
        [SerializeField] private Renderer _targetSurface;
        [SerializeField] private LineRenderer _lossGraph;
        [SerializeField] private Transform[] _reversePulses;
        [SerializeField] private Transform[] _networkStages;
        [SerializeField] private Text _status;
        [SerializeField] private Text _scoreLabel;
        [SerializeField] private Text[] _filterValues;
        [SerializeField] private GameObject[] _hiddenStages;
        [SerializeField] private Transform[] _featurePanels = new Transform[96];
        [SerializeField] private float _durationSeconds = 18f;
        [SerializeField] private Vector2 _graphSize = new Vector2(2.4f, 1.1f);

        private readonly float[] _initialWeights =
        {
            .04f, -.03f, .02f, .01f, .05f, -.04f, -.02f, .03f, .01f,
            -.02f, .02f, .01f, .05f, -.01f, .03f, .00f, -.04f, .02f,
            .03f, .01f, -.02f, -.03f, .02f, .04f, .01f, -.01f, .02f
        };
        private readonly float[] _adjustedWeights =
        {
            -.28f, .06f, .32f, -.45f, .10f, .48f, -.25f, .04f, .29f,
            -.31f, -.42f, -.27f, .08f, .16f, .06f, .28f, .46f, .30f,
            -.10f, -.24f, -.11f, -.21f, .85f, -.19f, -.12f, -.25f, -.10f
        };
        private readonly KdyNetworkConfiguration _configuration = new KdyNetworkConfiguration();
        private readonly Transform[] _reversePath = new Transform[5];
        private KdyLessonProgressController _progress;
        private Material _originalInputMaterial;
        private Material _originalOutputMaterial;
        private Material _originalTargetMaterial;
        private Material _inputMaterial;
        private Material _outputMaterial;
        private Material _targetMaterial;
        private Texture2D _inputTexture;
        private Texture2D _outputTexture;
        private Texture2D _targetTexture;
        private Color[][] _outputFrames;
        private float[] _lossCurve;
        private float _plannedImprovement;
        private float _targetImprovement;
        private int _attempt;
        private int _phase = -1;
        private int _lastCurveIndex = -1;
        private int _reverseSegmentCount;
        private bool _initialized;
        private bool _running;
        private bool _resultRecorded;
        private string _hint;

        public bool IsComplete { get; private set; }
        public bool DidPass => IsComplete && _plannedImprovement >= _targetImprovement;
        public bool CanReplay => _initialized && !_running && HasCompleteDesign;
        public string Hint => _hint;
        public event Action Changed;

        private bool HasCompleteDesign => _progress != null && _progress.Network != null && _progress.Network.IsComplete;

        public void Initialize(Camera viewCamera, KdyLessonProgressController progress)
        {
            if (_initialized)
            {
                return;
            }
            if (!HasRequiredReferences())
            {
                Debug.LogError("[KdyLearning] Training playback needs two image surfaces, graph, four pulses, five network stages and three hidden-stage labels.", this);
                return;
            }
            _progress = progress;
            CreateImageSurfaces();
            _lossGraph.useWorldSpace = false;
            _initialized = true;
            Replay();
        }

        public void Replay()
        {
            if (!_initialized || _running)
            {
                return;
            }
            StopAllCoroutines();
            IsComplete = false;
            _resultRecorded = false;
            _phase = -1;
            _lastCurveIndex = -1;
            _lossGraph.positionCount = 0;
            HideReversePulses();
            WriteTexture(_outputTexture, KdyTrainingExampleData.CreateBlurredImage());
            if (!HasCompleteDesign)
            {
                ApplyVisibleLayers(0);
                _scoreLabel.text = "설계 대기\n층 · 피쳐맵 수 · 필터 크기 · 활성화 함수를 선택하세요.";
                SetMessage("신경망 설계가 아직 완성되지 않았어요.",
                    "먼저 설계실에서 은닉층과 세 가지 부품을 선택하세요. 설계 데이터가 없으면 학습 결과를 만들지 않습니다.");
                return;
            }
            _configuration.CopyFrom(_progress.Network);
            _attempt = _progress.TrainingAttempts + 1;
            _targetImprovement = _progress.TargetImprovement;
            _plannedImprovement = KdyTrainingExampleData.GetImprovement(_configuration, _attempt);
            _lossCurve = KdyTrainingExampleData.CreateLossCurve(_configuration, _attempt);
            PrepareOutputFrames();
            ApplyVisibleLayers(_configuration.LayerCount);
            PrepareReversePath();
            _running = true;
            SetPhase(0);
            UpdateLearningDisplay(0f);
            StartCoroutine(PlayTraining());
        }

        private bool HasRequiredReferences()
        {
            if (_inputSurface == null || _outputSurface == null || _lossGraph == null ||
                _status == null || _scoreLabel == null ||
                _inputSurface.sharedMaterial == null || _outputSurface.sharedMaterial == null ||
                _reversePulses == null || _reversePulses.Length != 4 ||
                _networkStages == null || _networkStages.Length != 5 ||
                _filterValues == null || _filterValues.Length != 3 ||
                _hiddenStages == null || _hiddenStages.Length != 3)
            {
                return false;
            }
            foreach (Transform stage in _networkStages)
            {
                if (stage == null)
                {
                    return false;
                }
            }
            foreach (Transform pulse in _reversePulses)
            {
                if (pulse == null)
                {
                    return false;
                }
            }
            for (int i = 0; i < _hiddenStages.Length; i++)
            {
                if (_hiddenStages[i] == null || _filterValues[i] == null)
                {
                    return false;
                }
            }
            return true;
        }

        private IEnumerator PlayTraining()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(5f, _durationSeconds);
            while (elapsed < duration)
            {
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                int phase = Mathf.Min(PhaseCount - 1, Mathf.FloorToInt(normalizedTime * PhaseCount));
                if (phase != _phase)
                {
                    SetPhase(phase);
                }
                UpdateLearningDisplay(normalizedTime);
                UpdateReversePulses(normalizedTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            UpdateLearningDisplay(1f);
            HideReversePulses();
            _running = false;
            IsComplete = true;
            if (!_resultRecorded)
            {
                // A completed playback records one result; opening a scene without a design records nothing.
                _resultRecorded = true;
                _progress.RecordTrainingResult(_plannedImprovement, _configuration);
            }
            string score = _plannedImprovement.ToString("0.0", CultureInfo.InvariantCulture);
            string target = _targetImprovement.ToString("0.#", CultureInfo.InvariantCulture);
            if (DidPass)
            {
                SetMessage($"이번 개선율 {score}% / 목표 {target}% — 목표 달성!",
                    "정해진 교육용 사례에서 목표를 달성했어요. 설계를 바꾸거나 같은 설계로 재학습하며 결과를 비교해 보세요.");
            }
            else
            {
                SetMessage($"이번 개선율 {score}% / 목표 {target}% — 목표에 아직 도달하지 못했어요.",
                    "결과를 확인하고 설계를 바꾸거나 같은 설계로 재학습해 보세요. 모든 데이터에서 층이나 피쳐맵을 늘리면 좋아진다는 뜻은 아닙니다.");
            }
        }

        private void SetPhase(int phase)
        {
            _phase = phase;
            string stage;
            string hint;
            switch (phase)
            {
                case 0:
                    stage = "1 / 5 · 흐릿한 간판을 신경망에 넣습니다.";
                    hint = "왼쪽은 흐릿한 입력, 오른쪽은 현재 출력입니다. 선택한 은닉층 개수만큼 공정이 켜졌는지 살펴보세요.";
                    break;
                case 1:
                    stage = "2 / 5 · 현재 출력과 선명한 정답 사이의 오차를 비교합니다.";
                    hint = "오차는 출력과 정답의 차이를 숫자로 나타낸 값이에요. 여기서는 RGB의 평균 제곱 오차를 사용합니다.";
                    break;
                case 2:
                    stage = "3 / 5 · 오차 정보를 출력 쪽에서 입력 쪽으로 전달합니다.";
                    hint = "빛이 거꾸로 이동하는 방향을 따라가 보세요. 역전파는 출력의 오차로 앞쪽 필터의 수정 방향을 구하는 과정입니다.";
                    break;
                case 3:
                    stage = "4 / 5 · 필터 값을 조금씩 바꾸며 교육용 오차 곡선을 재생합니다.";
                    hint = "필터 숫자와 출력 이미지, 오차 곡선이 함께 달라져요. 이 장면은 실제 모델 학습 없이 정해진 예시를 보여줍니다.";
                    break;
                default:
                    stage = "5 / 5 · 최종 출력과 오차 개선율을 확인합니다.";
                    hint = "개선율은 처음 오차에 비해 오차가 얼마나 줄었는지를 뜻해요. 분류 정확도가 아니라 이 간판 복원 예제의 오차 개선율입니다.";
                    break;
            }
            SetMessage(stage, hint);
        }

        private void UpdateLearningDisplay(float normalizedTime)
        {
            float optimization = Mathf.InverseLerp(OptimizationStart, OptimizationEnd, normalizedTime);
            int curveIndex = Mathf.Clamp(Mathf.FloorToInt(optimization * (_lossCurve.Length - 1)), 0, _lossCurve.Length - 1);
            if (curveIndex == _lastCurveIndex)
            {
                return;
            }
            _lastCurveIndex = curveIndex;
            _lossGraph.positionCount = curveIndex + 1;
            float baseline = Mathf.Max(.000001f, KdyTrainingExampleData.BaselineError);
            for (int index = 0; index <= curveIndex; index++)
            {
                float x = index / (float)(_lossCurve.Length - 1) * _graphSize.x;
                float y = Mathf.Clamp01(_lossCurve[index] / baseline) * _graphSize.y;
                _lossGraph.SetPosition(index, new Vector3(x, y, 0f));
            }
            WriteTexture(_outputTexture, _outputFrames[curveIndex]);
            float improvement = Mathf.Clamp((1f - _lossCurve[curveIndex] / baseline) * 100f, 0f, 100f);
            _scoreLabel.text = "개선율 " + improvement.ToString("0.0", CultureInfo.InvariantCulture) + "% / 목표 " +
                _targetImprovement.ToString("0.#", CultureInfo.InvariantCulture) + "%\nRGB 평균 제곱 오차 " +
                _lossCurve[curveIndex].ToString("0.0000", CultureInfo.InvariantCulture);
            UpdateFilterWeights(curveIndex / (float)(_lossCurve.Length - 1));
        }

        private void PrepareOutputFrames()
        {
            float baseline = Mathf.Max(.000001f, KdyTrainingExampleData.BaselineError);
            _outputFrames = new Color[_lossCurve.Length][];
            for (int index = 0; index < _lossCurve.Length; index++)
            {
                float improvement = Mathf.Clamp((1f - _lossCurve[index] / baseline) * 100f, 0f, 100f);
                _outputFrames[index] = KdyTrainingExampleData.BlendForImprovement(improvement);
            }
        }

        private void ApplyVisibleLayers(int layerCount)
        {
            for (int i = 0; i < _hiddenStages.Length; i++)
            {
                bool visible = i < layerCount;
                _hiddenStages[i].SetActive(visible);
                _filterValues[i].gameObject.SetActive(visible);
            }
            if (_featurePanels != null)
            {
                for (int index = 0; index < _featurePanels.Length; index++)
                {
                    if (_featurePanels[index] != null)
                    {
                        bool visible = index / 32 < layerCount && index % 32 < _configuration.FeatureMaps;
                        _featurePanels[index].gameObject.SetActive(visible);
                    }
                }
            }
        }

        private void PrepareReversePath()
        {
            int index = 0;
            _reversePath[index++] = _networkStages[4];
            for (int layer = _configuration.LayerCount; layer >= 1; layer--)
            {
                _reversePath[index++] = _networkStages[layer];
            }
            _reversePath[index] = _networkStages[0];
            _reverseSegmentCount = index;
        }

        private void UpdateReversePulses(float normalizedTime)
        {
            if (_phase != 2)
            {
                HideReversePulses();
                return;
            }
            float phaseTime = Mathf.InverseLerp(.4f, .6f, normalizedTime);
            for (int i = 0; i < _reversePulses.Length; i++)
            {
                bool visible = i < _reverseSegmentCount;
                _reversePulses[i].gameObject.SetActive(visible);
                if (visible)
                {
                    float segmentTime = Mathf.Repeat(phaseTime * 2.5f - i * .18f, 1f);
                    _reversePulses[i].position = Vector3.Lerp(_reversePath[i].position, _reversePath[i + 1].position, segmentTime);
                }
            }
        }

        private void HideReversePulses()
        {
            foreach (Transform pulse in _reversePulses)
            {
                pulse.gameObject.SetActive(false);
            }
        }

        private void UpdateFilterWeights(float progress)
        {
            float blend = progress * Mathf.Clamp01(_plannedImprovement / 100f);
            for (int layer = 0; layer < _configuration.LayerCount; layer++)
            {
                StringBuilder values = new StringBuilder();
                values.Append(_configuration.KernelSize == 9 ? "9×9 한 채널의 중앙3×3 예시\n" : "3×3 한 채널의 필터 예시\n");
                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 3; x++)
                    {
                        int index = layer * 9 + y * 3 + x;
                        float value = Mathf.Lerp(_initialWeights[index], _adjustedWeights[index], blend);
                        if (x > 0)
                        {
                            values.Append("  ");
                        }
                        values.Append(value.ToString("0.00", CultureInfo.InvariantCulture));
                    }
                    values.Append('\n');
                }
                _filterValues[layer].text = values.ToString();
            }
        }

        private void CreateImageSurfaces()
        {
            _inputTexture = CreateTexture("Kdy Training Blurred Input");
            _outputTexture = CreateTexture("Kdy Training Current Output");
            Color[] blurred = KdyTrainingExampleData.CreateBlurredImage();
            WriteTexture(_inputTexture, blurred);
            WriteTexture(_outputTexture, blurred);
            _originalInputMaterial = _inputSurface.sharedMaterial;
            _originalOutputMaterial = _outputSurface.sharedMaterial;
            _inputMaterial = CreateDisplayMaterial(_originalInputMaterial, _inputTexture, "Kdy Training Input Material");
            _outputMaterial = CreateDisplayMaterial(_originalOutputMaterial, _outputTexture, "Kdy Training Output Material");
            _inputSurface.sharedMaterial = _inputMaterial;
            _outputSurface.sharedMaterial = _outputMaterial;
            if (_targetSurface != null && _targetSurface.sharedMaterial != null)
            {
                _targetTexture = CreateTexture("Kdy Training Clear Target");
                WriteTexture(_targetTexture, KdyTrainingExampleData.CreateClearImage());
                _originalTargetMaterial = _targetSurface.sharedMaterial;
                _targetMaterial = CreateDisplayMaterial(_originalTargetMaterial, _targetTexture, "Kdy Training Target Material");
                _targetSurface.sharedMaterial = _targetMaterial;
            }
        }

        private Texture2D CreateTexture(string textureName)
        {
            Texture2D texture = new Texture2D(KdySignExampleData.Width, KdySignExampleData.Height, TextureFormat.RGBA32, false);
            texture.name = textureName;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private void WriteTexture(Texture2D texture, Color[] topRowFirstPixels)
        {
            int width = KdySignExampleData.Width;
            int height = KdySignExampleData.Height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, height - 1 - y, topRowFirstPixels[y * width + x]);
                }
            }
            texture.Apply(false, false);
        }

        private Material CreateDisplayMaterial(Material original, Texture2D texture, string materialName)
        {
            Material material = new Material(original);
            material.name = materialName;
            material.mainTexture = texture;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }
            return material;
        }

        private void SetMessage(string stage, string hint)
        {
            _status.text = SimulationLabel + "\n" + stage;
            _hint = hint;
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            if (_inputSurface != null && _inputSurface.sharedMaterial == _inputMaterial)
            {
                _inputSurface.sharedMaterial = _originalInputMaterial;
            }
            if (_outputSurface != null && _outputSurface.sharedMaterial == _outputMaterial)
            {
                _outputSurface.sharedMaterial = _originalOutputMaterial;
            }
            if (_targetSurface != null && _targetSurface.sharedMaterial == _targetMaterial)
            {
                _targetSurface.sharedMaterial = _originalTargetMaterial;
            }
            if (_targetMaterial != null)
            {
                Destroy(_targetMaterial);
            }
            if (_targetTexture != null)
            {
                Destroy(_targetTexture);
            }
            if (_inputMaterial != null)
            {
                Destroy(_inputMaterial);
            }
            if (_outputMaterial != null)
            {
                Destroy(_outputMaterial);
            }
            if (_inputTexture != null)
            {
                Destroy(_inputTexture);
            }
            if (_outputTexture != null)
            {
                Destroy(_outputTexture);
            }
        }
    }
}
