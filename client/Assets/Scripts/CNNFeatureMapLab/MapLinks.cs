using System.Collections.Generic;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class MapLinks
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
        private CorridorVisuals _visuals;
        internal bool Dimmed { private get; set; }


        internal void Bind(CorridorVisuals visuals, Transform root, InputPanel input, FeatureMapPanel[][] maps, OutputStage output)
        {
            _visuals = visuals;
            int index = 0;
            for (int layer = 1; layer <= maps.Length; layer++)
            {
                int count = layer == 1 ? 1 : maps[layer-2].Length;
                for (int source = 0; source < count; source++)
                foreach (FeatureMapPanel target in maps[layer-1])
                {
                    Vector3 start = layer == 1 ? input.Picture.Root.position : maps[layer-2][source].Root.position;
                    Position(_links[index++].Line, start, target.Root.position, root.up * .33f);
                }
            }
            for (int source = 0; source < maps[2].Length; source++)
            for (int digit = 1; digit <= 3; digit++)
                Position(_links[index++].Line, maps[2][source].Root.position, root.TransformPoint(output.Tin(digit)), root.up * .33f);
        }

        private static void Position(LineRenderer line, Vector3 start, Vector3 end, Vector3 lift)
        {
            line.SetPosition(0, line.transform.InverseTransformPoint(start));
            line.SetPosition(1, line.transform.InverseTransformPoint((start + end) * .5f + lift));
            line.SetPosition(2, line.transform.InverseTransformPoint(end));
        }


        private MapLinks() { }

#if UNITY_EDITOR
        internal MapLinks(CorridorVisuals visuals, Transform root, InputPanel input, FeatureMapPanel[][] maps, OutputStage output)
        {
            _visuals = visuals;
            for (int l = 1; l <= maps.Length; l++)
            {
                int count = l == 1 ? 1 : maps[l-2].Length;
                for (int source = 0; source < count; source++)
                foreach (FeatureMapPanel target in maps[l-1])
                {
                    Vector3 start = l == 1 ? input.Picture.Position : maps[l-2][source].Position;
                    Add(root,start,target.Position,l,l == 1 ? "" : CorridorMath.MapKey(l-1,source));
                }
            }
            for (int source = 0; source < maps[2].Length; source++)
            for (int digit = 1; digit <= 3; digit++)
                Add(root,maps[2][source].Position,output.Tin(digit),4,CorridorMath.MapKey(3,source));
        }
#endif

#if UNITY_EDITOR
        private void Add(Transform root, Vector3 start, Vector3 end, int destination, string source)
        {
            var line = CorridorVisuals.Group(root,"Map link",Vector3.zero).gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 3;
            line.SetPosition(0,start);
            line.SetPosition(1,(start+end)*.5f+Vector3.up*.33f);
            line.SetPosition(2,end);
            _visuals.Line(line,_visuals.Theme.Cyan,.016f);
            _links.Add(new Link { Line = line, Destination = destination, Source = source });
        }
#endif

        internal void Celebrate(int layer) => _amberUntil[layer] = Time.unscaledTime+20;

        internal void Tick(float now, ISet<string> offMaps)
        {
            float pulse = .21f+.05f*Mathf.Sin(now*1.7f);
            foreach (Link link in _links)
            {
                bool off = offMaps.Contains(link.Source);
                bool amber = !Dimmed && !off && now < _amberUntil[link.Destination];
                Color color = off ? _visuals.Theme.Negative : amber ? _visuals.Theme.Amber : _visuals.Theme.Cyan;
                color.a = off ? .1f : amber ? .85f : Dimmed ? .04f : pulse;
                _visuals.Paint(link.Line,color);
            }
        }
    }
}
