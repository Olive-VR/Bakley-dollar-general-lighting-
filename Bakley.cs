using UnityEngine;
using UnityEditor;
using System.IO;

public class Bakley : EditorWindow
{
    float brightness = 1f;
    DefaultAsset folderAsset;
    string folderPath;
    bool enableAO;
    float aoMaxDistance = 1f;
    int lightmapResolution = 50;

    [MenuItem("Bakley/Dollar General Lighting")]
    static void Init() => GetWindow<Bakley>("Bakley");

    void OnGUI()
    {
        folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Folder", folderAsset, typeof(DefaultAsset), false);
        if (folderAsset != null)
            folderPath = AssetDatabase.GetAssetPath(folderAsset);

        brightness = EditorGUILayout.Slider("Brightness", brightness, 0f, 10f);
        enableAO = EditorGUILayout.Toggle("Ambient Occlusion", enableAO);
        if (enableAO)
            aoMaxDistance = EditorGUILayout.FloatField("Ambient Occlusion Intensity", aoMaxDistance);
        lightmapResolution = EditorGUILayout.IntField("Lightmap Pixel Amount Thing", lightmapResolution);

        if (GUILayout.Button("Bake"))
        {
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
                return;

            LightmapEditorSettings.enableAmbientOcclusion = enableAO;
            if (enableAO)
                LightmapEditorSettings.aoMaxDistance = aoMaxDistance;
            LightmapEditorSettings.realtimeResolution = lightmapResolution;

            foreach (var file in Directory.GetFiles(folderPath, "LightingData*.asset"))
            {
                var assetPath = "Assets" + file.Substring(Application.dataPath.Length);
                AssetDatabase.DeleteAsset(assetPath);
            }

            foreach (var r in FindObjectsOfType<Renderer>())
                foreach (var m in r.sharedMaterials)
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                        m.SetColor("_EmissionColor", m.GetColor("_EmissionColor") * brightness);
                        EditorUtility.SetDirty(m);
                    }

            EditorUtility.DisplayProgressBar("Bakley", "Baking lightmaps...", 0f);
            Lightmapping.Bake();
            EditorUtility.ClearProgressBar();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var src = "Assets/LightingData.asset";
            var dst = Path.Combine(folderPath, "LightingData.asset");
            if (AssetDatabase.LoadAssetAtPath<Object>(src) != null)
                AssetDatabase.CopyAsset(src, dst);

            Debug.Log($"[Bakley] Done -> {dst}");
        }
    }
}
