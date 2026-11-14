using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class EndingVisualController : MonoBehaviour
    {
        private static readonly Vector2 PhotoSize = new Vector2(2.32f, 1.305f);
        private static readonly string[] BasicLayers = { "INPUT", "CONV", "MAX POOL", "FLATTEN", "DENSE", "OUTPUT" };
        private static readonly int CardColor = Shader.PropertyToID("_BaseColor");
        private const float CardWidth = .26f;
        private const float CardSpacing = .288f;

        [SerializeField] private Transform _world, _portal, _timeline, _digit, _factory;
        [SerializeField] private Transform[] _boxes, _pixels, _eraProps;
        [SerializeField] private Transform _network, _modelFrame, _standingMarker, _holdHint;
        [SerializeField] private Material _recognitionMaterial;
        [SerializeField] private Text _year;
        [SerializeField] private Transform[] _networkLayers;
        [SerializeField] private MeshRenderer[] _layerCards;
        [SerializeField] private Text _modelHeading, _testAccuracy;
        [SerializeField] private Text[] _layerLabels;

        private RecognitionFrame _archiveFrame;
        private EndingYearRoll _yearRoll;
        private EndingArchiveTrail _trail;
        private int _visibleLayerCount;
        private Camera _camera;
        private Color _previousBackground;
        private CameraClearFlags _previousClearFlags;
        private bool _quiet;

        internal const float RecognitionLockTime = RecognitionFrame.LockAt;
        internal const float FocusLockDuration = RecognitionFrame.LockDuration;
        internal Vector3 FocusPosition => _boxes[0].position;

        internal void Bind(CinematicModelResult result, Camera camera)
        {
            _camera = camera;
            Transform head = camera.transform;
            // Move the stage around the viewer once; never move the camera or chase head rotation.
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            _world.position = new Vector3(head.position.x, head.position.y - 1.6f, head.position.z);
            _world.rotation = Quaternion.LookRotation(forward.sqrMagnitude > .01f ? forward : Vector3.forward);
            _archiveFrame = new RecognitionFrame(_boxes[0], _recognitionMaterial);
            _modelFrame.gameObject.SetActive(false);

            _portal.localPosition = new Vector3(0, 1.66f, 3f);
            _portal.localScale = new Vector3(PhotoSize.x, PhotoSize.y, 1f);
            _year.fontSize = 56;
            _year.alignment = TextAnchor.MiddleRight;
            _year.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 220f);
            Transform yearCanvas = _year.transform.parent;
            float yearHalfWidth = _year.rectTransform.rect.width * yearCanvas.localScale.x * .5f;
            yearCanvas.localPosition = new Vector3(-PhotoSize.x * .5f - .13f - yearHalfWidth,
                _portal.localPosition.y + PhotoSize.y * .5f - .10f, 3f);

            _yearRoll = _year.gameObject.AddComponent<EndingYearRoll>();
            _yearRoll.Bind(_year);
            BindModelCards(result);
        }

        private void BindModelCards(CinematicModelResult result)
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

        internal EndingArchiveTrail CreateTrail(int panelCount)
        {
            _trail = new EndingArchiveTrail(_world, _portal, _year, _recognitionMaterial, panelCount);
            return _trail;
        }

        private void Update() => _trail?.TickDust(Time.unscaledTime);

        private void OnDestroy() => _trail?.Dispose();

        internal void SetMontageVisible(bool visible)
        {
            _portal.gameObject.SetActive(visible);

        }

        internal Rect FrameArchive(EndingSequenceData.MontageEntry entry, out Rect visibleFocus)
        {
            Rect uv = entry.Clip.UV;
            Rect focus = entry.Focus;
            float frameAspect = PhotoSize.x / PhotoSize.y;
            Texture2D still = entry.Clip.Still;
            float sourceAspect = still != null ? still.width * uv.width / (still.height * uv.height) : frameAspect;
            float width = Mathf.Min(1f, frameAspect / sourceAspect);
            float height = Mathf.Min(1f, sourceAspect / frameAspect);
            float x = CoverOffset(width, focus.xMin, focus.xMax);
            float y = CoverOffset(height, focus.yMin, focus.yMax);

            visibleFocus = Rect.MinMaxRect(
                Mathf.Clamp01((focus.xMin - x) / width), Mathf.Clamp01((focus.yMin - y) / height),
                Mathf.Clamp01((focus.xMax - x) / width), Mathf.Clamp01((focus.yMax - y) / height));
            return new Rect(uv.x + x * uv.width, uv.y + y * uv.height, width * uv.width, height * uv.height);
        }

        private static float CoverOffset(float size, float targetMin, float targetMax)
        {
            // Keep a centered crop where possible, shifting just enough to keep the target in view.
            float min = Mathf.Max(0f, targetMax - size);
            float max = Mathf.Min(1f - size, targetMin);
            return min <= max
                ? Mathf.Clamp((1f - size) * .5f, min, max)
                : Mathf.Clamp((targetMin + targetMax - size) * .5f, 0f, 1f - size);
        }

        internal void HideArchiveFocus() => _boxes[0].gameObject.SetActive(false);

        internal void ShowYear(string previous, string year, float elapsed, float alpha) =>
            _yearRoll.Show(previous, year, elapsed, alpha);

        internal void HideYear() => _year.gameObject.SetActive(false);

        internal readonly struct FrameTarget
        {
            internal readonly Vector3 Position;
            internal readonly Vector2 Size;

            internal FrameTarget(Vector3 position, Vector2 size)
            {
                Position = position;
                Size = size;
            }
        }

        internal FrameTarget ArchiveTarget(Rect focus, float zoom)
        {
            Vector2 offset = (focus.center - Vector2.one * .5f) * zoom;
            Vector3 world = _portal.TransformPoint(new Vector3(offset.x, offset.y, -.045f));
            Vector2 size = new Vector2(focus.width * _portal.lossyScale.x, focus.height * _portal.lossyScale.y) * zoom;
            return new FrameTarget(_world.InverseTransformPoint(world), size);
        }

        internal FrameTarget ModelTarget
        {
            get
            {
                float width = (_visibleLayerCount - 1) * CardSpacing + CardWidth;
                Vector3 world = _network.TransformPoint(new Vector3(0f, 0f, -.055f));
                return new FrameTarget(_world.InverseTransformPoint(world), new Vector2(width + .06f, .28f) * _network.localScale.x);
            }
        }

        internal void ShowModelFocus(float elapsed)
        {
            FrameTarget target = ModelTarget;
            _archiveFrame.Show(target.Position, new Vector2(Mathf.Max(2.4f, target.Size.x + .18f), .76f),
                Vector2.zero, target.Size, elapsed);
        }

        internal void ShowReleasedFocus(FrameTarget target, float progress) =>
            _archiveFrame.Draw(target.Position, target.Size * Mathf.Lerp(1f, 1.1f, progress), PhotoSize.y, 1f, 0f);

        internal void ShowFlyingFocus(FrameTarget source, FrameTarget target, float progress)
        {
            float t = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 position = Vector3.Lerp(source.Position, target.Position, t);
            position.y += .19f * 4f * t * (1f - t);
            position.z -= .08f * 4f * t * (1f - t);
            _archiveFrame.Draw(position, Vector2.Lerp(source.Size * 1.1f, target.Size * 1.06f, t), PhotoSize.y, 1f, 0f);
        }

        internal void ShowLockedFocus(FrameTarget target, float elapsed)
        {
            float pulse = 1f - Mathf.Clamp01(elapsed / FocusLockDuration);
            _archiveFrame.Draw(target.Position, target.Size * (1f + .06f * pulse), PhotoSize.y, 1f, .18f * pulse);
        }

        internal void ShowModel(bool visible, float elapsed)
        {
            _network.gameObject.SetActive(visible);
            if (!visible) { return; }

            _network.localScale = Vector3.one * Mathf.Lerp(.84f, 1f, Mathf.SmoothStep(0f, 1f, elapsed / .6f));
            for (int i = 0; i < _visibleLayerCount; i++)
            {
                _networkLayers[i].gameObject.SetActive(elapsed >= i * .13f);
            }

        }

        internal void SetQuiet(bool quiet)
        {
            if (quiet == _quiet) { return; }
            _quiet = quiet;
            if (quiet)
            {
                _previousBackground = _camera.backgroundColor;
                _previousClearFlags = _camera.clearFlags;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = Color.black;
            }
            else if (_camera != null)
            {
                _camera.backgroundColor = _previousBackground;
                _camera.clearFlags = _previousClearFlags;
            }

            _standingMarker.gameObject.SetActive(!quiet);
            _holdHint.gameObject.SetActive(!quiet);
        }

        private void OnDisable()
        {
            if (_quiet && _camera != null)
            {
                _camera.backgroundColor = _previousBackground;
                _camera.clearFlags = _previousClearFlags;
            }
        }

        internal void Finish()
        {
            SetQuiet(false);
            _trail?.HidePhotos();
            foreach (Transform target in new[] { _portal, _timeline, _digit, _factory, _network })
            { target.gameObject.SetActive(false); }
            _year.gameObject.SetActive(false);
            foreach (Transform target in _boxes) { target.gameObject.SetActive(false); }
            foreach (Transform target in _pixels) { target.gameObject.SetActive(false); }
            foreach (Transform target in _eraProps) { target.gameObject.SetActive(false); }
        }

        private sealed class RecognitionFrame
        {
            private const float AppearAt = .3f;
            private const float FadeDuration = .2f;
            private const float CloseAt = AppearAt + FadeDuration + .4f;
            private const float CloseDuration = .6f;
            internal const float LockAt = CloseAt + CloseDuration;
            internal const float LockDuration = .15f;

            private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
            private static readonly Color Green = new Color32(0x3D, 0xFF, 0x8A, 0xFF);

            private readonly Transform _root;
            private readonly MeshRenderer[] _arms;
            private readonly Vector2[] _directions;
            private readonly bool[] _horizontal;
            private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();

            internal RecognitionFrame(Transform root, Material material)
            {
                _root = root;
                _arms = root.GetComponentsInChildren<MeshRenderer>(true);
                _directions = new Vector2[_arms.Length];
                _horizontal = new bool[_arms.Length];
                for (int i = 0; i < _arms.Length; i++)
                {
                    MeshRenderer arm = _arms[i];
                    Vector3 center = arm.transform.localPosition;
                    _directions[i] = new Vector2(Mathf.Sign(center.x), Mathf.Sign(center.y));
                    _horizontal[i] = arm.transform.localScale.x > arm.transform.localScale.y;
                    arm.sharedMaterial = material;
                }
            }

            internal void Show(Vector3 origin, Vector2 fullSize, Vector2 offset, Vector2 targetSize, float elapsed)
            {
                _root.gameObject.SetActive(elapsed >= AppearAt);
                if (elapsed < AppearAt) { return; }

                float close = Mathf.Clamp01((elapsed - CloseAt) / CloseDuration);
                float remaining = close - 1f;
                float easeOut = 1f + remaining * remaining * (1.8f * remaining + .8f);
                // Blend from the overshoot into the 6% lock pulse without a size discontinuity.
                float lockPulse = 1f - Mathf.Clamp01((elapsed - LockAt) / LockDuration);
                float lockScale = 1f + .06f * close * close * close * lockPulse;
                Vector2 size = Vector2.Max(targetSize * .85f,
                    Vector2.LerpUnclamped(fullSize * .94f, targetSize, easeOut)) * lockScale;
                Vector2 center = Vector2.LerpUnclamped(Vector2.zero, offset, easeOut);
                float flash = elapsed >= LockAt ? lockPulse * .18f : 0f;
                Draw(origin + new Vector3(center.x, center.y, 0f), size, PhotoSize.y,
                    Mathf.SmoothStep(0f, 1f, (elapsed - AppearAt) / FadeDuration), flash);
            }

            internal void Draw(Vector3 position, Vector2 size, float frameHeight, float alpha, float flash)
            {
                _root.gameObject.SetActive(true);
                _root.localPosition = position;
                _root.localScale = Vector3.one;
                float armLength = Mathf.Min(size.x, size.y) * .25f;
                float thickness = Mathf.Min(frameHeight * .012f, armLength);
                Color tint = Green * (1f + flash);
                tint.a = alpha;
                _properties.SetColor(BaseColor, tint);

                for (int i = 0; i < _arms.Length; i++)
                {
                    MeshRenderer renderer = _arms[i];
                    Transform arm = renderer.transform;
                    Vector2 direction = _directions[i];
                    float x = direction.x * size.x * .5f;
                    float y = direction.y * size.y * .5f;
                    bool horizontal = _horizontal[i];
                    float verticalLength = armLength - thickness * .5f;
                    arm.localPosition = horizontal
                        ? new Vector3(x - direction.x * (armLength - thickness) * .5f, y, 0f)
                        : new Vector3(x, y - direction.y * (verticalLength + thickness) * .5f, 0f);
                    arm.localScale = horizontal ? new Vector3(armLength, thickness, .004f) : new Vector3(thickness, verticalLength, .004f);
                    renderer.SetPropertyBlock(_properties);
                }
            }
        }
    }
}
