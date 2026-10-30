using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CNNFeatureMaps
{
    public sealed class FilterComparisonManager : MonoBehaviour
    {
        [SerializeField] private FilterScanController _redFilter;
        [SerializeField] private FilterScanController _edgeFilter;
        [SerializeField] private Button _completeButton;
        [SerializeField] private GameObject _popup;
        [SerializeField] private Text _popupBody;
        [SerializeField] private Button _confirmButton;
        private readonly Queue<bool> _pendingExplanations = new Queue<bool>();
        private bool _redAnnounced;
        private bool _edgeAnnounced;
        public event Action OnCompleted;

        private void OnEnable()
        {
            _redFilter.OnStateChanged += HandleRedChanged;
            _edgeFilter.OnStateChanged += HandleEdgeChanged;
            _completeButton.onClick.AddListener(Complete);
            _confirmButton.onClick.AddListener(ConfirmExplanation);
            RefreshCompletion();
        }

        private void HandleRedChanged()
        {
            HandleResult(true, _redFilter.Completed, ref _redAnnounced);
        }

        private void HandleEdgeChanged()
        {
            HandleResult(false, _edgeFilter.Completed, ref _edgeAnnounced);
        }

        private void HandleResult(bool redFilter, bool completed, ref bool announced)
        {
            if (!completed) { announced = false; }
            else if (!announced)
            {
                announced = true;
                _pendingExplanations.Enqueue(redFilter);
                if (!_popup.activeSelf) { ShowNextExplanation(); }
            }
            RefreshCompletion();
        }

        private void ShowNextExplanation()
        {
            if (_pendingExplanations.Count == 0) { return; }
            bool redFilter = _pendingExplanations.Dequeue();
            _popupBody.text = redFilter
                ? "빨간색을 검출하는 필터예요!\n\n빨간색이 많은 영역일수록\n결과가 더 선명한 빨강으로 나타나요."
                : "세로 경계를 검출하는 필터예요!\n\n이웃한 두 칸의 밝기 차이를 비교해\n가운데 경계를 한 줄로 보여줘요.";
            _popup.SetActive(true);
        }

        private void ConfirmExplanation()
        {
            _popup.SetActive(false);
            ShowNextExplanation();
            RefreshCompletion();
        }

        private void RefreshCompletion()
        {
            _completeButton.interactable = _redFilter.Completed && _edgeFilter.Completed
                && !_redFilter.IsScanning && !_edgeFilter.IsScanning
                && !_popup.activeSelf && _pendingExplanations.Count == 0;
        }

        private void Complete()
        {
            if (!_completeButton.interactable) { return; }
            _completeButton.interactable = false;
            OnCompleted?.Invoke();
        }

        private void OnDisable()
        {
            _redFilter.OnStateChanged -= HandleRedChanged;
            _edgeFilter.OnStateChanged -= HandleEdgeChanged;
            _completeButton.onClick.RemoveListener(Complete);
            _confirmButton.onClick.RemoveListener(ConfirmExplanation);
            _pendingExplanations.Clear();
            _popup.SetActive(false);
        }
    }
}
