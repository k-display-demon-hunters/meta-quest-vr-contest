namespace ModelDesignScene.Execution
{
    public sealed class ValidationIssue
    {
        public int? SlotIndex { get; }
        public string Message { get; }

        internal ValidationIssue(int? slotIndex, string message)
        {
            SlotIndex = slotIndex;
            Message = message;
        }
    }
}
