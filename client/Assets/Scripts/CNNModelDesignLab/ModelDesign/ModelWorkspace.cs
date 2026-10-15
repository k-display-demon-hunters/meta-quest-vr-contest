using System;
using System.Collections.Generic;

namespace ModelDesign
{
    public sealed class ModelWorkspace : IDisposable
    {
        private readonly List<ModelDefinition> _models = new List<ModelDefinition>();
        private bool _isDisposed;

        public event Action OnChanged;
        public event Action<IReadOnlyList<ValidationIssue>> OnValidationChanged;
        public IReadOnlyList<ModelDefinition> Models { get; }
        public IReadOnlyList<ValidationIssue> ValidationIssues { get; private set; }
        public LayerDefinition Input { get; }
        public LayerDefinition Output { get; }
        public ModelDefinition InputModel => GetModel(Input);

        public ModelWorkspace()
        {
            Models = _models.AsReadOnly();
            Input = LayerDefinition.Input();
            Output = LayerDefinition.Output();
            Attach(Input);
            Attach(Output);
            _models.Add(new ModelDefinition(new[] { Input }));
            _models.Add(new ModelDefinition(new[] { Output }));
            RefreshValidation();
        }

        public void AddLayer(LayerDefinition layer)
        {
            RequireActive();
            RequireEditable(layer);
            if (layer.Workspace != null)
            {
                throw new InvalidOperationException("The block already belongs to a workspace.");
            }
            Attach(layer);
            _models.Add(new ModelDefinition(new[] { layer }));
            NotifyChanged();
        }

        public void AddAndConnect(LayerDefinition existingLayer, LayerDefinition newLayer)
        {
            ModelDefinition model = GetModel(existingLayer);
            RequireEditable(newLayer);
            if (newLayer.Workspace != null)
            {
                throw new InvalidOperationException("The block already belongs to a workspace.");
            }
            RequireAvailableEndpoint(model, existingLayer);

            List<LayerDefinition> layers = new List<LayerDefinition>(model.Layers);
            if (!ReferenceEquals(layers[layers.Count - 1], existingLayer))
            {
                layers.Reverse();
            }
            layers.Add(newLayer);
            ModelDefinition connectedModel = new ModelDefinition(layers);
            Attach(newLayer);
            _models[_models.IndexOf(model)] = connectedModel;
            NotifyChanged();
        }

        public ModelDefinition GetModel(LayerDefinition layer)
        {
            RequireActive();
            if (layer == null)
            {
                throw new ArgumentNullException(nameof(layer));
            }
            foreach (ModelDefinition model in _models)
            {
                foreach (LayerDefinition block in model.Layers)
                {
                    if (ReferenceEquals(block, layer))
                    {
                        return model;
                    }
                }
            }
            throw new ArgumentException("The block does not belong to this workspace.", nameof(layer));
        }

        public void Connect(LayerDefinition first, LayerDefinition second)
        {
            ModelDefinition firstModel = GetModel(first);
            ModelDefinition secondModel = GetModel(second);
            if (ReferenceEquals(firstModel, secondModel))
            {
                throw new InvalidOperationException("Connecting a chain to itself would create a cycle.");
            }
            RequireAvailableEndpoint(firstModel, first);
            RequireAvailableEndpoint(secondModel, second);

            List<LayerDefinition> firstBlocks = new List<LayerDefinition>(firstModel.Layers);
            List<LayerDefinition> secondBlocks = new List<LayerDefinition>(secondModel.Layers);
            if (!ReferenceEquals(firstBlocks[firstBlocks.Count - 1], first))
            {
                firstBlocks.Reverse();
            }
            if (!ReferenceEquals(secondBlocks[0], second))
            {
                secondBlocks.Reverse();
            }
            firstBlocks.AddRange(secondBlocks);
            _models[_models.IndexOf(firstModel)] = new ModelDefinition(firstBlocks);
            _models.Remove(secondModel);
            NotifyChanged();
        }

