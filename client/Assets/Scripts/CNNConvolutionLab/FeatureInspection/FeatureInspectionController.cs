using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    // The desk, frames, grid cells and controls are saved in FeatureInspectionDesk.prefab.
    public sealed class FeatureInspectionController : MonoBehaviour
    {
        [SerializeField] private GameObject _deskRoot;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Transform _specimenPivot;
        [SerializeField] private Renderer[] _specimenTiles;
        [SerializeField] private Renderer[] _mapTiles;
        [SerializeField] private Text _mapLabel;
        [SerializeField] private Text _guidance;
        [SerializeField] private Text _specimenLabel;
        [SerializeField] private Text _stateLabel;
        [SerializeField] private Text _rotationButtonLabel;
        [SerializeField] private Button _filterAButton;
        [SerializeField] private Button _filterBButton;
        [SerializeField] private Button _rotationButton;
        [SerializeField] private Button _finishButton;
        [SerializeField] private Image _filterAIndicator;
        [SerializeField] private Image _filterBIndicator;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color IdleCell = new Color(0.035f, 0.055f, 0.065f);
        private static readonly Color RedCell = new Color(0.94f, 0.20f, 0.18f);
        private static readonly Color BoundaryCell = new Color(0.93f, 0.97f, 1f);
        private static readonly Color SelectedLamp = new Color(1f, 0.72f, 0.30f);
        private static readonly Color IdleLamp = new Color(0.17f, 0.23f, 0.26f);
        private const float RotationDuration = 0.4f;
        private MaterialPropertyBlock _tileProperties;

        public event Action FilterARequested;
        public event Action FilterBRequested;
        public event Action RotationRequested;
        public event Action FinishRequested;

        private void Awake()
        {
            _tileProperties = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _filterAButton.onClick.AddListener(HandleFilterA);
            _filterBButton.onClick.AddListener(HandleFilterB);
            _rotationButton.onClick.AddListener(HandleRotation);
            _finishButton.onClick.AddListener(HandleFinish);
        }

        private void OnDisable()
        {
            _filterAButton.onClick.RemoveListener(HandleFilterA);
            _filterBButton.onClick.RemoveListener(HandleFilterB);
            _rotationButton.onClick.RemoveListener(HandleRotation);
            _finishButton.onClick.RemoveListener(HandleFinish);
        }

        public void ShowDesk()
        {
            _canvas.worldCamera = Camera.main;
            _specimenPivot.localRotation = Quaternion.identity;
            _specimenPivot.localScale = Vector3.one;
            for (int row = 0; row < FeatureInspectionCalculation.InputSize; row++)
            {
                for (int column = 0; column < FeatureInspectionCalculation.InputSize; column++)
                {
                    int index = row * FeatureInspectionCalculation.InputSize + column;
                    SetTileColor(_specimenTiles[index], FeatureInspectionCalculation.SpecimenColor(row, column, false));
                }
            }
            _deskRoot.SetActive(true);
        }

        public void HideDesk()
        {
            _deskRoot.SetActive(false);
            _specimenPivot.localRotation = Quaternion.identity;
            _specimenPivot.localScale = Vector3.one;
        }

        public void ShowState(FeatureInspectionContent content, float[] signedScores)
        {
            bool redFilter = content.Filter == InspectionFilter.RedResponse;
            Color activeColor = redFilter ? RedCell : BoundaryCell;
            for (int index = 0; index < _mapTiles.Length; index++)
            {
                float intensity = FeatureInspectionCalculation.DisplayIntensity(content.Filter, signedScores[index]);
                SetTileColor(_mapTiles[index], Color.Lerp(IdleCell, activeColor, intensity));
            }

            _mapLabel.text = content.MapLabel;
            _specimenLabel.text = content.SpecimenLabel;
            _filterAIndicator.color = redFilter ? SelectedLamp : IdleLamp;
            _filterBIndicator.color = redFilter ? IdleLamp : SelectedLamp;
            _guidance.text = content.Guidance;
            _stateLabel.text = content.StateLabel;
            _rotationButtonLabel.text = content.RotationButtonLabel;
            _rotationButton.gameObject.SetActive(content.ShowRotation);
            _filterAButton.interactable = content.CanSwitchFilter;
            _filterBButton.interactable = content.CanSwitchFilter;
            _rotationButton.interactable = true;
            _finishButton.interactable = content.CanFinish;
        }

        public IEnumerator RotateSpecimen(bool rotated, string transitionMessage)
        {
            SetInteractionEnabled(false);
            _stateLabel.text = transitionMessage;
            Quaternion startRotation = _specimenPivot.localRotation;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotated ? -90f : 0f);
            float elapsed = 0f;
            while (elapsed < RotationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / RotationDuration));
                _specimenPivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                // Keep the rotating square within the fixed frame opening.
                float angle = Mathf.Lerp(rotated ? 0f : 90f, rotated ? 90f : 0f, progress) * Mathf.Deg2Rad;
                float fitScale = 1f / (Mathf.Abs(Mathf.Cos(angle)) + Mathf.Abs(Mathf.Sin(angle)));
                _specimenPivot.localScale = Vector3.one * fitScale;
                yield return null;
            }
            _specimenPivot.localRotation = targetRotation;
            _specimenPivot.localScale = Vector3.one;
        }

        public void ShowCompleted(string title, string message)
        {
            SetInteractionEnabled(false);
            _stateLabel.text = title;
            _guidance.text = message;
        }

        private void SetInteractionEnabled(bool enabled)
        {
            _filterAButton.interactable = enabled;
            _filterBButton.interactable = enabled;
            _rotationButton.interactable = enabled;
            _finishButton.interactable = enabled;
        }

        private void SetTileColor(Renderer tile, Color color)
        {
            _tileProperties.Clear();
            _tileProperties.SetColor(BaseColorId, color);
            _tileProperties.SetColor(ColorId, color);
            tile.SetPropertyBlock(_tileProperties);
        }

        private void HandleFilterA() { FilterARequested?.Invoke(); }
        private void HandleFilterB() { FilterBRequested?.Invoke(); }
        private void HandleRotation() { RotationRequested?.Invoke(); }
        private void HandleFinish() { FinishRequested?.Invoke(); }
    }
}
