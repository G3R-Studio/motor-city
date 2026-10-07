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

    public static GameObject LoadPreferredWheelSource(
        string standardPath,
        string legacyPath)
    {
        GameObject standard =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                standardPath);

        if (standard != null)
            return standard;

        return
            AssetDatabase.LoadAssetAtPath<GameObject>(
                legacyPath);
    }

    public static void BuildStandardWheelSet(
        Transform parent,
        GameObject frontSource,
        GameObject rearSource,
        Vector3 frontLeft,
        Vector3 frontRight,
        Vector3 rearLeft,
        Vector3 rearRight)
    {
        if (parent == null ||
            frontSource == null ||
            rearSource == null)
        {
            return;
        }

        CreateStandardWheel(
            parent,
            frontSource,
            "front_left",
            frontLeft,
            false);

        CreateStandardWheel(
            parent,
            frontSource,
            "front_right",
            frontRight,
            true);

        CreateStandardWheel(
            parent,
            rearSource,
            "rear_left",
            rearLeft,
            false);

        CreateStandardWheel(
            parent,
            rearSource,
            "rear_right",
            rearRight,
            true);
    }

    private static void CreateStandardWheel(
        Transform parent,
        GameObject source,
        string name,
        Vector3 localPosition,
        bool rightSide)
    {
        GameObject holder =
            new GameObject(
                name);

        holder.transform.SetParent(
            parent,
            false);

        holder.transform.localPosition =
            localPosition;

        // Standardized wheel exports follow the AmgGT convention:
        // source authored for the right side, mirror the left side around Y.
        holder.transform.localRotation =
            rightSide
                ? Quaternion.identity
                : Quaternion.Euler(
                    0f,
                    180f,
                    0f);

        holder.transform.localScale =
            Vector3.one;

        GameObject visual =
            PrefabUtility.InstantiatePrefab(
                source,
                holder.transform) as GameObject;

        if (visual == null)
        {
            visual =
                UnityEngine.Object.Instantiate(
                    source,
                    holder.transform);
        }

        if (visual == null)
            return;

        visual.name =
            name + "_visual";

        visual.transform.localPosition =
            Vector3.zero;

        visual.transform.localRotation =
            Quaternion.identity;

        visual.transform.localScale =
            Vector3.one;

        foreach (Collider collider in
                 visual.GetComponentsInChildren<Collider>(
                     true))
        {
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    collider);
            }
        }

        foreach (Rigidbody body in
                 visual.GetComponentsInChildren<Rigidbody>(
                     true))
        {
            if (body != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    body);
            }
        }
    }

    public static bool HasAsset(
        string assetPath)
    {
        return
            !string.IsNullOrWhiteSpace(
                assetPath) &&
            AssetDatabase.LoadMainAssetAtPath(
                assetPath) != null;
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
