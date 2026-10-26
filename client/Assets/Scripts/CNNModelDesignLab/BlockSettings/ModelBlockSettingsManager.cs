using System;
using ModelDesignScene.Model;
using UnityEngine;

namespace ModelDesignScene.BlockSettings
{
    public sealed class ModelBlockSettingsManager : MonoBehaviour
    {
        [SerializeField] private ModelSettingsController _controller;

        public event Action<LayerDefinition> OnDeleteRequested
        {
            add => _controller.OnDeleteRequested += value;
            remove => _controller.OnDeleteRequested -= value;
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
    }
}
