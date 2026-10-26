using System;
using System.Collections.Generic;
using ModelDesignScene.Execution.Engine;

namespace ModelDesignScene.Editing
{
    public sealed class ModelEditingManager : IDisposable
    {
        private readonly ModelWorkspace _workspace = new ModelWorkspace();
        public event Action OnChanged
        {
            add => _workspace.OnChanged += value;
            remove => _workspace.OnChanged -= value;
        }

        public LayerDefinition Input => _workspace.Input;
        public LayerDefinition Output => _workspace.Output;

        public ModelEditingManager()
        {
            SetLayerOrder(Array.Empty<LayerDefinition>());
        }

        public LayerDefinition CreateLayer(LayerType type)
        {
            switch (type)
            {
                case LayerType.Conv2D: return LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU);
                case LayerType.MaxPooling2D: return LayerDefinition.MaxPooling2D(2, 2);
                case LayerType.Flatten: return LayerDefinition.Flatten();
                case LayerType.Dense: return LayerDefinition.Dense(32, ActivationType.ReLU);
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public void RegisterLayer(LayerDefinition layer)
        {
            _workspace.AddLayer(layer);
        }

        public void RemoveLayer(LayerDefinition layer)
        {
            _workspace.RemoveLayer(layer);
        }

        public void SetLayerOrder(IReadOnlyList<LayerDefinition> layers)
        {
            _workspace.SetLayerOrder(layers);
        }

        public bool TryBuild(CnnTrainingOptions options, out ModelExecutionDefinition definition,
            out IReadOnlyList<ValidationIssue> issues)
        {
            return ModelExecutionDefinitionBuilder.TryBuild(_workspace, options, out definition, out issues);
        }

        public bool Matches(ModelExecutionDefinition first, ModelExecutionDefinition second)
        {
            return ModelExecutionDefinitionBuilder.Matches(first, second);
        }

        public void SetFilters(LayerDefinition layer, int filters)
        {
            layer.SetFilters(filters);
        }

        public void SetKernelSize(LayerDefinition layer, int height, int width)
        {
            layer.SetKernelSize(height, width);
        }

        public void SetPoolSize(LayerDefinition layer, int height, int width)
        {
            layer.SetPoolSize(height, width);
        }

        public void SetUnits(LayerDefinition layer, int units)
        {
            layer.SetUnits(units);
        }

        public void SetActivation(LayerDefinition layer, ActivationType activation)
        {
            layer.SetActivation(activation);
        }

        public void Dispose()
        {
            _workspace.Dispose();
        }
    }
}
