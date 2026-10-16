using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyActivationLeverController : MonoBehaviour
    {
        [SerializeField] private Transform _lever;
        [SerializeField] private KdyLessonGrabController _leverGrab;
        [SerializeField] private Renderer[] _inputCells = new Renderer[24];
        [SerializeField] private Renderer[] _outputCells = new Renderer[24];
        [SerializeField] private Text[] _inputLabels = new Text[24];
        [SerializeField] private Text[] _outputLabels = new Text[24];
        [SerializeField] private LineRenderer _graph;
        [SerializeField] private Transform _graphCursor;
        [SerializeField] private Text _status;
        [SerializeField] private float _leftX = -.6f;
        [SerializeField] private float _rightX = .6f;
        [SerializeField] private float _graphWidth = 3.2f;
        [SerializeField] private float _graphHeight = 1.8f;

        private const int CellCount = 24;
        private const int GraphSegments = 80;
        private const float GraphInputExtent = 4f;
        private const float GraphOutputMaximum = 4f;
        private readonly float[] _samples =
        {
            -4f, -3f, -2f, -1f, 0f, 1f,
            -2.5f, -1.5f, -.5f, .5f, 1.5f, 2.5f,
            -3.5f, -.75f, .25f, .75f, 2f, 3f,
            -2.25f, -1.25f, 1.25f, 2.25f, 3.25f, 4f
        };
        private MaterialPropertyBlock _properties;
        private Rigidbody _leverBody;
        private Transform _leverParent;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private int _selectedCell;
        private bool _sigmoid;
        private bool _visitedRelu;
        private bool _visitedSigmoid;
        private bool _initialized;
        private bool _resetting;

        public event Action Changed;
        public bool ReadyToCollect => _initialized && _visitedRelu && _visitedSigmoid;

        public string Hint
        {
            get
            {
                if (!_visitedRelu)
                {
                    return "레버를 Grip으로 잡고 왼쪽 ReLU 쪽에 놓아 보세요. 음수 입력이 0으로 바뀌는지 확인하세요.";
                }
                if (!_visitedSigmoid)
                {
                    return "이번에는 레버를 오른쪽 Sigmoid 쪽으로 옮겨 놓으세요. 같은 입력이 0과 1 사이로 바뀝니다.";
                }
                return "두 함수의 출력 범위와 모양을 비교했어요. 어느 함수가 항상 더 좋다는 뜻은 아니에요. 양피지를 수집하세요.";
            }
        }

        public void Initialize(Camera viewCamera)
        {
            if (_initialized)
            {
                return;
            }
            if (!HasRequiredReferences())
            {
                Debug.LogError("[KdyLearning] Scene 9 needs a grab lever, 24 input/output cells and labels, a graph and status text.", this);
                return;
            }

            _leverParent = _lever.parent;
            _homePosition = _lever.localPosition;
            _homeRotation = _lever.localRotation;
            _leverBody = _lever.GetComponent<Rigidbody>();
            _properties = new MaterialPropertyBlock();
            _graph.useWorldSpace = false;
            _graph.positionCount = GraphSegments + 1;
            _graphWidth = Mathf.Max(.1f, _graphWidth);
            _graphHeight = Mathf.Max(.1f, _graphHeight);
            _rightX = Mathf.Max(_leftX + .1f, _rightX);
            _leverGrab.GrabbedChanged += HandleLeverGrabbed;
            _initialized = true;
            ResetLever();
        }

        public void ResetLever()
        {
            if (!_initialized)
            {
                return;
            }

            _resetting = true;
            _leverGrab.SetInteractionEnabled(false);
            _visitedRelu = false;
            _visitedSigmoid = false;
            _sigmoid = false;
            _selectedCell = 0;
            MoveLever(_leftX);
            _leverGrab.SetInteractionEnabled(true);
            _resetting = false;
            RefreshDisplay();
        }

        private void LateUpdate()
        {
            if (!_initialized || !_leverGrab.IsGrabbed)
            {
                return;
            }

            float x = Mathf.Clamp(LeverRailPosition().x, _leftX, _rightX);
            MoveLever(x);
            bool sigmoid = x >= (_leftX + _rightX) * .5f;
            if (_sigmoid != sigmoid)
            {
                _sigmoid = sigmoid;
                RefreshDisplay();
            }
        }

        private void HandleLeverGrabbed(bool grabbed)
        {
            if (!_initialized || _resetting || grabbed || !isActiveAndEnabled)
            {
                return;
            }

            _sigmoid = LeverRailPosition().x >= (_leftX + _rightX) * .5f;
            MoveLever(_sigmoid ? _rightX : _leftX);
            if (_sigmoid)
            {
                _visitedSigmoid = true;
            }
            else
            {
                _visitedRelu = true;
            }
            RefreshDisplay();
        }

        private Vector3 LeverRailPosition()
        {
            return _leverParent != null ? _leverParent.InverseTransformPoint(_lever.position) : _lever.position;
        }

        private void MoveLever(float x)
        {
            // XRI temporarily unparents a grabbed object, so constrain it in the saved rail's coordinates.
            Vector3 railPosition = new Vector3(x, _homePosition.y, _homePosition.z);
            Vector3 worldPosition = _leverParent != null ? _leverParent.TransformPoint(railPosition) : railPosition;
            Quaternion worldRotation = _leverParent != null ? _leverParent.rotation * _homeRotation : _homeRotation;
            _lever.SetPositionAndRotation(worldPosition, worldRotation);
            if (_leverBody != null)
            {
                _leverBody.position = _lever.position;
                _leverBody.rotation = _lever.rotation;
            }
        }

        public void SelectCell(RaycastHit hit)
        {
            if (!_initialized || hit.collider == null)
            {
                return;
            }

            for (int i = 0; i < CellCount; i++)
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == _inputCells[i].transform || hitTransform.IsChildOf(_inputCells[i].transform) ||
                    hitTransform == _outputCells[i].transform || hitTransform.IsChildOf(_outputCells[i].transform))
                {
                    _selectedCell = i;
                    RefreshDisplay();
                    return;
                }
            }
        }

        private void RefreshDisplay()
        {
            for (int i = 0; i < CellCount; i++)
            {
                float input = _samples[i];
                float output = Evaluate(input);
                Color signedColor = input < 0f ? new Color(1f, .38f, .14f) : new Color(.12f, .8f, 1f);
                Color inputColor = Color.Lerp(new Color(.055f, .075f, .1f), signedColor, Mathf.Abs(input) / GraphInputExtent);
                // Both functions use the same brightness range, matching the common graph axes.
                Color outputColor = Color.Lerp(new Color(.035f, .055f, .075f), new Color(.3f, 1f, .72f), output / GraphOutputMaximum);
                SetCellColor(_inputCells[i], inputColor, i == _selectedCell);
                SetCellColor(_outputCells[i], outputColor, i == _selectedCell);
                _inputLabels[i].text = Format(input, "0.##");
                _outputLabels[i].text = Format(output, _sigmoid ? "0.000" : "0.##");
            }

            Color graphColor = _sigmoid ? new Color(.8f, .5f, 1f) : new Color(.2f, .95f, 1f);
            _graph.startColor = graphColor;
            _graph.endColor = graphColor;
            _properties.Clear();
            _properties.SetColor("_BaseColor", graphColor);
            _properties.SetColor("_Color", graphColor);
            _graph.SetPropertyBlock(_properties);
            for (int i = 0; i <= GraphSegments; i++)
            {
                float x = Mathf.Lerp(-GraphInputExtent, GraphInputExtent, i / (float)GraphSegments);
                _graph.SetPosition(i, GraphPoint(x, Evaluate(x)));
            }

            float selectedInput = _samples[_selectedCell];
            float selectedOutput = Evaluate(selectedInput);
            if (_graphCursor != null)
            {
                _graphCursor.position = _graph.transform.TransformPoint(GraphPoint(selectedInput, selectedOutput) + Vector3.back * .025f);
            }
            string xText = Format(selectedInput, "0.##");
            string resultText = Format(selectedOutput, "0.000000");
            string formula = _sigmoid
                ? $"Sigmoid(x) = 1 / (1 + exp(-x))\n1 / (1 + exp(-({xText}))) = {resultText}"
                : $"ReLU(x) = max(0, x)\nmax(0, {xText}) = {resultText}";
            string observation = _sigmoid
                ? "음수와 양수를 모두 0~1 사이로 바꿉니다."
                : "음수는 0으로, 양수는 입력 그대로 내보냅니다.";
            _status.text = formula + "\n" + observation + "\n그래프 공통 축: x -4~4 / y 0~4 · 숫자는 반올림\n"
                + $"ReLU {(_visitedRelu ? "✓" : "○")}   Sigmoid {(_visitedSigmoid ? "✓" : "○")}";
            Changed?.Invoke();
        }

        private float Evaluate(float input)
        {
            return _sigmoid ? 1f / (1f + Mathf.Exp(-input)) : Mathf.Max(0f, input);
        }

        private Vector3 GraphPoint(float input, float output)
        {
            return new Vector3(input / (2f * GraphInputExtent) * _graphWidth,
                (output / GraphOutputMaximum - .5f) * _graphHeight, 0f);
        }

        private void SetCellColor(Renderer target, Color color, bool selected)
        {
            if (selected)
            {
                color = Color.Lerp(color, new Color(1f, .88f, .35f), .5f);
            }
            _properties.Clear();
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            _properties.SetColor("_EmissionColor", selected ? color * .7f : Color.black);
            target.SetPropertyBlock(_properties);
        }

        private string Format(float value, string pattern)
        {
            return value.ToString(pattern, CultureInfo.InvariantCulture);
        }

        private bool HasRequiredReferences()
        {
            if (_lever == null || _leverGrab == null || _graph == null || _status == null ||
                _inputCells == null || _inputCells.Length != CellCount ||
                _outputCells == null || _outputCells.Length != CellCount ||
                _inputLabels == null || _inputLabels.Length != CellCount ||
                _outputLabels == null || _outputLabels.Length != CellCount)
            {
                return false;
            }
            for (int i = 0; i < CellCount; i++)
            {
                if (_inputCells[i] == null || _outputCells[i] == null || _inputLabels[i] == null || _outputLabels[i] == null)
                {
                    return false;
                }
            }
            return true;
        }

        private void OnDestroy()
        {
            if (_leverGrab != null)
            {
                _leverGrab.GrabbedChanged -= HandleLeverGrabbed;
            }
        }
    }
}
