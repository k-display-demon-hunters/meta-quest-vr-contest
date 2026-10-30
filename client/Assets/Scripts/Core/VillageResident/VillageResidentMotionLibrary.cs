using System;
using UnityEngine;

namespace FactoryIntro
{
    // Selected CC0 Quaternius source curves, retargeted to the village's existing proportions.
    // The Unity asset stores the original 30 fps samples; no external model or runtime importer is needed.
    public sealed class VillageResidentMotionLibrary : ScriptableObject
    {
        public enum Motion { Idle, Talking, Walk, HitChest, HitHead, Crouch, Dance }
        public const int JointCount = 12;

        [Serializable]
        public sealed class Frame
        {
            public float GroundClearance;
            public Quaternion[] Rotations;
        }

        [Serializable]
        public sealed class Clip
        {
            public string SourceName;
            public float Duration;
            public Frame[] Frames;
        }

        [SerializeField] private Clip[] _clips;

        public void Sample(Motion motion, float time, bool loop, Frame result)
        {
            Clip clip = _clips[(int)motion];
            float normalized = loop ? Mathf.Repeat(time, clip.Duration) / clip.Duration : Mathf.Clamp01(time / clip.Duration);
            float cursor = normalized * (clip.Frames.Length - 1);
            int first = Mathf.FloorToInt(cursor);
            int second = Mathf.Min(first + 1, clip.Frames.Length - 1);
            float blend = cursor - first;
            Frame a = clip.Frames[first];
            Frame b = clip.Frames[second];
            result.GroundClearance = Mathf.Lerp(a.GroundClearance, b.GroundClearance, blend);
            for (int i = 0; i < JointCount; i++)
            {
                result.Rotations[i] = Quaternion.Slerp(a.Rotations[i], b.Rotations[i], blend);
            }
        }

        public static Frame NewFrame()
        {
            return new Frame { Rotations = new Quaternion[JointCount] };
        }
    }
}
