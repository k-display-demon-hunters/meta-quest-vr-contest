using System;
using System.Collections.Generic;

namespace Learning
{
    public static class LearningStageCatalog
    {
        public static IReadOnlyList<LearningStageDefinition> Stages { get; } =
            Array.AsReadOnly(new[]
            {
                new LearningStageDefinition(LearningStageId.Tutorial, "튜토리얼", false, Array.Empty<int>()),
                new LearningStageDefinition(LearningStageId.PixelLab, "픽셀 랩", true, new[] { 4 }, 0),
                new LearningStageDefinition(LearningStageId.FeatureMap, "특징 맵", true, new[] { 1 }),
                new LearningStageDefinition(LearningStageId.ModelDesign, "모델 제작", true, new[] { 2, 3 })
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

        public static string GetSceneName(LearningStageId stage) => stage switch
        {
            LearningStageId.Tutorial => "FactoryTutorial",
            LearningStageId.PixelLab => "CNNPixelLab",
            LearningStageId.FeatureMap => "CNNFeatureMapLab",
            LearningStageId.ModelDesign => "CNNModelDesignLab",
            LearningStageId.Exit => "FactoryEnding",
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown learning stage.")
        };

        public static LearningStageId GetParchmentStage(int documentId)
        {
            foreach (LearningStageDefinition stage in Stages)
            {
                foreach (int assignedId in stage.ParchmentIds)
                {
                    if (assignedId == documentId) { return stage.Id; }
                }
            }
            return LearningStageId.None;
        }
    }
}
