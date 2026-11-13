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
        private CorridorRenderUtility _visuals;
        private Action _selected;
        private bool _active;
        private bool _hovered;
        private Color _tint;

        internal Transform CalculationAnchor => _calculationAnchor;
        internal Vector3 Position => _hit.transform.position;

        internal void Bind(CorridorRenderUtility visuals, Action selected)
        {
            _visuals = visuals;
            _selected = selected;
        }

        internal void SetAvailable(bool available, bool completed)
        {
            _tint = completed ? _visuals.Theme.Cyan :
                available ? _visuals.Theme.Amber : new Color32(113, 120, 123, 255);
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
            float brightness = _hovered ? .8f : .38f;
            _visuals.Paint(_face, new Color(_tint.r * brightness, _tint.g * brightness, _tint.b * brightness));
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
