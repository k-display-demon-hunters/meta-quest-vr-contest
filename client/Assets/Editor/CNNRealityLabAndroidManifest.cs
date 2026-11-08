#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

// Optional app capability: existing VR scenes do not require passthrough.
public sealed class CNNRealityLabAndroidManifest : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => int.MaxValue;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        const string android = "http://schemas.android.com/apk/res/android";
        string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
        var document = new XmlDocument();
        document.Load(manifestPath);
        XmlElement manifest = document.DocumentElement;
        bool hasCameraPermission = false;
        foreach (XmlNode node in manifest.SelectNodes("uses-permission"))
            hasCameraPermission |= node.Attributes["name", android]?.Value == "horizonos.permission.HEADSET_CAMERA";
        if (!hasCameraPermission)
        {
            XmlElement permission = document.CreateElement("uses-permission");
            permission.SetAttribute("name", android, "horizonos.permission.HEADSET_CAMERA");
            manifest.AppendChild(permission);
        }
        document.Save(manifestPath);
        foreach (XmlNode node in manifest.SelectNodes("uses-feature"))
            if (node.Attributes["name", android]?.Value == "com.oculus.feature.PASSTHROUGH")
                return;
        XmlElement feature = document.CreateElement("uses-feature");
        feature.SetAttribute("name", android, "com.oculus.feature.PASSTHROUGH");
        feature.SetAttribute("required", android, "false");
        manifest.AppendChild(feature);
        document.Save(manifestPath);
    }
}
#endif
