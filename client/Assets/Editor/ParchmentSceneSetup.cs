using Parchment;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XR;

namespace Education.Editor
{
    public static class ParchmentSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/FactoryEnvironment.unity";
        private const string RigPath = "Assets/Prefabs/XR/XRPlayerRig.prefab";
        private const string FontPath = "Assets/Fonts/Pretendard/Pretendard-Regular.otf";
        private const string SpritePath = "Assets/Textures/Common/LearningParchment.png";
        private const string GroupName = "Learning Parchments";

        [MenuItem("Tools/Factory/Place Learning Parchments")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play mode before placing learning parchments.");
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            Sprite sprite = LoadParchmentSprite();
            if (font == null || rigPrefab == null || sprite == null)
            {
                throw new InvalidOperationException("Learning parchments require the Pretendard font, hologram sprite and existing XRPlayerRig.");
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform factory = FindRoot(scene, "Factory Environment");
            Transform convolutionRoom = factory != null ? factory.Find("03 Original image inspection room") : null;
            Transform mlpRoom = factory != null ? factory.Find("05 Model assembly room") : null;
            if (convolutionRoom == null || mlpRoom == null)
            {
                throw new InvalidOperationException("FactoryEnvironment's existing Convolution and MLP room containers were not found.");
            }

            Transform convolutionPapers = PrepareGroup(convolutionRoom);
            Transform mlpPapers = PrepareGroup(mlpRoom);
            Vector3[] positions = {
                new Vector3(-1.1f, 1.45f, -1f), new Vector3(-3.8f, 1.45f, 6.5f),
                new Vector3(-1.1f, 1.45f, 12f), new Vector3(-3.8f, 1.45f, 19.7f),
                new Vector3(1.5f, 1.45f, 16.7f) };
            Vector3[] directions = { Vector3.forward, Vector3.left, Vector3.forward, Vector3.left, Vector3.right };
            for (int i = 0; i < positions.Length; i++)
            {
                CreateParchment(i == 0 ? convolutionPapers : mlpPapers,
                    i == 0 ? ParchmentRoom.PixelLab : ParchmentRoom.ModelDesignLab, i, positions[i],
                    positions[i] - directions[i], sprite, font);
            }

            EnsureRig(scene, rigPrefab);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Could not save the learning parchments in FactoryEnvironment.");
            }
            Debug.Log("[Education] Placed five learning parchments along the approach routes.");
        }

        private static Sprite LoadParchmentSprite()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null)
            {
                return null;
            }
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single || !importer.alphaIsTransparency)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static Transform FindRoot(Scene scene, string rootName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                {
                    return root.transform;
                }
            }
            return null;
        }

        private static Transform PrepareGroup(Transform room)
        {
            Transform group = room.Find(GroupName);
            if (group == null)
            {
                group = new GameObject(GroupName).transform;
                group.SetParent(room, false);
            }
            foreach (ParchmentPickupController previous in group.GetComponentsInChildren<ParchmentPickupController>(true))
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }
            return group;
        }

        private static void CreateParchment(Transform parent, ParchmentRoom placementRoom, int index, Vector3 position,
            Vector3 facingPoint, Sprite sprite, Font font)
        {
            ParchmentLesson lesson = ParchmentCatalog.Get(index);
            GameObject parchment = new GameObject("Parchment " + (index + 1) + " - " + lesson.Title);
            parchment.transform.SetParent(parent, false);
            parchment.transform.position = position;
            // World-space Canvas text is read from its local -Z side.
            parchment.transform.rotation = Quaternion.LookRotation(position - facingPoint, Vector3.up);

            SpriteRenderer paper = CreateSprite(parchment.transform, "Parchment", sprite,
                new Vector2(0.84f, 0.47f), Vector3.zero, Color.white, 2);
            SpriteRenderer glow = CreateSprite(parchment.transform, "Soft cyan outline", sprite,
                new Vector2(0.9f, 0.5f), new Vector3(0f, 0f, 0.015f), new Color(0.34f, 0.84f, 1f, 0.14f), 1);

            GameObject lightObject = new GameObject("Subtle cyan light");
            lightObject.transform.SetParent(parchment.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            Light glowLight = lightObject.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(0.34f, 0.84f, 1f);
            glowLight.intensity = 0.5f;
            glowLight.range = 1.5f;
            glowLight.shadows = LightShadows.None;
            glowLight.renderMode = LightRenderMode.Auto;

            BoxCollider collider = parchment.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.88f, 0.5f, 0.08f);
            XRSimpleInteractable interactable = parchment.AddComponent<XRSimpleInteractable>();
            interactable.colliders.Add(collider);
            ParchmentPickupVisualController visuals = parchment.AddComponent<ParchmentPickupVisualController>();
            SerializedObject visualFields = new SerializedObject(visuals);
            visualFields.FindProperty("_paperRenderer").objectReferenceValue = paper;
            visualFields.FindProperty("_glowRenderer").objectReferenceValue = glow;
            visualFields.FindProperty("_glowLight").objectReferenceValue = glowLight;
            visualFields.FindProperty("_floatPhase").floatValue = index * 0.7f;
            visualFields.ApplyModifiedPropertiesWithoutUndo();
            ParchmentPickupController pickup = parchment.AddComponent<ParchmentPickupController>();
            SerializedObject fields = new SerializedObject(pickup);
            fields.FindProperty("_lessonIndex").intValue = index;
            fields.FindProperty("_placementRoom").intValue = (int)placementRoom;
            fields.FindProperty("_readerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Common/ParchmentReader.prefab")
                    .GetComponent<ParchmentReaderController>();
            fields.FindProperty("_visuals").objectReferenceValue = visuals;
            fields.ApplyModifiedPropertiesWithoutUndo();
            CreateLabel(parchment.transform, index, lesson, font);
        }

        private static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite,
            Vector2 size, Vector3 position, Color color, int order)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = position;
            visual.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void CreateLabel(Transform parent, int index, ParchmentLesson lesson, Font font)
        {
            GameObject labelObject = new GameObject("Document label", typeof(RectTransform), typeof(Canvas));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            labelObject.transform.localScale = Vector3.one * 0.001f;
            Canvas canvas = labelObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 3;
            labelObject.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 650f);

            AddText(labelObject.transform, "Document number", "양피지 " + (index + 1).ToString("00"),
                font, new Vector2(0f, 78f), new Vector2(420f, 55f), 25);
            AddText(labelObject.transform, "Lesson title", lesson.Title,
                font, new Vector2(0f, -3f), new Vector2(500f, 100f), 42);
            AddText(labelObject.transform, "Collect instruction", "Trigger / Grip으로 열기",
                font, new Vector2(0f, -100f), new Vector2(470f, 50f), 19);
        }

        private static void AddText(Transform parent, string name, string content, Font font,
            Vector2 position, Vector2 size, int fontSize)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }

        private static void EnsureRig(Scene scene, GameObject rigPrefab)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<PersistentXRPlayerRig>(true) != null)
                {
                    return;
                }
            }
            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
            rig.transform.position = new Vector3(0f, 0f, -20f);
            rig.transform.rotation = Quaternion.identity;
        }
    }
}
