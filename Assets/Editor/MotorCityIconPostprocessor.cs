using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    public sealed class MotorCityIconPostprocessor : AssetPostprocessor
    {
        private const string IconRoot =
            "Assets/Resources/MotorCity/UI/Icons/Kenney/";

        private bool IsMotorCityIcon =>
            assetPath.StartsWith(
                IconRoot,
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

            importer.mipmapEnabled =
                false;

            importer.alphaIsTransparency =
                true;

            importer.isReadable =
                true;

            importer.textureCompression =
                TextureImporterCompression.Uncompressed;

            importer.filterMode =
                FilterMode.Bilinear;
        }

        private void OnPostprocessTexture(
            Texture2D texture)
        {
            if (!IsMotorCityIcon ||
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