        public void Disconnect(LayerDefinition first, LayerDefinition second)
        {
            ModelDefinition model = GetModel(first);
            if (!ReferenceEquals(model, GetModel(second)))
            {
                throw new InvalidOperationException("The blocks are not connected.");
            }
            List<LayerDefinition> layers = new List<LayerDefinition>(model.Layers);
            int firstIndex = layers.IndexOf(first);
            int secondIndex = layers.IndexOf(second);
            if (Math.Abs(firstIndex - secondIndex) != 1)
            {
                throw new InvalidOperationException("The blocks are not adjacent.");
            }
            int index = Math.Min(firstIndex, secondIndex);
            int modelIndex = _models.IndexOf(model);
            _models[modelIndex] = new ModelDefinition(layers.GetRange(0, index + 1));
            _models.Insert(modelIndex + 1, new ModelDefinition(layers.GetRange(index + 1, layers.Count - index - 1)));
            NotifyChanged();
        }

        public void RemoveLayer(LayerDefinition layer)
        {
            RequireEditable(layer);
            ModelDefinition model = GetModel(layer);
            List<LayerDefinition> layers = new List<LayerDefinition>(model.Layers);
            int index = layers.IndexOf(layer);
            int modelIndex = _models.IndexOf(model);
            _models.RemoveAt(modelIndex);
            // Deleting a block removes its connections rather than reconnecting its neighbors.
            if (index > 0)
            {
                _models.Insert(modelIndex++, new ModelDefinition(layers.GetRange(0, index)));
            }
            if (index + 1 < layers.Count)
            {
                _models.Insert(modelIndex, new ModelDefinition(layers.GetRange(index + 1, layers.Count - index - 1)));
            }
            Detach(layer);
            NotifyChanged();
        }

        public void ReplaceLayer(LayerDefinition oldLayer, LayerDefinition newLayer)
        {
            RequireEditable(oldLayer);
            RequireEditable(newLayer);
            ModelDefinition model = GetModel(oldLayer);
            if (newLayer.Workspace != null)
            {
                throw new InvalidOperationException("The replacement block already belongs to a workspace.");
            }
            List<LayerDefinition> layers = new List<LayerDefinition>(model.Layers);
            layers[layers.IndexOf(oldLayer)] = newLayer;
            Attach(newLayer);
            Detach(oldLayer);
            _models[_models.IndexOf(model)] = new ModelDefinition(layers);
            NotifyChanged();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            foreach (ModelDefinition model in _models)
            {
                foreach (LayerDefinition layer in model.Layers)
                {
                    Detach(layer);
                }
            }
            OnChanged = null;
            OnValidationChanged = null;
            _isDisposed = true;
        }

        private void Attach(LayerDefinition layer)
        {
            layer.Workspace = this;
            layer.OnChanged += NotifyChanged;
        }

        private void Detach(LayerDefinition layer)
        {
            layer.OnChanged -= NotifyChanged;
            layer.Workspace = null;
        }

        private void RefreshValidation()
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();
            foreach (ModelDefinition model in _models)
            {
                issues.AddRange(ModelValidator.ValidateEditing(model));
            }
            ValidationIssues = issues.AsReadOnly();
        }

        private void NotifyChanged()
        {
            RefreshValidation();
            OnValidationChanged?.Invoke(ValidationIssues);
            OnChanged?.Invoke();
        }

        private static void RequireAvailableEndpoint(ModelDefinition model, LayerDefinition layer)
        {
            if (!ReferenceEquals(model.Layers[0], layer) &&
                !ReferenceEquals(model.Layers[model.Layers.Count - 1], layer))
            {
                throw new InvalidOperationException("The block already has two connections.");
            }
            if ((layer.Type == LayerType.Input || layer.Type == LayerType.Output) && model.Layers.Count > 1)
            {
                throw new InvalidOperationException("Input and Output can have only one connection.");
            }
        }

        private static void RequireEditable(LayerDefinition layer)
        {
            if (layer == null)
            {
                throw new ArgumentNullException(nameof(layer));
            }
            if (layer.Type == LayerType.Input || layer.Type == LayerType.Output)
            {
                throw new InvalidOperationException("Input and Output are fixed blocks.");
            }
        }

        private void RequireActive()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ModelWorkspace));
            }
        }
    }
}
