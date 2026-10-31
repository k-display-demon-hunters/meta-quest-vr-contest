using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public sealed class PixelRevealStage : MonoBehaviour
    {
        [SerializeField] private GameObject _pixelRoot;
        [SerializeField] private Transform[] _columns;
        [SerializeField] private MeshRenderer[] _blocks;
        [SerializeField] private RgbPixelStackController _rgbStack;
        [SerializeField] private RgbValuesUIController _rgbValues;
        [SerializeField] private float _revealDuration = 2f;
        private IReadOnlyList<Color32> _pixels;
        private int _rows;
        private Vector2Int _selected;
        private MaterialPropertyBlock _properties;
        public PixelObservationPhase Phase { get; private set; }
        public bool IsReadyForConvolution => Phase == PixelObservationPhase.Revealed;
        public event Action PixelsRevealed;
        public event Action<PixelObservationPhase> PhaseChanged;

        private void OnEnable() { _rgbStack.ProgressChanged += HandleRgbProgress; }
        private void OnDisable()
        {
            _rgbStack.ProgressChanged -= HandleRgbProgress;
            ResetStage();
        }
        public void ShowImage(IReadOnlyList<Color32> pixels, int columns, int rows, Vector2Int selected)
        {
            _rgbValues.Initialize();
            _pixels = pixels;
            _rows = rows;
            _selected = selected;
            _properties = new MaterialPropertyBlock();
            _pixelRoot.SetActive(false);
            Phase = PixelObservationPhase.AwaitingClick;
        }
        public void RevealPixels(PixelPatchData patch)
        {
            SetPhase(PixelObservationPhase.Revealing);
            StartCoroutine(Reveal(patch));
        }
        public void ResetStage()
        {
            StopAllCoroutines();
            _pixelRoot.SetActive(false);
            _rgbStack.Hide();
            _rgbValues.Hide();
            SetPhase(PixelObservationPhase.Inactive);
        }
        private IEnumerator Reveal(PixelPatchData patch)
        {
            SetPhase(PixelObservationPhase.Unfolding);
            _rgbValues.ShowValues(patch);
            yield return _rgbStack.Unfold(patch, _columns.Length, _rows, _selected);
            SetPhase(PixelObservationPhase.Revealed);
            PixelsRevealed?.Invoke();
        }
        private void SetColumn(int column, float progress)
        {
            Vector3 position = _columns[column].localPosition;
            position.z = (1 - progress) * 0.1f;
            _columns[column].localPosition = position;
            for (int y = 0; y < _rows; y++)
            {
                int index = y * _columns.Length + column;
                Color color = _pixels[index];
                color.a = progress * 0.94f;
                _properties.SetColor("_BaseColor", color);
                _blocks[index].SetPropertyBlock(_properties);
                _blocks[index].enabled = color.a > 0.001f;
            }
        }
        private void SetPhase(PixelObservationPhase phase) { Phase = phase; PhaseChanged?.Invoke(phase); }
        private void HandleRgbProgress(int channel, float alpha) { _rgbValues.SetProgress(channel, alpha); }
    }
}
