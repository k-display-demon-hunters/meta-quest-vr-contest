using System.Collections.Generic;
using ModelDesignScene.Model;

namespace ModelDesignScene.BlockSettings
{
    internal static class BlockSettingsCatalog
    {
        private static readonly Dictionary<LayerType, BlockSettingsDefinition> Definitions =
            new Dictionary<LayerType, BlockSettingsDefinition>
            {
                [LayerType.Conv2D] = new BlockSettingsDefinition(
                    "Conv2D 설정", "합성곱 필터로 특징을 추출합니다.", true,
                    new BlockSettingsDefinition.NumberField(LayerNumberSetting.Filters,
                        "필터 수", LayerLimits.Conv2DMaxFilters, layer => layer.Filters),
                    new BlockSettingsDefinition.NumberField(LayerNumberSetting.KernelHeight,
                        "커널 높이", LayerLimits.Conv2DMaxKernelSize, layer => layer.Height),
                    new BlockSettingsDefinition.NumberField(LayerNumberSetting.KernelWidth,
                        "커널 너비", LayerLimits.Conv2DMaxKernelSize, layer => layer.Width)),

                [LayerType.MaxPooling2D] = new BlockSettingsDefinition(
                    "MaxPooling2D 설정", "최댓값 풀링으로 특징 맵의 크기를 줄입니다.", false,
                    new BlockSettingsDefinition.NumberField(LayerNumberSetting.PoolHeight,
                        "풀링 높이", LayerLimits.MaxPoolMaxPoolSize, layer => layer.Height),
                    new BlockSettingsDefinition.NumberField(LayerNumberSetting.PoolWidth,
                        "풀링 너비", LayerLimits.MaxPoolMaxPoolSize, layer => layer.Width)),

                [LayerType.Dense] = new BlockSettingsDefinition(
                    "Dense 설정", "연결된 뉴런으로 특징을 결합합니다.", true,
                    new BlockSettingsDefinition.NumberField(LayerNumberSetting.Units,
                        "뉴런 수", LayerLimits.DenseMaxUnits, layer => layer.Units)),

                [LayerType.Flatten] = new BlockSettingsDefinition(
                    "Flatten 설정", "특징 맵을 일차원 벡터로 펼칩니다.\n변경할 설정이 없습니다.", false),

                [LayerType.Input] = new BlockSettingsDefinition(
                    "Input 설정", "모델 입력입니다.\n이 블록의 설정은 고정되어 있습니다.", false),

                [LayerType.Output] = new BlockSettingsDefinition(
                    "Output 설정", $"{LayerLimits.OutputClassCount}개 클래스 / Softmax.\n이 블록의 설정은 고정되어 있습니다.", false)
            };

        internal static BlockSettingsDefinition Get(LayerType type)
        {
            return Definitions[type];
        }
    }
}
