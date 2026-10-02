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
                    "MotorCity/NightEmissive");

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

            Texture emissionTexture =
                GetFirstTexture(
                    source,
                    "_EmissionMap",
                    "_Illum");

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
                    0.58f);
            }
            else
            {
                material.SetFloat(
                    "_BumpScale",
                    0f);
            }

            if (emissionTexture != null)
            {
                material.SetTexture(
                    "_EmissionMap",
                    emissionTexture);

                CopyTextureTransform(
                    source,
                    material,
                    "_EmissionMap",
                    source.HasProperty(
                        "_EmissionMap")
                        ? "_EmissionMap"
                        : "_MainTex");
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

            material.SetColor(
                "_DayGlassTint",
                new Color(
                    0.20f,
                    0.205f,
                    0.21f,
                    1f));

            material.SetFloat(
                "_DayGlassLift",
                0.32f);

            material.SetColor(
                "_NightGlassTint",
                new Color(
                    0.04f,
                    0.045f,
                    0.05f,
                    1f));

            material.SetFloat(
                "_NightGlassLift",
                0.16f);

            material.SetFloat(
                "_Roughness",
                0.28f);

            material.SetFloat(
                "_ReflectionStrength",
                0.55f);

            material.SetFloat(
                "_AuthoredCubeStrength",
                0f);

            material.SetFloat(
                "_FresnelStrength",
                1f);

            material.SetFloat(
                "_SpecularStrength",
                0.12f);

            material.SetFloat(
                "_EmissionStrength",
                genericGlass ||
                emissionTexture == null
                    ? 0f
                    : 3.2f);

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
