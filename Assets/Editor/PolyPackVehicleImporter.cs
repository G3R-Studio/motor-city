using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PolyPackVehicleImporter
{
    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles";

    private const int VehicleCount = 4;

    static PolyPackVehicleImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Rebuild Vehicles - PolyPack Garage Cars")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Build(false);
    }

    private static void Build(
        bool force)
    {
        List<Candidate> candidates =
            FindCandidates();

        if (candidates.Count == 0)
        {
            if (force)
            {
                Debug.LogWarning(
                    "Motor City: Vehicles - PolyPack prefabs were not found. " +
                    "Import the free Vehicles - PolyPack package first.");
            }

            return;
        }

        Directory.CreateDirectory(
            OutputDirectory);

        List<Candidate> selected =
            SelectDistinctVehicles(
                candidates,
                VehicleCount);

        int written = 0;

        for (int i = 0;
             i < selected.Count;
             i++)
        {
            string outputPath =
                $"{OutputDirectory}/Vehicle_{i + 1:00}.prefab";

            if (!force &&
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    outputPath) != null)
            {
                written++;
                continue;
            }

            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    selected[i].Path);

            if (source == null)
                continue;

            GameObject instance =
                PrefabUtility.InstantiatePrefab(
                    source) as GameObject;

            if (instance == null)
            {
                instance =
                    UnityEngine.Object.Instantiate(
                        source);
            }

            instance.name =
                $"MotorCityVehicle_{i + 1:00}";

            try
            {
                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    outputPath);

                written++;

                Debug.Log(
                    $"Motor City: prepared garage vehicle {i + 1} " +
                    $"from '{selected[i].Path}'.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    instance);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (force)
        {
            Debug.Log(
                $"Motor City: prepared {written}/{VehicleCount} " +
                "Vehicles - PolyPack garage vehicles.");
        }
    }

    private static List<Candidate> FindCandidates()
    {
        List<Candidate> explicitVehicles =
            FindExplicitStarterVehicles();

        if (explicitVehicles.Count >=
            VehicleCount)
        {
            return explicitVehicles;
        }

        string[] paths =
            AssetDatabase.GetAllAssetPaths();

        var result =
            new List<Candidate>(
                explicitVehicles);

        var seen =
            new HashSet<string>(
                result.Select(
                    item => item.Path),
                StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(
                    path) ||
                !path.StartsWith(
                    "Assets/",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            string extension =
                Path.GetExtension(
                        path)
                    .ToLowerInvariant();

            if (extension != ".prefab" &&
                extension != ".fbx" &&
                extension != ".obj")
                continue;

            string lower =
                path.ToLowerInvariant();

            if (lower.Contains(
                    "/resources/motorcity/") ||
                lower.Contains(
                    "fantastic city generator") ||
                lower.Contains(
                    "arcade - free racing car") ||
                lower.Contains(
                    "cityvisual"))
                continue;

            bool packageMatch =
                lower.Contains("polypack") ||
                lower.Contains("alstra") ||
                lower.Contains("musclecar") ||
                lower.Contains("pickup") ||
                lower.Contains("suvv") ||
                lower.Contains("swifto") ||
                lower.Contains("minivan");

            if (!packageMatch)
                continue;

            if (seen.Contains(path))
                continue;

            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path);

            if (asset == null)
                continue;

            Renderer[] renderers =
                asset.GetComponentsInChildren<Renderer>(
                    true);

            if (renderers.Length == 0)
                continue;

            int score =
                Score(
                    path,
                    asset);

            if (score < -20)
                continue;

            result.Add(
                new Candidate
                {
                    Path = path,
                    Score = score,
                    Family =
                        FamilyKey(
                            path)
                });

            seen.Add(path);
        }

        if (result.Count == 0)
        {
            Debug.LogWarning(
                "Motor City: Vehicles - PolyPack scan found no usable GameObject assets. " +
                "Expected model names include SwiftoV1, MuscleCarV1, PickupV1 and SuvV1.");
        }
        else
        {
            Debug.Log(
                "Motor City: Vehicles - PolyPack candidates: " +
                string.Join(
                    " | ",
                    result
                        .OrderByDescending(
                            item => item.Score)
                        .Take(12)
                        .Select(
                            item =>
                                $"{item.Path} (score {item.Score})")));
        }

        return result
            .OrderByDescending(
                item => item.Score)
            .ThenBy(
                item => item.Path,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<Candidate> FindExplicitStarterVehicles()
    {
        string[] preferredNames =
        {
            "SwiftoV1",
            "MuscleCarV1",
            "PickupV1",
            "SuvV1"
        };

        var result =
            new List<Candidate>();

        foreach (string preferredName in
                 preferredNames)
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    preferredName);

            string bestPath =
                guids
                    .Select(
                        AssetDatabase.GUIDToAssetPath)
                    .Where(
                        path =>
                        {
                            if (string.IsNullOrWhiteSpace(
                                    path))
                                return false;

                            string extension =
                                Path.GetExtension(
                                        path)
                                    .ToLowerInvariant();

                            if (extension != ".fbx" &&
                                extension != ".prefab" &&
                                extension != ".obj")
                                return false;

                            string fileName =
                                Path.GetFileNameWithoutExtension(
                                    path);

                            return string.Equals(
                                fileName,
                                preferredName,
                                StringComparison.OrdinalIgnoreCase);
                        })
                    .OrderBy(
                        path =>
                            Path.GetExtension(
                                    path)
                                .Equals(
                                    ".prefab",
                                    StringComparison.OrdinalIgnoreCase)
                                ? 0
                                : 1)
                    .ThenBy(
                        path => path,
                        StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(
                    bestPath))
                continue;

            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    bestPath);

            if (asset == null)
                continue;

            result.Add(
                new Candidate
                {
                    Path = bestPath,
                    Score =
                        1000 -
                        result.Count * 10,
                    Family =
                        FamilyKey(
                            bestPath)
                });
        }

        if (result.Count > 0)
        {
            Debug.Log(
                "Motor City: explicit Vehicles - PolyPack models found: " +
                string.Join(
                    " | ",
                    result.Select(
                        item => item.Path)));
        }

        return result;
    }

    private static int Score(
        string path,
        GameObject prefab)
    {
        string lower =
            path.ToLowerInvariant();

        int score = 0;

        if (lower.Contains("prefab"))
            score += 25;

        string[] strong =
        {
            "car",
            "sedan",
            "coupe",
            "sport",
            "race",
            "muscle",
            "super",
            "pickup",
            "suv",
            "hatch"
        };

        foreach (string token in strong)
        {
            if (lower.Contains(token))
                score += 18;
        }

        string[] reject =
        {
            "wheel",
            "tire",
            "tyre",
            "trailer",
            "traffic",
            "prop",
            "demo",
            "scene",
            "showcase"
        };

        foreach (string token in reject)
        {
            if (lower.Contains(token))
                score -= 55;
        }

        int wheelLike =
            prefab
                .GetComponentsInChildren<Transform>(
                    true)
                .Count(
                    item =>
                    {
                        string name =
                            item.name.ToLowerInvariant();

                        return
                            name.Contains("wheel") ||
                            name.Contains("tire") ||
                            name.Contains("tyre");
                    });

        if (wheelLike >= 4)
            score += 55;
        else if (wheelLike > 0)
            score += 10;

        return score;
    }

    private static List<Candidate> SelectDistinctVehicles(
        List<Candidate> candidates,
        int count)
    {
        var selected =
            new List<Candidate>();

        var families =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (Candidate candidate in candidates)
        {
            if (families.Contains(
                    candidate.Family))
                continue;

            selected.Add(
                candidate);

            families.Add(
                candidate.Family);

            if (selected.Count >= count)
                return selected;
        }

        foreach (Candidate candidate in candidates)
        {
            if (selected.Any(
                    item =>
                        item.Path ==
                        candidate.Path))
                continue;

            selected.Add(
                candidate);

            if (selected.Count >= count)
                break;
        }

        return selected;
    }

    private static string FamilyKey(
        string path)
    {
        string stem =
            Path.GetFileNameWithoutExtension(
                    path)
                .ToLowerInvariant();

        string[] removable =
        {
            "blue",
            "red",
            "green",
            "yellow",
            "black",
            "white",
            "orange",
            "purple",
            "grey",
            "gray",
            "variant",
            "prefab"
        };

        foreach (string token in removable)
        {
            stem =
                stem.Replace(
                    token,
                    string.Empty);
        }

        return stem
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
    }

    private sealed class Candidate
    {
        public string Path;
        public int Score;
        public string Family;
    }
}
