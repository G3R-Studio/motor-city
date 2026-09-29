using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HybridVehicleImporter
{
    private const string SourceDirectory =
        "Assets/VehicleAssets/Hybrid";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string OutputPrefab =
        OutputDirectory + "/Hybrid.prefab";

    private const string BuildSessionKey =
        "MotorCity.HybridVehicleBuilt.V2";

    static HybridVehicleImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Hybrid")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!AssetDatabase.IsValidFolder(
                SourceDirectory))
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(
                OutputPrefab) != null)
        {
            return;
        }

        if (SessionState.GetBool(
                BuildSessionKey,
                false))
        {
            return;
        }

        SessionState.SetBool(
            BuildSessionKey,
            true);

        Build(false);
    }

    private static bool Build(
        bool verbose)
    {
        string sourcePath =
            FindSourceModel();

        if (string.IsNullOrWhiteSpace(
                sourcePath))
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Hybrid source model was not found in " +
                    SourceDirectory +
                    ". Put the Hybrid FBX/OBJ/prefab there and rebuild again.");
            }

            return false;
        }

        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                sourcePath);

        if (source == null)
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Hybrid source is not a GameObject asset: " +
                    sourcePath);
            }

            return false;
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
            if (verbose)
            {
                Debug.LogError(
                    "Motor City: failed to instantiate Hybrid source: " +
                    sourcePath);
            }

            return false;
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
                if (verbose)
                {
                    Debug.LogError(
                        "Motor City: failed to save rebuilt Hybrid prefab.");
                }

                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (verbose)
            {
                Selection.activeObject =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        OutputPrefab);

                Debug.Log(
                    "Motor City: Hybrid runtime visual rebuilt from '" +
                    sourcePath +
                    "'. Runtime path: MotorCity/Vehicles/Player/Hybrid");
            }

            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                instance);
        }
    }

    private static string FindSourceModel()
    {
        if (!AssetDatabase.IsValidFolder(
                SourceDirectory))
        {
            return null;
        }

        string[] guids =
            AssetDatabase.FindAssets(
                "t:GameObject",
                new[]
                {
                    SourceDirectory
                });

        return
            guids
                .Select(
                    AssetDatabase.GUIDToAssetPath)
                .Where(
                    path =>
                        !string.IsNullOrWhiteSpace(
                            path) &&
                        IsSupportedSource(
                            path))
                .OrderByDescending(
                    ScoreSource)
                .ThenBy(
                    path => path,
                    StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
    }

    private static bool IsSupportedSource(
        string path)
    {
        string extension =
            Path.GetExtension(
                    path)
                .ToLowerInvariant();

        return
            extension == ".prefab" ||
            extension == ".fbx" ||
            extension == ".obj";
    }

    private static int ScoreSource(
        string path)
    {
        string lower =
            path.ToLowerInvariant();

        string fileName =
            Path.GetFileNameWithoutExtension(
                    lower);

        int score = 0;

        if (fileName == "hybrid" ||
            fileName == "hybred")
        {
            score += 300;
        }

        if (fileName.Contains(
                "hybrid") ||
            fileName.Contains(
                "hybred"))
        {
            score += 180;
        }

        if (fileName.Contains(
                "body") ||
            fileName.Contains(
                "car"))
        {
            score += 90;
        }

        if (lower.EndsWith(
                ".prefab"))
        {
            score += 45;
        }
        else if (lower.EndsWith(
                     ".fbx"))
        {
            score += 35;
        }
        else if (lower.EndsWith(
                     ".obj"))
        {
            score += 25;
        }

        if (fileName.Contains(
                "wheel") ||
            fileName.Contains(
                "tire") ||
            fileName.Contains(
                "tyre") ||
            fileName.Contains(
                "rim"))
        {
            score -= 250;
        }

        if (fileName.Contains(
                "demo") ||
            fileName.Contains(
                "sample") ||
            fileName.Contains(
                "example"))
        {
            score -= 150;
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
