using UI;
using UnityEngine;

namespace RealityLab
{
    public sealed class RealityViewController : MonoBehaviour
    {
        [SerializeField] private Canvas _guideCanvas;
        [SerializeField] private RectTransform _selectionFrame;
        [SerializeField] private RectTransform _guideHeader;
        [SerializeField] private RectTransform _guideFooter;
        [SerializeField] private Transform _resultPanel;
        [SerializeField] private GameObject _inputPreview;
        [SerializeField] private WorldUIPopupController _help;
        private bool _following;
        private readonly Vector3[] _frameCorners = new Vector3[4];
        private const float FrameContentGap = 32f;

        internal void FollowSelectionGuide()
        {
            if (!_following) { return; }
            PlaceInView(_guideCanvas.transform);
            ArrangeGuideContent();
        }

        internal void ShowSelectionGuide(Pose pose, Vector2 size)
        {
            if (!_following) { return; }
            _selectionFrame.SetPositionAndRotation(pose.position, pose.rotation);
            // Only the frame and its preview follow the image bounds. Keep controls
            // at the guide canvas's authored scale and head-relative position.
            Vector3 parentScale = _selectionFrame.parent.lossyScale;
            _selectionFrame.localScale = new Vector3(
                size.x / (_selectionFrame.rect.width * parentScale.x),
                size.y / (_selectionFrame.rect.height * parentScale.y),
                1f);
            ArrangeGuideContent();
        }

        private void ArrangeGuideContent()
        {
            Transform guide = _guideCanvas.transform;
            Vector3 eye = _guideCanvas.worldCamera.transform.position;
            float planeDistance = Vector3.Dot(guide.forward, guide.position - eye);
            _selectionFrame.GetWorldCorners(_frameCorners);
            Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Vector3 corner in _frameCorners)
            {
                // Project onto the controls' plane from the viewer, so depth and
                // lens rotation cannot make the frame overlap the text visually.
                Vector3 direction = corner - eye;
                Vector3 projected = eye + direction * (planeDistance / Vector3.Dot(guide.forward, direction));
                Vector2 point = guide.InverseTransformPoint(projected);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
            float centerX = (minimum.x + maximum.x) * 0.5f;
            _guideHeader.anchoredPosition = new Vector2(centerX,
                maximum.y + FrameContentGap + _guideHeader.rect.height * 0.5f);
            _guideFooter.anchoredPosition = new Vector2(centerX,
                minimum.y - FrameContentGap - _guideFooter.rect.height * 0.5f);
        }

        internal void ShowStage(RealityStage stage)
        {
            bool showResult = stage == RealityStage.Result;
            if (showResult) { _help.Hide(); }
            if (showResult && !_resultPanel.gameObject.activeSelf) { PlaceInView(_resultPanel); }
            _resultPanel.gameObject.SetActive(showResult);
            _guideCanvas.gameObject.SetActive(!showResult);
            _inputPreview.SetActive(stage == RealityStage.Captured || stage == RealityStage.Recognizing);
            _following = stage == RealityStage.Ready;
            if (_following) { FollowSelectionGuide(); }
        }

        public void ShowHelp()
        {
            PlaceInView(_help.transform, 1f);
            _help.Show();
        }

        private void PlaceInView(Transform target, float distance = 1.2f)
        {
            Transform view = _guideCanvas.worldCamera.transform;
            target.SetPositionAndRotation(view.position + view.forward * distance, view.rotation);
        }
    }
}
