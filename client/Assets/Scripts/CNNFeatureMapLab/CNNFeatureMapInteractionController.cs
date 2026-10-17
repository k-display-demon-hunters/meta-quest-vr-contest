using System.Collections.Generic;
using UnityEngine;

namespace CNNFeatureMaps
{
    public sealed class CNNFeatureMapInteractionController
    {
        private readonly List<CNNFeatureMapGrabTarget> _heldTargets = new List<CNNFeatureMapGrabTarget>();
        private CNNFeatureMapGrabTarget _scaleTarget;
        public bool HasHeldTargets => _heldTargets.Count > 0;

        public bool CanGrab(CNNFeatureMapGrabTarget candidate)
        {
            foreach (CNNFeatureMapGrabTarget held in _heldTargets)
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
            foreach (CNNFeatureMapGrabTarget held in _heldTargets)
            {
                if (held.transform.IsChildOf(ancestor))
                {
                    return true;
                }
            }
            return false;
        }

        public void HoldTarget(CNNFeatureMapGrabTarget target)
        {
            if (!_heldTargets.Contains(target))
            {
                _heldTargets.Add(target);
            }
        }

        public void ReleaseTarget(CNNFeatureMapGrabTarget target)
        {
            if (_heldTargets.Remove(target))
            {
                SelectScaleTarget(target);
            }
        }

        public void SelectScaleTarget(CNNFeatureMapGrabTarget target)
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
