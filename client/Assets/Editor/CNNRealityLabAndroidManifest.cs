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
