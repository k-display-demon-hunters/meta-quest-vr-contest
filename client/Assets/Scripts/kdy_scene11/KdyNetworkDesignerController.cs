using System;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyNetworkDesignerController : MonoBehaviour
    {
        [SerializeField] private Transform[] _layerSlots = new Transform[3];
        [SerializeField] private KdyLayerBlockController[] _layerBlocks = new KdyLayerBlockController[3];
        [SerializeField] private KdyLayerBlockController[] _kernelParts = new KdyLayerBlockController[2];
        [SerializeField] private KdyLayerBlockController[] _activationParts = new KdyLayerBlockController[2];
        [SerializeField] private Transform _kernelSlot;
        [SerializeField] private Transform _activationSlot;
        [SerializeField] private Transform _featureDial;
        [SerializeField] private KdyLessonGrabController _featureDialGrab;
        [SerializeField] private Text _status;
        [SerializeField] private Renderer _inputSurface;
        [SerializeField] private float _dropRadius = .7f;
        [SerializeField] private float _dialLowAngle = -45f;
        [SerializeField] private float _dialHighAngle = 45f;

        private readonly KdyLayerBlockController[] _placedLayers = new KdyLayerBlockController[3];
        private KdyLayerBlockController _selectedKernel;
        private KdyLayerBlockController _selectedActivation;
        private KdyNetworkConfiguration _configuration;
        private Transform _dialParent;
        private Vector3 _dialHomePosition;
        private Quaternion _dialHomeRotation;
        private Rigidbody _dialBody;
        private Material _originalInputMaterial;
        private Material _inputMaterial;
        private Texture2D _inputTexture;
        private bool _initialized;
        private bool _resetting;
        private bool _hasGap;

        public event Action Changed;
        public bool ReadyToTrain => _initialized && _configuration.IsComplete && !_hasGap && !IsAnyPartGrabbed();

        public string Hint
        {
            get
            {
                if (_hasGap)
                {
                    return "빈 슬롯을 건너뛴 층이 있어요. 입력 쪽 첫 슬롯부터 빈틈없이 이어 주세요.";
                }
                if (!_initialized || _configuration.LayerCount == 0)
                {
                    return "선반의 은닉층 블록을 Grip으로 잡아 입력 바로 오른쪽 첫 슬롯에 놓으세요. 1~3개를 배치할 수 있어요.";
                }
                if (!_configuration.HasFeatureMaps)
                {
                    return "피쳐맵 다이얼을 Grip으로 잡고 손목을 돌린 뒤 놓으세요. 각 은닉층의 판 수를 4장 또는 32장으로 정합니다.";
                }
                if (!_configuration.HasKernel)
                {
                    return "3×3 또는 9×9 커널 틀을 잡아 필터 슬롯에 놓으세요. 컬러 입력의 첫 필터 깊이는 RGB 세 채널이에요.";
                }
                if (!_configuration.HasActivation)
                {
                    return "ReLU 또는 Sigmoid 토큰을 잡아 활성화함수 슬롯에 놓으세요.";
                }
                if (IsAnyPartGrabbed())
                {
                    return "잡고 있는 부품이나 다이얼을 놓으면 설계를 확인할 수 있어요.";
                }
                return "설계가 준비됐어요. 층 수가 많다고 항상 더 좋은 것은 아니에요. 학습실에서 같은 조건으로 결과를 비교해 보세요.";
            }
        }

        public void Initialize(Camera viewCamera, KdyNetworkConfiguration configuration)
        {
            if (_initialized)
            {
                return;
            }
            if (configuration == null || !HasRequiredReferences())
            {
                Debug.LogError("[KdyLearning] Scene 11 needs three layer blocks and sockets, two kernels, two activation tokens, and a grab dial.", this);
                return;
            }
            _configuration = configuration;
            foreach (KdyLayerBlockController block in _layerBlocks)
            {
                block.Initialize(this);
            }
            foreach (KdyLayerBlockController part in _kernelParts)
            {
                part.Initialize(this);
            }
            foreach (KdyLayerBlockController part in _activationParts)
            {
                part.Initialize(this);
            }
            _dialParent = _featureDial.parent;
            _dialHomePosition = _featureDial.localPosition;
            _dialHomeRotation = _featureDial.localRotation;
            _dialBody = _featureDial.GetComponent<Rigidbody>();
            _dialLowAngle = Mathf.Clamp(_dialLowAngle, -150f, 140f);
            _dialHighAngle = Mathf.Clamp(_dialHighAngle, _dialLowAngle + 5f, 150f);
            _dropRadius = Mathf.Max(.1f, _dropRadius);
            _featureDialGrab.GrabbedChanged += HandleDialGrabbed;
            CreateInputSurface();
            _initialized = true;
            RestoreConfiguration();
        }

        public void ResetDesign()
        {
            if (!_initialized)
            {
                return;
            }
            _resetting = true;
            SetPartsEnabled(false);
            _featureDialGrab.SetInteractionEnabled(false);
            _configuration.Clear();
            RestoreConfiguration();
            SetPartsEnabled(true);
            _featureDialGrab.SetInteractionEnabled(true);
            _resetting = false;
            RefreshDisplay();
        }

        private void CreateInputSurface()
        {
            if (_inputSurface == null || _inputSurface.sharedMaterial == null)
            {
                return;
            }
            int width = KdySignExampleData.Width;
            int height = KdySignExampleData.Height;
            Color[] source = KdyTrainingExampleData.CreateBlurredImage();
            Color[] pixels = new Color[source.Length];
            // The authored sign uses top-first rows; Unity texture rows begin at the bottom.
            for (int y = 0; y < height; y++)
            {
                Array.Copy(source, y * width, pixels, (height - 1 - y) * width, width);
            }
            _inputTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Kdy Designer Blurred Input",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _inputTexture.SetPixels(pixels);
            _inputTexture.Apply(false, true);
            _originalInputMaterial = _inputSurface.sharedMaterial;
            _inputMaterial = new Material(_originalInputMaterial)
            {
                name = "Kdy Designer Input Material",
                mainTexture = _inputTexture
            };
            if (_inputMaterial.HasProperty("_BaseMap"))
            {
                _inputMaterial.SetTexture("_BaseMap", _inputTexture);
            }
            if (_inputMaterial.HasProperty("_MainTex"))
            {
                _inputMaterial.SetTexture("_MainTex", _inputTexture);
            }
            if (_inputMaterial.HasProperty("_BaseColor"))
            {
                _inputMaterial.SetColor("_BaseColor", Color.white);
            }
            if (_inputMaterial.HasProperty("_Color"))
            {
                _inputMaterial.SetColor("_Color", Color.white);
            }
            _inputSurface.sharedMaterial = _inputMaterial;
        }

        private void RestoreConfiguration()
        {
            int layers = Mathf.Clamp(_configuration.LayerCount, 0, _layerSlots.Length);
            _configuration.LayerCount = layers;
            _configuration.FeatureMaps = _configuration.FeatureMaps >= 32 ? 32 : 4;
            for (int i = 0; i < _layerBlocks.Length; i++)
            {
                _placedLayers[i] = i < layers ? _layerBlocks[i] : null;
                if (i < layers)
                {
                    _layerBlocks[i].SnapTo(_layerSlots[i]);
                }
                else
                {
                    _layerBlocks[i].ReturnHome();
                }
            }
            _selectedKernel = null;
            foreach (KdyLayerBlockController part in _kernelParts)
            {
                bool selected = _configuration.HasKernel &&
                    ((_configuration.KernelSize == 9 && part.Kind == KdyNetworkPartKind.Kernel9) ||
                    (_configuration.KernelSize != 9 && part.Kind == KdyNetworkPartKind.Kernel3));
                if (selected)
                {
                    _selectedKernel = part;
                    part.SnapTo(_kernelSlot);
                }
                else
                {
                    part.ReturnHome();
                }
            }
            _selectedActivation = null;
            foreach (KdyLayerBlockController part in _activationParts)
            {
                bool selected = _configuration.HasActivation &&
                    ((_configuration.UseSigmoid && part.Kind == KdyNetworkPartKind.Sigmoid) ||
                    (!_configuration.UseSigmoid && part.Kind == KdyNetworkPartKind.Relu));
                if (selected)
                {
                    _selectedActivation = part;
                    part.SnapTo(_activationSlot);
                }
                else
                {
                    part.ReturnHome();
                }
            }
            MoveDial(_configuration.FeatureMaps == 32 ? _dialHighAngle : _dialLowAngle);
            RefreshDisplay();
        }

        internal void HandlePartGrabbed(KdyLayerBlockController part, bool grabbed)
        {
            if (!_initialized || _resetting || !isActiveAndEnabled)
            {
                return;
            }
            if (grabbed)
            {
                RemovePart(part);
            }
            else if (part.Kind == KdyNetworkPartKind.Layer)
            {
                PlaceLayer(part);
            }
            else if (part.Kind == KdyNetworkPartKind.Kernel3 || part.Kind == KdyNetworkPartKind.Kernel9)
            {
                if (Vector3.Distance(part.transform.position, _kernelSlot.position) <= _dropRadius)
                {
                    _selectedKernel?.ReturnHome();
                    _selectedKernel = part;
                    _configuration.KernelSize = part.Kind == KdyNetworkPartKind.Kernel9 ? 9 : 3;
                    _configuration.HasKernel = true;
                    part.SnapTo(_kernelSlot);
                }
                else
                {
                    part.ReturnHome();
                }
            }
            else
            {
                if (Vector3.Distance(part.transform.position, _activationSlot.position) <= _dropRadius)
                {
                    _selectedActivation?.ReturnHome();
                    _selectedActivation = part;
                    _configuration.UseSigmoid = part.Kind == KdyNetworkPartKind.Sigmoid;
                    _configuration.HasActivation = true;
                    part.SnapTo(_activationSlot);
                }
                else
                {
                    part.ReturnHome();
                }
            }
            RefreshDisplay();
        }

        private void RemovePart(KdyLayerBlockController part)
        {
            for (int i = 0; i < _placedLayers.Length; i++)
            {
                if (_placedLayers[i] == part)
                {
                    _placedLayers[i] = null;
                }
            }
            if (_selectedKernel == part)
            {
                _selectedKernel = null;
                _configuration.HasKernel = false;
            }
            if (_selectedActivation == part)
            {
                _selectedActivation = null;
                _configuration.HasActivation = false;
            }
        }

        private void PlaceLayer(KdyLayerBlockController part)
        {
            int nearest = -1;
            float distance = _dropRadius;
            for (int i = 0; i < _layerSlots.Length; i++)
            {
                if (_placedLayers[i] != null)
                {
                    continue;
                }
                float candidate = Vector3.Distance(part.transform.position, _layerSlots[i].position);
                if (candidate <= distance)
                {
                    distance = candidate;
                    nearest = i;
                }
            }
            if (nearest < 0)
            {
                part.ReturnHome();
                return;
            }
            _placedLayers[nearest] = part;
            part.SnapTo(_layerSlots[nearest]);
        }

        private void LateUpdate()
        {
            if (!_initialized || !_featureDialGrab.IsGrabbed)
            {
                return;
            }
            float angle = Mathf.Clamp(DialAngle(), _dialLowAngle, _dialHighAngle);
            MoveDial(angle);
            int maps = angle >= (_dialLowAngle + _dialHighAngle) * .5f ? 32 : 4;
            if (_configuration.FeatureMaps != maps)
            {
                _configuration.FeatureMaps = maps;
                RefreshDisplay();
            }
        }

        private void HandleDialGrabbed(bool grabbed)
        {
            if (!_initialized || _resetting || !isActiveAndEnabled)
            {
                return;
            }
            _configuration.HasFeatureMaps = !grabbed;
            if (!grabbed)
            {
                bool high = DialAngle() >= (_dialLowAngle + _dialHighAngle) * .5f;
                _configuration.FeatureMaps = high ? 32 : 4;
                MoveDial(high ? _dialHighAngle : _dialLowAngle);
            }
            RefreshDisplay();
        }

        private float DialAngle()
        {
            Quaternion rotation = _dialParent != null
                ? Quaternion.Inverse(_dialParent.rotation) * _featureDial.rotation
                : _featureDial.rotation;
            Quaternion delta = Quaternion.Inverse(_dialHomeRotation) * rotation;
            return Mathf.DeltaAngle(0f, delta.eulerAngles.z);
        }

        private void MoveDial(float angle)
        {
            Quaternion localRotation = _dialHomeRotation * Quaternion.AngleAxis(angle, Vector3.forward);
            Vector3 position = _dialParent != null ? _dialParent.TransformPoint(_dialHomePosition) : _dialHomePosition;
            Quaternion rotation = _dialParent != null ? _dialParent.rotation * localRotation : localRotation;
            _featureDial.SetPositionAndRotation(position, rotation);
            if (_dialBody != null)
            {
                _dialBody.position = position;
                _dialBody.rotation = rotation;
            }
        }

        private void RefreshDisplay()
        {
            int count = 0;
            bool emptySeen = false;
            _hasGap = false;
            for (int i = 0; i < _placedLayers.Length; i++)
            {
                if (_placedLayers[i] == null)
                {
                    emptySeen = true;
                }
                else
                {
                    count++;
                    _hasGap |= emptySeen;
                }
            }
            _configuration.LayerCount = _hasGap ? 0 : count;
            foreach (KdyLayerBlockController block in _layerBlocks)
            {
                block.SetFeatureMaps(_configuration.FeatureMaps);
            }
            string maps = _configuration.HasFeatureMaps ? _configuration.FeatureMaps.ToString() : "선택 필요";
            string kernel = _configuration.HasKernel ? _configuration.KernelSize + "×" + _configuration.KernelSize : "선택 필요";
            string activation = !_configuration.HasActivation ? "선택 필요" : _configuration.UseSigmoid ? "Sigmoid" : "ReLU";
            _status.text = $"은닉층 {count}/3 · 피쳐맵 {maps} · 커널 {kernel} · {activation}\n"
                + $"입력 16×12×3 → 은닉층마다 16×12×{_configuration.FeatureMaps} → 출력\n"
                + $"same padding · 첫 커널 깊이 3 / 다음 층 커널 깊이 {_configuration.FeatureMaps}\n"
                + (_hasGap ? "빈 슬롯을 건너뛴 층이 있어요." : "부품을 다시 잡아 빼면 해당 선택이 해제됩니다.");
            Changed?.Invoke();
        }

        private bool IsAnyPartGrabbed()
        {
            if (_featureDialGrab.IsGrabbed)
            {
                return true;
            }
            foreach (KdyLayerBlockController part in _layerBlocks)
            {
                if (part.IsGrabbed)
                {
                    return true;
                }
            }
            foreach (KdyLayerBlockController part in _kernelParts)
            {
                if (part.IsGrabbed)
                {
                    return true;
                }
            }
            foreach (KdyLayerBlockController part in _activationParts)
            {
                if (part.IsGrabbed)
                {
                    return true;
                }
            }
            return false;
        }

        private void SetPartsEnabled(bool enabled)
        {
            foreach (KdyLayerBlockController part in _layerBlocks)
            {
                part.SetInteractionEnabled(enabled);
            }
            foreach (KdyLayerBlockController part in _kernelParts)
            {
                part.SetInteractionEnabled(enabled);
            }
            foreach (KdyLayerBlockController part in _activationParts)
            {
                part.SetInteractionEnabled(enabled);
            }
        }

        private bool HasRequiredReferences()
        {
            if (_layerSlots == null || _layerSlots.Length != 3 || _layerBlocks == null || _layerBlocks.Length != 3 ||
                _kernelParts == null || _kernelParts.Length != 2 || _activationParts == null || _activationParts.Length != 2 ||
                _kernelSlot == null || _activationSlot == null || _featureDial == null || _featureDialGrab == null || _status == null)
            {
                return false;
            }
            for (int i = 0; i < 3; i++)
            {
                if (_layerSlots[i] == null || _layerBlocks[i] == null)
                {
                    return false;
                }
            }
            for (int i = 0; i < 2; i++)
            {
                if (_kernelParts[i] == null || _activationParts[i] == null)
                {
                    return false;
                }
            }
            return true;
        }

        private void OnDestroy()
        {
            if (_featureDialGrab != null)
            {
                _featureDialGrab.GrabbedChanged -= HandleDialGrabbed;
            }
            if (_inputSurface != null && _inputSurface.sharedMaterial == _inputMaterial)
            {
                _inputSurface.sharedMaterial = _originalInputMaterial;
            }
            if (_inputMaterial != null)
            {
                Destroy(_inputMaterial);
            }
            if (_inputTexture != null)
            {
                Destroy(_inputTexture);
            }
        }
    }
}
