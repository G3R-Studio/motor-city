using System;
using UnityEditor;
using UnityEngine;

public static class StandardVehicleImportUtility
{
    public static bool UsesStandardBodyLayout(
        GameObject root)
    {
        if (root == null)
            return false;

        bool hasBody =
            false;

        bool hasBodyMisc =
            false;

        foreach (Renderer renderer in
                 root.GetComponentsInChildren<Renderer>(
                     true))
        {
            if (renderer == null)
                continue;

            string rendererName =
                Normalize(
                    renderer.name);

            string meshName =
                Normalize(
                    ResolveMeshName(
                        renderer));

            if (rendererName == "body" ||
                meshName == "body")
            {
                hasBody =
                    true;
            }

            if (rendererName == "body_misc" ||
                meshName == "body_misc")
            {
                hasBodyMisc =
                    true;
            }

            if (hasBody &&
                hasBodyMisc)
            {
                return true;
            }
        }

        return false;
    }

    public static string DependencyHash(
        params string[] assetPaths)
    {
        if (assetPaths == null ||
            assetPaths.Length == 0)
        {
            return string.Empty;
        }

        string value =
            string.Empty;

        foreach (string path in
                 assetPaths)
        {
            if (string.IsNullOrWhiteSpace(
                    path))
            {
                continue;
            }

            value +=
                AssetDatabase
                    .GetAssetDependencyHash(
                        path)
                    .ToString() +
                "|";
        }

        return value;
    }

    public static bool ShouldRebuild(
        string outputPrefab,
        string editorPrefsHashKey,
        string dependencyHash)
    {
        bool prefabMissing =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                outputPrefab) == null;

        if (prefabMissing)
            return true;

        string previous =
            EditorPrefs.GetString(
                editorPrefsHashKey,
                string.Empty);

        return
            !string.Equals(
                previous,
                dependencyHash,
                StringComparison.Ordinal);
    }

    public static void MarkRebuilt(
        string editorPrefsHashKey,
        string dependencyHash)
    {
        EditorPrefs.SetString(
            editorPrefsHashKey,
            dependencyHash ?? string.Empty);
    }

    private static string ResolveMeshName(
        Renderer renderer)
    {
        MeshFilter filter =
            renderer.GetComponent<MeshFilter>();

        if (filter != null &&
            filter.sharedMesh != null)
        {
            return
                filter.sharedMesh.name;
        }

        SkinnedMeshRenderer skinned =
            renderer as SkinnedMeshRenderer;

        return
            skinned != null &&
            skinned.sharedMesh != null
                ? skinned.sharedMesh.name
                : string.Empty;
    }

    private static string Normalize(
        string value)
    {
        return
            (value ?? string.Empty)
            .Replace(
                " (Clone)",
                string.Empty)
            .Replace(
                " (Instance)",
                string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace(
                ' ',
                '_')
            .Replace(
                '.',
                '_');
    }
}
