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
                Debug.Log("[FactoryEnding] Created editable ending scene. Factory geometry is a placeholder; existing scenes and build settings were not changed.");
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
            Transform factory = Child(parent, "Editable Factory Placeholder");
            List<Renderer> factorySurfaces = new List<Renderer>();
            List<Renderer> landscapeSurfaces = new List<Renderer>();
            List<Transform> rotors = new List<Transform>();
            List<Light> factoryLights = new List<Light>();

            Cube(factory, "Factory ground", new Vector3(0f, -0.15f, 4f), new Vector3(18f, 0.3f, 20f), floor);
            Cube(factory, "Rear wall", new Vector3(0f, 2.5f, 10f), new Vector3(15f, 5f, 0.3f), floor);
            for (int side = -1; side <= 1; side += 2)
            {
                factorySurfaces.Add(Cube(factory, "Factory pillar " + side, new Vector3(side * 6f, 2.3f, 6f),
                    new Vector3(0.5f, 4.6f, 0.5f), structure));
                factorySurfaces.Add(Cube(factory, "Restored beam " + side, new Vector3(side * 3.1f, 4.4f, 6f),
                    new Vector3(6.1f, 0.3f, 0.5f), structure));
                for (int row = 0; row < 2; row++)
                {
                    Transform machine = Child(factory, "Machine " + side + " " + row);
                    machine.localPosition = new Vector3(side * 3.8f, 0f, 4f + row * 3f);
                    factorySurfaces.Add(Cube(machine, "Machine housing", new Vector3(0f, 1.1f, 0f),
                        new Vector3(1.6f, 2.2f, 1.2f), structure));
                    Cube(machine, "Machine base", new Vector3(0f, 0.15f, 0f), new Vector3(2f, 0.3f, 1.5f), metal);
                    Transform rotor = Child(machine, "Restoration rotor");
                    rotor.localPosition = new Vector3(0f, 1.3f, -0.68f);
                    Cube(rotor, "Rotor horizontal", Vector3.zero, new Vector3(1.1f, 0.13f, 0.1f), metal);
                    Cube(rotor, "Rotor vertical", Vector3.zero, new Vector3(0.13f, 1.1f, 0.1f), metal);
                    rotors.Add(rotor);
                }

                landscapeSurfaces.Add(Cube(factory, "Garden strip " + side, new Vector3(side * 7f, 0.02f, 4f),
                    new Vector3(1.8f, 0.16f, 14f), landscape));
                for (int treeIndex = 0; treeIndex < 3; treeIndex++)
                {
                    Vector3 treePosition = new Vector3(side * 7f, 0f, treeIndex * 3.5f);
                    Cube(factory, "Garden trunk", treePosition + Vector3.up * 0.6f, new Vector3(0.2f, 1.2f, 0.2f), metal);
                    landscapeSurfaces.Add(Cube(factory, "Restored tree", treePosition + Vector3.up * 1.6f,
                        new Vector3(1.1f, 1.4f, 1.1f), landscape));
                }

                Light lamp = new GameObject("Restoring lamp " + side, typeof(Light)).GetComponent<Light>();
                lamp.transform.SetParent(factory, false);
                lamp.transform.localPosition = new Vector3(side * 3f, 3.2f, 4f);
                lamp.type = LightType.Point;
                lamp.range = 7f;
                lamp.intensity = 1.4f;
                lamp.color = new Color(0.65f, 0.95f, 1f);
                lamp.shadows = LightShadows.None;
                factoryLights.Add(lamp);
            }

            Transform damaged = Child(factory, "Damaged fragments - removed during restoration");
            for (int index = 0; index < 7; index++)
            {
                Renderer fragment = Cube(damaged, "Fragment " + index,
                    new Vector3(-2.1f + index * 0.7f, 0.35f, 5.5f + (index % 2) * 0.5f),
                    new Vector3(0.35f, 0.7f, 0.45f), debrisMaterial);
                fragment.transform.localRotation = Quaternion.Euler(15f * index, 23f * index, 12f * index);
            }
            Transform restored = Child(factory, "Restored factory details");
            for (int index = 0; index < 9; index++)
            {
                Cube(restored, "Restored window " + index, new Vector3(-5.2f + index * 1.3f, 3f, 9.8f),
                    new Vector3(0.8f, 1.4f, 0.05f), lights);
            }

            Light daylight = new GameObject("Factory daylight", typeof(Light)).GetComponent<Light>();
            daylight.transform.SetParent(factory, false);
            daylight.type = LightType.Directional;
            daylight.intensity = 1f;
            daylight.color = new Color(0.9f, 0.95f, 1f);
            daylight.shadows = LightShadows.None;
            daylight.transform.localRotation = Quaternion.Euler(45f, -30f, 0f);

            SerializedObject fields = new SerializedObject(controller);
            AssignArray(fields, "_factorySurfaces", factorySurfaces);
            AssignArray(fields, "_landscapeSurfaces", landscapeSurfaces);
            AssignArray(fields, "_rotors", rotors);
            AssignArray(fields, "_factoryLights", factoryLights);
            Assign(fields, "_debris", damaged.gameObject);
            Assign(fields, "_restoredDetails", restored.gameObject);
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

            Color ink = new Color(0.14f, 0.2f, 0.25f);
            Color cyan = new Color(0.08f, 0.45f, 0.51f);
            Color parchment = new Color(0.95f, 0.89f, 0.72f);
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
                Box(card, "Upper rolled edge", new Vector2(0f, 145f), new Vector2(538f, 20f), new Color(0.68f, 0.49f, 0.27f));
                Box(card, "Lower rolled edge", new Vector2(0f, -145f), new Vector2(538f, 20f), new Color(0.68f, 0.49f, 0.27f));
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
