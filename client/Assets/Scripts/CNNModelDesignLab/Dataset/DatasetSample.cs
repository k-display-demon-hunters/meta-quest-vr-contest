using UnityEngine;

namespace ModelDesignScene.Dataset
{
    public sealed class DatasetSample
    {
        public string Id { get; }
        public Texture2D Image { get; }
        public int Label { get; }

        internal DatasetSample(string id, Texture2D image, int label)
        {
            Id = id;
            Image = image;
            Label = label;
        }
    }
}
