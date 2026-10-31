using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CNNFeatureMaps
{
    public sealed class FilterScanController : MonoBehaviour
    {
        private const float CellSize = 0.13f;
        [SerializeField] private bool _redFilter;
        [SerializeField] private Texture2D _input;
        [SerializeField] private Renderer[] _rgbCells;
        [SerializeField] private TextMesh[] _rgbValues;
        [SerializeField] private Renderer[] _outputCells;
        [SerializeField] private TextMesh[] _outputValues;
        [SerializeField] private Transform _kernel;
        [SerializeField] private TextMesh[] _kernelValues;
        [SerializeField] private Transform _window;
        [SerializeField] private Transform _connection;
        [SerializeField] private Button _scanButton;
        [SerializeField] private Text _scanLabel;
        private MaterialPropertyBlock _properties;
        private Coroutine _scan;
        private Vector3 _kernelHome;
        private Color[] _resultColors;
        private bool[] _evaluated;

        public bool Completed { get; private set; }
        public bool IsScanning => _scan != null;
        public event Action OnStateChanged;

        private void Awake()
        {
            _properties = new MaterialPropertyBlock();
            _kernelHome = _kernel.localPosition;
            _resultColors = new Color[_outputCells.Length];
            _evaluated = new bool[_outputCells.Length];
        }

        private void OnEnable()
        {
            _scanButton.onClick.AddListener(StartScan);
        }

        private void Start()
        {
            for (int channel = 0; channel < 3; channel++)
            {
                for (int row = 0; row < 6; row++)
                {
                    for (int column = 0; column < 6; column++)
                    {
                        int index = channel * 36 + row * 6 + column;
                        float value = _input.GetPixel(column, 5 - row)[channel];
                        _rgbValues[index].text = Mathf.RoundToInt(value * 255f).ToString();
                    }
                }
            }
            ResetOutput();
            SetInputFocus(-1, -1);
            _window.gameObject.SetActive(true);
            _connection.gameObject.SetActive(false);
        }

        private void ResetOutput()
        {
            for (int i = 0; i < _outputCells.Length; i++)
            {
                _evaluated[i] = false;
                Paint(_outputCells[i], new Color(0.035f, 0.045f, 0.06f, 0.08f));
                _outputValues[i].text = "·";
                _outputValues[i].color = new Color(1f, 1f, 1f, 0.12f);
            }
        }

        private void StartScan()
        {
            if (IsScanning) { return; }
            Completed = false;
            _scanButton.interactable = false;
            _scanLabel.text = "계산 중";
            SetKernelLabelOpacity(0.3f);
            ResetOutput();
            _scan = StartCoroutine(Scan());
            OnStateChanged?.Invoke();
        }

        private IEnumerator Scan()
        {
            int columns = RgbFilterCalculation.OutputWidth(_redFilter);
            _window.gameObject.SetActive(true);
            _connection.gameObject.SetActive(true);
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    SetInputFocus(-1, -1);
                    Vector3 target = new Vector3((column - (columns - 1) * 0.5f) * CellSize,
                        (1.5f - row) * CellSize, -0.045f);
                    yield return MoveKernel(target, row == 0 && column == 0 ? 0.8f : 0.35f,
                        row * columns + column);
                    SetInputFocus(row, column);
                    float red = RgbFilterCalculation.ChannelSum(_input, _redFilter, 0, row, column);
                    float green = RgbFilterCalculation.ChannelSum(_input, _redFilter, 1, row, column);
                    float blue = RgbFilterCalculation.ChannelSum(_input, _redFilter, 2, row, column);
                    float sum = red + green + blue;
                    int index = row * columns + column;
                    // Two explicit visual encodings: red evidence vs edge magnitude.
                    // The signed convolution number below is never replaced by this display value.
                    float strength = _redFilter ? Mathf.Clamp01(sum * 1.5f)
                        : Mathf.Clamp01(Mathf.Abs(sum) * 4f);
                    _resultColors[index] = Color.Lerp(new Color(0.025f, 0.03f, 0.04f),
                        _redFilter ? new Color(1f, 0.08f, 0.03f) : Color.white, strength);
                    _evaluated[index] = true;
                    _outputValues[index].text = sum.ToString("0.00");
                    ShowOutputFocus(index, false);
                    yield return new WaitForSeconds(0.4f);
                }
            }
            SetInputFocus(-1, -1);
            ShowOutputFocus(-1, true);
            _connection.gameObject.SetActive(false);
            yield return MoveKernel(_kernelHome, 0.6f, -1);
            SetKernelLabelOpacity(0.9f);
            _scan = null;
            Completed = true;
            _scanButton.interactable = true;
            _scanLabel.text = _redFilter ? "필터 A 실행하기" : "필터 B 실행하기";
            OnStateChanged?.Invoke();
        }

        private IEnumerator MoveKernel(Vector3 target, float duration, int outputIndex)
        {
            Vector3 start = _kernel.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _kernel.localPosition = Vector3.Lerp(start, target,
                    Mathf.SmoothStep(0f, 1f, elapsed / duration));
                if (outputIndex >= 0) { PointConnection(outputIndex); }
                yield return null;
            }
        }

        private void SetKernelLabelOpacity(float alpha)
        {
            foreach (TextMesh label in _kernelValues)
            {
                label.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private void SetInputFocus(int selectedRow, int selectedColumn)
        {
            for (int channel = 0; channel < 3; channel++)
            {
                for (int row = 0; row < 6; row++)
                {
                    for (int column = 0; column < 6; column++)
                    {
                        int index = channel * 36 + row * 6 + column;
                        bool selected = selectedRow >= 0 && row >= selectedRow && row < selectedRow + 3
                            && column >= selectedColumn
                            && column < selectedColumn + RgbFilterCalculation.KernelWidth(_redFilter);
                        Color tint = new Color(0f, 0f, 0f, selected ? 0.85f : 0.12f);
                        tint[channel] = _input.GetPixel(column, 5 - row)[channel];
                        Paint(_rgbCells[index], tint);
                        _rgbValues[index].color = new Color(1f, 1f, 1f, selected ? 1f : 0.07f);
                    }
                }
            }
        }

        private void ShowOutputFocus(int current, bool showComparison)
        {
            for (int i = 0; i < _outputCells.Length; i++)
            {
                if (!_evaluated[i]) { continue; }
                bool emphasized = showComparison || i == current;
                Color color = _resultColors[i];
                color.a = emphasized ? 1f : 0.25f;
                Paint(_outputCells[i], color);
                Color ink = !_redFilter && color.grayscale > 0.5f ? Color.black : Color.white;
                ink.a = showComparison ? 0f : emphasized ? 1f : 0.08f;
                _outputValues[i].color = ink;
            }
        }

        private void PointConnection(int index)
        {
            Vector3 from = _window.position + transform.forward * 0.4f;
            Vector3 to = _outputCells[index].transform.position;
            _connection.position = (from + to) * 0.5f;
            _connection.rotation = Quaternion.LookRotation(to - from, transform.up);
            _connection.localScale = new Vector3(0.008f, 0.008f, Vector3.Distance(from, to));
        }

        private void Paint(Renderer target, Color color)
        {
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            target.SetPropertyBlock(_properties);
        }

        private void OnDisable()
        {
            _scanButton.onClick.RemoveListener(StartScan);
            if (_scan != null)
            {
                StopCoroutine(_scan);
                _scan = null;
                _scanButton.interactable = true;
                _scanLabel.text = _redFilter ? "필터 A 실행하기" : "필터 B 실행하기";
            }
        }
    }
}
