using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CNNConvolution.Editor
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
            root.transform.localPosition = new Vector3(0, -0.375f, 0);
            root.transform.localScale = Vector3.one * 1.5f;
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
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CNNConvolutionLab/PixelCalculationUI.prefab"), content.transform);
            LessonUIController ui = uiObject.GetComponent<LessonUIController>();
            GameObject blocks = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CNNConvolutionLab/ConvolutionWorkbench.prefab"), content.transform);
            WorkbenchBuilder spatial = blocks.GetComponent<WorkbenchBuilder>();
            GameObject inspectionObject = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CNNConvolutionLab/FeatureInspectionDesk.prefab"), content.transform);
            FeatureInspectionManager inspection = inspectionObject.GetComponent<FeatureInspectionManager>();
            ConvolutionLessonManager lesson = root.AddComponent<ConvolutionLessonManager>();
            SerializedObject managerFields = new SerializedObject(manager);
            managerFields.FindProperty("_lesson").objectReferenceValue = lesson;
            managerFields.FindProperty("_inspection").objectReferenceValue = inspection;
            managerFields.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject fields = new SerializedObject(lesson);
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
