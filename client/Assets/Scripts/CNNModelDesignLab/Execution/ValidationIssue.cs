namespace ModelDesignScene.Execution
{
    public enum ValidationIssueKind
    {
        Unmapped,
        FlattenRequired,
        SpatialInputRequired,
        SpatialLayerAfterVector
    }

    public sealed class ValidationIssue
    {
        public int? SlotIndex { get; }
        public ValidationIssueKind Kind { get; }

        internal ValidationIssue(int? slotIndex, ValidationIssueKind kind)
        {
            SlotIndex = slotIndex;
            Kind = kind;
        }
    }
}
