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
        "Assets/Alstra Infinite/Vehicles LowPoly/Prefabs/Version 1.2/MuscleCarV2.prefab",
        "Assets/Alstra Infinite/Vehicles LowPoly/Prefabs/Version 1.2/SuvV1.prefab"
    };

    private const string BusSourcePath =
        "Assets/Fantastic City Generator/Traffic System/Vehicles/Prefabs/BusMirim.prefab";

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
             i < 5;
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
            $"Motor City: curated garage vehicles available: {valid}/5.");
    }

    private static void EnsureCuratedGarageCars()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        const string SessionKey =
            "MotorCity.CuratedGarageBuilt.V4";

        if (SessionState.GetBool(
                SessionKey,
                false))
        {
            return;
        }

        // Rebuild once per Editor session so changes to the curated source
        // selection are actually propagated even when Vehicle_01..04 already
        // exist from an older lineup.
        Build(false);

        SessionState.SetBool(
            SessionKey,
            true);
    }

    private static void Build(
        bool force)
    {
        Directory.CreateDirectory(
            OutputDirectory);

        int written = 0;

        for (int i = 0;
             i < SourcePaths.Length;
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

        if (BuildBusVehicle(force))
        {
            written++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (written < 5)
        {
            Debug.LogWarning(
                $"Motor City: rebuilt only {written}/5 curated garage vehicles. " +
                "Check the four PolyPack cars and Fantastic City Generator bus assets.");
        }
        else if (force)
        {
            Debug.Log(
                "Motor City: rebuilt 4 curated PolyPack cars plus the unlockable bus.");
        }
    }
    private static bool BuildBusVehicle(
        bool force)
    {
        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                BusSourcePath);

        if (source == null)
        {
            Debug.LogError(
                "Motor City: traffic bus source prefab is missing: " +
                BusSourcePath);
            return false;
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
            return false;

        const string outputPath =
            OutputDirectory + "/Vehicle_05.prefab";

        instance.name =
            "MotorCityVehicle_05_Bus";

        try
        {
            // Keep the original FCG bus rig intact in the generated resource.
            // The runtime installer reads its authored FL/FR/BL/BR axle layout,
            // WheelCollider radius and body dimensions before disabling imported
            // traffic physics. Stripping these here forced runtime heuristics to
            // reconstruct a rig that the source asset already defines correctly.

            GameObject saved =
                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    outputPath);

            if (saved == null)
                return false;

            if (force)
            {
                Debug.Log(
                    "Motor City: rebuilt final unlock bus from traffic prefab '" +
                    BusSourcePath +
                    "'.");
            }

            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                instance);
        }
    }

}
