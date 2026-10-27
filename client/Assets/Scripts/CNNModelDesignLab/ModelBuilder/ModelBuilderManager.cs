using System;
using ModelDesignScene.Model;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class ModelBuilderManager : MonoBehaviour
    {
        [SerializeField] private ModelBuilderController _controller;

        private Func<IXRActivateInteractor, Collider> _getActivationHit;

        public bool IsDragging => _controller.IsDragging;

        public event Action OnChanged;
        public event Action<LayerDefinition, Pose> OnSettingsRequested;

        public void Initialize(ModelDefinition model,
            Func<IXRActivateInteractor, Collider> getActivationHit)
        {
            _getActivationHit = getActivationHit;
            _controller.Initialize(this, model);
        }

        public void BeginInteraction() => _controller.BeginInteraction();
        public void EndInteraction() => _controller.EndInteraction();
        public void DeleteLayer(LayerDefinition layer) => _controller.DeleteLayer(layer);
        public void RefreshLabels() => _controller.RefreshLabels();

        internal Collider GetActivationHit(IXRActivateInteractor interactor) => _getActivationHit(interactor);
        internal void NotifyChanged() => OnChanged?.Invoke();
        internal void RequestSettings(LayerDefinition layer, Pose anchorPose) => OnSettingsRequested?.Invoke(layer, anchorPose);
    }
}
