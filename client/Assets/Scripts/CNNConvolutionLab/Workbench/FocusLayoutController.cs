using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal enum FocusLayout
    {
        Input,
        InputAndProducts,
        Products,
        InputAndOutput,
        Output,
        OutputAndEquation
    }

    // Shows only the panel groups a lesson stage needs, at the focus slots saved in the prefab.
    internal sealed class FocusLayoutController
    {
        // Group order: input, products, output, equations.
        private const int Input = 0, Products = 1, Output = 2;
        private const float LeaveDuration = 0.4f;
        private const float EnterDuration = 0.35f;
        // Never reach zero scale: child poses are still read while a group shrinks.
        private const float HiddenScale = 0.02f;

        private readonly Transform[] _groups;
        private readonly Transform _inputCenter, _inputSide;
        private readonly Transform _gridCenter, _gridSide;
        private readonly Transform _equationCenter, _equationSide;
        private readonly LessonPlayback _playback;

        public FocusLayoutController(Transform inputGroup, Transform productsGroup, Transform outputGroup, Transform equationGroup,
            Transform inputCenter, Transform inputSide, Transform gridCenter, Transform gridSide,
            Transform equationCenter, Transform equationSide, LessonPlayback playback)
        {
            _groups = new[] { inputGroup, productsGroup, outputGroup, equationGroup };
            _inputCenter = inputCenter;
            _inputSide = inputSide;
            _gridCenter = gridCenter;
            _gridSide = gridSide;
            _equationCenter = equationCenter;
            _equationSide = equationSide;
            _playback = playback;
        }

        public void Hide()
        {
            foreach (Transform group in _groups)
            {
                group.gameObject.SetActive(false);
                group.localScale = Vector3.one;
            }
        }

        public IEnumerator Show(FocusLayout layout)
        {
            Transform[] slots = new Transform[_groups.Length];
            Pose[] starts = new Pose[_groups.Length];
            bool leaving = false;
            bool entering = false;
            for (int index = 0; index < _groups.Length; index++)
            {
                slots[index] = Slot(index, layout);
                starts[index] = new Pose(_groups[index].position, _groups[index].rotation);
                bool visible = _groups[index].gameObject.activeSelf;
                leaving |= visible && (slots[index] == null || slots[index].position != starts[index].position);
                entering |= !visible && slots[index] != null;
            }

            if (leaving)
            {
                // Groups that are no longer needed shrink away while the others glide to their slots.
                for (float time = 0f; time < LeaveDuration; time += _playback.DeltaTime)
                {
                    float t = Mathf.SmoothStep(0f, 1f, time / LeaveDuration);
                    for (int index = 0; index < _groups.Length; index++)
                    {
                        Transform group = _groups[index];
                        if (!group.gameObject.activeSelf) { continue; }
                        if (slots[index] == null) { group.localScale = Vector3.one * Mathf.Lerp(1f, HiddenScale, t); }
                        else
                        {
                            group.SetPositionAndRotation(
                                Vector3.Lerp(starts[index].position, slots[index].position, t),
                                Quaternion.Slerp(starts[index].rotation, slots[index].rotation, t));
                        }
                    }
                    yield return null;
                }
            }

            for (int index = 0; index < _groups.Length; index++)
            {
                Transform group = _groups[index];
                if (slots[index] == null)
                {
                    group.gameObject.SetActive(false);
                    group.localScale = Vector3.one;
                    continue;
                }
                group.SetPositionAndRotation(slots[index].position, slots[index].rotation);
                if (group.gameObject.activeSelf) { continue; }
                group.localScale = Vector3.one * HiddenScale;
                group.gameObject.SetActive(true);
            }

            if (!entering) { yield break; }
            for (float time = 0f; time < EnterDuration; time += _playback.DeltaTime)
            {
                float scale = Mathf.Lerp(HiddenScale, 1f, Mathf.SmoothStep(0f, 1f, time / EnterDuration));
                SetEnteringScale(scale);
                yield return null;
            }
            SetEnteringScale(1f);
        }

        private void SetEnteringScale(float scale)
        {
            foreach (Transform group in _groups)
            {
                if (group.gameObject.activeSelf && group.localScale.x < 1f) { group.localScale = Vector3.one * scale; }
            }
        }

        private Transform Slot(int group, FocusLayout layout)
        {
            bool paired = layout == FocusLayout.InputAndProducts || layout == FocusLayout.InputAndOutput;
            switch (group)
            {
                case Input:
                    return paired ? _inputSide : layout == FocusLayout.Input ? _inputCenter : null;
                case Products:
                    return layout == FocusLayout.InputAndProducts ? _gridSide
                        : layout == FocusLayout.Products ? _gridCenter : null;
                case Output:
                    return layout == FocusLayout.InputAndOutput ? _gridSide
                        : layout == FocusLayout.Output || layout == FocusLayout.OutputAndEquation ? _gridCenter : null;
                default:
                    return paired ? _equationSide
                        : layout == FocusLayout.Products || layout == FocusLayout.OutputAndEquation ? _equationCenter : null;
            }
        }
    }
}
