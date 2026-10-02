using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MotorCity.World
{
    /// <summary>
    /// Runtime safety net for Fantastic City Generator materials.
    /// Some generated scenes keep embedded/material-instance references that do not
    /// point at the repaired assets under Resources. This pass fixes the materials
    /// that are actually assigned to renderers after each scene load.
    /// </summary>
    public static class FcgRuntimeMaterialGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            RepairLoadedRenderers();
        }

        private static void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            RepairLoadedRenderers();
        }

        private static void RepairLoadedRenderers()
        {
            Renderer[] renderers =
                UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials =
                    renderer.sharedMaterials;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material material =
                        materials[i];

                    if (material == null)
                        continue;

                    string normalized =
                        NormalizeName(
                            material.name);

                    if (IsArchitecturalGlass(
                            normalized))
                    {
                        ForceOpaqueGlass(
                            material);

                        continue;
                    }

                    if (IsFoliage(
                            normalized))
                    {
                        ForceFoliageCutout(
                            material);
                    }
                }
            }
        }

        private static bool IsArchitecturalGlass(
            string normalized)
        {
            if (string.IsNullOrWhiteSpace(
                    normalized))
                return false;

            return
                normalized == "glass01" ||
                normalized.StartsWith(
                    "glass01") ||
                normalized.StartsWith(
                    "winglass") ||
                normalized == "wins" ||
                normalized.StartsWith(
                    "wins02");
        }

        private static bool IsFoliage(
            string normalized)
        {
            if (string.IsNullOrWhiteSpace(
                    normalized))
                return false;

            return
                normalized.Contains("tree") ||
                normalized.Contains("leaf") ||
                normalized.Contains("leaves") ||
                normalized.Contains("foliage") ||
                normalized.Contains("vegetation") ||
                normalized.Contains("fern") ||
                normalized.Contains("palm");
        }

        private static void ForceOpaqueGlass(
            Material material)
        {
            if (material == null)
                return;

            if (material.HasProperty(
                    "_Surface"))
            {
                material.SetFloat(
                    "_Surface",
                    0f);
            }

            if (material.HasProperty(
                    "_AlphaClip"))
            {
                material.SetFloat(
                    "_AlphaClip",
                    0f);
            }

            if (material.HasProperty(
                    "_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.One);
            }

            if (material.HasProperty(
                    "_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.Zero);
            }

            if (material.HasProperty(
                    "_ZWrite"))
            {
                material.SetFloat(
                    "_ZWrite",
                    1f);
            }

            ForceColorAlphaOne(
                material,
                "_BaseColor");

            ForceColorAlphaOne(
                material,
                "_Color");

            material.DisableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");

            material.DisableKeyword(
                "_ALPHABLEND_ON");

            material.DisableKeyword(
                "_ALPHAPREMULTIPLY_ON");

            material.DisableKeyword(
                "_ALPHATEST_ON");

            material.SetOverrideTag(
                "RenderType",
                "Opaque");

            material.renderQueue =
                (int)RenderQueue.Geometry;
        }

        private static void ForceFoliageCutout(
            Material material)
        {
            if (material == null)
                return;

            // The custom MotorCity foliage shader performs clip() unconditionally,
            // so no keyword is required. For URP/Lit fallbacks enable alpha test.
            bool customFoliageShader =
                material.shader != null &&
                string.Equals(
                    material.shader.name,
                    "MotorCity/TwoSidedFoliage",
                    StringComparison.Ordinal);

            if (material.HasProperty(
                    "_Surface"))
            {
                material.SetFloat(
                    "_Surface",
                    0f);
            }

            if (material.HasProperty(
                    "_AlphaClip"))
            {
                material.SetFloat(
                    "_AlphaClip",
                    1f);
            }

            if (material.HasProperty(
                    "_Cutoff"))
            {
                material.SetFloat(
                    "_Cutoff",
                    0.18f);
            }

            if (material.HasProperty(
                    "_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.One);
            }

            if (material.HasProperty(
                    "_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.Zero);
            }

            if (material.HasProperty(
                    "_ZWrite"))
            {
                material.SetFloat(
                    "_ZWrite",
                    1f);
            }

            if (material.HasProperty(
                    "_Cull"))
            {
                material.SetFloat(
                    "_Cull",
                    (float)CullMode.Off);
            }

            ForceColorAlphaOne(
                material,
                "_BaseColor");

            ForceColorAlphaOne(
                material,
                "_Color");

            material.DisableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");

            material.DisableKeyword(
                "_ALPHABLEND_ON");

            material.DisableKeyword(
                "_ALPHAPREMULTIPLY_ON");

            if (customFoliageShader)
            {
                material.DisableKeyword(
                    "_ALPHATEST_ON");
            }
            else
            {
                material.EnableKeyword(
                    "_ALPHATEST_ON");
            }

            material.SetOverrideTag(
                "RenderType",
                "TransparentCutout");

            material.renderQueue =
                (int)RenderQueue.AlphaTest;

            material.doubleSidedGI =
                true;
        }

        private static void ForceColorAlphaOne(
            Material material,
            string property)
        {
            if (material == null ||
                !material.HasProperty(
                    property))
            {
                return;
            }

            Color color =
                material.GetColor(
                    property);

            if (Mathf.Approximately(
                    color.a,
                    1f))
            {
                return;
            }

            color.a =
                1f;

            material.SetColor(
                property,
                color);
        }

        private static string NormalizeName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
                return string.Empty;

            string lower =
                value.ToLowerInvariant();

            lower =
                lower.Replace(
                    "fcg_",
                    string.Empty);

            lower =
                lower.Replace(
                    "(instance)",
                    string.Empty);

            char[] chars =
                lower.ToCharArray();

            var buffer =
                new char[chars.Length];

            int length =
                0;

            foreach (char character in chars)
            {
                if (char.IsLetterOrDigit(
                        character))
                {
                    buffer[length++] =
                        character;
                }
            }

            return
                new string(
                    buffer,
                    0,
                    length);
        }
    }
}
