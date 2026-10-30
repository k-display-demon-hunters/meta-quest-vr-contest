using Tablet;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
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
        private static readonly Color HologramColor = new Color(0.34f, 0.84f, 1f);

        [SerializeField, Range(0, 4)] private int _lessonIndex;
        [SerializeField] private SpriteRenderer _paperRenderer;
        [SerializeField] private SpriteRenderer _glowRenderer;
        [SerializeField] private Light _glowLight;

        private XRSimpleInteractable _interactable;
        private TabletManager _tablet;
        private Canvas _labelCanvas;
        private ParchmentReaderController _reader;
        private Vector3 _restPosition;
        private Color _paperColor;
        private bool _hasSearchedForTablet;
        private bool _isCollected;
        private bool _readingRequested;

        public int LessonIndex => _lessonIndex;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _restPosition = transform.localPosition;
            _paperColor = _paperRenderer.color;
            ConfigureAppearance();
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
            if (_readingRequested)
            {
                return;
            }
            float phase = Time.time * 1.6f + _lessonIndex * 0.7f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(phase);
            transform.localPosition = _restPosition + Vector3.up * (Mathf.Sin(phase * 0.7f) * FloatAmplitude);

            _glowRenderer.color = new Color(HologramColor.r, HologramColor.g, HologramColor.b,
                Mathf.Lerp(0.09f, 0.16f, pulse));
            _glowLight.intensity = Mathf.Lerp(0.3f, 0.45f, pulse);
            _paperRenderer.color = Color.Lerp(_paperColor * 0.97f, _paperColor, pulse);
        }

        private void ConfigureAppearance()
        {
            Vector3 paperSize = _paperRenderer.sprite.bounds.size;
            Vector3 glowSize = _glowRenderer.sprite.bounds.size;
            _paperRenderer.transform.localScale = new Vector3(0.84f / paperSize.x, 0.47f / paperSize.y, 1f);
            _glowRenderer.transform.localScale = new Vector3(0.9f / glowSize.x, 0.5f / glowSize.y, 1f);
            _glowLight.color = HologramColor;
            _glowLight.range = 1.5f;
            GetComponent<BoxCollider>().size = new Vector3(0.88f, 0.5f, 0.08f);

            _labelCanvas = GetComponentInChildren<Canvas>(true);
            if (_labelCanvas == null)
            {
                return;
            }

            foreach (Text text in _labelCanvas.GetComponentsInChildren<Text>(true))
            {
                text.color = Color.white;
                switch (text.gameObject.name)
                {
                    case "Document number":
                        text.text = "양피지 " + (_lessonIndex + 1).ToString("00");
                        text.rectTransform.anchoredPosition = new Vector2(0f, 78f);
                        text.fontSize = 25;
                        break;
                    case "Lesson title":
                        text.rectTransform.anchoredPosition = new Vector2(0f, -3f);
                        text.rectTransform.sizeDelta = new Vector2(500f, 100f);
                        text.fontSize = 42;
                        text.fontStyle = FontStyle.Bold;
                        break;
                    case "Collect instruction":
                        text.text = "Trigger / Grip으로 열기";
                        text.rectTransform.anchoredPosition = new Vector2(0f, -100f);
                        text.fontSize = 19;
                        break;
                    default:
                        text.gameObject.SetActive(false);
                        break;
                }
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

        private void TryOpen(Transform interactor)
        {
            if (_isCollected || _readingRequested || interactor == null ||
                (interactor.position - transform.position).sqrMagnitude > CollectionDistance * CollectionDistance)
            {
                return;
            }

            XROrigin rig = interactor.GetComponentInParent<XROrigin>();
            if (rig == null || rig.Camera == null || FindFirstObjectByType<ParchmentReaderController>() != null)
            {
                return;
            }
            ResolveTablet(interactor);
            if (_tablet == null)
            {
                return;
            }

            _readingRequested = true;
            StartCoroutine(OpenAfterInteraction(rig.Camera));
        }

        private IEnumerator OpenAfterInteraction(Camera camera)
        {
            // Let XRI finish the pickup event before replacing its visual with the reader.
            yield return null;
            _interactable.enabled = false;
            _paperRenderer.enabled = false;
            _glowRenderer.enabled = false;
            _glowLight.enabled = false;
            if (_labelCanvas != null)
            {
                _labelCanvas.enabled = false;
            }

            GameObject readerObject = new GameObject("양피지 " + (_lessonIndex + 1).ToString("00") + " 열람",
                typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster));
            _reader = readerObject.AddComponent<ParchmentReaderController>();
            _reader.Initialize(_lessonIndex, camera, _paperRenderer.sprite,
                _tablet.ParchmentFont, _tablet.ParchmentTitleFont, transform.position, transform.rotation,
                CompleteReading, CancelReading);
        }

        private void CompleteReading()
        {
            if (_reader == null)
            {
                return;
            }
            bool collected = _tablet.CollectParchment(_lessonIndex) || _tablet.HasParchment(_lessonIndex);
            RemoveReader();
            if (collected)
            {
                _isCollected = true;
                StartCoroutine(HideAfterInteraction());
            }
            else
            {
                RestorePickup();
            }
        }

        private void CancelReading()
        {
            RemoveReader();
            RestorePickup();
        }

        private void RemoveReader()
        {
            if (_reader != null)
            {
                _reader.gameObject.SetActive(false);
                Destroy(_reader.gameObject);
                _reader = null;
            }
            _readingRequested = false;
        }

        private void RestorePickup()
        {
            _paperRenderer.enabled = true;
            _glowRenderer.enabled = true;
            _glowLight.enabled = true;
            if (_labelCanvas != null)
            {
                _labelCanvas.enabled = true;
            }
            _interactable.enabled = true;
        }

        private IEnumerator HideAfterInteraction()
        {
            // Let XRI finish its selection callbacks before removing the interaction target.
            yield return null;
            gameObject.SetActive(false);
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            TryOpen(args.interactorObject.transform);
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            TryOpen(args.interactorObject.transform);
        }

        private void OnDisable()
        {
            _interactable.activated.RemoveListener(HandleActivated);
            _interactable.selectEntered.RemoveListener(HandleSelected);
            RemoveReader();
        }
    }
}
