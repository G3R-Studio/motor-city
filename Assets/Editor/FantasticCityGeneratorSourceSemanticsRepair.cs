#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FantasticCityGeneratorSourceSemanticsRepair
{
    private const string GeneratedRoot =
        "Assets/Resources/MotorCity/Environment/FCGMaterials";

    private const string SourceRoot =
        "Assets/Fantastic City Generator";

    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Source Material Semantics",
                "Останови Play Mode перед восстановлением материалов.",
                "OK");
            return;
        }

        Shader urpLit =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Source Material Semantics",
                "Universal Render Pipeline/Lit не найден.",
                "OK");
            return;
        }

        Dictionary<string, Material> generated =
            LoadGeneratedMaterials();

        Dictionary<string, List<Material>> source =
            LoadSourceMaterials();

        string[] targetNames =
        {
            "FCG_Atlas-1",
            "FCG_Grade1",
            "FCG_Grade2",
            "FCG_Glass-01",
            "FCG_WinGlass-01",
            "FCG_WinGlass-01-D",
            "FCG_WinGlass-03",
            "FCG_Wins",
            "FCG_Wins-02",
            "FCG_Roads",
            "FCG_HighWay",
            "FCG_Grass-01",
            "FCG_Grass-Splat"
        };

        int repaired =
            0;

        var missing =
            new List<string>();

        var details =
            new List<string>();

        foreach (string generatedName in
                 targetNames)
        {
            if (!generated.TryGetValue(
                    generatedName,
                    out Material target) ||
                target == null)
            {
                missing.Add(
                    generatedName + " (generated)");

                continue;
            }

            string shortName =
                generatedName.StartsWith(
                    "FCG_",
                    StringComparison.OrdinalIgnoreCase)
                    ? generatedName.Substring(4)
                    : generatedName;

            Material original =
                FindBestSource(
                    source,
                    shortName);

            if (original == null)
            {
                missing.Add(
                    generatedName + " (source)");

                continue;
            }

            Undo.RecordObject(
                target,
                "Restore FCG source material semantics");

            target.shader =
                urpLit;

            CopyBaseTexture(
                original,
                target);

            CopyNormalTexture(
                original,
                target);

            CopyColor(
                original,
                target);

            string sourceShader =
                original.shader != null
                    ? original.shader.name
                    : string.Empty;

            int sourceQueue =
                original.renderQueue;

            if (generatedName == "FCG_Grade2" ||
                sourceShader.IndexOf(
                    "Transparent/Cutout",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                sourceQueue ==
                    (int)RenderQueue.AlphaTest)
            {
                float cutoff =
                    original.HasProperty("_Cutoff")
                        ? Mathf.Clamp01(
                            original.GetFloat("_Cutoff"))
                        : 0.436f;

                ConfigureCutout(
                    target,
                    cutoff);
            }
            else if (generatedName == "FCG_Grade1" ||
                     sourceShader.IndexOf(
                         "DiffuseAlpha",
                         StringComparison.OrdinalIgnoreCase) >= 0 ||
                     sourceQueue >=
                         (int)RenderQueue.Transparent)
            {
                ConfigureTransparent(
                    target);
            }
            else
            {
                ConfigureOpaque(
                    target);
            }

            // These FCG window materials use opaque reflective shaders in the
            // original package. Making them URP-transparent reveals that the
            // building meshes have no modeled interiors.
            if (generatedName.StartsWith(
                    "FCG_WinGlass",
                    StringComparison.OrdinalIgnoreCase))
            {
                ConfigureOpaque(
                    target);
            }

            // Atlas-1 is explicitly opaque in the source material and its TGA
            // has no alpha channel. Do not alpha-clip it.
            if (generatedName ==
                "FCG_Atlas-1")
            {
                ConfigureOpaque(
                    target);
            }

            target.enableInstancing =
                true;

            EditorUtility.SetDirty(
                target);

            repaired++;

            details.Add(
                $"{generatedName}: sourceShader={sourceShader}, " +
                $"sourceQueue={sourceQueue}, generatedQueue={target.renderQueue}, " +
                $"source={AssetDatabase.GetAssetPath(original)}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Motor City: restored FCG source material semantics.\n" +
            string.Join("\n", details) +
            (missing.Count > 0
                ? "\nMissing: " + string.Join(", ", missing)
                : string.Empty));

        EditorUtility.DisplayDialog(
            "Motor City — Source Material Semantics",
            "Готово.\n\n" +
            $"Исправлено: {repaired}/{targetNames.Length}\n" +
            $"Не найдено: {(missing.Count == 0 ? "0" : string.Join(", ", missing))}\n\n" +
            "Ключевые исправления:\n" +
            "• Grade2 снова alpha-cutout (дорожная разметка)\n" +
            "• Grade1 снова transparent\n" +
            "• WinGlass снова opaque, как в оригинале\n" +
            "• Atlas-1 снова opaque без alpha-clip\n\n" +
            "Теперь выполни Build Runtime City from Saved FCG City.",
            "OK");
    }

    private static Dictionary<string, Material> LoadGeneratedMaterials()
    {
        var result =
            new Dictionary<string, Material>(
                StringComparer.OrdinalIgnoreCase);

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[] { GeneratedRoot });

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (material == null ||
                string.IsNullOrWhiteSpace(
                    material.name))
                continue;

            if (!result.ContainsKey(
                    material.name))
            {
                result.Add(
                    material.name,
                    material);
            }
        }

        return result;
    }

    private static Dictionary<string, List<Material>> LoadSourceMaterials()
    {
        var result =
            new Dictionary<string, List<Material>>(
                StringComparer.OrdinalIgnoreCase);

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[] { SourceRoot });

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (material == null)
                continue;

            foreach (string raw in new[]
            {
                material.name,
                Path.GetFileNameWithoutExtension(path)
            })
            {
                string key =
                    Normalize(
                        raw);

                if (string.IsNullOrWhiteSpace(
                        key))
                    continue;

                if (!result.TryGetValue(
                        key,
                        out List<Material> list))
                {
                    list =
                        new List<Material>();

                    result.Add(
                        key,
                        list);
                }

                if (!list.Contains(
                        material))
                    list.Add(
                        material);
            }
        }

        return result;
    }

    private static Material FindBestSource(
        Dictionary<string, List<Material>> source,
        string name)
    {
        string normalized =
            Normalize(
                name);

        if (!source.TryGetValue(
                normalized,
                out List<Material> candidates))
            return null;

        return
            candidates
                .OrderByDescending(
                    material =>
                    {
                        string path =
                            AssetDatabase.GetAssetPath(
                                material);

                        int score =
                            0;

                        if (path.EndsWith(
                                ".mat",
                                StringComparison.OrdinalIgnoreCase))
                            score += 100;

                        if (path.IndexOf(
                                "/Textures/Materials/",
                                StringComparison.OrdinalIgnoreCase) >= 0)
                            score += 100;

                        if (path.EndsWith(
                                ".fbx",
                                StringComparison.OrdinalIgnoreCase))
                            score -= 50;

                        return score;
                    })
                .FirstOrDefault();
    }

    private static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
            return string.Empty;

        string text =
            value.Trim();

        if (text.StartsWith(
                "FCG_",
                StringComparison.OrdinalIgnoreCase))
        {
            text =
                text.Substring(4);
        }

        return new string(
            text
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
    }

    private static void CopyBaseTexture(
        Material source,
        Material target)
    {
        Texture texture =
            null;

        string sourceProperty =
            null;

        foreach (string property in new[]
        {
            "_BaseMap",
            "_MainTex",
            "_Albedo",
            "_AlbedoMap",
            "_Diffuse",
            "_DiffuseMap"
        })
        {
            if (!source.HasProperty(
                    property))
                continue;

            Texture candidate =
                source.GetTexture(
                    property);

            if (candidate == null)
                continue;

            texture =
                candidate;

            sourceProperty =
                property;

            break;
        }

        if (texture == null ||
            !target.HasProperty(
                "_BaseMap"))
            return;

        target.SetTexture(
            "_BaseMap",
            texture);

        target.SetTextureScale(
            "_BaseMap",
            source.GetTextureScale(
                sourceProperty));

        target.SetTextureOffset(
            "_BaseMap",
            source.GetTextureOffset(
                sourceProperty));
    }

    private static void CopyNormalTexture(
        Material source,
        Material target)
    {
        if (!target.HasProperty(
                "_BumpMap"))
            return;

        string property =
            source.HasProperty("_BumpMap")
                ? "_BumpMap"
                : source.HasProperty("_NormalMap")
                    ? "_NormalMap"
                    : null;

        if (property == null)
            return;

        Texture normal =
            source.GetTexture(
                property);

        if (normal == null)
            return;

        target.SetTexture(
            "_BumpMap",
            normal);

        target.EnableKeyword(
            "_NORMALMAP");
    }

    private static void CopyColor(
        Material source,
        Material target)
    {
        if (!target.HasProperty(
                "_BaseColor"))
            return;

        Color color =
            Color.white;

        if (source.HasProperty(
                "_BaseColor"))
        {
            color =
                source.GetColor(
                    "_BaseColor");
        }
        else if (source.HasProperty(
                     "_Color"))
        {
            color =
                source.GetColor(
                    "_Color");
        }

        target.SetColor(
            "_BaseColor",
            color);
    }

    private static void ConfigureOpaque(
        Material material)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.One);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 1f);

        material.DisableKeyword(
            "_SURFACE_TYPE_TRANSPARENT");

        material.DisableKeyword(
            "_ALPHATEST_ON");

        material.SetOverrideTag(
            "RenderType",
            "Opaque");

        material.renderQueue =
            (int)RenderQueue.Geometry;
    }

    private static void ConfigureCutout(
        Material material,
        float cutoff)
    {
        ConfigureOpaque(
            material);

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 1f);

        if (material.HasProperty("_Cutoff"))
            material.SetFloat("_Cutoff", cutoff);

        material.EnableKeyword(
            "_ALPHATEST_ON");

        material.SetOverrideTag(
            "RenderType",
            "TransparentCutout");

        material.renderQueue =
            (int)RenderQueue.AlphaTest;
    }

    private static void ConfigureTransparent(
        Material material)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.DisableKeyword(
            "_ALPHATEST_ON");

        material.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT");

        material.SetOverrideTag(
            "RenderType",
            "Transparent");

        material.renderQueue =
            (int)RenderQueue.Transparent;
    }
}
#endif
