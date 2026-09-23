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
        "Assets/Alstra Infinite/Vehicles LowPoly/Prefabs/Version 1.2/MuscleCarV2.prefab"
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
            $"Motor City: curated garage vehicles available: {valid}/4.");
    }

    private static void EnsureCuratedGarageCars()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        const string SessionKey =
            "MotorCity.CuratedGarageBuilt.V3";

        if (SessionState.GetBool(
                SessionKey,
                false))
        {
            return;
        }

        // Rebuild once per Editor session so changes to the curated source
        // selection are actually propagated even when Vehicle_01..03 already
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

        if (BuildBusVehicle(force))
        {
            written++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (written < 4)
        {
            Debug.LogWarning(
                $"Motor City: rebuilt only {written}/4 curated garage vehicles. " +
                "Check the PolyPack cars and Fantastic City Generator bus assets.");
        }
        else if (force)
        {
            Debug.Log(
                "Motor City: rebuilt 3 curated cars plus the unlockable bus. Apex was kept unchanged.");
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
            // The traffic prefab is reused only as a visual. Remove its AI,
            // traffic physics and colliders so the player car controller owns
            // all driving behaviour at runtime.
            foreach (MonoBehaviour behaviour in
                     instance.GetComponentsInChildren<MonoBehaviour>(
                         true))
            {
                if (behaviour != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        behaviour);
                }
            }

            foreach (Rigidbody body in
                     instance.GetComponentsInChildren<Rigidbody>(
                         true))
            {
                if (body != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        body);
                }
            }

            foreach (Collider collider in
                     instance.GetComponentsInChildren<Collider>(
                         true))
            {
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        collider);
                }
            }

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
