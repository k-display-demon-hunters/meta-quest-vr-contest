using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UI.Editor
{
    // Creates saved assets and scene bindings; none of this runs in the player.
    public static class ScreenLabVisualsBuilder
    {
        private const string Prefabs = "Assets/Prefabs/CNNScreenLab/";
        private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");

        public static void Install(Scene scene, CNNScreenLabManager lab)
        {
            EnsureAssets();
            PixelSelectionController selection = lab.GetComponentInChildren<PixelSelectionController>(true);
            SerializedObject selectionFields = new SerializedObject(selection);
            Transform board = ((Transform)selectionFields.FindProperty("_board").objectReferenceValue);
            XRGrabInteractable filter = (XRGrabInteractable)selectionFields.FindProperty("_filter").objectReferenceValue;
            SelectedPixelsController volume = Instance<SelectedPixelsController>("FilterDisplay", filter.transform);
            foreach (MeshRenderer renderer in filter.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.transform.IsChildOf(volume.transform.parent)) { renderer.enabled = false; }
            }
            filter.trackPosition = true;
            filter.trackRotation = false;
            filter.movementType = XRBaseInteractable.MovementType.Instantaneous;
            BoxCollider collider = filter.GetComponent<BoxCollider>();
            collider.size = new Vector3(0.54f, 0.54f, 0.18f);
            collider.center = new Vector3(0, 0, -0.02f);
            RgbPixelStackController rgb = Instance<RgbPixelStackController>("RgbPixelStack", board);
            PixelRevealStage stage = Instance<PixelRevealStage>("ImagePixelGrid", board);
            Bind(stage, "_rgbStack", rgb);
            Bind(selection, "_volume", volume);
            Bind(lab, "_revealStage", stage);
            PixelLabUIController ui = lab.GetComponentInChildren<PixelLabUIController>(true);
            Bind(stage, "_rgbValues", rgb.GetComponent<RgbValuesUIController>());
            Transform controls = lab.transform.Find("Spatial controls");
            Transform guide = controls.Find("Interaction hint");
            Text instruction = guide != null ? guide.GetComponent<Text>()
                : Label(controls, "Interaction hint", "", new Vector2(0, -105), new Vector2(1400, 90), 32);
            Bind(ui, "_instruction", instruction);
            Bind(ui, "_nextButton", controls.Find("다음으로").GetComponent<Button>());
            foreach (Button button in controls.GetComponentsInChildren<Button>(true))
            {
                bool next = button.name == "다음으로";
                bool reset = button.name == "처음부터" || button.name == "처음으로";
                if (!reset)
                {
                    SceneUIStyle.Apply(button);
                    if (button.GetComponent<Outline>() == null) { button.gameObject.AddComponent<Outline>(); }
                }
                RectTransform rect = (RectTransform)button.transform;
                rect.sizeDelta = new Vector2(next ? 380 : 320, 100);
                rect.anchoredPosition = new Vector2(reset ? 210 : -210, 20);
                Text caption = button.GetComponentInChildren<Text>();
                caption.fontSize = next ? 48 : 34;
                caption.resizeTextForBestFit = false;
                caption.rectTransform.anchorMin = Vector2.zero;
                caption.rectTransform.anchorMax = Vector2.one;
                caption.rectTransform.offsetMin = new Vector2(10, 5);
                caption.rectTransform.offsetMax = new Vector2(-10, -5);
                if (reset) { caption.text = "처음으로"; }
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        private static T Instance<T>(string name, Transform parent) where T : Component
        {
            T existing = parent.GetComponentInChildren<T>(true);
            if (existing != null) { return existing; }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return instance.GetComponentInChildren<T>(true);
        }

        private static void EnsureAssets()
        {
            Directory.CreateDirectory(Prefabs);
            Directory.CreateDirectory("Assets/Meshes/CNNScreenLab");
            Directory.CreateDirectory("Assets/Materials/CNNScreenLab");
            AssetDatabase.Refresh();
            string meshPath = "Assets/Meshes/CNNScreenLab/PixelBlock.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh = PixelBlockMeshBuilder.Create(); AssetDatabase.CreateAsset(mesh, meshPath); }
            Material pixels = Material("PixelBlock", true);
            Material frame = Material("FilterFrame", false);
            CreatePrefab("FilterDisplay", root =>
            {
                Transform group = Child(root.transform, "Selected pixels");
                MeshRenderer[] blocks = new MeshRenderer[9];
                for (int i = 0; i < 9; i++)
                {
                    blocks[i] = PixelBlockMeshBuilder.Add(group, "Pixel " + i, mesh, pixels,
                        new Vector3((i % 3 - 1) * 0.153f, (i / 3 - 1) * 0.153f, -0.03f), new Vector3(0.145f, 0.145f, 0.13f));
                }
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    PixelBlockMeshBuilder.Add(root.transform, "Vertical rim " + sign, mesh, frame,
                        new Vector3(sign * 0.247f, 0, 0), new Vector3(0.035f, 0.53f, 0.11f));
                    PixelBlockMeshBuilder.Add(root.transform, "Horizontal rim " + sign, mesh, frame,
                        new Vector3(0, sign * 0.247f, 0), new Vector3(0.46f, 0.035f, 0.11f));
                }
                SelectedPixelsController controller = group.gameObject.AddComponent<SelectedPixelsController>();
                BindArray(controller, "_pixels", blocks);
                group.gameObject.SetActive(false);
            });
            CreatePrefab("RgbPixelStack", root =>
            {
                Transform content = Child(root.transform, "RGB layers");
                Transform[] layers = new Transform[3];
                MeshRenderer[] blocks = new MeshRenderer[27];
                Text[] values = new Text[27];
                CanvasGroup[] labels = new CanvasGroup[3];
                string[] names = { "R · 빨강", "G · 초록", "B · 파랑" };
                for (int c = 0; c < 3; c++)
                {
                    layers[c] = Child(content, names[c]);
                    GameObject canvasObject = new GameObject("Pixel values", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
                    canvasObject.transform.SetParent(layers[c], false);
                    canvasObject.transform.localPosition = Vector3.back * 0.035f;
                    canvasObject.transform.localScale = Vector3.one * 0.001f;
                    canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                    labels[c] = canvasObject.GetComponent<CanvasGroup>();
                    labels[c].alpha = 0;
                    labels[c].blocksRaycasts = false;
                    labels[c].interactable = false;
                    Label(canvasObject.transform, "Channel", names[c], new Vector2(0, 345), new Vector2(220, 45), 30);
                    for (int i = 0; i < 9; i++)
                    {
                        Vector3 position = new Vector3((i % 3 - 1) * 0.19f, (1 - i / 3) * 0.19f, 0);
                        blocks[c * 9 + i] = PixelBlockMeshBuilder.Add(layers[c], "Pixel " + i, mesh, pixels, position, new Vector3(0.1786f, 0.1786f, 0.055f));
                        values[c * 9 + i] = Label(canvasObject.transform, "Value " + i, "", new Vector2(position.x, position.y) * 1000, new Vector2(175, 130), 48);
                    }
                }
                RgbPixelStackController controller = root.AddComponent<RgbPixelStackController>();
                Bind(controller, "_content", content.gameObject);
                BindArray(controller, "_layers", layers);
                BindArray(controller, "_blocks", blocks);
                RgbValuesUIController ui = root.AddComponent<RgbValuesUIController>();
                BindArray(ui, "_values", values);
                BindArray(ui, "_labels", labels);
                content.gameObject.SetActive(false);
            });
            CreatePrefab("ImagePixelGrid", root =>
            {
                Transform grid = Child(root.transform, "Image pixels");
                grid.localPosition = Vector3.back * 0.12f;
                Transform[] columns = new Transform[32];
                MeshRenderer[] blocks = new MeshRenderer[576];
                for (int x = 0; x < 32; x++)
                {
                    columns[x] = Child(grid, "Column " + x);
                    columns[x].localPosition = new Vector3((x + 0.5f - 16) * 0.075f, 0, 0);
                    for (int y = 0; y < 18; y++)
                    {
                        blocks[y * 32 + x] = PixelBlockMeshBuilder.Add(columns[x], "Pixel " + x + "," + y, mesh, pixels,
                            new Vector3(0, (y + 0.5f - 9) * 0.075f, 0.02175f), new Vector3(0.07109375f, 0.07109375f, 0.0375f));
                    }
                }
                PixelRevealStage stage = root.AddComponent<PixelRevealStage>();
                Bind(stage, "_pixelRoot", grid.gameObject);
                BindArray(stage, "_columns", columns);
                BindArray(stage, "_blocks", blocks);
                grid.gameObject.SetActive(false);
            });
            AssetDatabase.SaveAssets();
        }

        private static Material Material(string name, bool transparent)
        {
            string path = "Assets/Materials/CNNScreenLab/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) { return material; }
            material = PixelBlockMeshBuilder.CreateMaterial(transparent);
            material.name = name;
            if (!transparent) { material.SetColor("_BaseColor", new Color(0.28f, 0.32f, 0.37f)); }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreatePrefab(string name, Action<GameObject> populate)
        {
            string path = Prefabs + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) { return; }
            GameObject root = new GameObject(name);
            try { populate(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Text Label(Transform parent, string name, string caption, Vector2 position, Vector2 size, int fontSize)
        {
            GameObject label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(parent, false);
            Text text = label.GetComponent<Text>();
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = size;
            text.font = Font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = caption;
            text.raycastTarget = false;
            return text;
        }

        private static void Bind(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject fields = new SerializedObject(target);
            fields.FindProperty(field).objectReferenceValue = value;
            fields.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) { PrefabUtility.RecordPrefabInstancePropertyModifications(target); }
        }

        private static void BindArray<T>(UnityEngine.Object target, string field, T[] values) where T : UnityEngine.Object
        {
            SerializedObject fields = new SerializedObject(target);
            SerializedProperty array = fields.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) { array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
            fields.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
