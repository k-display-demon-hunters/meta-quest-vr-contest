using System.Collections.Generic;

namespace FeatureMapCorridor
{
    public interface ICnnModel
    {
        int Size
        {
            get;
        }
        int LayerSize(int layer);
        int[] MapsPerLayer
        {
            get;
        }
        float[][] Input(int digit);
        LayerOutput[][] Forward(int digit, ISet<string> offMaps);
        float[, ] Kernel(int layer, int map, int src);
        float Bias(int layer, int map);
        float Scale(int layer, int map);
        float[] OutputTemplate(int digit, int map);
        float[] Scores(LayerOutput[][] forward);
        float[] Probabilities(LayerOutput[][] forward);
    }
    public struct LayerOutput
    {
        public float[] pre;
        public float[] act;
        public bool dead;
    }
}
