using System;
using ModelDesignScene.Model;
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

        private LayerDefinition _layer;
        private BlockSettingsDefinition _definition;
        private bool _isConfigured;

        internal event Action OnDeleteRequested;
        internal event Action<LayerDefinition, LayerNumberSetting, int> OnNumberChangeRequested;
        internal event Action<LayerDefinition, ActivationType> OnActivationChangeRequested;

        private void Awake()
        {
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
            _definition = BlockSettingsCatalog.Get(layer.Type);
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

            _title.text = _definition.Title;
            _summary.text = _definition.Summary;
            _deleteButton.interactable = _layer.Type != LayerType.Input && _layer.Type != LayerType.Output;
            _activationRoot.SetActive(_definition.HasActivation);
            if (_definition.HasActivation)
            {
                _activationValue.text = _layer.Activation.ToString();
            }

            for (int index = 0; index < _definition.NumberFields.Count; index++)
            {
                ShowNumber(index, _definition.NumberFields[index]);
            }

            for (int index = _definition.NumberFields.Count; index < _numberRows.Length; index++)
            {
                _numberRows[index].Hide();
            }
        }

        private void ShowNumber(int index, BlockSettingsDefinition.NumberField field)
        {
            _numberRows[index].Show(field.Label, field.ReadValue(_layer), field.Maximum);
        }

        private void AdjustNumber(int index, int direction)
        {
            if (_layer == null || !_numberRows[index].IsVisible)
            {
                return;
            }

            BlockSettingsDefinition.NumberField field = _definition.NumberFields[index];
            int value = StepPositiveNumber(field.ReadValue(_layer), direction, field.Maximum);
            OnNumberChangeRequested?.Invoke(_layer, field.Setting, value);
        }

        private static int StepPositiveNumber(int value, int direction, int maximum)
        {
            if (direction < 0)
            {
                return value > LayerLimits.MinParameterValue ? value - 1 : LayerLimits.MinParameterValue;
            }

            return value < maximum ? value + 1 : maximum;
        }

        private void ChangeActivation(int direction)
        {
            if (_layer == null || !_definition.HasActivation)
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
                _definition = null;
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
                _decrease.interactable = value > LayerLimits.MinParameterValue;
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
