using System;
using ModelDesignScene.Editing;
using UnityEngine;

namespace ModelDesignScene.BlockSettings
{
    public sealed class ModelBlockSettingsManager : MonoBehaviour
    {
        [SerializeField] private ModelSettingsController _controller;

        private ModelEditingManager _editing;

        public event Action<LayerDefinition> OnDeleteRequested;

        private void Awake()
        {
            _controller.OnDeleteRequested += HandleDeleteRequested;
            _controller.OnNumberChangeRequested += HandleNumberChangeRequested;
            _controller.OnActivationChangeRequested += HandleActivationChangeRequested;
        }

        public void Initialize(ModelEditingManager editing)
        {
            _editing = editing;
        }

        public void Open(LayerDefinition layer, Pose popupPose)
        {
            _controller.Open(layer, popupPose);
        }

        public void Close()
        {
            _controller.Close();
        }

        public void Refresh()
        {
            _controller.Refresh();
        }

        private void HandleDeleteRequested(LayerDefinition layer)
        {
            OnDeleteRequested?.Invoke(layer);
        }

        private void HandleNumberChangeRequested(LayerDefinition layer, LayerNumberSetting setting, int value)
        {
            switch (setting)
            {
                case LayerNumberSetting.Filters:
                    _editing.SetFilters(layer, value);
                    break;
                case LayerNumberSetting.KernelHeight:
                    _editing.SetKernelSize(layer, value, layer.Width);
                    break;
                case LayerNumberSetting.KernelWidth:
                    _editing.SetKernelSize(layer, layer.Height, value);
                    break;
                case LayerNumberSetting.PoolHeight:
                    _editing.SetPoolSize(layer, value, layer.Width);
                    break;
                case LayerNumberSetting.PoolWidth:
                    _editing.SetPoolSize(layer, layer.Height, value);
                    break;
                case LayerNumberSetting.Units:
                    _editing.SetUnits(layer, value);
                    break;
            }
            _controller.Refresh();
        }

        private void HandleActivationChangeRequested(LayerDefinition layer, ActivationType activation)
        {
            _editing.SetActivation(layer, activation);
            _controller.Refresh();
        }

        private void OnDestroy()
        {
            _controller.OnDeleteRequested -= HandleDeleteRequested;
            _controller.OnNumberChangeRequested -= HandleNumberChangeRequested;
            _controller.OnActivationChangeRequested -= HandleActivationChangeRequested;
        }
    }
}
