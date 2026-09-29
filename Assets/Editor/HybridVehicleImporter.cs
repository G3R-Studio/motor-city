using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class HybridVehicleImporter
{
    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string OutputPrefab =
        OutputDirectory + "/Hybrid.prefab";

    [MenuItem("Motor City/Vehicles/Rebuild Hybrid")]
    private static void RebuildFromMenu()
    {
        string sourcePath =
            ResolveSourcePath();

        if (string.IsNullOrWhiteSpace(
                sourcePath))
        {
            Debug.LogWarning(
                "Motor City: Hybrid source model was not found. " +
                "Select the new Hybrid model/prefab in the Project window " +
                "and run Motor City/Vehicles/Rebuild Hybrid again.");

            return;
        }

        if (string.Equals(
                sourcePath,
                OutputPrefab,
                StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning(
                "Motor City: the runtime Hybrid prefab cannot rebuild itself. " +
                "Select the new source model/prefab in the Project window.");

            return;
        }

        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                sourcePath);

        if (source == null)
        {
            Debug.LogWarning(
                "Motor City: selected Hybrid source is not a GameObject asset: " +
                sourcePath);

            return;
        }

        Directory.CreateDirectory(
            OutputDirectory);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                source) as GameObject;

        if (instance == null)
        {
            instance =
                UnityEngine.Object.Instantiate(
                    source);
        }

        if (instance == null)
        {
            Debug.LogError(
                "Motor City: failed to instantiate Hybrid source: " +
                sourcePath);

            return;
        }

        instance.name =
            "HybridVisual";

        try
        {
            instance.transform.position =
                Vector3.zero;

            instance.transform.rotation =
                Quaternion.identity;

            instance.transform.localScale =
                Vector3.one;

            StripImportedRuntimeComponents(
                instance);

            EnsureRenderersEnabled(
                instance);

            GameObject saved =
                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    OutputPrefab);

            if (saved == null)
            {
                Debug.LogError(
                    "Motor City: failed to save rebuilt Hybrid prefab.");

                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    OutputPrefab);

            Debug.Log(
                "Motor City: Hybrid runtime visual rebuilt from '" +
                sourcePath +
                "'. Runtime path: MotorCity/Vehicles/Player/Hybrid");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                instance);
        }
    }

    private static string ResolveSourcePath()
    {
        UnityEngine.Object selected =
            Selection.activeObject;

        if (selected != null)
        {
            string selectedPath =
                AssetDatabase.GetAssetPath(
                    selected);

            if (!string.IsNullOrWhiteSpace(
                    selectedPath) &&
                !string.Equals(
                    selectedPath,
                    OutputPrefab,
                    StringComparison.OrdinalIgnoreCase) &&
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    selectedPath) != null)
            {
                return
                    selectedPath;
            }
        }

        string[] guids =
            AssetDatabase.FindAssets(
                "hybrid");

        string[] hybredGuids =
            AssetDatabase.FindAssets(
                "hybred");

        return
            guids
                .Concat(
                    hybredGuids)
                .Distinct()
                .Select(
                    AssetDatabase.GUIDToAssetPath)
                .Where(
                    path =>
                        !string.IsNullOrWhiteSpace(
                            path) &&
                        !string.Equals(
                            path,
                            OutputPrefab,
                            StringComparison.OrdinalIgnoreCase) &&
                        AssetDatabase.LoadAssetAtPath<GameObject>(
                            path) != null)
                .OrderByDescending(
                    Score)
                .ThenBy(
                    path => path,
                    StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
    }

    private static int Score(
        string path)
    {
        string lower =
            path.ToLowerInvariant();

        int score = 0;

        if (lower.Contains(
                "hybrid"))
        {
            score += 100;
        }

        if (lower.Contains(
                "hybred"))
        {
            score += 100;
        }

        if (lower.Contains(
                "prefab"))
        {
            score += 40;
        }

        if (lower.Contains(
                "vehicle") ||
            lower.Contains(
                "car"))
        {
            score += 25;
        }

        if (lower.Contains(
                "demo") ||
            lower.Contains(
                "example") ||
            lower.Contains(
                "sample"))
        {
            score -= 60;
        }

        if (lower.Contains(
                "/resources/motorcity/"))
        {
            score -= 100;
        }

        return score;
    }

    private static void StripImportedRuntimeComponents(
        GameObject root)
    {
        foreach (Collider collider in
                 root.GetComponentsInChildren<Collider>(
                     true))
        {
            UnityEngine.Object.DestroyImmediate(
                collider);
        }

        foreach (Rigidbody body in
                 root.GetComponentsInChildren<Rigidbody>(
                     true))
        {
            UnityEngine.Object.DestroyImmediate(
                body);
        }

        foreach (MonoBehaviour behaviour in
                 root.GetComponentsInChildren<MonoBehaviour>(
                     true))
        {
            if (behaviour == null)
                continue;

            UnityEngine.Object.DestroyImmediate(
                behaviour);
        }
    }

    private static void EnsureRenderersEnabled(
        GameObject root)
    {
        foreach (Renderer renderer in
                 root.GetComponentsInChildren<Renderer>(
                     true))
        {
            if (renderer == null)
                continue;

            renderer.enabled =
                true;

            if (!renderer.gameObject.activeSelf)
            {
                renderer.gameObject.SetActive(
                    true);
            }
        }
    }
}
