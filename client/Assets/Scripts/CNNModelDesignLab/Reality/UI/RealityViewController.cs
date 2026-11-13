using UI;
using UnityEngine;

namespace RealityLab
{
    public sealed class RealityViewController : MonoBehaviour
    {
        [SerializeField] private Canvas _guideCanvas;
        [SerializeField] private RectTransform _selectionFrame;
        [SerializeField] private Transform _resultPanel;
        [SerializeField] private GameObject _inputPreview;
        [SerializeField] private WorldUIPopupController _help;
        private bool _following;

        internal void FollowSelectionGuide()
        {
            if (_following) { PlaceInView(_guideCanvas.transform); }
        }

        internal void ShowSelectionGuide(Pose pose, Vector2 size)
        {
            if (!_following) { return; }
            Transform guide = _guideCanvas.transform;
            guide.SetPositionAndRotation(pose.position, pose.rotation);
            // The existing corner marks share the guide canvas and surround this rect.
            guide.localScale = new Vector3(
                size.x / _selectionFrame.rect.width,
                size.y / _selectionFrame.rect.height,
                0.001f);
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
            if (_following) { PlaceInView(_guideCanvas.transform); }
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
