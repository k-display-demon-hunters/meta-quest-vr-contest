using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Parchment
{
    public sealed class ParchmentPickupVisualController : MonoBehaviour
    {
        private const float FloatAmplitude = 0.025f;
        private static readonly Color HologramColor = new Color(0.34f, 0.84f, 1f);
        [SerializeField] private SpriteRenderer _paperRenderer;
        [SerializeField] private SpriteRenderer _glowRenderer;
        [SerializeField] private Light _glowLight;
        [SerializeField] private float _floatPhase;
        private XRSimpleInteractable _interactable;
        private Canvas _labelCanvas;
        private Vector3 _restPosition;
        private Color _paperColor;
        private bool _hidden;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _restPosition = transform.localPosition;
            _paperColor = _paperRenderer.color;
            ConfigureAppearance();
        }

        private void OnEnable()
        {
            _hidden = false;
            SetVisible(true);
        }

        private void Update()
        {
            if (_hidden) { return; }
            float phase = Time.time * 1.6f + _floatPhase;
            float pulse = 0.5f + 0.5f * Mathf.Sin(phase);
            transform.localPosition = _restPosition + Vector3.up * (Mathf.Sin(phase * 0.7f) * FloatAmplitude);
            _glowRenderer.color = new Color(HologramColor.r, HologramColor.g, HologramColor.b,
                Mathf.Lerp(0.09f, 0.16f, pulse));
            _glowLight.intensity = Mathf.Lerp(0.3f, 0.45f, pulse);
            _paperRenderer.color = Color.Lerp(_paperColor * 0.97f, _paperColor, pulse);
        }

        public void ShowCollected()
        {
            _hidden = true;
            StartCoroutine(HideAfterInteraction());
        }

        private void SetVisible(bool visible)
        {
            _interactable.enabled = visible;
            _paperRenderer.enabled = visible;
            _glowRenderer.enabled = visible;
            _glowLight.enabled = visible;
            if (_labelCanvas != null) { _labelCanvas.enabled = visible; }
        }

        private IEnumerator HideAfterInteraction()
        {
            // Let XRI finish selection callbacks before disabling the pickup.
            yield return null;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
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
                        text.text = "Trigger / Grip으로 수집하고 읽기";
                        text.rectTransform.anchoredPosition = new Vector2(0f, -100f);
                        text.fontSize = 19;
                        break;
                    default:
                        text.gameObject.SetActive(false);
                        break;
                }
            }
        }
    }
}
