using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace UI.Editor
{
    public static class SpatialPixelLabBuilder
    {
        public const string ScenePath = "Assets/Scenes/CNNPixelLab.unity";
        private const string MaterialFolder = "Assets/Materials/CNNPixelLab";
        private static Font _font;
        private static Material _dark;
        private static Material _cyan;
        private static Material _amber;
        private static Material _white;

        [MenuItem("Tools/CNN/Create Spatial Pixel Lab")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) { throw new InvalidOperationException("Stop Play Mode first."); }
            if (File.Exists(ScenePath)) { Debug.Log("[CNN] Spatial lab already exists: " + ScenePath); return; }
            _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansKR/NotoSansCJKkr-Regular.otf");
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
            _dark = Material("Structure", new Color32(36, 41, 47, 255), true);
            _cyan = Material("System light", new Color32(86, 94, 101, 255), true);
            _amber = Material("Filter frame", new Color32(173, 142, 100, 255), true);
            _white = Material("Image surface", Color.white, true);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CNNXRPlayer.prefab"), scene);
            GameObject rig = player.transform.Find("Quest XR Origin").gameObject;
            Camera camera = rig.GetComponentInChildren<Camera>();
            camera.fieldOfView = 62;
            camera.backgroundColor = new Color32(7, 14, 24, 255);
            XRInteractionManager manager = player.GetComponentInChildren<XRInteractionManager>();
            foreach (XRRayInteractor ray in rig.GetComponentsInChildren<XRRayInteractor>())
            {
                ray.interactionManager = manager;
                string hand = ray.name.StartsWith("Left") ? "LeftHand" : "RightHand";
                XRInputButtonReader select = new XRInputButtonReader("Drag filter", inputSourceMode: XRInputButtonReader.InputSourceMode.InputAction);
                select.inputActionPerformed.AddBinding("<XRController>{" + hand + "}/triggerPressed");
                select.inputActionValue.AddBinding("<XRController>{" + hand + "}/trigger");
                ray.selectInput = select;
                GameObject directObject = new GameObject(hand + " Grab", typeof(SphereCollider), typeof(Rigidbody));
                directObject.SetActive(false);
                directObject.transform.SetParent(ray.transform, false);
                SphereCollider collider = directObject.GetComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.13f;
                Rigidbody body = directObject.GetComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                XRDirectInteractor direct = directObject.AddComponent<XRDirectInteractor>();
                direct.interactionManager = manager;
                XRInputButtonReader grip = new XRInputButtonReader("Grab filter", inputSourceMode: XRInputButtonReader.InputSourceMode.InputAction);
                grip.inputActionPerformed.AddBinding("<XRController>{" + hand + "}/gripPressed");
                grip.inputActionValue.AddBinding("<XRController>{" + hand + "}/grip");
                direct.selectInput = grip;
                Cube(directObject.transform, "Controller tip", Vector3.zero, Vector3.one * 0.035f, _cyan);
                directObject.SetActive(true);
            }

            GameObject lab = new GameObject("CNN Pixel Workshop");
            SpatialPixelLab controller = lab.AddComponent<SpatialPixelLab>();
            Transform board = new GameObject("Floating image board").transform;
            board.SetParent(lab.transform, false);
            board.localPosition = new Vector3(0, 1.65f, 1.8f);
            MeshRenderer image = Quad(board, "Low resolution image", Vector3.zero, new Vector2(2.4f, 1.35f), _white);
            Transform selection = new GameObject("Selected source pixels").transform;
            selection.SetParent(board, false);
            Frame(selection, "Selection", 0.225f, 0.225f, 0.006f, _amber);
            selection.gameObject.SetActive(false);
            GameObject filter = new GameObject("Grab the filter", typeof(Rigidbody), typeof(BoxCollider));
            filter.transform.SetParent(lab.transform, false);
            filter.transform.localPosition = new Vector3(1.55f, 1.55f, 1.5f);
            Rigidbody filterBody = filter.GetComponent<Rigidbody>();
            filterBody.isKinematic = true;
            filterBody.useGravity = false;
            BoxCollider grabCollider = filter.GetComponent<BoxCollider>();
            grabCollider.size = new Vector3(0.54f, 0.54f, 0.12f);
            Frame(filter.transform, "Filter rim", 0.48f, 0.48f, 0.024f, _amber);
            for (int i = -1; i <= 1; i += 2)
            {
                Cube(filter.transform, "Kernel vertical " + i, new Vector3(i * 0.08f, 0, -0.025f), new Vector3(0.006f, 0.46f, 0.012f), _amber);
                Cube(filter.transform, "Kernel horizontal " + i, new Vector3(0, i * 0.08f, -0.025f), new Vector3(0.46f, 0.006f, 0.012f), _amber);
            }
            MeshRenderer lens = Quad(filter.transform, "Magnified pixels", new Vector3(0, 0, 0.005f), new Vector2(0.46f, 0.46f), _white);
            lens.gameObject.SetActive(false);
            XRGrabInteractable grab = filter.AddComponent<XRGrabInteractable>();
            grab.interactionManager = manager;
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;
            grab.attachEaseInTime = 0.08f;
            Canvas controls = Canvas(lab.transform, "Spatial controls", new Vector3(0, 0.74f, 1.7f), new Vector2(1200, 155), 0.002f, camera);
            Button details = Button(controls.transform, "더 자세히 보기", -360, 25, 320, 70);
            UnityEventTools.AddPersistentListener(details.onClick, controller.ShowDetails);
            UnityEventTools.AddPersistentListener(Button(controls.transform, "처음부터", 0, 25, 270, 70).onClick, controller.ResetLab);
            Canvas detail = Canvas(lab.transform, "Floating pixel values", new Vector3(0, 1.7f, 1.25f), new Vector2(1000, 620), 0.0022f, camera);
            Box(detail.transform, "Panel", 0, 0, 1000, 620, new Color32(13, 27, 43, 255));
            Image[] cells = new Image[9];
            Text[] values = new Text[9];
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    int index = row * 3 + column;
                    RectTransform cell = Box(detail.transform, "Pixel " + index, -138 + column * 138, 148 - row * 138, 132, 132, Color.black);
                    cells[index] = cell.GetComponent<Image>();
                    values[index] = Label(cell, "Value", "", 0, 0, 128, 128, 32, Color.white);
                }
            }
            UnityEventTools.AddPersistentListener(Button(detail.transform, "이미지로 돌아가기", 0, -245, 410, 76).onClick, controller.CloseDetails);
            detail.gameObject.SetActive(false);

            SerializedObject fields = new SerializedObject(controller);
            Assign(fields, "_sourceImage", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/CNN/ColorPhoto.png"));
            Assign(fields, "_board", board);
            Assign(fields, "_boardRenderer", image);
            Assign(fields, "_filter", grab);
            Assign(fields, "_lensRenderer", lens);
            Assign(fields, "_selectionFrame", selection);
            Assign(fields, "_camera", camera);
            Assign(fields, "_detailButton", details);
            Assign(fields, "_detail", detail.gameObject);
            fields.FindProperty("_cells").arraySize = fields.FindProperty("_values").arraySize = 9;
            for (int i = 0; i < 9; i++)
            {
                fields.FindProperty("_cells").GetArrayElementAtIndex(i).objectReferenceValue = cells[i];
                fields.FindProperty("_values").GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            fields.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            PixelRevealStageBuilder.Install();
            if (!EditorBuildSettings.scenes.Any(item => item.path == ScenePath))
            {
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            }

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = lab;
        }

        private static Material Material(string name, Color color, bool unlit)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            return cube;
        }

        private static MeshRenderer Quad(Transform parent, string name, Vector3 position, Vector2 size, Material material)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = position;
            quad.transform.localScale = new Vector3(size.x, size.y, 1);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static void Frame(Transform parent, string name, float width, float height, float thickness, Material material)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Cube(parent, name + " vertical " + side, new Vector3(side * width / 2, 0, -0.02f), new Vector3(thickness, height + thickness, thickness), material);
                Cube(parent, name + " horizontal " + side, new Vector3(0, side * height / 2, -0.02f), new Vector3(width, thickness, thickness), material);
            }
        }

        private static Canvas Canvas(Transform parent, string name, Vector3 position, Vector2 size, float scale, Camera camera)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localScale = Vector3.one * scale;
            ((RectTransform)root.transform).sizeDelta = size;
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            return canvas;
        }

        private static RectTransform Box(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = Rect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Text Label(Transform parent, string name, string value, float x, float y, float width, float height, int size, Color color)
        {
            Text text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(Transform parent, string label, float x, float y, float width, float height)
        {
            RectTransform rect = Box(parent, label, x, y, width, height, new Color32(151, 158, 165, 255));
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Label(rect, "Label", label, 0, 0, width - 10, height, 28, new Color32(13, 27, 43, 255));
            return button;
        }

        private static void Assign(SerializedObject fields, string name, UnityEngine.Object value)
        {
            fields.FindProperty(name).objectReferenceValue = value;
        }
    }
}
