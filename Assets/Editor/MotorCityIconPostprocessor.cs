using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    public sealed class MotorCityIconPostprocessor : AssetPostprocessor
    {
        private const string IconRoot =
            "Assets/Resources/MotorCity/UI/Icons/Kenney/";

        private const string TablerRoot =
            "Assets/Resources/MotorCity/UI/Icons/Tabler/";

        private bool IsKenneyIcon => assetPath.StartsWith(
            IconRoot, System.StringComparison.OrdinalIgnoreCase);

        private bool IsMotorCityIcon => IsKenneyIcon ||
            assetPath.StartsWith(TablerRoot,
                System.StringComparison.OrdinalIgnoreCase);

        private void OnPreprocessTexture()
        {
            if (!IsMotorCityIcon)
                return;

            TextureImporter importer =
                (TextureImporter)assetImporter;

            importer.textureType =
                TextureImporterType.Sprite;

            importer.spriteImportMode =
                SpriteImportMode.Single;

            // Tabler outlines are often displayed at 18-30 canvas units
            // from a 192/384 px source. Mips provide proper minification
            // rather than aliasing from a single 2x2 bilinear sample.
            // Leave Kenney's existing icon-font textures unchanged.
            importer.mipmapEnabled =
                !IsKenneyIcon;

            importer.alphaIsTransparency =
                true;

            // Kenney's post-import whitening pass needs readable pixels.
            // Tabler UI textures do not: avoid duplicating them in CPU RAM.
            importer.isReadable =
                IsKenneyIcon;

            importer.npotScale =
                TextureImporterNPOTScale.None;

            importer.wrapMode =
                TextureWrapMode.Clamp;

            importer.maxTextureSize =
                IsKenneyIcon ? importer.maxTextureSize : 512;

            importer.textureCompression =
                TextureImporterCompression.Uncompressed;

            importer.crunchedCompression =
                false;

            importer.filterMode =
                FilterMode.Bilinear;
        }

        private void OnPostprocessTexture(
            Texture2D texture)
        {
            if (!IsKenneyIcon ||
                texture == null)
            {
                return;
            }

            Color32[] pixels =
                texture.GetPixels32();

            bool changed =
                false;

            for (int i = 0;
                 i < pixels.Length;
                 i++)
            {
                Color32 pixel =
                    pixels[i];

                if (pixel.a == 0)
                    continue;

                // Kenney icon-font PNGs are black glyphs on transparency.
                // Convert only the glyph RGB to white while preserving alpha,
                // so Unity UI/SpriteRenderer tinting works as intended.
                if (pixel.r != 255 ||
                    pixel.g != 255 ||
                    pixel.b != 255)
                {
                    pixels[i] =
                        new Color32(
                            255,
                            255,
                            255,
                            pixel.a);

                    changed =
                        true;
                }
            }

            if (!changed)
                return;

            texture.SetPixels32(
                pixels);

            texture.Apply(
                false,
                false);
        }
    }
}
