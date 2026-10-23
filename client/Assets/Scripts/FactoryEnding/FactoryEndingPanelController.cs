using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FactoryEnding
{
    public sealed class FactoryEndingPanelController : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Text _title;
        [SerializeField] private Text _description;
        [SerializeField] private Text _message;
        [SerializeField] private GameObject _recapCards;
        [SerializeField] private Button _completeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _exitButton;

        private XRRayInteractor _rightRay;
        private XRInputButtonReader _previousUIPress;
        private XRInputButtonReader _uiPress;
        private bool _previousUIInteraction;
        private bool _previousHitClosestOnly;
        private GameObject _ownedEventSystem;
        private XRUIInputModule _addedInputModule;

        public event Action OnCompleteRequested;
        public event Action OnRestartRequested;
        public event Action OnExitRequested;

        private void OnEnable()
        {
            _completeButton.onClick.AddListener(HandleComplete);
            _restartButton.onClick.AddListener(HandleRestart);
            _exitButton.onClick.AddListener(HandleExit);
            UI.SceneUIStyle.ApplyButtons(_canvas.transform);
            PrepareUIInput();
            BindRightHand();
        }

        public void ShowRestoration()
        {
            _canvas.enabled = false;
            _title.text = "공장이 다시 깨어납니다";
            _description.text = "빛이 돌아오고 기계가 제자리를 찾습니다.\n잠시 주변을 둘러보며 복원되는 공장을 확인해 보세요.";
            _message.text = "공장의 불빛이 돌아오고 있습니다.";
            _recapCards.SetActive(false);
            _completeButton.gameObject.SetActive(false);
            _restartButton.gameObject.SetActive(false);
            _exitButton.gameObject.SetActive(false);
        }

        public void ShowRecap()
        {
            _canvas.enabled = true;
            _title.text = "축하합니다! 공장에 빛이 돌아왔어요";
            _description.text = "CNN은 작은 영역의 특징을 찾고, 여러 층을 거쳐 결과를 만듭니다.\n여섯 장의 개념 두루마리로 체험을 돌아보세요.";
            _message.text = "여섯 개 개념을 읽고 체험을 마무리해 보세요.";
            _recapCards.SetActive(true);
            _completeButton.gameObject.SetActive(true);
        }

        public void ShowCompletion()
        {
            _title.text = "체험 완료";
            _description.text = "체험이 끝났습니다.\n픽셀에서 특징을 찾고 결과를 만드는 CNN의 흐름을 기억해 주세요.\n공장에 다시 방문하고 싶다면 처음으로 돌아갈 수 있습니다.";
            _message.text = "오른손 레이로 버튼을 가리킨 뒤 Trigger를 누르세요.";
            _recapCards.SetActive(false);
            _completeButton.gameObject.SetActive(false);
            _restartButton.gameObject.SetActive(true);
            _exitButton.gameObject.SetActive(true);
            SetNavigationAvailable(true);
        }

        public void ShowMessage(string message)
        {
            _message.text = message;
        }

        public void SetNavigationAvailable(bool available)
        {
            _restartButton.interactable = available;
            _exitButton.interactable = available;
        }

        private void PrepareUIInput()
        {
            _canvas.worldCamera = Camera.main;
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("Factory Ending UI Input", typeof(EventSystem), typeof(XRUIInputModule));
                _ownedEventSystem.transform.SetParent(transform, false);
            }
            else if (EventSystem.current.GetComponent<XRUIInputModule>() == null)
            {
                _addedInputModule = EventSystem.current.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private void BindRightHand()
        {
            foreach (XRRayInteractor ray in FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None))
            {
                if (ray.handedness == InteractorHandedness.Right && ray.isActiveAndEnabled)
                {
                    _rightRay = ray;
                    break;
                }
            }
            if (_rightRay == null)
            {
                Debug.LogWarning("[FactoryEnding] An active right-hand XR ray is required for the ending buttons.", this);
                return;
            }

            _previousUIPress = _rightRay.uiPressInput;
            _previousUIInteraction = _rightRay.enableUIInteraction;
            _previousHitClosestOnly = _rightRay.hitClosestOnly;
            _uiPress = new XRInputButtonReader("Factory Ending UI", inputSourceMode: XRInputButtonReader.InputSourceMode.InputAction);
            _uiPress.inputActionPerformed.AddBinding("<XRController>{RightHand}/triggerPressed");
            _uiPress.inputActionValue.AddBinding("<XRController>{RightHand}/trigger");
            // XRI's setter enables these actions and disables the previously assigned reader.
            _rightRay.uiPressInput = _uiPress;
            _rightRay.enableUIInteraction = true;
            _rightRay.hitClosestOnly = true;
        }

        private void HandleComplete() => OnCompleteRequested?.Invoke();
        private void HandleRestart() => OnRestartRequested?.Invoke();
        private void HandleExit() => OnExitRequested?.Invoke();

        private void OnDisable()
        {
            _completeButton.onClick.RemoveListener(HandleComplete);
            _restartButton.onClick.RemoveListener(HandleRestart);
            _exitButton.onClick.RemoveListener(HandleExit);
            if (_rightRay != null)
            {
                _rightRay.uiPressInput = _previousUIPress;
                _rightRay.enableUIInteraction = _previousUIInteraction;
                _rightRay.hitClosestOnly = _previousHitClosestOnly;
                _rightRay = null;
            }
            if (_uiPress != null)
            {
                _uiPress.DisableDirectActionIfModeUsed();
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
    }
}
