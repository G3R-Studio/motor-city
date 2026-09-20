#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class FantasticCityGeneratorTextureBindingDiagnostic
{
    private const string GeneratedRoot =
        "Assets/Resources/MotorCity/Environment/FCGMaterials";

    private const string SourceRoot =
        "Assets/Fantastic City Generator";

    private const string OutputPath =
        "MotorCity_FCGTextureBindingReport.txt";

    public static void Diagnose()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Texture Binding Diagnostic",
                "Останови Play Mode перед диагностикой.",
                "OK");
            return;
        }

        Dictionary<string, Material> generated =
            LoadMaterials(GeneratedRoot);

        Dictionary<string, List<Material>> source =
            LoadMaterialLists(SourceRoot);

        var builder =
            new StringBuilder();

        builder.AppendLine(
            "Motor City — FCG texture binding mismatch diagnostic");

        builder.AppendLine(
            $"Unity: {Application.unityVersion}");

        builder.AppendLine(
            $"Generated materials: {generated.Count}");

        builder.AppendLine();

        int generatedWithoutBase =
            0;

        int sourceHasBaseButGeneratedDoesNot =
            0;

        int baseTexturePathMismatch =
            0;

        int sourceCandidatesMissing =
            0;

        int alphaCutoutWithoutAlpha =
            0;

        var problemNames =
            new List<string>();

        foreach (Material generatedMaterial in
                 generated.Values
                     .Distinct()
                     .OrderBy(m => m.name))
        {
            string generatedPath =
                AssetDatabase.GetAssetPath(
                    generatedMaterial);

            string shortName =
                generatedMaterial.name.StartsWith(
                    "FCG_",
                    StringComparison.OrdinalIgnoreCase)
                    ? generatedMaterial.name.Substring(4)
                    : generatedMaterial.name;

            TextureInfo generatedBase =
                FindBestBaseTexture(
                    generatedMaterial);

            if (generatedBase.Texture == null)
                generatedWithoutBase++;

            List<Material> candidates =
                FindSourceCandidates(
                    source,
                    shortName);

            if (candidates.Count == 0)
                sourceCandidatesMissing++;

            Material bestSource =
                null;

            TextureInfo bestSourceTexture =
                default;

            int bestScore =
                int.MinValue;

            foreach (Material candidate in
                     candidates)
            {
                TextureInfo info =
                    FindBestBaseTexture(
                        candidate);

                int score =
                    ScoreSourceCandidate(
                        candidate,
                        info,
                        shortName);

                if (score <= bestScore)
                    continue;

                bestScore =
                    score;

                bestSource =
                    candidate;

                bestSourceTexture =
                    info;
            }

            bool sourceHasBase =
                bestSourceTexture.Texture != null;

            bool generatedHasBase =
                generatedBase.Texture != null;

            bool missingFromGenerated =
                sourceHasBase &&
                !generatedHasBase;

            bool pathMismatch =
                sourceHasBase &&
                generatedHasBase &&
                !string.Equals(
                    generatedBase.AssetPath,
                    bestSourceTexture.AssetPath,
                    StringComparison.OrdinalIgnoreCase);

            bool alphaProblem =
                IsAlphaCutout(
                    generatedMaterial) &&
                generatedHasBase &&
                !TextureHasAlpha(
                    generatedBase.AssetPath);

            if (missingFromGenerated)
                sourceHasBaseButGeneratedDoesNot++;

            if (pathMismatch)
                baseTexturePathMismatch++;

            if (alphaProblem)
                alphaCutoutWithoutAlpha++;

            bool problem =
                missingFromGenerated ||
                pathMismatch ||
                alphaProblem;

            if (problem)
                problemNames.Add(
                    generatedMaterial.name);

            builder.AppendLine(
                "==============================================================================");

            builder.AppendLine(
                generatedMaterial.name);

            builder.AppendLine(
                $"GENERATED material={generatedPath}");

            AppendMaterialState(
                builder,
                generatedMaterial,
                generatedBase);

            if (bestSource == null)
            {
                builder.AppendLine(
                    "SOURCE <NOT FOUND>");
            }
            else
            {
                builder.AppendLine(
                    $"SOURCE material={AssetDatabase.GetAssetPath(bestSource)}");

                AppendMaterialState(
                    builder,
                    bestSource,
                    bestSourceTexture);
            }

            builder.AppendLine(
                $"COMPARE sourceHasBase={sourceHasBase}, generatedHasBase={generatedHasBase}, " +
                $"missingFromGenerated={missingFromGenerated}, pathMismatch={pathMismatch}, " +
                $"alphaCutoutWithoutAlpha={alphaProblem}");

            builder.AppendLine();

            AppendSerializedTextureSlots(
                builder,
                "GENERATED SERIALIZED TEXENVS",
                generatedMaterial);

            if (bestSource != null)
            {
                AppendSerializedTextureSlots(
                    builder,
                    "SOURCE SERIALIZED TEXENVS",
                    bestSource);
            }

            builder.AppendLine();
        }

        builder.Insert(
            builder.ToString().IndexOf(
                Environment.NewLine + Environment.NewLine,
                StringComparison.Ordinal) +
            (Environment.NewLine + Environment.NewLine).Length,
            "SUMMARY" + Environment.NewLine +
            $"Generated without base texture: {generatedWithoutBase}" + Environment.NewLine +
            $"Source has base but generated does not: {sourceHasBaseButGeneratedDoesNot}" + Environment.NewLine +
            $"Generated/source base texture path mismatch: {baseTexturePathMismatch}" + Environment.NewLine +
            $"Source candidate missing: {sourceCandidatesMissing}" + Environment.NewLine +
            $"Alpha-cutout material whose base texture has no alpha: {alphaCutoutWithoutAlpha}" + Environment.NewLine +
            $"Problem material names: {string.Join(", ", problemNames)}" + Environment.NewLine +
            Environment.NewLine);

        File.WriteAllText(
            Path.GetFullPath(
                OutputPath),
            builder.ToString(),
            new UTF8Encoding(false));

        AssetDatabase.Refresh();

        Debug.Log(
            "Motor City: FCG texture binding diagnostic complete. " +
            $"generatedWithoutBase={generatedWithoutBase}, " +
            $"sourceHasBaseButGeneratedDoesNot={sourceHasBaseButGeneratedDoesNot}, " +
            $"baseTexturePathMismatch={baseTexturePathMismatch}, " +
            $"alphaCutoutWithoutAlpha={alphaCutoutWithoutAlpha}, " +
            $"report={OutputPath}");

        EditorUtility.DisplayDialog(
            "Motor City — Texture Binding Diagnostic",
            "Готово.\n\n" +
            $"Generated без BaseMap: {generatedWithoutBase}\n" +
            $"У source есть BaseMap, а у generated нет: {sourceHasBaseButGeneratedDoesNot}\n" +
            $"BaseMap не совпадает с source: {baseTexturePathMismatch}\n" +
            $"Alpha-cutout без alpha в текстуре: {alphaCutoutWithoutAlpha}\n\n" +
            $"Отчёт: {OutputPath}\n\n" +
            "Инструмент ничего не меняет.",
            "OK");
    }

    private static Dictionary<string, Material> LoadMaterials(
        string root)
    {
        var result =
            new Dictionary<string, Material>(
                StringComparer.OrdinalIgnoreCase);

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[] { root });

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

    private static Dictionary<string, List<Material>> LoadMaterialLists(
        string root)
    {
        var result =
            new Dictionary<string, List<Material>>(
                StringComparer.OrdinalIgnoreCase);

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[] { root });

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

            string[] keys =
            {
                material.name,
                Path.GetFileNameWithoutExtension(
                    path)
            };

            foreach (string rawKey in keys)
            {
                string key =
                    Normalize(
                        rawKey);

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

    private static List<Material> FindSourceCandidates(
        Dictionary<string, List<Material>> source,
        string name)
    {
        string normalized =
            Normalize(
                name);

        if (source.TryGetValue(
                normalized,
                out List<Material> exact))
        {
            return exact;
        }

        var result =
            new List<Material>();

        foreach (KeyValuePair<string, List<Material>> pair in
                 source)
        {
            if (pair.Key.Contains(
                    normalized) ||
                normalized.Contains(
                    pair.Key))
            {
                result.AddRange(
                    pair.Value);
            }
        }

        return result
            .Distinct()
            .ToList();
    }

    private static int ScoreSourceCandidate(
        Material candidate,
        TextureInfo info,
        string wantedName)
    {
        int score =
            0;

        string normalizedCandidate =
            Normalize(
                candidate.name);

        string normalizedWanted =
            Normalize(
                wantedName);

        if (normalizedCandidate ==
            normalizedWanted)
            score += 1000;

        if (info.Texture != null)
            score += 500;

        if (!string.IsNullOrWhiteSpace(
                info.AssetPath))
            score += 200;

        string path =
            AssetDatabase.GetAssetPath(
                candidate);

        if (path.IndexOf(
                "/Materials/",
                StringComparison.OrdinalIgnoreCase) >= 0)
            score += 100;

        if (path.EndsWith(
                ".mat",
                StringComparison.OrdinalIgnoreCase))
            score += 100;

        if (path.EndsWith(
                ".fbx",
                StringComparison.OrdinalIgnoreCase))
            score -= 25;

        return score;
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

    private static TextureInfo FindBestBaseTexture(
        Material material)
    {
        TextureInfo result =
            default;

        if (material == null)
            return result;

        string[] preferred =
        {
            "_BaseMap",
            "_MainTex",
            "_BaseColorMap",
            "_Albedo",
            "_AlbedoMap",
            "_Diffuse",
            "_DiffuseMap",
            "_ColorMap",
            "_Texture"
        };

        foreach (string property in
                 preferred)
        {
            if (!material.HasProperty(
                    property))
                continue;

            Texture texture =
                material.GetTexture(
                    property);

            if (texture == null)
                continue;

            return new TextureInfo
            {
                Property =
                    property,
                Texture =
                    texture,
                AssetPath =
                    AssetDatabase.GetAssetPath(
                        texture)
            };
        }

        SerializedObject serialized =
            new SerializedObject(
                material);

        SerializedProperty texEnvs =
            serialized.FindProperty(
                "m_SavedProperties.m_TexEnvs");

        if (texEnvs == null ||
            !texEnvs.isArray)
            return result;

        int bestScore =
            int.MinValue;

        for (int i = 0;
             i < texEnvs.arraySize;
             i++)
        {
            SerializedProperty entry =
                texEnvs.GetArrayElementAtIndex(
                    i);

            SerializedProperty first =
                entry.FindPropertyRelative(
                    "first");

            SerializedProperty second =
                entry.FindPropertyRelative(
                    "second");

            if (first == null ||
                second == null)
                continue;

            SerializedProperty textureProperty =
                second.FindPropertyRelative(
                    "m_Texture");

            Texture texture =
                textureProperty != null
                    ? textureProperty.objectReferenceValue as Texture
                    : null;

            if (texture == null)
                continue;

            string property =
                first.stringValue ??
                string.Empty;

            string lower =
                property.ToLowerInvariant();

            if (lower.Contains("normal") ||
                lower.Contains("bump") ||
                lower.Contains("mask") ||
                lower.Contains("metal") ||
                lower.Contains("spec") ||
                lower.Contains("occlusion") ||
                lower.Contains("emission"))
                continue;

            int score =
                0;

            if (lower == "_basemap")
                score += 100;

            if (lower == "_maintex")
                score += 90;

            if (lower.Contains("albedo"))
                score += 80;

            if (lower.Contains("diff"))
                score += 70;

            if (lower.Contains("base"))
                score += 60;

            if (score <= bestScore)
                continue;

            bestScore =
                score;

            result =
                new TextureInfo
                {
                    Property =
                        property,
                    Texture =
                        texture,
                    AssetPath =
                        AssetDatabase.GetAssetPath(
                            texture)
                };
        }

        return result;
    }

    private static void AppendMaterialState(
        StringBuilder builder,
        Material material,
        TextureInfo baseTexture)
    {
        builder.AppendLine(
            $"  shader={(material.shader != null ? material.shader.name : "<null>")}");

        builder.AppendLine(
            $"  renderQueue={material.renderQueue}");

        builder.AppendLine(
            $"  baseProperty={(string.IsNullOrWhiteSpace(baseTexture.Property) ? "<none>" : baseTexture.Property)}");

        builder.AppendLine(
            $"  baseTexture={(baseTexture.Texture != null ? baseTexture.Texture.name : "<null>")}");

        builder.AppendLine(
            $"  baseTexturePath={(string.IsNullOrWhiteSpace(baseTexture.AssetPath) ? "<missing>" : baseTexture.AssetPath)}");

        if (baseTexture.Texture != null &&
            !string.IsNullOrWhiteSpace(
                baseTexture.AssetPath))
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(
                    baseTexture.AssetPath) as TextureImporter;

            if (importer != null)
            {
                bool alpha =
                    false;

                try
                {
                    alpha =
                        importer.DoesSourceTextureHaveAlpha();
                }
                catch
                {
                    alpha =
                        importer.alphaSource !=
                        TextureImporterAlphaSource.None;
                }

                builder.AppendLine(
                    $"  textureAlpha={alpha}, alphaSource={importer.alphaSource}, sRGB={importer.sRGBTexture}");
            }
        }

        if (material.HasProperty(
                "_AlphaClip"))
        {
            builder.AppendLine(
                $"  _AlphaClip={material.GetFloat("_AlphaClip")}");
        }

        if (material.HasProperty(
                "_Cutoff"))
        {
            builder.AppendLine(
                $"  _Cutoff={material.GetFloat("_Cutoff")}");
        }

        if (material.HasProperty(
                "_Surface"))
        {
            builder.AppendLine(
                $"  _Surface={material.GetFloat("_Surface")}");
        }
    }

    private static void AppendSerializedTextureSlots(
        StringBuilder builder,
        string label,
        Material material)
    {
        builder.AppendLine(
            label);

        SerializedObject serialized =
            new SerializedObject(
                material);

        SerializedProperty texEnvs =
            serialized.FindProperty(
                "m_SavedProperties.m_TexEnvs");

        if (texEnvs == null ||
            !texEnvs.isArray)
        {
            builder.AppendLine(
                "  <none>");

            return;
        }

        for (int i = 0;
             i < texEnvs.arraySize;
             i++)
        {
            SerializedProperty entry =
                texEnvs.GetArrayElementAtIndex(
                    i);

            SerializedProperty first =
                entry.FindPropertyRelative(
                    "first");

            SerializedProperty second =
                entry.FindPropertyRelative(
                    "second");

            if (first == null ||
                second == null)
                continue;

            SerializedProperty textureProperty =
                second.FindPropertyRelative(
                    "m_Texture");

            Texture texture =
                textureProperty != null
                    ? textureProperty.objectReferenceValue as Texture
                    : null;

            builder.AppendLine(
                $"  {first.stringValue} => {(texture != null ? texture.name : "<null>")} | " +
                $"{(texture != null ? AssetDatabase.GetAssetPath(texture) : "<missing>")}");
        }
    }

    private static bool IsAlphaCutout(
        Material material)
    {
        return
            material != null &&
            ((material.HasProperty("_AlphaClip") &&
              material.GetFloat("_AlphaClip") > 0.5f) ||
             material.IsKeywordEnabled(
                 "_ALPHATEST_ON"));
    }

    private static bool TextureHasAlpha(
        string path)
    {
        if (string.IsNullOrWhiteSpace(
                path))
            return false;

        TextureImporter importer =
            AssetImporter.GetAtPath(
                path) as TextureImporter;

        if (importer == null)
            return false;

        try
        {
            return importer.DoesSourceTextureHaveAlpha();
        }
        catch
        {
            return importer.alphaSource !=
                TextureImporterAlphaSource.None;
        }
    }

    private struct TextureInfo
    {
        public string Property;
        public Texture Texture;
        public string AssetPath;
    }
}
#endif
