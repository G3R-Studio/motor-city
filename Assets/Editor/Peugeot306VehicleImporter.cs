using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Peugeot306VehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/Peugeot306/peugeot306.obj";

    // Revised Peugeot follows the Beatall export layout:
    // one front_wheels OBJ is reused on both axles.
    private const string WheelSource =
        "Assets/VehicleAssets/Peugeot306/front_wheels.obj";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string OutputPrefab =
        OutputDirectory + "/Peugeot306.prefab";

    private const string BuildSessionKey =
        "MotorCity.Peugeot306VehicleBuilt.V2";

    private const string SourceHashKey =
        "MotorCity.Peugeot306VehicleSourceHash.V2";

    static Peugeot306VehicleImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Peugeot306")]
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
                WheelSource);

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
                    "Motor City: Peugeot306 body or front_wheels OBJ is missing.");
            }

            return false;
        }

        if (!StandardVehicleImportUtility.UsesStandardBodyLayout(
                bodySource))
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: updated Peugeot306 must contain body and body_misc meshes.");
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
                Object.Instantiate(
                    bodySource);
        }

        if (instance == null)
            return false;

        instance.name =
            "Peugeot306";

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

            // Keep OBJ/MTL materials authored exactly like Beatall.
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
                    "Motor City: Peugeot306 rebuilt from peugeot306.obj + shared front_wheels.obj.");
            }

            return true;
        }
        finally
        {
            Object.DestroyImmediate(
                instance);
        }
    }

    private static void BuildWheelSet(
        Transform parent,
        GameObject wheelSource)
    {
        // Preserve the already tuned Peugeot axle centres while switching the
        // visual source to the same shared-wheel layout used by Beatall.
        CreateWheel(
            parent,
            wheelSource,
            "front_left",
            new Vector3(
                -0.780f,
                0.336f,
                1.240f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "front_right",
            new Vector3(
                0.780f,
                0.336f,
                1.240f),
            true);

        CreateWheel(
            parent,
            wheelSource,
            "rear_left",
            new Vector3(
                -0.780f,
                0.336f,
                -1.395f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "rear_right",
            new Vector3(
                0.780f,
                0.336f,
                -1.395f),
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
                Object.Instantiate(
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
                Object.DestroyImmediate(
                    collider);
            }
        }

        foreach (Rigidbody body in
                 root.GetComponentsInChildren<Rigidbody>(
                     true))
        {
            if (body != null)
            {
                Object.DestroyImmediate(
                    body);
            }
        }

        foreach (MonoBehaviour behaviour in
                 root.GetComponentsInChildren<MonoBehaviour>(
                     true))
        {
            if (behaviour != null)
            {
                Object.DestroyImmediate(
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
