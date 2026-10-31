using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace UI.Editor
{
    public static class ConvolutionLabBuilder
    {
        private const string ScenePath = "Assets/Scenes/CNNConvolutionLab.unity";

        [MenuItem("Tools/CNN/Create Convolution Lab")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) { throw new InvalidOperationException("Stop Play Mode first."); }
            if (File.Exists(ScenePath)) { Debug.Log("[CNN] Convolution lab already exists: " + ScenePath); return; }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            GameObject root = new GameObject("CNN Convolution Lab");
            CNNConvolutionLabManager manager = root.AddComponent<CNNConvolutionLabManager>();
            GameObject content = new GameObject("Floating pixel values", typeof(RectTransform),
                typeof(Canvas), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            RectTransform rect = (RectTransform)content.transform;
            rect.SetParent(root.transform, false);
            rect.localPosition = new Vector3(0, 1.7f, 1.25f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(1000, 620);
            content.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            content.SetActive(true);
            GameObject uiObject = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Common/PixelCalculationUI.prefab"), content.transform);
            LessonUIController ui = uiObject.GetComponent<LessonUIController>();
            GameObject blocks = new GameObject("B quiz blocks");
            blocks.transform.SetParent(content.transform, false);
            RgbVolumeController spatial = blocks.AddComponent<RgbVolumeController>();
            SerializedObject fields = new SerializedObject(manager);
            fields.FindProperty("_lessonRoot").objectReferenceValue = content;
            fields.FindProperty("_ui").objectReferenceValue = ui;
            fields.FindProperty("_spatial").objectReferenceValue = spatial;
            fields.FindProperty("_font").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
            fields.ApplyModifiedPropertiesWithoutUndo();
            manager.enabled = true;
            root.SetActive(true);
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!EditorBuildSettings.scenes.Any(item => item.path == ScenePath))
            {
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(
                    new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            }
        }
    }
}
