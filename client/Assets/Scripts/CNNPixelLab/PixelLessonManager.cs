using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    // Owns RGB observation and calculation; the scene owns progress between lessons.
    public sealed class PixelLessonManager : MonoBehaviour
    {
        private readonly int[,] _weights =
        {
            { -1, 0, 1, -1, 0, 1, -1, 0, 1 },
            { -1, 0, 1, -1, 0, 1, -1, 0, 1 },
            { -1, 0, 1, -1, 0, 1, -1, 0, 1 }
        };
        private readonly int[,] _inputs = new int[3, 9];
        private RgbConvolutionView _view;
        private SpatialConvolutionController _observation;
        private ConvolutionExercise _exercise;
        private Vector3 _selectionWorldPosition;
        private Vector2Int _selected;
        private int _columns;
        private int _rows;
        private bool _combined;
        private bool _busy;
        private bool _initialized;

        public bool IsVisible => gameObject.activeSelf;
        public Vector3 PreviewFocus => transform.position + transform.right * 0.35f;

        public event Action CloseRequested;
        public event Action AdvanceRequested;
        public event Action<bool> BackgroundHiddenChanged;
        public event Action<int, int> SelectionMoveRequested;
        public event Action Completed;

        private void OnEnable()
        {
            if (_initialized)
            {
                SubscribeView();
            }
        }

        public void Initialize(Font font)
        {
            _view = gameObject.AddComponent<RgbConvolutionView>();
            _view.Initialize(font);
            GameObject observation = new GameObject("RGB layers in space");
            observation.transform.SetParent(_view.ObservationRoot, false);
            _observation = observation.AddComponent<SpatialConvolutionController>();
            _observation.Initialize(font);
            _exercise = _view.ExerciseRoot.gameObject.AddComponent<ConvolutionExercise>();
            _exercise.Initialize(font);
            _initialized = true;
            if (isActiveAndEnabled)
            {
                SubscribeView();
            }
        }

        private void Update()
        {
            if (_initialized)
            {
                _view.ShowObservationProgress(_observation.Volume.HasPendingAction, _busy);
            }
        }

        public void Show(IReadOnlyList<Color32> pixels, int columns, int rows, Vector2Int selected,
            Vector3 selectionWorldPosition, bool calculationAvailable)
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            _observation.ReleaseInteractions();
            _exercise.Close();
            _busy = false;
            _combined = false;
            _selectionWorldPosition = selectionWorldPosition;
            _selected = selected;
            _columns = columns;
            _rows = rows;
            for (int channel = 0; channel < 3; channel++)
            {
                for (int i = 0; i < 9; i++)
                {
                    Color32 pixel = pixels[(selected.y + 2 - i / 3) * columns + selected.x + i % 3];
                    _inputs[channel, i] = channel == 0 ? pixel.r : channel == 1 ? pixel.g : pixel.b;
                }
            }
            _view.ShowObservation();
            _observation.SetData(_inputs, _weights);
            if (calculationAvailable)
            {
                BeginCalculation();
            }
            else
            {
                StartCoroutine(ShowObservation());
            }
        }

        public void Close()
        {
            // Return held objects while their content parent is still active.
            StopAllCoroutines();
            _observation.ReleaseInteractions();
            _exercise.Close();
            gameObject.SetActive(false);
        }

        private IEnumerator ShowObservation(bool extract = true)
        {
            SetBusy(true);
            yield return _observation.Observe(_selectionWorldPosition, extract);
            BackgroundHiddenChanged?.Invoke(true);
            SetBusy(false);
        }

        private void AdvanceObservation()
        {
            if (_observation.Volume.CompletePendingAction() || _busy)
            {
                return;
            }
            AdvanceRequested?.Invoke();
        }

        private void BeginCalculation()
        {
            if (_busy)
            {
                return;
            }
            _observation.ReleaseInteractions();
            _view.ShowCalculation();
            _exercise.SelectionSource = _selectionWorldPosition;
            _exercise.Begin(_inputs, _weights, CompleteExercise);
        }

        private void ToggleCombined()
        {
            if (!_busy)
            {
                StartCoroutine(Combine());
            }
        }

        private IEnumerator Combine()
        {
            SetBusy(true);
            _combined = !_combined;
            yield return _observation.SpreadColours(_combined);
            _view.ShowCombined(_combined);
            SetBusy(false);
        }

        private void CompleteExercise(int[] sums)
        {
            Completed?.Invoke();
            _view.ShowCompleted(sums, _selected, _columns, _rows);
            StartCoroutine(ShowObservation(false));
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            _view.SetObservationBusy(busy);
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void HandleSelectionMoveRequested(int dx, int dy)
        {
            SelectionMoveRequested?.Invoke(dx, dy);
        }

        private void SubscribeView()
        {
            _view.CombineRequested += ToggleCombined;
            _view.RestartRequested += BeginCalculation;
            _view.AdvanceRequested += AdvanceObservation;
            _view.CloseRequested += HandleCloseRequested;
            _view.SelectionMoveRequested += HandleSelectionMoveRequested;
        }

        private void UnsubscribeView()
        {
            _view.CombineRequested -= ToggleCombined;
            _view.RestartRequested -= BeginCalculation;
            _view.AdvanceRequested -= AdvanceObservation;
            _view.CloseRequested -= HandleCloseRequested;
            _view.SelectionMoveRequested -= HandleSelectionMoveRequested;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _busy = false;
            if (_initialized)
            {
                UnsubscribeView();
                _observation.ReleaseInteractions();
                _exercise.Close();
            }
        }
    }
}
