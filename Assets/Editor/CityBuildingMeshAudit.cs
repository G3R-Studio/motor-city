using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Audits building meshes used by the selected CityVisual hierarchy.
/// Only meshes imported from Fantastic City Generator/Buildings/FBX are included.
/// Collider assets are excluded.
/// </summary>
public static class CityBuildingMeshAudit
{
    private const string BuildingFbxFolder =
        "Assets/Fantastic City Generator/Buildings/FBX/";

    private sealed class MeshInfo
    {
        public Mesh Mesh;
        public string AssetPath;
        public int Vertices;
        public long TrianglesPerInstance;
        public int Instances;
        public long TotalTriangles;
        public readonly HashSet<string> ObjectNames = new HashSet<string>();
    }

    [MenuItem("Tools/Motor City/Audit Building Meshes")]
    private static void AuditSelectedCity()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "City Building Mesh Audit",
                "Select the CityVisual root object in the Hierarchy, then run the audit again.",
                "OK");
            return;
        }

        Dictionary<Mesh, MeshInfo> results = new Dictionary<Mesh, MeshInfo>();

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in meshFilters)
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            if (!IsBuildingMesh(filter.sharedMesh))
                continue;

            AddMesh(results, filter.sharedMesh, GetHierarchyPath(filter.transform, root.transform));
        }

        SkinnedMeshRenderer[] skinnedRenderers =
            root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
        {
            if (renderer == null || renderer.sharedMesh == null)
                continue;

            if (!IsBuildingMesh(renderer.sharedMesh))
                continue;

            AddMesh(results, renderer.sharedMesh, GetHierarchyPath(renderer.transform, root.transform));
        }

        List<MeshInfo> sorted = results.Values
            .OrderByDescending(info => info.TotalTriangles)
            .ThenByDescending(info => info.TrianglesPerInstance)
            .ToList();

        if (sorted.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "City Building Mesh Audit",
                "No building meshes were found. Select the CityVisual root and verify that building FBX assets are located in:\n" +
                BuildingFbxFolder,
                "OK");
            return;
        }

        string outputPath = EditorUtility.SaveFilePanel(
            "Save City Building Mesh Audit",
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "MotorCity_Building_Mesh_Audit",
            "csv");

        if (string.IsNullOrEmpty(outputPath))
            return;

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Rank,Mesh,Source FBX,Vertices,Triangles Per Instance,Instances,Total Triangles,Scene Objects");

        long uniqueTriangles = 0;
        long totalTriangles = 0;

        for (int i = 0; i < sorted.Count; i++)
        {
            MeshInfo info = sorted[i];
            uniqueTriangles += info.TrianglesPerInstance;
            totalTriangles += info.TotalTriangles;

            csv.Append(i + 1).Append(',')
                .Append(Csv(info.Mesh.name)).Append(',')
                .Append(Csv(info.AssetPath)).Append(',')
                .Append(info.Vertices).Append(',')
                .Append(info.TrianglesPerInstance).Append(',')
                .Append(info.Instances).Append(',')
                .Append(info.TotalTriangles).Append(',')
                .Append(Csv(string.Join(" | ", info.ObjectNames.OrderBy(name => name))))
                .AppendLine();
        }

        File.WriteAllText(outputPath, csv.ToString(), new UTF8Encoding(true));

        Debug.Log(
            "[City Building Mesh Audit] Complete.\n" +
            "Root: " + root.name + "\n" +
            "Unique building meshes: " + results.Count + "\n" +
            "Unique-mesh triangles (sum): " + uniqueTriangles + "\n" +
            "Triangles across scene instances: " + totalTriangles + "\n" +
            "CSV: " + outputPath);

        EditorUtility.DisplayDialog(
            "City Building Mesh Audit Complete",
            "Unique building meshes: " + results.Count +
            "\nUnique-mesh triangles (sum): " + uniqueTriangles +
            "\nTriangles across scene instances: " + totalTriangles +
            "\n\nCSV saved to:\n" + outputPath,
            "OK");
    }

    private static bool IsBuildingMesh(Mesh mesh)
    {
        string assetPath = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(assetPath))
            return false;

        assetPath = assetPath.Replace('\\', '/');

        if (!assetPath.StartsWith(BuildingFbxFolder, StringComparison.OrdinalIgnoreCase))
            return false;

        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        if (fileName.IndexOf("collider", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        if (mesh.name.IndexOf("collider", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        return true;
    }

    private static void AddMesh(
        Dictionary<Mesh, MeshInfo> results,
        Mesh mesh,
        string hierarchyPath)
    {
        if (!results.TryGetValue(mesh, out MeshInfo info))
        {
            long triangleCount = 0;

            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (mesh.GetTopology(subMesh) == MeshTopology.Triangles)
                    triangleCount += (long)mesh.GetIndexCount(subMesh) / 3;
            }

            info = new MeshInfo
            {
                Mesh = mesh,
                AssetPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/'),
                Vertices = mesh.vertexCount,
                TrianglesPerInstance = triangleCount
            };

            results.Add(mesh, info);
        }

        info.Instances++;
        info.TotalTriangles += info.TrianglesPerInstance;
        info.ObjectNames.Add(hierarchyPath);
    }

    private static string GetHierarchyPath(Transform target, Transform root)
    {
        List<string> parts = new List<string>();
        Transform current = target;

        while (current != null)
        {
            parts.Add(current.name);
            if (current == root)
                break;

            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static string Csv(string value)
    {
        value = value ?? string.Empty;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
