#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorMissingTextureDiagnostic
{
    private const string OutputPath =
        "MotorCity_FCGMissingTextureReport.txt";

    private const string GeneratedMaterialRoot =
        "Assets/Resources/MotorCity/Environment/FCGMaterials";

    private const string SourceMaterialRoot =
        "Assets/Fantastic City Generator";

    private static readonly Regex GuidRegex =
        new Regex(
            @"guid:\s*([0-9a-fA-F]{32})",
            RegexOptions.Compiled);

    [MenuItem("Motor City/Fantastic City Generator/Diagnose Missing Texture References")]
    public static void Diagnose()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Missing Texture Diagnostic",
                "Останови Play Mode перед диагностикой.",
                "OK");
            return;
        }

        if (!FantasticCityGeneratorSceneSource.TryOpenSourceScene(
                out Scene scene,
                out bool openedTemporarily,
                out Scene previousActiveScene))
        {
            EditorUtility.DisplayDialog(
                "Motor City — Missing Texture Diagnostic",
                "Не найдена сохранённая FCG-сцена в Assets/LocalGenerated.",
                "OK");
            return;
        }

        try
        {
            Dictionary<string, Material> generatedMaterials =
                LoadMaterials(
                    GeneratedMaterialRoot);

            Dictionary<string, Material> sourceMaterials =
                LoadMaterials(
                    SourceMaterialRoot);

            var builder =
                new StringBuilder();

            builder.AppendLine(
                "Motor City — FCG missing texture diagnostic");

            builder.AppendLine(
                $"Scene: {scene.path}");

            builder.AppendLine(
                $"Unity: {Application.unityVersion}");

            builder.AppendLine();

            int rendererCount =
                0;

            int rendererProblems =
                0;

            int materialSlots =
                0;

            int slotsWithMissingTexture =
                0;

            int generatedMaterialProblems =
                0;

            int unresolvedSerializedGuids =
                0;

            var affectedMaterialNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            var problemBlocks =
                new List<string>();

            foreach (GameObject root in
                     scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in
                         root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                        continue;

                    rendererCount++;

                    Material[] materials =
                        renderer.sharedMaterials ??
                        Array.Empty<Material>();

                    bool rendererHasProblem =
                        false;

                    var rendererBlock =
                        new StringBuilder();

                    string rendererPath =
                        GetHierarchyPath(
                            renderer.transform);

                    string meshName =
                        GetMeshName(
                            renderer);

                    for (int slot = 0;
                         slot < materials.Length;
                         slot++)
                    {
                        materialSlots++;

                        Material material =
                            materials[slot];

                        if (material == null)
                        {
                            rendererHasProblem =
                                true;

                            slotsWithMissingTexture++;

                            rendererBlock.AppendLine(
                                $"  slot {slot}: <NULL MATERIAL>");

                            continue;
                        }

                        Texture baseTexture =
                            GetBaseTexture(
                                material,
                                out string baseProperty);

                        string materialPath =
                            AssetDatabase.GetAssetPath(
                                material);

                        string texturePath =
                            baseTexture != null
                                ? AssetDatabase.GetAssetPath(
                                    baseTexture)
                                : string.Empty;

                        bool missingBaseTexture =
                            baseTexture == null ||
                            string.IsNullOrWhiteSpace(
                                texturePath);

                        SerializedMaterialInfo serialized =
                            InspectSerializedMaterial(
                                materialPath);

                        bool unresolvedSerialized =
                            serialized.UnresolvedGuids.Count > 0;

                        bool generated =
                            !string.IsNullOrWhiteSpace(
                                materialPath) &&
                            materialPath.Replace('\\', '/')
                                .StartsWith(
                                    GeneratedMaterialRoot + "/",
                                    StringComparison.OrdinalIgnoreCase);

                        if (!missingBaseTexture &&
                            !unresolvedSerialized)
                            continue;

                        rendererHasProblem =
                            true;

                        affectedMaterialNames.Add(
                            material.name);

                        if (missingBaseTexture)
                            slotsWithMissingTexture++;

                        if (generated)
                            generatedMaterialProblems++;

                        unresolvedSerializedGuids +=
                            serialized.UnresolvedGuids.Count;

                        rendererBlock.AppendLine(
                            $"  slot {slot}: {material.name}");

                        rendererBlock.AppendLine(
                            $"    materialPath={materialPath}");

                        rendererBlock.AppendLine(
                            $"    shader={(material.shader != null ? material.shader.name : "<null>")}");

                        rendererBlock.AppendLine(
                            $"    baseProperty={(string.IsNullOrWhiteSpace(baseProperty) ? "<none>" : baseProperty)}");

                        rendererBlock.AppendLine(
                            $"    baseTexture={(baseTexture != null ? baseTexture.name : "<null>")}");

                        rendererBlock.AppendLine(
                            $"    baseTexturePath={(string.IsNullOrWhiteSpace(texturePath) ? "<missing>" : texturePath)}");

                        rendererBlock.AppendLine(
                            $"    serializedTextureGuids={serialized.TextureGuids.Count}");

                        if (serialized.UnresolvedGuids.Count > 0)
                        {
                            rendererBlock.AppendLine(
                                "    UNRESOLVED serialized GUIDs:");

                            foreach (string guid in
                                     serialized.UnresolvedGuids)
                            {
                                rendererBlock.AppendLine(
                                    $"      {guid}");
                            }
                        }

                        Material matchingGenerated =
                            FindMaterial(
                                generatedMaterials,
                                material.name);

                        Material matchingSource =
                            FindSourceMaterial(
                                sourceMaterials,
                                material.name);

                        if (matchingGenerated != null &&
                            matchingGenerated != material)
                        {
                            AppendComparison(
                                rendererBlock,
                                "generated-match",
                                matchingGenerated);
                        }

                        if (matchingSource != null)
                        {
                            AppendComparison(
                                rendererBlock,
                                "source-match",
                                matchingSource);
                        }
                    }

                    if (!rendererHasProblem)
                        continue;

                    rendererProblems++;

                    problemBlocks.Add(
                        $"RENDERER {rendererPath}\n" +
                        $"  mesh={meshName}\n" +
                        $"  enabled={renderer.enabled}, active={renderer.gameObject.activeInHierarchy}\n" +
                        $"  bounds={renderer.bounds}\n" +
                        rendererBlock);
                }
            }

            builder.AppendLine(
                "SUMMARY");

            builder.AppendLine(
                $"Renderers scanned: {rendererCount}");

            builder.AppendLine(
                $"Renderer problems: {rendererProblems}");

            builder.AppendLine(
                $"Material slots scanned: {materialSlots}");

            builder.AppendLine(
                $"Slots with missing/unresolvable base texture: {slotsWithMissingTexture}");

            builder.AppendLine(
                $"Generated material problems: {generatedMaterialProblems}");

            builder.AppendLine(
                $"Unresolved serialized GUID references: {unresolvedSerializedGuids}");

            builder.AppendLine(
                $"Affected material names: {affectedMaterialNames.Count}");

            builder.AppendLine(
                string.Join(
                    ", ",
                    affectedMaterialNames.OrderBy(x => x)));

            builder.AppendLine();

            builder.AppendLine(
                "=== PROBLEM RENDERERS ===");

            foreach (string block in problemBlocks)
            {
                builder.AppendLine();
                builder.AppendLine(block);
            }

            File.WriteAllText(
                Path.GetFullPath(
                    OutputPath),
                builder.ToString(),
                new UTF8Encoding(false));

            AssetDatabase.Refresh();

            Debug.Log(
                "Motor City: FCG missing texture diagnostic complete. " +
                $"Renderers={rendererCount}, rendererProblems={rendererProblems}, " +
                $"missingBaseTextureSlots={slotsWithMissingTexture}, " +
                $"generatedMaterialProblems={generatedMaterialProblems}, " +
                $"unresolvedSerializedGuids={unresolvedSerializedGuids}, " +
                $"report={OutputPath}");

            EditorUtility.DisplayDialog(
                "Motor City — Missing Texture Diagnostic",
                "Готово.\n\n" +
                $"Renderer'ов проверено: {rendererCount}\n" +
                $"Проблемных Renderer'ов: {rendererProblems}\n" +
                $"Слотов без рабочей BaseMap: {slotsWithMissingTexture}\n" +
                $"Проблемных generated-материалов: {generatedMaterialProblems}\n" +
                $"Неразрешимых GUID-ссылок в .mat: {unresolvedSerializedGuids}\n\n" +
                $"Отчёт: {OutputPath}\n\n" +
                "Этот инструмент ничего не меняет.",
                "OK");
        }
        finally
        {
            FantasticCityGeneratorSceneSource.FinishSourceScene(
                scene,
                openedTemporarily,
                previousActiveScene,
                false);
        }
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

            string fileName =
                Path.GetFileNameWithoutExtension(
                    path);

            if (!string.IsNullOrWhiteSpace(
                    fileName) &&
                !result.ContainsKey(
                    fileName))
            {
                result.Add(
                    fileName,
                    material);
            }
        }

        return result;
    }

    private static Material FindMaterial(
        Dictionary<string, Material> materials,
        string name)
    {
        if (materials.TryGetValue(
                name,
                out Material exact))
            return exact;

        string normalized =
            Normalize(
                name);

        foreach (KeyValuePair<string, Material> pair in
                 materials)
        {
            if (Normalize(pair.Key) ==
                normalized)
                return pair.Value;
        }

        return null;
    }

    private static Material FindSourceMaterial(
        Dictionary<string, Material> materials,
        string generatedName)
    {
        string shortName =
            generatedName.StartsWith(
                "FCG_",
                StringComparison.OrdinalIgnoreCase)
                ? generatedName.Substring(4)
                : generatedName;

        Material exact =
            FindMaterial(
                materials,
                shortName);

        if (exact != null)
            return exact;

        return
            FindMaterial(
                materials,
                generatedName);
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

    private static Texture GetBaseTexture(
        Material material,
        out string property)
    {
        property =
            string.Empty;

        if (material == null)
            return null;

        foreach (string candidate in new[]
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
        })
        {
            if (!material.HasProperty(
                    candidate))
                continue;

            Texture texture =
                material.GetTexture(
                    candidate);

            if (texture == null)
                continue;

            property =
                candidate;

            return texture;
        }

        try
        {
            foreach (string candidate in
                     material.GetTexturePropertyNames())
            {
                Texture texture =
                    null;

                try
                {
                    texture =
                        material.GetTexture(
                            candidate);
                }
                catch
                {
                }

                if (texture == null)
                    continue;

                string lower =
                    candidate.ToLowerInvariant();

                if (lower.Contains("normal") ||
                    lower.Contains("bump") ||
                    lower.Contains("mask") ||
                    lower.Contains("metal") ||
                    lower.Contains("spec") ||
                    lower.Contains("occlusion") ||
                    lower.Contains("emission"))
                    continue;

                property =
                    candidate;

                return texture;
            }
        }
        catch
        {
        }

        return null;
    }

    private static SerializedMaterialInfo InspectSerializedMaterial(
        string path)
    {
        var result =
            new SerializedMaterialInfo();

        if (string.IsNullOrWhiteSpace(
                path) ||
            !File.Exists(
                Path.GetFullPath(
                    path)))
            return result;

        string text =
            File.ReadAllText(
                Path.GetFullPath(
                    path));

        foreach (Match match in
                 GuidRegex.Matches(
                     text))
        {
            string guid =
                match.Groups[1].Value;

            if (result.TextureGuids.Contains(
                    guid))
                continue;

            string assetPath =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (string.IsNullOrWhiteSpace(
                    assetPath))
            {
                result.UnresolvedGuids.Add(
                    guid);
                continue;
            }

            Type type =
                AssetDatabase.GetMainAssetTypeAtPath(
                    assetPath);

            if (type == typeof(Texture2D) ||
                type == typeof(Texture) ||
                typeof(Texture).IsAssignableFrom(
                    type))
            {
                result.TextureGuids.Add(
                    guid);
            }
        }

        return result;
    }

    private static void AppendComparison(
        StringBuilder builder,
        string label,
        Material material)
    {
        Texture texture =
            GetBaseTexture(
                material,
                out string property);

        string path =
            AssetDatabase.GetAssetPath(
                material);

        string texturePath =
            texture != null
                ? AssetDatabase.GetAssetPath(
                    texture)
                : string.Empty;

        builder.AppendLine(
            $"    {label}: {material.name}");

        builder.AppendLine(
            $"      materialPath={path}");

        builder.AppendLine(
            $"      shader={(material.shader != null ? material.shader.name : "<null>")}");

        builder.AppendLine(
            $"      baseProperty={(string.IsNullOrWhiteSpace(property) ? "<none>" : property)}");

        builder.AppendLine(
            $"      baseTexture={(texture != null ? texture.name : "<null>")}");

        builder.AppendLine(
            $"      baseTexturePath={(string.IsNullOrWhiteSpace(texturePath) ? "<missing>" : texturePath)}");
    }

    private static string GetMeshName(
        Renderer renderer)
    {
        MeshFilter filter =
            renderer.GetComponent<MeshFilter>();

        if (filter != null &&
            filter.sharedMesh != null)
            return filter.sharedMesh.name;

        SkinnedMeshRenderer skinned =
            renderer as SkinnedMeshRenderer;

        return skinned != null &&
               skinned.sharedMesh != null
            ? skinned.sharedMesh.name
            : string.Empty;
    }

    private static string GetHierarchyPath(
        Transform item)
    {
        string path =
            item.name;

        Transform current =
            item.parent;

        while (current != null)
        {
            path =
                current.name +
                "/" +
                path;

            current =
                current.parent;
        }

        return path;
    }

    private sealed class SerializedMaterialInfo
    {
        public readonly List<string> TextureGuids =
            new List<string>();

        public readonly List<string> UnresolvedGuids =
            new List<string>();
    }
}
#endif
