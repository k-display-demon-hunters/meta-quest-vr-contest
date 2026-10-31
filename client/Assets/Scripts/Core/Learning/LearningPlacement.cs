using UnityEngine;

namespace Learning
{
    public readonly struct LearningPlacement
    {
        public Pose Pose { get; }
        public Vector3 Scale { get; }
        public bool UseEnvironmentLighting { get; }

        public LearningPlacement(Pose pose, Vector3 scale, bool useEnvironmentLighting)
        {
            Pose = pose;
            Scale = scale;
            UseEnvironmentLighting = useEnvironmentLighting;
        }
    }
}
