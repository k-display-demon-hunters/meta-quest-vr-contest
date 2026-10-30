using Parchment;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Education.Editor
{
    public static class ParchmentSceneSetup
    {
        [MenuItem("Tools/Factory/Refresh Learning Parchment Labels")]
        public static void RefreshLabels()
        {
            foreach (ParchmentPickupController pickup in Object.FindObjectsByType<ParchmentPickupController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (Text label in pickup.GetComponentsInChildren<Text>(true))
                {
                    Undo.RecordObject(label, "Refresh parchment label");
                    switch (label.name)
                    {
                        case "Document number": label.text = "양피지 " + (pickup.LessonIndex + 1).ToString("00"); break;
                        case "Lesson title": label.text = ParchmentCatalog.Get(pickup.LessonIndex).Title; break;
                        case "Collect instruction": label.text = "Trigger / Grip으로 수집하고 읽기"; break;
                    }
                }
            }
        }
    }
}
