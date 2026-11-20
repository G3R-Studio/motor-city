using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Read-only source-asset audit for the CityVisual prefab and its transitive
/// dependencies. Reports source file sizes plus mesh/texture characteristics.
/// It does not modify assets or claim per-object compressed AssetBundle sizes.
/// </summary>
public static class MotorCityBundleContentAudit
{
    private const string CityPrefabPath =
        "Assets/Resources/MotorCity/Environment/CityVisual.prefab";

    private sealed class Row
    {
        public string Path;
        public string Type;
        public long Bytes;
        public int Vertices;
        public long Triangles;
        public int Width;
        public int Height;
        public string Details;
    }

    [MenuItem("Tools/Motor City/Build/Audit City Bundle Source Contents")]
    private static void Run()
    {
        if (!File.Exists(CityPrefabPath))
        {
            EditorUtility.DisplayDialog("City Bundle Content Audit",
                "City prefab was not found at:\n" + CityPrefabPath +
                "\n\nIf a bundle experiment has temporarily moved it, restore the city first.",
                "OK");
            return;
        }

        string[] dependencies = AssetDatabase.GetDependencies(CityPrefabPath, true)
            .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
                        !AssetDatabase.IsValidFolder(p))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        var rows = new List<Row>();
        try
        {
            for (int i = 0; i < dependencies.Length; i++)
            {
                string path = dependencies[i];
                EditorUtility.DisplayProgressBar("Auditing CityVisual dependencies",
                    path, (float)i / Math.Max(1, dependencies.Length));

                string absolute = Path.GetFullPath(path);
                long bytes = File.Exists(absolute) ? new FileInfo(absolute).Length : 0;
                UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                bool firstRow = true;
                foreach (UnityEngine.Object asset in subAssets)
                {
                    if (asset == null) continue;
                    var row = new Row
                    {
                        Path = path,
                        Type = asset.GetType().Name,
                        // A source file can contain many imported sub-assets (e.g. FBX meshes).
                        // Count its disk size only once to avoid inflating the total.
                        Bytes = firstRow ? bytes : 0
                    };
                    firstRow = false;

                    Mesh mesh = asset as Mesh;
                    Texture texture = asset as Texture;
                    if (mesh != null)
                    {
                        row.Vertices = mesh.vertexCount;
                        long triangles = 0;
                        for (int s = 0; s < mesh.subMeshCount; s++)
                            if (mesh.GetTopology(s) == MeshTopology.Triangles)
                                triangles += (long)mesh.GetIndexCount(s) / 3L;
                        row.Triangles = triangles;
                        row.Details = "SubAsset=" + asset.name +
                            "; SubMeshes=" + mesh.subMeshCount +
                            "; IndexFormat=" + mesh.indexFormat;
                    }
                    else if (texture != null)
                    {
                        row.Width = texture.width;
                        row.Height = texture.height;
                        row.Details = "SubAsset=" + asset.name;
                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (importer != null)
                        {
                            var webgl = importer.GetPlatformTextureSettings("WebGL");
                            row.Details += "; TextureType=" + importer.textureType +
                                "; WebGLOverride=" + (webgl != null && webgl.overridden) +
                                "; WebGLMaxSize=" + (webgl != null && webgl.overridden
                                    ? webgl.maxTextureSize : importer.maxTextureSize) +
                                "; WebGLCompression=" + (webgl != null && webgl.overridden
                                    ? webgl.textureCompression : importer.textureCompression) +
                                "; Crunch=" + (webgl != null && webgl.overridden &&
                                    webgl.crunchedCompression);
                        }
                    }
                    else
                    {
                        row.Details = "SubAsset=" + asset.name;
                    }
                    rows.Add(row);
                }
                if (firstRow)
                    rows.Add(new Row { Path = path, Type = "Unknown", Bytes = bytes });

            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        string output = EditorUtility.SaveFilePanel(
            "Save City Bundle Content Audit",
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "MotorCity_CityBundle_Content_Audit",
            "csv");
        if (string.IsNullOrEmpty(output))
            return;

        var csv = new StringBuilder();
        csv.AppendLine("AssetPath,Type,SourceBytes,SourceMiB,Vertices,Triangles,Width,Height,Details");
        foreach (Row r in rows.OrderByDescending(x => x.Bytes)
                              .ThenByDescending(x => x.Triangles))
        {
            csv.Append(Csv(r.Path)).Append(',')
                .Append(Csv(r.Type)).Append(',')
                .Append(r.Bytes).Append(',')
                .Append((r.Bytes / 1048576.0).ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(r.Vertices).Append(',')
                .Append(r.Triangles).Append(',')
                .Append(r.Width).Append(',')
                .Append(r.Height).Append(',')
                .Append(Csv(r.Details)).AppendLine();
        }

        File.WriteAllText(output, csv.ToString(), new UTF8Encoding(true));

        long totalSourceBytes = rows
            .GroupBy(r => r.Path, StringComparer.Ordinal)
            .Sum(g => g.Max(r => r.Bytes));
        int meshCount = rows.Count(r => r.Type == "Mesh");
        int textureCount = rows.Count(r =>
            r.Type == "Texture2D" || r.Type == "Cubemap" || r.Type == "Texture");

        Debug.Log("[Motor City City Bundle Content Audit]\n" +
            "City prefab: " + CityPrefabPath + "\n" +
            "Dependency files: " + dependencies.Length + "\n" +
            "Mesh sub-assets: " + meshCount + "\n" +
            "Texture sub-assets: " + textureCount + "\n" +
            "Unique dependency source size: " +
            (totalSourceBytes / 1048576.0).ToString("F2", CultureInfo.InvariantCulture) +
            " MiB\nCSV: " + output);

        EditorUtility.DisplayDialog("City Bundle Content Audit Complete",
            "Dependency files scanned: " + dependencies.Length +
            "\nMesh sub-assets: " + meshCount +
            "\nTexture sub-assets: " + textureCount +
            "\nUnique source file size: " +
            (totalSourceBytes / 1048576.0).ToString("F2", CultureInfo.InvariantCulture) +
            " MiB\n\nReport saved to:\n" + output +
            "\n\nSource file sizes are not compressed AssetBundle contribution sizes.",
            "OK");
    }

    private static string Csv(string value)
    {
        value = value ?? string.Empty;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
