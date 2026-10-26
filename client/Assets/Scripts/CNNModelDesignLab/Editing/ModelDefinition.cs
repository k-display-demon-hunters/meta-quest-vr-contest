using System.Collections.Generic;

namespace ModelDesignScene.Editing
{
    // A snapshot of one connected chain; block settings retain their object references.
    public sealed class ModelDefinition
    {
        public IReadOnlyList<LayerDefinition> Layers { get; }

        internal ModelDefinition(IEnumerable<LayerDefinition> layers)
        {
            Layers = new List<LayerDefinition>(layers).AsReadOnly();
        }
    }
}
