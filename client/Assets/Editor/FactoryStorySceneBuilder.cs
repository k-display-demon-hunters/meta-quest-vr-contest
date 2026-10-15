using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FactoryScenes.Editor
{
    public static class FactoryStorySceneBuilder
    {
        [MenuItem("Tools/Factory/Create Missing Intro and Ending Scenes")]
        public static void CreateMissingScenes()
        {
            if (!File.Exists(FactoryIntroSceneBuilder.ScenePath))
            {
                FactoryIntroSceneBuilder.Create();
            }
            if (!File.Exists(FactoryEndingSceneBuilder.ScenePath))
            {
                FactoryEndingSceneBuilder.Create();
            }

            // Preserve the existing start scene, ordering and enabled flags.
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            AppendScene(scenes, FactoryIntroSceneBuilder.ScenePath);
            AppendScene(scenes, FactoryEndingSceneBuilder.ScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Factory] Intro and ending are available. Existing scenes were preserved.");
        }

        private static void AppendScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            if (!scenes.Exists(scene => scene.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }
    }
}
