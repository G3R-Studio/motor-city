using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DeloreanVehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/Delorean/delorean.obj";

    private const string FrontWheelSource =
        "Assets/VehicleAssets/Delorean/front_wheels.obj";

    private const string RearWheelSource =
        "Assets/VehicleAssets/Delorean/rear_wheels.obj";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string OutputPrefab =
        OutputDirectory + "/Delorean.prefab";

    private const string BuildSessionKey =
        "MotorCity.DeloreanVehicleBuilt.V2";

    private const string SourceHashKey =
        "MotorCity.DeloreanVehicleSourceHash.V2";

    static DeloreanVehicleImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Delorean")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(
                BodySource) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(
                FrontWheelSource) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(
                RearWheelSource) == null)
        {
            return;
        }

        string dependencyHash =
            StandardVehicleImportUtility.DependencyHash(
                BodySource,
                FrontWheelSource,
                RearWheelSource);

        if (!StandardVehicleImportUtility.ShouldRebuild(
                OutputPrefab,
                SourceHashKey,
                dependencyHash))
        {
            return;
        }

        if (Build(false))
        {
            StandardVehicleImportUtility.MarkRebuilt(
                SourceHashKey,
                dependencyHash);
        }
    }

    private static bool Build(
        bool verbose)
    {
        GameObject bodySource =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                BodySource);

        GameObject frontWheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                FrontWheelSource);

        GameObject rearWheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                RearWheelSource);

        if (bodySource == null ||
            frontWheelSource == null ||
            rearWheelSource == null)
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Delorean body/front/rear OBJ source is missing.");
            }

            return false;
        }

        if (!StandardVehicleImportUtility.UsesStandardBodyLayout(
                bodySource))
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: updated Delorean must contain body and body_misc meshes.");
            }

            return false;
        }

        Directory.CreateDirectory(
            OutputDirectory);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                bodySource) as GameObject;

        if (instance == null)
        {
            instance =
                UnityEngine.Object.Instantiate(
                    bodySource);
        }

        if (instance == null)
            return false;

        instance.name =
            "Delorean";

        try
        {
            instance.transform.position =
                Vector3.zero;
            instance.transform.rotation =
                Quaternion.identity;
            instance.transform.localScale =
                Vector3.one;

            StripImportedPhysics(
                instance);

            BuildWheelSet(
                instance.transform,
                frontWheelSource,
                rearWheelSource);

            // Keep body/body_misc material slots authored in OBJ/MTL.
            // The shared runtime installer upgrades them for URP, matching Beatall.

            EnsureRenderersEnabled(
                instance);

            GameObject saved =
                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    OutputPrefab);

            if (saved == null)
                return false;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (verbose)
            {
                Debug.Log(
                    "Motor City: Delorean rebuilt from delorean.obj + authored front/rear wheel OBJs.");
            }

            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                instance);
        }
    }

    private static void BuildWheelSet(
        Transform parent,
        GameObject frontSource,
        GameObject rearSource)
    {
        // Corrected Delorean export is already Z-forward.
        // Wheel source meshes are centered at their own pivots, so only the
        // four authored wheel-center locations are applied here. Runtime
        // WheelColliders are still generated automatically from these meshes.
        CreateWheel(
            parent,
            frontSource,
            "front_left",
            new Vector3(
                -0.865f,
                0.38146f,
                1.445f),
            false);

        CreateWheel(
            parent,
            frontSource,
            "front_right",
            new Vector3(
                0.865f,
                0.33646f,
                1.445f),
            true);

        CreateWheel(
            parent,
            rearSource,
            "rear_left",
            new Vector3(
                -0.855f,
                0.41997f,
                -1.185f),
            false);

        CreateWheel(
            parent,
            rearSource,
            "rear_right",
            new Vector3(
                0.855f,
                0.37497f,
                -1.185f),
            true);
    }

    private static void CreateWheel(
        Transform parent,
        GameObject source,
        string name,
        Vector3 localPosition,
        bool oppositeSide)
    {
        GameObject holder =
            new GameObject(
                name);

        holder.transform.SetParent(
            parent,
            false);

        holder.transform.localPosition =
            localPosition;

        holder.transform.localRotation =
            oppositeSide
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

        StripImportedPhysics(
            visual);
    }

    private static void StripImportedPhysics(
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
            if (behaviour != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    behaviour);
            }
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

            renderer.enabled = true;

            if (!renderer.gameObject.activeSelf)
                renderer.gameObject.SetActive(true);
        }
    }
}
