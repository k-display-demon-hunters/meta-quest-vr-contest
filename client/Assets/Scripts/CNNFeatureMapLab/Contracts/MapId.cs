namespace FeatureMapCorridor
{
    internal readonly struct MapId
    {
        internal int Layer
        {
            get;
        }

        internal int Map
        {
            get;
        }

        internal MapId(int layer, int map)
        {
            Layer = layer;
            Map = map;
        }

    }
}
