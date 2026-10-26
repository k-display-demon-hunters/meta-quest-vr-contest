using System;
using ModelDesignScene.Editing;
using UnityEngine;

namespace ModelDesignScene.BlockSettings
{
    public sealed class ModelSettingsController : MonoBehaviour
    {
        [SerializeField] private BlockSettingsPopup _popupPrefab;

        private BlockSettingsPopup _popup;
        private LayerDefinition _settingsLayer;

        internal event Action<LayerDefinition> OnDeleteRequested;
        internal event Action<LayerDefinition, LayerNumberSetting, int> OnNumberChangeRequested;
        internal event Action<LayerDefinition, ActivationType> OnActivationChangeRequested;

        private void Awake()
        {
            _popup = Instantiate(_popupPrefab, transform);
            _popup.Hide();
            _popup.OnDeleteRequested += HandleDeleteRequested;
            _popup.OnNumberChangeRequested += HandleNumberChangeRequested;
            _popup.OnActivationChangeRequested += HandleActivationChangeRequested;
        }

        internal void Open(LayerDefinition layer, Pose popupPose)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            _settingsLayer = layer;
            // The fixed slot supplies a pose value; this module keeps ownership of the popup.
            _popup.Show(layer, popupPose.position, popupPose.rotation);
        }

        internal void Close()
        {
            _settingsLayer = null;
            if (_popup != null)
            {
                _popup.Hide();
            }
        }

        internal void Refresh()
        {
            _popup.Refresh();
        }

        private void HandleDeleteRequested()
        {
            if (isActiveAndEnabled && _settingsLayer != null &&
                _settingsLayer.Type != LayerType.Input && _settingsLayer.Type != LayerType.Output)
            {
                OnDeleteRequested?.Invoke(_settingsLayer);
            }
        }

        private void HandleNumberChangeRequested(LayerDefinition layer, LayerNumberSetting setting, int value)
        {
            OnNumberChangeRequested?.Invoke(layer, setting, value);
        }

        private void HandleActivationChangeRequested(LayerDefinition layer, ActivationType activation)
        {
            OnActivationChangeRequested?.Invoke(layer, activation);
        }

        private void OnDisable()
        {
            Close();
        }

        private void OnDestroy()
        {
            if (_popup != null)
            {
                _popup.OnDeleteRequested -= HandleDeleteRequested;
                _popup.OnNumberChangeRequested -= HandleNumberChangeRequested;
                _popup.OnActivationChangeRequested -= HandleActivationChangeRequested;
                Destroy(_popup.gameObject);
            }
        }
    }
}
