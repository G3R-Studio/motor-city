using System;
using UnityEngine;

namespace MotorCity.World
{
    internal static class FcgRuntimeGlassMaterialFactory
    {
        public static bool IsArchitecturalGlassKey(
            string key)
        {
            if (string.IsNullOrWhiteSpace(
                    key))
            {
                return false;
            }

            return
                key.StartsWith(
                    "glass",
                    StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(
                    "winglass",
                    StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(
                    "wins",
                    StringComparison.OrdinalIgnoreCase);
        }

        public static Material Create(
            Material source,
            string key)
        {
            if (source == null ||
                !IsArchitecturalGlassKey(
                    key))
            {
                return null;
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            if (shader == null)
                return null;

            Texture baseTexture =
                GetFirstTexture(
                    source,
                    "_BaseMap",
                    "_MainTex");

            Texture normalTexture =
                GetFirstTexture(
                    source,
                    "_BumpMap");

            Material material =
                new Material(
                    shader)
                {
                    name =
                        "MotorCity_Runtime_" +
                        source.name,
                    hideFlags =
                        HideFlags.DontSave
                };

            material.enableInstancing =
                true;

            if (baseTexture != null)
            {
                material.SetTexture(
                    "_BaseMap",
                    baseTexture);

                CopyTextureTransform(
                    source,
                    material,
                    "_BaseMap",
                    source.HasProperty(
                        "_BaseMap")
                        ? "_BaseMap"
                        : "_MainTex");
            }

            if (normalTexture != null)
            {
                material.SetTexture(
                    "_BumpMap",
                    normalTexture);

                CopyTextureTransform(
                    source,
                    material,
                    "_BumpMap",
                    "_BumpMap");

                material.SetFloat(
                    "_BumpScale",
                    0.35f);

                material.EnableKeyword(
                    "_NORMALMAP");
            }

            bool genericGlass =
                key.StartsWith(
                    "glass",
                    StringComparison.OrdinalIgnoreCase) &&
                !key.StartsWith(
                    "winglass",
                    StringComparison.OrdinalIgnoreCase);

            material.SetColor(
                "_BaseColor",
                genericGlass
                    ? new Color(
                        0.25f,
                        0.25f,
                        0.25f,
                        1f)
                    : Color.white);

            material.SetFloat(
                "_Surface",
                0f);

            material.SetFloat(
                "_Metallic",
                0f);

            material.SetFloat(
                "_Smoothness",
                0.05f);

            material.SetFloat(
                "_EnvironmentReflections",
                0f);

            material.SetFloat(
                "_SpecularHighlights",
                0f);

            material.SetFloat(
                "_ZWrite",
                1f);

            if (material.HasProperty(
                    "_EmissionColor"))
            {
                material.SetColor(
                    "_EmissionColor",
                    Color.black);
            }

            material.DisableKeyword(
                "_EMISSION");

            material.DisableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");

            material.SetOverrideTag(
                "RenderType",
                "Opaque");

            material.renderQueue =
                (int)UnityEngine.Rendering.RenderQueue.Geometry;

            return material;
        }

        private static Texture GetFirstTexture(
            Material material,
            params string[] properties)
        {
            if (material == null ||
                properties == null)
            {
                return null;
            }

            for (int i = 0;
                 i < properties.Length;
                 i++)
            {
                string property =
                    properties[i];

                if (string.IsNullOrWhiteSpace(
                        property) ||
                    !material.HasProperty(
                        property))
                {
                    continue;
                }

                Texture texture =
                    material.GetTexture(
                        property);

                if (texture != null)
                    return texture;
            }

            return null;
        }

        private static void CopyTextureTransform(
            Material source,
            Material destination,
            string destinationProperty,
            string sourceProperty)
        {
            if (source == null ||
                destination == null ||
                string.IsNullOrWhiteSpace(
                    destinationProperty) ||
                string.IsNullOrWhiteSpace(
                    sourceProperty) ||
                !source.HasProperty(
                    sourceProperty) ||
                !destination.HasProperty(
                    destinationProperty))
            {
                return;
            }

            destination.SetTextureScale(
                destinationProperty,
                source.GetTextureScale(
                    sourceProperty));

            destination.SetTextureOffset(
                destinationProperty,
                source.GetTextureOffset(
                    sourceProperty));
        }
    }
}
