using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    internal sealed class EndingModelController
    {
        private static readonly string[] BasicLayers = { "INPUT", "CONV", "MAX POOL", "FLATTEN", "DENSE", "OUTPUT" };
        private static readonly int CardColor = Shader.PropertyToID("_BaseColor");
        private const float CardWidth = .26f;
        private const float CardSpacing = .288f;

        private readonly Transform _world, _network;
        private readonly Transform[] _networkLayers;
        private readonly MeshRenderer[] _layerCards;
        private readonly Text _modelHeading, _testAccuracy;
        private readonly Text[] _layerLabels;
        private int _visibleLayerCount;
        private readonly Vector3 _scale;

        internal EndingModelController(Transform world, Transform network, Transform[] layers,
            MeshRenderer[] cards, Text heading, Text accuracy, Text[] labels)
        {
            _world = world;
            _network = network;
            _scale = network.localScale;
            _networkLayers = layers;
            _layerCards = cards;
            _modelHeading = heading;
            _testAccuracy = accuracy;
            _layerLabels = labels;
        }

        internal void Bind(CinematicModelResult result)
        {
            bool hasRecord = result.Layers.Count > 0;
            System.Collections.Generic.IReadOnlyList<string> layers = hasRecord ? result.Layers : BasicLayers;
            _visibleLayerCount = Mathf.Min(layers.Count, _networkLayers.Length);
            _modelHeading.text = hasRecord ? "내가 만든 모델" : "CNN의 기본 구조";

            double? accuracy = hasRecord ? result.TestAccuracy : null;
            _testAccuracy.text = accuracy.HasValue
                ? $"테스트 정확도 {System.Math.Round(accuracy.Value * 100d, System.MidpointRounding.AwayFromZero):0}%"
                : string.Empty;
            _testAccuracy.gameObject.SetActive(accuracy.HasValue);

            var cardProperties = new MaterialPropertyBlock();
            for (int i = 0; i < _networkLayers.Length; i++)
            {
                Transform slot = _networkLayers[i];
                slot.gameObject.SetActive(false);
                if (i >= _visibleLayerCount) { continue; }

                slot.localPosition = new Vector3((i - (_visibleLayerCount - 1) * .5f) * CardSpacing, 0, 0);
                string name = CardName(layers[i]);
                Text label = _layerLabels[i];
                label.text = name;
                label.color = InkForCard(name);
                label.rectTransform.localScale = new Vector3(name == "MAX POOL" ? .82f : 1f, 1f, 1f);
                cardProperties.SetColor(CardColor, ColorForCard(name));
                _layerCards[i].SetPropertyBlock(cardProperties);
            }
        }

        private static Color32 InkForCard(string name) => name switch
        {
            "CONV" => new Color32(0x06, 0x15, 0x2E, 0xFF),
            "MAX POOL" => new Color32(0x14, 0x09, 0x2D, 0xFF),
            "FLATTEN" => new Color32(0x29, 0x06, 0x24, 0xFF),
            "DENSE" => new Color32(0x05, 0x25, 0x17, 0xFF),
            _ => new Color32(0x0D, 0x19, 0x26, 0xFF)
        };

        private static string CardName(string record)
        {
            int newline = record.IndexOf('\n');
            string kind = (newline < 0 ? record : record.Substring(0, newline)).Trim();
            return kind switch
            {
                "Conv2D" => "CONV",
                "MaxPooling2D" => "MAX POOL",
                _ => kind.ToUpperInvariant()
            };
        }

        private static Color32 ColorForCard(string name) => name switch
        {
            "CONV" => new Color32(0x4C, 0x8D, 0xFF, 0xFF),
            "MAX POOL" => new Color32(0x9B, 0x6B, 0xFF, 0xFF),
            "FLATTEN" => new Color32(0xE0, 0x40, 0xC8, 0xFF),
            "DENSE" => new Color32(0x3D, 0xDC, 0x84, 0xFF),
            _ => new Color32(0x9F, 0xB3, 0xC8, 0xFF)
        };

        internal EndingFocusTarget Target
        {
            get
            {
                float width = (_visibleLayerCount - 1) * CardSpacing + CardWidth;
                Vector3 world = _network.TransformPoint(new Vector3(0f, 0f, -.055f));
                return new EndingFocusTarget(_world.InverseTransformPoint(world), new Vector2(width + .06f, .28f) * _network.localScale.x);
            }
        }

        internal void ShowModel(bool visible, float elapsed)
        {
            _network.gameObject.SetActive(visible);
            if (!visible) { return; }

            _network.localScale = _scale * Mathf.Lerp(.84f, 1f, Mathf.SmoothStep(0f, 1f, elapsed / .6f));
            for (int i = 0; i < _visibleLayerCount; i++)
            {
                _networkLayers[i].gameObject.SetActive(elapsed >= i * .13f);
            }

        }

    }
}
