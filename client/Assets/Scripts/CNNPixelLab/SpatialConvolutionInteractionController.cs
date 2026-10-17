using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UI
{
    internal sealed class SpatialConvolutionInteractionController : IDisposable
    {
        private static readonly Vector3 KernelTarget = new Vector3(-0.3f, 0.055f, -0.3f);
        private readonly Transform _root;
        private readonly SpatialConvolutionViewBuilder.View _view;
        private readonly XRGrabInteractable _kernelGrab;
        private readonly XRGrabInteractable[] _answers = new XRGrabInteractable[3];
        private readonly Vector3[] _answerHomes = new Vector3[3];
        private readonly UnityAction<SelectExitEventArgs>[] _answerReleased = new UnityAction<SelectExitEventArgs>[3];
        private Action<int> _choose;
        private bool _closing;

        public static Vector3 KernelHome => new Vector3(-1.05f, 0, -0.22f);
        public bool IsAligned { get; private set; }

        public SpatialConvolutionInteractionController(Transform root, SpatialConvolutionViewBuilder.View view)
        {
            _root = root;
            _view = view;
            _kernelGrab = MakeGrab(view.Kernel, new Vector3(0.6f, 0.52f, 0.04f));
            _kernelGrab.selectEntered.AddListener(HandleKernelGrabbed);
            _kernelGrab.selectExited.AddListener(HandleKernelReleased);
            for (int index = 0; index < _answers.Length; index++)
            {
                int option = index;
                _answerHomes[index] = new Vector3(-0.6f + index * 0.38f, -0.58f, -0.15f);
                _answers[index] = MakeGrab(view.Answers[index], new Vector3(0.32f, 0.15f, 0.06f));
                _answerReleased[index] = _ => ReleaseAnswer(option);
                _answers[index].selectExited.AddListener(_answerReleased[index]);
                _answers[index].gameObject.SetActive(false);
            }
        }

        public void PrepareKernel()
        {
            IsAligned = false;
            _kernelGrab.enabled = false;
        }

        public void EnableKernel()
        {
            ResetGrab(_kernelGrab, KernelHome);
            _kernelGrab.enabled = true;
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
        }

        public void DisableKernel()
        {
            _closing = true;
            CancelGrab(_kernelGrab);
            _closing = false;
            _kernelGrab.enabled = false;
        }

        public void ShowAnswers(int[] options, Action<int> choose)
        {
            _choose = choose;
            for (int index = 0; index < _answers.Length; index++)
            {
                _view.AnswerLabels[index].text = options[index].ToString();
                _answers[index].gameObject.SetActive(true);
                ResetGrab(_answers[index], _answerHomes[index]);
            }
        }

        public void HideAnswers()
        {
            _choose = null;
            _closing = true;
            foreach (XRGrabInteractable answer in _answers)
            {
                CancelGrab(answer);
                answer.gameObject.SetActive(false);
            }
            _closing = false;
        }

#if UNITY_EDITOR
        public bool CanDrag(XRGrabInteractable target)
        {
            return target != null && target.isActiveAndEnabled &&
                (target == _kernelGrab || Array.IndexOf(_answers, target) >= 0);
        }

        public void Release(XRGrabInteractable target)
        {
            if (target == _kernelGrab)
            {
                AlignReleasedKernel();
                return;
            }
            int index = Array.IndexOf(_answers, target);
            if (index >= 0) { ReleaseAnswer(index); }
        }
#endif

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

        private void ReleaseAnswer(int option)
        {
            if (_closing || _choose == null) { return; }
            XRGrabInteractable answer = _answers[option];
            bool inside = Vector3.Distance(answer.transform.position, _view.Slot.position) < 0.24f;
            ResetGrab(answer, _answerHomes[option]);
            if (inside) { _choose.Invoke(option); }
        }

        private static XRGrabInteractable MakeGrab(Transform target, Vector3 size)
        {
            target.gameObject.AddComponent<BoxCollider>().size = size;
            Rigidbody body = target.gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            XRGrabInteractable grab = target.gameObject.AddComponent<XRGrabInteractable>();
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

        public void ReleaseInteractions()
        {
            _closing = true;
            CancelGrab(_kernelGrab);
            foreach (XRGrabInteractable answer in _answers) { CancelGrab(answer); }
            _choose = null;
            _closing = false;
        }

        public void Dispose()
        {
            ReleaseInteractions();
            if (_kernelGrab != null)
            {
                _kernelGrab.selectEntered.RemoveListener(HandleKernelGrabbed);
                _kernelGrab.selectExited.RemoveListener(HandleKernelReleased);
                DestroyDetached(_kernelGrab);
            }
            for (int index = 0; index < _answers.Length; index++)
            {
                if (_answers[index] == null) { continue; }
                _answers[index].selectExited.RemoveListener(_answerReleased[index]);
                DestroyDetached(_answers[index]);
            }
        }

        private void DestroyDetached(XRGrabInteractable grab)
        {
            if (!grab.transform.IsChildOf(_root)) { UnityEngine.Object.Destroy(grab.gameObject); }
        }
    }
}
