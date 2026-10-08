using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BeatallVehicleImporter
{
    private const string SourceModel =
        "Assets/VehicleAssets/Beatall/beatall.obj";

    private const string MaterialSource =
        "Assets/VehicleAssets/Beatall/beatall.mtl";

    private const string SourceTexture =
        "Assets/VehicleAssets/Beatall/all.png";

    // Beatall uses one authored wheel source for both axles.
    // Unlike AmgGT there is no separate rear_wheels.obj.
    private const string WheelSource =
        "Assets/VehicleAssets/Beatall/front_wheels.obj";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string OutputPrefab =
        OutputDirectory + "/Beatall.prefab";

    private const string BuildSessionKey =
        "MotorCity.BeatallVehicleBuilt.V4";

    private const string SourceHashKey =
        "MotorCity.BeatallVehicleSourceHash.V3";

    static BeatallVehicleImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Beatall")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(
                SourceModel) == null)
        {
            return;
        }

        string sourceHash =
            ResolveSourceDependencyHash();

        bool prefabMissing =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                OutputPrefab) == null;

        bool sourceChanged =
            !string.Equals(
                EditorPrefs.GetString(
                    SourceHashKey,
                    string.Empty),
                sourceHash,
                StringComparison.Ordinal);

        bool builtThisSession =
            SessionState.GetBool(
                BuildSessionKey,
                false);

        if (!prefabMissing &&
            !sourceChanged &&
            builtThisSession)
        {
            return;
        }

        SessionState.SetBool(
            BuildSessionKey,
            true);

        if (Build(false))
        {
            EditorPrefs.SetString(
                SourceHashKey,
                sourceHash);
        }
    }

    private static string ResolveSourceDependencyHash()
    {
        Hash128 modelHash =
            AssetDatabase.GetAssetDependencyHash(
                SourceModel);

        Hash128 wheelHash =
            AssetDatabase.GetAssetDependencyHash(
                WheelSource);

        Hash128 textureHash =
            AssetDatabase.GetAssetDependencyHash(
                SourceTexture);

        Hash128 materialHash =
            AssetDatabase.GetAssetDependencyHash(
                MaterialSource);

        return
            modelHash + "|" +
            wheelHash + "|" +
            textureHash + "|" +
            materialHash;
    }

    private static bool Build(
        bool verbose)
    {
        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                SourceModel);

        GameObject wheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                WheelSource);

        if (source == null ||
            wheelSource == null)
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Beatall body or front_wheels OBJ is missing.");
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
            return false;

        instance.name =
            "Beatall";

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

            // Keep the OBJ/MTL material assignments exactly like AmgGT.
            // ArcadeRacingCarRuntimeInstaller performs the shared pipeline
            // upgrade for body/body_misc/wheel materials at runtime.
            BuildWheelSet(
                instance.transform,
                wheelSource);

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
                    "Motor City: Beatall runtime visual rebuilt from " +
                    SourceModel +
                    ". Runtime path: MotorCity/Vehicles/Player/Beatall");
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
        GameObject wheelSource)
    {
        if (parent == null ||
            wheelSource == null)
        {
            return;
        }

        // The revised Beatall follows the same split-visual approach as AmgGT,
        // but exposes one shared front_wheels OBJ instead of separate front
        // and rear wheel sources. Instantiate that same authored wheel visual
        // at all four axle positions.
        CreateWheel(
            parent,
            wheelSource,
            "front_left",
            new Vector3(
                -0.573373f,
                0.2620855f,
                1.057121f),
            true);

        CreateWheel(
            parent,
            wheelSource,
            "front_right",
            new Vector3(
                0.573373f,
                0.2620855f,
                1.057121f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "rear_left",
            new Vector3(
                -0.600407f,
                0.2620855f,
                -0.968537f),
            true);

        CreateWheel(
            parent,
            wheelSource,
            "rear_right",
            new Vector3(
                0.600407f,
                0.2620855f,
                -0.968537f),
            false);
    }

    private static void CreateWheel(
        Transform parent,
        GameObject wheelSource,
        string wheelName,
        Vector3 localPosition,
        bool rightSide)
    {
        GameObject holder =
            new GameObject(
                wheelName);

        holder.transform.SetParent(
            parent,
            false);

        holder.transform.localPosition =
            localPosition;

        // The supplied wheel is authored for the RIGHT side.
        // Keep that exact orientation for both right wheels. Left-side
        // wheels are mirrored across the car by rotating around local Y.
        holder.transform.localRotation =
            rightSide
                ? Quaternion.Euler(
                    0f,
                    180f,
                    0f)
                : Quaternion.identity;

        holder.transform.localScale =
            Vector3.one;

        GameObject visual =
            PrefabUtility.InstantiatePrefab(
                wheelSource,
                holder.transform) as GameObject;

        if (visual == null)
        {
            visual =
                UnityEngine.Object.Instantiate(
                    wheelSource,
                    holder.transform);
        }

        if (visual == null)
            return;

        visual.name =
            wheelName + "_visual";

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
