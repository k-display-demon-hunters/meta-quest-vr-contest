using System;
using System.IO;
using OpeningEnding;
using OpeningEnding.Intro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FactoryScenes.Editor
{
    public static class FactoryIntroSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/FactoryIntro.unity";

        [MenuItem("Tools/Factory/Create Intro Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before creating a scene.");
            if (File.Exists(ScenePath)) return;
            GameObject rigAsset = RequiredPrefab("Assets/Prefabs/XR/XRPlayerRig.prefab");
            GameObject villageAsset = RequiredPrefab("Assets/Prefabs/OpeningEnding/VillageOpening.prefab");
            GameObject entranceAsset = RequiredPrefab("Assets/Prefabs/OpeningEnding/FactoryEntrance.prefab");
            GameObject wraithAsset = RequiredPrefab("Assets/Prefabs/OpeningEnding/PixelWraith.prefab");
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
            if (font == null) throw new InvalidOperationException("The shared Korean font is required.");
            Material effectMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/OpeningEnding/WraithAttackRay.mat");
            Material skyMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/OpeningEnding/VillageSky.mat");
            AudioClip soundtrack = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/OpeningEnding/VillageOpening.wav");
            if (villageAsset.GetComponent<FactoryVillageController>() == null ||
                wraithAsset.GetComponent<FactoryWraithController>() == null ||
                effectMaterial == null || skyMaterial == null || soundtrack == null)
            {
                throw new InvalidOperationException("The opening needs its village, wraith, effect and sky materials, and soundtrack.");
            }
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, scene);
                GameObject root = new GameObject("Factory Intro");
                FactoryIntroManager manager = root.AddComponent<FactoryIntroManager>();
                FactoryIntroPresentationController presentation = root.AddComponent<FactoryIntroPresentationController>();
                FactoryIntroCinematicController cinematic = root.AddComponent<FactoryIntroCinematicController>();
                FactoryIntroInputController input = root.AddComponent<FactoryIntroInputController>();
                GameObject entrance = (GameObject)PrefabUtility.InstantiatePrefab(entranceAsset, root.transform);
                GameObject village = (GameObject)PrefabUtility.InstantiatePrefab(villageAsset, root.transform);
                GameObject wraith = (GameObject)PrefabUtility.InstantiatePrefab(wraithAsset, root.transform);
                wraith.transform.localPosition = new Vector3(1.45f, 2.2f, 7.2f);
                wraith.transform.localScale = Vector3.one * 1.3f;
                wraith.SetActive(false);
                Transform arrival = new GameObject("Title floor start").transform;
                arrival.SetParent(root.transform, false);
                Light fill = new GameObject("Factory story fill", typeof(Light)).GetComponent<Light>();
                fill.transform.SetParent(root.transform, false);
                fill.type = LightType.Directional;
                fill.intensity = 1.25f;
                fill.color = new Color(1f, 0.95f, 0.87f);
                fill.transform.localRotation = Quaternion.Euler(35f, -25f, 0f);
                RenderSettings.skybox = skyMaterial;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.76f, 0.82f, 0.84f);
                Canvas canvas = CreateCanvas(root.transform, player.GetComponentInChildren<Camera>());
                Image background = Panel(canvas.transform, "Guide background", Vector2.zero, new Vector2(1080f, 560f),
                    new Color(0.025f, 0.065f, 0.09f, 0.9f));
                Text heading = Label(canvas.transform, font, "Heading", "CNN 공장에 오신 것을 환영합니다", new Vector2(0f, 195f), new Vector2(1000f, 80f), 44, Color.white);
                Text instruction = Label(canvas.transform, font, "Instruction", "오른손 A 버튼을 눌러 시작하세요.", new Vector2(0f, 35f), new Vector2(1000f, 180f), 40, Color.white);
                Button next = Button(canvas.transform, font, "Continue", "다음", new Vector2(0f, -135f), new Vector2(600f, 100f));
                Text inputHint = Label(canvas.transform, font, "A button hint", "A 버튼으로 계속",
                    new Vector2(0f, -225f), new Vector2(800f, 56f), 28, Color.white);
                SerializedObject flow = new SerializedObject(manager);
                Assign(flow, "_presentation", presentation);
                Assign(flow, "_cinematic", cinematic);
                Assign(flow, "_exteriorStart", arrival);
                Assign(flow, "_input", input);
                Assign(flow, "_entry", entrance.GetComponent<FactoryIntroEntryController>());
                flow.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject view = new SerializedObject(presentation);
                Assign(view, "_canvas", canvas);
                Assign(view, "_background", background);
                Assign(view, "_inputHint", inputHint);
                Assign(view, "_heading", heading);
                Assign(view, "_instruction", instruction);
                Assign(view, "_continueLabel", next.GetComponentInChildren<Text>());
                Assign(view, "_continueButton", next);
                Assign(view, "_titleFont", AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Jua/Jua-Regular.ttf"));
                Assign(view, "_titleButtonSprite", AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));
                view.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject opening = new SerializedObject(cinematic);
                Assign(opening, "_village", village.GetComponent<FactoryVillageController>());
                Assign(opening, "_wraith", wraith.GetComponent<FactoryWraithController>());
                Assign(opening, "_effectMaterial", effectMaterial);
                Assign(opening, "_skyMaterial", skyMaterial);
                Assign(opening, "_sun", fill);
                Assign(opening, "_soundtrack", soundtrack);
                Assign(opening, "_titleMusic", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/OpeningEnding/Cipher.mp3"));
                Assign(opening, "_attackSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/OpeningEnding/WraithAttackWarning.wav"));
                Assign(opening, "_doorSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/OpeningEnding/FactoryDoorOpen.wav"));
                opening.ApplyModifiedPropertiesWithoutUndo();
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    throw new IOException("Could not save the intro scene: " + ScenePath);
                }
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject RequiredPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing story prefab: " + path);
            return prefab;
        }

        private static Canvas CreateCanvas(Transform parent, Camera camera)
        {
            GameObject root = new GameObject("Factory guide", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, 1.05f, 3.4f);
            root.transform.localScale = Vector3.one * 0.0022f;
            ((RectTransform)root.transform).sizeDelta = new Vector2(1080, 560);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            return canvas;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            Image image = Rect(parent, name, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(Transform parent, Font font, string name, string text, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            Text label = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }

        private static Button Button(Transform parent, Font font, string name, string caption, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(parent, name, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.03f, 0.42f, 0.5f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label(rect, font, "Label", caption, Vector2.zero, size - new Vector2(32, 12), 46, Color.white);
            UI.SceneUIStyle.Apply(button);
            return button;
        }

        private static void Assign(SerializedObject target, string field, UnityEngine.Object value)
        {
            target.FindProperty(field).objectReferenceValue = value;
        }
    }
}
