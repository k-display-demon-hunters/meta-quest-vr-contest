using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene
{
    public sealed class ModelTrainingController : MonoBehaviour
    {
        [SerializeField] private Button _trainingStartButton;
        [SerializeField] private Button _trainingStopButton;
        [SerializeField] private Text _trainingStatus;

        public event Action OnStartRequested;
        public event Action OnStopRequested;

        private void Awake()
        {
            _trainingStartButton.onClick.AddListener(HandleStartClicked);
            _trainingStopButton.onClick.AddListener(HandleStopClicked);
            SetBusyState(false);
        }

        private void SetBusyState(bool isBusy)
        {
            _trainingStartButton.gameObject.SetActive(!isBusy);
            _trainingStopButton.gameObject.SetActive(isBusy);
            _trainingStatus.gameObject.SetActive(isBusy);
        }

        public void ShowState(ModelExecutionState state, bool isBusy, bool canCancel)
        {
            SetBusyState(isBusy);
            _trainingStopButton.interactable = canCancel;
            switch (state)
            {
                case ModelExecutionState.Connecting:
                    _trainingStatus.text = "서버 연결 중";
                    break;
                case ModelExecutionState.StartingTraining:
                    _trainingStatus.text = "학습 준비 중";
                    break;
                case ModelExecutionState.CancellingTraining:
                    _trainingStatus.text = "중지 확인 중";
                    break;
                case ModelExecutionState.Inferring:
                    _trainingStatus.text = "이미지 예측 중";
                    _trainingStopButton.gameObject.SetActive(false);
                    break;
                default:
                    _trainingStatus.text = "학습 중";
                    break;
            }
        }

        private void HandleStartClicked()
        {
            if (isActiveAndEnabled)
            {
                OnStartRequested?.Invoke();
            }
        }

        private void HandleStopClicked()
        {
            if (isActiveAndEnabled)
            {
                OnStopRequested?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (_trainingStartButton != null)
            {
                _trainingStartButton.onClick.RemoveListener(HandleStartClicked);
            }
            if (_trainingStopButton != null)
            {
                _trainingStopButton.onClick.RemoveListener(HandleStopClicked);
            }
        }
    }
}
