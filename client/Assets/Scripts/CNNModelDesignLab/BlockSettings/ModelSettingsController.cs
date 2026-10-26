using System;
using ModelDesignScene.Model;
using UnityEngine;

namespace ModelDesignScene.BlockSettings
{
    public sealed class ModelSettingsController : MonoBehaviour
    {
        [SerializeField] private BlockSettingsPopup _popupPrefab;

        private BlockSettingsPopup _popup;
        private LayerDefinition _settingsLayer;

        internal event Action<LayerDefinition> OnDeleteRequested;

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

        private void ChangeNumber(LayerDefinition layer, LayerNumberSetting setting, int value)
        {
            switch (setting)
            {
                case LayerNumberSetting.Filters: layer.SetFilters(value); break;
                case LayerNumberSetting.KernelHeight: layer.SetKernelSize(value, layer.Width); break;
                case LayerNumberSetting.KernelWidth: layer.SetKernelSize(layer.Height, value); break;
                case LayerNumberSetting.PoolHeight: layer.SetPoolSize(value, layer.Width); break;
                case LayerNumberSetting.PoolWidth: layer.SetPoolSize(layer.Height, value); break;
                case LayerNumberSetting.Units: layer.SetUnits(value); break;
            }
        }

        private void ChangeActivation(LayerDefinition layer, ActivationType activation)
        {
            layer.SetActivation(activation);
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
            ChangeNumber(layer, setting, value);
            Refresh();
        }

        private void HandleActivationChangeRequested(LayerDefinition layer, ActivationType activation)
        {
            ChangeActivation(layer, activation);
            Refresh();
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
