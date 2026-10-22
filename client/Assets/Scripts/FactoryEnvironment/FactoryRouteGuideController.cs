using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace FactoryEnvironment
{
    public sealed class FactoryRouteGuideController : MonoBehaviour
    {
        [Serializable]
        private struct RouteNode
        {
            public Vector3 Position;
            public int[] Neighbours;
        }

        [SerializeField] private Material _material;
        [SerializeField] private RouteNode[] _nodes;
        [SerializeField] private int[] _destinationNodes;
        [SerializeField, Min(0.01f)] private float _lineWidth = 0.12f;
        [SerializeField, Min(0.01f)] private float _refreshInterval = 0.15f;
        [SerializeField, Min(0f)] private float _floorOffset = 0.03f;
        [SerializeField] private LayerMask _obstacles = ~0;

        private Transform _player;
        private LineRenderer _line;
        private Vector3[] _positions;
        private float[] _distances;
        private int[] _nextNodes;
        private bool[] _visited;
        private int _destination = -1;
        private float _nextRefreshTime;

        private void Update()
        {
            if (_line == null || _player == null || _destination < 0 || Time.time < _nextRefreshTime)
            {
                return;
            }
            _nextRefreshTime = Time.time + _refreshInterval;
            RefreshLine();
        }

        public void Bind(Transform player)
        {
            _player = player;
            if (!HasValidGraph())
            {
                Debug.LogError("[FactoryRoute] 안내선 재질과 경유점 연결을 확인해 주세요.", this);
                enabled = false;
                return;
            }
            _distances = new float[_nodes.Length];
            _nextNodes = new int[_nodes.Length];
            _visited = new bool[_nodes.Length];
            _positions = new Vector3[_nodes.Length + 3];
            GameObject lineObject = new GameObject("Floor route line");
            lineObject.transform.SetParent(transform, false);
            // TransformZ keeps the ribbon flat on the factory floor instead of facing the headset.
            lineObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _line = lineObject.AddComponent<LineRenderer>();
            _line.sharedMaterial = _material;
            _line.useWorldSpace = true;
            _line.alignment = LineAlignment.TransformZ;
            _line.widthMultiplier = _lineWidth;
            _line.numCornerVertices = 3;
            _line.numCapVertices = 3;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.positionCount = 0;
        }

        public void SetDestination(int destination)
        {
            if (_line == null || _destination == destination)
            {
                return;
            }
            _destination = destination;
            _line.positionCount = 0;
            if (destination < 0 || destination >= _destinationNodes.Length)
            {
                _destination = -1;
                return;
            }
            BuildPaths(_destinationNodes[destination]);
            _nextRefreshTime = 0f;
        }

        private bool HasValidGraph()
        {
            if (_material == null || _nodes == null || _nodes.Length == 0 || _destinationNodes == null)
            {
                return false;
            }
            foreach (int destination in _destinationNodes)
            {
                if (destination < 0 || destination >= _nodes.Length)
                {
                    return false;
                }
            }
            for (int index = 0; index < _nodes.Length; index++)
            {
                if (_nodes[index].Neighbours == null)
                {
                    return false;
                }
                foreach (int neighbour in _nodes[index].Neighbours)
                {
                    if (neighbour < 0 || neighbour >= _nodes.Length || _nodes[neighbour].Neighbours == null
                        || Array.IndexOf(_nodes[neighbour].Neighbours, index) < 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void BuildPaths(int target)
        {
            for (int index = 0; index < _nodes.Length; index++)
            {
                _distances[index] = float.PositiveInfinity;
                _nextNodes[index] = -1;
                _visited[index] = false;
            }
            _distances[target] = 0f;
            for (int count = 0; count < _nodes.Length; count++)
            {
                int nearest = -1;
                float distance = float.PositiveInfinity;
                for (int index = 0; index < _nodes.Length; index++)
                {
                    if (!_visited[index] && _distances[index] < distance)
                    {
                        nearest = index;
                        distance = _distances[index];
                    }
                }
                if (nearest < 0)
                {
                    return;
                }
                _visited[nearest] = true;
                foreach (int neighbour in _nodes[nearest].Neighbours)
                {
                    float candidate = distance + Vector3.Distance(NodePosition(nearest), NodePosition(neighbour));
                    if (candidate < _distances[neighbour])
                    {
                        _distances[neighbour] = candidate;
                        _nextNodes[neighbour] = nearest;
                    }
                }
            }
        }

        private void RefreshLine()
        {
            Vector3 player = _player.position;
            player.y = transform.position.y + _floorOffset;
            float bestDistance = float.PositiveInfinity;
            Vector3 entry = player;
            int firstNode = -1;
            // Join a visible corridor segment, so a nearby node across a wall is never used as a shortcut.
            for (int index = 0; index < _nodes.Length; index++)
            {
                foreach (int neighbour in _nodes[index].Neighbours)
                {
                    if (neighbour < index)
                    {
                        continue;
                    }
                    Vector3 from = NodePosition(index);
                    Vector3 to = NodePosition(neighbour);
                    Vector3 segment = to - from;
                    float ratio = segment.sqrMagnitude > 0f
                        ? Mathf.Clamp01(Vector3.Dot(player - from, segment) / segment.sqrMagnitude) : 0f;
                    Vector3 projection = from + segment * ratio;
                    float joinDistance = Vector3.Distance(player, projection);
                    float viaFrom = Vector3.Distance(projection, from) + _distances[index];
                    float viaTo = Vector3.Distance(projection, to) + _distances[neighbour];
                    float total = joinDistance + Mathf.Min(viaFrom, viaTo);
                    if (total >= bestDistance || !CanJoin(player, projection))
                    {
                        continue;
                    }
                    bestDistance = total;
                    entry = projection;
                    firstNode = viaFrom <= viaTo ? index : neighbour;
                }
            }
            if (firstNode < 0)
            {
                _line.positionCount = 0;
                return;
            }
            int positionCount = 0;
            _positions[positionCount++] = player;
            if ((entry - player).sqrMagnitude > 0.001f)
            {
                _positions[positionCount++] = entry;
            }
            for (int node = firstNode; node >= 0 && positionCount < _positions.Length; node = _nextNodes[node])
            {
                Vector3 position = NodePosition(node);
                if ((position - _positions[positionCount - 1]).sqrMagnitude > 0.001f)
                {
                    _positions[positionCount++] = position;
                }
            }
            _line.positionCount = positionCount;
            for (int index = 0; index < positionCount; index++)
            {
                _line.SetPosition(index, _positions[index]);
            }
        }

        private Vector3 NodePosition(int index)
        {
            Vector3 position = transform.TransformPoint(_nodes[index].Position);
            position.y += _floorOffset;
            return position;
        }

        private bool CanJoin(Vector3 from, Vector3 to)
        {
            Vector3 offset = Vector3.up * 0.45f;
            Vector3 delta = to - from;
            return delta.sqrMagnitude < 0.001f
                || !Physics.SphereCast(from + offset, 0.2f, delta.normalized, out _, delta.magnitude,
                    _obstacles, QueryTriggerInteraction.Ignore);
        }

        private void OnDisable()
        {
            if (_line != null)
            {
                _line.positionCount = 0;
            }
        }
    }
}
