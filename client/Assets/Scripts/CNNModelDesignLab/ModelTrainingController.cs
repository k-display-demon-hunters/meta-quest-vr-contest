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

        public bool IsTraining { get; private set; }

        public event Action OnStartRequested;
        public event Action OnStopRequested;

        private void Awake()
        {
            _trainingStartButton.onClick.AddListener(HandleStartClicked);
            _trainingStopButton.onClick.AddListener(HandleStopClicked);
            SetTrainingState(false);
        }

        public void SetTrainingState(bool isTraining)
        {
            IsTraining = isTraining;
            _trainingStartButton.gameObject.SetActive(!isTraining);
            _trainingStopButton.gameObject.SetActive(isTraining);
            _trainingStatus.gameObject.SetActive(isTraining);
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
