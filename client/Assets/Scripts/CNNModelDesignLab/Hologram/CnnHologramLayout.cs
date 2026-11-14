using System.Collections.Generic;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.Hologram
{
    internal sealed class CnnHologramLayout
    {
        internal const int PointLimit = 7000;
        internal const int LineLimit = 4000;
        private const float MaximumPointPitch = 0.030f;
        private const float ChannelOffsetX = 0.009f;
        private const float ChannelOffsetY = 0.014f;
        private const int ChannelLimit = 4;
        private const int VectorLimit = 64;

        internal sealed class Layer
        {
            internal readonly int Width, Height, Channels, Elements;
            internal readonly Vector3 Origin;
            internal readonly string Name;
            internal readonly Color32 Color;
            internal readonly bool IsInput, IsOutput, IsFlatten;
            internal readonly Dictionary<int, int> NodeByIndex = new Dictionary<int, int>();
            internal int First, Count, DisplayedChannels;
            internal Vector3 Center;
            internal float GridHalfWidth => IsSpatial ? (Width - 1) * 0.5f : 0f;
            internal float SideMargin => IsSpatial ? (Mathf.Min(ChannelLimit, Channels) - 1) * ChannelOffsetX * 0.5f + 0.012f
                : IsOutput ? 0.16f : 0.013f;
            internal bool IsSpatial => Channels > 0;
            internal Color32 NodeColor(int channel) => !IsInput ? Color : channel switch
            {
                0 => new Color32(255, 89, 105, 255),
                1 => new Color32(84, 243, 136, 255),
                _ => new Color32(93, 160, 255, 255)
            };
            internal string Shape => IsSpatial ? $"{Width}×{Height}×{Channels}" : Elements.ToString();
            internal string SamplingLabel => IsSpatial
                ? (Count < Width * Height * DisplayedChannels ? $"격자 일부 · {Channels}장 중 {DisplayedChannels}장 표시"
                    : Channels > DisplayedChannels ? $"{Channels}장 중 {DisplayedChannels}장 표시" : "")
                : Elements > Count ? $"{Elements}개 중 일부" : "";

            internal Layer((Vector3Int Grid, int Elements, Vector3 Origin) data, string name, Color32 color)
            {
                Width = data.Grid.x;
                Height = data.Grid.y;
                Channels = data.Grid.z;
                Elements = data.Elements;
                Origin = data.Origin;
                Name = name;
                Color = color;
                IsInput = name == "INPUT";
                IsOutput = name == "OUTPUT";
                IsFlatten = name == "FLATTEN";
            }
        }

        internal readonly List<Layer> Layers = new List<Layer>();
        internal readonly List<(Vector3 Position, Color32 Color, float Radius)> Nodes =
            new List<(Vector3, Color32, float)>();
        internal readonly List<(int Start, int End, int Stage)> Lines = new List<(int, int, int)>();
        internal float Top { get; private set; }
        internal float Bottom { get; private set; }
        private readonly float _pointPitch;

        internal CnnHologramLayout(ModelExecutionDefinition snapshot,
            IReadOnlyList<(Vector3Int Grid, int Elements, Vector3 Origin)> sizes, Transform anchor)
        {
            Layers.Add(new Layer(sizes[0], "INPUT", new Color32(234, 246, 255, 255)));
            for (int index = 0; index < snapshot.Layers.Count; index++)
            {
                CnnLayerDefinition definition = snapshot.Layers[index];
                bool output = index == snapshot.Layers.Count - 1;
                (string name, Color32 color) = output ? ("OUTPUT", new Color32(57, 230, 255, 255)) : definition.Kind switch
                {
                    LayerKind.Conv2D => ("CONV", new Color32(76, 141, 255, 255)),
                    LayerKind.MaxPooling2D => ("POOL", new Color32(155, 107, 255, 255)),
                    LayerKind.Flatten => ("FLATTEN", new Color32(224, 64, 200, 255)),
                    _ => ("DENSE", new Color32(61, 220, 132, 255))
                };
                Layers.Add(new Layer(sizes[index + 1], name, color));
            }

            foreach (Layer layer in Layers)
            {
                Vector3 offset = anchor.InverseTransformDirection(layer.Origin - anchor.position);
                layer.Center = new Vector3(offset.x, 0f, offset.z);
            }

            // Use one pitch so pooling still looks smaller, while fitting adjacent grids above their slots.
            _pointPitch = MaximumPointPitch;
            for (int index = 1; index < Layers.Count; index++)
            {
                Layer left = Layers[index - 1];
                Layer right = Layers[index];
                float gridColumns = left.GridHalfWidth + right.GridHalfWidth;
                if (gridColumns <= 0f) { continue; }
                float gap = right.Center.x - left.Center.x - left.SideMargin - right.SideMargin - 0.04f;
                _pointPitch = Mathf.Min(_pointPitch, Mathf.Max(0.014f, gap / gridColumns));
            }

            // Keep the full RGB input and vectors. Share the remaining budget among feature maps.
            int remaining = PointLimit - Layers[0].Width * Layers[0].Height * Layers[0].Channels;
            int desiredSpatial = 0;
            for (int index = 1; index < Layers.Count; index++)
            {
                Layer layer = Layers[index];
                if (layer.IsSpatial) { desiredSpatial += SpatialPointCount(layer); }
                else { remaining -= Mathf.Min(VectorLimit, layer.Elements); }
            }
            float spatialFraction = desiredSpatial > remaining ? (float)remaining / desiredSpatial : 1f;

            for (int index = 0; index < Layers.Count; index++)
            {
                Layer layer = Layers[index];
                layer.First = Nodes.Count;
                if (layer.IsSpatial)
                {
                    int count = SpatialPointCount(layer);
                    AddSpatial(layer, index == 0 ? count : Mathf.FloorToInt(count * spatialFraction));
                }
                else { AddVector(layer, Layers[index - 1]); }
                layer.Count = Nodes.Count - layer.First;
            }

            var candidates = new List<(int Start, int End, int Stage)>[Layers.Count - 1];
            int total = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                candidates[index] = BuildConnection(index, snapshot.Layers[index]);
                total += candidates[index].Count;
            }
            // Share the line limit across transitions so late layers keep their visible connections.
            int retained = Mathf.Min(total, LineLimit);
            foreach (var connection in candidates)
            {
                if (connection.Count == 0) { continue; }
                int count = retained * connection.Count / total;
                for (int index = 0; index < count; index++)
                {
                    Lines.Add(connection[Sample(index, connection.Count, count)]);
                }
                total -= connection.Count;
                retained -= count;
            }
        }

        private static int SpatialPointCount(Layer layer) =>
            layer.Width * layer.Height * Mathf.Min(ChannelLimit, layer.Channels);

        private void AddSpatial(Layer layer, int budget)
        {
            int area = layer.Width * layer.Height;
            layer.DisplayedChannels = Mathf.Clamp(budget / area, 1, Mathf.Min(ChannelLimit, layer.Channels));
            int cells = Mathf.Min(area, budget / layer.DisplayedChannels);
            for (int channel = 0; channel < layer.DisplayedChannels; channel++)
            {
                for (int index = 0; index < cells; index++)
                {
                    int cell = Sample(index, area, cells);
                    int x = cell % layer.Width;
                    int y = cell / layer.Width;
                    float depth = channel - (layer.DisplayedChannels - 1) * 0.5f;
                    var position = layer.Center + new Vector3((x - (layer.Width - 1) * 0.5f) * _pointPitch + depth * ChannelOffsetX,
                        ((layer.Height - 1) * 0.5f - y) * _pointPitch + depth * ChannelOffsetY, depth * 0.19f);
                    AddNode(layer, cell * layer.Channels + channel, position, layer.NodeColor(channel), 0.0105f);
                }
            }
        }

        private void AddVector(Layer layer, Layer previous)
        {
            int count = Mathf.Min(VectorLimit, layer.IsFlatten ? previous.Count : layer.Elements);
            // Flatten uses the original HWC indices, including gaps caused by hidden channels.
            var sourceIndices = layer.IsFlatten ? new List<int>(previous.NodeByIndex.Keys) : null;
            sourceIndices?.Sort();
            bool gap = layer.Elements > count;
            float pitch = layer.IsOutput ? 0.20f : Mathf.Min(0.028f, 0.95f / Mathf.Max(1, count - 1));
            for (int index = 0; index < count; index++)
            {
                int source = layer.IsFlatten ? sourceIndices[Sample(index, sourceIndices.Count, count)]
                    : Sample(index, layer.Elements, count);
                float y = ((count - 1) * 0.5f - index) * pitch;
                if (gap) { y += index < (count + 1) / 2 ? 0.035f : -0.035f; }
                AddNode(layer, source, layer.Center + Vector3.up * y, layer.Color, layer.IsOutput ? 0.032f : 0.0105f);
            }
        }

        private void AddNode(Layer layer, int source, Vector3 position, Color32 color, float radius)
        {
            layer.NodeByIndex.Add(source, Nodes.Count);
            Nodes.Add((position, color, radius));
            Top = Mathf.Max(Top, position.y + radius);
            Bottom = Mathf.Min(Bottom, position.y - radius);
        }

        private List<(int Start, int End, int Stage)> BuildConnection(int stage, CnnLayerDefinition definition)
        {
            Layer input = Layers[stage];
            Layer output = Layers[stage + 1];
            var edges = new List<(int, int, int)>();
            if (output.IsFlatten)
            {
                foreach (var entry in output.NodeByIndex)
                {
                    edges.Add((input.NodeByIndex[entry.Key], entry.Value, stage));
                }
            }
            else if (output.IsSpatial)
            {
                bool conv = definition.Kind == LayerKind.Conv2D;
                var outputIndices = new List<int>(output.NodeByIndex.Keys);
                outputIndices.Sort();
                int count = Mathf.Min(conv ? 16 : 32, outputIndices.Count);
                for (int index = 0; index < count; index++)
                {
                    int id = outputIndices[Sample(index, outputIndices.Count, count)];
                    int cell = id / output.Channels;
                    int x = (cell % output.Width) * (conv ? 1 : definition.Width);
                    int y = (cell / output.Width) * (conv ? 1 : definition.Height);
                    int firstChannel = conv ? 0 : id % output.Channels;
                    int endChannel = conv ? input.DisplayedChannels : firstChannel + 1;
                    for (int ky = 0; ky < definition.Height; ky++)
                    for (int kx = 0; kx < definition.Width; kx++)
                    for (int channel = firstChannel; channel < endChannel; channel++)
                    {
                        int source = ((y + ky) * input.Width + x + kx) * input.Channels + channel;
                        if (input.NodeByIndex.TryGetValue(source, out int start))
                        {
                            edges.Add((start, output.NodeByIndex[id], stage));
                        }
                    }
                }
            }
            else
            {
                for (int start = input.First; start < input.First + input.Count; start++)
                for (int end = output.First; end < output.First + output.Count; end++)
                {
                    edges.Add((start, end, stage));
                }
            }
            return edges;
        }

        private static int Sample(int index, int available, int count) =>
            count == 1 ? available / 2 : index * (available - 1) / (count - 1);
    }
}
