using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyFactoryInspectionController : MonoBehaviour
    {
        private const int MaximumLayers = 3;
        private const int MaximumMaps = 32;

        [Header("New Sign Input")]
        [SerializeField] private Transform _factorySign;
        [SerializeField] private Transform _inputSlot;
        [SerializeField] private KdyLessonGrabController _signGrab;
        [SerializeField] private Renderer _signSurface;
        [SerializeField] private float _dropRadius = .7f;

        [Header("Forward Passage")]
        [SerializeField] private Transform[] _networkStages;
        [SerializeField] private GameObject[] _hiddenStages;
        [SerializeField] private Renderer[] _featureSurfaces;
        [SerializeField] private Transform[] _forwardPulses;
        [SerializeField] private float _durationSeconds = 4.5f;

        [Header("Result Comparison")]
        [SerializeField] private KdySignComparisonController _comparison;
        [SerializeField] private Text _status;

        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private MaterialPropertyBlock _properties;
        private KdyLessonProgressController _progress;
        private Transform _signHomeParent;
        private Vector3 _signHomePosition;
        private Vector3 _signHomeScale;
        private Quaternion _signHomeRotation;
        private Vector3 _releasedPosition;
        private Rigidbody _signBody;
        private bool _initialized;
        private bool _validTraining;
        private bool _pendingDrop;
        private bool _processing;
        private bool _completed;
        private bool _resetting;
        private string _hint;

        public bool ReadyToContinue => _initialized && _validTraining && _completed &&
            _comparison != null && _comparison.HasCompared;
        public string Hint => _completed && _comparison != null ? _comparison.Hint : _hint;
        public event Action Changed;

        private void LateUpdate()
        {
            if (!_pendingDrop || _signGrab.IsGrabbed)
            {
                return;
            }

            // XRI restores its temporary parent after selectExited; snap after that restoration.
            _pendingDrop = false;
            if (Vector3.Distance(_releasedPosition, _inputSlot.position) > _dropRadius)
            {
                RestoreSign();
                SetMessage("간판을 입력 슬롯의 빛나는 틀 가까이에 놓으세요.",
                    "오른손 Grip으로 새 간판을 잡고 입력 슬롯에 가져간 뒤 놓으세요. 멀리 놓으면 진열대로 돌아갑니다.");
                return;
            }

            _processing = true;
            _signGrab.SetInteractionEnabled(false);
            SetSignPose(_inputSlot.position, _inputSlot.rotation);
            SetMessage("새 간판이 들어왔어요. 빛을 따라 신경망 안을 걸어 보세요.",
                "학습에 쓰지 않은 새 간판을 같은 설계에 넣습니다. 각 평면은 특징 반응을 설명하기 위한 가상 예시입니다.");
            StartCoroutine(ShowForwardPass());
        }

        public void Initialize(Camera viewCamera, KdyLessonProgressController progress)
        {
            if (_initialized)
            {
                return;
            }
            if (_factorySign == null || _inputSlot == null || _signGrab == null ||
                _signSurface == null || _comparison == null || _networkStages == null ||
                _networkStages.Length != 5 || _hiddenStages == null || _hiddenStages.Length != MaximumLayers ||
                _featureSurfaces == null || _featureSurfaces.Length != MaximumLayers * MaximumMaps ||
                _forwardPulses == null || _forwardPulses.Length != MaximumLayers + 1)
            {
                Debug.LogError("[KdyLearning] Scene 16 needs a grabbable sign, input slot, five stages, three hidden roots, 96 feature surfaces, four pulses and comparison.", this);
                _signGrab?.SetInteractionEnabled(false);
                return;
            }

            for (int stage = 0; stage < _networkStages.Length; stage++)
            {
                if (_networkStages[stage] == null)
                {
                    Debug.LogError("[KdyLearning] Scene 16 has an unassigned network stage.", this);
                    _signGrab.SetInteractionEnabled(false);
                    return;
                }
            }
            for (int index = 0; index < _featureSurfaces.Length; index++)
            {
                if (_featureSurfaces[index] == null)
                {
                    Debug.LogError("[KdyLearning] Scene 16 has an unassigned feature surface.", this);
                    _signGrab.SetInteractionEnabled(false);
                    return;
                }
            }

            _progress = progress;
            _properties = new MaterialPropertyBlock();
            _signHomeParent = _factorySign.parent;
            _signHomePosition = _factorySign.localPosition;
            _signHomeRotation = _factorySign.localRotation;
            _signHomeScale = _factorySign.localScale;
            _signBody = _factorySign.GetComponent<Rigidbody>();
            _validTraining = progress != null && progress.HasTrainingResult && progress.TrainingAttempts > 0 &&
                progress.LastTrainedNetwork.IsComplete && progress.LastImprovement >= progress.TargetImprovement;
            _comparison.Initialize(progress);
            _comparison.Changed += HandleComparisonChanged;
            _signGrab.GrabbedChanged += HandleSignGrabbed;
            _initialized = true;

            Color[] input = KdyFactorySignExampleData.CreateBlurredImage();
            SetTexture(_signSurface, CreateTexture(input, "Kdy16 New Sign Input"));
            PrepareFeatureMaps(input);
            Replay();
        }

        public void Replay()
        {
            if (!_initialized)
            {
                return;
            }
            if (_signGrab.IsGrabbed)
            {
                SetMessage("간판을 먼저 놓은 뒤 다시 시작하세요.",
                    "오른손 Grip을 놓으면 간판을 입력 슬롯에 넣거나 진열대로 돌려놓을 수 있습니다.");
                return;
            }

            StopAllCoroutines();
            _resetting = true;
            _signGrab.SetInteractionEnabled(false);
            _pendingDrop = false;
            _processing = false;
            _completed = false;
            RestoreSign();
            _comparison.ResetResult();
            HidePulses();
            for (int layer = 0; layer < MaximumLayers; layer++)
            {
                SetLayerBrightness(layer, .2f);
            }
            _signGrab.SetInteractionEnabled(_validTraining);
            _resetting = false;

            if (!_validTraining)
            {
                SetMessage("먼저 설계대에서 학습을 마치고 통과 결과를 받아 오세요.",
                    "이 장면은 통과한 설계를 그대로 사용합니다. 유효한 학습 기록이 없어 새 간판 입력을 잠갔습니다. 설계대로 돌아가 학습해 주세요.");
                return;
            }

            KdyNetworkConfiguration configuration = _progress.LastTrainedNetwork;
            SetMessage($"새 간판을 잡아 입력 슬롯에 넣으세요.\n{configuration.LayerCount}층 · 층마다 {configuration.FeatureMaps}장 · 교육용 가상 결과",
                "오른손 Grip으로 간판을 잡고 빛나는 입력 틀에 놓으세요. 학습했던 설계가 새 이미지에서도 어떻게 동작하는지 살펴봅니다.");
        }

        private void PrepareFeatureMaps(Color[] input)
        {
            int layerCount = _validTraining ? _progress.LastTrainedNetwork.LayerCount : 0;
            int mapCount = _validTraining ? _progress.LastTrainedNetwork.FeatureMaps : 0;
            float[][] previous = null;
            for (int layer = 0; layer < MaximumLayers; layer++)
            {
                _hiddenStages[layer]?.SetActive(layer < layerCount);
                float[][] current = layer < layerCount ? new float[mapCount][] : null;
                for (int map = 0; map < MaximumMaps; map++)
                {
                    Renderer surface = _featureSurfaces[layer * MaximumMaps + map];
                    if (surface == null)
                    {
                        continue;
                    }
                    bool visible = layer < layerCount && map < mapCount;
                    surface.gameObject.SetActive(visible);
                    if (!visible)
                    {
                        continue;
                    }

                    current[map] = CreateFeatureResponse(input, previous, layer, map, mapCount);
                    Color[] pixels = new Color[current[map].Length];
                    Color bright = Color.HSVToRGB(.47f + .08f * (map % 4), .48f, 1f);
                    Color dark = new Color(.012f, .024f, .038f);
                    for (int pixel = 0; pixel < pixels.Length; pixel++)
                    {
                        pixels[pixel] = Color.Lerp(dark, bright, current[map][pixel]);
                    }
                    SetTexture(surface, CreateTexture(pixels, $"Kdy16 Example Feature {layer + 1}.{map + 1}"));
                }
                previous = current;
            }
        }

        private float[] CreateFeatureResponse(Color[] input, float[][] previous, int layer, int map, int mapCount)
        {
            // Fixed demonstration filters visualize a possible flow; no learned weights are claimed.
            int width = KdyFactorySignExampleData.Width;
            int height = KdyFactorySignExampleData.Height;
            float[] values = new float[width * height];
            int radius = _progress.LastTrainedNetwork.KernelSize == 9 ? 4 : 1;
            float angle = (map % 8) * Mathf.PI / 4f;
            int offsetX = Mathf.RoundToInt(Mathf.Cos(angle) * radius);
            int offsetY = Mathf.RoundToInt(Mathf.Sin(angle) * radius);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float center = SampleInput(input, x, y, map);
                    float left = SampleInput(input, x - offsetX, y - offsetY, map);
                    float right = SampleInput(input, x + offsetX, y + offsetY, map);
                    float response = map % 4 == 3
                        ? center - .2f
                        : Mathf.Abs(right - left) * 1.7f + center * .15f;
                    if (previous != null)
                    {
                        int first = map % mapCount;
                        int second = (map + layer + 1) % mapCount;
                        float nearby = SampleResponse(previous[first], x - 1, y) +
                            SampleResponse(previous[first], x + 1, y) +
                            SampleResponse(previous[second], x, y - 1) +
                            SampleResponse(previous[second], x, y + 1);
                        response = response * .25f + nearby * .22f - .08f;
                    }
                    values[y * width + x] = _progress.LastTrainedNetwork.UseSigmoid
                        ? 1f / (1f + Mathf.Exp(-5f * (response - .35f)))
                        : Mathf.Clamp01(Mathf.Max(0f, response));
                }
            }
            return values;
        }

        private static float SampleInput(Color[] pixels, int x, int y, int map)
        {
            x = Mathf.Clamp(x, 0, KdyFactorySignExampleData.Width - 1);
            y = Mathf.Clamp(y, 0, KdyFactorySignExampleData.Height - 1);
            Color color = pixels[y * KdyFactorySignExampleData.Width + x];
            switch ((map / 8) % 4)
            {
                case 0: return color.r * .7f + color.g * .2f + color.b * .1f;
                case 1: return color.r * .2f + color.g * .7f + color.b * .1f;
                case 2: return color.r * .1f + color.g * .2f + color.b * .7f;
                default: return color.grayscale;
            }
        }

        private static float SampleResponse(float[] values, int x, int y)
        {
            x = Mathf.Clamp(x, 0, KdyFactorySignExampleData.Width - 1);
            y = Mathf.Clamp(y, 0, KdyFactorySignExampleData.Height - 1);
            return values[y * KdyFactorySignExampleData.Width + x];
        }

        private IEnumerator ShowForwardPass()
        {
            int layerCount = _progress.LastTrainedNetwork.LayerCount;
            float segmentDuration = Mathf.Max(.25f, _durationSeconds / (layerCount + 1));
            for (int segment = 0; segment <= layerCount; segment++)
            {
                Transform from = _networkStages[segment];
                Transform to = _networkStages[segment < layerCount ? segment + 1 : 4];
                Transform pulse = _forwardPulses[segment];
                pulse?.gameObject.SetActive(true);
                float elapsed = 0f;
                while (elapsed < segmentDuration)
                {
                    if (pulse != null)
                    {
                        pulse.position = Vector3.Lerp(from.position, to.position, elapsed / segmentDuration);
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                pulse?.gameObject.SetActive(false);
                if (segment < layerCount)
                {
                    SetLayerBrightness(segment, 1f);
                    SetMessage($"은닉층 {segment + 1} · {_progress.LastTrainedNetwork.FeatureMaps}개 특징 반응을 통과했어요.",
                        "피쳐맵 사이를 걸어 들어가거나 옆에서 겹친 구조를 살펴보세요. 화면은 학습된 실제 값 대신 고정 필터로 만든 교육용 반응 예시입니다.");
                }
            }

            _processing = false;
            _completed = true;
            _comparison.RevealResult();
            SetMessage("새 간판의 처리가 끝났어요. 비교 손잡이를 움직여 결과를 확인하세요.",
                "같은 설계에 새 입력을 넣은 교육용 가상 결과입니다. 비교 손잡이를 좌우 끝까지 움직여 흐린 입력과 복원 결과를 확인하세요.");
        }

        private void SetLayerBrightness(int layer, float brightness)
        {
            for (int map = 0; map < MaximumMaps; map++)
            {
                Renderer surface = _featureSurfaces[layer * MaximumMaps + map];
                if (surface == null)
                {
                    continue;
                }
                surface.GetPropertyBlock(_properties);
                Color tint = new Color(brightness, brightness, brightness, 1f);
                _properties.SetColor("_BaseColor", tint);
                _properties.SetColor("_Color", tint);
                surface.SetPropertyBlock(_properties);
            }
        }

        private Texture2D CreateTexture(Color[] topFirstPixels, string textureName)
        {
            int width = KdyFactorySignExampleData.Width;
            int height = KdyFactorySignExampleData.Height;
            Color[] bottomFirstPixels = new Color[topFirstPixels.Length];
            for (int y = 0; y < height; y++)
            {
                Array.Copy(topFirstPixels, y * width, bottomFirstPixels, (height - y - 1) * width, width);
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = textureName;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(bottomFirstPixels);
            texture.Apply(false, true);
            _textures.Add(texture);
            return texture;
        }

        private void SetTexture(Renderer surface, Texture texture)
        {
            surface.GetPropertyBlock(_properties);
            _properties.SetTexture("_BaseMap", texture);
            _properties.SetTexture("_MainTex", texture);
            surface.SetPropertyBlock(_properties);
        }

        private void RestoreSign()
        {
            _factorySign.SetParent(_signHomeParent, false);
            _factorySign.localScale = _signHomeScale;
            _factorySign.SetLocalPositionAndRotation(_signHomePosition, _signHomeRotation);
            SynchronizeSignBody();
        }

        private void SetSignPose(Vector3 position, Quaternion rotation)
        {
            _factorySign.SetParent(_signHomeParent, false);
            _factorySign.localScale = _signHomeScale;
            _factorySign.SetPositionAndRotation(position, rotation);
            SynchronizeSignBody();
        }

        private void SynchronizeSignBody()
        {
            if (_signBody != null)
            {
                _signBody.position = _factorySign.position;
                _signBody.rotation = _factorySign.rotation;
            }
        }

        private void HidePulses()
        {
            foreach (Transform pulse in _forwardPulses)
            {
                pulse?.gameObject.SetActive(false);
            }
        }

        private void SetMessage(string status, string hint)
        {
            _hint = hint;
            if (_status != null)
            {
                _status.text = status;
            }
            Changed?.Invoke();
        }

        private void HandleSignGrabbed(bool grabbed)
        {
            if (!_initialized || !_validTraining || _resetting || _processing || _completed)
            {
                return;
            }
            if (grabbed)
            {
                _pendingDrop = false;
                SetMessage("좋아요. 빛나는 입력 슬롯에 간판을 가져간 뒤 놓으세요.",
                    "Grip을 누른 채 오른손을 입력 틀 쪽으로 움직이세요. 노트북에서는 G를 유지하고 I·K로 손 거리를 조절할 수 있습니다.");
                return;
            }
            _releasedPosition = _factorySign.position;
            _pendingDrop = true;
        }

        private void HandleComparisonChanged()
        {
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            if (_signGrab != null)
            {
                _signGrab.GrabbedChanged -= HandleSignGrabbed;
            }
            if (_comparison != null)
            {
                _comparison.Changed -= HandleComparisonChanged;
            }
            foreach (Texture2D texture in _textures)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }
    }
}
