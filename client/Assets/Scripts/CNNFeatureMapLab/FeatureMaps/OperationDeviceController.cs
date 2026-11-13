using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class OperationDeviceController : IXRSelectFilter
    {
        [SerializeField] private XRSimpleInteractable _hit;
        [SerializeField] private Renderer _face;
        [SerializeField] private Transform _calculationAnchor;
        [SerializeField] private Renderer[] _kernelPreview;
        private CorridorRenderUtility _visuals;
        private Action _selected;
        private bool _active;
        private bool _hovered;
        private bool _pulse;
        private float _pulseBrightness = 1f;
        private Color _tint;

        internal Transform CalculationAnchor => _calculationAnchor;
        internal Vector3 FilterSplitOrigin => _hit.transform.TransformPoint(0, 0, -.25f);
        internal void SetVisible(bool visible) => _hit.gameObject.SetActive(visible);
        internal Pose GetLessonPanelPose(bool showingCalculation) => new Pose(
            showingCalculation
                ? _calculationAnchor.TransformPoint(Vector3.up * 1.65f)
                : _hit.transform.TransformPoint(Vector3.up * 1.5f),
            _calculationAnchor.rotation);

        internal void Bind(CorridorRenderUtility visuals, Action selected)
        {
            _visuals = visuals;
            _selected = selected;
        }

        internal void ShowKernel(RoundCalculationResult data)
        {
            for (int cell = 0; cell < _kernelPreview.Length; cell++)
            {
                _visuals.Paint(_kernelPreview[cell], _visuals.KernelColor(data.Weight(0, cell)));
            }
        }

        internal void SetHighlight(bool pulse)
        {
            _pulse = pulse;
            _pulseBrightness = 1f;
            _tint = _face.sharedMaterial.color;
            RefreshFace();
        }

        internal void TickHighlight(float now)
        {
            if (!_pulse) return;
            _pulseBrightness = .58f + .42f * Mathf.Sin(now * 5f);
            RefreshFace();
        }

        internal void SetInputActive(bool active)
        {
            if (_active == active) return;
            _active = active;
            if (active)
            {
                _hit.selectFilters.Add(this);
                _hit.hoverEntered.AddListener(OnHoverEntered);
                _hit.hoverExited.AddListener(OnHoverExited);
                _hit.activated.AddListener(OnActivated);
            }
            else
            {
                _hit.activated.RemoveListener(OnActivated);
                _hit.hoverEntered.RemoveListener(OnHoverEntered);
                _hit.hoverExited.RemoveListener(OnHoverExited);
                _hit.selectFilters.Remove(this);
                _hovered = false;
                RefreshFace();
            }
            _hit.enabled = active;
        }

        public bool canProcess => true;
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => false;

        private void OnHoverEntered(HoverEnterEventArgs args) => SetHovered(args.interactorObject, true);
        private void OnHoverExited(HoverExitEventArgs args) => SetHovered(args.interactorObject, false);

        private void SetHovered(IXRHoverInteractor interactor, bool hovered)
        {
            if (interactor is not XRRayInteractor { handedness: InteractorHandedness.Right }) return;
            _hovered = hovered;
            RefreshFace();
        }

        private void RefreshFace()
        {
            _visuals.Paint(_face, _pulse
                ? Color.Lerp(_tint, _visuals.Theme.Amber, _hovered ? 1f : _pulseBrightness)
                : _tint);
        }

        private void OnActivated(ActivateEventArgs args)
        {
            if (args.interactorObject is not XRRayInteractor ray ||
                ray.handedness != InteractorHandedness.Right || ray.hasSelection) return;
            if (ray.TryGetCurrentRaycast(out var hit, out _, out _, out _, out bool uiClosest) &&
                !uiClosest && hit.HasValue && _hit.colliders.Contains(hit.Value.collider))
            {
                _selected();
            }
        }
    }
}
