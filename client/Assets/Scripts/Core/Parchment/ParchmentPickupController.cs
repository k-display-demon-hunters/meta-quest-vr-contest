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

        public int LessonIndex => _lessonIndex;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _parchment = FindFirstObjectByType<ParchmentManager>();
        }

        private void Start()
        {
            _visuals.ConfigureLesson(ParchmentCatalog.Get(_lessonIndex));
        }

        private void OnEnable()
        {
            _interactable.activated.AddListener(HandleActivated);
            _interactable.selectEntered.AddListener(HandleSelected);
            _parchment.RegisterPickup(_lessonIndex, transform, _readerPrefab,
                _visuals.ShowCollected);
        }

        private void OnDisable()
        {
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
