using System;
using UnityEditor;

namespace XR.Editor
{
    public static class QuestSetup
    {
        public const string ScenePath = CNNScreen.Editor.ScreenLabBuilder.ScenePath;

        [MenuItem("Tools/CNN/Configure Quest VR")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before configuring Quest.");
            }

            // Both menus use the same project settings without changing the app identity or scene order.
            XRProjectSetup.ConfigureProject();
        }
    }
}
