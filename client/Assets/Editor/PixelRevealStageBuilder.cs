using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Editor
{
    public static class PixelRevealStageBuilder
    {
        [MenuItem("Tools/CNN/Install Pixel Reveal Stage")]
        public static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != SpatialPixelLabBuilder.ScenePath)
            {
                throw new InvalidOperationException("Open CNNPixelLab in Edit Mode first.");
            }
            CNNPixelLabManager lab = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                lab = root.GetComponentInChildren<CNNPixelLabManager>();
                if (lab != null) { break; }
            }
            if (lab == null) { throw new InvalidOperationException("CNNPixelLabManager is missing."); }
            PixelRevealStage stage = lab.GetComponent<PixelRevealStage>();
            if (stage == null) { stage = lab.gameObject.AddComponent<PixelRevealStage>(); }
            Transform controls = lab.transform.Find("Spatial controls");
            Transform nextTransform = controls.Find("다음으로");
            if (nextTransform == null)
            {
                GameObject nextObject = UnityEngine.Object.Instantiate(controls.Find("처음부터").gameObject, controls);
                nextObject.name = "다음으로";
                nextTransform = nextObject.transform;
            }
            Button next = nextTransform.GetComponent<Button>();
            next.GetComponentInChildren<Text>().text = "다음으로";
            next.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(next.onClick, lab.NextStage);
            Transform menu = controls.Find("메뉴로");
            if (menu != null) { UnityEngine.Object.DestroyImmediate(menu.gameObject); }
            string[] names = { "더 자세히 보기", "다음으로", "처음부터" };
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform rect = (RectTransform)controls.Find(names[i]);
                rect.anchoredPosition = new Vector2(-320 + i * 320, 25);
                rect.sizeDelta = new Vector2(270, 70);
                rect.GetComponentInChildren<Text>().rectTransform.sizeDelta = new Vector2(260, 70);
            }
            SerializedObject stageFields = new SerializedObject(stage);
            stageFields.FindProperty("_board").objectReferenceValue = lab.transform.Find("Floating image board");
            stageFields.FindProperty("_controls").objectReferenceValue = controls;
            stageFields.FindProperty("_camera").objectReferenceValue = null;
            stageFields.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
            stageFields.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject labFields = new SerializedObject(lab);
            labFields.FindProperty("_revealStage").objectReferenceValue = stage;
            labFields.FindProperty("_nextButton").objectReferenceValue = next;
            labFields.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
