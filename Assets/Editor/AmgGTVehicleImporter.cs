using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AmgGTVehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/AmgGT/amggt.obj";

    private const string FrontWheelSource =
        "Assets/VehicleAssets/AmgGT/front_wheels.obj";

    private const string RearWheelSource =
        "Assets/VehicleAssets/AmgGT/rear_wheels.obj";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string OutputPrefab =
        OutputDirectory + "/AmgGT.prefab";

    private const string BuildSessionKey =
        "MotorCity.AmgGTVehicleBuilt.V1";

    static AmgGTVehicleImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild AmgGT")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

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
                    "Motor City: AmgGT body/front/rear OBJ source is missing.");
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
            "AmgGT";

        try
        {
            instance.transform.position =
                Vector3.zero;
            instance.transform.rotation =
                Quaternion.identity;
            instance.transform.localScale =
                Vector3.one;

            BuildWheelSet(
                instance.transform,
                frontWheelSource,
                rearWheelSource);

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
                    "Motor City: AmgGT runtime visual rebuilt. " +
                    "Runtime path: MotorCity/Vehicles/Player/AmgGT");
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
        // AMG GT export is Z-forward. The updated wheel OBJ files are now
        // centered almost exactly on their own pivots:
        // front centre ~= (-0.007555, 0.000007, -0.000003),
        // rear centre  ~= (-0.007958, 0.000008, -0.000004).
        // Keep the authored axle positions and only cancel that tiny residual
        // source-pivot offset so visuals and WheelColliders share one centre.
        CreateWheel(
            parent,
            frontSource,
            "front_left",
            new Vector3(
                -0.779f,
                0.303f,
                1.210f),
            new Vector3(
                0.007555f,
                -0.000007f,
                0.000003f),
            false);

        CreateWheel(
            parent,
            frontSource,
            "front_right",
            new Vector3(
                0.779f,
                0.303f,
                1.210f),
            new Vector3(
                0.007555f,
                -0.000007f,
                0.000003f),
            true);

        CreateWheel(
            parent,
            rearSource,
            "rear_left",
            new Vector3(
                -0.770f,
                0.319f,
                -1.170f),
            new Vector3(
                0.007958f,
                -0.000008f,
                0.000004f),
            false);

        CreateWheel(
            parent,
            rearSource,
            "rear_right",
            new Vector3(
                0.770f,
                0.319f,
                -1.170f),
            new Vector3(
                0.007958f,
                -0.000008f,
                0.000004f),
            true);
    }

    private static void CreateWheel(
        Transform parent,
        GameObject source,
        string name,
        Vector3 localPosition,
        Vector3 sourceCenterOffset,
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

        // Cancel the small residual pivot offset in the updated OBJ export so
        // both visual wheels and runtime WheelColliders share the same centre.
        visual.transform.localPosition =
            sourceCenterOffset;

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


}
