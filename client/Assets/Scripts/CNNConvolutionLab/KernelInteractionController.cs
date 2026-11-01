using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNConvolution
{
    internal sealed class KernelInteractionController : IDisposable
    {
        private static readonly Vector3 KernelTarget = new Vector3(-0.3f, 0.055f, -0.3f);
        private readonly Transform _root;
        private readonly RgbVolumeController.Geometry _view;
        private readonly XRGrabInteractable _kernelGrab;
        private bool _closing;
        public static Vector3 KernelHome => new Vector3(-1.05f, 0, -0.22f);
        public bool IsAligned { get; private set; }
        public bool IsHeld => _kernelGrab.isSelected;
        public KernelInteractionController(Transform root, RgbVolumeController.Geometry view)
        {
            _root = root;
            _view = view;
            _kernelGrab = MakeGrab(view.Kernel, new Vector3(0.6f, 0.52f, 0.04f));
            _kernelGrab.selectEntered.AddListener(HandleKernelGrabbed);
            _kernelGrab.selectExited.AddListener(HandleKernelReleased);
        }
        public void PrepareKernel()
        {
            IsAligned = false;
            _kernelGrab.enabled = false;
            foreach (Collider collider in _kernelGrab.colliders) { collider.enabled = false; }
        }
        public void EnableKernel()
        {
            ResetGrab(_kernelGrab, KernelHome);
            _kernelGrab.enabled = true;
            foreach (Collider collider in _kernelGrab.colliders) { collider.enabled = true; }
        }
        public void AlignKernel()
        {
            if (_kernelGrab.isSelected) { return; }
            ResetGrab(_kernelGrab, KernelTarget);
            IsAligned = true;
        }
        public void LockAlignedKernel()
        {
            _closing = true;
            CancelGrab(_kernelGrab);
            _closing = false;
            AlignKernel();
            _kernelGrab.enabled = false;
            foreach (Collider collider in _kernelGrab.colliders) { collider.enabled = false; }
        }
        public void DisableKernel()
        {
            _closing = true;
            CancelGrab(_kernelGrab);
            _closing = false;
            _kernelGrab.enabled = false;
            foreach (Collider collider in _kernelGrab.colliders) { collider.enabled = false; }
        }
        private void HandleKernelGrabbed(SelectEnterEventArgs args)
        {
            IsAligned = false;
        }
        private void HandleKernelReleased(SelectExitEventArgs args)
        {
            AlignReleasedKernel();
        }
        private void AlignReleasedKernel()
        {
            if (_closing) { return; }
            // The target stays in front of the input so both numbers remain visible.
            if (Vector3.Distance(_view.Kernel.position, _root.TransformPoint(KernelTarget)) < 0.3f)
            {
                AlignKernel();
            }
            else { ResetGrab(_kernelGrab, KernelHome); }
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
        private void ResetGrab(XRGrabInteractable grab, Vector3 position)
        {
            grab.transform.SetParent(_root, false);
            grab.transform.localPosition = position;
            grab.transform.localRotation = Quaternion.identity;
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
            _closing = true; CancelGrab(_kernelGrab); _closing = false;
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
