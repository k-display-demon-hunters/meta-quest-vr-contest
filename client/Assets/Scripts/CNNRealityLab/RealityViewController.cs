using UnityEngine;

namespace RealityLab
{
    public sealed class RealityViewController : MonoBehaviour
    {
        [SerializeField] private Canvas _guideCanvas;
        [SerializeField] private Transform _resultPanel;
        [SerializeField] private GameObject _inputPreview;
        private bool _following = true;

        private void LateUpdate()
        {
            if (_following) { PlaceInView(_guideCanvas.transform); }
        }

        public void ShowStage(RealityStage stage)
        {
            bool showResult = stage == RealityStage.Result || stage == RealityStage.Summary;
            if (showResult && !_resultPanel.gameObject.activeSelf) { PlaceInView(_resultPanel); }
            _resultPanel.gameObject.SetActive(showResult);
            _guideCanvas.gameObject.SetActive(!showResult);
            _inputPreview.SetActive(stage == RealityStage.Captured || stage == RealityStage.Recognizing);
            _following = stage == RealityStage.Ready || stage == RealityStage.AwaitingModel;
        }

        private void PlaceInView(Transform target)
        {
            Transform view = _guideCanvas.worldCamera.transform;
            target.SetPositionAndRotation(view.position + view.forward * 1.2f, view.rotation);
        }
    }
}
