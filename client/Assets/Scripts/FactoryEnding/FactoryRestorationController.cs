using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryEnding
{
    public sealed class FactoryRestorationController : MonoBehaviour
    {
        private const int LandscapeWidth = 32;
        private const int LandscapeHeight = 24;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        [SerializeField] private Transform _factoryRoot;
        [SerializeField] private Transform _spiritRoot;
        [SerializeField] private Renderer[] _restoredLights;
        [SerializeField] private Renderer[] _windowSurfaces;
        [SerializeField, Min(0.5f)] private float _duration = 5f;
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

        public IEnumerator RestoreFactory()
        {
            if (_factoryRoot == null || _spiritRoot == null || _restoredLights == null || _windowSurfaces == null)
            {
                Debug.LogError("[FactoryEnding] 복구 연출의 공장·악령·조명·창문 참조를 확인해 주세요.", this);
                yield break;
            }
            CacheRendererStates();
            CreateWindowLandscapes();
            _originalSpiritScale = _spiritRoot.localScale;
            _originalSpiritActive = _spiritRoot.gameObject.activeSelf;
            _hasSpiritSnapshot = true;
            ApplyRestoredLights(0);
            int previousStep = -1;
            for (float elapsed = 0f; elapsed < _duration; elapsed += Time.deltaTime)
            {
                float fraction = Mathf.Clamp01(elapsed / _duration);
                int step = Mathf.FloorToInt(fraction * 20f);
                if (step != previousStep)
                {
                    ApplyFactoryBrightness(Mathf.SmoothStep(0f, 1f, step / 20f));
                    ApplyRestoredLights(Mathf.FloorToInt(fraction * _lightStates.Length));
                    ApplyWindowClarity(fraction);
                    previousStep = step;
                }
                float disappearance = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.85f, fraction));
                _spiritRoot.localScale = _originalSpiritScale * (1f - disappearance);
                yield return null;
            }
            ApplyFactoryBrightness(1f);
            ApplyRestoredLights(_lightStates.Length);
            ApplyWindowClarity(1f);
            _spiritRoot.gameObject.SetActive(false);
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
                    name = "Restored Window " + (i + 1),
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                _originalWindowMaterials[i] = _windowSurfaces[i].sharedMaterial;
                Material material = new Material(_originalWindowMaterials[i])
                {
                    name = "Ending Window Material " + (i + 1),
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
