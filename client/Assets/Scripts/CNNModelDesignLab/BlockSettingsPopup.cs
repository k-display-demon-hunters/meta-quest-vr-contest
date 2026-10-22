using System;
using ModelDesign;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModelDesignScene
{
    public sealed class BlockSettingsPopup : MonoBehaviour
    {
        private const int NumberRowCount = 3;
        private static readonly ActivationType[] Activations =
            (ActivationType[])Enum.GetValues(typeof(ActivationType));

        [Header("Popup")]
        [SerializeField] private Text _title;
        [SerializeField] private Text _summary;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _deleteButton;

        [Header("Settings")]
        [SerializeField] private NumberRow[] _numberRows = new NumberRow[NumberRowCount];
        [SerializeField] private GameObject _activationRoot;
        [SerializeField] private Text _activationValue;
        [SerializeField] private Button _previousActivation;
        [SerializeField] private Button _nextActivation;

        private readonly NumberSetting[] _numberSettings = new NumberSetting[NumberRowCount];
        private LayerDefinition _layer;
        private bool _isConfigured;

        public event Action OnDeleteRequested;

        private void Awake()
        {
            UI.SceneUIStyle.ApplyButtons(transform);
            _isConfigured = HasRequiredReferences();
            if (!_isConfigured)
            {
                Debug.LogError("[ModelDesignScene] Block settings popup references are incomplete.", this);
                return;
            }

            _closeButton.onClick.AddListener(Hide);
            _deleteButton.onClick.AddListener(HandleDeleteClicked);
            _previousActivation.onClick.AddListener(HandlePreviousActivationClicked);
            _nextActivation.onClick.AddListener(HandleNextActivationClicked);
            for (int index = 0; index < _numberRows.Length; index++)
            {
                _numberRows[index].Bind(AdjustNumber, index);
            }
        }

        public void Show(LayerDefinition layer, Vector3 position, Quaternion rotation)
        {
            if (layer == null)
            {
                Hide();
                return;
            }

            // The prefab starts inactive; activating it first also initializes its button listeners.
            gameObject.SetActive(true);
            if (!_isConfigured)
            {
                Hide();
                return;
            }

            ClearSelection();
            _layer = layer;
            _layer.OnChanged += Refresh;
            transform.SetPositionAndRotation(position, rotation);
            Refresh();
        }

        public void Hide()
        {
            ClearSelection();
            gameObject.SetActive(false);
        }

        private bool HasRequiredReferences()
        {
            if (_title == null || _summary == null || _closeButton == null ||
                _deleteButton == null ||
                _activationRoot == null || _activationValue == null ||
                _previousActivation == null || _nextActivation == null ||
                _numberRows == null || _numberRows.Length != NumberRowCount)
            {
                return false;
            }

            foreach (NumberRow row in _numberRows)
            {
                if (row == null || !row.IsConfigured)
                {
                    return false;
                }
            }

            return true;
        }

        private void Refresh()
        {
            if (_layer == null)
            {
                return;
            }

            _title.text = _layer.Type + " 설정";
            _deleteButton.interactable = _layer.Type != LayerType.Input && _layer.Type != LayerType.Output;
            int visibleNumberRowCount = 0;
            bool hasActivation = _layer.Type == LayerType.Conv2D || _layer.Type == LayerType.Dense;
            _activationRoot.SetActive(hasActivation);
            if (hasActivation)
            {
                _activationValue.text = _layer.Activation.ToString();
            }

            switch (_layer.Type)
            {
                case LayerType.Conv2D:
                    visibleNumberRowCount = 3;
                    _summary.text = "합성곱 필터로 특징을 추출합니다.";
                    ShowNumber(0, "필터 수", NumberSetting.Filters, _layer.Filters);
                    ShowNumber(1, "커널 높이", NumberSetting.KernelHeight, _layer.Height);
                    ShowNumber(2, "커널 너비", NumberSetting.KernelWidth, _layer.Width);
                    break;
                case LayerType.MaxPooling2D:
                    visibleNumberRowCount = 2;
                    _summary.text = "최댓값 풀링으로 특징 맵의 크기를 줄입니다.";
                    ShowNumber(0, "풀링 높이", NumberSetting.PoolHeight, _layer.Height);
                    ShowNumber(1, "풀링 너비", NumberSetting.PoolWidth, _layer.Width);
                    break;
                case LayerType.Dense:
                    visibleNumberRowCount = 1;
                    _summary.text = "연결된 뉴런으로 특징을 결합합니다.";
                    ShowNumber(0, "뉴런 수", NumberSetting.Units, _layer.Units);
                    break;
                case LayerType.Flatten:
                    _summary.text = "특징 맵을 일차원 벡터로 펼칩니다.\n변경할 설정이 없습니다.";
                    break;
                case LayerType.Input:
                    _summary.text = "모델 입력입니다.\n이 블록의 설정은 고정되어 있습니다.";
                    break;
                case LayerType.Output:
                    _summary.text = "10개 클래스 / Softmax.\n이 블록의 설정은 고정되어 있습니다.";
                    break;
            }

            for (int index = visibleNumberRowCount; index < _numberRows.Length; index++)
            {
                _numberRows[index].Hide();
            }
        }

        private void ShowNumber(int index, string label, NumberSetting setting, int value)
        {
            _numberSettings[index] = setting;
            _numberRows[index].Show(label, value);
        }

        private void AdjustNumber(int index, int direction)
        {
            if (_layer == null || !_numberRows[index].IsVisible)
            {
                return;
            }

            switch (_numberSettings[index])
            {
                case NumberSetting.Filters:
                    _layer.SetFilters(StepPositiveNumber(_layer.Filters, direction));
                    break;
                case NumberSetting.KernelHeight:
                    _layer.SetKernelSize(StepPositiveNumber(_layer.Height, direction), _layer.Width);
                    break;
                case NumberSetting.KernelWidth:
                    _layer.SetKernelSize(_layer.Height, StepPositiveNumber(_layer.Width, direction));
                    break;
                case NumberSetting.PoolHeight:
                    _layer.SetPoolSize(StepPositiveNumber(_layer.Height, direction), _layer.Width);
                    break;
                case NumberSetting.PoolWidth:
                    _layer.SetPoolSize(_layer.Height, StepPositiveNumber(_layer.Width, direction));
                    break;
                case NumberSetting.Units:
                    _layer.SetUnits(StepPositiveNumber(_layer.Units, direction));
                    break;
            }
        }

        private static int StepPositiveNumber(int value, int direction)
        {
            if (direction < 0)
            {
                return value > 1 ? value - 1 : 1;
            }

            return value < int.MaxValue ? value + 1 : int.MaxValue;
        }

        private void ChangeActivation(int direction)
        {
            if (_layer == null || (_layer.Type != LayerType.Conv2D && _layer.Type != LayerType.Dense))
            {
                return;
            }

            int currentIndex = Array.IndexOf(Activations, _layer.Activation);
            int nextIndex = (currentIndex + direction + Activations.Length) % Activations.Length;
            _layer.SetActivation(Activations[nextIndex]);
        }

        private void ClearSelection()
        {
            if (_layer != null)
            {
                _layer.OnChanged -= Refresh;
                _layer = null;
            }
        }

        private void HandleDeleteClicked()
        {
            if (_layer != null && _layer.Type != LayerType.Input && _layer.Type != LayerType.Output)
            {
                OnDeleteRequested?.Invoke();
            }
        }

        private void HandlePreviousActivationClicked()
        {
            ChangeActivation(-1);
        }

        private void HandleNextActivationClicked()
        {
            ChangeActivation(1);
        }

        private void OnDisable()
        {
            ClearSelection();
        }

        private void OnDestroy()
        {
            if (!_isConfigured)
            {
                return;
            }

            _closeButton.onClick.RemoveListener(Hide);
            _deleteButton.onClick.RemoveListener(HandleDeleteClicked);
            _previousActivation.onClick.RemoveListener(HandlePreviousActivationClicked);
            _nextActivation.onClick.RemoveListener(HandleNextActivationClicked);
            foreach (NumberRow row in _numberRows)
            {
                row.Unbind();
            }
        }

        private enum NumberSetting
        {
            Filters,
            KernelHeight,
            KernelWidth,
            PoolHeight,
            PoolWidth,
            Units
        }

        [Serializable]
        private sealed class NumberRow
        {
            [SerializeField] private GameObject _root;
            [SerializeField] private Text _label;
            [SerializeField] private Text _value;
            [SerializeField] private Button _decrease;
            [SerializeField] private Button _increase;

            private UnityAction _onDecrease;
            private UnityAction _onIncrease;

            public bool IsConfigured => _root != null && _label != null && _value != null &&
                _decrease != null && _increase != null;
            public bool IsVisible => _root.activeSelf;

            public void Bind(Action<int, int> adjustNumber, int index)
            {
                _onDecrease = () => adjustNumber(index, -1);
                _onIncrease = () => adjustNumber(index, 1);
                _decrease.onClick.AddListener(_onDecrease);
                _increase.onClick.AddListener(_onIncrease);
            }

            public void Unbind()
            {
                _decrease.onClick.RemoveListener(_onDecrease);
                _increase.onClick.RemoveListener(_onIncrease);
            }

            public void Show(string label, int value)
            {
                _label.text = label;
                _value.text = value.ToString();
                _decrease.interactable = value > 1;
                _increase.interactable = value < int.MaxValue;
                _root.SetActive(true);
            }

            public void Hide()
            {
                _root.SetActive(false);
            }
        }
    }
}
