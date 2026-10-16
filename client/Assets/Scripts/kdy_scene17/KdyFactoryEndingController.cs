using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyFactoryEndingController : MonoBehaviour
    {
        private const int LandscapeWidth = 32;
        private const int LandscapeHeight = 24;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform _factoryRoot;
        [SerializeField] private Transform _spiritRoot;
        [SerializeField] private Renderer[] _restoredLights = new Renderer[8];
        [SerializeField] private Renderer[] _windowSurfaces = new Renderer[3];
        [SerializeField] private Text _status;
        [SerializeField] private Text _summary;
        [SerializeField, Min(.5f)] private float _durationSeconds = 5f;

        private sealed class RendererState
        {
            public Renderer Target;
            public MaterialPropertyBlock Original;
            public MaterialPropertyBlock Working;
            public Color OriginalColor;
            public bool HasBaseColor;
            public bool HasColor;
            public bool HasEmission;
        }

        private KdyLessonProgressController _progress;
        private RendererState[] _factoryStates;
        private RendererState[] _lightStates;
        private Material[] _originalWindowMaterials;
        private Material[] _windowMaterials;
        private Texture2D[] _windowTextures;
        private Color[][] _sharpLandscapes;
        private Color[][] _blurredLandscapes;
        private Color[] _landscapePixels;
        private Vector3 _originalSpiritScale;
        private bool _originalSpiritActive;
        private bool _hasSpiritSnapshot;
        private bool _initialized;
        private bool _passed;
        private bool _restoring;
        private bool _restored;
        private bool _finished;

        public event Action Changed;
        public bool ReadyToFinish => _initialized && _passed && _restored && !_restoring && !_finished;
        public bool IsFinished => _finished;

        public string Hint
        {
            get
            {
                if (!_initialized || !_passed)
                {
                    return "16번에서 학습한 신경망으로 새 간판을 처리하고 입력·결과를 먼저 비교해 보세요.";
                }
                if (_restoring)
                {
                    return "공장 조명이 돌아오고 창밖 풍경이 선명해지고 있어요. 주변을 둘러보세요.";
                }
                if (_finished)
                {
                    return "체험 완료! 태블릿에서 모은 양피지와 학습 기록을 다시 볼 수 있어요.";
                }
                return "복구된 공장을 둘러보고 학습 요약을 확인한 뒤 [게임 완료]를 선택하세요.";
            }
        }

        public void Initialize(Camera viewCamera, KdyLessonProgressController progress)
        {
            if (_initialized)
            {
                return;
            }
            _progress = progress;
            _initialized = true;
            UpdateSummary();
            if (!HasRequiredReferences())
            {
                if (_status != null)
                {
                    _status.text = "엔딩 장치 연결을 확인해 주세요.";
                }
                Debug.LogError("[KdyLearning] Scene 17 needs the factory, spirit, eight lamps, three window surfaces and summary labels.", this);
                Changed?.Invoke();
                return;
            }
            _passed = progress != null && progress.HasInspectedFactorySign && progress.HasTrainingResult &&
                progress.LastTrainedNetwork.IsComplete && progress.LastImprovement >= progress.TargetImprovement;
            if (!_passed)
            {
                _status.text = "복구 확인이 아직 끝나지 않았어요.\n16번에서 새 간판 처리와 비교를 먼저 완료해 주세요.";
                Changed?.Invoke();
                return;
            }

            CacheRendererStates();
            CreateWindowLandscapes();
            _originalSpiritScale = _spiritRoot.localScale;
            _originalSpiritActive = _spiritRoot.gameObject.activeSelf;
            _hasSpiritSnapshot = true;
            _restoring = true;
            _status.text = "공장 전체 복구 중\n조명과 창밖 풍경을 둘러보세요.";
            Changed?.Invoke();
            StartCoroutine(RestoreFactory());
        }

        public void CompleteExperience()
        {
            if (!ReadyToFinish)
            {
                return;
            }
            _progress.CompleteExperience();
            _finished = _progress.IsGameComplete;
            if (!_finished)
            {
                return;
            }
            _status.text = "체험 완료!\n태블릿에서 양피지와 학습 기록을 복습할 수 있어요.";
            UpdateSummary();
            Changed?.Invoke();
        }

        private IEnumerator RestoreFactory()
        {
            float elapsed = 0f;
            int brightnessStep = -1;
            int landscapeStep = -1;
            int previousLampCount = -1;
            float duration = Mathf.Max(.5f, _durationSeconds);
            while (elapsed < duration)
            {
                float fraction = Mathf.Clamp01(elapsed / duration);
                int nextBrightnessStep = Mathf.FloorToInt(fraction * 20f);
                if (nextBrightnessStep != brightnessStep)
                {
                    ApplyFactoryBrightness(Mathf.SmoothStep(0f, 1f, nextBrightnessStep / 20f));
                    brightnessStep = nextBrightnessStep;
                }
                int litCount = Mathf.Clamp(Mathf.FloorToInt(fraction * _lightStates.Length), 0, _lightStates.Length);
                if (litCount != previousLampCount)
                {
                    ApplyRestoredLights(litCount);
                    previousLampCount = litCount;
                }
                int nextLandscapeStep = Mathf.FloorToInt(fraction * 10f);
                if (nextLandscapeStep != landscapeStep)
                {
                    ApplyWindowClarity(nextLandscapeStep / 10f);
                    landscapeStep = nextLandscapeStep;
                }
                float spiritFraction = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.15f, .85f, fraction));
                _spiritRoot.localScale = _originalSpiritScale * (1f - spiritFraction);
                if (spiritFraction >= 1f)
                {
                    _spiritRoot.gameObject.SetActive(false);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            ApplyFactoryBrightness(1f);
            ApplyRestoredLights(_lightStates.Length);
            ApplyWindowClarity(1f);
            _spiritRoot.localScale = Vector3.zero;
            _spiritRoot.gameObject.SetActive(false);
            _restoring = false;
            _restored = true;
            _status.text = "공장 복구 완료 · 픽셀 악령이 사라졌어요.\n학습 요약과 수집 기록을 확인하고 [게임 완료]를 누르세요.\n교육용 가상 학습 결과와 복구 연출";
            UpdateSummary();
            Changed?.Invoke();
        }

        private void CacheRendererStates()
        {
            HashSet<Renderer> specialRenderers = new HashSet<Renderer>(_restoredLights);
            specialRenderers.UnionWith(_windowSurfaces);
            Renderer[] renderers = _factoryRoot.GetComponentsInChildren<Renderer>(true);
            List<RendererState> factoryStates = new List<RendererState>(renderers.Length);
            foreach (Renderer renderer in renderers)
            {
                if (!specialRenderers.Contains(renderer) && renderer.sharedMaterial != null)
                {
                    factoryStates.Add(CaptureRenderer(renderer));
                }
            }
            _factoryStates = factoryStates.ToArray();
            _lightStates = new RendererState[_restoredLights.Length];
            for (int i = 0; i < _restoredLights.Length; i++)
            {
                _lightStates[i] = CaptureRenderer(_restoredLights[i]);
            }
        }

        private RendererState CaptureRenderer(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            RendererState state = new RendererState
            {
                Target = renderer,
                Original = new MaterialPropertyBlock(),
                Working = new MaterialPropertyBlock(),
                HasBaseColor = material.HasProperty(BaseColorId),
                HasColor = material.HasProperty(ColorId),
                HasEmission = material.HasProperty(EmissionColorId)
            };
            renderer.GetPropertyBlock(state.Original);
            renderer.GetPropertyBlock(state.Working);
            state.OriginalColor = state.HasBaseColor ? material.GetColor(BaseColorId) :
                state.HasColor ? material.GetColor(ColorId) : Color.white;
            return state;
        }

        private void ApplyFactoryBrightness(float fraction)
        {
            foreach (RendererState state in _factoryStates)
            {
                Color original = state.OriginalColor;
                Color restored = new Color(
                    Mathf.Clamp01(original.r * 1.3f + .025f),
                    Mathf.Clamp01(original.g * 1.3f + .03f),
                    Mathf.Clamp01(original.b * 1.3f + .035f), original.a);
                ApplyColor(state, Color.Lerp(original, restored, fraction));
            }
        }

        private void ApplyRestoredLights(int count)
        {
            for (int i = 0; i < _lightStates.Length; i++)
            {
                bool restored = i < count;
                Color color = restored
                    ? (i % 2 == 0 ? new Color(.16f, .95f, .74f) : new Color(1f, .72f, .22f))
                    : new Color(.035f, .065f, .08f);
                RendererState state = _lightStates[i];
                ApplyColor(state, color);
                if (state.HasEmission)
                {
                    state.Working.SetColor(EmissionColorId, restored ? color * 2f : Color.black);
                    state.Target.SetPropertyBlock(state.Working);
                }
            }
        }

        private void ApplyColor(RendererState state, Color color)
        {
            if (state.Target == null)
            {
                return;
            }
            if (state.HasBaseColor)
            {
                state.Working.SetColor(BaseColorId, color);
            }
            if (state.HasColor)
            {
                state.Working.SetColor(ColorId, color);
            }
            state.Target.SetPropertyBlock(state.Working);
        }

        private void CreateWindowLandscapes()
        {
            int count = _windowSurfaces.Length;
            _originalWindowMaterials = new Material[count];
            _windowMaterials = new Material[count];
            _windowTextures = new Texture2D[count];
            _sharpLandscapes = new Color[count][];
            _blurredLandscapes = new Color[count][];
            _landscapePixels = new Color[LandscapeWidth * LandscapeHeight];
            for (int i = 0; i < count; i++)
            {
                _sharpLandscapes[i] = CreateLandscape(i);
                _blurredLandscapes[i] = BlurLandscape(_sharpLandscapes[i]);
                _windowTextures[i] = new Texture2D(LandscapeWidth, LandscapeHeight, TextureFormat.RGBA32, false)
                {
                    name = "Kdy Restored Window " + (i + 1),
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                _originalWindowMaterials[i] = _windowSurfaces[i].sharedMaterial;
                Material material = new Material(_originalWindowMaterials[i])
                {
                    name = "Kdy Ending Window Material " + (i + 1),
                    mainTexture = _windowTextures[i]
                };
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", _windowTextures[i]);
                }
                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", _windowTextures[i]);
                }
                if (material.HasProperty(BaseColorId))
                {
                    material.SetColor(BaseColorId, Color.white);
                }
                if (material.HasProperty(ColorId))
                {
                    material.SetColor(ColorId, Color.white);
                }
                _windowMaterials[i] = material;
                _windowSurfaces[i].sharedMaterial = material;
            }
            ApplyWindowClarity(0f);
        }

        private Color[] CreateLandscape(int windowIndex)
        {
            Color[] pixels = new Color[LandscapeWidth * LandscapeHeight];
            for (int y = 0; y < LandscapeHeight; y++)
            {
                for (int x = 0; x < LandscapeWidth; x++)
                {
                    int offsetX = x + windowIndex * 9;
                    int distantRidge = 11 + Mathf.RoundToInt(4f * Mathf.Abs(Mathf.Sin(offsetX * .14f)));
                    int nearRidge = 7 + Mathf.RoundToInt(3f * Mathf.Abs(Mathf.Sin(offsetX * .18f + 1f)));
                    Color color = Color.Lerp(new Color(.68f, .86f, .93f), new Color(.16f, .46f, .76f), y / 23f);
                    if ((x - 24) * (x - 24) + (y - 19) * (y - 19) <= 6)
                    {
                        color = new Color(1f, .9f, .58f);
                    }
                    if (y < distantRidge)
                    {
                        color = new Color(.24f, .46f, .5f);
                    }
                    if (y < nearRidge)
                    {
                        color = new Color(.12f, .34f, .31f);
                    }
                    if (y < 5)
                    {
                        color = (x + y + windowIndex) % 5 == 0
                            ? new Color(.52f, .72f, .3f) : new Color(.29f, .55f, .25f);
                    }
                    pixels[y * LandscapeWidth + x] = color;
                }
            }
            return pixels;
        }

        private Color[] BlurLandscape(Color[] sharp)
        {
            Color[] blurred = new Color[sharp.Length];
            for (int y = 0; y < LandscapeHeight; y++)
            {
                for (int x = 0; x < LandscapeWidth; x++)
                {
                    Color sum = Color.clear;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int sampleX = Mathf.Clamp(x + dx, 0, LandscapeWidth - 1);
                            int sampleY = Mathf.Clamp(y + dy, 0, LandscapeHeight - 1);
                            sum += sharp[sampleY * LandscapeWidth + sampleX];
                        }
                    }
                    blurred[y * LandscapeWidth + x] = sum / 9f;
                }
            }
            return blurred;
        }

        private void ApplyWindowClarity(float fraction)
        {
            for (int window = 0; window < _windowTextures.Length; window++)
            {
                for (int pixel = 0; pixel < _landscapePixels.Length; pixel++)
                {
                    _landscapePixels[pixel] = Color.Lerp(
                        _blurredLandscapes[window][pixel], _sharpLandscapes[window][pixel], fraction);
                }
                _windowTextures[window].SetPixels(_landscapePixels);
                _windowTextures[window].Apply(false, false);
            }
        }

        private void UpdateSummary()
        {
            if (_summary == null)
            {
                return;
            }
            int scrolls = _progress != null ? _progress.ScrollCount : 0;
            int trainingNote = _progress != null && _progress.HasTrainingScroll ? 1 : 0;
            _summary.text =
                "픽셀·RGB → 필터·합성곱\n" +
                "피쳐맵·활성화 → 손실·필터 수정\n" +
                $"양피지 {scrolls} / 5 · 학습 기록 {trainingNote} / 1\n" +
                $"수집 {scrolls + trainingNote} / 6 · 태블릿에서 복습\n" +
                "이 결과는 교육용 가상 학습 시뮬레이션입니다.";
        }

        private bool HasRequiredReferences()
        {
            if (_factoryRoot == null || _spiritRoot == null || _status == null || _summary == null ||
                _restoredLights == null || _restoredLights.Length != 8 ||
                _windowSurfaces == null || _windowSurfaces.Length != 3)
            {
                return false;
            }
            foreach (Renderer lamp in _restoredLights)
            {
                if (lamp == null || lamp.sharedMaterial == null)
                {
                    return false;
                }
            }
            foreach (Renderer surface in _windowSurfaces)
            {
                if (surface == null || surface.sharedMaterial == null)
                {
                    return false;
                }
            }
            return true;
        }

        private void RestoreRendererStates(RendererState[] states)
        {
            if (states == null)
            {
                return;
            }
            foreach (RendererState state in states)
            {
                if (state.Target != null)
                {
                    state.Target.SetPropertyBlock(state.Original);
                }
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            RestoreRendererStates(_factoryStates);
            RestoreRendererStates(_lightStates);
            if (_hasSpiritSnapshot && _spiritRoot != null)
            {
                _spiritRoot.localScale = _originalSpiritScale;
                _spiritRoot.gameObject.SetActive(_originalSpiritActive);
            }
            if (_windowMaterials == null)
            {
                return;
            }
            for (int i = 0; i < _windowMaterials.Length; i++)
            {
                if (_windowSurfaces[i] != null && _windowSurfaces[i].sharedMaterial == _windowMaterials[i])
                {
                    _windowSurfaces[i].sharedMaterial = _originalWindowMaterials[i];
                }
                if (_windowMaterials[i] != null)
                {
                    Destroy(_windowMaterials[i]);
                }
                if (_windowTextures[i] != null)
                {
                    Destroy(_windowTextures[i]);
                }
            }
        }
    }
}
