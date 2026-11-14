using UnityEngine;
using UnityEngine.XR;

namespace FactoryEnvironment
{
    public sealed class FactoryTeleportFeedbackController : MonoBehaviour
    {
        private const float DisplaySeconds = 2f;
        private const float ShakeSeconds = 0.22f;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _panel;
        private Transform _view;
        private float _shownAt;

        internal void ShowDenied(Transform view)
        {
            _view = view;
            _shownAt = Time.unscaledTime;
            _panel.anchoredPosition = Vector2.zero;
            PlaceInView();
            _canvas.gameObject.SetActive(true);

            InputDevice controller = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (controller.TryGetHapticCapabilities(out HapticCapabilities capability) && capability.supportsImpulse)
                controller.SendHapticImpulse(0, 0.45f, ShakeSeconds);
        }

        internal void Hide() => _canvas.gameObject.SetActive(false);

        private void LateUpdate()
        {
            if (!_canvas.gameObject.activeSelf) { return; }
            float elapsed = Time.unscaledTime - _shownAt;
            if (elapsed >= DisplaySeconds)
            {
                Hide();
                return;
            }

            PlaceInView();
            float shake = 1f - Mathf.Clamp01(elapsed / ShakeSeconds);
            _panel.anchoredPosition = Vector2.right * (Mathf.Sin(elapsed * 92f) * 14f * shake);
        }

        private void PlaceInView() => _canvas.transform.SetPositionAndRotation(
            _view.position + _view.forward * 0.24f, _view.rotation);
    }
}
