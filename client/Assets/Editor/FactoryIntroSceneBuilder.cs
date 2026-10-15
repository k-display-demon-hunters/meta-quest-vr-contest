using System;
using System.IO;
using FactoryIntro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FactoryScenes.Editor
{
    public static class FactoryIntroSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/FactoryIntro.unity";
        private const string MaterialFolder = "Assets/Materials/FactoryIntro";

        [MenuItem("Tools/Factory/Create Intro Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before creating the intro scene.");
            }
            if (File.Exists(ScenePath))
            {
                Debug.Log("[FactoryIntro] Existing scene preserved: " + ScenePath);
                return;
            }
            GameObject rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/XR/XRPlayerRig.prefab");
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansKR/NotoSansCJKkr-Regular.otf");
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (rigAsset == null || font == null || shader == null)
            {
                throw new InvalidOperationException("The existing XR rig, Korean font and URP Unlit shader are required.");
            }
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
            Material dark = CreateMaterial("Factory structure", new Color(0.055f, 0.085f, 0.12f), shader);
            Material floor = CreateMaterial("Floor", new Color(0.12f, 0.16f, 0.2f), shader);
            Material cyan = CreateMaterial("Running machinery", new Color(0.08f, 0.75f, 0.85f), shader);
            Material amber = CreateMaterial("Practice tool", new Color(1f, 0.62f, 0.14f), shader);
            Material violet = CreateMaterial("Spirit", new Color(0.52f, 0.13f, 0.84f), shader);
            Material damage = CreateMaterial("Damaged machinery", new Color(0.45f, 0.23f, 0.2f), shader);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, scene);
                player.name = "XRPlayerRig";
                GameObject root = new GameObject("Factory Intro");
                FactoryIntroManager manager = root.AddComponent<FactoryIntroManager>();
                FactoryIntroPresentationController presentation = root.AddComponent<FactoryIntroPresentationController>();
                FactoryTutorialController tutorial = root.AddComponent<FactoryTutorialController>();
                AudioSource audio = root.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 0f;

                Transform environment = Empty(root.transform, "Factory environment", Vector3.zero);
                Cube(environment, "Walkable floor", new Vector3(0, -0.1f, 3), new Vector3(12, 0.2f, 14), floor);
                Cube(environment, "Back wall", new Vector3(0, 2f, 8), new Vector3(12, 4f, 0.25f), dark);
                for (int side = -1; side <= 1; side += 2)
                {
                    Cube(environment, "Side wall " + side, new Vector3(side * 5.9f, 2, 3), new Vector3(0.2f, 4, 10), dark);
                    for (int machine = 0; machine < 3; machine++)
                    {
                        Cube(environment, "Machine housing " + side + " " + machine, new Vector3(side * 4.5f, 0.7f, machine * 2.5f + 1), new Vector3(1.1f, 1.4f, 1.5f), dark);
                    }
                }
                Transform normal = Empty(environment, "Normal scenery", Vector3.zero);
                Transform damaged = Empty(environment, "Damaged pixel scenery", Vector3.zero);
                for (int i = 0; i < 8; i++)
                {
                    float x = -4.9f + i * 1.4f;
                    Cube(normal, "Window strip " + i, new Vector3(x, 2.6f, 7.8f), new Vector3(0.95f, 1.6f, 0.05f), cyan);
                    for (int row = 0; row < 4; row++)
                    {
                        Cube(damaged, "Broken pixel " + i + " " + row, new Vector3(x + (row % 2) * 0.1f, 1.95f + row * 0.42f, 7.7f), new Vector3(0.85f, 0.37f, 0.15f), (i + row) % 3 == 0 ? dark : damage);
                    }
                }
                Cube(normal, "Working core", new Vector3(0, 1.4f, 6.9f), new Vector3(1.25f, 2.1f, 0.5f), cyan);
                Transform brokenCore = Cube(damaged, "Broken core", new Vector3(0, 1.1f, 6.9f), new Vector3(1.25f, 1.8f, 0.5f), damage).transform;
                brokenCore.localRotation = Quaternion.Euler(0, 0, 13);
                damaged.gameObject.SetActive(false);

                GameObject spirit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spirit.name = "Temporary spirit silhouette";
                spirit.transform.SetParent(environment, false);
                spirit.transform.localScale = new Vector3(0.65f, 1f, 0.65f);
                spirit.GetComponent<Renderer>().sharedMaterial = violet;
                UnityEngine.Object.DestroyImmediate(spirit.GetComponent<Collider>());
                spirit.SetActive(false);
                Transform spiritStart = Empty(environment, "Spirit arrival", new Vector3(-3.5f, 3, 5.2f));
                Transform attackPoint = Empty(environment, "Core impact", new Vector3(0, 1.8f, 6.7f));
                Light machineLight = Empty(environment, "Core light", new Vector3(0, 2.8f, 5.5f)).gameObject.AddComponent<Light>();
                machineLight.type = LightType.Point;
                machineLight.range = 8;
                machineLight.color = Color.cyan;
                machineLight.intensity = 2;

                Transform exterior = Empty(root.transform, "Title floor start", Vector3.zero);
                Transform interior = Empty(root.transform, "Interior floor start", new Vector3(0, 0, 3));
                Transform panelAnchor = Empty(root.transform, "Interior guide position", new Vector3(0, 1.9f, 5.7f));
                Transform movementTarget = Empty(root.transform, "Movement destination", new Vector3(-0.8f, 0, 3.7f));
                GameObject movementMarker = Cube(root.transform, "Walk onto the cyan marker", new Vector3(-0.8f, 0.015f, 3.7f), new Vector3(0.8f, 0.025f, 0.8f), cyan);
                Transform toolHome = Empty(root.transform, "Practice tool home", new Vector3(-0.8f, 1.1f, 4.4f));
                Transform placement = Empty(root.transform, "Placement destination", new Vector3(0.8f, 1.1f, 4.4f));
                GameObject placementMarker = Cube(root.transform, "Place the tool above this pad", new Vector3(0.8f, 0.85f, 4.4f), new Vector3(0.65f, 0.06f, 0.65f), cyan);
                GameObject tool = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tool.name = "Grip practice tool";
                tool.transform.SetParent(root.transform, false);
                tool.transform.position = toolHome.position;
                tool.transform.localScale = Vector3.one * 0.3f;
                tool.GetComponent<Renderer>().sharedMaterial = amber;
                Rigidbody body = tool.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.isKinematic = true;
                XRGrabInteractable grab = tool.AddComponent<XRGrabInteractable>();
                grab.movementType = XRBaseInteractable.MovementType.Kinematic;
                grab.throwOnDetach = false;
                grab.useDynamicAttach = true;

                Canvas canvas = CreateCanvas(root.transform, player.GetComponentInChildren<Camera>());
                Panel(canvas.transform, "Guide background", Vector2.zero, new Vector2(1000, 620), new Color(0.035f, 0.065f, 0.1f, 0.98f));
                Text heading = Label(canvas.transform, font, "Heading", "뭉개진 세상을 구해라!", new Vector2(0, 225), new Vector2(930, 100), 42, Color.white);
                Text instruction = Label(canvas.transform, font, "Instruction", "오른손 레이로 Game Start를 가리키고 Trigger를 누르세요.", new Vector2(0, 50), new Vector2(900, 235), 31, new Color(0.82f, 0.9f, 0.94f));
                Button continueButton = Button(canvas.transform, font, "Continue", "Game Start", new Vector2(0, -150), new Vector2(630, 80));
                Button resetButton = Button(canvas.transform, font, "Reset tool", "연습 물체 되돌리기", new Vector2(0, -245), new Vector2(630, 65));
                resetButton.gameObject.SetActive(false);
                UnityEventTools.AddPersistentListener(continueButton.onClick, manager.Continue);
                UnityEventTools.AddPersistentListener(resetButton.onClick, manager.ResetPracticeObject);

                SerializedObject flow = new SerializedObject(manager);
                Assign(flow, "_presentation", presentation);
                Assign(flow, "_tutorial", tutorial);
                Assign(flow, "_exteriorStart", exterior);
                Assign(flow, "_interiorStart", interior);
                flow.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject view = new SerializedObject(presentation);
                Assign(view, "_canvas", canvas);
                Assign(view, "_heading", heading);
                Assign(view, "_instruction", instruction);
                Assign(view, "_continueButton", continueButton);
                Assign(view, "_continueLabel", continueButton.GetComponentInChildren<Text>());
                Assign(view, "_resetObjectButton", resetButton);
                Assign(view, "_interiorPanel", panelAnchor);
                Assign(view, "_normalScenery", normal.gameObject);
                Assign(view, "_damagedScenery", damaged.gameObject);
                Assign(view, "_spirit", spirit);
                Assign(view, "_spiritStart", spiritStart);
                Assign(view, "_attackPoint", attackPoint);
                Assign(view, "_machineLight", machineLight);
                Assign(view, "_movementMarker", movementMarker);
                Assign(view, "_placementMarker", placementMarker);
                Assign(view, "_practiceObject", grab);
                Assign(view, "_practiceObjectHome", toolHome);
                Assign(view, "_audio", audio);
                view.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject practice = new SerializedObject(tutorial);
                Assign(practice, "_movementTarget", movementTarget);
                Assign(practice, "_returnPoint", interior);
                Assign(practice, "_grabTarget", grab);
                Assign(practice, "_placementTarget", placement);
                practice.ApplyModifiedPropertiesWithoutUndo();
                movementMarker.SetActive(false);
                placementMarker.SetActive(false);
                tool.SetActive(false);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    throw new IOException("Could not save " + ScenePath);
                }
                Debug.Log("[FactoryIntro] Created " + ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                }
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Material CreateMaterial(string name, Color color, Shader shader)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }
            material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Transform Empty(Transform parent, string name, Vector3 position)
        {
            Transform result = new GameObject(name).transform;
            result.SetParent(parent, false);
            result.localPosition = position;
            return result;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            return cube;
        }

        private static Canvas CreateCanvas(Transform parent, Camera camera)
        {
            GameObject root = new GameObject("Factory guide", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0, 1.65f, 2.3f);
            root.transform.localScale = Vector3.one * 0.0025f;
            ((RectTransform)root.transform).sizeDelta = new Vector2(1000, 620);
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

        private static void Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            Image image = Rect(parent, name, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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
            Label(rect, font, "Label", caption, Vector2.zero, size - new Vector2(20, 8), 30, Color.white);
            return button;
        }

        private static void Assign(SerializedObject target, string field, UnityEngine.Object value)
        {
            target.FindProperty(field).objectReferenceValue = value;
        }
    }
}
