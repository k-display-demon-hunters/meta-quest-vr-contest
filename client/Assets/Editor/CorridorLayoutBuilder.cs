using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Persists transient layout resources only when explicitly saving an edited corridor.
public static class CorridorLayoutBuilder
{
    public static void PersistGeneratedAssets(GameObject root)
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
