using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Reports source file sizes for building FBX assets and their referenced textures.
/// This is a read-only audit; it does not modify import settings or assets.
/// </summary>
public static class CityBuildingAssetSizeAudit
{
    private const string BuildingFolder =
        "Assets/Fantastic City Generator/Buildings/FBX/";

    private sealed class AssetRow
    {
        public string Type;
        public string Path;
        public long Bytes;
        public string Details;
    }

    [MenuItem("Tools/Motor City/Audit Building File Sizes")]
    private static void RunAudit()
    {
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { BuildingFolder });
        if (fbxGuids == null || fbxGuids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Building Asset Size Audit",
                "No model assets found in:\n" + BuildingFolder,
                "OK");
            return;
        }

        Dictionary<string, AssetRow> rows =
            new Dictionary<string, AssetRow>(StringComparer.OrdinalIgnoreCase);

        try
        {
            for (int i = 0; i < fbxGuids.Length; i++)
            {
                string fbxPath = AssetDatabase.GUIDToAssetPath(fbxGuids[i]);
                EditorUtility.DisplayProgressBar(
                    "Scanning building assets",
                    "Checking " + fbxPath,
                    (float)i / Math.Max(1, fbxGuids.Length));

                AddFileRow(rows, fbxPath, "FBX", "Source building model");

                string[] dependencies = AssetDatabase.GetDependencies(fbxPath, true);
                foreach (string dependency in dependencies)
                {
                    if (string.Equals(dependency, fbxPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!IsTexturePath(dependency))
                        continue;

                    AddTextureRow(rows, dependency);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        List<AssetRow> sorted = rows.Values
            .OrderByDescending(row => row.Bytes)
            .ThenBy(row => row.Type)
            .ToList();

        string outputPath = EditorUtility.SaveFilePanel(
            "Save Building Asset Size Audit",
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "MotorCity_Building_Asset_Sizes",
            "csv");

        if (string.IsNullOrEmpty(outputPath))
            return;

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Rank,Type,Size Bytes,Size MiB,Asset Path,Details");

        long fbxBytes = 0;
        long textureBytes = 0;
        int fbxCount = 0;
        int textureCount = 0;

        for (int i = 0; i < sorted.Count; i++)
        {
            AssetRow row = sorted[i];
            if (row.Type == "FBX")
            {
                fbxBytes += row.Bytes;
                fbxCount++;
            }
            else if (row.Type == "Texture")
            {
                textureBytes += row.Bytes;
                textureCount++;
            }

            csv.Append(i + 1).Append(',')
                .Append(Csv(row.Type)).Append(',')
                .Append(row.Bytes).Append(',')
                .Append((row.Bytes / 1048576.0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                .Append(Csv(row.Path)).Append(',')
                .Append(Csv(row.Details))
                .AppendLine();
        }

        csv.AppendLine();
        csv.AppendLine("SUMMARY,Count,Total Bytes,Total MiB");
        csv.Append("FBX,").Append(fbxCount).Append(',').Append(fbxBytes).Append(',')
            .Append((fbxBytes / 1048576.0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
        csv.Append("Referenced Textures,").Append(textureCount).Append(',').Append(textureBytes).Append(',')
            .Append((textureBytes / 1048576.0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture)).AppendLine();

        File.WriteAllText(outputPath, csv.ToString(), new UTF8Encoding(true));

        Debug.Log(
            "[Building Asset Size Audit] Complete.\n" +
            "FBX files: " + fbxCount + " (" + (fbxBytes / 1048576.0).ToString("F2") + " MiB)\n" +
            "Unique referenced textures: " + textureCount + " (" + (textureBytes / 1048576.0).ToString("F2") + " MiB)\n" +
            "CSV: " + outputPath);

        EditorUtility.DisplayDialog(
            "Building Asset Size Audit Complete",
            "FBX files: " + fbxCount + " (" + (fbxBytes / 1048576.0).ToString("F2") + " MiB)" +
            "\nUnique referenced textures: " + textureCount +
            " (" + (textureBytes / 1048576.0).ToString("F2") + " MiB)" +
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

    private static void AddFileRow(
        Dictionary<string, AssetRow> rows,
        string assetPath,
        string type,
        string details)
    {
        string absolutePath = Path.GetFullPath(assetPath);
        if (!File.Exists(absolutePath))
            return;

        rows[assetPath] = new AssetRow
        {
            Type = type,
            Path = assetPath,
            Bytes = new FileInfo(absolutePath).Length,
            Details = details
        };
    }

    private static void AddTextureRow(
        Dictionary<string, AssetRow> rows,
        string assetPath)
    {
        if (rows.ContainsKey(assetPath))
            return;

        string absolutePath = Path.GetFullPath(assetPath);
        if (!File.Exists(absolutePath))
            return;

        Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
        string dimensions = texture != null
            ? texture.width + "x" + texture.height
            : "Dimensions unavailable";

        rows[assetPath] = new AssetRow
        {
            Type = "Texture",
            Path = assetPath,
            Bytes = new FileInfo(absolutePath).Length,
            Details = dimensions + "; source file size, not compressed build size"
        };
    }

    private static string Csv(string value)
    {
        value = value ?? string.Empty;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
