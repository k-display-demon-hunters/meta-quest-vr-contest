using System;
using System.Collections.Generic;

namespace Learning
{
    public sealed class LearningStageDefinition
    {
        public LearningStageId Id { get; }
        public string Title { get; }
        public IReadOnlyList<int> EntranceParchmentIds { get; }
        public IReadOnlyList<int> ParchmentIds { get; }

        internal LearningStageDefinition(LearningStageId id, string title,
            int[] entranceParchmentIds)
        {
            Id = id;
            Title = title;
            EntranceParchmentIds = Array.AsReadOnly((int[])entranceParchmentIds.Clone());
            ParchmentIds = EntranceParchmentIds;
        }
    }
}
