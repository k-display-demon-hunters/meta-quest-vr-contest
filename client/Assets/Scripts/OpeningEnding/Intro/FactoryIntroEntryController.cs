using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using XR;

namespace OpeningEnding.Intro
{
    // Owns the opening's physical doorway, walking route and left-hand cue.
    public sealed class FactoryIntroEntryController : MonoBehaviour
    {
        [SerializeField] private Transform _leftDoor;
        [SerializeField] private Transform _rightDoor;
        [SerializeField] private Transform _threshold;
        [SerializeField] private GameObject _route;
        [SerializeField] private Transform _handCue;
        private Transform _camera;
        private Transform _leftHand;
        private TouchPlusHandController _leftController;
        private Vector3 _leftClosed;
        private Vector3 _rightClosed;
        private bool _entryActive;

        public bool HasEntered { get; private set; }

        private void Awake()
        {
            _leftClosed = _leftDoor.localPosition;
            _rightClosed = _rightDoor.localPosition;
        }

        public void BindPlayer(XROrigin rig)
        {
            _camera = rig.Camera.transform;
            foreach (Transform node in rig.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == "LeftHand Grip Visual")
                {
                    _leftHand = node;
                    break;
                }
            }
        }

        public IEnumerator OpenDoor()
        {
            const float duration = 3.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float offset = Mathf.SmoothStep(0f, 1.85f, elapsed / duration);
                _leftDoor.localPosition = _leftClosed + Vector3.left * offset;
                _rightDoor.localPosition = _rightClosed + Vector3.right * offset;
                elapsed += Time.deltaTime;
                yield return null;
            }
            _leftDoor.localPosition = _leftClosed + Vector3.left * 1.85f;
            _rightDoor.localPosition = _rightClosed + Vector3.right * 1.85f;
        }

        public void BeginEntry()
        {
            HasEntered = false;
            _entryActive = true;
            _route.SetActive(true);
            _handCue.gameObject.SetActive(true);
            // PersistentXRPlayerRig has initialized its hand visuals by this stage.
            _leftController = _leftHand.GetComponentInChildren<TouchPlusHandController>();
            _leftController.SetCue(5);
        }

        private void Update()
        {
            if (!_entryActive) return;
            _handCue.SetPositionAndRotation(_leftHand.position, _leftHand.rotation);
            _handCue.localScale = Vector3.one * (0.055f + 0.008f * Mathf.Sin(Time.unscaledTime * 6f));
            Vector3 position = _threshold.InverseTransformPoint(_camera.position);
            if (Mathf.Abs(position.x) > 1.65f || position.z < 0f || position.z > 2f) return;
            HasEntered = true;
            EndEntry();
        }

        public void EndEntry()
        {
            if (!_entryActive) return;
            _entryActive = false;
            _route.SetActive(false);
            _handCue.gameObject.SetActive(false);
            _leftController.SetCue(-1);
        }

        private void OnDisable()
        {
            EndEntry();
        }
    }
}
