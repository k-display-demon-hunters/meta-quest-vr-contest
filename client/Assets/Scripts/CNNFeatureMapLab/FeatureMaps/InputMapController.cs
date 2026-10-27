using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps
{
    public sealed class InputMapController : MonoBehaviour
    {
        [SerializeField] private Texture2D _inputImage;

        public bool TryReadInput(out float[,] input)
        {
            input = null;
            int size = ConvolutionSimulation.Size;
            if (_inputImage == null || !_inputImage.isReadable ||
                _inputImage.width != size || _inputImage.height != size)
            {
                Debug.LogError($"InputMapController requires a readable {size} x {size} input image.", this);
                return false;
            }

            Color[] pixels = _inputImage.GetPixels();
            input = new float[size, size];
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    // Tensor rows run top to bottom; Unity texture rows run bottom to top.
                    input[row, column] = pixels[(size - 1 - row) * size + column].grayscale;
                }
            }
            return true;
        }
    }
}
