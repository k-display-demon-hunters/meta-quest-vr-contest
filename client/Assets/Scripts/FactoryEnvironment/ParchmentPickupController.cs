using Tablet;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Education
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(XRSimpleInteractable))]
    public sealed class ParchmentPickupController : MonoBehaviour
    {
        private const float CollectionDistance = 3f;
        private const float FloatAmplitude = 0.025f;

        [SerializeField, Range(0, 4)] private int _lessonIndex;
        [SerializeField] private SpriteRenderer _paperRenderer;
        [SerializeField] private SpriteRenderer _glowRenderer;
        [SerializeField] private Light _glowLight;

        private XRSimpleInteractable _interactable;
        private TabletManager _tablet;
        private Vector3 _restPosition;
        private Color _paperColor;
        private bool _hasSearchedForTablet;
        private bool _isCollected;

        public int LessonIndex => _lessonIndex;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _restPosition = transform.localPosition;
            if (_paperRenderer != null)
            {
                _paperColor = _paperRenderer.color;
            }
        }

        private void OnEnable()
        {
            _interactable.activated.AddListener(HandleActivated);
            _interactable.selectEntered.AddListener(HandleSelected);
        }

        private void Start()
        {
            ResolveTablet(null);
            if (_tablet != null && _tablet.HasParchment(_lessonIndex))
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            float phase = Time.time * 1.6f + _lessonIndex * 0.7f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(phase);
            transform.localPosition = _restPosition + Vector3.up * (Mathf.Sin(phase * 0.7f) * FloatAmplitude);

            if (_glowRenderer != null)
            {
                _glowRenderer.color = new Color(1f, 0.7f, 0.28f, Mathf.Lerp(0.1f, 0.17f, pulse));
            }
            if (_glowLight != null)
            {
                _glowLight.intensity = Mathf.Lerp(0.35f, 0.5f, pulse);
            }
            if (_paperRenderer != null)
            {
                _paperRenderer.color = Color.Lerp(_paperColor * 0.97f, _paperColor, pulse);
            }
        }

        private void ResolveTablet(Transform interactor)
        {
            if (interactor != null)
            {
                XROrigin rig = interactor.GetComponentInParent<XROrigin>();
                if (rig != null)
                {
                    TabletManager rigTablet = rig.GetComponentInChildren<TabletManager>(true);
                    if (rigTablet != null)
                    {
                        _tablet = rigTablet;
                        return;
                    }
                }
            }

            if (_tablet == null && !_hasSearchedForTablet)
            {
                _hasSearchedForTablet = true;
                _tablet = FindFirstObjectByType<TabletManager>();
            }
        }

        private void TryCollect(Transform interactor)
        {
            if (_isCollected || interactor == null ||
                (interactor.position - transform.position).sqrMagnitude > CollectionDistance * CollectionDistance)
            {
                return;
            }

            ResolveTablet(interactor);
            if (_tablet == null)
            {
                return;
            }

            if (_tablet.CollectParchment(_lessonIndex) || _tablet.HasParchment(_lessonIndex))
            {
                _isCollected = true;
                StartCoroutine(HideAfterInteraction());
            }
        }

        private IEnumerator HideAfterInteraction()
        {
            // Let XRI finish its selection callbacks before removing the interaction target.
            yield return null;
            gameObject.SetActive(false);
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            TryCollect(args.interactorObject.transform);
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            TryCollect(args.interactorObject.transform);
        }

        private void OnDisable()
        {
            _interactable.activated.RemoveListener(HandleActivated);
            _interactable.selectEntered.RemoveListener(HandleSelected);
        }
    }
}
