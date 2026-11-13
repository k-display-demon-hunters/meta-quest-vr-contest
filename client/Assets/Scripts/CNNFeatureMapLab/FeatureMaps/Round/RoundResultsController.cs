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
        private FeatureMapDisplayController _display;
        private Vector3 _destination;
        private int _lastScan;
        private int Layer => _data.Layer;
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
            _display = display;
            _sources = display.Sources(data.Layer);
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
            bool showResults = IsOutput && (step is LessonStep.Scan or LessonStep.Explain or LessonStep.Merge);
            for (int i = 0; i < _count; i++)
            {
                FeatureMapPanelController image = _images[i];
                image.Root.gameObject.SetActive(showResults);
                bool showWindow = step == LessonStep.Scan;
                _scanWindows[i].gameObject.SetActive(showWindow);
                if (showWindow)
                {
                    SetScanPlate(i);
                    Color color = colors[i];
                    color.a = IsOutput ? .5f : .12f;
                    _visuals.Paint(_scanWindows[i], color);
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
            }
        }

        internal void Tick(LessonStep step, float elapsed)
        {
            if (step == LessonStep.Scan)
            {
                Scan(elapsed);
            }
            if (step == LessonStep.Merge && IsOutput)
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
            if (step == LessonStep.Merge && IsOutput)
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
            _images[index].Root.localPosition = source.Position + Vector3.up * (source.Width + .85f);
            _images[index].Root.localRotation = source.Root.localRotation;
            _images[index].Root.localScale = Vector3.one;
        }

        private void Scan(float elapsed)
        {
            float duration = IsOutput ? 3.5f : 10;
            int shown = Mathf.Min(_size*_size, Mathf.FloorToInt(_size*_size*elapsed/duration));
            bool changed = shown != _lastScan;
            _lastScan = shown;
            if (!IsOutput && changed)
            {
                _display.DrawResult(Layer, _data.Map, shown);
            }
            float fly = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed/.43f));
            for (int i = 0; i < _count; i++)
            {
                if (IsOutput)
                {
                    PositionImage(i);
                    _images[i].UpdateMap(_data.Values(i), _data.Scale(i), shown);
                }
                _scanWindows[i].gameObject.SetActive(shown < _size*_size);
                if (shown == _size*_size)
                {
                    continue;
                }
                Transform window = _scanWindows[i].transform;
                FeatureMapPanelController source = _sources[i];
                float cursor = Mathf.Clamp(elapsed / duration * _size * _size, 0, _size * _size - 1);
                int cell = Mathf.FloorToInt(cursor);
                int nextCell = Mathf.Min(cell + 1, _size * _size - 1);
                Vector3 local = Vector3.Lerp(source.CellPosition(cell), source.CellPosition(nextCell), cursor - cell);
                Vector3 position = IsOutput ? source.Root.TransformPoint(0, 0, -.035f)
                    : source.Root.TransformPoint(local);
                if (!IsOutput)
                {
                    source.Highlight(cell / _size - 1, cell % _size - 1,
                        cell / _size + 1, cell % _size + 1, Layer == 1 ? CorridorTheme.Rgb[i] : CorridorTheme.Pairs[i]);
                }
                Transform plate = _filter.Plate(i);
                window.position = Vector3.Lerp(plate.position, position, fly);
                window.rotation = Quaternion.Slerp(plate.rotation, source.Root.rotation, fly);
                float width = IsOutput ? source.Width : source.Width*3/_size;
                if (Layer == 1)
                {
                    width *= source.Root.localScale.x;
                }
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
                image.Root.localRotation = Quaternion.Slerp(_sources[i].Root.localRotation, Quaternion.identity, t);
                image.Root.localScale = Vector3.one * Mathf.Lerp(1, .2f, t);
                image.Opacity(1-Mathf.InverseLerp(.75f, 1, t));
                if (t == 1)
                {
                    image.Root.gameObject.SetActive(false);
                }
            }
        }
    }
}
