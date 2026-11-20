using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Read-only audit of WebGL texture import settings for textures referenced by
/// Fantastic City Generator building FBX assets.
/// Does not modify texture import settings or source files.
/// </summary>
public static class CityBuildingWebGlTextureAudit
{
    private const string BuildingFolder =
        "Assets/Fantastic City Generator/Buildings/FBX/";

    [MenuItem("Tools/Motor City/Audit Building WebGL Texture Settings")]
    private static void RunAudit()
    {
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { BuildingFolder });
        HashSet<string> texturePaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            for (int i = 0; i < modelGuids.Length; i++)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(modelGuids[i]);
                EditorUtility.DisplayProgressBar(
                    "Collecting building texture dependencies",
                    modelPath,
                    (float)i / Math.Max(1, modelGuids.Length));

                foreach (string dependency in AssetDatabase.GetDependencies(modelPath, true))
                {
                    if (IsTexturePath(dependency))
                        texturePaths.Add(dependency.Replace('\\', '/'));
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (texturePaths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "WebGL Texture Audit",
                "No referenced textures were found for building FBX assets.",
                "OK");
            return;
        }

        List<string> sortedPaths = texturePaths.OrderBy(path => path).ToList();
        StringBuilder csv = new StringBuilder();
        csv.AppendLine(
            "Texture,Source Bytes,Source MiB,Width,Height,Importer Type," +
            "Default Max Size,Default Compression,Default Format," +
            "WebGL Override,WebGL Max Size,WebGL Compression,WebGL Format," +
            "WebGL Crunch,WebGL Quality,Asset Path");

        long totalSourceBytes = 0;
        int missingImporterCount = 0;

        for (int i = 0; i < sortedPaths.Count; i++)
        {
            string path = sortedPaths[i];
            EditorUtility.DisplayProgressBar(
                "Auditing WebGL texture settings",
                path,
                (float)i / Math.Max(1, sortedPaths.Count));

            string absolutePath = Path.GetFullPath(path);
            long sourceBytes = File.Exists(absolutePath)
                ? new FileInfo(absolutePath).Length
                : 0;
            totalSourceBytes += sourceBytes;

            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            int width = texture != null ? texture.width : 0;
            int height = texture != null ? texture.height : 0;

            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                missingImporterCount++;
                csv.Append(Csv(Path.GetFileName(path))).Append(',')
                    .Append(sourceBytes).Append(',')
                    .Append(MiB(sourceBytes)).Append(',')
                    .Append(width).Append(',')
                    .Append(height).Append(',')
                    .Append("No TextureImporter").Append(',')
                    .Append(",,,,,,,,")
                    .Append(Csv(path)).AppendLine();
                continue;
            }

            TextureImporterPlatformSettings defaultSettings =
                importer.GetDefaultPlatformTextureSettings();
            TextureImporterPlatformSettings webGlSettings =
                importer.GetPlatformTextureSettings("WebGL");

            bool hasOverride = webGlSettings != null && webGlSettings.overridden;
            TextureImporterPlatformSettings effectiveSettings =
                hasOverride ? webGlSettings : defaultSettings;

            csv.Append(Csv(Path.GetFileName(path))).Append(',')
                .Append(sourceBytes).Append(',')
                .Append(MiB(sourceBytes)).Append(',')
                .Append(width).Append(',')
                .Append(height).Append(',')
                .Append(Csv(importer.textureType.ToString())).Append(',')
                .Append(defaultSettings.maxTextureSize).Append(',')
                .Append(Csv(defaultSettings.textureCompression.ToString())).Append(',')
                .Append(Csv(defaultSettings.format.ToString())).Append(',')
                .Append(hasOverride ? "Yes" : "No").Append(',')
                .Append(effectiveSettings.maxTextureSize).Append(',')
                .Append(Csv(effectiveSettings.textureCompression.ToString())).Append(',')
                .Append(Csv(effectiveSettings.format.ToString())).Append(',')
                .Append(effectiveSettings.crunchedCompression ? "Yes" : "No").Append(',')
                .Append(effectiveSettings.compressionQuality).Append(',')
                .Append(Csv(path))
                .AppendLine();
        }

        EditorUtility.ClearProgressBar();

        string outputPath = EditorUtility.SaveFilePanel(
            "Save WebGL Texture Settings Audit",
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "MotorCity_WebGL_Texture_Settings",
            "csv");

        if (string.IsNullOrEmpty(outputPath))
            return;

        File.WriteAllText(outputPath, csv.ToString(), new UTF8Encoding(true));

        Debug.Log(
            "[Motor City WebGL Texture Audit] Complete.\n" +
            "Building FBX models scanned: " + modelGuids.Length + "\n" +
            "Unique referenced textures: " + texturePaths.Count + "\n" +
            "Total source texture size: " + (totalSourceBytes / 1048576.0).ToString("F2") + " MiB\n" +
            "Textures without TextureImporter: " + missingImporterCount + "\n" +
            "CSV: " + outputPath);

        EditorUtility.DisplayDialog(
            "WebGL Texture Audit Complete",
            "Building FBX models scanned: " + modelGuids.Length +
            "\nUnique referenced textures: " + texturePaths.Count +
            "\nTotal source texture size: " + (totalSourceBytes / 1048576.0).ToString("F2") + " MiB" +
            "\n\nReport saved to:\n" + outputPath,
            "OK");
    }

    private static bool IsTexturePath(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension == ".png" || extension == ".jpg" ||
               extension == ".jpeg" || extension == ".tga" ||
               extension == ".tif" || extension == ".tiff" ||
               extension == ".psd" || extension == ".exr" ||
               extension == ".bmp" || extension == ".gif";
    }

    private static string MiB(long bytes)
    {
        return (bytes / 1048576.0).ToString(
            "F3", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Csv(string value)
    {
        value = value ?? string.Empty;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
