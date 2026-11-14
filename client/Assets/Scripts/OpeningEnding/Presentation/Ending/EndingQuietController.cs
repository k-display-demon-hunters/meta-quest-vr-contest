using UnityEngine;
namespace Cinematic
{
    internal sealed class EndingQuietController
    {
        private readonly Camera _camera;
        private readonly Transform _standingMarker, _holdHint;
        private Color _previousBackground;
        private CameraClearFlags _previousClearFlags;
        private bool _quiet;
        internal EndingQuietController(Camera camera, Transform standingMarker, Transform holdHint)
        {
            _camera = camera;
            _standingMarker = standingMarker;
            _holdHint = holdHint;
        }
        internal void SetQuiet(bool quiet)
        {
            if (quiet == _quiet) { return; }
            _quiet = quiet;
            if (quiet)
            {
                _previousBackground = _camera.backgroundColor;
                _previousClearFlags = _camera.clearFlags;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = Color.black;
            }
            else if (_camera != null)
            {
                _camera.backgroundColor = _previousBackground;
                _camera.clearFlags = _previousClearFlags;
            }

            _standingMarker.gameObject.SetActive(!quiet);
            _holdHint.gameObject.SetActive(!quiet);
        }

        internal void Restore()
        {
            if (_quiet && _camera != null)
            {
                _camera.backgroundColor = _previousBackground;
                _camera.clearFlags = _previousClearFlags;
                _quiet = false;
            }
        }

    }
}
