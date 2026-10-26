namespace ModelDesignScene.ModelBuilder
{
    // Unity object references for the belt; ModelDefinition owns the layer slots.
    internal sealed class ConveyorSlots
    {
        private readonly ModelBlockController[] _blocks;

        internal int Count => _blocks.Length;
        internal ModelBlockController this[int index] => _blocks[index];

        internal ConveyorSlots(int count)
        {
            _blocks = new ModelBlockController[count];
        }

        internal bool TryPlace(int index, ModelBlockController block)
        {
            if (index < 0 || _blocks[index] != null)
            {
                return false;
            }
            _blocks[index] = block;
            return true;
        }

        internal int Remove(ModelBlockController block)
        {
            for (int index = 0; index < _blocks.Length; index++)
            {
                if (_blocks[index] == block)
                {
                    _blocks[index] = null;
                    return index;
                }
            }
            return -1;
        }
    }
}
