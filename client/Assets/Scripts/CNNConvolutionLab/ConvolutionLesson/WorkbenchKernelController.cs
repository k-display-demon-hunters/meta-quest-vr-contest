using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNConvolution
{
    // Workbench implementation: physical input and presentation, without lesson-stage state.
    internal sealed class WorkbenchKernelController : IDisposable
    {
        private readonly Transform _root;
        private readonly RgbVolumeController.Geometry _view;
        private readonly Text _hint;
        private readonly XRGrabInteractable _grab;
        private readonly Rigidbody _body;
        private readonly MaterialPropertyBlock _handleProperties = new MaterialPropertyBlock();
        private bool _cancellingSelection;

        public bool IsSelected => _grab.isSelected;
        public bool IsHovered => _grab.isHovered;
        public event Action Grabbed;
        public event Action Released;

        public WorkbenchKernelController(Transform root, RgbVolumeController.Geometry view, Text hint)
        {
            _root = root;
            _view = view;
            _hint = hint;
            BoxCollider collider = view.Kernel.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.60f, 0.54f, 0.04f);
            _body = view.Kernel.gameObject.AddComponent<Rigidbody>();
            _body.useGravity = false;
            _body.isKinematic = true;
            _grab = view.Kernel.gameObject.AddComponent<XRGrabInteractable>();
            _grab.colliders.Clear();
            _grab.colliders.Add(collider);
            _grab.throwOnDetach = false;
            _grab.retainTransformParent = false;
            _grab.trackRotation = false;
            _grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            BoxCollider handle = view.Kernel.gameObject.AddComponent<BoxCollider>();
            handle.center = new Vector3(0, 0.32f, 0);
            handle.size = new Vector3(0.26f, 0.10f, 0.09f);
            _grab.colliders.Add(handle);
            _grab.selectEntered.AddListener(HandleGrabbed);
            _grab.selectExited.AddListener(HandleReleased);
        }

        public void RestoreParent()
        {
            // XR detaches the object while held. Keep its world scale when returning it.
            _view.Kernel.SetParent(_root, true);
        }

        public void SetPose(Pose pose)
        {
            _view.Kernel.SetPositionAndRotation(pose.position, pose.rotation);
            _body.position = pose.position;
            _body.rotation = pose.rotation;
        }

        public void SetGrabEnabled(bool enabled)
        {
            _grab.enabled = enabled;
            foreach (Collider collider in _grab.colliders) { collider.enabled = enabled; }
        }

        public void ShowPlacement(string message, bool ready, bool completed, bool showGrip, bool emphasize)
        {
            _view.DropTarget.gameObject.SetActive(true);
            _view.PlacementDashed.gameObject.SetActive(!ready);
            _view.PlacementSolid.gameObject.SetActive(ready);
            _view.PlacementCheck.gameObject.SetActive(completed);
            _view.GripArrow.gameObject.SetActive(showGrip);
            _view.KernelTitle.gameObject.SetActive(showGrip);
            _hint.gameObject.SetActive(true);
            _hint.text = message;
            Color accent = ready ? new Color(1f, 0.86f, 0.57f) : new Color(0.35f, 0.78f, 0.85f);
            _hint.color = ready ? accent : Color.white;
            TintHandle(accent, emphasize ? 0.35f : 0.12f);
        }

        public void HidePlacement()
        {
            _view.DropTarget.gameObject.SetActive(false);
            _view.GripArrow.gameObject.SetActive(false);
            _hint.gameObject.SetActive(false);
            TintHandle(new Color(0.47f, 0.38f, 0.24f), 0);
        }

        public void CancelGrab()
        {
            if (_grab == null) { return; }
            if (_grab.isSelected && _grab.interactionManager != null)
            {
                // Assistance and shutdown are not user releases, regardless of OnDisable order.
                _cancellingSelection = true;
                _grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_grab);
                _cancellingSelection = false;
            }
            if (_grab.transform.parent != _root)
            {
                if (_root.gameObject.activeInHierarchy) { RestoreParent(); }
                else { _grab.gameObject.SetActive(false); }
            }
        }

        public void ReleaseInteractions()
        {
            CancelGrab();
            SetGrabEnabled(false);
            HidePlacement();
        }

        public void Dispose()
        {
            // Destruction can follow child destruction; do not repaint the workbench here.
            CancelGrab();
            _grab.selectEntered.RemoveListener(HandleGrabbed);
            _grab.selectExited.RemoveListener(HandleReleased);
            if (!_grab.transform.IsChildOf(_root)) { UnityEngine.Object.Destroy(_grab.gameObject); }
        }

        private void TintHandle(Color color, float emission)
        {
            _handleProperties.SetColor("_BaseColor", color);
            _handleProperties.SetColor("_EmissionColor", color * emission);
            foreach (MeshRenderer groove in _view.GripAccents) { groove.SetPropertyBlock(_handleProperties); }
        }

        private void HandleGrabbed(SelectEnterEventArgs args) { Grabbed?.Invoke(); }
        private void HandleReleased(SelectExitEventArgs args)
        {
            if (!_cancellingSelection) { Released?.Invoke(); }
        }
    }
}
