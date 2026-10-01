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

        public void Connect(LayerDefinition from, LayerDefinition to)
        {
            ModelDefinition left = GetModel(from);
            ModelDefinition right = GetModel(to);
            if (ReferenceEquals(left, right))
            {
                throw new InvalidOperationException("Connecting a chain to itself would create a cycle.");
            }
            if (!ReferenceEquals(left.Layers[left.Layers.Count - 1], from) ||
                !ReferenceEquals(right.Layers[0], to))
            {
                throw new InvalidOperationException("Connect the end of a chain to the start of another chain.");
            }
            if (from.Type == LayerType.Output || to.Type == LayerType.Input)
            {
                throw new InvalidOperationException("Input has no incoming connection; Output has no outgoing connection.");
            }
            List<LayerDefinition> layers = new List<LayerDefinition>(left.Layers);
            layers.AddRange(right.Layers);
            int index = _models.IndexOf(left);
            _models[index] = new ModelDefinition(layers);
            _models.Remove(right);
            NotifyChanged();
        }

        public void Disconnect(LayerDefinition from, LayerDefinition to)
        {
            ModelDefinition model = GetModel(from);
            if (!ReferenceEquals(model, GetModel(to)))
            {
                throw new InvalidOperationException("The blocks are not connected.");
            }
            List<LayerDefinition> layers = new List<LayerDefinition>(model.Layers);
            int index = layers.IndexOf(from);
            if (index + 1 >= layers.Count || !ReferenceEquals(layers[index + 1], to))
            {
                throw new InvalidOperationException("The blocks are not adjacent in this direction.");
            }
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
