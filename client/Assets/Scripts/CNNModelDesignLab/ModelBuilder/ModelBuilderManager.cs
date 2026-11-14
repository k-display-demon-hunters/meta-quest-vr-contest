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
        public void CancelEntryGuide() => _controller.CancelEntryGuide();
        internal void PlayHologramFlow((Vector3 Center, Vector2 Extents, bool Trainable)[] frames,
            bool training, float readyAt) => _controller.PlayHologramFlow(frames, training, readyAt);
        public void ShowEpochFlow(int epoch) => _controller.ShowEpochFlow(epoch);
        public void FadeModelFlow() => _controller.FadeModelFlow();
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
        internal string[] ReadOutputSizes() => _controller.ReadOutputSizes();
        internal (Vector3Int Grid, int Elements, Vector3 Origin)[] ReadHologramLayers() => _controller.ReadHologramLayers();

        internal void NotifyChanged() => OnChanged?.Invoke();
        internal void RequestSettings(LayerDefinition layer, Pose anchorPose) => OnSettingsRequested?.Invoke(layer, anchorPose);
    }
}
