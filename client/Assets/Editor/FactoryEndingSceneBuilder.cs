using System;
using System.IO;
using FactoryEnding;
using FactoryIntro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FactoryScenes.Editor
{
    public static class FactoryEndingSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/FactoryEnding.unity";
        private const string RigPath = "Assets/Prefabs/XR/XRPlayerRig.prefab";
        private const string VillagePath = "Assets/Prefabs/FactoryIntro/VillageOpening.prefab";
        private const string FontPath = "Assets/Fonts/NotoSansCJKkr-Regular.otf";

        [MenuItem("Tools/Factory/Create Ending Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play mode before creating the ending scene.");
            }
            if (File.Exists(ScenePath))
            {
                throw new InvalidOperationException("The ending scene already exists and will not be overwritten: " + ScenePath);
            }

            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            GameObject villagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VillagePath);
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Material glow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FactoryIntro/VillageCyan.mat");
            Material sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FactoryIntro/VillageSky.mat");
            if (rigPrefab == null || villagePrefab == null || font == null || glow == null || sky == null)
            {
                throw new InvalidOperationException("FactoryEnding requires the existing XR rig, opening village, Korean font and village materials.");
            }

            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
                rig.transform.position = Vector3.zero;
                GameObject ending = new GameObject("Factory Ending");
                FactoryEndingManager manager = ending.AddComponent<FactoryEndingManager>();
                FactoryRestorationController restoration = ending.AddComponent<FactoryRestorationController>();
                GameObject village = (GameObject)PrefabUtility.InstantiatePrefab(villagePrefab, ending.transform);
                Light sunlight = new GameObject("Village sunlight", typeof(Light)).GetComponent<Light>();
                sunlight.transform.SetParent(ending.transform, false);
                sunlight.transform.localPosition = new Vector3(0f, 6f, 0f);
                sunlight.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
                sunlight.type = LightType.Directional;
                sunlight.color = new Color(0.53f, 0.48f, 0.8f);
                sunlight.intensity = 0.32f;
                sunlight.shadows = LightShadows.None;
                RenderSettings.skybox = sky;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.22f, 0.17f, 0.31f);
                RenderSettings.sun = sunlight;

                SerializedObject restorationFields = new SerializedObject(restoration);
                Assign(restorationFields, "_village", village.GetComponent<FactoryVillageController>());
                Assign(restorationFields, "_effectMaterial", glow);
                Assign(restorationFields, "_skyMaterial", sky);
                Assign(restorationFields, "_sun", sunlight);
                restorationFields.ApplyModifiedPropertiesWithoutUndo();

                FactoryEndingPanelController panel = BuildPanel(ending.transform, font, rig.GetComponentInChildren<Camera>());
                Transform arrival = new GameObject("Ending floor start").transform;
                arrival.SetParent(ending.transform, false);
                SerializedObject managerFields = new SerializedObject(manager);
                Assign(managerFields, "_restoration", restoration);
                Assign(managerFields, "_panel", panel);
                Assign(managerFields, "_entryPoint", arrival);
                managerFields.ApplyModifiedPropertiesWithoutUndo();

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    throw new IOException("Could not save the ending scene: " + ScenePath);
                }
                Debug.Log("[FactoryEnding] Created the 23-second village restoration ending using the shared opening village prefab.");
            }
            finally
            {
                if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousScene);
                }
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static FactoryEndingPanelController BuildPanel(Transform parent, Font font, Camera camera)
        {
            GameObject panelRoot = new GameObject("Ending Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            panelRoot.transform.SetParent(parent, false);
            panelRoot.transform.localPosition = new Vector3(0f, 1.15f, 3.4f);
            panelRoot.transform.localScale = Vector3.one * 0.0024f;
            ((RectTransform)panelRoot.transform).sizeDelta = new Vector2(1100f, 420f);
            Canvas canvas = panelRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.enabled = false;
            panelRoot.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;

            Box(panelRoot.transform, "Title backdrop", Vector2.zero, new Vector2(1100f, 420f),
                new Color(0.04f, 0.09f, 0.14f, 0.96f));
            Text title = Label(panelRoot.transform, font, "Ending title", "축하합니다. 공장에 빛이 돌아왔어요.",
                new Vector2(0f, 100f), new Vector2(1020f, 100f), 42);
            Text message = Label(panelRoot.transform, font, "Ending feedback", string.Empty,
                new Vector2(0f, -5f), new Vector2(1000f, 70f), 25);
            Button restart = CreateButton(panelRoot.transform, font, "처음으로", new Vector2(-190f, -125f), UI.SceneUIStyle.Accent);
            Button exit = CreateButton(panelRoot.transform, font, "앱 종료", new Vector2(190f, -125f), new Color(0.29f, 0.33f, 0.4f));
            restart.gameObject.SetActive(false);
            exit.gameObject.SetActive(false);
            FactoryEndingPanelController panel = panelRoot.AddComponent<FactoryEndingPanelController>();
            SerializedObject fields = new SerializedObject(panel);
            Assign(fields, "_canvas", canvas);
            Assign(fields, "_title", title);
            Assign(fields, "_message", message);
            Assign(fields, "_restartButton", restart);
            Assign(fields, "_exitButton", exit);
            fields.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = Rect(parent, name, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static Text Label(Transform parent, Font font, string name, string value,
            Vector2 position, Vector2 size, int fontSize)
        {
            Text text = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.text = value;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, Font font, string label, Vector2 position, Color color)
        {
            RectTransform rect = Box(parent, label, position, new Vector2(340f, 90f), color);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Label(rect, font, "Button label", label, Vector2.zero, new Vector2(320f, 80f), 36);
            UI.SceneUIStyle.Apply(button);
            return button;
        }

        private static void Assign(SerializedObject fields, string name, UnityEngine.Object value)
        {
            fields.FindProperty(name).objectReferenceValue = value;
        }
    }
}
