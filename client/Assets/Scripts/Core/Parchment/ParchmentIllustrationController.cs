using System;
using UnityEngine;

namespace Parchment
{
    public sealed class ParchmentIllustrationController : MonoBehaviour
    {
        [SerializeField] private GameObject[] _panels;

        public void Show(ParchmentIllustrationType illustration)
        {
            int index;
            switch (illustration)
            {
                case ParchmentIllustrationType.PixelGrid: index = 0; break;
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
        }
    }
}
