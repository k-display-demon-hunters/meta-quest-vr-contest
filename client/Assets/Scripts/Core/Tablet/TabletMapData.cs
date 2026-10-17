using System.Collections.Generic;
using UnityEngine;

namespace Tablet
{
    // Scene code supplies geometry; the tablet only projects and displays it.
    public sealed class TabletMapData
    {
        public IReadOnlyList<Floor> Floors { get; }
        public IReadOnlyList<Rect> Walls { get; }
        public IReadOnlyList<Parchment> Parchments { get; }
        public Rect Bounds { get; }

        public TabletMapData(IEnumerable<Floor> floors, IEnumerable<Rect> walls,
            IEnumerable<Parchment> parchments, Rect bounds)
        {
            Floors = new List<Floor>(floors).AsReadOnly();
            Walls = new List<Rect>(walls).AsReadOnly();
            Parchments = new List<Parchment>(parchments).AsReadOnly();
            Bounds = bounds;
        }

        public readonly struct Floor
        {
            public Rect Bounds { get; }
            public string Name { get; }

            public Floor(Rect bounds, string name)
            {
                Bounds = bounds;
                Name = name;
            }
        }

        public readonly struct Parchment
        {
            public Behaviour Source { get; }
            public int LessonIndex { get; }
            public bool IsActive => Source != null && Source.isActiveAndEnabled;
            public Vector3 Position => Source != null ? Source.transform.position : Vector3.zero;

            public Parchment(Behaviour source, int lessonIndex)
            {
                Source = source;
                LessonIndex = lessonIndex;
            }
        }
    }
}
