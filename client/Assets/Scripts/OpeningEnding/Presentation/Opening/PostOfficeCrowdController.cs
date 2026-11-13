using System;
using UnityEngine;

namespace Cinematic
{
    public sealed class PostOfficeCrowdController : MonoBehaviour
    {
        [Serializable]
        private sealed class Person
        {
            public Transform Root;
            public Animation Animation;
            public string Clip;
            public Vector3 Start;
            public Vector3 End;
            public float TravelSeconds;
            public float Phase;
            public float Facing;
        }

        [SerializeField] private Person[] _people;

        internal void Evaluate(float elapsed)
        {
            foreach (Person person in _people)
            {
                float time = elapsed + person.Phase;
                if (person.TravelSeconds > 0f)
                {
                    float cycle = Mathf.Repeat(time / person.TravelSeconds, 2f);
                    bool returning = cycle >= 1f;
                    float progress = returning ? 2f - cycle : cycle;
                    person.Root.localPosition = Vector3.Lerp(person.Start, person.End, progress);
                    Vector3 direction = returning ? person.Start - person.End : person.End - person.Start;
                    person.Root.localRotation = Quaternion.LookRotation(direction);
                }
                else
                {
                    person.Root.localRotation = Quaternion.Euler(0f, person.Facing + Mathf.Sin(time * .7f) * 5f, 0f);
                }

                AnimationState state = person.Animation[person.Clip];
                state.enabled = true;
                state.weight = 1f;
                state.time = Mathf.Repeat(time, state.length);
                person.Animation.Sample();
                state.enabled = false;
            }
        }
    }
}
