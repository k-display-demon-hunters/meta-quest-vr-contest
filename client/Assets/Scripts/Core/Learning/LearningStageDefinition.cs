using System;
using System.Collections.Generic;

namespace Learning
{
    public sealed class LearningStageDefinition
    {
        public LearningStageId Id { get; }
        public string Title { get; }
        public bool CountsForProgress { get; }
        public IReadOnlyList<int> EntranceParchmentIds { get; }
        public IReadOnlyList<int> ParchmentIds { get; }

        internal LearningStageDefinition(LearningStageId id, string title,
            bool countsForProgress, int[] entranceParchmentIds, params int[] interiorParchmentIds)
        {
            Id = id;
            Title = title;
            CountsForProgress = countsForProgress;
            EntranceParchmentIds = Array.AsReadOnly((int[])entranceParchmentIds.Clone());
            List<int> documents = new List<int>(entranceParchmentIds);
            documents.AddRange(interiorParchmentIds);
            ParchmentIds = documents.AsReadOnly();
        }
    }
}
