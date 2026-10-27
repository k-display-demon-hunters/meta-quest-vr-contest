using System.Collections.Generic;
using CNNFeatureMaps.FeatureMaps;
using UnityEngine;

namespace CNNFeatureMaps.Interaction
{
    public sealed class FilterController : MonoBehaviour
    {
        private const float CellSpacing = .066f;
        [SerializeField] private GrabTarget _filter;
        [SerializeField] private Material _toolMaterial;
        private Transform _inputRoot;
        private Rigidbody _body;
        private LineRenderer _connection;
        private readonly MapCell[,,] _firstNodes = new MapCell[2, 8, 8];
        private int _scanIndex;
        private float _nextScan;

        public int Row { get; private set; } = 3;
        public int Column { get; private set; } = 3;
        public bool IsScanning { get; private set; }

        public void Initialize(Transform inputRoot, IReadOnlyList<MapCell> nodes)
        {
            _inputRoot = inputRoot;
            _body = _filter.GetComponent<Rigidbody>();
            foreach (MapCell node in nodes)
            {
                if (node.Layer == 0)
                {
                    _firstNodes[node.Channel, node.Row, node.Column] = node;
                }
            }
            GameObject connection = new GameObject("Patch to output neuron");
            connection.transform.SetParent(transform, false);
            _connection = connection.AddComponent<LineRenderer>();
            _connection.sharedMaterial = _toolMaterial;
            _connection.positionCount = 2;
            _connection.useWorldSpace = true;
            _connection.startWidth = .006f;
            _connection.endWidth = .006f;
            _connection.startColor = _connection.endColor = new Color(1f, .83f, .22f);
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", new Color(1f, .83f, .22f));
            properties.SetColor("_EmissionColor", new Color(.4f, .3f, .02f));
            _connection.SetPropertyBlock(properties);
        }

        public void SelectPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public void SetVisible(bool visible)
        {
            _filter.gameObject.SetActive(visible);
            _connection.enabled = visible;
        }

        public void ToggleScanning()
        {
            IsScanning = !IsScanning;
            _scanIndex = 0;
            _nextScan = Time.unscaledTime;
        }

        public void StopScanning()
        {
            IsScanning = false;
        }

        public bool UpdatePosition()
        {
            bool changed = false;
            if (_filter.IsHeld)
            {
                StopScanning();
                Vector3 local = _inputRoot.InverseTransformPoint(_filter.transform.position);
                int row = Mathf.Clamp(Mathf.RoundToInt(3.5f - local.y / CellSpacing), 0, 7);
                int column = Mathf.Clamp(Mathf.RoundToInt(local.x / CellSpacing + 3.5f), 0, 7);
                changed = row != Row || column != Column;
                SelectPosition(row, column);
            }
            if (IsScanning && Time.unscaledTime >= _nextScan)
            {
                SelectPosition(_scanIndex / 8, _scanIndex % 8);
                changed = true;
                _scanIndex++;
                _nextScan = Time.unscaledTime + .1f;
                if (_scanIndex >= 64)
                {
                    StopScanning();
                }
            }
            // Only the teaching filter follows the grid; maps and neurons remain freely movable.
            Vector3 position = new Vector3((Column - 3.5f) * CellSpacing, (3.5f - Row) * CellSpacing, -.075f);
            _body.position = _inputRoot.TransformPoint(position);
            _body.rotation = _inputRoot.rotation;
            _filter.transform.SetPositionAndRotation(_body.position, _body.rotation);
            return changed;
        }

        public void UpdateConnection(int channel)
        {
            _connection.SetPosition(0, _filter.transform.position);
            _connection.SetPosition(1, _firstNodes[channel, Row, Column].transform.position);
        }
    }
}
