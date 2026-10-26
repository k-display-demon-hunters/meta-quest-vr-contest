using System;
using System.Collections.Generic;
using ModelDesignScene.Editing;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ModelDesignScene.Assembly
{
    public sealed class ModelAssemblyManager : MonoBehaviour
    {
        [SerializeField] private ModelAssemblyController _controller;

        private ModelEditingManager _editing;
        private Func<IXRActivateInteractor, Collider> _getActivationHit;

        public bool IsDragging => _controller.IsDragging;
        public IReadOnlyList<LayerDefinition> SlotLayers => _controller.GetSlotLayers();

        public event Action OnChanged;
        public event Action<LayerDefinition, Pose> OnSettingsRequested;

        public void Initialize(ModelEditingManager editing,
            Func<IXRActivateInteractor, Collider> getActivationHit)
        {
            _editing = editing;
            _getActivationHit = getActivationHit;
            _controller.Initialize(this, editing.Input, editing.Output);
        }

        public void BeginInteraction() => _controller.BeginInteraction();
        public void EndInteraction() => _controller.EndInteraction();
        public void DeleteLayer(LayerDefinition layer) => _controller.DeleteLayer(layer);
        public void RefreshLabels() => _controller.RefreshLabels();

        internal LayerDefinition CreateLayer(LayerType type) => _editing.CreateLayer(type);
        internal void RegisterLayer(LayerDefinition layer) => _editing.RegisterLayer(layer);
        internal void RemoveLayer(LayerDefinition layer) => _editing.RemoveLayer(layer);
        internal void ApplyOrder(IReadOnlyList<LayerDefinition> layers) => _editing.SetLayerOrder(layers);
        internal Collider GetActivationHit(IXRActivateInteractor interactor) => _getActivationHit(interactor);
        internal void NotifyChanged() => OnChanged?.Invoke();
        internal void RequestSettings(LayerDefinition layer, Pose anchorPose) => OnSettingsRequested?.Invoke(layer, anchorPose);
    }
}
