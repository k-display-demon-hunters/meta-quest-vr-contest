using System;
using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps
{
    public sealed class MapLayer : MonoBehaviour
    {
        [SerializeField] private MapPanel[] _maps;
        [SerializeField] private int _layerIndex;
        [SerializeField] private float _spreadSpacing = .64f;
        private Func<Transform, bool> _hasHeldDescendant;
        private Vector3[] _stackPositions;
        private bool _expanded;
        private bool _animating;
        public bool IsExpanded => _expanded;
        public int Index => _layerIndex;

        public void Initialize(Func<Transform, bool> hasHeldDescendant)
        {
            _hasHeldDescendant = hasHeldDescendant;
        }

        private void Awake()
        {
            _stackPositions = new Vector3[_maps.Length];
            for (int index = 0; index < _maps.Length; index++)
            {
                _stackPositions[index] = _maps[index].transform.localPosition;
            }
        }

        private void Update()
        {
            if (!_animating)
            {
                return;
            }
            bool settled = true;
            float blend = 1f - Mathf.Exp(-9f * Time.deltaTime);
            for (int index = 0; index < _maps.Length; index++)
            {
                MapPanel map = _maps[index];
                // Extracted maps and their free cell nodes remain where the user put them.
                if (map.IsExtracted)
                {
                    continue;
                }
                if (_hasHeldDescendant(map.transform))
                {
                    settled = false;
                    continue;
                }
                Vector3 destination = _expanded ? new Vector3((index - (_maps.Length - 1) * .5f) * _spreadSpacing, .08f, -.55f) :
                    _stackPositions[index];
                map.transform.localPosition = Vector3.Lerp(map.transform.localPosition, destination, blend);
                if ((map.transform.localPosition - destination).sqrMagnitude > .000001f)
                {
                    settled = false;
                }
                else
                {
                    map.transform.localPosition = destination;
                }
            }
            _animating = !settled;
        }

        public void ToggleSpread()
        {
            _expanded = !_expanded;
            _animating = true;
        }

        public void Collapse()
        {
            _expanded = false;
            _animating = false;
        }
    }
}

