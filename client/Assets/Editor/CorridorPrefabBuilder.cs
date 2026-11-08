using System;
using System.Collections.Generic;
using System.IO;
using FeatureMapCorridor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>One-time migration from the procedural corridor to an editable prefab.</summary>
public static class CorridorPrefabBuilder
{
    private const string ScenePath = "Assets/Scenes/CNNFeatureMapLab.unity";
    private const string PrefabPath = "Assets/Prefabs/CNNFeatureMapLab/FeatureMapCorridor.prefab";

    [MenuItem("Tools/CNN/Save Feature Map Corridor Prefab")]
    public static void SaveCorridorPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Save the corridor outside Play mode.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            throw new InvalidOperationException("The corridor prefab already exists. Edit that prefab or its scene instance; do not regenerate it.");

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        if (scene.isDirty)
            throw new InvalidOperationException("Save or discard your own scene edits before the one-time corridor migration.");

        CorridorDirector director = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            director = root.GetComponent<CorridorDirector>();
            if (director != null) { break; }
        }
        if (director == null)
            throw new InvalidOperationException("CNNFeatureMapLab must contain its original CorridorRoot.");

        CnnModelProvider provider = director.GetComponent<CnnModelProvider>();
        var providerData = new SerializedObject(provider);
        SerializedProperty modelSlot = providerData.FindProperty("_modelInstance");
        var modelPrefab = (MonoBehaviour)modelSlot.objectReferenceValue;
        var modelObject = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab.gameObject, director.transform);
        modelSlot.objectReferenceValue = modelObject.GetComponent(modelPrefab.GetType());
        providerData.ApplyModifiedPropertiesWithoutUndo();

        director.BuildLayoutForEditor();
        foreach (TMP_Text text in director.GetComponentsInChildren<TMP_Text>(true))
            text.ForceMeshUpdate(true, true);

        SaveCurrentLayout();
    }

    public static void SaveCurrentLayout()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        CorridorDirector director = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            director = root.GetComponent<CorridorDirector>();
            if (director != null) { break; }
        }
        PersistGeneratedAssets(director.gameObject);
        EditorUtility.SetDirty(director);
        PrefabUtility.SaveAsPrefabAssetAndConnect(director.gameObject, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("Could not save CNNFeatureMapLab after creating the corridor prefab.");

        Selection.activeGameObject = director.gameObject;
        Debug.Log("Saved editable feature map corridor: " + PrefabPath);
    }

    private static void PersistGeneratedAssets(GameObject root)
    {
        Object[] dependencies = CollectTransientAssets(root);
        var saved = new HashSet<Object>();

        // A TMP font keeps its atlas and material as sub-assets, not scene-only objects.
        foreach (Object dependency in dependencies)
        {
            if (dependency is not TMP_FontAsset font || IsPersistent(font)) { continue; }
            string path = NewPath("Fonts/CNNFeatureMapLab", font.sourceFontFile.name + " SDF", ".asset");
            font.name = font.sourceFontFile.name + " SDF";
            font.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(font, path);
            saved.Add(font);
            AddSubAsset(font.material, font, saved);
            foreach (Texture2D atlas in font.atlasTextures) { AddSubAsset(atlas, font, saved); }
        }

        foreach (Object dependency in dependencies)
        {
            if (dependency is not VolumeProfile profile || IsPersistent(profile)) { continue; }
            profile.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(profile, NewPath("Settings", "CorridorPostProcessing", ".asset"));
            saved.Add(profile);
            foreach (VolumeComponent component in profile.components) { AddSubAsset(component, profile, saved); }
        }

        foreach (Object dependency in dependencies)
        {
            if (IsPersistent(dependency) || saved.Contains(dependency)) { continue; }
            string folder;
            string extension = ".asset";
            switch (dependency)
            {
                case Material: folder = "Materials/CNNFeatureMapLab"; extension = ".mat"; break;
                case Texture2D: folder = "Textures/CNNFeatureMapLab"; break;
                case Sprite: folder = "Textures/CNNFeatureMapLab"; break;
                case Mesh mesh when mesh.name == "Receptive field outlines": folder = "Meshes/CNNFeatureMapLab"; break;
                default: continue;
            }
            dependency.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(dependency, NewPath(folder, dependency.name, extension));
            saved.Add(dependency);
        }

        // Save again after all cross-asset references have acquired persistent IDs.
        foreach (Object asset in saved) { EditorUtility.SetDirty(asset); }
        foreach (Object asset in saved) { AssetDatabase.SaveAssetIfDirty(asset); }
    }

    private static bool IsPersistent(Object asset) => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset));

    private static Object[] CollectTransientAssets(GameObject root)
    {
        // CollectDependencies omits newly created, unsaved assets. Walk live serialized
        // references so textures, fonts and materials acquire IDs before prefab serialization.
        var pending = new Queue<Object>();
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
            pending.Enqueue(component);
        var visited = new HashSet<Object>();
        var assets = new List<Object>();
        while (pending.Count > 0)
        {
            Object current = pending.Dequeue();
            if (!visited.Add(current)) { continue; }
            using var serialized = new SerializedObject(current);
            SerializedProperty property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) { continue; }
                Object reference = property.objectReferenceValue;
                if (reference == null || reference is Component || reference is GameObject || IsPersistent(reference)) { continue; }
                pending.Enqueue(reference);
            }
            if (current is not Component) { assets.Add(current); }
        }
        return assets.ToArray();
    }

    private static void AddSubAsset(Object asset, Object owner, ISet<Object> saved)
    {
        if (IsPersistent(asset) || saved.Contains(asset)) { return; }
        asset.hideFlags = HideFlags.None;
        AssetDatabase.AddObjectToAsset(asset, owner);
        saved.Add(asset);
    }

    private static string NewPath(string folder, string name, string extension)
    {
        string parent = "Assets";
        foreach (string part in folder.Split('/'))
        {
            string child = parent + "/" + part;
            if (!AssetDatabase.IsValidFolder(child)) { AssetDatabase.CreateFolder(parent, part); }
            parent = child;
        }
        foreach (char invalid in Path.GetInvalidFileNameChars()) { name = name.Replace(invalid, '_'); }
        if (string.IsNullOrWhiteSpace(name)) { name = "Corridor"; }
        return AssetDatabase.GenerateUniqueAssetPath(parent + "/" + name + extension);
    }
}
