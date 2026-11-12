using System;
using System.Collections.Generic;

namespace Learning
{
    public static class LearningStageCatalog
    {
        public static IReadOnlyList<LearningStageDefinition> Stages { get; } =
            Array.AsReadOnly(new[]
            {
                new LearningStageDefinition(LearningStageId.Tutorial, "튜토리얼", Array.Empty<int>()),
                new LearningStageDefinition(LearningStageId.ScreenLab, "픽셀 관찰", new[] { 0 }),
                new LearningStageDefinition(LearningStageId.ConvolutionLab, "합성곱", new[] { 0 }),
                new LearningStageDefinition(LearningStageId.FeatureMap, "특징 맵", new[] { 1 }),
                new LearningStageDefinition(LearningStageId.ModelDesign, "모델 제작", new[] { 1 })
            });

        // Preserve the existing tablet card order. Overall progress uses all Stages.
        public static IReadOnlyList<LearningStageDefinition> ProgressStages { get; } =
            Array.AsReadOnly(new[]
            {
                Get(LearningStageId.ScreenLab),
                Get(LearningStageId.ConvolutionLab),
                Get(LearningStageId.FeatureMap),
                Get(LearningStageId.ModelDesign)
            });

        public static LearningStageDefinition Get(LearningStageId stage)
        {
            foreach (LearningStageDefinition definition in Stages)
            {
                if (definition.Id == stage) { return definition; }
            }
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown learning stage.");
        }

        public static string GetSceneName(LearningStageId stage) => stage switch
        {
            LearningStageId.Tutorial => "FactoryTutorial",
            LearningStageId.ScreenLab => "CNNScreenLab",
            LearningStageId.ConvolutionLab => "CNNConvolutionLab",
            LearningStageId.FeatureMap => "CNNFeatureMapLab",
            LearningStageId.ModelDesign => "CNNModelDesignLab",
            LearningStageId.Exit => "CNNArchiveEnding",
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
