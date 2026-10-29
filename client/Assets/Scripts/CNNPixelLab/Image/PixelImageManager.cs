using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UI
{
    public sealed class PixelImageManager : IDisposable
    {
        public const int Columns = 32;
        public const int Rows = 18;

        private readonly Transform _board;
        private readonly XRGrabInteractable _filter;
        private readonly PixelRevealStage _reveal;
        private readonly PixelSelectionController _selection;
        private readonly Texture2D _smoothTexture;
        private readonly Texture2D _pixelTexture;
        private readonly Material _boardMaterial;
        private readonly Material _lensMaterial;
        private bool _backgroundHidden;
        private bool _boardWasActive;
        private bool _filterWasActive;

        public event Action SelectionChanged;
        public event Action FilterGrabbed;
        public event Action PixelsRevealed;

        public IReadOnlyList<Color32> Pixels { get; }
        public bool HasSelection => _selection.HasSelection;
        public bool IsFilterHeld => _filter.isSelected;
        public Vector2Int SelectedPixel => _selection.SelectedPixel;
        public Vector3 SelectionWorldPosition => _selection.SelectionWorldPosition;
        public bool IsReadyForConvolution => _reveal != null && _reveal.IsReadyForConvolution;
        public bool IsRevealing => _reveal != null
            && (_reveal.Phase == PixelRevealStage.RevealPhase.AwaitingClick
                || _reveal.Phase == PixelRevealStage.RevealPhase.Revealing);
        public bool CanBeginReveal => _reveal != null && !IsFilterHeld
            && _reveal.Phase == PixelRevealStage.RevealPhase.Inactive;

        public PixelImageManager(Texture2D sourceImage, Transform board, MeshRenderer boardRenderer,
            Vector2 boardSize, XRGrabInteractable filter, MeshRenderer lensRenderer,
            Transform selectionFrame, PixelRevealStage reveal)
        {
            _board = board;
            _filter = filter;
            _reveal = reveal;
            _smoothTexture = PixelImageFactory.CreateSmoothPhoto(sourceImage);
            Color32[] pixels = PixelImageFactory.SamplePixels(_smoothTexture, Columns, Rows);
            Pixels = Array.AsReadOnly(pixels);
            _pixelTexture = PixelImageFactory.CreateTexture(pixels, Columns, Rows, FilterMode.Point);
            _boardMaterial = boardRenderer.material;
            _boardMaterial.mainTexture = _smoothTexture;
            _lensMaterial = lensRenderer.material;
            _lensMaterial.mainTexture = _pixelTexture;
            _selection = new PixelSelectionController(board, boardSize, filter,
                lensRenderer, selectionFrame, _lensMaterial, Columns, Rows, pixels);
        }

        public void EnableInput()
        {
            _filter.selectEntered.AddListener(HandleGrabbed);
            _filter.selectExited.AddListener(HandleReleased);
            if (_reveal != null)
            {
                _reveal.PixelsRevealed += HandlePixelsRevealed;
            }
        }

        public void DisableInput(Transform contentRoot)
        {
            _filter.selectEntered.RemoveListener(HandleGrabbed);
            _filter.selectExited.RemoveListener(HandleReleased);
            if (_reveal != null)
            {
                _reveal.PixelsRevealed -= HandlePixelsRevealed;
            }
            if (_filter.isSelected)
            {
                if (_filter.interactionManager != null)
                {
                    _filter.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_filter);
                }
                _filter.transform.SetParent(contentRoot, true);
                _selection.HandleReleased();
            }
        }

        public void UpdateSelection()
        {
            if (!IsRevealing && !IsReadyForConvolution && _selection.UpdateSelection())
            {
                SelectionChanged?.Invoke();
            }
        }

        public void Reset()
        {
            if (_reveal != null)
            {
                _reveal.ResetStage();
            }
            _filter.gameObject.SetActive(true);
            _selection.ResetSelection();
        }

        public void BeginReveal()
        {
            _selection.SuspendSelection();
            _filter.gameObject.SetActive(false);
            _reveal.ShowImage(Pixels, Columns, Rows, _selection.SelectedPixel);
            _reveal.RevealPixels();
        }

        public void CompleteReveal()
        {
            _reveal.CompleteReveal();
        }

        public void MoveSelection(int dx, int dy)
        {
            _selection.MoveSelection(dx, dy);
        }

        public void SetBackgroundHidden(bool hidden)
        {
            if (_backgroundHidden == hidden)
            {
                return;
            }
            if (hidden)
            {
                _boardWasActive = _board.gameObject.activeSelf;
                _filterWasActive = _filter.gameObject.activeSelf;
            }
            _board.gameObject.SetActive(hidden ? false : _boardWasActive);
            _filter.gameObject.SetActive(hidden ? false : _filterWasActive);
            _backgroundHidden = hidden;
        }

        private void HandleGrabbed(SelectEnterEventArgs args)
        {
            _selection.HandleGrabbed();
            FilterGrabbed?.Invoke();
        }

        private void HandleReleased(SelectExitEventArgs args)
        {
            _selection.HandleReleased();
        }

        private void HandlePixelsRevealed()
        {
            PixelsRevealed?.Invoke();
        }


        public void Dispose()
        {
            _selection.Dispose();
            UnityEngine.Object.Destroy(_smoothTexture);
            UnityEngine.Object.Destroy(_pixelTexture);
            UnityEngine.Object.Destroy(_boardMaterial);
            UnityEngine.Object.Destroy(_lensMaterial);
        }
    }
}
