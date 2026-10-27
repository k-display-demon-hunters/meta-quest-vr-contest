using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps
{
    public sealed class InputMapController : MonoBehaviour
    {
        [SerializeField] private Texture2D _inputImage;

        public float[,] ReadInput()
        {
            int size = ConvolutionSimulation.Size;
            if (_inputImage == null || !_inputImage.isReadable ||
                _inputImage.width != size || _inputImage.height != size)
            {
                Debug.LogError($"InputMapController requires a readable {size} x {size} input image.", this);
                return null;
            }

            Color[] pixels = _inputImage.GetPixels();
            float[,] input = new float[size, size];
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    // Tensor rows run top to bottom; Unity texture rows run bottom to top.
                    input[row, column] = pixels[(size - 1 - row) * size + column].grayscale;
                }
            }
            return input;
        }
    }
}
