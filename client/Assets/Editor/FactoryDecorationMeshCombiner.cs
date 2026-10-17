using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment.Editor
{
    public static class FactoryDecorationMeshCombiner
    {
        private const string ScenePath = "Assets/Scenes/FactoryEnvironment.unity";
        private const string MeshFolder = "Assets/Meshes/FactoryEnvironment";
        private const string CombinedPrefix = "Combined decoration - ";
        private const int AdditionalLightLimit = 4;
        private const float MaximumGroupSize = 8f;
        private const float MovingLightMargin = 0.5f;

        private sealed class MeshGroup
        {
            public Transform Parent;
            public string Signature;
            public Bounds Bounds;
            public readonly List<MeshRenderer> Sources = new List<MeshRenderer>();
        }

        [MenuItem("Tools/Factory/Combine Fixed Decorations")]
        public static void Combine()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath || scene.isDirty)
            {
                throw new InvalidOperationException("Open the saved FactoryEnvironment scene in Edit mode first.");
            }

            Transform factory = FindFactory(scene);
            foreach (Transform child in factory.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith(CombinedPrefix, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Combined decorations already exist. Revert the previous combination before rebuilding.");
                }
            }

            // A new renderer has a different probe sampling position. Do not silently change baked lighting.
            if ((LightmapSettings.lightProbes != null && LightmapSettings.lightProbes.count > 0) ||
                UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Length > 0)
            {
                throw new InvalidOperationException("Review probe lighting before combining this scene.");
            }

            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            List<MeshGroup> groups = CollectGroups(factory, lights);
            if (groups.Count == 0)
            {
                Debug.Log("[Factory] No compatible fixed decorations to combine.");
                return;
            }

            EnsureMeshFolder();
            List<string> createdAssets = new List<string>();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Combine factory decorations");
            int sourceCount = 0;
            try
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    CreateCombinedRenderer(groups[i], i, createdAssets);
                    sourceCount += groups[i].Sources.Count;
                }

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("Could not save FactoryEnvironment.");
                }
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log($"[Factory] Combined {sourceCount} decoration renderers into {groups.Count}. Original objects, transforms and colliders were retained.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                foreach (string path in createdAssets)
                {
                    AssetDatabase.DeleteAsset(path);
                }
                throw;
            }
        }

        private static Transform FindFactory(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Factory Environment")
                {
                    return root.transform;
                }
            }
            throw new InvalidOperationException("Factory Environment root was not found.");
        }

        private static List<MeshGroup> CollectGroups(Transform factory, Light[] lights)
        {
            List<MeshGroup> groups = new List<MeshGroup>();
            foreach (Transform room in factory)
            {
                if (room.name.Length < 3 || room.name[0] != '0' || room.name[1] < '0' || room.name[1] > '6')
                {
                    continue;
                }
                foreach (MeshRenderer renderer in room.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!CanCombine(renderer, factory))
                    {
                        continue;
                    }

                    // Keep every serialized renderer setting, including material and shadow settings,
                    // in the grouping key. Only the owning GameObject is allowed to differ.
                    string signature = Regex.Replace(EditorJsonUtility.ToJson(renderer),
                        "\"m_GameObject\":\\{[^}]*\\}", "\"m_GameObject\":{}");
                    signature += ":" + renderer.gameObject.layer + ":" +
                        (int)GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                    MeshGroup matchingGroup = null;
                    foreach (MeshGroup group in groups)
                    {
                        if (group.Parent != renderer.transform.parent || group.Signature != signature)
                        {
                            continue;
                        }
                        Bounds combinedBounds = group.Bounds;
                        combinedBounds.Encapsulate(renderer.bounds);
                        if (FitsLightingAndSize(combinedBounds, renderer.gameObject.layer, lights))
                        {
                            matchingGroup = group;
                            matchingGroup.Bounds = combinedBounds;
                            break;
                        }
                    }
                    if (matchingGroup == null)
                    {
                        matchingGroup = new MeshGroup
                        {
                            Parent = renderer.transform.parent,
                            Signature = signature,
                            Bounds = renderer.bounds
                        };
                        groups.Add(matchingGroup);
                    }
                    matchingGroup.Sources.Add(renderer);
                }
            }
            groups.RemoveAll(group => group.Sources.Count < 2);
            return groups;
        }

        private static bool CanCombine(MeshRenderer renderer, Transform factory)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Material[] materials = renderer.sharedMaterials;
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.isPartOfStaticBatch ||
                filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable ||
                filter.sharedMesh.subMeshCount != 1 || materials.Length != 1 || materials[0] == null ||
                materials[0].renderQueue > (int)RenderQueue.GeometryLast || renderer.HasPropertyBlock() ||
                renderer.additionalVertexStreams != null || renderer.transform.childCount != 0 ||
                renderer.localToWorldMatrix.determinant <= 0f ||
                (renderer.lightmapIndex >= 0 && renderer.lightmapIndex < 65534) ||
                (renderer.realtimeLightmapIndex >= 0 && renderer.realtimeLightmapIndex < 65534) ||
                (GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.BatchingStatic) == 0 ||
                PrefabUtility.IsPartOfPrefabInstance(renderer))
            {
                return false;
            }
            foreach (Component component in renderer.GetComponents<Component>())
            {
                if (!(component is Transform) && !(component is MeshFilter) && !(component is MeshRenderer))
                {
                    return false;
                }
            }
            for (Transform ancestor = renderer.transform.parent; ancestor != null; ancestor = ancestor.parent)
            {
                foreach (Component component in ancestor.GetComponents<Component>())
                {
                    if (component == null || component is Behaviour || component is Rigidbody || component is LODGroup)
                    {
                        return false;
                    }
                }
                if (ancestor == factory)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool FitsLightingAndSize(Bounds bounds, int layer, Light[] lights)
        {
            if (Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) > MaximumGroupSize)
            {
                return false;
            }
            int lightCount = 0;
            foreach (Light light in lights)
            {
                if (!light.enabled || !light.gameObject.activeInHierarchy || light.type == LightType.Directional ||
                    light.lightmapBakeType == LightmapBakeType.Baked || (light.cullingMask & (1 << layer)) == 0)
                {
                    continue;
                }
                // Mobile URP selects at most four additional lights per renderer. Bound each group
                // conservatively, including the small motion of the collectible parchment lights.
                float range = light.range + MovingLightMargin;
                if (bounds.SqrDistance(light.transform.position) <= range * range && ++lightCount > AdditionalLightLimit)
                {
                    return false;
                }
            }
            return true;
        }

        private static void CreateCombinedRenderer(MeshGroup group, int index, List<string> createdAssets)
        {
            MeshRenderer source = group.Sources[0];
            CombineInstance[] instances = new CombineInstance[group.Sources.Count];
            int vertexCount = 0;
            for (int i = 0; i < instances.Length; i++)
            {
                MeshRenderer renderer = group.Sources[i];
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                vertexCount += mesh.vertexCount;
                instances[i] = new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = 0,
                    transform = group.Parent.worldToLocalMatrix * renderer.localToWorldMatrix
                };
            }
            Mesh combinedMesh = new Mesh
            {
                name = $"FactoryDecoration_{index:D3}_{source.sharedMaterial.name}",
                indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            try
            {
                combinedMesh.CombineMeshes(instances, true, true, false);
                combinedMesh.RecalculateBounds();
                string path = AssetDatabase.GenerateUniqueAssetPath($"{MeshFolder}/{combinedMesh.name}.asset");
                AssetDatabase.CreateAsset(combinedMesh, path);
                createdAssets.Add(path);
            }
            catch
            {
                if (!EditorUtility.IsPersistent(combinedMesh))
                {
                    UnityEngine.Object.DestroyImmediate(combinedMesh);
                }
                throw;
            }

            GameObject combined = new GameObject($"{CombinedPrefix}{source.sharedMaterial.name} {index:D3}");
            Undo.RegisterCreatedObjectUndo(combined, "Create combined decoration");
            combined.transform.SetParent(group.Parent, false);
            combined.layer = source.gameObject.layer;
            // The mesh is already combined; avoid a second static-batching mesh copy.
            GameObjectUtility.SetStaticEditorFlags(combined,
                GameObjectUtility.GetStaticEditorFlags(source.gameObject) & ~StaticEditorFlags.BatchingStatic);
            combined.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
            MeshRenderer combinedRenderer = combined.AddComponent<MeshRenderer>();
            EditorUtility.CopySerialized(source, combinedRenderer);
            foreach (MeshRenderer renderer in group.Sources)
            {
                Undo.RecordObject(renderer, "Disable original decoration renderer");
                renderer.enabled = false;
            }
        }

        private static void EnsureMeshFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Meshes"))
            {
                throw new InvalidOperationException("The existing Assets/Meshes folder is missing.");
            }
            if (!AssetDatabase.IsValidFolder(MeshFolder))
            {
                AssetDatabase.CreateFolder("Assets/Meshes", "FactoryEnvironment");
            }
        }
    }
}
