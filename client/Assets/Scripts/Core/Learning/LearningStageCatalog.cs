using System;
using System.Collections.Generic;

namespace Learning
{
    public static class LearningStageCatalog
    {
        public static IReadOnlyList<LearningStageDefinition> Stages { get; } =
            Array.AsReadOnly(new[]
            {
                new LearningStageDefinition(LearningStageId.Tutorial, RoomId.Tutorial, "튜토리얼", false),
                new LearningStageDefinition(LearningStageId.PixelObservation, RoomId.PixelLab, "픽셀", true, 4),
                new LearningStageDefinition(LearningStageId.Convolution, RoomId.PixelLab, "합성곱", true, 0),
                new LearningStageDefinition(LearningStageId.FeatureMap, RoomId.FeatureMapLab, "특징 맵", true, 1),
                new LearningStageDefinition(LearningStageId.ModelDesign, RoomId.ModelDesignLab, "모델 제작", true, 2, 3)
            });

        public static IReadOnlyList<LearningStageDefinition> ProgressStages { get; } = CreateProgressStages();

        private static IReadOnlyList<LearningStageDefinition> CreateProgressStages()
        {
            List<LearningStageDefinition> stages = new List<LearningStageDefinition>();
            foreach (LearningStageDefinition stage in Stages)
            {
                if (stage.CountsForProgress) { stages.Add(stage); }
            }
            return stages.AsReadOnly();
        }

        public static LearningStageDefinition Get(LearningStageId stage)
        {
            foreach (LearningStageDefinition definition in Stages)
            {
                if (definition.Id == stage) { return definition; }
            }
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown learning stage.");
        }

        public static LearningStageId Next(LearningStageId stage)
        {
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].Id == stage)
                {
                    return i + 1 < Stages.Count ? Stages[i + 1].Id : LearningStageId.None;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown learning stage.");
        }

        public static string GetSceneName(RoomId room) => room switch
        {
            RoomId.Tutorial => "FactoryTutorial",
            RoomId.PixelLab => "CNNPixelLab",
            RoomId.FeatureMapLab => "CNNFeatureMapLab",
            RoomId.ModelDesignLab => "CNNModelDesignLab",
            RoomId.Exit => "FactoryEnding",
            _ => throw new ArgumentOutOfRangeException(nameof(room), room, "Unknown learning room.")
        };

        public static LearningStageId GetParchmentEntranceStage(int documentId)
        {
            foreach (LearningStageDefinition stage in Stages)
            {
                foreach (int assignedId in stage.EntranceParchmentIds)
                {
                    if (assignedId == documentId) { return stage.Id; }
                }
            }
            return LearningStageId.None;
        }
    }
}
