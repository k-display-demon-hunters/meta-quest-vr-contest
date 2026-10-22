using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyFeatureMapGalleryController : MonoBehaviour
    {
        [SerializeField] private Renderer _inputSurface;
        [SerializeField] private Renderer _outputSurface;
        [SerializeField] private Renderer[] _mapSurfaces = new Renderer[8];
        [SerializeField] private Transform[] _mapRoots = new Transform[8];
        [SerializeField] private Renderer[] _switchSurfaces = new Renderer[3];
        [SerializeField] private Text _status;
        [SerializeField] private Text[] _mapLabels = new Text[8];

        private const int MapCount = 8;
        private const int FeatureCount = 3;
        private const int RequiredMapCount = 3;
        private static readonly string[] MapNames =
        {
            "세로 경계", "가로 경계", "윤곽",
            "밝아지는 세로 경계", "어두워지는 세로 경계",
            "밝아지는 가로 경계", "어두워지는 가로 경계", "전체 경계 세기"
        };
        private static readonly string[] MapExplanations =
        {
            "좌우로 밝기가 바뀌는 곳이 밝아요. 글자의 세로획 양옆을 찾아보세요.",
            "위아래로 밝기가 바뀌는 곳이 밝아요. 글자의 가로획 위아래를 찾아보세요.",
            "주변과 밝기가 다른 부분을 모아 글자의 테두리를 보여 줍니다.",
            "좌우 변화 중 어두운 쪽에서 밝은 쪽으로 바뀌는 경계만 보여 줍니다.",
            "좌우 변화 중 밝은 쪽에서 어두운 쪽으로 바뀌는 경계만 보여 줍니다.",
            "위아래 변화 중 어두운 쪽에서 밝은 쪽으로 바뀌는 경계만 보여 줍니다.",
            "위아래 변화 중 밝은 쪽에서 어두운 쪽으로 바뀌는 경계만 보여 줍니다.",
            "가로와 세로의 변화 크기를 합쳐 방향에 관계없이 경계의 세기를 보여 줍니다."
        };
        private static readonly string[] FeatureNames = { "세로", "가로", "윤곽" };
        private static readonly Color[] MapColors =
        {
            new Color(.20f, .85f, 1.0f), new Color(.30f, 1.0f, .68f), new Color(1.0f, .73f, .22f),
            new Color(.42f, .73f, 1.0f), new Color(.70f, .46f, 1.0f),
            new Color(.35f, 1.0f, .88f), new Color(1.0f, .45f, .67f), new Color(1.0f, .93f, .64f)
        };

        private readonly HashSet<int> _inspectedMaps = new HashSet<int>();
        private readonly bool[] _featuresEnabled = { true, true, true };
        private readonly bool[] _featuresTurnedOff = new bool[FeatureCount];
        private readonly Texture2D[] _textures = new Texture2D[MapCount + 2];
        private readonly Material[] _materials = new Material[MapCount + 2];
        private readonly KdyLessonGrabController[] _mapGrabs = new KdyLessonGrabController[MapCount];
        private readonly float[][] _mapValues = new float[MapCount][];
        private readonly float[] _mapMaximums = new float[MapCount];
        private MaterialPropertyBlock _switchProperties;
        private Color[] _clearImage;
        private Color[] _blurredImage;
        private Color[] _outputPixels;
        private string _explanation;
        private int _selectedMap = -1;
        private bool _completedSwitchCycle;
        private bool _initialized;

        public event Action Changed;

        public bool CanToggleFeatures => _initialized && _inspectedMaps.Count >= RequiredMapCount;
        public bool ReadyToCollect => CanToggleFeatures && _completedSwitchCycle;

        public string Hint
        {
            get
            {
                if (_inspectedMaps.Count < RequiredMapCount)
                {
                    return $"{_explanation}\nGrip으로 가져와 살펴보고 서로 다른 판을 Trigger로 선택하세요. {_inspectedMaps.Count}/3";
                }

                if (!_completedSwitchCycle)
                {
                    return $"{_explanation}\n스위치 하나를 OFF로 바꿔 출력을 비교한 뒤 같은 스위치를 ON으로 되돌리세요.";
                }

                return $"{_explanation}\n비교 완료! 양피지 ④를 수집하고 활성화 함수 공정으로 이동하세요.";
            }
        }

        public void Initialize(Camera camera)
        {
            if (_initialized)
            {
                return;
            }

            if (!HasRequiredReferences())
            {
                Debug.LogError("[Kdy Feature Maps] Input, output, eight map panels and three switch indicators must be assigned.", this);
                return;
            }

            _switchProperties = new MaterialPropertyBlock();
            _clearImage = KdySignExampleData.CreateImage();
            _blurredImage = KdyTrainingExampleData.CreateBlurredImage();
            _outputPixels = new Color[_clearImage.Length];
            BuildFeatureValues();
            _textures[0] = CreateTexture("Kdy blurred input", _blurredImage);
            _textures[1] = CreateTexture("Kdy feature reconstruction", _clearImage);
            ApplyMaterial(_inputSurface, 0);
            ApplyMaterial(_outputSurface, 1);

            for (int i = 0; i < MapCount; i++)
            {
                _textures[i + 2] = CreateTexture($"Kdy feature map {i + 1}", CreateMapColors(i));
                ApplyMaterial(_mapSurfaces[i], i + 2);
                _mapGrabs[i] = _mapRoots[i].GetComponent<KdyLessonGrabController>();
            }

            _initialized = true;
            Replay();
        }

        public void SelectMap(RaycastHit hit)
        {
            if (!_initialized)
            {
                return;
            }

            for (int i = 0; i < MapCount; i++)
            {
                if (hit.transform != _mapRoots[i] && !hit.transform.IsChildOf(_mapRoots[i]))
                {
                    continue;
                }

                _selectedMap = i;
                _inspectedMaps.Add(i);
                _explanation = $"특징판 {i + 1:00} · {MapNames[i]}\n{MapExplanations[i]}";
                RefreshAppearance();
                RefreshStatus();
                Changed?.Invoke();
                return;
            }
        }

        public void ToggleFeature(int index)
        {
            if (!_initialized || index < 0 || index >= FeatureCount)
            {
                return;
            }

            if (!CanToggleFeatures)
            {
                _explanation = "먼저 서로 다른 특징판 3개를 선택해 보세요. 필터마다 찾는 특징이 달라요.";
                RefreshStatus();
                Changed?.Invoke();
                return;
            }

            _featuresEnabled[index] = !_featuresEnabled[index];
            if (!_featuresEnabled[index])
            {
                _featuresTurnedOff[index] = true;
                _explanation = $"{FeatureNames[index]} 특징 OFF · 해당 경계 주변이 흐려진 출력을 살펴보세요.";
            }
            else
            {
                if (_featuresTurnedOff[index])
                {
                    _completedSwitchCycle = true;
                }

                _explanation = $"{FeatureNames[index]} 특징 ON · 같은 특징 정보를 다시 사용하는 출력을 비교하세요.";
            }

            UpdateOutput();
            RefreshAppearance();
            RefreshStatus();
            Changed?.Invoke();
        }

        public void Replay()
        {
            if (!_initialized)
            {
                return;
            }

            _inspectedMaps.Clear();
            _selectedMap = -1;
            _completedSwitchCycle = false;
            for (int i = 0; i < FeatureCount; i++)
            {
                _featuresEnabled[i] = true;
                _featuresTurnedOff[i] = false;
            }

            foreach (KdyLessonGrabController grab in _mapGrabs)
            {
                if (grab != null)
                {
                    grab.ResetPose();
                }
            }

            _explanation = "같은 간판을 여덟 가지 방식으로 살펴본 특징판입니다. 밝은 부분이 각 필터가 찾아낸 변화예요.";
            UpdateOutput();
            RefreshAppearance();
            RefreshStatus();
            Changed?.Invoke();
        }

        private void BuildFeatureValues()
        {
            int count = KdySignExampleData.Width * KdySignExampleData.Height;
            for (int i = 0; i < MapCount; i++)
            {
                _mapValues[i] = new float[count];
            }

            for (int y = 0; y < KdySignExampleData.Height; y++)
            {
                for (int x = 0; x < KdySignExampleData.Width; x++)
                {
                    int index = y * KdySignExampleData.Width + x;
                    float vertical = FilterInput(0, x, y);
                    float horizontal = FilterInput(1, x, y);
                    float outline = FilterInput(2, x, y);
                    _mapValues[0][index] = Mathf.Abs(vertical);
                    _mapValues[1][index] = Mathf.Abs(horizontal);
                    _mapValues[2][index] = Mathf.Abs(outline);
                    _mapValues[3][index] = Mathf.Max(0.0f, vertical);
                    _mapValues[4][index] = Mathf.Max(0.0f, -vertical);
                    _mapValues[5][index] = Mathf.Max(0.0f, horizontal);
                    _mapValues[6][index] = Mathf.Max(0.0f, -horizontal);
                    _mapValues[7][index] = Mathf.Sqrt(vertical * vertical + horizontal * horizontal);
                    for (int map = 0; map < MapCount; map++)
                    {
                        _mapMaximums[map] = Mathf.Max(_mapMaximums[map], _mapValues[map][index]);
                    }
                }
            }
        }

        private float FilterInput(int kind, int x, int y)
        {
            float response = 0.0f;
            for (int ky = 0; ky < 3; ky++)
            {
                for (int kx = 0; kx < 3; kx++)
                {
                    response += KdySignExampleData.Weight(kind, kx, ky) * SampleInput(x + kx - 1, y + ky - 1);
                }
            }

            return response;
        }

        private float SampleInput(int x, int y)
        {
            if (x < 0 || x >= KdySignExampleData.Width || y < 0 || y >= KdySignExampleData.Height)
            {
                return 0.0f;
            }

            Color pixel = _blurredImage[y * KdySignExampleData.Width + x];
            return .2126f * pixel.r + .7152f * pixel.g + .0722f * pixel.b;
        }

        private Color[] CreateMapColors(int map)
        {
            Color[] colors = new Color[_clearImage.Length];
            Color background = new Color(.012f, .025f, .045f, 1.0f);
            float maximum = Mathf.Max(.001f, _mapMaximums[map]);
            for (int i = 0; i < colors.Length; i++)
            {
                float amount = Mathf.Clamp01(_mapValues[map][i] / maximum);
                colors[i] = Color.Lerp(background, MapColors[map], amount);
            }

            return colors;
        }

        private void UpdateOutput()
        {
            for (int y = 0; y < KdySignExampleData.Height; y++)
            {
                for (int x = 0; x < KdySignExampleData.Width; x++)
                {
                    int sourceIndex = y * KdySignExampleData.Width + x;
                    float missingFeature = 0.0f;
                    for (int feature = 0; feature < FeatureCount; feature++)
                    {
                        if (!_featuresEnabled[feature])
                        {
                            float maximum = Mathf.Max(.001f, _mapMaximums[feature]);
                            missingFeature = Mathf.Max(missingFeature, _mapValues[feature][sourceIndex] / maximum);
                        }
                    }

                    // This mask illustrates feature removal; it is not a learned image reconstruction.
                    Color color = Color.Lerp(_clearImage[sourceIndex], _blurredImage[sourceIndex] * .55f,
                        Mathf.Clamp01(missingFeature) * .95f);
                    color.a = 1.0f;
                    int destination = (KdySignExampleData.Height - 1 - y) * KdySignExampleData.Width + x;
                    _outputPixels[destination] = color;
                }
            }

            _textures[1].SetPixels(_outputPixels);
            _textures[1].Apply(false, false);
        }

        private void RefreshAppearance()
        {
            for (int i = 0; i < FeatureCount; i++)
            {
                Renderer indicator = _switchSurfaces[i];
                Color color = !CanToggleFeatures ? new Color(.10f, .16f, .20f) :
                    _featuresEnabled[i] ? new Color(.25f, 1.0f, .72f) : new Color(1.0f, .40f, .16f);
                indicator.GetPropertyBlock(_switchProperties);
                _switchProperties.SetColor("_BaseColor", color);
                _switchProperties.SetColor("_Color", color);
                indicator.SetPropertyBlock(_switchProperties);
            }

            for (int i = 0; i < MapCount; i++)
            {
                bool active = IsMapFeatureEnabled(i);
                Color tint = active ? Color.white : new Color(.24f, .24f, .24f);
                _materials[i + 2].SetColor("_BaseColor", tint);
                if (i < _mapLabels.Length && _mapLabels[i] != null)
                {
                    _mapLabels[i].color = i == _selectedMap ? new Color(1.0f, .85f, .35f) : Color.white;
                    string state = !active ? "OFF" : _inspectedMaps.Contains(i) ? "관찰 완료" : "Trigger로 선택";
                    _mapLabels[i].text = $"{i + 1:00} · {MapNames[i]}\n{state}";
                }
            }
        }

        private bool IsMapFeatureEnabled(int map)
        {
            if (map == 0 || map == 3 || map == 4)
            {
                return _featuresEnabled[0];
            }

            if (map == 1 || map == 5 || map == 6)
            {
                return _featuresEnabled[1];
            }

            if (map == 2)
            {
                return _featuresEnabled[2];
            }

            return _featuresEnabled[0] || _featuresEnabled[1];
        }

        private void RefreshStatus()
        {
            if (_status == null)
            {
                return;
            }

            string vertical = _featuresEnabled[0] ? "ON" : "OFF";
            string horizontal = _featuresEnabled[1] ? "ON" : "OFF";
            string outline = _featuresEnabled[2] ? "ON" : "OFF";
            _status.text = $"{_explanation}\n관찰 {_inspectedMaps.Count}/8 · 세로 {vertical} / 가로 {horizontal} / 윤곽 {outline}\n"
                + "고정 필터·복원 효과를 사용한 교육용 시각화입니다.";
        }

        private Texture2D CreateTexture(string name, Color[] topRowFirstColors)
        {
            Texture2D texture = new Texture2D(KdySignExampleData.Width, KdySignExampleData.Height,
                TextureFormat.RGBA32, false);
            texture.name = name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[topRowFirstColors.Length];
            for (int y = 0; y < KdySignExampleData.Height; y++)
            {
                int source = y * KdySignExampleData.Width;
                int destination = (KdySignExampleData.Height - 1 - y) * KdySignExampleData.Width;
                Array.Copy(topRowFirstColors, source, pixels, destination, KdySignExampleData.Width);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private void ApplyMaterial(Renderer surface, int textureIndex)
        {
            Material material = new Material(surface.sharedMaterial);
            material.name = $"Kdy gallery surface {textureIndex}";
            material.mainTexture = _textures[textureIndex];
            material.SetColor("_BaseColor", Color.white);
            _materials[textureIndex] = material;
            surface.sharedMaterial = material;
        }

        private bool HasRequiredReferences()
        {
            if (_inputSurface == null || _outputSurface == null || _mapSurfaces.Length != MapCount ||
                _mapRoots.Length != MapCount || _switchSurfaces.Length != FeatureCount)
            {
                return false;
            }

            for (int i = 0; i < MapCount; i++)
            {
                if (_mapSurfaces[i] == null || _mapRoots[i] == null)
                {
                    return false;
                }
            }

            foreach (Renderer indicator in _switchSurfaces)
            {
                if (indicator == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                if (_textures[i] != null)
                {
                    Destroy(_textures[i]);
                }

                if (_materials[i] != null)
                {
                    Destroy(_materials[i]);
                }
            }
        }
    }
}
