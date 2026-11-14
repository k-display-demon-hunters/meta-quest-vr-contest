using UnityEngine;

namespace CNNConvolution
{
    internal enum FocusLayout
    {
        Picture,
        PictureAndFormula,
        PictureAndMap
    }

    // The prefab owns panel positions. The lesson chooses which right-hand panel to show.
    internal sealed class FocusLayoutController
    {
        private readonly Transform _formula;
        private readonly Transform _output;

        public FocusLayoutController(Transform formula, Transform output)
        {
            _formula = formula;
            _output = output;
        }

        public void Show(FocusLayout layout)
        {
            _formula.gameObject.SetActive(layout == FocusLayout.PictureAndFormula);
            _output.gameObject.SetActive(layout == FocusLayout.PictureAndMap);
        }
    }
}
