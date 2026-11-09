using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class InteractionManager
    {
        // Trigger activates the hovered target. Grip should never hold a corridor control.
        private sealed class ClickOnly : IXRSelectFilter
        {
            public bool canProcess => true;
            public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => false;
        }

        private sealed class Binding
        {
            internal XRSimpleInteractable Target;
            internal Action<Vector3?> Hover;
            internal UnityAction<HoverEnterEventArgs> Enter;
            internal UnityAction<HoverExitEventArgs> Exit;
            internal UnityAction<ActivateEventArgs> Activate;
        }

        private const float DoubleClickWindow = .4f;
        private static readonly ClickOnly SelectFilter = new ClickOnly();
        [SerializeField] private ZoneSelectionController _zones;
        private readonly List<Binding> _bindings = new List<Binding>();
        private bool _active;
        private XRRayInteractor _hoverRay;
        private Binding _hover;
        private XRSimpleInteractable _lastTarget;
        private float _lastTime;
        private bool _showingHover;

        internal event Action<int> ZoneSelected;
        internal string HoverName => _showingHover ? _hover.Target.name : "—";
        internal string LastClick { get; private set; } = "—";

        internal void Initialize()
        {
            _zones.Bind((target, zone) => Bind(target, _ => ZoneSelected?.Invoke(zone)));
        }

        internal void Bind(XRSimpleInteractable target, Action<Vector3> click,
            Action<Vector3> doubleClick = null, Action<Vector3?> hover = null)
        {
            var binding = new Binding { Target = target, Hover = hover };
            binding.Enter = args =>
            {
                if (args.interactorObject is XRRayInteractor ray &&
                    ray.handedness == InteractorHandedness.Right)
                {
                    ClearHover();
                    _hover = binding;
                    _hoverRay = ray;
                }
            };
            binding.Exit = args =>
            {
                if (_hover == binding && ReferenceEquals(args.interactorObject, _hoverRay))
                {
                    ClearHover();
                }
            };
            binding.Activate = args =>
            {
                if (args.interactorObject is XRRayInteractor ray &&
                    ray.handedness == InteractorHandedness.Right && !ray.hasSelection &&
                    TryGetHit(ray, target, out Vector3 point))
                {
                    Activate(target, point, click, doubleClick);
                }
            };
            _bindings.Add(binding);
        }

        internal void SetActive(bool active)
        {
            if (_active == active)
            {
                return;
            }

            _active = active;
            if (!active)
            {
                ClearHover();
                _lastTarget = null;
            }

            foreach (var binding in _bindings)
            {
                var target = binding.Target;
                if (active)
                {
                    target.selectFilters.Add(SelectFilter);
                    target.hoverEntered.AddListener(binding.Enter);
                    target.hoverExited.AddListener(binding.Exit);
                    target.activated.AddListener(binding.Activate);
                    target.enabled = true;
                }
                else
                {
                    target.hoverEntered.RemoveListener(binding.Enter);
                    target.hoverExited.RemoveListener(binding.Exit);
                    target.activated.RemoveListener(binding.Activate);
                    target.enabled = false;
                    target.selectFilters.Remove(SelectFilter);
                }
            }
        }

        internal void UpdateHover()
        {
            if (_hover == null)
            {
                return;
            }

            if (TryGetHit(_hoverRay, _hover.Target, out Vector3 point))
            {
                _hover.Hover?.Invoke(point);
                _showingHover = true;
            }
            else if (_showingHover)
            {
                _hover.Hover?.Invoke(null);
                _showingHover = false;
            }
        }

        private void ClearHover()
        {
            if (_showingHover)
            {
                _hover.Hover?.Invoke(null);
            }
            _showingHover = false;
            _hover = null;
            _hoverRay = null;
        }

        private static bool TryGetHit(XRRayInteractor ray, XRSimpleInteractable target, out Vector3 point)
        {
            if (ray.TryGetCurrentRaycast(out var hit, out _, out _, out _, out bool uiClosest) &&
                !uiClosest && hit.HasValue && target.colliders.Contains(hit.Value.collider))
            {
                point = hit.Value.point;
                return true;
            }
            point = default;
            return false;
        }

        private void Activate(XRSimpleInteractable target, Vector3 point, Action<Vector3> click,
            Action<Vector3> doubleClick)
        {
            float now = Time.unscaledTime;
            bool twice = target == _lastTarget && now - _lastTime <= DoubleClickWindow && doubleClick != null;
            _lastTarget = twice ? null : target;
            _lastTime = now;
            LastClick = target.name;
            if (twice)
            {
                doubleClick(point);
            }
            else
            {
                click(point);
            }
        }
    }
}
