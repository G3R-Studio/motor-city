#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FantasticCityGeneratorCriticalMaterialDiagnostic
{
    private const string GeneratedRoot =
        "Assets/Resources/MotorCity/Environment/FCGMaterials";

    private const string SourceRoot =
        "Assets/Fantastic City Generator";

    private const string OutputPath =
        "MotorCity_FCGCriticalMaterialReport.txt";

    private static readonly string[] Names =
    {
        "Atlas-1",
        "Grass-01",
        "Grass-Splat",
        "HighWay",
        "Wins",
        "Wins-02",
        "Glass-01",
        "WinGlass-01",
        "WinGlass-01-D",
        "WinGlass-03"
    };

    [MenuItem("Motor City/Fantastic City Generator/Diagnose Critical Material State")]
    public static void Diagnose()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Critical Material Diagnostic",
                "Останови Play Mode перед диагностикой.",
                "OK");
            return;
        }

        Dictionary<string, Material> generated =
            LoadMaterials(
                GeneratedRoot);

        Dictionary<string, Material> source =
            LoadMaterials(
                SourceRoot);

        var builder =
            new StringBuilder();

        builder.AppendLine(
            "Motor City — FCG critical material diagnostic");

        builder.AppendLine(
            $"Unity: {Application.unityVersion}");

        builder.AppendLine();

        foreach (string shortName in Names)
        {
            string generatedName =
                "FCG_" + shortName;

            builder.AppendLine(
                "==============================================================================");

            builder.AppendLine(
                generatedName);

            builder.AppendLine(
                "------------------------------------------------------------------------------");

            Material generatedMaterial =
                FindMaterial(
                    generated,
                    generatedName,
                    shortName);

            Material sourceMaterial =
                FindMaterial(
                    source,
                    shortName,
                    generatedName);

            AppendMaterial(
                builder,
                "GENERATED",
                generatedMaterial);

            AppendMaterial(
                builder,
                "SOURCE",
                sourceMaterial);

            builder.AppendLine();
        }

        File.WriteAllText(
            Path.GetFullPath(
                OutputPath),
            builder.ToString(),
            new UTF8Encoding(false));

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Motor City — Critical Material Diagnostic",
            "Готово.\n\n" +
            $"Отчёт: {OutputPath}\n\n" +
            "Он ничего не меняет. Пришли файл сюда.",
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

            if (material == null)
                continue;

            if (!string.IsNullOrWhiteSpace(
                    material.name) &&
                !result.ContainsKey(
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
        params string[] wanted)
    {
        foreach (string key in wanted)
        {
            if (materials.TryGetValue(
                    key,
                    out Material exact))
                return exact;
        }

        string[] normalizedWanted =
            wanted
                .Select(Normalize)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

        foreach (KeyValuePair<string, Material> pair in materials)
        {
            string normalized =
                Normalize(
                    pair.Key);

            if (normalizedWanted.Contains(
                    normalized))
                return pair.Value;
        }

        return null;
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

    private static void AppendMaterial(
        StringBuilder builder,
        string label,
        Material material)
    {
        if (material == null)
        {
            builder.AppendLine(
                $"{label}: <NOT FOUND>");

            return;
        }

        string path =
            AssetDatabase.GetAssetPath(
                material);

        builder.AppendLine(
            $"{label}: {path}");

        builder.AppendLine(
            $"  name={material.name}");

        builder.AppendLine(
            $"  shader={(material.shader != null ? material.shader.name : "<null>")}");

        builder.AppendLine(
            $"  renderQueue={material.renderQueue}");

        builder.AppendLine(
            $"  RenderTypeTag={material.GetTag("RenderType", false, "<none>")}");

        AppendFloat(
            builder,
            material,
            "_Surface");

        AppendFloat(
            builder,
            material,
            "_AlphaClip");

        AppendFloat(
            builder,
            material,
            "_Cutoff");

        AppendFloat(
            builder,
            material,
            "_SrcBlend");

        AppendFloat(
            builder,
            material,
            "_DstBlend");

        AppendFloat(
            builder,
            material,
            "_ZWrite");

        AppendFloat(
            builder,
            material,
            "_Cull");

        AppendColor(
            builder,
            material,
            "_BaseColor");

        AppendColor(
            builder,
            material,
            "_Color");

        builder.AppendLine(
            "  keywords=" +
            string.Join(
                ",",
                material.shaderKeywords ?? Array.Empty<string>()));

        string[] properties;

        try
        {
            properties =
                material.GetTexturePropertyNames();
        }
        catch
        {
            properties =
                Array.Empty<string>();
        }

        builder.AppendLine(
            $"  textureProperties={properties.Length}");

        foreach (string property in properties)
        {
            Texture texture = null;

            try
            {
                texture =
                    material.GetTexture(
                        property);
            }
            catch
            {
            }

            if (texture == null)
                continue;

            string texturePath =
                AssetDatabase.GetAssetPath(
                    texture);

            builder.AppendLine(
                $"    {property} => {texture.name} | {texturePath} | scale={material.GetTextureScale(property)} | offset={material.GetTextureOffset(property)}");

            TextureImporter importer =
                !string.IsNullOrWhiteSpace(
                    texturePath)
                    ? AssetImporter.GetAtPath(
                        texturePath) as TextureImporter
                    : null;

            if (importer != null)
            {
                bool hasAlpha =
                    false;

                try
                {
                    hasAlpha =
                        importer.DoesSourceTextureHaveAlpha();
                }
                catch
                {
                    hasAlpha =
                        importer.alphaSource !=
                        TextureImporterAlphaSource.None;
                }

                builder.AppendLine(
                    $"      alpha={hasAlpha}, alphaSource={importer.alphaSource}, sRGB={importer.sRGBTexture}");
            }
        }

        AppendSerializedTexEnvs(
            builder,
            material);
    }

    private static void AppendSerializedTexEnvs(
        StringBuilder builder,
        Material material)
    {
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
                "  serializedTexEnvs=<none>");

            return;
        }

        builder.AppendLine(
            $"  serializedTexEnvs={texEnvs.arraySize}");

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

            string texturePath =
                texture != null
                    ? AssetDatabase.GetAssetPath(
                        texture)
                    : "<null>";

            builder.AppendLine(
                $"    {first.stringValue} => {(texture != null ? texture.name : "<null>")} | {texturePath}");
        }
    }

    private static void AppendFloat(
        StringBuilder builder,
        Material material,
        string property)
    {
        if (!material.HasProperty(
                property))
        {
            builder.AppendLine(
                $"  {property}=<missing>");

            return;
        }

        builder.AppendLine(
            $"  {property}={material.GetFloat(property)}");
    }

    private static void AppendColor(
        StringBuilder builder,
        Material material,
        string property)
    {
        if (!material.HasProperty(
                property))
        {
            builder.AppendLine(
                $"  {property}=<missing>");

            return;
        }

        builder.AppendLine(
            $"  {property}={material.GetColor(property)}");
    }
}
#endif
