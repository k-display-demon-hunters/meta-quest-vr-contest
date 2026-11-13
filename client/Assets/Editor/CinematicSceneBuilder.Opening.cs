using System;
using System.Linq;
using Cinematic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CinematicEditor
{
    public static partial class CinematicSceneBuilder
    {
        private const string PostalModels = "Assets/Meshes/CNNArchiveOpening/";
        private const string PostalMaterials = "Assets/Materials/CNNArchiveOpening/";

        [MenuItem("Tools/Factory/Revise Opening Post Office")]
        public static void ReviseOpening()
        {
            PreparePostalModels();
            ApplyPostalTextures();
            _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pretendard/Pretendard-SemiBold.otf");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CNNArchiveOpening.unity", OpenSceneMode.Single);
            OpeningVisualController view = UnityEngine.Object.FindFirstObjectByType<OpeningVisualController>();
            SerializedObject serialized = new SerializedObject(view);
            Transform stage = (Transform)serialized.FindProperty("_world").objectReferenceValue;
            Transform oldRoom = (Transform)serialized.FindProperty("_postOffice").objectReferenceValue;
            UnityEngine.Object.DestroyImmediate(oldRoom.gameObject);
            Assign(view, "_postOffice", BuildPostOffice(stage, view));
            stage.localPosition = Vector3.up * .85f;
            stage.Find("Opening Start Panel").localPosition = new Vector3(0f, .75f, 2.15f);
            stage.Find("Archive caption").localPosition = new Vector3(0f, 1.1f, 1.2f);

            Transform digit = stage.Find("Handwritten digit");
            digit.localPosition = new Vector3(0f, 1.25f, 2.4f);
            digit.Find("Handwritten numeral").localScale = Vector3.one * .0048f;
            digit.Find("Digit recognition").localScale = Vector3.one * .6f;
            // Derive the grid from its row/column order so repeating this command is idempotent.
            serialized.Update();
            SerializedProperty pixels = serialized.FindProperty("_pixels");
            int index = 0;
            for (int row = 0; row < 16; row++)
            for (int col = 0; col < 16; col++)
            {
                bool ink = (row >= 2 && row <= 4 && col >= 4 && col <= 11)
                    || (col >= 10 && col <= 12 && row >= 3 && row <= 7)
                    || (row >= 6 && row <= 11 && Mathf.Abs(col - (17 - row)) < 2)
                    || (row >= 11 && row <= 13 && col >= 3 && col <= 12);
                if (!ink) { continue; }
                Transform pixel = (Transform)pixels.GetArrayElementAtIndex(index++).objectReferenceValue;
                pixel.localPosition = digit.localPosition + new Vector3((col - 7.5f) / 7.5f * .48f, -(row - 7.5f) / 7.5f * .48f, 0f);
            }

            RectTransform captions = stage.GetComponentsInChildren<Canvas>(true)
                .Single(c => c.name == "Narration").GetComponent<RectTransform>();
            captions.localPosition = new Vector3(0f, .5f, 2.5f);
            captions.sizeDelta = new Vector2(1740f, 200f);
            captions.Find("Caption backing").GetComponent<RectTransform>().sizeDelta = new Vector2(1740f, 200f);
            captions.Find("Type line").GetComponent<RectTransform>().sizeDelta = new Vector2(1660f, 190f);
            ConfigureNarrationAudio(UnityEngine.Object.FindFirstObjectByType<CinematicAudioController>(), true);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Opening] Saved eye-level layout, textured post office, animated visitors and narration mix.");
        }

        private static void ConfigureNarrationAudio(CinematicAudioController audio, bool opening)
        {
            SerializedObject serialized = new SerializedObject(audio);
            AudioSource voice = (AudioSource)serialized.FindProperty("_voice").objectReferenceValue;
            voice.spatialBlend = 0f;
            voice.volume = 1f;
            voice.priority = 0;
            if (!opening) { return; }
            AudioSource crowd = (AudioSource)serialized.FindProperty("_screen").objectReferenceValue;
            crowd.spatialBlend = 0f;
            crowd.GetComponent<AudioLowPassFilter>().cutoffFrequency = 6500f;
            AudioSource score = (AudioSource)serialized.FindProperty("_pad").objectReferenceValue;
            score.spatialBlend = 0f;
        }

        private static void ApplyPostalTextures()
        {
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/OpeningEnding/ArchiveMaterials.png");
            string[] names = { "Oak", "Floor", "Plaster", "Paper" };
            Vector2[] offsets = { new Vector2(.0025f, .5025f), new Vector2(.5025f, .5025f), new Vector2(.0025f, .0025f), new Vector2(.5025f, .0025f) };
            Color[] colors = { new Color(.72f, .62f, .48f), new Color(.72f, .70f, .66f), new Color(.82f, .79f, .71f), new Color(.94f, .91f, .82f) };
            for (int i = 0; i < names.Length; i++)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(PostalMaterials + "PostOffice" + names[i] + ".mat");
                material.SetTexture("_BaseMap", atlas);
                material.SetTextureScale("_BaseMap", Vector2.one * .495f);
                material.SetTextureOffset("_BaseMap", offsets[i]);
                material.SetColor("_BaseColor", colors[i]);
                EditorUtility.SetDirty(material);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { PostalMaterials.TrimEnd('/') }))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.shader.name != "Universal Render Pipeline/Simple Lit") { continue; }
                // Simple Lit's enum uses 1 for NoSpecular (the Lit toggle uses the opposite value).
                material.SetFloat("_SpecularHighlights", 1f);
                material.DisableKeyword("_SPECULAR_COLOR");
                material.DisableKeyword("_SPECGLOSSMAP");
                // This stage intentionally has no realtime shadows; do not sample its unused shadow atlas.
                material.SetFloat("_ReceiveShadows", 0f);
                material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
                EditorUtility.SetDirty(material);
            }
        }

        private static void PreparePostalModels()
        {
            foreach (string name in new[] { "Suit", "Casual_2" })
            {
                string path = PostalModels + name + ".fbx";
                ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations
                    .Where(c => c.name.EndsWith("|Walk", StringComparison.Ordinal) || c.name.EndsWith("|Idle", StringComparison.Ordinal)).ToArray();
                foreach (ModelImporterClipAnimation clip in clips)
                {
                    clip.loopTime = true;
                    clip.wrapMode = WrapMode.Loop;
                    clip.lockRootPositionXZ = true;
                    clip.lockRootRotation = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        private static PostOfficeCrowdController BuildPostOfficeCrowd(Transform room)
        {
            Transform crowd = Root("Postal staff and visitors", room);
            PostOfficeCrowdController controller = crowd.gameObject.AddComponent<PostOfficeCrowdController>();
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty people = serialized.FindProperty("_people");
            Vector3[] starts = {
                new Vector3(-.65f, -.49f, 2.95f), new Vector3(1.75f, -.49f, 2.95f),
                new Vector3(-2.7f, -.49f, 1.1f), new Vector3(2.7f, -.49f, .9f),
                new Vector3(-3.5f, -.49f, -2.5f), new Vector3(3.55f, -.49f, -2.6f),
                new Vector3(-1.25f, -.49f, 4.2f) };
            Vector3[] ends = {
                starts[0], starts[1], starts[2], starts[3],
                new Vector3(-3.5f, -.49f, .65f), new Vector3(3.55f, -.49f, .65f),
                new Vector3(1.25f, -.49f, 4.2f) };
            people.arraySize = starts.Length;
            for (int i = 0; i < starts.Length; i++)
            {
                bool walking = i >= 4;
                string modelName = i % 2 == 0 ? "Suit" : "Casual_2";
                string path = PostalModels + modelName + ".fbx";
                Transform person = Root(i < 2 ? "Postal clerk " + i : "Postal visitor " + i, crowd);
                person.localPosition = starts[i];
                float facing = i < 2 ? 180f : 0f;
                person.localRotation = Quaternion.Euler(0f, facing, 0f);
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), person);
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                // Preserve the FBX unit conversion; animated culling bounds are not a body-height measure.
                model.transform.localScale = Vector3.one * (i % 2 == 0 ? .96f : .92f);
                model.transform.localPosition = Vector3.up * .035f;
                foreach (Renderer renderer in renderers)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(source => PostalCharacterMaterial(modelName, source)).ToArray();
                }
                Animation animation = model.GetComponent<Animation>();
                if (animation == null) { animation = model.AddComponent<Animation>(); }
                AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)
                        && c.name.EndsWith(walking ? "|Walk" : "|Idle", StringComparison.Ordinal));
                animation.AddClip(clip, clip.name);
                animation.clip = clip;
                animation.playAutomatically = false;
                animation.cullingType = AnimationCullingType.AlwaysAnimate;
                SerializedProperty entry = people.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Root").objectReferenceValue = person;
                entry.FindPropertyRelative("Animation").objectReferenceValue = animation;
                entry.FindPropertyRelative("Clip").stringValue = clip.name;
                entry.FindPropertyRelative("Start").vector3Value = starts[i];
                entry.FindPropertyRelative("End").vector3Value = ends[i];
                entry.FindPropertyRelative("TravelSeconds").floatValue = walking ? Vector3.Distance(starts[i], ends[i]) / .65f : 0f;
                entry.FindPropertyRelative("Phase").floatValue = i * .73f;
                entry.FindPropertyRelative("Facing").floatValue = facing;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static Material PostalCharacterMaterial(string model, Material source)
        {
            string path = PostalMaterials + model + "_" + source.name.Replace(" ", "_") + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) { return material; }
            material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            material.SetColor("_BaseColor", source.color);
            material.SetFloat("_SpecularHighlights", 1f);
            material.DisableKeyword("_SPECULAR_COLOR");
            material.SetFloat("_ReceiveShadows", 0f);
            material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            material.SetFloat("_Smoothness", 0f);
            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void BuildPostOfficeDetails(Transform room, Material oak, Material brass, Material paper, Material ink, Material blue)
        {
            Transform details = Root("Period fixtures", room);
            string[] labels = { "STAMPS & PARCELS", "LETTERS", "REGISTERED MAIL" };
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 2.7f;
                Box("Service plaque frame", details, new Vector3(x, 2.32f, 2.29f), new Vector3(1.8f, .39f, .065f), brass);
                Box("Service plaque enamel", details, new Vector3(x, 2.32f, 2.25f), new Vector3(1.72f, .31f, .018f), paper);
                Canvas sign = CanvasAt(details, "Service plaque", new Vector3(x, 2.32f, 2.23f), new Vector2(900, 140), .0018f);
                TextAt(sign.transform, "Service", labels[i], Vector2.zero, new Vector2(900, 140), 55).color = new Color(.12f, .15f, .13f);
                for (int j = 0; j < 9; j++)
                {
                    Box("Counter panel bead", details, new Vector3(x - 1f + j * .25f, .12f, 1.605f), new Vector3(.012f, .58f, .016f), brass);
                }
                Box("Blotting pad", details, new Vector3(x + .55f, .709f, 2.03f), new Vector3(.57f, .015f, .36f), blue);
                for (int j = 0; j < 5; j++)
                {
                    Transform letter = Box("Loose form", details, new Vector3(x + .54f + j * .008f, .723f + j * .006f, 2.04f), new Vector3(.28f, .004f, .21f), paper);
                    letter.localRotation = Quaternion.Euler(0f, j * 4f - 8f, 0f);
                }
            }
            // Shallow trim and service fittings carry the period cues without new texture files.
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Picture rail", details, new Vector3(side * 4.37f, 2.78f, 1f), new Vector3(.09f, .065f, 9.9f), oak);
                Box("Dado rail", details, new Vector3(side * 4.35f, .42f, 1f), new Vector3(.1f, .065f, 9.9f), brass);
                for (int i = 0; i < 3; i++)
                {
                    Box("Window crossbar", details, new Vector3(side * 4.22f, 1.65f, -2f + i * 3f), new Vector3(.08f, .045f, 1.62f), oak);
                    Box("Window sill", details, new Vector3(side * 4.16f, .82f, -2f + i * 3f), new Vector3(.36f, .07f, 1.9f), oak);
                }
            }
            Box("Clock case", details, new Vector3(0f, 2.65f, 5.7f), new Vector3(.68f, .68f, .12f), oak);
            Box("Clock face", details, new Vector3(0f, 2.65f, 5.62f), new Vector3(.59f, .59f, .025f), paper);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f;
                Transform tick = Box("Clock marker", details, new Vector3(Mathf.Sin(angle) * .24f, 2.65f + Mathf.Cos(angle) * .24f, 5.6f), new Vector3(.018f, .05f, .008f), ink);
                tick.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);
            }
            Transform minute = Box("Minute hand", details, new Vector3(.08f, 2.59f, 5.58f), new Vector3(.025f, .21f, .01f), ink);
            minute.localRotation = Quaternion.Euler(0f, 0f, 55f);
            Box("Hour hand", details, new Vector3(0f, 2.71f, 5.57f), new Vector3(.035f, .14f, .012f), ink);
            Box("CRT housing", details, new Vector3(3.55f, .98f, 2.04f), new Vector3(.43f, .48f, .42f), paper);
            Box("CRT glass", details, new Vector3(3.55f, 1.01f, 1.818f), new Vector3(.34f, .31f, .025f), ink);
            Box("Keyboard", details, new Vector3(3.5f, .73f, 1.67f), new Vector3(.43f, .045f, .16f), paper);
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 9; col++)
            {
                Box("Key", details, new Vector3(3.34f + col * .039f, .759f, 1.62f + row * .04f), new Vector3(.029f, .012f, .026f), ink);
            }
            Box("Telephone base", details, new Vector3(-3.55f, .76f, 1.92f), new Vector3(.3f, .12f, .22f), ink);
            Box("Telephone receiver", details, new Vector3(-3.55f, .88f, 1.95f), new Vector3(.37f, .065f, .075f), ink);
            for (int i = 0; i < 7; i++)
            {
                float x = -3.25f + i * 1.05f;
                Box("Mail slot label", details, new Vector3(x, 1.61f, 4.98f), new Vector3(.33f, .065f, .02f), brass);
            }
        }
    }
}
