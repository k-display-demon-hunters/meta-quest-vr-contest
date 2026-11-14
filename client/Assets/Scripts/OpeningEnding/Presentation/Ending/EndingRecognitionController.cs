using UnityEngine;

namespace Cinematic
{
    internal sealed class EndingRecognitionController
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
        private readonly Transform _photo;
        private readonly MeshRenderer[] _arms;
        private readonly Vector2[] _directions;
        private readonly bool[] _horizontal;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();

        internal EndingRecognitionController(Transform root, Material material, Transform photo)
        {
            _root = root;
            _photo = photo;
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

        internal void ShowModelFocus(EndingFocusTarget target, float elapsed)
        {
            Show(target.Position, new Vector2(Mathf.Max(2.4f, target.Size.x + .18f), .76f),
                Vector2.zero, target.Size, elapsed);
        }

        internal void ShowReleasedFocus(EndingFocusTarget target, float progress) =>
            Draw(target.Position, target.Size * Mathf.Lerp(1f, 1.1f, progress), _photo.localScale.y, 1f, 0f);

        internal void ShowFlyingFocus(EndingFocusTarget source, EndingFocusTarget target, float progress)
        {
            float t = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 position = Vector3.Lerp(source.Position, target.Position, t);
            position.y += .19f * 4f * t * (1f - t);
            position.z -= .08f * 4f * t * (1f - t);
            Draw(position, Vector2.Lerp(source.Size * 1.1f, target.Size * 1.06f, t), _photo.localScale.y, 1f, 0f);
        }

        internal void ShowLockedFocus(EndingFocusTarget target, float elapsed)
        {
            float pulse = 1f - Mathf.Clamp01(elapsed / LockDuration);
            Draw(target.Position, target.Size * (1f + .06f * pulse), _photo.localScale.y, 1f, .18f * pulse);
        }

        internal void Hide() => _root.gameObject.SetActive(false);

        private void Show(Vector3 origin, Vector2 fullSize, Vector2 offset, Vector2 targetSize, float elapsed)
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
            Draw(origin + new Vector3(center.x, center.y, 0f), size, _photo.localScale.y,
                Mathf.SmoothStep(0f, 1f, (elapsed - AppearAt) / FadeDuration), flash);
        }

        private void Draw(Vector3 position, Vector2 size, float frameHeight, float alpha, float flash)
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
