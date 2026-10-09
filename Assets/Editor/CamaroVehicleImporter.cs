using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CamaroVehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/Camaro/camaro.obj";

    private const string MaterialSource =
        "Assets/VehicleAssets/Camaro/camaro.mtl";

    // Updated Camaro follows the same wheel export layout as Beatall:
    // one authored front_wheels source is reused on both axles.
    private const string WheelSource =
        "Assets/VehicleAssets/Camaro/front_wheels.obj";

    private const string ColorTextureSource =
        "Assets/VehicleAssets/Camaro/Color.png";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string OutputPrefab =
        OutputDirectory + "/Camaro.prefab";

    private const string SourceHashKey =
        "MotorCity.CamaroVehicleSourceHash.V3";

    static CamaroVehicleImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Camaro")]
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
                WheelSource) == null)
        {
            return;
        }

        string dependencyHash =
            StandardVehicleImportUtility.DependencyHash(
                BodySource,
                MaterialSource,
                WheelSource,
                ColorTextureSource);

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

        GameObject wheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                WheelSource);

        if (bodySource == null ||
            wheelSource == null)
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Camaro body or front_wheels OBJ is missing.");
            }

            return false;
        }

        if (!StandardVehicleImportUtility.UsesStandardBodyLayout(
                bodySource))
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: updated Camaro must contain both body and body_misc meshes.");
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
            "Camaro";

        try
        {
            instance.transform.position =
                Vector3.zero;

            instance.transform.rotation =
                Quaternion.identity;

            instance.transform.localScale =
                Vector3.one * 1.10f;

            StripImportedPhysics(
                instance);

            // Preserve OBJ/MTL material slots exactly as authored. The shared
            // runtime vehicle installer performs the URP conversion, matching
            // AmgGT and the revised Beatall.
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
                Selection.activeObject =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        OutputPrefab);

                Debug.Log(
                    "Motor City: Camaro runtime visual rebuilt from " +
                    "camaro.obj + shared front_wheels.obj. " +
                    "Runtime path: MotorCity/Vehicles/Player/Camaro");
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
        // Keep the previously tuned Camaro axle centres. Only the source
        // layout changed: the same authored wheel visual is now reused for
        // front and rear, exactly like the revised Beatall.
        CreateWheel(
            parent,
            wheelSource,
            "front_left",
            new Vector3(
                -0.855f,
                0.392f,
                1.340f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "front_right",
            new Vector3(
                0.855f,
                0.392f,
                1.340f),
            true);

        CreateWheel(
            parent,
            wheelSource,
            "rear_left",
            new Vector3(
                -0.855f,
                0.392f,
                -1.335f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "rear_right",
            new Vector3(
                0.855f,
                0.392f,
                -1.335f),
            true);
    }

    private static void CreateWheel(
        Transform parent,
        GameObject source,
        string name,
        Vector3 localPosition,
        bool rightSide)
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
            rightSide
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
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    collider);
            }
        }

        foreach (Rigidbody body in
                 root.GetComponentsInChildren<Rigidbody>(
                     true))
        {
            if (body != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    body);
            }
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

            renderer.enabled =
                true;

            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.On;

            renderer.receiveShadows =
                true;
        }
    }
}
