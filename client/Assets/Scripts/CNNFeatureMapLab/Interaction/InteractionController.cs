using System.Collections.Generic;
using UnityEngine;

namespace CNNFeatureMaps.Interaction
{
    public sealed class InteractionController
    {
        private readonly List<GrabTarget> _heldTargets = new List<GrabTarget>();
        private GrabTarget _scaleTarget;
        public bool HasHeldTargets => _heldTargets.Count > 0;

        public bool CanGrab(GrabTarget candidate)
        {
            foreach (GrabTarget held in _heldTargets)
            {
                if (held != candidate && (held.transform.IsChildOf(candidate.transform) || candidate.transform.IsChildOf(held.transform)))
                {
                    return false;
                }
            }
            return true;
        }

        public bool HasHeldDescendant(Transform ancestor)
        {
            foreach (GrabTarget held in _heldTargets)
            {
                if (held.transform.IsChildOf(ancestor))
                {
                    return true;
                }
            }
            return false;
        }

        public void HoldTarget(GrabTarget target)
        {
            if (!_heldTargets.Contains(target))
            {
                _heldTargets.Add(target);
            }
        }

        public void ReleaseTarget(GrabTarget target)
        {
            if (_heldTargets.Remove(target))
            {
                SelectScaleTarget(target);
            }
        }

        public void SelectScaleTarget(GrabTarget target)
        {
            if (target.CanScale)
            {
                _scaleTarget = target;
            }
        }

        public bool ScaleSelectedTarget(float multiplier)
        {
            if (_scaleTarget == null || !_scaleTarget.isActiveAndEnabled || HasHeldTargets)
            {
                return false;
            }
            _scaleTarget.ScaleBy(multiplier);
            return true;
        }
    }
}
