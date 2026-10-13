using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    // Import only the curated Tabler sprites, never raw SVG source libraries.
    public sealed class MotorCityIconPostprocessor : AssetPostprocessor
    {
        private const string TablerRoot =
            "Assets/Resources/MotorCity/UI/Icons/Tabler/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(
                    TablerRoot, System.StringComparison.OrdinalIgnoreCase))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.filterMode = FilterMode.Bilinear;
        }
    }
}
