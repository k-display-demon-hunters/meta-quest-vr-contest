using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Parchment
{
    public sealed class ParchmentPickupVisualController : MonoBehaviour
    {
        private const float FloatAmplitude = 0.025f;
        private const float OpenWidth = 1.25f;
        private const float ScrollHeight = 0.7f;
        private const float RollerFraction = 0.17f;
        private static readonly Color HologramColor = new Color(0.34f, 0.84f, 1f);
        [SerializeField] private SpriteRenderer _paperRenderer;
        [SerializeField] private SpriteRenderer _glowRenderer;
        [SerializeField] private Light _glowLight;
        [SerializeField] private float _floatPhase;
        private XRSimpleInteractable _interactable;
        [SerializeField] private Canvas _labelCanvas;
        private Vector3 _restPosition;
        private Color _paperColor;
        private SpriteRenderer _rightRoller;
        private Sprite _leftSprite;
        private Sprite _rightSprite;
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
            _glowLight.intensity = Mathf.Lerp(0.3f, 0.45f, pulse);
            _paperRenderer.color = Color.Lerp(_paperColor * 0.97f, _paperColor, pulse);
            _rightRoller.color = _paperRenderer.color;
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
            _rightRoller.enabled = visible;
            _glowRenderer.enabled = false;
            _glowLight.enabled = visible;
            _labelCanvas.enabled = visible;
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
            Sprite source = _paperRenderer.sprite;
            Vector3 paperSize = source.bounds.size;
            Rect rect = source.rect;
            float rollerPixels = rect.width * RollerFraction;
            _leftSprite = Sprite.Create(source.texture,
                new Rect(rect.x, rect.y, rollerPixels, rect.height),
                new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            _rightSprite = Sprite.Create(source.texture,
                new Rect(rect.xMax - rollerPixels, rect.y, rollerPixels, rect.height),
                new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);

            // Reuse the original image's metal ends; no additional image asset or scene edit is needed.
            _paperRenderer.sprite = _leftSprite;
            _paperRenderer.transform.localScale = new Vector3(OpenWidth / paperSize.x, ScrollHeight / paperSize.y, 1f);
            _paperRenderer.transform.localPosition = new Vector3(-OpenWidth * RollerFraction * 0.5f, 0f, 0f);
            _rightRoller = new GameObject("Closed Right Roller").AddComponent<SpriteRenderer>();
            _rightRoller.transform.SetParent(_paperRenderer.transform.parent, false);
            _rightRoller.transform.localPosition = new Vector3(OpenWidth * RollerFraction * 0.5f, 0f, 0f);
            _rightRoller.transform.localRotation = _paperRenderer.transform.localRotation;
            _rightRoller.transform.localScale = _paperRenderer.transform.localScale;
            _rightRoller.sprite = _rightSprite;
            _rightRoller.sharedMaterial = _paperRenderer.sharedMaterial;
            _rightRoller.sortingLayerID = _paperRenderer.sortingLayerID;
            _rightRoller.sortingOrder = _paperRenderer.sortingOrder;
            _rightRoller.color = _paperColor;
            _glowLight.color = HologramColor;
            _glowLight.range = 1.5f;
            GetComponent<BoxCollider>().size = new Vector3(OpenWidth * RollerFraction * 2f, ScrollHeight, 0.08f);
        }

        private void OnDestroy()
        {
            Destroy(_leftSprite);
            Destroy(_rightSprite);
        }
    }
}
