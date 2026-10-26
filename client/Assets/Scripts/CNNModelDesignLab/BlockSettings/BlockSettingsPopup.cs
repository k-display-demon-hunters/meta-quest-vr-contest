using System;
using ModelDesignScene.Editing;
using ModelDesignScene.Execution.Engine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModelDesignScene.BlockSettings
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

        private readonly LayerNumberSetting[] _numberSettings = new LayerNumberSetting[NumberRowCount];
        private LayerDefinition _layer;
        private bool _isConfigured;

        internal event Action OnDeleteRequested;
        internal event Action<LayerDefinition, LayerNumberSetting, int> OnNumberChangeRequested;
        internal event Action<LayerDefinition, ActivationType> OnActivationChangeRequested;

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

        internal void Show(LayerDefinition layer, Vector3 position, Quaternion rotation)
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
            transform.SetPositionAndRotation(position, rotation);
            Refresh();
        }

        internal void Hide()
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

        internal void Refresh()
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
                    ShowNumber(0, "필터 수", LayerNumberSetting.Filters, _layer.Filters);
                    ShowNumber(1, "커널 높이", LayerNumberSetting.KernelHeight, _layer.Height);
                    ShowNumber(2, "커널 너비", LayerNumberSetting.KernelWidth, _layer.Width);
                    break;
                case LayerType.MaxPooling2D:
                    visibleNumberRowCount = 2;
                    _summary.text = "최댓값 풀링으로 특징 맵의 크기를 줄입니다.";
                    ShowNumber(0, "풀링 높이", LayerNumberSetting.PoolHeight, _layer.Height);
                    ShowNumber(1, "풀링 너비", LayerNumberSetting.PoolWidth, _layer.Width);
                    break;
                case LayerType.Dense:
                    visibleNumberRowCount = 1;
                    _summary.text = "연결된 뉴런으로 특징을 결합합니다.";
                    ShowNumber(0, "뉴런 수", LayerNumberSetting.Units, _layer.Units);
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

        private void ShowNumber(int index, string label, LayerNumberSetting setting, int value)
        {
            _numberSettings[index] = setting;
            _numberRows[index].Show(label, value, GetMaximum(setting));
        }

        private void AdjustNumber(int index, int direction)
        {
            if (_layer == null || !_numberRows[index].IsVisible)
            {
                return;
            }

            switch (_numberSettings[index])
            {
                case LayerNumberSetting.Filters:
                    OnNumberChangeRequested?.Invoke(_layer, LayerNumberSetting.Filters, StepPositiveNumber(_layer.Filters, direction, ModelExecutionDefinition.MaxFilters));
                    break;
                case LayerNumberSetting.KernelHeight:
                    OnNumberChangeRequested?.Invoke(_layer, LayerNumberSetting.KernelHeight, StepPositiveNumber(_layer.Height, direction, 28));
                    break;
                case LayerNumberSetting.KernelWidth:
                    OnNumberChangeRequested?.Invoke(_layer, LayerNumberSetting.KernelWidth, StepPositiveNumber(_layer.Width, direction, 28));
                    break;
                case LayerNumberSetting.PoolHeight:
                    OnNumberChangeRequested?.Invoke(_layer, LayerNumberSetting.PoolHeight, StepPositiveNumber(_layer.Height, direction, 28));
                    break;
                case LayerNumberSetting.PoolWidth:
                    OnNumberChangeRequested?.Invoke(_layer, LayerNumberSetting.PoolWidth, StepPositiveNumber(_layer.Width, direction, 28));
                    break;
                case LayerNumberSetting.Units:
                    OnNumberChangeRequested?.Invoke(_layer, LayerNumberSetting.Units, StepPositiveNumber(_layer.Units, direction, ModelExecutionDefinition.MaxDenseUnits));
                    break;
            }
        }

        private static int GetMaximum(LayerNumberSetting setting)
        {
            switch (setting)
            {
                case LayerNumberSetting.Filters: return ModelExecutionDefinition.MaxFilters;
                case LayerNumberSetting.Units: return ModelExecutionDefinition.MaxDenseUnits;
                default: return 28;
            }
        }

        private static int StepPositiveNumber(int value, int direction, int maximum)
        {
            if (direction < 0)
            {
                return value > 1 ? value - 1 : 1;
            }

            return value < maximum ? value + 1 : maximum;
        }

        private void ChangeActivation(int direction)
        {
            if (_layer == null || (_layer.Type != LayerType.Conv2D && _layer.Type != LayerType.Dense))
            {
                return;
            }

            int currentIndex = Array.IndexOf(Activations, _layer.Activation);
            int nextIndex = (currentIndex + direction + Activations.Length) % Activations.Length;
            OnActivationChangeRequested?.Invoke(_layer, Activations[nextIndex]);
        }

        private void ClearSelection()
        {
            if (_layer != null)
            {
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

            internal void Show(string label, int value, int maximum)
            {
                _label.text = label;
                _value.text = value.ToString();
                _decrease.interactable = value > 1;
                _increase.interactable = value < maximum;
                _root.SetActive(true);
            }

            internal void Hide()
            {
                _root.SetActive(false);
            }
        }
    }
}
