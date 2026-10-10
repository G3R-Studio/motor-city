using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    public static class FcgRuntimeGlassMaterialFactory
    {
        private static readonly HashSet<Material> windowMaterials = new();
        private static readonly HashSet<Material> runtimeCreatedMaterials = new();

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetWindowMaterialTracking()
        {
            foreach (Material material in runtimeCreatedMaterials)
            {
                if (material != null)
                {
                    UnityEngine.Object.Destroy(
                        material);
                }
            }

            runtimeCreatedMaterials.Clear();
            windowMaterials.Clear();
        }

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

        // Night glow is the only intentional runtime city-material change.
        // Clone each exact authored window material once, preserving textures,
        // surface type, metallic, smoothness and all reflection settings.
        // Non-window materials and windows without a real emission mask
        // remain assigned directly from CityVisual.prefab.
        public static void BindAuthoredWindowEmission(GameObject city)
        {
            if (city == null)
                return;

            var clones = new Dictionary<Material, Material>();
            Renderer[] renderers = city.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                    continue;

                bool changed = false;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material authored = materials[index];
                    if (authored == null ||
                        !IsArchitecturalGlassKey(authored.name) ||
                        !authored.HasProperty("_EmissionMap") ||
                        authored.GetTexture("_EmissionMap") == null ||
                        !authored.HasProperty("_EmissionColor"))
                    {
                        continue;
                    }

                    if (!clones.TryGetValue(authored, out Material copy))
                    {
                        copy = CloneForRuntime(authored);
                        copy.name = "MotorCity_NightEmission_" + authored.name;
                        copy.SetColor("_EmissionColor", Color.black);
                        copy.EnableKeyword("_EMISSION");
                        windowMaterials.Add(copy);
                        clones.Add(authored, copy);
                    }

                    materials[index] = copy;
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }
        }

        public static Material CloneForRuntime(
            Material source)
        {
            if (source == null)
                return null;

            Material material =
                new Material(
                    source)
                {
                    name =
                        "MotorCity_Runtime_" +
                        source.name,
                    hideFlags =
                        HideFlags.DontSave
                };

            runtimeCreatedMaterials.Add(
                material);

            return material;
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

            Texture emissionMask = GetFirstTexture(source, "_EmissionMap", "_Illum");
            if (emissionMask != null)
            {
                material.SetTexture("_EmissionMap", emissionMask);
                CopyTextureTransform(source, material, "_EmissionMap",
                    source.HasProperty("_EmissionMap") ? "_EmissionMap" : "_Illum");
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

            runtimeCreatedMaterials.Add(
                material);

            ConfigureReflections(material);
            return material;
        }

        public static void ConfigureReflections(Material material)
        {
            if (material == null) return;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .30f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .88f);
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", .12f);
            if (material.HasProperty("_EnvironmentReflections")) material.SetFloat("_EnvironmentReflections", 1f);
            if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 1f);
            material.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            // Only authored emission masks light windows; no whole-facade glow.
            if (material.HasProperty("_EmissionMap") && material.GetTexture("_EmissionMap") != null)
            {
                material.EnableKeyword("_EMISSION");
                windowMaterials.Add(material);
            }
        }
        public static void UpdateWindowEmission(float night)
        {
            windowMaterials.RemoveWhere(material => material == null);
            float intensity = Mathf.SmoothStep(0f, 2.4f, Mathf.InverseLerp(.15f, .75f, night));
            foreach (Material material in windowMaterials)
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", new Color(1f, .78f, .48f, 1f) * intensity);
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
