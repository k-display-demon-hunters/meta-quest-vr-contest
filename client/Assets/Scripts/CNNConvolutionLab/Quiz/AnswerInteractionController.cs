using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNConvolution
{
    internal sealed class AnswerInteractionController : IDisposable
    {
        private readonly Transform _root;
        private readonly RgbVolumeController.Geometry _view;
        private readonly XRGrabInteractable[] _answers = new XRGrabInteractable[3];
        private readonly Vector3[] _answerHomes = new Vector3[3];
        private readonly UnityAction<SelectExitEventArgs>[] _answerReleased = new UnityAction<SelectExitEventArgs>[3];
        private Action<int> _choose;
        private bool _closing;
        public AnswerInteractionController(Transform root, RgbVolumeController.Geometry view)
        {
            _root = root;
            _view = view;
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
        private void DestroyDetached(XRGrabInteractable grab)
        {
            if (!grab.transform.IsChildOf(_root)) { UnityEngine.Object.Destroy(grab.gameObject); }
        }
        public void ReleaseInteractions() { HideAnswers(); }
        public void Dispose()
        {
            HideAnswers();
            for (int i = 0; i < _answers.Length; i++)
            {
                _answers[i].selectExited.RemoveListener(_answerReleased[i]);
                DestroyDetached(_answers[i]);
            }
        }
    }
}
