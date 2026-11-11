using System;
using System.Collections.Generic;
namespace RealityLab
{
    public sealed class RealityRecognitionData
    {
        public string RequestId { get; }
        public IReadOnlyList<float> Pixels { get; }
        public int Width => 28;
        public int Height => 28;
        public int Channels => 3;
        public RealityRecognitionData(string requestId, float[] pixels)
        {
            RequestId = requestId;
            Pixels = Array.AsReadOnly((float[])pixels.Clone());
        }
        public float[] CopyInput()
        {
            float[] copy = new float[Pixels.Count];
            for (int i = 0; i < copy.Length; i++) { copy[i] = Pixels[i]; }
            return copy;
        }
    }
}
