using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    public sealed class FeatureInspectionManager : MonoBehaviour
    {
        [SerializeField] private FeatureInspectionController _controller;

        private float[] _redMap;
        private float[] _boundaryMap;
        private float[] _rotatedBoundaryMap;
        private InspectionFilter _filter;
        private bool _active;
        private bool _rotated;
        private bool _completed;
        private bool _transitioning;

        public event Action Completed;
        public event Action ExitRequested;

        private void OnEnable()
        {
            _controller.FilterARequested += HandleFilterA;
            _controller.FilterBRequested += HandleFilterB;
            _controller.RotationRequested += HandleRotation;
            _controller.FinishRequested += HandleFinish;
        }

        private void OnDisable()
        {
            _controller.FilterARequested -= HandleFilterA;
            _controller.FilterBRequested -= HandleFilterB;
            _controller.RotationRequested -= HandleRotation;
            _controller.FinishRequested -= HandleFinish;
            StopInspection();
        }

        public void BeginInspection()
        {
            StopAllCoroutines();
            _redMap = FeatureInspectionCalculation.CalculateMap(InspectionFilter.RedResponse, false);
            _boundaryMap = FeatureInspectionCalculation.CalculateMap(InspectionFilter.VerticalBoundary, false);
            _rotatedBoundaryMap = FeatureInspectionCalculation.CalculateMap(InspectionFilter.VerticalBoundary, true);
            _filter = InspectionFilter.RedResponse;
            _rotated = false;
            _completed = false;
            _transitioning = false;
            _active = true;
            _controller.ShowDesk();
            RefreshDisplay();
        }

        public void StopInspection()
        {
            _active = false;
            _transitioning = false;
            StopAllCoroutines();
            _controller.HideDesk();
        }

        private void HandleFilterA()
        {
            if (!_active || _transitioning || _rotated) { return; }
            _filter = InspectionFilter.RedResponse;
            RefreshDisplay();
        }

        private void HandleFilterB()
        {
            if (!_active || _transitioning || _rotated) { return; }
            _filter = InspectionFilter.VerticalBoundary;
            bool firstComparison = !_completed;
            _completed = true;
            RefreshDisplay();
            if (firstComparison) { Completed?.Invoke(); }
        }

        private void HandleRotation()
        {
            if (!_active || _transitioning || _filter != InspectionFilter.VerticalBoundary) { return; }
            _transitioning = true;
            StartCoroutine(ChangeSpecimenOrientation());
        }

        private IEnumerator ChangeSpecimenOrientation()
        {
            bool nextOrientation = !_rotated;
            yield return _controller.RotateSpecimen(nextOrientation, FeatureInspectionContent.RotationMessage(nextOrientation));
            _rotated = nextOrientation;
            _transitioning = false;
            RefreshDisplay();
        }

        private void HandleFinish()
        {
            if (!_active || _transitioning || !_completed) { return; }
            _active = false;
            _controller.ShowCompleted(FeatureInspectionContent.CompletedTitle, FeatureInspectionContent.CompletedMessage);
            ExitRequested?.Invoke();
        }

        private void RefreshDisplay()
        {
            float[] scores = _filter == InspectionFilter.RedResponse
                ? _redMap
                : _rotated ? _rotatedBoundaryMap : _boundaryMap;
            FeatureInspectionContent content = FeatureInspectionContent.ForState(_filter, _rotated, _completed);
            _controller.ShowState(content, scores);
        }
    }
}
