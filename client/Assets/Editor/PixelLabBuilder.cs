using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Editor
{
    public static class PixelLabBuilder
    {
        private const string ScenePath = "Assets/Scenes/CNNPixelLab.unity";

        [MenuItem("Tools/CNN/Create Pixel Lab Host")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) { throw new InvalidOperationException("Stop Play Mode first."); }
            if (File.Exists(ScenePath)) { Debug.Log("[CNN] Pixel lab host already exists: " + ScenePath); return; }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            GameObject root = new GameObject("CNN Pixel Lab");
            root.AddComponent<PixelLabSceneLoadController>();
            root.AddComponent<CNNPixelLabManager>();
            root.SetActive(false);
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!EditorBuildSettings.scenes.Any(item => item.path == ScenePath))
            {
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(
                    new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            }
        }
    }
}
