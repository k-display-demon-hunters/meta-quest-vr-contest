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
        [SerializeField] private Button _disconnectButton;
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

        public event Action OnDisconnectRequested;
        public event Action OnDeleteRequested;

        private void Awake()
        {
            _isConfigured = HasRequiredReferences();
            if (!_isConfigured)
            {
                Debug.LogError("[ModelDesignScene] Block settings popup references are incomplete.", this);
                return;
            }

            _closeButton.onClick.AddListener(Hide);
            _disconnectButton.onClick.AddListener(HandleDisconnectClicked);
            _deleteButton.onClick.AddListener(HandleDeleteClicked);
            _previousActivation.onClick.AddListener(HandlePreviousActivationClicked);
            _nextActivation.onClick.AddListener(HandleNextActivationClicked);
            for (int index = 0; index < _numberRows.Length; index++)
            {
                _numberRows[index].Bind(AdjustNumber, index);
            }
        }

        public void Show(LayerDefinition layer, Vector3 position, Quaternion rotation, bool canDisconnect)
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
            _disconnectButton.interactable = canDisconnect;
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
                _disconnectButton == null || _deleteButton == null ||
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

            _title.text = _layer.Type + " settings";
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
                    _summary.text = "Extract features with convolution filters.";
                    ShowNumber(0, "Filters", NumberSetting.Filters, _layer.Filters);
                    ShowNumber(1, "Kernel height", NumberSetting.KernelHeight, _layer.Height);
                    ShowNumber(2, "Kernel width", NumberSetting.KernelWidth, _layer.Width);
                    break;
                case LayerType.MaxPooling2D:
                    visibleNumberRowCount = 2;
                    _summary.text = "Reduce feature map size with max pooling.";
                    ShowNumber(0, "Pool height", NumberSetting.PoolHeight, _layer.Height);
                    ShowNumber(1, "Pool width", NumberSetting.PoolWidth, _layer.Width);
                    break;
                case LayerType.Dense:
                    visibleNumberRowCount = 1;
                    _summary.text = "Combine features with connected neurons.";
                    ShowNumber(0, "Units", NumberSetting.Units, _layer.Units);
                    break;
                case LayerType.Flatten:
                    _summary.text = "Convert feature maps to a flat vector.\nNo adjustable settings.";
                    break;
                case LayerType.Input:
                    _summary.text = "Model input.\nThis block has fixed settings.";
                    break;
                case LayerType.Output:
                    _summary.text = "10 classes / Softmax.\nThis block has fixed settings.";
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

        private void HandleDisconnectClicked()
        {
            if (_layer != null && _disconnectButton.interactable)
            {
                OnDisconnectRequested?.Invoke();
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
            _disconnectButton.onClick.RemoveListener(HandleDisconnectClicked);
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
