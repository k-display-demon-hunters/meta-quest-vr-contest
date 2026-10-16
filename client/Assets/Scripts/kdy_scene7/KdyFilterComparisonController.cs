using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyFilterComparisonController : MonoBehaviour
    {
        [SerializeField] private KdyFilterCardController[] _cards;
        [SerializeField] private Renderer _inputSurface;
        [SerializeField] private Renderer _outputSurface;
        [SerializeField] private Transform _scanFrame;
        [SerializeField] private Text _status;
        [SerializeField] private float _scanStepSeconds = .025f;

        private readonly HashSet<int> _completedFilters = new HashSet<int>();
        private Material _inputOriginalMaterial;
        private Material _outputOriginalMaterial;
        private Material _inputMaterial;
        private Material _outputMaterial;
        private Texture2D _inputTexture;
        private Texture2D _outputTexture;
        private Color[] _emptyOutput;
        private float[][] _responses;
        private float[] _responseMaximums;
        private KdyFilterCardController _activeCard;
        private WaitForSeconds _scanDelay;
        private bool _initialized;
        private bool _isScanning;
        private string _hint;

        public bool ReadyToCollect => _initialized && !_isScanning && _completedFilters.Count >= 2;
        public string Hint => _hint;
        public event Action Changed;

        public void Initialize(Camera viewCamera)
        {
            if (_initialized)
            {
                return;
            }
            if (_cards == null || _cards.Length != 3 || _inputSurface == null ||
                _outputSurface == null || _scanFrame == null || _status == null ||
                _inputSurface.sharedMaterial == null || _outputSurface.sharedMaterial == null)
            {
                Debug.LogError("[KdyLearning] Filter comparison needs three cards, two textured surfaces, a scan frame and status text.", this);
                return;
            }
            foreach (KdyFilterCardController card in _cards)
            {
                if (card == null)
                {
                    Debug.LogError("[KdyLearning] A scene 7 filter card reference is missing.", this);
                    return;
                }
                card.Initialize();
            }
            BuildTeachingTextures();
            CacheFilterResponses();
            _scanDelay = new WaitForSeconds(Mathf.Max(.01f, _scanStepSeconds));
            foreach (KdyFilterCardController card in _cards)
            {
                card.Docked += HandleCardDocked;
            }
            _initialized = true;
            Replay();
        }

        public void Replay()
        {
            if (!_initialized)
            {
                return;
            }
            StopAllCoroutines();
            _isScanning = false;
            _completedFilters.Clear();
            _activeCard = null;
            foreach (KdyFilterCardController card in _cards)
            {
                card.ResetPose();
            }
            ClearOutput();
            _scanFrame.gameObject.SetActive(false);
            SetMessage("서로 다른 필터 두 개를 슬롯에 끼워 결과를 비교하세요. 0 / 2",
                "오른손 Grip으로 카드를 잡아 슬롯 가까이 놓으세요. 이 교재용 필터는 RGB의 명도 조합에서 경계를 찾습니다.");
        }

        private void HandleCardDocked(int filterIndex)
        {
            if (!_initialized || !isActiveAndEnabled || _isScanning)
            {
                return;
            }
            KdyFilterCardController selected = null;
            foreach (KdyFilterCardController card in _cards)
            {
                if (card.FilterIndex == filterIndex && card.IsDocked)
                {
                    selected = card;
                    break;
                }
            }
            if (selected == null)
            {
                return;
            }
            if (_activeCard != null && _activeCard != selected)
            {
                _activeCard.ResetPose();
            }
            _activeCard = selected;
            _isScanning = true;
            foreach (KdyFilterCardController card in _cards)
            {
                card.SetInteractionEnabled(false);
            }
            ClearOutput();
            _scanFrame.gameObject.SetActive(true);
            SetMessage(FilterName(filterIndex) + " 필터로 같은 입력을 훑고 있습니다.",
                "청록색은 양수, 주황색은 음수 응답입니다. 각 결과 안에서 밝을수록 응답 크기가 큽니다. 스캔이 끝나면 다른 카드로 바꿔 보세요.");
            StartCoroutine(ScanFilter(filterIndex));
        }

        private IEnumerator ScanFilter(int filterIndex)
        {
            int width = KdySignExampleData.Width;
            int height = KdySignExampleData.Height;
            float[] responses = _responses[filterIndex];
            float maximum = _responseMaximums[filterIndex];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    _scanFrame.localPosition = new Vector3((x + .5f) / width - .5f,
                        .5f - (y + .5f) / height, -.04f);
                    _outputTexture.SetPixel(x, height - 1 - y, ResponseColor(responses[y * width + x], maximum));
                    _outputTexture.Apply(false, false);
                    yield return _scanDelay;
                }
            }
            _completedFilters.Add(filterIndex);
            _isScanning = false;
            _scanFrame.gameObject.SetActive(false);
            foreach (KdyFilterCardController card in _cards)
            {
                card.SetInteractionEnabled(card != _activeCard);
            }
            if (ReadyToCollect)
            {
                SetMessage("비교 완료! 같은 간판에서도 필터에 따라 다른 경계가 드러납니다. 양피지 ③을 수집하세요.",
                    "두 종류 이상의 필터를 비교했어요. 세 번째 필터도 체험할 수 있습니다. 이 값은 학습된 모델이 아닌 정해진 교육용 필터의 응답입니다.");
            }
            else
            {
                SetMessage(FilterName(filterIndex) + " 결과를 확인했어요. 다른 카드를 끼워 보세요. 1 / 2",
                    "다음 카드도 같은 입력과 같은 이동 간격을 사용합니다. 필터 값만 바꿨을 때 어느 경계가 달라지는지 관찰하세요.");
            }
        }

        private void BuildTeachingTextures()
        {
            int width = KdySignExampleData.Width;
            int height = KdySignExampleData.Height;
            _inputTexture = CreateTexture("Kdy Color Sign Input");
            Color[] image = KdySignExampleData.CreateImage();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Teaching data starts at the top row; Unity textures start at the bottom row.
                    _inputTexture.SetPixel(x, height - 1 - y, image[y * width + x]);
                }
            }
            _inputTexture.Apply(false, false);
            _outputTexture = CreateTexture("Kdy Filter Feature Map");
            _emptyOutput = new Color[width * height];
            for (int index = 0; index < _emptyOutput.Length; index++)
            {
                _emptyOutput[index] = new Color(.025f, .04f, .06f, 1f);
            }
            _inputOriginalMaterial = _inputSurface.sharedMaterial;
            _outputOriginalMaterial = _outputSurface.sharedMaterial;
            _inputMaterial = CreateDisplayMaterial(_inputOriginalMaterial, _inputTexture, "Kdy Sign Input Material");
            _outputMaterial = CreateDisplayMaterial(_outputOriginalMaterial, _outputTexture, "Kdy Filter Output Material");
            _inputSurface.sharedMaterial = _inputMaterial;
            _outputSurface.sharedMaterial = _outputMaterial;
        }

        private void CacheFilterResponses()
        {
            int width = KdySignExampleData.Width;
            int height = KdySignExampleData.Height;
            _responses = new float[3][];
            _responseMaximums = new float[3];
            for (int kind = 0; kind < 3; kind++)
            {
                float[] responses = new float[width * height];
                float maximum = 0f;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float response = KdySignExampleData.Filter(kind, x, y);
                        responses[y * width + x] = response;
                        maximum = Mathf.Max(maximum, Mathf.Abs(response));
                    }
                }
                _responses[kind] = responses;
                _responseMaximums[kind] = maximum;
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

        private void ClearOutput()
        {
            _outputTexture.SetPixels(_emptyOutput);
            _outputTexture.Apply(false, false);
        }

        internal static Color ResponseColor(float response, float maximum)
        {
            float strength = Mathf.Clamp01(Mathf.Abs(response) / Mathf.Max(.0001f, maximum));
            Color background = new Color(.025f, .04f, .06f, 1f);
            Color signedColor = response < 0f ? new Color(1f, .42f, .15f, 1f) : new Color(.1f, .85f, 1f, 1f);
            return Color.Lerp(background, signedColor, strength);
        }

        private string FilterName(int filterIndex)
        {
            return filterIndex == 0 ? "세로 경계" : filterIndex == 1 ? "가로 경계" : "전체 윤곽";
        }

        private void SetMessage(string status, string hint)
        {
            _status.text = status;
            _hint = hint;
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            if (_cards != null)
            {
                foreach (KdyFilterCardController card in _cards)
                {
                    if (card != null)
                    {
                        card.Docked -= HandleCardDocked;
                    }
                }
            }
            if (_inputSurface != null && _inputSurface.sharedMaterial == _inputMaterial)
            {
                _inputSurface.sharedMaterial = _inputOriginalMaterial;
            }
            if (_outputSurface != null && _outputSurface.sharedMaterial == _outputMaterial)
            {
                _outputSurface.sharedMaterial = _outputOriginalMaterial;
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
