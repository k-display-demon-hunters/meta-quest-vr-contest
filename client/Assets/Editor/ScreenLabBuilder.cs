using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CNNScreen.Editor
{
    public static class ScreenLabBuilder
    {
        public const string ScenePath = "Assets/Scenes/CNNScreenLab.unity";
        private const string MaterialFolder = "Assets/Materials/CNNScreenLab";
        private static Font _font;
        private static Material _amber;
        private static Material _white;

        [MenuItem("Tools/CNN/Create Screen Lab")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) { throw new InvalidOperationException("Stop Play Mode first."); }
            if (File.Exists(ScenePath)) { Debug.Log("[CNN] Spatial lab already exists: " + ScenePath); return; }
            _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
            _amber = Material("Filter frame", new Color32(173, 142, 100, 255), true);
            _white = Material("Image surface", Color.white, true);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/XR/XRPlayerRig.prefab"), scene);
            Camera camera = player.GetComponentInChildren<Camera>();

            GameObject lab = new GameObject("CNN Screen Lab");
            CNNScreenLabManager controller = lab.AddComponent<CNNScreenLabManager>();
            PixelImageFactory imageFactory = lab.AddComponent<PixelImageFactory>();
            GameObject selectionRoot = new GameObject("Pixel Selection");
            selectionRoot.transform.SetParent(lab.transform, false);
            PixelSelectionController selectionController = selectionRoot.AddComponent<PixelSelectionController>();
            Transform board = new GameObject("Floating image board").transform;
            board.SetParent(lab.transform, false);
            board.localPosition = new Vector3(0, 3.125f, 1.8f);
            board.localScale = Vector3.one * 3f;
            MeshRenderer image = Quad(board, "Low resolution image", Vector3.zero, new Vector2(2.4f, 1.35f), _white);
            PixelBoardController boardController = image.gameObject.AddComponent<PixelBoardController>();
            Transform selection = new GameObject("Selected source pixels").transform;
            selection.SetParent(board, false);
            Frame(selection, "Selection", 0.225f, 0.225f, 0.006f, _amber);
            selection.gameObject.SetActive(false);
            GameObject filter = new GameObject("Grab the filter", typeof(Rigidbody), typeof(BoxCollider));
            filter.transform.SetParent(selectionRoot.transform, false);
            filter.transform.localPosition = new Vector3(-1.2f, 0.13f, -0.6f);
            filter.transform.localRotation = Quaternion.Euler(90, 0, 0);
            filter.transform.localScale = Vector3.one * 1.3f;
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
            // Resolve the surviving rig's manager instead of a scene-local duplicate.
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;
            grab.attachEaseInTime = 0.08f;
            Canvas controls = Canvas(lab.transform, "Spatial controls", new Vector3(0, 0.74f, 1.7f), new Vector2(1200, 155), 0.002f, camera);
            ScreenLabUIController ui = controls.gameObject.AddComponent<ScreenLabUIController>();
            Button details = Button(controls.transform, "더 자세히 보기", -360, 25, 320, 70);
            UnityEventTools.AddPersistentListener(details.onClick, ui.RequestDetails);
            UnityEventTools.AddPersistentListener(Button(controls.transform, "처음부터", 0, 25, 270, 70).onClick, ui.RequestReset);
            SerializedObject fields = new SerializedObject(controller);
            Assign(fields, "_imageFactory", imageFactory);
            SerializedObject factoryFields = new SerializedObject(imageFactory);
            Assign(factoryFields, "_sourceImage", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/CNNScreenLab/OpeningBroadcast.png"));
            factoryFields.ApplyModifiedPropertiesWithoutUndo();
            Assign(fields, "_board", boardController);
            Assign(fields, "_selection", selectionController);
            SerializedObject boardFields = new SerializedObject(boardController);
            Assign(boardFields, "_boardRenderer", image);
            Assign(boardFields, "_presentationRoot", board);
            boardFields.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject selectionFields = new SerializedObject(selectionController);
            Assign(selectionFields, "_board", board);
            Assign(selectionFields, "_filter", grab);
            Assign(selectionFields, "_selectionFrame", selection);
            selectionFields.ApplyModifiedPropertiesWithoutUndo();
            fields.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject uiFields = new SerializedObject(ui);
            Assign(uiFields, "_detailButton", details);
            uiFields.ApplyModifiedPropertiesWithoutUndo();
            Assign(fields, "_ui", ui);
            fields.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            PixelRevealStageBuilder.Install();
            controller.enabled = true;
            lab.SetActive(true);
            EditorSceneManager.SaveScene(scene);
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
