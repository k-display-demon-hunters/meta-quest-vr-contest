using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    [DefaultExecutionOrder(100)]
    public sealed class KdySignComparisonController : MonoBehaviour
    {
        [SerializeField] private Transform _compareHandle;
        [SerializeField] private Transform _sliderRail;
        [SerializeField] private KdyLessonGrabController _handleGrab;
        [SerializeField] private Renderer _blurredSurface;
        [SerializeField] private Renderer _resultSurface;
        [SerializeField] private Renderer _clearSurface;
        [SerializeField] private Renderer _wipeSurface;
        [SerializeField] private Renderer _zoomSurface;
        [SerializeField] private Transform _zoomMarker;
        [SerializeField] private Text _status;
        [SerializeField] private float _travel = .7f;

        private const int SurfaceCount = 5;
        private const int ZoomSize = 6;
        private readonly Renderer[] _surfaces = new Renderer[SurfaceCount];
        private readonly Material[] _originalMaterials = new Material[SurfaceCount];
        private readonly Material[] _materials = new Material[SurfaceCount];
        private readonly Texture2D[] _textures = new Texture2D[SurfaceCount];
        private Color[] _blurredPixels;
        private Color[] _resultPixels;
        private Color[] _clearPixels;
        private Color[] _wipePixels;
        private KdyLessonProgressController _progress;
        private Rigidbody _handleBody;
        private Vector3 _railOffset;
        private Quaternion _railRotation;
        private float _amount = .5f;
        private float _improvement;
        private int _splitColumn = -1;
        private int _focusX = 4;
        private int _focusY = 5;
        private bool _initialized;
        private bool _hasValidTraining;
        private bool _revealed;
        private bool _visitedLeft;
        private bool _visitedRight;
        private bool _resetting;

        public event Action Changed;
        public bool HasCompared { get; private set; }
        public bool IsReady => _initialized && _hasValidTraining && _revealed;

        public string Hint
        {
            get
            {
                if (!_hasValidTraining)
                {
                    return "설계실에서 학습을 완료한 다음, 통과한 결과로 새 간판을 가져오세요.";
                }
                if (!_revealed)
                {
                    return "새 간판을 복원 장치에 넣어 보세요. 아직 내 신경망 결과는 표시하지 않습니다.";
                }
                if (HasCompared)
                {
                    return "흐린 입력·내 신경망 결과·원본을 비교했어요. 판의 글자를 Trigger로 가리키면 확대해서 볼 수 있습니다.";
                }
                if (_visitedLeft && _visitedRight)
                {
                    return "좌우 결과를 모두 확인했어요. Grip을 놓으면 비교가 완료됩니다.";
                }
                return "민트색 비교 손잡이를 Grip으로 잡고 양쪽 끝까지 움직여 보세요. 왼쪽 끝은 흐린 입력, 오른쪽 끝은 내 신경망 결과입니다.";
            }
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            if (IsReady && _handleGrab.IsGrabbed)
            {
                Vector3 railPosition = _sliderRail.InverseTransformPoint(_compareHandle.position);
                _amount = Mathf.InverseLerp(-_travel, _travel, Mathf.Clamp(railPosition.x, -_travel, _travel));
                bool visitedLeft = _visitedLeft;
                bool visitedRight = _visitedRight;
                _visitedLeft |= _amount < .2f;
                _visitedRight |= _amount > .8f;
                MoveHandle(_amount);

                int split = Mathf.RoundToInt(_amount * KdyFactorySignExampleData.Width);
                if (_splitColumn != split)
                {
                    _focusX = Mathf.Clamp(split, 0, KdyFactorySignExampleData.Width - 1);
                    RefreshWipe();
                    RefreshStatus();
                }
                if (visitedLeft != _visitedLeft || visitedRight != _visitedRight)
                {
                    RefreshStatus();
                    Changed?.Invoke();
                }
            }
            else
            {
                // XRI can restore its parent after selectExited; hold the saved rail pose in world space.
                MoveHandle(_amount);
            }
        }

        public void Initialize(KdyLessonProgressController progress)
        {
            if (_initialized)
            {
                return;
            }
            if (!HasRequiredReferences())
            {
                Debug.LogError("[KdyLearning] Scene 16 needs five sign surfaces, a rail, a grab handle and status text.", this);
                return;
            }

            _progress = progress;
            _hasValidTraining = progress != null && progress.HasTrainingResult && progress.TrainingAttempts > 0 &&
                progress.LastTrainedNetwork.IsComplete && progress.LastImprovement >= progress.TargetImprovement;
            _travel = Mathf.Max(.1f, _travel);
            _railOffset = _sliderRail.InverseTransformPoint(_compareHandle.position);
            _railRotation = Quaternion.Inverse(_sliderRail.rotation) * _compareHandle.rotation;
            _handleBody = _compareHandle.GetComponent<Rigidbody>();
            _surfaces[0] = _blurredSurface;
            _surfaces[1] = _resultSurface;
            _surfaces[2] = _clearSurface;
            _surfaces[3] = _wipeSurface;
            _surfaces[4] = _zoomSurface;
            _blurredPixels = KdyFactorySignExampleData.CreateBlurredImage();
            _clearPixels = KdyFactorySignExampleData.CreateClearImage();
            _wipePixels = new Color[_clearPixels.Length];
            if (_hasValidTraining)
            {
                _improvement = KdyFactorySignExampleData.GetImprovement(progress.LastImprovement);
                _resultPixels = KdyFactorySignExampleData.BlendForImprovement(_improvement);
            }

            CreateSurfaces();
            _handleGrab.GrabbedChanged += HandleGrabChanged;
            _initialized = true;
            ResetComparison();
        }

        public void RevealResult()
        {
            if (!_initialized || !_hasValidTraining || _revealed)
            {
                return;
            }

            _revealed = true;
            WritePixels(_textures[1], _resultPixels, KdyFactorySignExampleData.Width, KdyFactorySignExampleData.Height);
            _handleGrab.SetInteractionEnabled(true);
            _splitColumn = -1;
            RefreshWipe();
            RefreshStatus();
            Changed?.Invoke();
        }

        public void ResetComparison()
        {
            if (!_initialized)
            {
                return;
            }

            _resetting = true;
            _handleGrab.SetInteractionEnabled(false);
            _revealed = false;
            _visitedLeft = false;
            _visitedRight = false;
            HasCompared = false;
            _amount = .5f;
            _splitColumn = -1;
            _focusX = 4;
            _focusY = 5;
            MoveHandle(_amount);
            FillBlack(_textures[1]);
            RefreshWipe();
            RefreshStatus();
            _resetting = false;
            Changed?.Invoke();
        }

        public void ResetResult()
        {
            ResetComparison();
        }

        public void SelectPixel(RaycastHit hit)
        {
            if (!IsReady || hit.collider == null)
            {
                return;
            }

            Transform hitTransform = hit.collider.transform;
            for (int index = 0; index < SurfaceCount - 1; index++)
            {
                Transform surfaceTransform = _surfaces[index].transform;
                if (hitTransform != surfaceTransform && !hitTransform.IsChildOf(surfaceTransform))
                {
                    continue;
                }
                Vector3 localPoint = surfaceTransform.InverseTransformPoint(hit.point);
                _focusX = Mathf.Clamp(Mathf.FloorToInt((localPoint.x + .5f) * KdyFactorySignExampleData.Width),
                    0, KdyFactorySignExampleData.Width - 1);
                _focusY = Mathf.Clamp(Mathf.FloorToInt((.5f - localPoint.y) * KdyFactorySignExampleData.Height),
                    0, KdyFactorySignExampleData.Height - 1);
                RefreshZoom();
                RefreshStatus();
                return;
            }
        }

        private bool HasRequiredReferences()
        {
            return _compareHandle != null && _sliderRail != null && _handleGrab != null && _status != null &&
                HasMaterial(_blurredSurface) && HasMaterial(_resultSurface) && HasMaterial(_clearSurface) &&
                HasMaterial(_wipeSurface) && HasMaterial(_zoomSurface);
        }

        private bool HasMaterial(Renderer surface)
        {
            return surface != null && surface.sharedMaterial != null;
        }

        private void CreateSurfaces()
        {
            for (int index = 0; index < SurfaceCount; index++)
            {
                int width = index == SurfaceCount - 1 ? ZoomSize : KdyFactorySignExampleData.Width;
                int height = index == SurfaceCount - 1 ? ZoomSize : KdyFactorySignExampleData.Height;
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.name = "Kdy New Sign Comparison " + index;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                _textures[index] = texture;
                _originalMaterials[index] = _surfaces[index].sharedMaterial;
                Material material = new Material(_originalMaterials[index]);
                material.name = texture.name + " Material";
                material.mainTexture = texture;
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", Color.white);
                }
                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", Color.white);
                }
                _materials[index] = material;
                _surfaces[index].sharedMaterial = material;
            }

            WritePixels(_textures[0], _blurredPixels, KdyFactorySignExampleData.Width, KdyFactorySignExampleData.Height);
            WritePixels(_textures[2], _clearPixels, KdyFactorySignExampleData.Width, KdyFactorySignExampleData.Height);
        }

        private void RefreshWipe()
        {
            int width = KdyFactorySignExampleData.Width;
            int height = KdyFactorySignExampleData.Height;
            _splitColumn = Mathf.RoundToInt(_amount * width);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    _wipePixels[index] = _revealed && x < _splitColumn ? _resultPixels[index] : _blurredPixels[index];
                }
            }
            WritePixels(_textures[3], _wipePixels, width, height);
            RefreshZoom();
        }

        private void RefreshZoom()
        {
            int width = KdyFactorySignExampleData.Width;
            int height = KdyFactorySignExampleData.Height;
            int left = Mathf.Clamp(_focusX - ZoomSize / 2, 0, width - ZoomSize);
            int top = Mathf.Clamp(_focusY - ZoomSize / 2, 0, height - ZoomSize);
            for (int y = 0; y < ZoomSize; y++)
            {
                for (int x = 0; x < ZoomSize; x++)
                {
                    _textures[4].SetPixel(x, ZoomSize - 1 - y, _wipePixels[(top + y) * width + left + x]);
                }
            }
            _textures[4].Apply(false, false);

            if (_zoomMarker != null)
            {
                Vector3 localPosition = _zoomMarker.localPosition;
                localPosition.x = (_focusX + .5f) / width - .5f;
                localPosition.y = .5f - (_focusY + .5f) / height;
                _zoomMarker.localPosition = localPosition;
            }
        }

        private void RefreshStatus()
        {
            if (!_hasValidTraining)
            {
                _status.text = "새 간판 비교 대기\n통과한 학습 기록이 없습니다. 설계·학습을 먼저 완료하세요.";
                return;
            }
            if (!_revealed)
            {
                _status.text = "흐린 간판 | 내 신경망 결과(대기) | 원본\n새 간판을 복원 장치에 넣어 주세요.";
                return;
            }

            string score = _improvement.ToString("0.0", CultureInfo.InvariantCulture);
            string visits = $"왼쪽 {(_visitedLeft ? "✓" : "○")}  오른쪽 {(_visitedRight ? "✓" : "○")}";
            string completion = HasCompared ? "비교 완료" : "Grip으로 양 끝을 확인하고 놓으세요";
            _status.text = $"학습 간판 오차 감소 {_progress.LastImprovement.ToString("0.0", CultureInfo.InvariantCulture)}%\n"
                + $"새 간판 예시 오차 감소 {score}% · 교육용 가상 결과\n"
                + $"{visits} · {completion}\n글자 확대 위치 ({_focusX + 1}, {_focusY + 1})";
        }

        private void MoveHandle(float amount)
        {
            Vector3 railPosition = new Vector3(Mathf.Lerp(-_travel, _travel, amount), _railOffset.y, _railOffset.z);
            Vector3 worldPosition = _sliderRail.TransformPoint(railPosition);
            Quaternion worldRotation = _sliderRail.rotation * _railRotation;
            _compareHandle.SetPositionAndRotation(worldPosition, worldRotation);
            if (_handleBody != null)
            {
                _handleBody.position = worldPosition;
                _handleBody.rotation = worldRotation;
            }
        }

        private void HandleGrabChanged(bool grabbed)
        {
            if (!IsReady || _resetting || grabbed || !isActiveAndEnabled)
            {
                return;
            }
            if (_visitedLeft && _visitedRight)
            {
                HasCompared = true;
            }
            RefreshStatus();
            Changed?.Invoke();
        }

        private void WritePixels(Texture2D texture, Color[] pixels, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, height - 1 - y, pixels[y * width + x]);
                }
            }
            texture.Apply(false, false);
        }

        private void FillBlack(Texture2D texture)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, Color.black);
                }
            }
            texture.Apply(false, false);
        }

        private void OnDestroy()
        {
            if (_handleGrab != null)
            {
                _handleGrab.GrabbedChanged -= HandleGrabChanged;
            }
            for (int index = 0; index < SurfaceCount; index++)
            {
                if (_surfaces[index] != null && _materials[index] != null &&
                    _surfaces[index].sharedMaterial == _materials[index])
                {
                    _surfaces[index].sharedMaterial = _originalMaterials[index];
                }
                if (_materials[index] != null)
                {
                    Destroy(_materials[index]);
                }
                if (_textures[index] != null)
                {
                    Destroy(_textures[index]);
                }
            }
        }
    }
}
