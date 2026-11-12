using System;
using UnityEngine;

namespace Parchment
{
    public sealed class ParchmentIllustrationController : MonoBehaviour
    {
        [SerializeField] private GameObject[] _panels;
        [SerializeField] private ParchmentDigitIllustration _digits;

        internal void Play()
        {
            if (_digits.isActiveAndEnabled) _digits.Play();
        }

        public void Show(ParchmentIllustrationType illustration, bool animate)
        {
            int index;
            switch (illustration)
            {
                case ParchmentIllustrationType.PixelDigit:
                case ParchmentIllustrationType.GrayscaleDigit:
                case ParchmentIllustrationType.RgbDigit:
                    index = 0;
                    break;
                case ParchmentIllustrationType.PaddingGrid: index = 1; break;
                case ParchmentIllustrationType.VerticalEdgeFilter: index = 2; break;
                case ParchmentIllustrationType.FeatureMaps: index = 3; break;
                case ParchmentIllustrationType.ReluGraph: index = 4; break;
                default: throw new ArgumentOutOfRangeException(nameof(illustration));
            }
            for (int i = 0; i < _panels.Length; i++)
            {
                _panels[i].SetActive(i == index);
            }

            if (index == 0) _digits.Show(illustration, animate);
        }
    }
}
