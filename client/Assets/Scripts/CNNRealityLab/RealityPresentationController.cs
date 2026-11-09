using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RealityLab
{
    public sealed class RealityPresentationController
    {
        private readonly Dictionary<Renderer, bool> _renderers = new Dictionary<Renderer, bool>();
        private readonly Dictionary<Canvas, bool> _canvases = new Dictionary<Canvas, bool>();
        private readonly Dictionary<Light, bool> _lights = new Dictionary<Light, bool>();
        public void Hide(Scene source)
        {
            foreach (GameObject root in source.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                { _renderers.Add(renderer, renderer.forceRenderingOff); renderer.forceRenderingOff = true; }
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                { _canvases.Add(canvas, canvas.enabled); canvas.enabled = false; }
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                { _lights.Add(light, light.enabled); light.enabled = false; }
            }
        }
        public void Restore()
        {
            foreach (var item in _renderers) { if (item.Key != null) { item.Key.forceRenderingOff = item.Value; } }
            foreach (var item in _canvases) { if (item.Key != null) { item.Key.enabled = item.Value; } }
            foreach (var item in _lights) { if (item.Key != null) { item.Key.enabled = item.Value; } }
            _renderers.Clear(); _canvases.Clear(); _lights.Clear();
        }
    }
}
