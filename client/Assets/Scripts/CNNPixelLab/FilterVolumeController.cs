using System;
using UnityEngine;

namespace UI
{
    internal sealed class FilterVolumeController : IDisposable
    {
        private readonly Mesh _mesh;
        private readonly Material _frameMaterial;
        private readonly Material _pixelMaterial;
        private readonly MeshRenderer[] _pixels = new MeshRenderer[9];
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        private readonly Color32[] _source;
        private readonly int _columns;
        private readonly GameObject _frame;
        private readonly GameObject _volume;

        public FilterVolumeController(Transform filter, MeshRenderer lens, Color32[] source, int columns)
        {
            BoxCollider grabBounds = filter.GetComponent<BoxCollider>();
            if (grabBounds != null)
            {
                grabBounds.size = new Vector3(0.54f, 0.54f, 0.18f);
                grabBounds.center = new Vector3(0, 0, -0.02f);
            }
            _source = source;
            _columns = columns;
            _mesh = PixelVolumeMesh.Create();
            _frameMaterial = PixelVolumeMesh.CreateMaterial(false);
            _frameMaterial.SetColor("_BaseColor", new Color(0.28f, 0.32f, 0.37f));
            _pixelMaterial = PixelVolumeMesh.CreateMaterial(true);
            // Keep existing grab colliders, but replace the thin visual bars and flat lens.
            foreach (MeshRenderer renderer in filter.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.enabled = false;
            }
            _frame = new GameObject("Solid filter frame");
            _frame.transform.SetParent(filter, false);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                PixelVolumeMesh.Add(_frame.transform, "Vertical rim", _mesh, _frameMaterial,
                    new Vector3(sign * 0.247f, 0, 0), new Vector3(0.035f, 0.53f, 0.11f));
                PixelVolumeMesh.Add(_frame.transform, "Horizontal rim", _mesh, _frameMaterial,
                    new Vector3(0, sign * 0.247f, 0), new Vector3(0.46f, 0.035f, 0.11f));
            }
            _volume = new GameObject("Selected pixel volume");
            _volume.transform.SetParent(lens.transform, false);
            Vector3 scale = lens.transform.localScale;
            _volume.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            for (int i = 0; i < 9; i++)
            {
                _pixels[i] = PixelVolumeMesh.Add(_volume.transform, "Pixel " + i, _mesh, _pixelMaterial,
                    new Vector3((i % 3 - 1) * 0.153f, (i / 3 - 1) * 0.153f, -0.035f),
                    new Vector3(0.145f, 0.145f, 0.13f));
            }
        }

        public void SetOrigin(Vector2Int origin)
        {
            for (int i = 0; i < 9; i++)
            {
                Color color = _source[(origin.y + i / 3) * _columns + origin.x + i % 3];
                color.a = 0.94f;
                _properties.SetColor("_BaseColor", color);
                _pixels[i].SetPropertyBlock(_properties);
            }
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(_frame);
            UnityEngine.Object.Destroy(_volume);
            UnityEngine.Object.Destroy(_mesh);
            UnityEngine.Object.Destroy(_frameMaterial);
            UnityEngine.Object.Destroy(_pixelMaterial);
        }
    }
}
