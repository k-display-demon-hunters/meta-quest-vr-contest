using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UI
{
    public sealed class FilterAlignmentController : MonoBehaviour
    {
        private RgbVolumeController _spatial;
        private LessonUIController _ui;
        private Transform _filter;
        private XRGrabInteractable _filterGrab;
        private bool _aligned, _closing;
        public bool HasPendingAction { get; private set; }
        private static readonly Vector3 FilterTarget = new Vector3(-0.38f, 0, 0.15f);
        public void Initialize(RgbVolumeController spatial, LessonUIController ui)
        {
            _spatial = spatial;
            _ui = ui;
            _filter = new GameObject("One 3x3x3 filter").transform;
            _filter.SetParent(transform, false);
            _filterGrab = Grab(_filter, new Vector3(0.6f, 0.52f, 1.5f), new Vector3(0.08f, 0.055f, 0.55f));
            if (isActiveAndEnabled) { _filterGrab.selectExited.AddListener(Released); }
        }
        public IEnumerator Run()
        {
            _ui.Title = "필터 하나의 입체 구조 맞추기";
            _ui.Explanation = "필터 하나는 R·G·B에 대응하는 고정 가중치 27개로 이루어져 있어요.";
            _ui.Guide = "정면에서는 가로·세로 위치를, 옆면에서는 색 성분의 순서를 확인하세요.\n\n같은 칸을 가리키면 RGB의 대응 위치와 입력–가중치 연결이 표시돼요.\n\n세 층 전체를 잡아 입력에 맞추세요.";
            _spatial.PrepareFilterDisplay();
            yield return FitWholeFilter();
            yield return _ui.WaitForNext("RGB 대응 관계 확인 완료");
        }
        public IEnumerator FitWholeFilter()
        {
            _spatial.StopLayerInteraction();
            StopInteraction();
            _aligned = false;
            _filter.gameObject.SetActive(true);
            _filter.localPosition = new Vector3(-1.65f, 0, 0.15f);
            _filter.localRotation = Quaternion.identity;
            _spatial.ArrangeWholeFilter(_filter);
            EnableGrab(_filterGrab);
            HasPendingAction = true;
            _spatial.ExplorePairs();
            _spatial.ShowSpatialInstruction("왼쪽의 3층 가중치 묶음 전체가 필터 하나예요.\n묶음을 잡아 오른쪽 RGB 입력에 맞춰 놓으세요. 정면과 옆면에서 위치를 확인하세요.");
            yield return new WaitUntil(() => _aligned);
            HasPendingAction = false;
            StopGrab(_filterGrab);
            _spatial.ReleaseWholeFilter();
            _spatial.ShowSpatialInstruction("R에는 R 가중치, G에는 G, B에는 B가 대응해요.\n3 × 3 × 3 = 27쌍의 곱을 더해 출력 하나를 만듭니다.");
        }
        private void Released(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
        {
            if (_closing) { return; }
            _filter.SetParent(transform, true);
            _aligned = _spatial.IsWholeFilterAligned();
            _filter.localPosition = _aligned ? FilterTarget : new Vector3(-1.65f, 0, 0.15f);
            _filter.localRotation = Quaternion.identity;
            if (!_aligned) { _spatial.ShowSpatialInstruction("세 색의 순서와 가로·세로 위치를 맞춰 다시 놓으세요."); }
            Rigidbody body = _filterGrab.GetComponent<Rigidbody>();
            body.position = _filter.position; body.rotation = _filter.rotation;
        }
        public bool CompletePendingAction()
        {
            if (!HasPendingAction) { return false; }
            _closing = true;
            StopGrab(_filterGrab);
            _filter.localPosition = FilterTarget;
            _filter.localRotation = Quaternion.identity;
            _aligned = true; HasPendingAction = false; _closing = false;
            return true;
        }
        public void StopInteraction()
        {
            HasPendingAction = false; _closing = true;
            StopGrab(_filterGrab); _closing = false;
        }
        private void OnEnable()
        {
            if (_filterGrab != null) { _filterGrab.selectExited.AddListener(Released); }
        }
        private void OnDisable()
        {
            if (_filterGrab != null) { _filterGrab.selectExited.RemoveListener(Released); }
            StopInteraction();
        }
        private void OnDestroy() { DestroyDetached(_filterGrab); }
        private void StopGrab(XRGrabInteractable grab)
        {
            if (grab == null) { return; }
            if (grab.isSelected && grab.interactionManager != null)
            {
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            }
            if (grab.transform.parent != transform)
            {
                if (gameObject.activeInHierarchy) { grab.transform.SetParent(transform, true); }
                else { grab.gameObject.SetActive(false); }
            }
            grab.enabled = false;
            foreach (Collider collider in grab.colliders) { collider.enabled = false; }
        }
        private static void EnableGrab(XRGrabInteractable grab)
        {
            foreach (Collider collider in grab.colliders) { collider.enabled = true; }
            grab.enabled = true;
        }
        private static XRGrabInteractable Grab(Transform target, Vector3 size, Vector3 center)
        {
            BoxCollider collider = target.gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = center;
            Rigidbody body = target.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            XRGrabInteractable grab = target.gameObject.AddComponent<XRGrabInteractable>();
            grab.colliders.Clear();
            grab.colliders.Add(collider);
            grab.throwOnDetach = false;
            grab.retainTransformParent = false;
            grab.trackRotation = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            return grab;
        }
        private void DestroyDetached(XRGrabInteractable grab)
        {
            if (grab != null && !grab.transform.IsChildOf(transform)) { Destroy(grab.gameObject); }
        }
    }
}
