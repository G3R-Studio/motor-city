using System;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    // Restrict the experiment to background music on WebGL.
    public sealed class MotorCityWebMusicImport : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/MotorCity/Music";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder + "/", StringComparison.Ordinal) ||
                !assetPath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                return;

            var importer = (AudioImporter)assetImporter;
            var settings = importer.ContainsSampleSettingsOverride("WebGL")
                ? importer.GetOverrideSampleSettings("WebGL")
                : importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.quality = 0.35f;
            // Preserve sample rate, channels and preload behavior.
            if (!importer.SetOverrideSampleSettings("WebGL", settings))
                Debug.LogWarning("Motor City: WebGL music override rejected: " + assetPath);
        }

        [MenuItem("Tools/Motor City/Build/Reimport Optimized Music")]
        private static void ReimportMusic()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("Motor City: music reimported. Check WebGL audio settings and rebuild.");
        }
    }
}
