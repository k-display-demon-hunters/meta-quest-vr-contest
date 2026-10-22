using System;
using System.Collections.Generic;
using System.IO;
using FactoryEnding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FactoryScenes.Editor
{
    public static class FactoryEndingSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/FactoryEnding.unity";

        private const string MaterialFolder = "Assets/Materials/FactoryEnding";
        private const string RigPath = "Assets/Prefabs/XR/XRPlayerRig.prefab";
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
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (rigPrefab == null || font == null || shader == null)
            {
                throw new InvalidOperationException("FactoryEnding requires the existing XRPlayerRig, Korean font and URP Lit shader.");
            }

            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = default;
            try
            {
                EnsureMaterialsFolder();
                Material structure = MaterialAsset("FactoryStructure", shader, new Color(0.14f, 0.62f, 0.65f));
                Material floor = MaterialAsset("FactoryFloor", shader, new Color(0.11f, 0.16f, 0.2f));
                Material metal = MaterialAsset("MachineMetal", shader, new Color(0.35f, 0.45f, 0.52f));
                Material landscape = MaterialAsset("RestoredLandscape", shader, new Color(0.22f, 0.56f, 0.3f));
                Material light = MaterialAsset("RestoredLights", shader, new Color(0.84f, 0.72f, 0.35f));
                Material debris = MaterialAsset("DamagedFragments", shader, new Color(0.24f, 0.16f, 0.23f));

                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
                rig.transform.position = Vector3.zero;

                GameObject ending = new GameObject("Factory Ending");
                FactoryEndingManager manager = ending.AddComponent<FactoryEndingManager>();
                FactoryRestorationController restoration = ending.AddComponent<FactoryRestorationController>();
                BuildFactory(ending.transform, restoration, structure, floor, metal, landscape, light, debris);
                FactoryEndingPanelController panel = BuildPanel(ending.transform, font, rig.GetComponentInChildren<Camera>());
                Transform arrival = Child(ending.transform, "Ending floor start");

                SerializedObject managerFields = new SerializedObject(manager);
                Assign(managerFields, "_restoration", restoration);
                Assign(managerFields, "_panel", panel);
                Assign(managerFields, "_entryPoint", arrival);
                managerFields.ApplyModifiedPropertiesWithoutUndo();

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    throw new IOException("Could not save the ending scene: " + ScenePath);
                }
                Debug.Log("[FactoryEnding] Created editable ending scene. Existing shared factory assets are connected; existing scenes and build settings were not changed.");
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

        private static void BuildFactory(Transform parent, FactoryRestorationController controller,
            Material structure, Material floor, Material metal, Material landscape, Material lights, Material debrisMaterial)
        {
            GameObject hallAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Common/KdyFactoryHall.prefab");
            GameObject wraithAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/kdy_scene2/PixelWraith.prefab");
            if (hallAsset == null || wraithAsset == null) throw new InvalidOperationException("The shared factory and wraith prefabs are required.");
            Transform set = Child(parent, "Restoring factory set");
            GameObject hall = (GameObject)PrefabUtility.InstantiatePrefab(hallAsset, set);
            GameObject spirit = (GameObject)PrefabUtility.InstantiatePrefab(wraithAsset, set);
            spirit.transform.localPosition = new Vector3(0f, 2.7f, 9f);
            spirit.GetComponent<FactoryIntro.FactoryWraithController>().enabled = false;
            List<Renderer> lamps = new List<Renderer>();
            List<Renderer> windows = new List<Renderer>();
            for (int index = 0; index < 8; index++)
            {
                lamps.Add(Cube(set, "Restored light " + (index + 1),
                    new Vector3(index % 2 == 0 ? -4.8f : 4.8f, 2.7f, 5.5f + index / 2 * 3f),
                    new Vector3(0.17f, 0.44f, 0.12f), lights));
            }
            for (int index = 0; index < 3; index++)
            {
                windows.Add(Cube(set, "Restored outside view " + (index + 1),
                    new Vector3((index - 1) * 3.7f, 3.1f, 14f), new Vector3(3.2f, 2.1f, 0.05f), landscape));
            }
            Light daylight = new GameObject("Factory daylight", typeof(Light)).GetComponent<Light>();
            daylight.transform.SetParent(set, false);
            daylight.type = LightType.Directional;
            daylight.intensity = 1f;
            daylight.transform.localRotation = Quaternion.Euler(45f, -30f, 0f);
            SerializedObject fields = new SerializedObject(controller);
            Assign(fields, "_factoryRoot", hall.transform);
            Assign(fields, "_spiritRoot", spirit.transform);
            AssignArray(fields, "_restoredLights", lamps);
            AssignArray(fields, "_windowSurfaces", windows);
            fields.ApplyModifiedPropertiesWithoutUndo();
        }

        private static FactoryEndingPanelController BuildPanel(Transform parent, Font font, Camera camera)
        {
            GameObject panelRoot = new GameObject("Ending Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            panelRoot.transform.SetParent(parent, false);
            panelRoot.transform.localPosition = new Vector3(0f, 1.9f, 3.8f);
            panelRoot.transform.localScale = Vector3.one * 0.0026f;
            ((RectTransform)panelRoot.transform).sizeDelta = new Vector2(1720f, 1180f);
            Canvas canvas = panelRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            panelRoot.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;
            FactoryEndingPanelController panel = panelRoot.AddComponent<FactoryEndingPanelController>();

            Color ink = UI.SceneUIStyle.Muted;
            Color cyan = UI.SceneUIStyle.Accent;
            Color parchment = UI.SceneUIStyle.Surface;
            Box(panelRoot.transform, "Title backdrop", new Vector2(0f, 450f), new Vector2(1660f, 245f), new Color(0.04f, 0.09f, 0.14f, 0.96f));
            Text title = Label(panelRoot.transform, font, "Ending title", "축하합니다! 공장에 빛이 돌아왔어요",
                new Vector2(0f, 510f), new Vector2(1570f, 85f), 50, Color.white);
            Text description = Label(panelRoot.transform, font, "Ending description",
                "CNN은 작은 영역의 특징을 찾고, 여러 층을 거쳐 결과를 만듭니다.\n여섯 장의 개념 두루마리로 체험을 돌아보세요.",
                new Vector2(0f, 405f), new Vector2(1570f, 125f), 30, new Color(0.79f, 0.9f, 0.94f));

            RectTransform cards = Rect(panelRoot.transform, "Six concept parchments", Vector2.zero, new Vector2(1720f, 700f));
            string[] headings = { "01  픽셀", "02  합성곱과 패딩", "03  특징 탐지기, 필터", "04  특징 맵", "05  활성화 함수", "06  학습" };
            string[] summaries =
            {
                "흑백 이미지의 픽셀은 밝기를 나타냅니다.\n0은 검정, 255는 흰색입니다. 이 숫자를 행과 열로 놓으면 행렬이 됩니다.",
                "입력과 필터의 같은 위치 값을 곱해 더합니다.\n패딩은 입력 가장자리에 값을 덧대어 경계에서도 계산할 수 있게 합니다.",
                "필터는 특정 모양에 반응하는 특징 탐지기입니다.\n가중치가 다르면 선이나 경계 등 서로 다른 특징에 반응합니다.",
                "필터를 움직여 계산한 결과를 위치별로 모으면 특징 맵이 됩니다.\n값의 크기를 통해 어느 위치에서 강하게 반응했는지 봅니다.",
                "ReLU는 음수를 0으로 바꿉니다.\nSigmoid는 값을 0과 1 사이로 바꿉니다. 이런 비선형 변환으로 표현을 풍부하게 합니다.",
                "손실은 예측과 목표의 차이를 나타냅니다. 역전파로 구한 기울기로 가중치를 조정합니다.\n사용할 때는 학습된 가중치로 계산합니다."
            };
            for (int index = 0; index < headings.Length; index++)
            {
                Vector2 position = new Vector2((index % 3 - 1) * 550f, index < 3 ? 160f : -180f);
                RectTransform card = Box(cards, "Parchment " + (index + 1), position, new Vector2(520f, 310f), parchment);
                Box(card, "Upper rolled edge", new Vector2(0f, 145f), new Vector2(538f, 20f), UI.SceneUIStyle.Button);
                Box(card, "Lower rolled edge", new Vector2(0f, -145f), new Vector2(538f, 20f), UI.SceneUIStyle.Button);
                Label(card, font, "Concept title", headings[index], new Vector2(0f, 94f), new Vector2(474f, 65f), 32, cyan);
                Label(card, font, "Concept summary", summaries[index], new Vector2(0f, -25f), new Vector2(474f, 172f), 27, ink);
            }

            Text message = Label(panelRoot.transform, font, "Ending feedback",
                "여섯 개 개념을 읽고 체험을 마무리해 보세요.",
                new Vector2(0f, -407f), new Vector2(1600f, 70f), 25, Color.white);
            Button complete = Button(panelRoot.transform, font, "체험 마무리", new Vector2(0f, -510f), cyan);
            Button restart = Button(panelRoot.transform, font, "처음으로", new Vector2(-245f, -510f), cyan);
            Button exit = Button(panelRoot.transform, font, "앱 종료", new Vector2(245f, -510f), new Color(0.29f, 0.33f, 0.4f));
            restart.gameObject.SetActive(false);
            exit.gameObject.SetActive(false);

            SerializedObject fields = new SerializedObject(panel);
            Assign(fields, "_canvas", canvas);
            Assign(fields, "_title", title);
            Assign(fields, "_description", description);
            Assign(fields, "_message", message);
            Assign(fields, "_recapCards", cards.gameObject);
            Assign(fields, "_completeButton", complete);
            Assign(fields, "_restartButton", restart);
            Assign(fields, "_exitButton", exit);
            fields.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        private static void EnsureMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "FactoryEnding");
            }
        }

        private static Material MaterialAsset(string name, Shader shader, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }
            Material material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Renderer Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            Renderer renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
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
            Vector2 position, Vector2 size, int fontSize, Color color)
        {
            Text text = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(Transform parent, Font font, string label, Vector2 position, Color color)
        {
            RectTransform rect = Box(parent, label, position, new Vector2(450f, 100f), color);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Label(rect, font, "Button label", label, Vector2.zero, new Vector2(420f, 90f), 36, Color.white);
            UI.SceneUIStyle.Apply(button);
            return button;
        }

        private static void Assign(SerializedObject fields, string name, UnityEngine.Object value)
        {
            fields.FindProperty(name).objectReferenceValue = value;
        }

        private static void AssignArray<T>(SerializedObject fields, string name, List<T> values) where T : UnityEngine.Object
        {
            SerializedProperty array = fields.FindProperty(name);
            array.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}
