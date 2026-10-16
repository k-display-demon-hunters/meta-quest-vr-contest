using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FactoryIntro
{
    public sealed class FactoryIntroPresentationController : MonoBehaviour
    {
        private const float HologramRevealDuration = 0.7f;
        private const int TutorialStepCount = 5;

        [SerializeField] private Canvas _canvas;
        [SerializeField] private Text _heading;
        [SerializeField] private Text _instruction;
        [SerializeField] private Text _continueLabel;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _resetObjectButton;
        [SerializeField] private Transform _interiorPanel;
        [SerializeField] private GameObject _normalScenery;
        [SerializeField] private GameObject _damagedScenery;
        [SerializeField] private GameObject _spirit;
        [SerializeField] private Transform _spiritStart;
        [SerializeField] private Transform _attackPoint;
        [SerializeField] private Light _machineLight;
        [SerializeField] private GameObject _movementMarker;
        [SerializeField] private GameObject _placementMarker;
        [SerializeField] private XRGrabInteractable _practiceObject;
        [SerializeField] private Transform _practiceObjectHome;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _alarmClip;
        [SerializeField, Min(0.1f)] private float _attackDuration = 3f;

        private XRRayInteractor _rightRay;
        private XRInputButtonReader _previousUIPress;
        private XRInputButtonReader _uiPress;
        private bool _previousUIInteraction;
        private GameObject _ownedEventSystem;
        private XRUIInputModule _addedInputModule;
        private RectTransform _tutorialChrome;
        private GameObject _skipConfirmation;
        private Text _progressLabel;
        private Image[] _progressSegments;
        private Image _guideBackground;
        private Color _originalBackgroundColor;
        private RectTransform _headingRect;
        private RectTransform _instructionRect;
        private RectTransform _continueRect;
        private RectTransform _resetRect;
        private Vector2 _originalHeadingPosition;
        private Vector2 _originalInstructionPosition;
        private Vector2 _originalContinuePosition;
        private Vector2 _originalResetPosition;
        private Vector2 _originalInstructionSize;
        private Vector2 _originalContinueSize;
        private Vector2 _originalResetSize;
        private CanvasGroup _canvasGroup;
        private Coroutine _revealCoroutine;
        private Vector3 _revealDestination;
        private Vector3 _revealScale;

        public event Action OnSkipRequested;
        public event Action OnSkipCancelled;
        public event Action OnSkipConfirmed;

        public void BindPlayer(XROrigin rig)
        {
            _canvas.worldCamera = rig.Camera;
            BuildTutorialChrome();
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("Factory Intro UI Input", typeof(EventSystem), typeof(XRUIInputModule));
                _ownedEventSystem.transform.SetParent(transform, false);
            }
            else if (EventSystem.current.GetComponent<XRUIInputModule>() == null)
            {
                _addedInputModule = EventSystem.current.gameObject.AddComponent<XRUIInputModule>();
            }
            foreach (XRRayInteractor ray in rig.GetComponentsInChildren<XRRayInteractor>())
            {
                if (ray.handedness == InteractorHandedness.Right && ray.isActiveAndEnabled)
                {
                    _rightRay = ray;
                    break;
                }
            }
            if (_rightRay == null)
            {
                Debug.LogError("[FactoryIntro] The common rig needs an active right-hand ray.", this);
                return;
            }
            _previousUIPress = _rightRay.uiPressInput;
            _previousUIInteraction = _rightRay.enableUIInteraction;
            _uiPress = new XRInputButtonReader("Factory Intro UI", inputSourceMode: XRInputButtonReader.InputSourceMode.InputAction)
            {
                inputActionPerformed = new InputAction("Factory Intro Press", InputActionType.Button, "<XRController>{RightHand}/triggerPressed"),
                inputActionValue = new InputAction("Factory Intro Value", InputActionType.Value, "<XRController>{RightHand}/trigger")
            };
            _rightRay.uiPressInput = _uiPress;
            _rightRay.enableUIInteraction = true;
        }

        public void Show(string heading, string instruction, string button, bool canContinue)
        {
            _heading.text = heading;
            _instruction.text = instruction;
            _continueLabel.text = button;
            _continueButton.gameObject.SetActive(!string.IsNullOrEmpty(button));
            _continueButton.interactable = canContinue;
        }

        public void SetInstruction(string instruction)
        {
            _instruction.text = instruction;
        }

        public void ShowTutorialChrome(bool visible)
        {
            BuildTutorialChrome();
            if (!visible)
            {
                StopHologramReveal();
            }

            _tutorialChrome.gameObject.SetActive(visible);
            _skipConfirmation.SetActive(false);
            if (_guideBackground != null)
            {
                _guideBackground.color = visible
                    ? new Color(0.025f, 0.10f, 0.15f, 0.84f)
                    : _originalBackgroundColor;
            }
            _headingRect.anchoredPosition = visible ? new Vector2(0f, 156f) : _originalHeadingPosition;
            _instructionRect.anchoredPosition = visible ? new Vector2(0f, -62f) : _originalInstructionPosition;
            _instructionRect.sizeDelta = visible ? new Vector2(860f, 150f) : _originalInstructionSize;
            _continueRect.anchoredPosition = visible ? new Vector2(0f, -191f) : _originalContinuePosition;
            _resetRect.anchoredPosition = visible ? new Vector2(-348f, -260f) : _originalResetPosition;
            _continueRect.sizeDelta = visible ? new Vector2(430f, 76f) : _originalContinueSize;
            _resetRect.sizeDelta = visible ? new Vector2(258f, 64f) : _originalResetSize;
        }

        public void SetTutorialProgress(TutorialStep step)
        {
            BuildTutorialChrome();
            int completed = step == TutorialStep.Complete ? TutorialStepCount : Mathf.Clamp((int)step, 0, TutorialStepCount - 1);
            int current = step == TutorialStep.Complete ? TutorialStepCount : completed + 1;
            _progressLabel.text = step == TutorialStep.Complete
                ? "조작 연습 완료  ·  05 / 05"
                : $"조작 연습  ·  {current:00} / 05";

            for (int index = 0; index < _progressSegments.Length; index++)
            {
                _progressSegments[index].color = index < completed
                    ? new Color(0.18f, 0.96f, 0.78f, 0.96f)
                    : index == completed && step != TutorialStep.Complete
                        ? new Color(0.51f, 0.98f, 1f, 1f)
                        : new Color(0.16f, 0.34f, 0.40f, 0.75f);
            }
        }

        public void PlayHologramReveal(Transform tablet)
        {
            BuildTutorialChrome();
            StopHologramReveal();
            _revealDestination = _canvas.transform.position;
            _revealScale = _canvas.transform.localScale;
            Vector3 origin = tablet != null
                ? tablet.position + tablet.up * 0.12f
                : _revealDestination - _canvas.transform.forward * 0.5f;
            _revealCoroutine = StartCoroutine(RevealHologram(origin));
        }

        public void ResetFactory()
        {
            _normalScenery.SetActive(true);
            _damagedScenery.SetActive(false);
            _spirit.SetActive(false);
            _machineLight.color = new Color(0.1f, 0.8f, 1f);
            _machineLight.intensity = 2f;
            HidePractice();
        }

        public IEnumerator PlayAttack()
        {
            _spirit.SetActive(true);
            _spirit.transform.position = _spiritStart.position;
            if (_alarmClip != null)
            {
                _audio.PlayOneShot(_alarmClip);
            }
            float duration = Mathf.Max(0.1f, _attackDuration);
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(time / duration);
                _spirit.transform.position = Vector3.Lerp(_spiritStart.position, _attackPoint.position, progress)
                    + Vector3.up * Mathf.Sin(progress * Mathf.PI) * 0.6f;
                _spirit.transform.Rotate(Vector3.up, 100f * Time.deltaTime, Space.World);
                _machineLight.color = Color.Lerp(Color.cyan, new Color(0.65f, 0.15f, 1f), progress);
                yield return null;
            }
            // Only scenery is degraded; the subtitle and control canvas remains legible.
            _normalScenery.SetActive(false);
            _damagedScenery.SetActive(true);
            _machineLight.color = new Color(1f, 0.25f, 0.05f);
            _machineLight.intensity = 0.6f;
            _spirit.SetActive(false);
        }

        public void EnterInterior()
        {
            _canvas.transform.SetPositionAndRotation(_interiorPanel.position, _interiorPanel.rotation);
        }

        public void ShowPractice(TutorialStep step)
        {
            _movementMarker.SetActive(step == TutorialStep.Movement);
            _placementMarker.SetActive(step == TutorialStep.Grab);
            _practiceObject.gameObject.SetActive(step == TutorialStep.Grab);
            _resetObjectButton.gameObject.SetActive(step == TutorialStep.Grab);
            if (step == TutorialStep.Grab)
            {
                ResetPracticeObject();
            }
        }

        public void HidePractice()
        {
            _movementMarker.SetActive(false);
            _placementMarker.SetActive(false);
            _practiceObject.gameObject.SetActive(false);
            _resetObjectButton.gameObject.SetActive(false);
        }

        public void ResetPracticeObject()
        {
            if (_practiceObject.isSelected)
            {
                return;
            }
            _practiceObject.transform.SetPositionAndRotation(_practiceObjectHome.position, _practiceObjectHome.rotation);
            Rigidbody body = _practiceObject.GetComponent<Rigidbody>();
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void BuildTutorialChrome()
        {
            if (_tutorialChrome != null)
            {
                return;
            }

            _headingRect = (RectTransform)_heading.transform;
            _instructionRect = (RectTransform)_instruction.transform;
            _continueRect = (RectTransform)_continueButton.transform;
            _resetRect = (RectTransform)_resetObjectButton.transform;
            _originalHeadingPosition = _headingRect.anchoredPosition;
            _originalInstructionPosition = _instructionRect.anchoredPosition;
            _originalContinuePosition = _continueRect.anchoredPosition;
            _originalResetPosition = _resetRect.anchoredPosition;
            _originalInstructionSize = _instructionRect.sizeDelta;
            _originalContinueSize = _continueRect.sizeDelta;
            _originalResetSize = _resetRect.sizeDelta;
            Transform background = _canvas.transform.Find("Guide background");
            if (background != null)
            {
                _guideBackground = background.GetComponent<Image>();
                if (_guideBackground != null)
                {
                    _originalBackgroundColor = _guideBackground.color;
                }
            }

            _canvasGroup = _canvas.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = _canvas.gameObject.AddComponent<CanvasGroup>();
            }

            _tutorialChrome = CreateRect(_canvas.transform, "Tutorial hologram chrome", Vector2.zero, new Vector2(1000f, 620f));
            _tutorialChrome.SetSiblingIndex(1);
            AddImage(_tutorialChrome, "Glass tint", Vector2.zero, new Vector2(980f, 600f), new Color(0.02f, 0.21f, 0.28f, 0.18f));
            AddImage(_tutorialChrome, "Header glow", new Vector2(0f, 251f), new Vector2(944f, 78f), new Color(0.04f, 0.35f, 0.43f, 0.45f));
            AddImage(_tutorialChrome, "Instruction glass", new Vector2(0f, -61f), new Vector2(920f, 175f), new Color(0.01f, 0.16f, 0.23f, 0.52f));
            AddImage(_tutorialChrome, "Top edge", new Vector2(0f, 300f), new Vector2(956f, 3f), new Color(0.40f, 0.98f, 1f, 0.86f));
            AddImage(_tutorialChrome, "Bottom edge", new Vector2(0f, -300f), new Vector2(956f, 3f), new Color(0.17f, 0.75f, 0.83f, 0.68f));
            AddImage(_tutorialChrome, "Left edge", new Vector2(-478f, 0f), new Vector2(3f, 600f), new Color(0.22f, 0.82f, 0.92f, 0.62f));
            AddImage(_tutorialChrome, "Right edge", new Vector2(478f, 0f), new Vector2(3f, 600f), new Color(0.22f, 0.82f, 0.92f, 0.62f));
            AddImage(_tutorialChrome, "Header divider", new Vector2(0f, 208f), new Vector2(920f, 2f), new Color(0.21f, 0.75f, 0.84f, 0.50f));
            AddImage(_tutorialChrome, "Instruction accent", new Vector2(-456f, -61f), new Vector2(5f, 171f), new Color(0.25f, 0.98f, 0.96f, 0.83f));

            for (int index = 0; index < 11; index++)
            {
                AddImage(_tutorialChrome, "Glass scanline " + index,
                    new Vector2(0f, 185f - index * 43f), new Vector2(950f, 1f),
                    new Color(0.35f, 0.88f, 0.95f, 0.055f));
            }

            AddLabel(_tutorialChrome, "Skip instruction",
                "오른손 레이로 오른쪽 아래 [건너뛰기]를 가리킨 뒤\nTrigger를 누르면 튜토리얼을 건너뛸 수 있습니다.",
                new Vector2(0f, 251f), new Vector2(920f, 72f), 24,
                new Color(0.88f, 1f, 1f, 1f));
            _progressLabel = AddLabel(_tutorialChrome, "Tutorial progress", "조작 연습  ·  01 / 05",
                new Vector2(0f, 92f), new Vector2(880f, 25f), 22,
                new Color(0.65f, 0.94f, 1f, 0.96f));

            string[] stepNames = { "이동", "선택", "잡기", "복귀", "태블릿" };
            _progressSegments = new Image[TutorialStepCount];
            for (int index = 0; index < TutorialStepCount; index++)
            {
                float x = -336f + index * 168f;
                _progressSegments[index] = AddImage(_tutorialChrome, $"Progress {index + 1}",
                    new Vector2(x, 65f), new Vector2(146f, 9f), new Color(0.16f, 0.34f, 0.40f, 0.75f));
                AddLabel(_tutorialChrome, $"Step {index + 1} name", stepNames[index],
                    new Vector2(x, 44f), new Vector2(146f, 22f), 19,
                    new Color(0.68f, 0.86f, 0.91f, 0.90f));
            }

            Button skip = AddButton(_tutorialChrome, "Skip tutorial", "건너뛰기  ›",
                new Vector2(348f, -260f), new Vector2(258f, 64f),
                new Color(0.09f, 0.31f, 0.39f, 0.97f), 28);
            skip.onClick.AddListener(HandleSkipRequested);

            BuildSkipConfirmation();
            SetTutorialProgress(TutorialStep.Movement);
            _tutorialChrome.gameObject.SetActive(false);
        }

        private void BuildSkipConfirmation()
        {
            RectTransform overlay = CreateRect(_canvas.transform, "Skip confirmation", Vector2.zero, new Vector2(1000f, 620f));
            _skipConfirmation = overlay.gameObject;
            Image veil = overlay.gameObject.AddComponent<Image>();
            veil.color = new Color(0.004f, 0.025f, 0.04f, 0.95f);
            veil.raycastTarget = true;
            AddImage(overlay, "Confirmation glass", Vector2.zero, new Vector2(860f, 410f),
                new Color(0.035f, 0.18f, 0.24f, 0.98f));
            AddImage(overlay, "Confirmation top glow", new Vector2(0f, 205f), new Vector2(860f, 4f),
                new Color(0.50f, 0.99f, 1f, 0.93f));
            AddImage(overlay, "Confirmation bottom glow", new Vector2(0f, -205f), new Vector2(860f, 3f),
                new Color(0.23f, 0.78f, 0.86f, 0.75f));
            AddLabel(overlay, "Confirmation title", "튜토리얼을 건너뛸까요?",
                new Vector2(0f, 102f), new Vector2(800f, 75f), 42, Color.white);
            AddLabel(overlay, "Confirmation explanation",
                "남은 조작 연습을 건너뛰고\n공장 탐색 준비 화면으로 이동합니다.",
                new Vector2(0f, 0f), new Vector2(800f, 115f), 30,
                new Color(0.78f, 0.93f, 0.96f, 1f));
            Button keepLearning = AddButton(overlay, "Keep learning", "계속 배우기",
                new Vector2(-215f, -123f), new Vector2(320f, 76f),
                new Color(0.10f, 0.32f, 0.39f, 1f), 28);
            keepLearning.onClick.AddListener(HandleSkipCancelled);
            Button confirmSkip = AddButton(overlay, "Confirm skip", "건너뛰고 공장 탐색",
                new Vector2(215f, -123f), new Vector2(370f, 76f),
                new Color(0.08f, 0.58f, 0.63f, 1f), 28);
            confirmSkip.onClick.AddListener(HandleSkipConfirmed);
            _skipConfirmation.SetActive(false);
        }

        private RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private Image AddImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            Image image = CreateRect(parent, name, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text AddLabel(Transform parent, string name, string content, Vector2 position, Vector2 size,
            int fontSize, Color color)
        {
            Text label = CreateRect(parent, name, position, size).gameObject.AddComponent<Text>();
            label.font = _heading.font;
            label.text = content;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }

        private Button AddButton(Transform parent, string name, string caption, Vector2 position, Vector2 size,
            Color color, int fontSize)
        {
            RectTransform rect = CreateRect(parent, name, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.75f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.42f, 0.86f, 0.91f, 1f);
            button.colors = colors;
            AddLabel(rect, "Button label", caption, Vector2.zero, size - new Vector2(18f, 8f), fontSize, Color.white);
            return button;
        }

        private void HandleSkipRequested()
        {
            if (!_tutorialChrome.gameObject.activeInHierarchy || _skipConfirmation.activeSelf)
            {
                return;
            }

            _skipConfirmation.SetActive(true);
            OnSkipRequested?.Invoke();
        }

        private void HandleSkipConfirmed()
        {
            _skipConfirmation.SetActive(false);
            OnSkipConfirmed?.Invoke();
        }

        private void HandleSkipCancelled()
        {
            _skipConfirmation.SetActive(false);
            OnSkipCancelled?.Invoke();
        }

        private IEnumerator RevealHologram(Vector3 origin)
        {
            Transform panel = _canvas.transform;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            panel.position = origin;
            panel.localScale = _revealScale * 0.08f;

            for (float elapsed = 0f; elapsed < HologramRevealDuration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / HologramRevealDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                panel.position = Vector3.Lerp(origin, _revealDestination, eased);
                panel.localScale = _revealScale * Mathf.Lerp(0.08f, 1f, eased);
                _canvasGroup.alpha = Mathf.Clamp01(progress * 1.7f);
                yield return null;
            }

            panel.position = _revealDestination;
            panel.localScale = _revealScale;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _revealCoroutine = null;
        }

        private void StopHologramReveal()
        {
            if (_revealCoroutine == null)
            {
                return;
            }

            StopCoroutine(_revealCoroutine);
            _revealCoroutine = null;
            _canvas.transform.position = _revealDestination;
            _canvas.transform.localScale = _revealScale;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        public void ReleasePlayer()
        {
            if (_rightRay != null)
            {
                _rightRay.uiPressInput = _previousUIPress;
                _rightRay.enableUIInteraction = _previousUIInteraction;
                _rightRay = null;
            }
            if (_uiPress != null)
            {
                _uiPress.inputActionPerformed.Dispose();
                _uiPress.inputActionValue.Dispose();
                _uiPress = null;
            }
            if (_ownedEventSystem != null)
            {
                _ownedEventSystem.SetActive(false);
                Destroy(_ownedEventSystem);
                _ownedEventSystem = null;
            }
            if (_addedInputModule != null)
            {
                _addedInputModule.enabled = false;
                Destroy(_addedInputModule);
                _addedInputModule = null;
            }
        }

        private void OnDisable()
        {
            StopHologramReveal();
            StopAllCoroutines();
            ReleasePlayer();
        }
    }
}
