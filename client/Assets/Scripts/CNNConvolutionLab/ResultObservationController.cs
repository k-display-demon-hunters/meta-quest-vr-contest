using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    public sealed class ResultObservationController : MonoBehaviour
    {
        private RgbVolumeController _spatial;
        private LessonUIController _ui;
        private bool _busy, _combined;
        public event Action ReturnRequested;
        public void Initialize(RgbVolumeController spatial, LessonUIController ui)
        {
            _spatial = spatial;
            _ui = ui;
            enabled = false;
        }
        public void Show(int[] sums)
        {
            enabled = true;
            _ui.ShowCompleted(sums);
            _combined = false;
            StartCoroutine(Observe());
        }
        private IEnumerator Observe()
        {
            _busy = true;
            yield return _spatial.Observe(Vector3.zero, false);
            _busy = false;
        }
        public void Advance()
        {
            if (_spatial.CompletePendingAction() || _busy) { return; }
            ReturnRequested?.Invoke();
        }
        public void Combine()
        {
            if (!_busy) { StartCoroutine(CombineColours()); }
        }
        private IEnumerator CombineColours()
        {
            _busy = true; _combined = !_combined;
            yield return _spatial.SpreadColours(_combined);
            _ui.ShowCombined(_combined); _busy = false;
        }
        private void Update()
        {
            _ui.ShowObservationProgress(_spatial.HasPendingAction, _busy);
            _ui.SetObservationBusy(_busy);
        }
        public void Close()
        {
            StopAllCoroutines(); _busy = false;
            _spatial.ReleaseInteractions();
            enabled = false;
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            _busy = false;
            _spatial.ReleaseInteractions();
        }
    }
}
