using System;
using System.IO;
using OpeningEnding.Ending;
using OpeningEnding;
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
        private const string VillagePath = "Assets/Prefabs/OpeningEnding/VillageOpening.prefab";
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
            Material glow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/OpeningEnding/VillageCyan.mat");
            Material sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/OpeningEnding/VillageSky.mat");
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
                Debug.Log("[FactoryEnding] Created the CNN upscaling presentation and village restoration ending.");
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
            panelRoot.transform.localPosition = new Vector3(0f, .94f, 3.4f);
            panelRoot.transform.localScale = Vector3.one * 0.0024f;
            ((RectTransform)panelRoot.transform).sizeDelta = new Vector2(1240f, 360f);
            Canvas canvas = panelRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.enabled = false;
            panelRoot.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;

            RectTransform backdrop = Box(panelRoot.transform, "Title backdrop", Vector2.zero, new Vector2(1240f, 360f),
                new Color(0.025f, 0.065f, 0.09f, 0.94f));
            Text title = Label(panelRoot.transform, font, "Ending title", "CNN 공장, 다시 가동!",
                new Vector2(0f, 105f), new Vector2(1140f, 100f), 60);
            Text message = Label(panelRoot.transform, font, "Ending feedback", string.Empty,
                new Vector2(0f, 20f), new Vector2(1140f, 65f), 38);
            Button start = CreateButton(panelRoot.transform, font, "AI 업스케일링 시작", new Vector2(0f, -100f), new Vector2(760f, 124f), 70);
            Button dialogue = CreateButton(panelRoot.transform, font, "다음", new Vector2(0f, -135f), new Vector2(500f, 104f), 60);
            Button restart = CreateButton(panelRoot.transform, font, "처음으로", new Vector2(-210f, -70f), new Vector2(380f, 124f), 70);
            Button exit = CreateButton(panelRoot.transform, font, "종료", new Vector2(210f, -70f), new Vector2(380f, 124f), 70);
            start.gameObject.SetActive(false);
            dialogue.gameObject.SetActive(false);
            restart.gameObject.SetActive(false);
            exit.gameObject.SetActive(false);
            FactoryEndingPanelController panel = panelRoot.AddComponent<FactoryEndingPanelController>();
            SerializedObject fields = new SerializedObject(panel);
            Assign(fields, "_canvas", canvas);
            Assign(fields, "_backdrop", backdrop);
            Assign(fields, "_title", title);
            Assign(fields, "_message", message);
            Assign(fields, "_startButton", start);
            Assign(fields, "_dialogueButton", dialogue);
            Assign(fields, "_dialogueButtonLabel", dialogue.GetComponentInChildren<Text>(true));
            Assign(fields, "_restartButton", restart);
            Assign(fields, "_exitButton", exit);
            Assign(fields, "_animation", EndingUpscalingVisualsBuilder.Build(parent));
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
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
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

        private static Button CreateButton(Transform parent, Font font, string label, Vector2 position, Vector2 size, int fontSize)
        {
            RectTransform rect = Box(parent, label, position, size, UI.SceneUIStyle.Button);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Label(rect, font, "Button label", label, Vector2.zero, size - new Vector2(48f, 12f), fontSize);
            UI.SceneUIStyle.Apply(button);
            return button;
        }

        private static void Assign(SerializedObject fields, string name, UnityEngine.Object value)
        {
            fields.FindProperty(name).objectReferenceValue = value;
        }
    }
}
