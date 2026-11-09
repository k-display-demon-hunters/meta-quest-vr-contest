using System.Collections.Generic;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class MapLinksController
    {
        [System.Serializable]
        private struct Link
        {
            public LineRenderer Line;
            public int Destination;
            public string Source;
        }
        [SerializeField] private List<Link> _links = new List<Link>();
        private float[] _amberUntil = new float[5];
        private CorridorRenderUtility _visuals;
        internal bool Dimmed
        {
            private get;
            set;
        }

        internal void Bind(CorridorRenderUtility visuals, Vector3[] starts, Vector3[] ends, Vector3 lift)
        {
            _visuals = visuals;
            for (int i = 0;i<_links.Count;i++)
            {
                Position(_links[i].Line, starts[i], ends[i], lift);
            }
        }

        private static void Position(LineRenderer line, Vector3 start, Vector3 end, Vector3 lift)
        {
            line.SetPosition(0, line.transform.InverseTransformPoint(start));
            line.SetPosition(1, line.transform.InverseTransformPoint((start + end) * .5f + lift));
            line.SetPosition(2, line.transform.InverseTransformPoint(end));
        }

        private MapLinksController()
        {
        }

        internal void Celebrate(int layer) => _amberUntil[layer] = Time.unscaledTime+20;
        internal void Tick(float now, FeatureMapSnapshot snapshot)
        {
            float pulse = .21f+.05f*Mathf.Sin(now*1.7f);
            foreach (Link link in _links)
            {
                bool off = snapshot.IsOff(link.Source);
                bool amber = !Dimmed && !off && now < _amberUntil[link.Destination];
                Color color = off ? _visuals.Theme.Negative : amber ? _visuals.Theme.Amber : _visuals.Theme.Cyan;
                color.a = off ? .1f : amber ? .85f : Dimmed ? .04f : pulse;
                _visuals.Paint(link.Line, color);
            }
        }
    }
}
