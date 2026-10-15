using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene
{
    public sealed class ModelMetricGraph : MaskableGraphic
    {
        private struct MetricPoint
        {
            public int Epoch;
            public double? Training;
            public double? Validation;
        }

        private readonly List<MetricPoint> _points = new List<MetricPoint>();
        private bool _isAccuracy;
        private int _totalEpochs = 1;
        private Color _trainingColor;
        private Color _validationColor;

        public double Minimum { get; private set; }
        public double Maximum { get; private set; } = 1;

        public void Initialize(bool isAccuracy, Color trainingColor, Color validationColor)
        {
            _isAccuracy = isAccuracy;
            _trainingColor = trainingColor;
            _validationColor = validationColor;
            raycastTarget = false;
            Clear();
        }

        public void Clear()
        {
            _points.Clear();
            _totalEpochs = 1;
            Minimum = 0;
            Maximum = 1;
            SetVerticesDirty();
        }

        public void AddPoint(int epoch, int totalEpochs, double? training, double? validation)
        {
            if (epoch < 1)
            {
                return;
            }
            MetricPoint point = new MetricPoint
            {
                Epoch = epoch,
                Training = FiniteValue(training),
                Validation = FiniteValue(validation)
            };
            int index = 0;
            while (index < _points.Count && _points[index].Epoch < epoch)
            {
                index++;
            }
            if (index < _points.Count && _points[index].Epoch == epoch)
            {
                _points[index] = point;
            }
            else
            {
                _points.Insert(index, point);
            }
            _totalEpochs = Math.Max(_totalEpochs, Math.Max(epoch, totalEpochs));
            UpdateRange();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect bounds = GetPixelAdjustedRect();
            Rect plot = new Rect(bounds.xMin + 8, bounds.yMin + 8,
                Mathf.Max(1, bounds.width - 16), Mathf.Max(1, bounds.height - 16));
            Color gridColor = new Color(0.34f, 0.4f, 0.49f, 0.7f);
            for (int i = 0; i <= 4; i++)
            {
                float y = Mathf.Lerp(plot.yMin, plot.yMax, i / 4f);
                AddLine(vertexHelper, new Vector2(plot.xMin, y), new Vector2(plot.xMax, y), 1, gridColor);
            }
            AddLine(vertexHelper, new Vector2(plot.xMin, plot.yMin), new Vector2(plot.xMin, plot.yMax), 1, gridColor);
            DrawSeries(vertexHelper, plot, true, _trainingColor);
            DrawSeries(vertexHelper, plot, false, _validationColor);
        }

        private static double? FiniteValue(double? value)
        {
            return value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value)
                ? value : null;
        }

        private void UpdateRange()
        {
            Minimum = 0;
            Maximum = _isAccuracy ? 1 : 0.0001;
            if (_isAccuracy)
            {
                return;
            }
            foreach (MetricPoint point in _points)
            {
                IncludeInRange(point.Training);
                IncludeInRange(point.Validation);
            }
        }

        private void IncludeInRange(double? value)
        {
            if (value.HasValue)
            {
                Minimum = Math.Min(Minimum, value.Value);
                Maximum = Math.Max(Maximum, value.Value);
            }
        }

        private void DrawSeries(VertexHelper vertexHelper, Rect plot, bool training, Color seriesColor)
        {
            Vector2? previous = null;
            foreach (MetricPoint point in _points)
            {
                double? value = training ? point.Training : point.Validation;
                if (!value.HasValue)
                {
                    previous = null;
                    continue;
                }
                // Epoch zero has no measurement. The first point is placed at its actual epoch.
                float x = Mathf.Lerp(plot.xMin, plot.xMax, (float)point.Epoch / _totalEpochs);
                float y = Mathf.Lerp(plot.yMin, plot.yMax, (float)((value.Value - Minimum) / (Maximum - Minimum)));
                Vector2 position = new Vector2(x, y);
                if (previous.HasValue)
                {
                    AddLine(vertexHelper, previous.Value, position, 3, seriesColor);
                }
                AddQuad(vertexHelper, position + new Vector2(-3, -3), position + new Vector2(-3, 3),
                    position + new Vector2(3, 3), position + new Vector2(3, -3), seriesColor);
                previous = position;
            }
        }

        private static void AddLine(VertexHelper vertexHelper, Vector2 start, Vector2 end, float width, Color lineColor)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            AddQuad(vertexHelper, start - normal, start + normal, end + normal, end - normal, lineColor);
        }

        private static void AddQuad(VertexHelper vertexHelper, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color quadColor)
        {
            int index = vertexHelper.currentVertCount;
            vertexHelper.AddVert(a, quadColor, Vector2.zero);
            vertexHelper.AddVert(b, quadColor, Vector2.zero);
            vertexHelper.AddVert(c, quadColor, Vector2.zero);
            vertexHelper.AddVert(d, quadColor, Vector2.zero);
            vertexHelper.AddTriangle(index, index + 1, index + 2);
            vertexHelper.AddTriangle(index, index + 2, index + 3);
        }
    }
}
