using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tablet
{
    [DisallowMultipleComponent]
    public sealed class TabletMinimapController : MaskableGraphic
    {
        private const float RefreshInterval = 0.1f;
        private const float NearbyWorldHeight = 22f;
        private static readonly Color BackgroundColor = new Color(0.035f, 0.085f, 0.15f);
        private static readonly Color GridColor = new Color(0.08f, 0.15f, 0.23f);
        private static readonly Color FloorColor = new Color(0.10f, 0.23f, 0.35f);
        private static readonly Color WallColor = new Color(0.40f, 0.67f, 0.85f);
        private static readonly Color PaperColor = new Color(0.70f, 0.88f, 1f);
        private static readonly Color PlayerColor = new Color(0.24f, 0.90f, 1f);

        private readonly List<TabletMapData.Floor> _floors = new List<TabletMapData.Floor>();
        private readonly List<Rect> _walls = new List<Rect>();
        private readonly List<TabletMapData.Parchment> _parchments = new List<TabletMapData.Parchment>();
        private readonly List<Text> _parchmentLabels = new List<Text>();
        private readonly List<Text> _roomLabels = new List<Text>();
        private TabletManager _tablet;
        private Transform _player;
        private Font _font;
        private Text _status;
        private bool _overview;
        private bool _initialized;
        private Rect _mapBounds;
        private Rect _viewBounds;
        private Vector2 _playerPosition;
        private Vector2 _playerForward = Vector2.up;
        private int _visibleParchments;
        private float _nextRefreshTime;
        private string _lastStatus;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_initialized)
            {
                RebuildMap();
            }
        }

        public void Initialize(TabletManager tablet, Transform player, Font font, Text status, bool overview)
        {
            _tablet = tablet;
            _player = player;
            _font = font;
            _status = status;
            _overview = overview;
            raycastTarget = false;
            _initialized = true;
            RebuildMap();
        }

        private void LateUpdate()
        {
            if (!_initialized || Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }
            _nextRefreshTime = Time.unscaledTime + RefreshInterval;
            RefreshTracking();
        }

        internal void RebuildMap()
        {
            _floors.Clear();
            _walls.Clear();
            _parchments.Clear();
            ClearLabels(_parchmentLabels);
            ClearLabels(_roomLabels);
            TabletMapData data = _tablet.MapData;
            _mapBounds = data != null ? data.Bounds : default;
            if (data != null)
            {
                _floors.AddRange(data.Floors);
                _walls.AddRange(data.Walls);
                _parchments.AddRange(data.Parchments);
                if (_overview)
                {
                    foreach (TabletMapData.Floor floor in _floors)
                    {
                        _roomLabels.Add(CreateLabel("Room " + floor.Name, floor.Name, 17,
                            new Color(0.68f, 0.82f, 0.94f), new Vector2(118f, 26f)));
                    }
                }
                foreach (TabletMapData.Parchment marker in _parchments)
                {
                    _parchmentLabels.Add(CreateLabel("Parchment Marker " + marker.LessonIndex,
                        (marker.LessonIndex + 1).ToString(), _overview ? 18 : 12, BackgroundColor,
                        _overview ? new Vector2(24f, 28f) : new Vector2(18f, 20f)));
                }
            }
            _nextRefreshTime = 0f;
            RefreshTracking();
            SetVerticesDirty();
        }

        private void RefreshTracking()
        {
            Vector2 previousPosition = _playerPosition;
            Vector2 previousForward = _playerForward;
            int previousVisibility = _visibleParchments;
            if (_player != null)
            {
                _playerPosition = new Vector2(_player.position.x, _player.position.z);
                Vector3 forward = _player.forward;
                Vector2 horizontalForward = new Vector2(forward.x, forward.z);
                if (horizontalForward.sqrMagnitude > 0.001f)
                {
                    _playerForward = horizontalForward.normalized;
                }
            }

            _visibleParchments = 0;
            for (int index = 0; index < _parchments.Count; index++)
            {
                TabletMapData.Parchment pickup = _parchments[index];
                if (pickup.IsActive && !_tablet.HasParchment(pickup.LessonIndex))
                {
                    _visibleParchments |= 1 << index;
                }
            }

            Rect previousView = _viewBounds;
            Rect canvas = rectTransform.rect;
            if (_overview && _floors.Count > 0)
            {
                float scale = Mathf.Min((canvas.width - 44f) / _mapBounds.width,
                    (canvas.height - 32f) / _mapBounds.height);
                Vector2 size = canvas.size / Mathf.Max(scale, 0.01f);
                _viewBounds = new Rect(_mapBounds.center - size * 0.5f, size);
            }
            else
            {
                Vector2 size = new Vector2(NearbyWorldHeight * canvas.width / Mathf.Max(canvas.height, 1f), NearbyWorldHeight);
                _viewBounds = new Rect(_playerPosition - size * 0.5f, size);
            }
            RefreshLabels();
            RefreshStatus();
            if (previousPosition != _playerPosition || previousForward != _playerForward ||
                previousVisibility != _visibleParchments || previousView != _viewBounds)
            {
                SetVerticesDirty();
            }
        }

        private void RefreshLabels()
        {
            Rect safeArea = rectTransform.rect;
            safeArea.min += Vector2.one * 12f;
            safeArea.max -= Vector2.one * 12f;
            for (int index = 0; index < _parchments.Count; index++)
            {
                TabletMapData.Parchment pickup = _parchments[index];
                Vector2 point = WorldToMap(pickup.Position);
                bool visible = (_visibleParchments & (1 << index)) != 0 && safeArea.Contains(point);
                Text label = _parchmentLabels[index];
                if (label.gameObject.activeSelf != visible)
                {
                    label.gameObject.SetActive(visible);
                }
                if (label.rectTransform.anchoredPosition != point)
                {
                    label.rectTransform.anchoredPosition = point;
                }
            }
            for (int index = 0; index < _roomLabels.Count; index++)
            {
                TabletMapData.Floor floor = _floors[index];
                Vector2 point = WorldToMap(floor.Bounds.center);
                if (_roomLabels[index].rectTransform.anchoredPosition != point)
                {
                    _roomLabels[index].rectTransform.anchoredPosition = point;
                }
            }
        }

        private void RefreshStatus()
        {
            string value = "공장 외부";
            if (_floors.Count == 0)
            {
                value = "공장 지도 미연결";
            }
            else if (_overview)
            {
                int remaining = 0;
                for (int index = 0; index < _parchments.Count; index++)
                {
                    if ((_visibleParchments & (1 << index)) != 0)
                    {
                        remaining++;
                    }
                }
                value = "공장 전체 · 미수집 양피지 " + remaining + "개  ·  N ↑";
            }
            else
            {
                foreach (TabletMapData.Floor floor in _floors)
                {
                    if (floor.Bounds.Contains(_playerPosition))
                    {
                        value = floor.Name;
                    }
                }
            }
            if (_status != null && value != _lastStatus)
            {
                _status.text = value;
                _lastStatus = value;
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect viewport = rectTransform.rect;
            AddRectangle(mesh, viewport, BackgroundColor);
            if (!_initialized || _viewBounds.width <= 0f || _floors.Count == 0)
            {
                return;
            }
            for (float x = Mathf.Ceil(_viewBounds.xMin / 5f) * 5f; x < _viewBounds.xMax; x += 5f)
            {
                float pixelX = WorldToMap(new Vector2(x, 0f)).x;
                AddRectangle(mesh, new Rect(pixelX, viewport.yMin, 0.6f, viewport.height), GridColor);
            }
            for (float z = Mathf.Ceil(_viewBounds.yMin / 5f) * 5f; z < _viewBounds.yMax; z += 5f)
            {
                float pixelY = WorldToMap(new Vector2(0f, z)).y;
                AddRectangle(mesh, new Rect(viewport.xMin, pixelY, viewport.width, 0.6f), GridColor);
            }
            foreach (TabletMapData.Floor floor in _floors)
            {
                AddClippedRectangle(mesh, ToMapRect(floor.Bounds), FloorColor);
            }
            foreach (Rect wall in _walls)
            {
                Rect outline = ToMapRect(wall);
                outline.size = Vector2.Max(outline.size, Vector2.one * (_overview ? 1.8f : 1.3f));
                AddClippedRectangle(mesh, outline, WallColor);
            }
            for (int index = 0; index < _parchments.Count; index++)
            {
                if (!_parchmentLabels[index].gameObject.activeSelf)
                {
                    continue;
                }
                Vector2 point = _parchmentLabels[index].rectTransform.anchoredPosition;
                Vector2 size = _overview ? new Vector2(20f, 25f) : new Vector2(14f, 18f);
                AddRectangle(mesh, new Rect(point - size * 0.5f - Vector2.one * 3f, size + Vector2.one * 6f),
                    new Color(0.20f, 0.58f, 0.90f, 0.32f));
                AddRectangle(mesh, new Rect(point - size * 0.5f, size), PaperColor);
                AddRectangle(mesh, new Rect(point.x + size.x * 0.5f - 4f, point.y + size.y * 0.5f - 5f, 4f, 5f), WallColor);
            }
            Vector2 player = WorldToMap(_playerPosition);
            Rect playerArea = viewport;
            playerArea.min += Vector2.one * 10f;
            playerArea.max -= Vector2.one * 10f;
            player = Vector2.Max(playerArea.min, Vector2.Min(playerArea.max, player));
            AddPlayer(mesh, player);
        }

        private void AddPlayer(VertexHelper mesh, Vector2 position)
        {
            float radius = _overview ? 10f : 8f;
            const int segments = 20;
            Color halo = new Color(PlayerColor.r, PlayerColor.g, PlayerColor.b, 0.19f);
            for (int index = 0; index < segments; index++)
            {
                float first = index * Mathf.PI * 2f / segments;
                float second = (index + 1) * Mathf.PI * 2f / segments;
                AddTriangle(mesh, position, position + new Vector2(Mathf.Cos(first), Mathf.Sin(first)) * radius * 1.7f,
                    position + new Vector2(Mathf.Cos(second), Mathf.Sin(second)) * radius * 1.7f, halo);
            }
            Vector2 right = new Vector2(_playerForward.y, -_playerForward.x);
            AddTriangle(mesh, position + _playerForward * radius,
                position - _playerForward * radius * 0.65f - right * radius * 0.7f,
                position - _playerForward * radius * 0.65f + right * radius * 0.7f, PlayerColor);
        }

        private Vector2 WorldToMap(Vector3 position)
        {
            return WorldToMap(new Vector2(position.x, position.z));
        }

        private Vector2 WorldToMap(Vector2 position)
        {
            Rect viewport = rectTransform.rect;
            return viewport.min + (position - _viewBounds.min) * new Vector2(viewport.width / _viewBounds.width,
                viewport.height / _viewBounds.height);
        }

        private Rect ToMapRect(Rect world)
        {
            return new Rect(WorldToMap(world.min), world.size * (rectTransform.rect.size / _viewBounds.size));
        }

        private void AddClippedRectangle(VertexHelper mesh, Rect rectangle, Color tint)
        {
            Rect viewport = rectTransform.rect;
            Vector2 minimum = Vector2.Max(viewport.min, rectangle.min);
            Vector2 maximum = Vector2.Min(viewport.max, rectangle.max);
            if (minimum.x < maximum.x && minimum.y < maximum.y)
            {
                AddRectangle(mesh, Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y), tint);
            }
        }

        private static void AddRectangle(VertexHelper mesh, Rect rect, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddTriangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }

        private Text CreateLabel(string labelName, string value, int size, Color tint, Vector2 dimensions)
        {
            GameObject label = new GameObject(labelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.layer = gameObject.layer;
            label.transform.SetParent(transform, false);
            Text text = label.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = tint;
            text.text = value;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.one * 0.5f;
            text.rectTransform.anchorMax = Vector2.one * 0.5f;
            text.rectTransform.sizeDelta = dimensions;
            return text;
        }

        private static void ClearLabels(List<Text> labels)
        {
            foreach (Text label in labels)
            {
                if (label != null)
                {
                    label.gameObject.SetActive(false);
                    Destroy(label.gameObject);
                }
            }
            labels.Clear();
        }

    }
}
