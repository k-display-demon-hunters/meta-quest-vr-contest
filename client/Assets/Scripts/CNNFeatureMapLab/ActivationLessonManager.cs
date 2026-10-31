using System;
using UnityEngine;
using UnityEngine.UI;

namespace CNNFeatureMaps.Activation
{
    public sealed class ActivationLessonManager : MonoBehaviour
    {
        [SerializeField] private ActivationMapController _display;
        [SerializeField] private GameObject _content;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Text _applyLabel;
        [SerializeField] private Button _mapButton;
        [SerializeField] private TextMesh _mapName;
        [SerializeField] private Button _completeButton;
        [SerializeField] private GameObject _popup;
        [SerializeField] private Text _popupBody;
        [SerializeField] private Button _confirmButton;
        private float[][,] _maps;
        private int _channel;
        private bool _applied;
        private bool _completing;
        public event Action OnCompleted;

        private void OnEnable()
        {
            _applyButton.onClick.AddListener(Apply);
            _mapButton.onClick.AddListener(ChangeMap);
            _completeButton.onClick.AddListener(Complete);
            _confirmButton.onClick.AddListener(Confirm);
            _display.OnApplied += HandleApplied;
        }

        private void OnDisable()
        {
            _applyButton.onClick.RemoveListener(Apply);
            _mapButton.onClick.RemoveListener(ChangeMap);
            _completeButton.onClick.RemoveListener(Complete);
            _confirmButton.onClick.RemoveListener(Confirm);
            _display.OnApplied -= HandleApplied;
        }

        public void BeginLesson(float[,] redMap, float[,] edgeMap)
        {
            _maps = new[] { redMap, edgeMap };
            _channel = 0;
            _completing = false;
            gameObject.SetActive(true);
            _content.SetActive(true);
            ShowCurrentMap();
            ShowPopup(ActivationLessonContent.Intro);
        }

        private void ShowCurrentMap()
        {
            _applied = false;
            float[,] before = _maps[_channel];
            float[,] after = new float[before.GetLength(0), before.GetLength(1)];
            for (int row = 0; row < before.GetLength(0); row++)
            {
                for (int column = 0; column < before.GetLength(1); column++)
                {
                    after[row, column] = Mathf.Max(0f, before[row, column]);
                }
            }
            _mapName.text = ActivationLessonContent.MapName(_channel);
            _display.ShowMap(before, after);
            _applyLabel.text = "ReLU 적용";
            RefreshButtons();
        }

        private void Apply()
        {
            if (_popup.activeSelf || _display.IsAnimating || _completing) { return; }
            _applied = false;
            _applyLabel.text = "적용 중";
            _display.Apply();
            RefreshButtons();
        }

        private void ChangeMap()
        {
            if (_popup.activeSelf || _display.IsAnimating || _completing) { return; }
            _channel = 1 - _channel;
            ShowCurrentMap();
        }

        private void HandleApplied()
        {
            _applied = true;
            _applyLabel.text = "다시 실행";
            ShowPopup(ActivationLessonContent.Applied);
        }

        private void ShowPopup(string message)
        {
            // Keep the explanation readable: the comparison and its labels return on confirmation.
            _content.SetActive(false);
            _popupBody.text = message;
            _popup.SetActive(true);
            RefreshButtons();
        }

        private void Confirm()
        {
            _popup.SetActive(false);
            if (_completing) { OnCompleted?.Invoke(); return; }
            _content.SetActive(true);
            RefreshButtons();
        }

        private void Complete()
        {
            if (!_completeButton.interactable) { return; }
            _completing = true;
            ShowPopup(ActivationLessonContent.Completed);
        }

        private void RefreshButtons()
        {
            bool available = !_display.IsAnimating && !_popup.activeSelf && !_completing;
            _applyButton.interactable = available;
            _mapButton.interactable = available;
            _completeButton.interactable = available && _applied;
        }
    }
}
