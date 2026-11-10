using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class RoundResultsController
    {
        [SerializeField] private FeatureMapPanelController[] _images;
        [SerializeField] private Renderer[] _scanWindows;
        [SerializeField] private Texture2D _plain;
        private readonly Vector3[] _mergePositions = new Vector3[5];
        private CorridorRenderUtility _visuals;
        private FilterStackController _filter;
        private RoundCalculationResult _data;
        private FeatureMapPanelController[] _sources;
        private FeatureMapPanelController _target;
        private Vector3 _destination;
        private int _lastScan;
        private int Layer => _data.Layer;
        private int Index => _data.Index;
        private bool IsOutput => _data.IsOutput;
        private int _size => _data.Size;
        private int _count => _data.Count;
        internal void Bind(CorridorRenderUtility visuals, FilterStackController filter)
        {
            _visuals = visuals;
            _filter = filter;
            foreach (var image in _images)
            {
                image.Bind(visuals);
            }
        }

        internal void Configure(RoundCalculationResult data, FeatureMapDisplayController display)
        {
            _data = data;
            _sources = display.Sources(data.Layer);
            _target = data.IsOutput ? null : display.Map(data.Layer, data.Map);
            _destination = display.Destination(data.Layer, data.Map, data.Digit);
            for (int i = 0; i < data.Count; i++)
            {
                _images[i].SetGridSize(data.Size);
            }
        }

        internal void Show(LessonStep step)
        {
            Color32[] colors = Layer == 1 ? CorridorTheme.Rgb : CorridorTheme.Pairs;
            _lastScan = -1;
            bool showResults = step is LessonStep.Scan or LessonStep.Explain or LessonStep.CellBreakdown or LessonStep.Merge;
            for (int i = 0; i < _count; i++)
            {
                FeatureMapPanelController image = _images[i];
                image.Root.gameObject.SetActive(showResults);
                _scanWindows[i].gameObject.SetActive(step == LessonStep.Scan);
                if (step == LessonStep.Scan)
                {
                    SetScanPlate(i);
                }
                if (!showResults)
                {
                    continue;
                }
                PositionImage(i);
                _mergePositions[i] = image.Position;
                image.Frame(colors[i]);
                image.Opacity(1);
                image.ClearHighlight();
                image.UpdateMap(_data.Values(i), _data.Scale(i), step == LessonStep.Scan ? 0 : int.MaxValue);
                if ((step is LessonStep.CellBreakdown or LessonStep.Merge) && !IsOutput)
                {
                    image.Cell(Index, _visuals.Theme.Yellow);
                }
                Color color = colors[i];
                color.a = .5f;
                _visuals.Paint(_scanWindows[i], color);
            }
        }

        internal void Tick(LessonStep step, float elapsed)
        {
            if (step == LessonStep.Scan)
            {
                Scan(elapsed);
            }
            if (step == LessonStep.Merge)
            {
                Merge(elapsed);
            }
        }

        internal void Finish(LessonStep step)
        {
            if (step == LessonStep.Scan)
            {
                Scan(IsOutput ? 3.5f : 10f);
            }
            if (step == LessonStep.Merge)
            {
                Merge(1.9f);
            }
        }

        internal void Hide()
        {
            for (int i = 0;i<_images.Length;i++)
            {
                _images[i].Root.gameObject.SetActive(false);
                _scanWindows[i].gameObject.SetActive(false);
            }
        }

        internal void Dispose()
        {
            foreach (var image in _images)
            {
                image.Dispose();
            }
        }

        private void SetScanPlate(int src)
        {
            _visuals.Texture(_scanWindows[src], IsOutput ? _filter.TemplateTexture(src) : _plain);
        }

        private void PositionImage(int index)
        {
            FeatureMapPanelController source = _sources[index];
            float gap = Layer == 1 ? .12f : .85f;
            _images[index].Root.localPosition = source.Position+Vector3.up*(source.Width+gap);
            _images[index].Root.localRotation = source.Root.localRotation;
            _images[index].Root.localScale = Vector3.one;
        }

        private void Scan(float elapsed)
        {
            float duration = IsOutput ? 3.5f : 10;
            int shown = Mathf.Min(_size*_size, Mathf.FloorToInt(_size*_size*elapsed/duration));
            if (shown == _lastScan)
            {
                return;
            }
            _lastScan = shown;
            float fly = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed/.43f));
            for (int i = 0; i < _count; i++)
            {
                PositionImage(i);
                _images[i].UpdateMap(_data.Values(i), _data.Scale(i), shown);
                _scanWindows[i].gameObject.SetActive(shown < _size*_size);
                if (shown == _size*_size)
                {
                    continue;
                }
                Transform window = _scanWindows[i].transform;
                FeatureMapPanelController source = _sources[i];
                Vector3 position = IsOutput ? source.Root.TransformPoint(0, 0, -.035f)
                : source.Root.TransformPoint(source.CellPosition(shown));
                Transform plate = _filter.Plate(i);
                window.position = Vector3.Lerp(plate.position, position, fly);
                window.rotation = Quaternion.Slerp(plate.rotation, source.Root.rotation, fly);
                float width = IsOutput ? source.Width : source.Width*3/_size;
                window.localScale = Vector3.one*Mathf.Lerp(.8f, width, fly);
            }
        }

        private void Merge(float elapsed)
        {
            float t = Mathf.Clamp01(elapsed/1.9f);
            for (int i = 0; i < _count; i++)
            {
                FeatureMapPanelController image = _images[i];
                image.Root.localPosition = Vector3.Lerp(_mergePositions[i], _destination+Vector3.back*.058f, Mathf.SmoothStep(0, 1, t));
                image.Root.localRotation = Quaternion.Slerp(_sources[i].Root.localRotation, _target == null ? Quaternion.identity : _target.Root.localRotation, t);
                image.Root.localScale = Vector3.one*(IsOutput ? Mathf.Lerp(1, .2f, t) : 1);
                image.Opacity(1-Mathf.InverseLerp(.75f, 1, t));
                if (t == 1)
                {
                    image.Root.gameObject.SetActive(false);
                }
            }
        }
    }
}
