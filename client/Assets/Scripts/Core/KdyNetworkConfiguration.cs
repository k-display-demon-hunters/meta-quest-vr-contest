using System;

namespace KdyLearning
{
    [Serializable]
    public sealed class KdyNetworkConfiguration
    {
        public int LayerCount { get; set; }
        public int FeatureMaps { get; set; }
        public int KernelSize { get; set; }
        public bool UseSigmoid { get; set; }
        public bool HasFeatureMaps { get; set; }
        public bool HasKernel { get; set; }
        public bool HasActivation { get; set; }

        public bool IsComplete => LayerCount >= 1 && LayerCount <= 3 &&
            HasFeatureMaps && (FeatureMaps == 4 || FeatureMaps == 32) &&
            HasKernel && (KernelSize == 3 || KernelSize == 9) && HasActivation;

        public void CopyFrom(KdyNetworkConfiguration other)
        {
            LayerCount = other.LayerCount;
            FeatureMaps = other.FeatureMaps;
            KernelSize = other.KernelSize;
            UseSigmoid = other.UseSigmoid;
            HasFeatureMaps = other.HasFeatureMaps;
            HasKernel = other.HasKernel;
            HasActivation = other.HasActivation;
        }

        public void Clear()
        {
            LayerCount = 0;
            FeatureMaps = 0;
            KernelSize = 0;
            UseSigmoid = false;
            HasFeatureMaps = false;
            HasKernel = false;
            HasActivation = false;
        }
    }
}
