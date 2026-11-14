using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Cinematic
{
    internal sealed class EndingArchiveTrail
    {
        private const float RetreatDuration = 1.2f;
        private const float CrossfadeDuration = .4f;
        private const float FinalFadeDuration = .8f;
        private static readonly Vector2[] FinaleOffsets =
        {
            new Vector2(-1.08f, .26f), new Vector2(-1.08f, -.26f),
            new Vector2(1.08f, .26f), new Vector2(1.08f, -.26f)
        };
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        private readonly Vector3 _front;
        private readonly Vector3 _photoSize;
        private readonly Panel[] _panels;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        private readonly int _panelCount;

        [System.Serializable]
        internal sealed class Panel
        {
            public Transform Root;
            public MeshRenderer Photo;
            public Text Year;
            [System.NonSerialized] internal Material Material;
            [System.NonSerialized] internal Vector3 LabelPosition;
        }

        internal EndingArchiveTrail(Transform front, Panel[] panels, int panelCount)
        {
            _front = front.localPosition;
            _photoSize = front.localScale;
            _panels = panels;
            _panelCount = panelCount;
            for (int i = 0; i < panels.Length; i++)
            {
                Panel panel = panels[i];
                panel.Root.gameObject.SetActive(false);
                if (i >= panelCount) { continue; }
                panel.LabelPosition = panel.Year.transform.parent.localPosition;
                panel.Material = new Material(panel.Photo.sharedMaterial)
                {
                    renderQueue = (int)RenderQueue.Transparent - panelCount + i,
                    name = "Ending history photo"
                };
                panel.Photo.sharedMaterial = panel.Material;
            }
        }

        internal void SetPhoto(int index, Texture2D still, Rect crop, string year)
        {
            Panel panel = _panels[index];
            panel.Material.SetTexture(BaseMap, still);
            panel.Material.SetTextureScale("_BaseMap", crop.size);
            panel.Material.SetTextureOffset("_BaseMap", crop.position);
            panel.Year.text = year;
            panel.Root.name = "Retired photo " + year;
        }

        internal void Evaluate(float time, float[] starts, float outroStart, float quietStart)
        {
            float fade = 1f - Mathf.SmoothStep(0f, 1f, (time - quietStart + FinalFadeDuration) / FinalFadeDuration);
            float finale = Mathf.SmoothStep(0f, 1f, (time - outroStart - .3f) / 1.4f);
            for (int i = 0; i < _panelCount; i++)
            {
                Panel panel = _panels[i];
                int firstDeparture = i + 1;
                float departure = firstDeparture < starts.Length ? starts[firstDeparture] : outroStart;
                float elapsed = time - departure;
                bool visible = elapsed > 0f && fade > 0f;
                panel.Root.gameObject.SetActive(visible);
                if (!visible) { continue; }

                float firstStep = Mathf.SmoothStep(0f, 1f, elapsed / RetreatDuration);
                float depth = firstStep;
                for (int step = firstDeparture + 1; step <= starts.Length; step++)
                {
                    float stepStart = step < starts.Length ? starts[step] : outroStart;
                    depth += Mathf.SmoothStep(0f, 1f, (time - stepStart) / RetreatDuration);
                }

                Vector3 retreatPosition = _front + new Vector3(.35f * depth, .15f * depth, .6f * depth);
                bool centerpiece = i == _panelCount - 1;
                Vector2 offset = centerpiece ? Vector2.zero : FinaleOffsets[i % FinaleOffsets.Length];
                Vector3 finalePosition = _front + new Vector3(offset.x, offset.y + .16f, centerpiece ? .23f : .48f);
                float finaleScale = centerpiece ? .58f : .31f;
                panel.Root.localPosition = Vector3.Lerp(retreatPosition, finalePosition, finale);
                panel.Photo.transform.localScale = new Vector3(
                    _photoSize.x * Mathf.Lerp(1f, finaleScale, finale),
                    _photoSize.y * Mathf.Lerp(1f, finaleScale, finale), _photoSize.z);

                Transform yearCanvas = panel.Year.transform.parent;
                Vector3 finaleLabelPosition = new Vector3(0f, -_photoSize.y * finaleScale * .5f - .045f, -.02f);
                yearCanvas.localPosition = Vector3.Lerp(panel.LabelPosition, finaleLabelPosition, finale);

                // The foreground shader already carries the outgoing frame during its 0.4 s blend.
                float reveal = firstDeparture < starts.Length
                    ? Mathf.SmoothStep(0f, 1f, elapsed / CrossfadeDuration) : 1f;
                float opacity = Mathf.Lerp(Mathf.Lerp(1f, .35f, firstStep), centerpiece ? 1f : .78f, finale);
                opacity *= reveal * fade;
                _properties.SetColor(BaseColor, new Color(1f, 1f, 1f, opacity));
                panel.Photo.SetPropertyBlock(_properties);
                float labelAlpha = Mathf.Lerp(.7f * firstStep, .92f, finale) * reveal * fade;
                panel.Year.color = new Color(.84f, .93f, .95f, labelAlpha);
            }
        }

        internal void HidePhotos()
        {
            foreach (Panel panel in _panels) { panel.Root.gameObject.SetActive(false); }
        }

        internal void Dispose()
        {
            for (int i = 0; i < _panelCount; i++) { Object.Destroy(_panels[i].Material); }
        }
    }
}
