using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRController = UnityEngine.InputSystem.XR.XRController;

namespace FactoryIntro
{
    public enum TutorialStep { Movement, Selection, Grab, Return, Tablet, Complete }

    public sealed class FactoryTutorialController : MonoBehaviour
    {
        [SerializeField] private Transform _movementTarget;
        [SerializeField] private Transform _returnPoint;
        [SerializeField] private XRGrabInteractable _grabTarget;
        [SerializeField] private Transform _placementTarget;
        [SerializeField, Min(0.05f)] private float _targetRadius = 0.45f;
        [SerializeField, Min(0.05f)] private float _placementRadius = 0.3f;

        private XROrigin _rig;
        private XRController _leftController;
        private ButtonControl _trigger;
        private ButtonControl _xButton;
        private ButtonControl _yButton;
        private ButtonControl _menuButton;
        private Vector2Control _stick;
        private bool _hasMovedWithStick;
        private bool _hasGrabbed;
        private bool _tabletInputArmed;
        private bool _leftInputReady;
        private int _tabletStep;
        private int _runVersion;

        public TutorialStep CurrentStep { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public string CurrentInstruction { get; private set; }
        public event Action<TutorialStep> OnStepChanged;
        public event Action<string> OnInstructionChanged;
        public event Action OnCompleted;

        private void Update()
        {
            if (!IsRunning || IsPaused || _rig == null || _rig.Camera == null)
            {
                return;
            }
            if (CurrentStep != TutorialStep.Movement && CurrentStep != TutorialStep.Tablet)
            {
                return;
            }
            if (!TryReadLeftController())
            {
                _leftInputReady = false;
                SetInstruction("왼손 컨트롤러 연결과 추적을 확인하세요. 연결되면 현재 연습을 이어갑니다.");
                return;
            }
            if (!_leftInputReady)
            {
                _leftInputReady = true;
                SetInstruction(GetStepInstruction());
            }
            if (CurrentStep == TutorialStep.Movement)
            {
                _hasMovedWithStick |= _stick.ReadValue().sqrMagnitude > 0.36f;
                Vector3 offset = _rig.Camera.transform.position - _movementTarget.position;
                offset.y = 0f;
                if (_hasMovedWithStick && offset.sqrMagnitude <= _targetRadius * _targetRadius)
                {
                    ChangeStep(TutorialStep.Selection);
                }
            }
            else
            {
                UpdateTabletPractice();
            }
        }

        public void BeginTutorial(XROrigin rig)
        {
            StopTutorial();
            if (!isActiveAndEnabled || rig == null || rig.Camera == null || _movementTarget == null ||
                _returnPoint == null || _grabTarget == null || _placementTarget == null)
            {
                SetInstruction("튜토리얼의 XR 리그 또는 연습 대상 연결이 필요합니다.");
                return;
            }
            _rig = rig;
            _hasMovedWithStick = false;
            _hasGrabbed = false;
            _tabletStep = 0;
            _grabTarget.selectEntered.AddListener(HandleGrab);
            _grabTarget.selectExited.AddListener(HandleRelease);
            IsRunning = true;
            IsPaused = false;
            ChangeStep(TutorialStep.Movement);
        }

        public void SetPaused(bool paused)
        {
            if (!IsRunning)
            {
                return;
            }
            bool wasPaused = IsPaused;
            IsPaused = paused;
            _tabletInputArmed = false;
            if (wasPaused && !paused && CurrentStep == TutorialStep.Tablet)
            {
                // The tablet closes its active menu when the confirmation pauses input.
                // Restart its short exercise so the instructions match the visible UI.
                _tabletStep = 0;
                SetInstruction(GetStepInstruction());
            }
        }

        public void StopTutorial()
        {
            _runVersion++;
            StopAllCoroutines();
            IsRunning = false;
            IsPaused = false;
            if (_grabTarget != null)
            {
                _grabTarget.selectEntered.RemoveListener(HandleGrab);
                _grabTarget.selectExited.RemoveListener(HandleRelease);
            }
            _rig = null;
            _leftController = null;
            _tabletInputArmed = false;
            _leftInputReady = false;
        }

        public void ConfirmSelection()
        {
            if (IsRunning && !IsPaused && CurrentStep == TutorialStep.Selection)
            {
                ChangeStep(TutorialStep.Grab);
            }
        }

        public void ReturnToStart()
        {
            if (!IsRunning || IsPaused || CurrentStep != TutorialStep.Return || _rig == null || _rig.Camera == null)
            {
                return;
            }
            // The marker denotes the floor; retain the user's tracked standing or seated height.
            Transform origin = _rig.Origin.transform;
            float cameraHeight = Vector3.Dot(_rig.Camera.transform.position - origin.position, origin.up);
            Vector3 forward = Vector3.ProjectOnPlane(_returnPoint.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }
            if (_rig.MatchOriginUpCameraForward(Vector3.up, forward) &&
                _rig.MoveCameraToWorldLocation(_returnPoint.position + Vector3.up * cameraHeight))
            {
                ChangeStep(TutorialStep.Tablet);
            }
        }

        private bool TryReadLeftController()
        {
            XRController controller = XRController.leftHand;
            if (controller != _leftController)
            {
                _leftController = controller;
                _trigger = controller?.TryGetChildControl("triggerPressed") as ButtonControl;
                _xButton = controller?.TryGetChildControl("primaryButton") as ButtonControl;
                _yButton = controller?.TryGetChildControl("secondaryButton") as ButtonControl;
                _menuButton = controller?.TryGetChildControl("menuButton") as ButtonControl;
                _stick = controller?.TryGetChildControl("primary2DAxis") as Vector2Control;
                _tabletInputArmed = false;
            }
            if (controller == null || !controller.added || !controller.enabled || !controller.isTracked.isPressed ||
                _trigger == null || _xButton == null || _yButton == null || _menuButton == null || _stick == null)
            {
                _tabletInputArmed = false;
                return false;
            }
            return true;
        }

        private void UpdateTabletPractice()
        {
            Vector2 stick = _stick.ReadValue();
            bool neutral = !_trigger.isPressed && !_xButton.isPressed && !_yButton.isPressed && !_menuButton.isPressed &&
                stick.sqrMagnitude < 0.0625f;
            if (!_tabletInputArmed)
            {
                _tabletInputArmed = neutral;
                if (neutral && _tabletStep == 6)
                {
                    ChangeStep(TutorialStep.Complete);
                }
                return;
            }
            bool performed = _tabletStep switch
            {
                0 => _trigger.isPressed,
                1 => _trigger.isPressed && stick.sqrMagnitude > 0.36f,
                2 => _menuButton.isPressed,
                3 => _menuButton.isPressed,
                4 => _xButton.isPressed,
                5 => _yButton.isPressed,
                _ => false
            };
            if (performed)
            {
                _tabletStep++;
                _tabletInputArmed = false;
                SetInstruction(GetStepInstruction());
            }
        }

        private void ChangeStep(TutorialStep step)
        {
            int version = _runVersion;
            CurrentStep = step;
            _tabletInputArmed = false;
            if (step == TutorialStep.Complete)
            {
                IsRunning = false;
            }
            OnStepChanged?.Invoke(step);
            if (_runVersion != version || CurrentStep != step)
            {
                return;
            }
            SetInstruction(GetStepInstruction());
            if (step == TutorialStep.Complete && _runVersion == version)
            {
                OnCompleted?.Invoke();
            }
        }

        private string GetStepInstruction()
        {
            return CurrentStep switch
            {
                TutorialStep.Movement => "이동 · 왼쪽 스틱을 밀어 바닥의 이동 표식 안으로 들어오세요.\n이동 방향은 시선을 따릅니다. 고개나 몸을 돌려 주변을 둘러보세요.",
                TutorialStep.Selection => "선택 · 오른손 레이로 '선택 연습' 버튼을 가리킨 뒤 검지 Trigger를 누르세요.",
                TutorialStep.Grab => "잡기 · 오른손 레이로 연습 블록을 가리키고 손잡이 Grip을 누르세요.\n표시된 놓기 지점으로 옮긴 뒤 Grip을 놓으세요.",
                TutorialStep.Return => "복귀 · 오른손 레이로 '원위치로 돌아가기' 버튼을 선택하세요.\n현재 눈높이를 유지한 채 시작 표식으로 이동합니다.",
                TutorialStep.Tablet => GetTabletInstruction(),
                _ => "조작 연습 완료 · 이동, 선택, 잡기, 복귀와 왼손 입력을 확인했습니다."
            };
        }

        private string GetTabletInstruction()
        {
            string instruction = _tabletStep switch
            {
                0 => "왼손 태블릿을 보세요. 버튼과 스틱을 놓은 뒤 왼손 Trigger를 한 번 누르세요.",
                1 => "Trigger를 놓았다가 다시 누른 채 왼쪽 스틱을 밀어 태블릿 포커스를 이동하세요.",
                2 => "Trigger와 스틱을 놓은 뒤 왼손 세 줄 Menu를 눌러 설정을 여세요.",
                3 => "Menu를 놓았다가 다시 눌러 설정을 닫으세요.",
                4 => "Menu를 놓고 왼손 X를 눌러 포커스된 항목을 선택하세요.",
                5 => "X를 놓고 왼손 Y를 눌러 이전 화면으로 돌아가세요.",
                _ => "모든 버튼을 놓고 스틱을 중앙으로 돌려놓으세요."
            };
            return "태블릿 입력 연습 · " + instruction;
        }

        private void SetInstruction(string instruction)
        {
            if (CurrentInstruction == instruction)
            {
                return;
            }
            CurrentInstruction = instruction;
            OnInstructionChanged?.Invoke(instruction);
        }

        private void HandleGrab(SelectEnterEventArgs args)
        {
            if (IsRunning && !IsPaused && CurrentStep == TutorialStep.Grab &&
                args.interactorObject.transform.IsChildOf(_rig.transform))
            {
                _hasGrabbed = true;
            }
        }

        private void HandleRelease(SelectExitEventArgs args)
        {
            if (!IsRunning || IsPaused || CurrentStep != TutorialStep.Grab || !_hasGrabbed)
            {
                return;
            }
            _hasGrabbed = false;
            if (!args.isCanceled && !_grabTarget.isSelected &&
                Vector3.Distance(_grabTarget.transform.position, _placementTarget.position) <= _placementRadius)
            {
                StartCoroutine(CompletePlacement());
            }
        }

        private IEnumerator CompletePlacement()
        {
            // The next step hides this interactable. Let XRI finish its select-exit dispatch first.
            yield return null;
            while (IsRunning && IsPaused)
            {
                yield return null;
            }
            if (IsRunning && CurrentStep == TutorialStep.Grab && !_grabTarget.isSelected)
            {
                ChangeStep(TutorialStep.Return);
            }
        }

        private void OnDisable()
        {
            StopTutorial();
        }
    }
}
