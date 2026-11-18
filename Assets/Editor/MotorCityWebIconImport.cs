using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    // Preserve the source images and sprite IDs. Keep UI textures small.
    public sealed class MotorCityWebIconImport : AssetPostprocessor
    {
        private const string Prefix = "Assets/Resources/MotorCity/UI/Icons/Tabler/";
        public override int GetPostprocessOrder() => 100;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Prefix, System.StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.mipmapEnabled = false;
            var settings = importer.GetPlatformTextureSettings("WebGL");
            settings.name = "WebGL";
            settings.overridden = true;
            settings.maxTextureSize = 128;
            settings.format = TextureImporterFormat.RGBA32;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(settings);
        }

        [MenuItem("Tools/Motor City/Build/Reimport Optimized UI Icons")]
        private static void ReimportIcons()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Prefix.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("Motor City: Tabler icons reimported.");
        }
    }
}
