using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ArcadeFreeRacingCarImporter
{
    private const string OutputDirectory = "Assets/Resources/MotorCity";
    private const string OutputPrefab = OutputDirectory + "/PlayerCarVisual.prefab";

    static ArcadeFreeRacingCarImporter()
    {
        EditorApplication.delayCall += TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild STREET Visual")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Build(false);
    }

    private static void Build(bool force)
    {
        string sourcePath = FindSourcePrefab();

        if (string.IsNullOrEmpty(sourcePath))
        {
            if (force)
                Debug.LogWarning(
                    "Motor City: ARCADE Free Racing Car was not found. " +
                    "Import asset 161085 from Package Manager first.");
            return;
        }

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefab);
        if (!force && existing != null)
        {
            bool existingHasBodyCollider =
                existing.GetComponentsInChildren<Collider>(true)
                    .Any(c =>
                    {
                        string lower = c.transform.name.ToLowerInvariant();
                        return
                            !(lower.Contains("wheel") ||
                              lower.Contains("tire") ||
                              lower.Contains("tyre") ||
                              lower.Contains("rim"));
                    });

            GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            bool candidateHasBodyCollider =
                candidate != null &&
                candidate.GetComponentsInChildren<Collider>(true)
                    .Any(c =>
                    {
                        string lower = c.transform.name.ToLowerInvariant();
                        return
                            !(lower.Contains("wheel") ||
                              lower.Contains("tire") ||
                              lower.Contains("tyre") ||
                              lower.Contains("rim"));
                    });

            if (existingHasBodyCollider || !candidateHasBodyCollider)
                return;
        }

        Directory.CreateDirectory(OutputDirectory);

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null) return;

        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(source);

        instance.name = "PlayerCarVisual";

        try
        {
            PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Motor City: prepared ARCADE Free Racing Car visual from '{sourcePath}'.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static string FindSourcePrefab()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        var candidates = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path =>
            {
                string lower = path.ToLowerInvariant();
                return
                    lower.Contains("arcade - free racing car") ||
                    lower.Contains("free_racing_car") ||
                    lower.Contains("free racing car");
            })
            .Select(path => new
            {
                Path = path,
                Score = Score(path)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return candidates.Length > 0 ? candidates[0].Path : null;
    }

    private static int Score(string path)
    {
        string lower = path.ToLowerInvariant();
        int score = 0;

        if (lower.Contains("arcade - free racing car")) score += 100;
        if (lower.Contains("prefab")) score += 40;
        if (lower.Contains("blue")) score += 30;
        if (lower.Contains("collider")) score += 35;
        if (lower.Contains("variant")) score += 10;
        if (lower.Contains("meshes only")) score -= 10;
        if (lower.Contains("demo")) score -= 50;

        return score;
    }
}
