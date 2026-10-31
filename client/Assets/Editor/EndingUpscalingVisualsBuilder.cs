using OpeningEnding.Ending;
using UnityEditor;
using UnityEngine;

namespace FactoryScenes.Editor
{
    public static class EndingUpscalingVisualsBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/FactoryEnding/EndingUpscalingProcess.prefab";

        public static EndingUpscalingAnimationController Build(Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return instance.GetComponent<EndingUpscalingAnimationController>();
        }
    }
}
