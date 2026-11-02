using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNConvolution
{
    internal sealed class KernelInteractionController : IDisposable
    {
        private const float PlacementRadius = 0.3f;
        private const float PlacementDuration = 0.18f;
        private const float ReturnDuration = 0.3f;
        private enum PlacementState { Hidden, Waiting, Hovered, Moving, Ready, Returning, Settling, Complete }
        private readonly Transform _home;
        private readonly Transform _target;
        private readonly Transform _root;
        private readonly RgbVolumeController.Geometry _view;
        private readonly XRGrabInteractable _kernelGrab;
        private readonly Rigidbody _body;
        private readonly Text _hint;
        private readonly MaterialPropertyBlock _handleProperties = new MaterialPropertyBlock();
        private PlacementState _state = PlacementState.Hidden;
        private Transform _motionTarget;
        private Vector3 _motionStart;
        private Quaternion _motionRotation;
        private float _motionTime;
        private bool _active;
        private bool _closing;
        public bool IsAligned { get; private set; }
        private bool IsWithinTarget => Vector3.Distance(_view.Kernel.position, _target.position) <= PlacementRadius;
        public KernelInteractionController(Transform root, RgbVolumeController.Geometry view, Transform home, Transform target, Text hint)
        {
            _root = root;
            _view = view;
            _home = home;
            _target = target;
            _hint = hint;
            _kernelGrab = MakeGrab(view.Kernel, new Vector3(0.6f, 0.52f, 0.04f));
            _body = _kernelGrab.GetComponent<Rigidbody>();
            BoxCollider handle = view.Kernel.gameObject.AddComponent<BoxCollider>();
            handle.center = new Vector3(0, -0.34f, 0);
            handle.size = new Vector3(0.3f, 0.14f, 0.09f);
            _kernelGrab.colliders.Add(handle);
            _kernelGrab.selectEntered.AddListener(HandleKernelGrabbed);
            _kernelGrab.selectExited.AddListener(HandleKernelReleased);
        }
        public void PrepareKernel()
        {
            DisableKernel();
        }
        public void EnableKernel()
        {
            ResetGrab(_kernelGrab, _home);
            _active = true;
            SetGrabEnabled(true);
            ShowState(PlacementState.Waiting);
        }
        public void AlignKernel()
        {
            if (!_active || IsAligned || _motionTarget == _target) { return; }
            // Assistance also works while held; cancellation is not a user release.
            _closing = true;
            CancelGrab(_kernelGrab);
            _closing = false;
            BeginMotion(_target);
        }
        public void LockAlignedKernel()
        {
            _active = false;
            SetGrabEnabled(false);
        }
        public void DisableKernel()
        {
            _closing = true;
            CancelGrab(_kernelGrab);
            _closing = false;
            _active = false;
            _motionTarget = null;
            IsAligned = false;
            SetGrabEnabled(false);
            HidePlacementFeedback();
        }
        private void HandleKernelGrabbed(SelectEnterEventArgs args)
        {
            IsAligned = false;
            RefreshState();
        }
        private void HandleKernelReleased(SelectExitEventArgs args)
        {
            AlignReleasedKernel();
        }
        private void AlignReleasedKernel()
        {
            if (_closing || !_active) { return; }
            // Use the same predicate for the preview and the actual release.
            BeginMotion(IsWithinTarget ? _target : _home);
        }

        public void UpdatePlacement(float deltaTime)
        {
            if (!_active) { return; }
            if (_motionTarget != null)
            {
                _motionTime += deltaTime;
                float duration = _motionTarget == _target ? PlacementDuration : ReturnDuration;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_motionTime / duration));
                SetPose(Vector3.Lerp(_motionStart, _motionTarget.position, t),
                    Quaternion.Slerp(_motionRotation, _motionTarget.rotation, t));
                if (_motionTime >= duration)
                {
                    IsAligned = _motionTarget == _target;
                    ResetGrab(_kernelGrab, _motionTarget);
                    _motionTarget = null;
                    if (!IsAligned) { SetGrabEnabled(true); }
                }
            }
            RefreshState();
        }

        private void BeginMotion(Transform destination)
        {
            // XR detaches the object while held. Preserve its world size when returning it.
            _view.Kernel.SetParent(_root, true);
            _motionStart = _view.Kernel.position;
            _motionRotation = _view.Kernel.rotation;
            _motionTarget = destination;
            _motionTime = 0;
            SetGrabEnabled(false);
            RefreshState();
        }

        private void SetPose(Vector3 position, Quaternion rotation)
        {
            _view.Kernel.SetPositionAndRotation(position, rotation);
            _body.position = position;
            _body.rotation = rotation;
        }

        private void SetGrabEnabled(bool enabled)
        {
            _kernelGrab.enabled = enabled;
            foreach (Collider collider in _kernelGrab.colliders) { collider.enabled = enabled; }
        }

        private void RefreshState()
        {
            if (IsAligned) { ShowState(PlacementState.Complete); }
            else if (_motionTarget != null) { ShowState(_motionTarget == _target ? PlacementState.Settling : PlacementState.Returning); }
            else if (_kernelGrab.isSelected) { ShowState(IsWithinTarget ? PlacementState.Ready : PlacementState.Moving); }
            else { ShowState(_kernelGrab.isHovered ? PlacementState.Hovered : PlacementState.Waiting); }
        }

        public void HidePlacementFeedback()
        {
            // Also enforce this on initial preparation, before any state transition.
            _state = PlacementState.Hidden;
            _view.DropTarget.gameObject.SetActive(false);
            _view.GripArrow.gameObject.SetActive(false);
            _hint.gameObject.SetActive(false);
            TintHandle(new Color(0.47f, 0.38f, 0.24f), 0);
        }

        private void ShowState(PlacementState state)
        {
            if (_state == state) { return; }
            _state = state;
            bool ready = state == PlacementState.Ready || state == PlacementState.Settling || state == PlacementState.Complete;
            _view.DropTarget.gameObject.SetActive(true);
            _view.PlacementDashed.gameObject.SetActive(!ready);
            _view.PlacementSolid.gameObject.SetActive(ready);
            _view.PlacementCheck.gameObject.SetActive(state == PlacementState.Complete);
            _view.GripArrow.gameObject.SetActive(state == PlacementState.Waiting || state == PlacementState.Hovered);
            _view.KernelTitle.gameObject.SetActive(state == PlacementState.Waiting || state == PlacementState.Hovered);
            _hint.gameObject.SetActive(true);
            switch (state)
            {
                case PlacementState.Waiting: _hint.text = "필터 손잡이를 Grip으로 잡아 주세요."; break;
                case PlacementState.Hovered: _hint.text = "Grip을 누른 채 필터를 옮겨 주세요."; break;
                case PlacementState.Moving: _hint.text = "점선 테두리 안으로 옮겨 주세요."; break;
                case PlacementState.Ready: _hint.text = "여기에 놓으면 배치돼요."; break;
                case PlacementState.Returning: _hint.text = "거치대로 돌아가요. 다시 잡아 주세요."; break;
                case PlacementState.Settling: _hint.text = "필터를 맞추고 있어요."; break;
                case PlacementState.Complete: _hint.text = "배치 완료 · 같은 위치끼리 계산해요."; break;
            }
            Color accent = ready ? new Color(1f, 0.86f, 0.57f) : new Color(0.35f, 0.78f, 0.85f);
            _hint.color = ready ? accent : Color.white;
            TintHandle(accent, state == PlacementState.Hovered || state == PlacementState.Ready ? 0.35f : 0.12f);
        }

        private void TintHandle(Color color, float emission)
        {
            _handleProperties.SetColor("_BaseColor", color);
            _handleProperties.SetColor("_EmissionColor", color * emission);
            foreach (MeshRenderer groove in _view.GripAccents) { groove.SetPropertyBlock(_handleProperties); }
        }
        private static XRGrabInteractable MakeGrab(Transform target, Vector3 size)
        {
            BoxCollider collider = target.gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            Rigidbody body = target.gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            XRGrabInteractable grab = target.gameObject.AddComponent<XRGrabInteractable>();
            grab.colliders.Clear();
            grab.colliders.Add(collider);
            grab.throwOnDetach = false;
            grab.retainTransformParent = false;
            grab.trackRotation = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            return grab;
        }
        private void ResetGrab(XRGrabInteractable grab, Transform anchor)
        {
            grab.transform.SetParent(_root, true);
            grab.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            Rigidbody body = grab.GetComponent<Rigidbody>();
            body.position = grab.transform.position;
            body.rotation = grab.transform.rotation;
        }
        private void CancelGrab(XRGrabInteractable grab)
        {
            if (grab == null) { return; }
            if (grab.isSelected && grab.interactionManager != null)
            {
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            }
            if (grab.transform.parent != _root)
            {
                if (_root.gameObject.activeInHierarchy) { grab.transform.SetParent(_root, true); }
                else { grab.gameObject.SetActive(false); }
            }
        }
        private void DestroyDetached(XRGrabInteractable grab)
        {
            if (!grab.transform.IsChildOf(_root)) { UnityEngine.Object.Destroy(grab.gameObject); }
        }
        public void ReleaseInteractions()
        {
            // OnDisable hides the feedback; disposal must not touch destroyed child renderers.
            _active = false;
            _motionTarget = null;
            _closing = true;
            CancelGrab(_kernelGrab);
            _closing = false;
        }
        public void Dispose()
        {
            ReleaseInteractions();
            _kernelGrab.selectEntered.RemoveListener(HandleKernelGrabbed);
            _kernelGrab.selectExited.RemoveListener(HandleKernelReleased);
            DestroyDetached(_kernelGrab);
        }
    }
}
