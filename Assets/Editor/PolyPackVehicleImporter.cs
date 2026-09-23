using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PolyPackVehicleImporter
{
    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles";

    private static readonly string[] SourcePaths =
    {
        "Assets/Alstra Infinite/Vehicles LowPoly/Prefabs/Version 1.2/SwiftoV2.prefab",
        "Assets/Alstra Infinite/Vehicles LowPoly/Prefabs/Version 1.2/PickupV2.prefab",
        "Assets/Alstra Infinite/Vehicles LowPoly/Prefabs/Version 1.2/MuscleCarV4.prefab",
        // Keep the existing authored Apex resource untouched.
        null
    };

    static PolyPackVehicleImporter()
    {
        EditorApplication.delayCall +=
            EnsureCuratedGarageCars;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Curated Garage Cars")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    [MenuItem("Motor City/Vehicles/Validate Curated Garage Cars")]
    private static void ValidateCuratedGarageCars()
    {
        int valid = 0;

        for (int i = 0;
             i < 4;
             i++)
        {
            string outputPath =
                $"{OutputDirectory}/Vehicle_{i + 1:00}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    outputPath) != null)
            {
                valid++;
            }
            else
            {
                Debug.LogWarning(
                    "Motor City: missing/unreadable curated garage vehicle: " +
                    outputPath);
            }
        }

        Debug.Log(
            $"Motor City: curated garage vehicles available: {valid}/4.");
    }

    private static void EnsureCuratedGarageCars()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool needsBuild = false;

        for (int i = 0;
             i < 3;
             i++)
        {
            string outputPath =
                $"{OutputDirectory}/Vehicle_{i + 1:00}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    outputPath) == null)
            {
                needsBuild = true;
                break;
            }
        }

        if (needsBuild)
        {
            Build(false);
        }
    }

    private static void Build(
        bool force)
    {
        Directory.CreateDirectory(
            OutputDirectory);

        int written = 0;

        for (int i = 0;
             i < 3;
             i++)
        {
            string sourcePath =
                SourcePaths[i];

            string outputPath =
                $"{OutputDirectory}/Vehicle_{i + 1:00}.prefab";

            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    sourcePath);

            if (source == null)
            {
                Debug.LogError(
                    "Motor City: curated garage source prefab is missing: " +
                    sourcePath);
                continue;
            }

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
                    "Motor City: failed to instantiate curated garage source: " +
                    sourcePath);
                continue;
            }

            instance.name =
                $"MotorCityVehicle_{i + 1:00}";

            try
            {
                GameObject saved =
                    PrefabUtility.SaveAsPrefabAsset(
                        instance,
                        outputPath);

                if (saved == null)
                {
                    Debug.LogError(
                        "Motor City: failed to save curated garage vehicle: " +
                        outputPath);
                    continue;
                }

                written++;

                if (force)
                {
                    Debug.Log(
                        $"Motor City: rebuilt curated garage vehicle {i + 1} " +
                        $"from '{sourcePath}'.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    instance);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (written < 3)
        {
            Debug.LogWarning(
                $"Motor City: rebuilt only {written}/3 curated garage cars. " +
                "Check that the Alstra PolyPack source prefabs are imported.");
        }
        else if (force)
        {
            Debug.Log(
                "Motor City: rebuilt all 3 curated garage cars. Apex was kept unchanged.");
        }
    }
}
