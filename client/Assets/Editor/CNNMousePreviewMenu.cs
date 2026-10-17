using UnityEditor;
using UnityEngine;
using UI;

[InitializeOnLoad]
public static class CNNMousePreviewMenu
{
    private const string MenuPath = "Tools/CNN/Mouse Preview (This Computer Only)";
    private static double _nextCheck;

    static CNNMousePreviewMenu()
    {
        EditorApplication.update += UpdatePreview;
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        EditorPrefs.SetBool(CNNMousePreviewController.PreferenceKey,
            !EditorPrefs.GetBool(CNNMousePreviewController.PreferenceKey, false));
        _nextCheck = 0;
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateToggle()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(CNNMousePreviewController.PreferenceKey, false));
        return true;
    }

    private static void UpdatePreview()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling ||
            EditorApplication.timeSinceStartup < _nextCheck) { return; }
        _nextCheck = EditorApplication.timeSinceStartup + 0.5;
        CNNPixelLabManager lab = Object.FindFirstObjectByType<CNNPixelLabManager>();
        if (lab == null) { return; }
        CNNMousePreviewController preview = lab.GetComponent<CNNMousePreviewController>();
        bool requested = EditorPrefs.GetBool(CNNMousePreviewController.PreferenceKey, false)
            && !UnityEngine.XR.XRSettings.isDeviceActive;
        if (requested && preview == null)
        {
            lab.gameObject.AddComponent<CNNMousePreviewController>();
        }
        else if (!requested && preview != null)
        {
            Object.Destroy(preview);
        }
    }
}
