using System;
using System.Collections.Generic;

namespace Learning
{
    public sealed class LearningStageDefinition
    {
        public LearningStageId Id { get; }
        public RoomId Room { get; }
        public string Title { get; }
        public bool CountsForProgress { get; }
        public IReadOnlyList<int> EntranceParchmentIds { get; }

        internal LearningStageDefinition(LearningStageId id, RoomId room, string title,
            bool countsForProgress, params int[] entranceParchmentIds)
        {
            Id = id;
            Room = room;
            Title = title;
            CountsForProgress = countsForProgress;
            EntranceParchmentIds = Array.AsReadOnly((int[])entranceParchmentIds.Clone());
        }
    }
}
