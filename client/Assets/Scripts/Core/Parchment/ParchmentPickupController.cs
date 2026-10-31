using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Parchment
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(XRSimpleInteractable))]
    public sealed class ParchmentPickupController : MonoBehaviour
    {
        [SerializeField, Range(0, 4)] private int _lessonIndex;
        [SerializeField] private ParchmentReaderController _readerPrefab;
        [SerializeField] private ParchmentPickupVisualController _visuals;
        private XRSimpleInteractable _interactable;
        private ParchmentManager _parchment;
        private bool _hasLesson;

        public int LessonIndex => _lessonIndex;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _parchment = FindFirstObjectByType<ParchmentManager>();
            // Older scenes contain five pickups. Only the three catalog entries remain in this lesson.
            _hasLesson = _lessonIndex >= 0 && _lessonIndex < ParchmentCatalog.All.Count;
            if (!_hasLesson)
            {
                gameObject.SetActive(false);
                return;
            }

            Pose pose = ParchmentCatalog.Get(_lessonIndex).PickupPose;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void Start()
        {
            _visuals.ConfigureLesson(ParchmentCatalog.Get(_lessonIndex));
        }

        private void OnEnable()
        {
            if (!_hasLesson) { return; }
            _interactable.activated.AddListener(HandleActivated);
            _interactable.selectEntered.AddListener(HandleSelected);
            _parchment.RegisterPickup(_lessonIndex, transform, _readerPrefab,
                _visuals.ShowCollected);
        }

        private void OnDisable()
        {
            if (!_hasLesson) { return; }
            _interactable.activated.RemoveListener(HandleActivated);
            _interactable.selectEntered.RemoveListener(HandleSelected);
            // The persistent session may already be destroyed during shutdown.
            if (_parchment != null) { _parchment.UnregisterPickup(_lessonIndex, transform); }
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            _parchment.RequestPickup(_lessonIndex, args.interactorObject.transform);
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            _parchment.RequestPickup(_lessonIndex, args.interactorObject.transform);
        }
    }
}
