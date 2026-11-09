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

/// <summary>Saves the existing editable corridor without rebuilding its hierarchy.</summary>
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

        CNNFeatureMapLabManager director = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            director = root.GetComponent<CNNFeatureMapLabManager>();
            if (director != null) { break; }
        }
        if (director == null)
            throw new InvalidOperationException("CNNFeatureMapLab must contain its original CorridorRoot.");

        SaveCurrentLayout();
    }

    public static void SaveCurrentLayout()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        CNNFeatureMapLabManager director = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            director = root.GetComponent<CNNFeatureMapLabManager>();
            if (director != null) { break; }
        }
        CorridorLayoutBuilder.PersistGeneratedAssets(director.gameObject);
        EditorUtility.SetDirty(director);
        PrefabUtility.SaveAsPrefabAssetAndConnect(director.gameObject, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("Could not save CNNFeatureMapLab after creating the corridor prefab.");

        Selection.activeGameObject = director.gameObject;
        Debug.Log("Saved editable feature map corridor: " + PrefabPath);
    }

}
