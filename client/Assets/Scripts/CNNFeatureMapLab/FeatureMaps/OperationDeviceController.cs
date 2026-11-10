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

        internal Transform CalculationAnchor => _calculationAnchor;
        internal Vector3 Position => _hit.transform.position;

        internal void Bind(CorridorRenderUtility visuals, Action selected)
        {
            _visuals = visuals;
            _selected = selected;
        }

        internal void SetAvailable(bool available, bool completed)
        {
            Color tint = completed ? _visuals.Theme.Cyan :
                available ? _visuals.Theme.Amber : _visuals.Theme.Dim;
            _visuals.Paint(_face, new Color(tint.r * .38f, tint.g * .38f, tint.b * .38f));
        }

        internal void SetInputActive(bool active)
        {
            if (_active == active) return;
            _active = active;
            if (active)
            {
                _hit.selectFilters.Add(this);
                _hit.activated.AddListener(OnActivated);
            }
            else
            {
                _hit.activated.RemoveListener(OnActivated);
                _hit.selectFilters.Remove(this);
            }
            _hit.enabled = active;
        }

        public bool canProcess => true;
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => false;

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
