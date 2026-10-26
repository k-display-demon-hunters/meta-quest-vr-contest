using System;
using System.Collections.Generic;
using ModelDesignScene.Model;

namespace ModelDesignScene.BlockSettings
{
    internal sealed class BlockSettingsDefinition
    {
        internal string Title { get; }
        internal string Summary { get; }
        internal bool HasActivation { get; }
        internal IReadOnlyList<NumberField> NumberFields { get; }

        internal BlockSettingsDefinition(string title, string summary, bool hasActivation,
            params NumberField[] numberFields)
        {
            Title = title;
            Summary = summary;
            HasActivation = hasActivation;
            NumberFields = Array.AsReadOnly(numberFields);
        }

        internal sealed class NumberField
        {
            private readonly Func<LayerDefinition, int> _readValue;

            internal LayerNumberSetting Setting { get; }
            internal string Label { get; }
            internal int Maximum { get; }

            internal NumberField(LayerNumberSetting setting, string label, int maximum,
                Func<LayerDefinition, int> readValue)
            {
                Setting = setting;
                Label = label;
                Maximum = maximum;
                _readValue = readValue;
            }

            internal int ReadValue(LayerDefinition layer)
            {
                return _readValue(layer);
            }
        }
    }
}
