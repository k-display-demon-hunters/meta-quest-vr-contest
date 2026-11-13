using System;
using ModelDesignScene.Model;
using UnityEngine;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class ModelBuilderManager : MonoBehaviour
    {
        [SerializeField] private ModelBuilderController _controller;

        public bool IsDragging => _controller.IsDragging;

        public bool CanPlayPlacementExample => _controller.CanPlayPlacementExample;
        public void PlayPlacementExample() => _controller.PlayPlacementExample();
        public void CancelPlacementExample() => _controller.CancelPlacementExample();
        public void PlayTrainingFlow() => _controller.PlayTrainingFlow();
        public void FinishTrainingFlow() => _controller.FinishTrainingFlow();
        public void CancelTrainingFlow() => _controller.CancelTrainingFlow();

        public event Action OnChanged;
        public event Action<LayerDefinition, Pose> OnSettingsRequested;

        public void Initialize(ModelDefinition model)
        {
            _controller.Initialize(this, model);
        }

        public void BeginInteraction() => _controller.BeginInteraction();
        public void EndInteraction() => _controller.EndInteraction();
        public void DeleteLayer(LayerDefinition layer) => _controller.DeleteLayer(layer);
        public void RefreshLabels() => _controller.RefreshLabels();

        internal void NotifyChanged() => OnChanged?.Invoke();
        internal void RequestSettings(LayerDefinition layer, Pose anchorPose) => OnSettingsRequested?.Invoke(layer, anchorPose);
    }
}
