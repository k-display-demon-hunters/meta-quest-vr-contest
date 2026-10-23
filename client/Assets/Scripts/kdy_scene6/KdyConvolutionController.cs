using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyConvolutionController : MonoBehaviour
    {
        private const int InputSize = 6;
        private const int ChannelCount = 3;
        private const int KernelSize = 3;
        private const float CellPitch = .26f;
        private const float InputHalfWidth = .65f;

        [Header("RGB Input")]
        [SerializeField] private Texture2D _source;
        [SerializeField] private Transform _inputRoot;
        [SerializeField] private Renderer[] _inputCells;
        [SerializeField] private Text[] _inputValues;
        [SerializeField] private GameObject _paddingRoot;
        [SerializeField] private Renderer[] _paddingCells;

        [Header("Movable 3 x 3 x 3 Filter")]
        [SerializeField] private Transform _filterRoot;
        [SerializeField] private KdyLessonGrabController _filterGrab;
        [SerializeField] private Renderer[] _filterCells;
        [SerializeField] private Text[] _filterValues;
        [SerializeField] private Transform _snapTarget;
        [SerializeField] private float _snapRadius = .18f;

        [Header("Feature Map")]
        [SerializeField] private Transform _outputRoot;
        [SerializeField] private Renderer[] _outputCells;
        [SerializeField] private Text[] _outputValues;
        [SerializeField] private Transform _resultToken;
        [SerializeField] private Text _calculation;
        [SerializeField] private Text _status;

        private readonly int[] _inputSamples = new int[InputSize * InputSize * ChannelCount];
        private readonly int[] _weights =
        {
            0, 0, 0, -1, 0, 1, 0, 0, 0,
            0, 0, 0, 0, 1, 0, 0, 0, 0,
            0, -1, 0, 0, 0, 0, 0, 1, 0
        };
        private readonly int[] _answerOptions = new int[3];
        private readonly int[] _channelSums = new int[ChannelCount];
        private Rigidbody _filterBody;
        private MaterialPropertyBlock _properties;
        private Camera _viewCamera;
        private LessonStage _stage;
        private bool _initialized;
        private bool _paddingEnabled;
        private bool _filterWasGrabbed;
        private int _expectedResult;
        private int _currentX = 1;
        private int _currentY = 1;
        private int _outputWidth = 4;
        private int _outputMaximum;
        private string _hint;

        public bool ReadyToCollect => _initialized && _stage == LessonStage.Complete;
        public bool CanAnswer => _initialized && _stage == LessonStage.AwaitAnswer;
        public bool CanPad => _initialized && _stage == LessonStage.AwaitPadding;
        public int[] AnswerOptions => _answerOptions;
        public string Hint => _hint;
        public event Action Changed;

        private enum LessonStage
        {
            AwaitFilterMove,
            AwaitAnswer,
            ExplainAnswer,
            ScanValid,
            AwaitPadding,
            ScanPadded,
            Complete
        }

        public void Initialize(Camera viewCamera)
        {
            if (_initialized)
            {
                return;
            }
            if (_inputRoot == null || _filterRoot == null || _filterGrab == null ||
                _outputRoot == null || _inputCells == null || _inputCells.Length != 108 ||
                _filterCells == null || _filterCells.Length != 27 ||
                _outputCells == null || _outputCells.Length != 36 ||
                _paddingRoot == null || _paddingCells == null || _paddingCells.Length != 84)
            {
                Debug.LogError("[KdyLearning] Scene 6 needs RGB input 108 cells, filter 27 cells, output 36 cells and padding 84 cells.", this);
                return;
            }
            _filterBody = _filterRoot.GetComponent<Rigidbody>();
            _viewCamera = viewCamera;
            _properties = new MaterialPropertyBlock();
            ReadInputSamples();
            InitializeFilterValues();
            _filterGrab.GrabbedChanged += HandleFilterGrabbed;
            _initialized = true;
            Replay();
        }

        public void SubmitAnswer(int answer)
        {
            if (!CanAnswer)
            {
                return;
            }
            if (answer != _expectedResult)
            {
                _stage = LessonStage.ExplainAnswer;
                SetMessage("괜찮아요. 27개의 곱셈을 천천히 따라가 봅시다.",
                    "같은 위치의 픽셀 값 × 필터 값을 모두 더하세요. R·G·B의 합도 함께 더합니다.");
                StartCoroutine(ExplainContributions());
                return;
            }
            _stage = LessonStage.ScanValid;
            SetMessage("정답! 세 채널의 합이 출력 한 칸이 됩니다.",
                "필터가 같은 규칙으로 이동하며 4 × 4 feature map을 만드는 모습을 살펴보세요.");
            StartCoroutine(ScanWithoutPadding());
        }

        public void BeginPadding()
        {
            if (!CanPad)
            {
                return;
            }
            _paddingEnabled = true;
            _stage = LessonStage.ScanPadded;
            _paddingRoot?.SetActive(true);
            PrepareOutput(6);
            RestoreInputColors();
            SetMessage("입력 둘레에 0을 한 겹 채웠습니다. 이것이 zero padding입니다.",
                "테두리 밖의 값은 0입니다. 같은 3 × 3 × 3 필터로 출력 6 × 6을 만듭니다.");
            StartCoroutine(ScanWithPadding());
        }

        public void Replay()
        {
            if (!_initialized)
            {
                return;
            }
            StopAllCoroutines();
            _filterGrab.SetInteractionEnabled(false);
            _paddingEnabled = false;
            _paddingRoot?.SetActive(false);
            _filterWasGrabbed = false;
            _stage = LessonStage.AwaitFilterMove;
            _currentX = 1;
            _currentY = 1;
            PrepareOutput(4);
            SnapFilter(1, 1);
            HighlightWindow(1, 1);
            if (_snapTarget != null)
            {
                _snapTarget.gameObject.SetActive(true);
                _snapTarget.SetPositionAndRotation(InputPosition(2, 1), _inputRoot.rotation);
            }
            if (_resultToken != null)
            {
                _resultToken.gameObject.SetActive(false);
            }
            ShowChannelSummary(1, 1, true);
            _filterGrab.SetInteractionEnabled(true);
            SetMessage("필터 손잡이를 잡아 오른쪽의 빛나는 칸으로 한 칸 옮겨 놓으세요.",
                "오른손 Grip으로 필터를 잡고 이동한 뒤 놓으세요. 노트북에서는 G를 누르고 손 방향을 움직입니다.");
        }

        private void ReadInputSamples()
        {
            bool canReadSource = _source != null && _source.isReadable;
            if (_source != null && !canReadSource)
            {
                Debug.LogWarning("[KdyLearning] Scene 6 source texture must enable Read/Write. Using the instructional fallback pattern.", this);
            }
            for (int y = 0; y < InputSize; y++)
            {
                for (int x = 0; x < InputSize; x++)
                {
                    Color source = canReadSource
                        ? _source.GetPixelBilinear((x + .5f) / InputSize, 1f - (y + .5f) / InputSize)
                        : new Color(x / 5f, y / 5f, (x + y) / 10f);
                    for (int channel = 0; channel < ChannelCount; channel++)
                    {
                        int index = InputIndex(channel, x, y);
                        _inputSamples[index] = Mathf.Clamp(Mathf.RoundToInt(source[channel] * 9f), 0, 9);
                        if (_inputValues != null && index < _inputValues.Length && _inputValues[index] != null)
                        {
                            _inputValues[index].text = _inputSamples[index].ToString();
                        }
                    }
                }
            }
            RestoreInputColors();
        }

        private void InitializeFilterValues()
        {
            for (int index = 0; index < _weights.Length; index++)
            {
                if (_filterValues != null && index < _filterValues.Length && _filterValues[index] != null)
                {
                    _filterValues[index].text = _weights[index].ToString();
                }
                SetColor(_filterCells[index], WeightColor(_weights[index]), .3f);
            }
        }

        private void HandleFilterGrabbed(bool isGrabbed)
        {
            if (!_initialized || _stage != LessonStage.AwaitFilterMove)
            {
                return;
            }
            if (isGrabbed)
            {
                _filterWasGrabbed = true;
                SetMessage("좋아요. 오른쪽 한 칸의 빛나는 틀에 맞춘 뒤 Grip을 놓으세요.",
                    "필터는 R·G·B 전체 깊이를 한 번에 덮습니다. 여기서는 stride 1, 즉 한 칸씩 이동합니다.");
                return;
            }
            if (!_filterWasGrabbed)
            {
                return;
            }
            _filterWasGrabbed = false;
            Vector3 released = _inputRoot.InverseTransformPoint(_filterRoot.position);
            Vector3 target = _inputRoot.InverseTransformPoint(InputPosition(2, 1));
            float planarDistance = new Vector2(released.x - target.x, released.y - target.y).magnitude;
            if (planarDistance > _snapRadius || Mathf.Abs(released.z - target.z) > .6f)
            {
                SnapFilter(1, 1);
                HighlightWindow(1, 1);
                SetMessage("필터를 시작 위치로 돌려놓았어요. 빛나는 틀 근처에 놓으면 자동으로 맞춰집니다.",
                    "필터의 중앙을 오른쪽 목표 틀로 옮기세요. 화면을 걷거나 고개를 돌려 옆에서도 확인할 수 있습니다.");
                return;
            }
            SnapFilter(2, 1);
            _filterGrab.SetInteractionEnabled(false);
            _snapTarget?.gameObject.SetActive(false);
            HighlightWindow(2, 1);
            _expectedResult = Convolve(2, 1);
            _answerOptions[0] = _expectedResult - 3;
            _answerOptions[1] = _expectedResult;
            _answerOptions[2] = _expectedResult + 3;
            _stage = LessonStage.AwaitAnswer;
            ShowChannelSummary(2, 1);
            SetMessage("이 위치에서 출력될 값은 얼마일까요? 아래 세 값 중 하나를 선택하세요.",
                "채널마다 9개의 곱셈을 더한 뒤 R 합 + G 합 + B 합을 계산하세요. bias는 0입니다.");
        }

        private IEnumerator ExplainContributions()
        {
            int grandTotal = 0;
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                int channelTotal = 0;
                for (int ky = 0; ky < KernelSize; ky++)
                {
                    for (int kx = 0; kx < KernelSize; kx++)
                    {
                        int weightIndex = channel * 9 + ky * 3 + kx;
                        int inputX = _currentX + kx - 1;
                        int inputY = _currentY + ky - 1;
                        int value = GetInput(channel, inputX, inputY);
                        int weight = _weights[weightIndex];
                        int contribution = value * weight;
                        channelTotal += contribution;
                        HighlightWindow(_currentX, _currentY);
                        SetColor(_filterCells[weightIndex], Color.white, 2f);
                        HighlightContribution(channel, inputX, inputY);
                        SetCalculation($"{ChannelName(channel)}: {value} × ({weight}) = {contribution}\n채널 누적 {channelTotal} / 앞선 채널 합 {grandTotal}");
                        yield return new WaitForSeconds(.15f);
                    }
                }
                grandTotal += channelTotal;
                SetCalculation($"{ChannelName(channel)} 채널 합 = {channelTotal}\n지금까지 R·G·B 합 = {grandTotal}");
                yield return new WaitForSeconds(.35f);
            }
            HighlightWindow(_currentX, _currentY);
            ShowChannelSummary(_currentX, _currentY);
            _stage = LessonStage.AwaitAnswer;
            SetMessage("27개의 곱셈을 모두 확인했어요. 다시 결과를 선택해 보세요.",
                $"세 채널의 합은 {grandTotal}입니다. 0을 곱한 칸도 포함해서 더하며, bias와 활성화 함수는 아직 적용하지 않습니다.");
        }

        private IEnumerator ScanWithoutPadding()
        {
            ShowChannelSummary(2, 1, true);
            yield return TransferResult(1, 0, _expectedResult);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    if (x == 1 && y == 0)
                    {
                        continue;
                    }
                    yield return MoveFilter(x + 1, y + 1);
                    HighlightWindow(x + 1, y + 1);
                    ShowChannelSummary(x + 1, y + 1, true);
                    yield return TransferResult(x, y, Convolve(x + 1, y + 1));
                }
            }
            RestoreInputColors();
            InitializeFilterValues();
            _stage = LessonStage.AwaitPadding;
            SetCalculation("입력 6 × 6 × 3 → 필터 3 × 3 × 3 한 개 → 출력 4 × 4 한 장\nstride 1 / padding 0 / bias 0 / 활성화 적용 전");
            SetMessage("테두리 밖으로 필터를 놓을 수 없어 출력이 작아졌습니다. '0 패딩 추가'를 선택하세요.",
                "출력 크기: (6 − 3) ÷ 1 + 1 = 4. 입력 가장자리도 처리하려면 둘레에 0을 한 겹 더합니다.");
        }

        private IEnumerator ScanWithPadding()
        {
            yield return new WaitForSeconds(.65f);
            for (int y = 0; y < InputSize; y++)
            {
                for (int x = 0; x < InputSize; x++)
                {
                    yield return MoveFilter(x, y);
                    HighlightWindow(x, y);
                    ShowChannelSummary(x, y, true);
                    yield return TransferResult(x, y, Convolve(x, y));
                }
            }
            RestoreInputColors();
            InitializeFilterValues();
            _stage = LessonStage.Complete;
            SetCalculation("입력 6 × 6 × 3 + zero padding 1 → 출력 6 × 6 한 장\n출력 크기: (6 + 2 × 1 − 3) ÷ 1 + 1 = 6 / stride 1");
            SetMessage("완료! 세 색 채널에 걸친 필터 하나가 feature map 한 장을 만들었습니다. 두 번째 양피지를 수집하세요.",
                "패딩은 빈 테두리를 0으로 채웁니다. 이 예제에서는 출력 크기를 6 × 6으로 유지하며 새 이미지 정보를 추가하지 않습니다.");
        }

        private IEnumerator MoveFilter(int centerX, int centerY)
        {
            Vector3 start = _filterRoot.position;
            Vector3 end = InputPosition(centerX, centerY);
            Quaternion startRotation = _filterRoot.rotation;
            float elapsed = 0f;
            const float duration = .16f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                SetFilterPose(Vector3.Lerp(start, end, progress),
                    Quaternion.Slerp(startRotation, _inputRoot.rotation, progress));
                yield return null;
            }
            SnapFilter(centerX, centerY);
        }

        private IEnumerator TransferResult(int outputX, int outputY, int value)
        {
            int outputIndex = outputY * _outputWidth + outputX;
            Renderer cell = _outputCells[outputIndex];
            if (_resultToken != null)
            {
                Vector3 start = _filterRoot.position + _inputRoot.forward * .35f;
                Vector3 end = cell.transform.position;
                _resultToken.gameObject.SetActive(true);
                if (_viewCamera != null)
                {
                    _resultToken.rotation = _viewCamera.transform.rotation;
                }
                float elapsed = 0f;
                const float duration = .24f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / duration);
                    _resultToken.position = Vector3.Lerp(start, end, progress) + Vector3.up * Mathf.Sin(progress * Mathf.PI) * .15f;
                    yield return null;
                }
                _resultToken.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(.24f);
            }
            float brightness = .3f + .7f * Mathf.Abs(value) / Mathf.Max(1, _outputMaximum);
            Color resultColor = value < 0 ? new Color(1f, .38f, .15f) : new Color(.15f, .85f, 1f);
            SetColor(cell, resultColor * brightness, .8f);
            if (_outputValues != null && outputIndex < _outputValues.Length && _outputValues[outputIndex] != null)
            {
                _outputValues[outputIndex].text = value.ToString();
            }
        }

        private void PrepareOutput(int width)
        {
            _outputWidth = width;
            _outputMaximum = 1;
            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int centerX = _paddingEnabled ? x : x + 1;
                    int centerY = _paddingEnabled ? y : y + 1;
                    _outputMaximum = Mathf.Max(_outputMaximum, Mathf.Abs(Convolve(centerX, centerY)));
                }
            }
            float halfWidth = (width - 1) * CellPitch * .5f;
            for (int index = 0; index < _outputCells.Length; index++)
            {
                bool isVisible = index < width * width;
                Renderer cell = _outputCells[index];
                cell.gameObject.SetActive(isVisible);
                if (isVisible)
                {
                    int x = index % width;
                    int y = index / width;
                    cell.transform.position = _outputRoot.TransformPoint(new Vector3(-halfWidth + x * CellPitch, halfWidth - y * CellPitch, 0f));
                    SetColor(cell, new Color(.08f, .15f, .18f), 0f);
                }
                if (_outputValues != null && index < _outputValues.Length && _outputValues[index] != null)
                {
                    Text label = _outputValues[index];
                    label.gameObject.SetActive(isVisible);
                    label.text = "·";
                    if (isVisible)
                    {
                        label.transform.position = cell.transform.position - _outputRoot.forward * .065f;
                    }
                }
            }
        }

        private int Convolve(int centerX, int centerY)
        {
            int total = 0;
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                for (int ky = 0; ky < KernelSize; ky++)
                {
                    for (int kx = 0; kx < KernelSize; kx++)
                    {
                        total += GetInput(channel, centerX + kx - 1, centerY + ky - 1) * _weights[channel * 9 + ky * 3 + kx];
                    }
                }
            }
            return total;
        }

        private void ShowChannelSummary(int centerX, int centerY, bool showResult = false)
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                int sum = 0;
                for (int ky = 0; ky < KernelSize; ky++)
                {
                    for (int kx = 0; kx < KernelSize; kx++)
                    {
                        sum += GetInput(channel, centerX + kx - 1, centerY + ky - 1) * _weights[channel * 9 + ky * 3 + kx];
                    }
                }
                _channelSums[channel] = sum;
            }
            int total = _channelSums[0] + _channelSums[1] + _channelSums[2];
            string result = showResult ? total.ToString() : "?";
            SetCalculation($"R 합 {_channelSums[0]} + G 합 {_channelSums[1]} + B 합 {_channelSums[2]} = {result}\n읽기 쉽게 RGB를 0~9로 양자화한 교육용 값 / bias 0 / 활성화 적용 전");
        }

        private void HighlightWindow(int centerX, int centerY)
        {
            RestoreInputColors();
            InitializeFilterValues();
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                for (int ky = 0; ky < KernelSize; ky++)
                {
                    for (int kx = 0; kx < KernelSize; kx++)
                    {
                        HighlightContribution(channel, centerX + kx - 1, centerY + ky - 1);
                    }
                }
            }
        }

        private void HighlightContribution(int channel, int x, int y)
        {
            if (x >= 0 && x < InputSize && y >= 0 && y < InputSize)
            {
                int index = InputIndex(channel, x, y);
                SetColor(_inputCells[index], Color.Lerp(SampleColor(channel, _inputSamples[index]), Color.white, .5f), 1.1f);
                return;
            }
            if (!_paddingEnabled || _paddingCells == null)
            {
                return;
            }
            int paddingIndex = 0;
            for (int py = -1; py <= InputSize; py++)
            {
                for (int px = -1; px <= InputSize; px++)
                {
                    if (px >= 0 && px < InputSize && py >= 0 && py < InputSize)
                    {
                        continue;
                    }
                    if (px == x && py == y)
                    {
                        int index = channel * 28 + paddingIndex;
                        if (index < _paddingCells.Length)
                        {
                            SetColor(_paddingCells[index], new Color(.78f, .8f, .9f), .8f);
                        }
                        return;
                    }
                    paddingIndex++;
                }
            }
        }

        private void RestoreInputColors()
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                for (int y = 0; y < InputSize; y++)
                {
                    for (int x = 0; x < InputSize; x++)
                    {
                        int index = InputIndex(channel, x, y);
                        SetColor(_inputCells[index], SampleColor(channel, _inputSamples[index]), .1f);
                    }
                }
            }
            if (_paddingCells != null)
            {
                foreach (Renderer cell in _paddingCells)
                {
                    SetColor(cell, new Color(.12f, .14f, .2f), .05f);
                }
            }
        }

        private int GetInput(int channel, int x, int y)
        {
            if (x < 0 || x >= InputSize || y < 0 || y >= InputSize)
            {
                return 0;
            }
            return _inputSamples[InputIndex(channel, x, y)];
        }

        private int InputIndex(int channel, int x, int y)
        {
            return channel * InputSize * InputSize + y * InputSize + x;
        }

        private Vector3 InputPosition(int x, int y)
        {
            return _inputRoot.TransformPoint(new Vector3(-InputHalfWidth + x * CellPitch, InputHalfWidth - y * CellPitch, 0f));
        }

        private void SnapFilter(int centerX, int centerY)
        {
            _currentX = centerX;
            _currentY = centerY;
            SetFilterPose(InputPosition(centerX, centerY), _inputRoot.rotation);
        }

        private void SetFilterPose(Vector3 position, Quaternion rotation)
        {
            _filterRoot.SetPositionAndRotation(position, rotation);
            if (_filterBody != null)
            {
                _filterBody.position = position;
                _filterBody.rotation = rotation;
            }
        }

        private Color SampleColor(int channel, int value)
        {
            float brightness = .12f + value / 9f * .78f;
            Color color = new Color(.035f, .035f, .035f);
            color[channel] = brightness;
            color.a = 1f;
            return color;
        }

        private Color WeightColor(int weight)
        {
            if (weight < 0)
            {
                return new Color(1f, .4f, .15f);
            }
            if (weight > 0)
            {
                return new Color(.2f, .9f, 1f);
            }
            return new Color(.1f, .16f, .2f);
        }

        private string ChannelName(int channel)
        {
            return channel == 0 ? "R" : channel == 1 ? "G" : "B";
        }

        private void SetColor(Renderer target, Color color, float emission)
        {
            if (target == null)
            {
                return;
            }
            _properties.Clear();
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            _properties.SetColor("_EmissionColor", color * emission);
            target.SetPropertyBlock(_properties);
        }

        private void SetCalculation(string value)
        {
            if (_calculation != null)
            {
                _calculation.text = value;
            }
        }

        private void SetMessage(string status, string hint)
        {
            if (_status != null)
            {
                _status.text = status;
            }
            _hint = hint;
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            if (_filterGrab != null)
            {
                _filterGrab.GrabbedChanged -= HandleFilterGrabbed;
            }
        }
    }
}
