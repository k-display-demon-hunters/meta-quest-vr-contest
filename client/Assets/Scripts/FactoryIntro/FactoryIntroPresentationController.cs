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

        public void BindPlayer(XROrigin rig)
        {
            _canvas.worldCamera = rig.Camera;
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
            StopAllCoroutines();
            ReleasePlayer();
        }
    }
}
