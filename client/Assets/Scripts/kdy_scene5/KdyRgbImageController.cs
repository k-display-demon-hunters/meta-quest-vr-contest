using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyRgbImageController : MonoBehaviour
    {
        [SerializeField] private Transform _assembly;
        [SerializeField] private Renderer _colorSurface;
        [SerializeField] private Transform[] _channelPlanes = new Transform[3];
        [SerializeField] private Renderer[] _channelSurfaces = new Renderer[3];
        [SerializeField] private Transform _unfoldHandle;
        [SerializeField] private KdyLessonGrabController _handleGrab;
        [SerializeField] private KdyLessonGrabController[] _planeGrabs = new KdyLessonGrabController[3];
        [SerializeField] private Text _pixelReadout;
        [SerializeField] private Text[] _channelLabels = new Text[3];
        [SerializeField] private Text[] _channelMatrices = new Text[3];
        [SerializeField] private GameObject[] _matrixRoots = new GameObject[0];
        [SerializeField] private Text[] _matrixCells = new Text[0];
        [SerializeField] private GameObject _numberGrid;
        [SerializeField] private Transform[] _selectionMarkers = new Transform[4];
        [SerializeField] private Texture2D _source;
        [SerializeField] private Vector3[] _openPositions =
        {
            new Vector3(-2.3f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.65f),
            new Vector3(2.3f, 0.0f, 1.3f)
        };
        [SerializeField] private float _pullDistance = 2.0f;
        [SerializeField] private int _gridSize = 12;

        private const float OpenGrabThreshold = 0.75f;
        private const float VisibleChannelThreshold = 0.025f;
        private const float ApproachDistance = 3.2f;
        private readonly Vector3[] _closedPositions = new Vector3[3];
        private readonly Quaternion[] _closedRotations = new Quaternion[3];
        private readonly Vector3[] _planeOffsets = new Vector3[3];
        private readonly Quaternion[] _planeRotations = new Quaternion[3];
        private readonly bool[] _planeWasGrabbed = new bool[3];
        private readonly Texture2D[] _generatedTextures = new Texture2D[4];
        private readonly Material[] _runtimeMaterials = new Material[4];
        private readonly HashSet<int> _inspectedPixels = new HashSet<int>();
        private Collider[] _colorColliders;
        private Color32[] _sampledPixels;
        private Camera _camera;
        private Vector3 _handleHome;
        private Quaternion _handleHomeRotation;
        private Vector3 _handleGrabStart;
        private float _amountAtGrab;
        private float _unfoldAmount;
        private bool _hasApproached;
        private bool _hasUnfolded;
        private bool _initialized;
        private bool _showNumbers;
        private bool _hasSelectedPixel;
        private int _selectedX;
        private int _selectedY;

        public event Action Changed;

        public bool ReadyToCollect => _hasApproached && _hasUnfolded && _inspectedPixels.Count >= 3;

        public string Hint
        {
            get
            {
                if (!_hasApproached)
                {
                    return "사진 가까이 다가가 보세요. 컬러 픽셀이 드러납니다.";
                }

                if (!_hasUnfolded)
                {
                    return "사진 오른쪽 손잡이를 Grip으로 잡고 오른쪽으로 당겨 보세요.";
                }

                if (_inspectedPixels.Count < 3)
                {
                    return $"사진이나 RGB판에서 서로 다른 픽셀 3개를 선택하세요. {_inspectedPixels.Count}/3";
                }

                return "RGB 세 판은 같은 이미지의 색 성분입니다. 양피지를 수집하고 다음 공정으로 이동하세요.";
            }
        }

        public void Initialize(Camera camera)
        {
            if (_initialized)
            {
                _camera = camera;
                return;
            }

            if (!HasRequiredReferences())
            {
                Debug.LogError("[Kdy RGB] Source image and all three channel surfaces must be assigned.", this);
                return;
            }

            _camera = camera;
            _gridSize = Mathf.Clamp(_gridSize, 4, 32);
            _pullDistance = Mathf.Max(0.2f, _pullDistance);
            _handleHome = _assembly.InverseTransformPoint(_unfoldHandle.position);
            _handleHomeRotation = _unfoldHandle.localRotation;
            _colorColliders = _colorSurface.GetComponents<Collider>();

            for (int i = 0; i < 3; i++)
            {
                _closedPositions[i] = _channelPlanes[i].localPosition;
                _closedRotations[i] = _channelPlanes[i].localRotation;
                _planeRotations[i] = _closedRotations[i];
                _planeGrabs[i].SetInteractionEnabled(false);
            }

            BuildChannelTextures();
            BuildChannelMatrices();
            ApplyRuntimeMaterial(_colorSurface, 0, _source);
            for (int i = 0; i < 3; i++)
            {
                ApplyRuntimeMaterial(_channelSurfaces[i], i + 1, _generatedTextures[i + 1]);
            }

            _handleGrab.GrabbedChanged += HandleUnfoldGrabChanged;
            _initialized = true;
            UpdateNumberVisibility();

            UpdateChannelLabels();
            UpdateAssembly();
            UpdateReadout();
            UpdateSelectionMarkers();
        }

        public void SelectPixel(RaycastHit hit)
        {
            if (!_initialized)
            {
                return;
            }

            Renderer surface = FindSurface(hit.transform);
            if (surface == null || !surface.gameObject.activeInHierarchy || !surface.enabled)
            {
                return;
            }

            Vector3 point = surface.transform.InverseTransformPoint(hit.point);
            _selectedX = Mathf.Clamp(Mathf.FloorToInt((point.x + 0.5f) * _gridSize), 0, _gridSize - 1);
            _selectedY = Mathf.Clamp(Mathf.FloorToInt((point.y + 0.5f) * _gridSize), 0, _gridSize - 1);
            _hasSelectedPixel = true;
            if (_hasUnfolded)
            {
                _inspectedPixels.Add(_selectedY * _gridSize + _selectedX);
            }

            UpdateReadout();
            UpdateSelectionMarkers();
            Changed?.Invoke();
        }

        public void Fold()
        {
            if (!_initialized || _handleGrab.IsGrabbed)
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                if (_planeGrabs[i].IsGrabbed)
                {
                    return;
                }
            }

            _unfoldAmount = 0.0f;
            for (int i = 0; i < 3; i++)
            {
                _planeOffsets[i] = Vector3.zero;
                _planeRotations[i] = _closedRotations[i];
                _planeWasGrabbed[i] = false;
            }

            MoveHandleToRail();
            UpdateAssembly();
            UpdateReadout();
            UpdateSelectionMarkers();
            Changed?.Invoke();
        }

        public void ToggleNumbers()
        {
            _showNumbers = !_showNumbers;
            UpdateNumberVisibility();

            UpdateReadout();
            Changed?.Invoke();
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            if (!_hasApproached && _camera != null &&
                Vector3.Distance(_camera.transform.position, _colorSurface.transform.position) <= ApproachDistance)
            {
                _hasApproached = true;
                _runtimeMaterials[0].mainTexture = _generatedTextures[0];
                UpdateReadout();
                Changed?.Invoke();
            }

            if (_handleGrab.IsGrabbed)
            {
                Vector3 handlePosition = _assembly.InverseTransformPoint(_unfoldHandle.position);
                float previousAmount = _unfoldAmount;
                _unfoldAmount = Mathf.Clamp01(_amountAtGrab + (handlePosition.x - _handleGrabStart.x) / _pullDistance);
                if (!_hasUnfolded && _unfoldAmount >= OpenGrabThreshold)
                {
                    _hasUnfolded = true;
                    UpdateReadout();
                    Changed?.Invoke();
                }

                if (Mathf.Abs(previousAmount - _unfoldAmount) > 0.0001f)
                {
                    UpdateAssembly();
                    UpdateSelectionMarkers();
                }
            }

            PreservePlaneManipulation();
        }

        private void HandleUnfoldGrabChanged(bool grabbed)
        {
            if (grabbed)
            {
                _handleGrabStart = _assembly.InverseTransformPoint(_unfoldHandle.position);
                _amountAtGrab = _unfoldAmount;
                return;
            }

            MoveHandleToRail();
            UpdateAssembly();
            UpdateReadout();
            UpdateSelectionMarkers();
            Changed?.Invoke();
        }

        private void PreservePlaneManipulation()
        {
            for (int i = 0; i < 3; i++)
            {
                bool grabbed = _planeGrabs[i].IsGrabbed;
                if (!grabbed && _planeWasGrabbed[i])
                {
                    Vector3 basePosition = Vector3.Lerp(_closedPositions[i], _openPositions[i], _unfoldAmount);
                    _planeOffsets[i] = _channelPlanes[i].localPosition - basePosition;
                    _planeRotations[i] = _channelPlanes[i].localRotation;
                    Changed?.Invoke();
                }

                _planeWasGrabbed[i] = grabbed;
            }
        }

        private void MoveHandleToRail()
        {
            Vector3 localPosition = _handleHome + Vector3.right * (_unfoldAmount * _pullDistance);
            _unfoldHandle.position = _assembly.TransformPoint(localPosition);
            _unfoldHandle.localRotation = _handleHomeRotation;
        }

        private void UpdateAssembly()
        {
            bool channelsVisible = _unfoldAmount > VisibleChannelThreshold;
            _colorSurface.enabled = !channelsVisible;
            foreach (Collider collider in _colorColliders)
            {
                collider.enabled = !channelsVisible;
            }

            for (int i = 0; i < 3; i++)
            {
                _channelPlanes[i].gameObject.SetActive(channelsVisible);
                _planeGrabs[i].SetInteractionEnabled(_unfoldAmount >= OpenGrabThreshold);
                if (!_planeGrabs[i].IsGrabbed)
                {
                    Vector3 basePosition = Vector3.Lerp(_closedPositions[i], _openPositions[i], _unfoldAmount);
                    float freePoseWeight = Mathf.InverseLerp(0.0f, OpenGrabThreshold, _unfoldAmount);
                    _channelPlanes[i].localPosition = basePosition + _planeOffsets[i] * freePoseWeight;
                    _channelPlanes[i].localRotation = Quaternion.Slerp(_closedRotations[i], _planeRotations[i], freePoseWeight);
                }
            }
        }

        private void UpdateReadout()
        {
            if (_pixelReadout == null)
            {
                return;
            }

            if (!_hasSelectedPixel)
            {
                _pixelReadout.text = _hasApproached
                    ? "컬러 한 칸 안에는 R · G · B 세 값이 있습니다.\n오른손 레이로 픽셀을 가리키고 Trigger를 누르세요."
                    : "사진 가까이 다가가 픽셀을 살펴보세요.";
                return;
            }

            Color32 pixel = _sampledPixels[_selectedY * _gridSize + _selectedX];
            int displayedY = _gridSize - _selectedY;
            _pixelReadout.text = $"같은 위치 · {_selectedX + 1}열 {displayedY}행\n"
                + $"<color=#FF817D>R {pixel.r}</color>  +  <color=#9AF4B4>G {pixel.g}</color>  +  <color=#84BDFF>B {pixel.b}</color>\n"
                + (_unfoldAmount > VisibleChannelThreshold
                    ? "세 판의 빛나는 칸이 원래 사진의 픽셀 하나를 이룹니다."
                    : "세 색 성분을 모으면 원래 컬러 픽셀이 됩니다.");

            if (_showNumbers)
            {
                _pixelReadout.text += $"\n0은 해당 색 없음 · 255는 최대 밝기\n정규화 값: {pixel.r / 255.0f:0.00} / {pixel.g / 255.0f:0.00} / {pixel.b / 255.0f:0.00}";
            }

            UpdateChannelLabels();
        }

        private void UpdateChannelLabels()
        {
            string[] names = { "R · 빨강 성분", "G · 초록 성분", "B · 파랑 성분" };
            for (int i = 0; i < _channelLabels.Length && i < 3; i++)
            {
                if (_channelLabels[i] == null)
                {
                    continue;
                }

                string value = string.Empty;
                if (_hasSelectedPixel)
                {
                    Color32 pixel = _sampledPixels[_selectedY * _gridSize + _selectedX];
                    int channelValue = i == 0 ? pixel.r : i == 1 ? pixel.g : pixel.b;
                    value = $"\n선택한 칸: {channelValue}";
                }

                _channelLabels[i].text = names[i] + value;
            }
        }

        private void UpdateSelectionMarkers()
        {
            for (int i = 0; i < _selectionMarkers.Length; i++)
            {
                Transform marker = _selectionMarkers[i];
                if (marker == null)
                {
                    continue;
                }

                bool markerVisible = _hasSelectedPixel &&
                    (i == 0 ? _unfoldAmount <= VisibleChannelThreshold : _unfoldAmount > VisibleChannelThreshold);
                marker.gameObject.SetActive(markerVisible);
                if (!markerVisible)
                {
                    continue;
                }

                marker.localPosition = new Vector3(
                    -0.5f + (_selectedX + 0.5f) / _gridSize,
                    -0.5f + (_selectedY + 0.5f) / _gridSize,
                    -0.85f);
                marker.localScale = new Vector3(1.0f / _gridSize, 1.0f / _gridSize, 1.0f);
            }
        }

        private Renderer FindSurface(Transform hitTransform)
        {
            if (hitTransform == _colorSurface.transform || hitTransform.IsChildOf(_colorSurface.transform))
            {
                return _colorSurface;
            }

            for (int i = 0; i < 3; i++)
            {
                if (hitTransform == _channelSurfaces[i].transform || hitTransform.IsChildOf(_channelPlanes[i]))
                {
                    return _channelSurfaces[i];
                }
            }

            return null;
        }

        private void BuildChannelTextures()
        {
            _sampledPixels = new Color32[_gridSize * _gridSize];
            Color32[][] textures = new Color32[4][];
            for (int i = 0; i < 4; i++)
            {
                textures[i] = new Color32[_sampledPixels.Length];
            }

            for (int y = 0; y < _gridSize; y++)
            {
                for (int x = 0; x < _gridSize; x++)
                {
                    int index = y * _gridSize + x;
                    Color32 pixel = _source.GetPixelBilinear((x + 0.5f) / _gridSize, (y + 0.5f) / _gridSize);
                    pixel.a = 255;
                    _sampledPixels[index] = pixel;
                    textures[0][index] = pixel;
                    textures[1][index] = new Color32(pixel.r, 0, 0, 255);
                    textures[2][index] = new Color32(0, pixel.g, 0, 255);
                    textures[3][index] = new Color32(0, 0, pixel.b, 255);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                Texture2D texture = new Texture2D(_gridSize, _gridSize, TextureFormat.RGBA32, false);
                texture.name = $"Kdy RGB channel {i}";
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(textures[i]);
                texture.Apply(false, false);
                _generatedTextures[i] = texture;
            }
        }

        private void BuildChannelMatrices()
        {
            for (int channel = 0; channel < _channelMatrices.Length && channel < 3; channel++)
            {
                Text matrix = _channelMatrices[channel];
                if (matrix == null)
                {
                    continue;
                }

                StringBuilder values = new StringBuilder(_gridSize * _gridSize * 4);
                for (int y = _gridSize - 1; y >= 0; y--)
                {
                    for (int x = 0; x < _gridSize; x++)
                    {
                        Color32 pixel = _sampledPixels[y * _gridSize + x];
                        int value = channel == 0 ? pixel.r : channel == 1 ? pixel.g : pixel.b;
                        if (x > 0)
                        {
                            values.Append(' ');
                        }

                        values.Append(value.ToString("D3"));
                    }

                    if (y > 0)
                    {
                        values.Append('\n');
                    }
                }

                matrix.text = values.ToString();
            }

            int cellsPerChannel = _gridSize * _gridSize;
            int cellCount = Mathf.Min(_matrixCells.Length, cellsPerChannel * 3);
            for (int index = 0; index < cellCount; index++)
            {
                Text cell = _matrixCells[index];
                if (cell == null)
                {
                    continue;
                }

                int channel = index / cellsPerChannel;
                int row = index % cellsPerChannel / _gridSize;
                int x = index % _gridSize;
                int y = _gridSize - 1 - row;
                Color32 pixel = _sampledPixels[y * _gridSize + x];
                int value = channel == 0 ? pixel.r : channel == 1 ? pixel.g : pixel.b;
                cell.text = value.ToString("D3");
            }
        }

        private void UpdateNumberVisibility()
        {
            if (_numberGrid != null)
            {
                _numberGrid.SetActive(_showNumbers);
            }

            foreach (Text matrix in _channelMatrices)
            {
                if (matrix != null)
                {
                    matrix.gameObject.SetActive(_showNumbers);
                }
            }

            foreach (GameObject matrixRoot in _matrixRoots)
            {
                if (matrixRoot != null)
                {
                    matrixRoot.SetActive(_showNumbers);
                }
            }
        }

        private void ApplyRuntimeMaterial(Renderer target, int index, Texture texture)
        {
            Material material = new Material(target.sharedMaterial);
            material.name = $"Kdy RGB surface {index}";
            material.mainTexture = texture;
            target.sharedMaterial = material;
            _runtimeMaterials[index] = material;
        }

        private bool HasRequiredReferences()
        {
            if (_assembly == null || _colorSurface == null || _unfoldHandle == null || _handleGrab == null ||
                _source == null || !_source.isReadable || _channelPlanes.Length != 3 ||
                _channelSurfaces.Length != 3 || _planeGrabs.Length != 3 || _openPositions.Length != 3)
            {
                return false;
            }

            for (int i = 0; i < 3; i++)
            {
                if (_channelPlanes[i] == null || _channelSurfaces[i] == null || _planeGrabs[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnDestroy()
        {
            if (_handleGrab != null)
            {
                _handleGrab.GrabbedChanged -= HandleUnfoldGrabChanged;
            }

            for (int i = 0; i < 4; i++)
            {
                if (_runtimeMaterials[i] != null)
                {
                    Destroy(_runtimeMaterials[i]);
                }

                if (_generatedTextures[i] != null)
                {
                    Destroy(_generatedTextures[i]);
                }
            }
        }
    }
}
