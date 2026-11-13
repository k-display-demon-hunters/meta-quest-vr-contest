using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class TrainingFlowController : MonoBehaviour
    {
        private const float PulseDuration = 0.32f;
        private static readonly Color PeakColor = new Color(1f, 0.76f, 0.38f);

        [SerializeField] private Material _outlineMaterial;

        private Coroutine _animation;
        private ModelBlockOutline _outline;
        private bool _finishCurrentPass;

        internal void Play(IReadOnlyList<ModelBlockController> blocks)
        {
            Cancel();
            _finishCurrentPass = false;
            _outline = new ModelBlockOutline(transform, "Training flow outline", _outlineMaterial);
            _animation = StartCoroutine(Animate(blocks));
        }

        internal void FinishCurrentPass() => _finishCurrentPass = true;

        internal void Cancel()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                Clear();
            }
        }

        private IEnumerator Animate(IReadOnlyList<ModelBlockController> blocks)
        {
            do
            {
                foreach (ModelBlockController block in blocks)
                {
                    _outline.Show(block.transform, block.VisualBounds);
                    for (float elapsed = 0f; elapsed < PulseDuration; elapsed += Time.unscaledDeltaTime)
                    {
                        float pulse = Mathf.Sin(Mathf.PI * elapsed / PulseDuration);
                        _outline.SetAppearance(Color.Lerp(ModelBlockOutline.Amber, PeakColor, pulse),
                            Mathf.Lerp(0.022f, 0.042f, pulse));
                        yield return null;
                    }
                }
            }
            while (!_finishCurrentPass);

            Clear();
        }

        private void Clear()
        {
            _outline.Remove();
            _outline = null;
            _animation = null;
        }

        private void OnDisable() => Cancel();
    }
}
