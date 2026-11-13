using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class CorridorCameraController
    {
        [SerializeField] private Transform _root;
        private Camera _camera;
        internal Camera Camera => _camera;
        internal bool Visible
        {
            get;
            private set;
        }

        internal event Action<bool> VisibilityChanged;
        internal void Tick()
        {
            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
            }
            if (_camera == null)
            {
                return;
            }
            Vector3 view = _root.InverseTransformPoint(_camera.transform.position);
            bool inside = Mathf.Abs(view.x) <= 7.12f && view.z >= -13.4f && view.z <= 21.94f;
            if (Visible == inside)
            {
                return;
            }
            Visible = inside;
            VisibilityChanged?.Invoke(inside);
        }

        internal void Disable()
        {
            Visible = false;
        }
    }
}
