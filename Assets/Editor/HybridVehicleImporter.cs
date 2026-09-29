using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HybridVehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/Hybrid/body.obj";

    private const string LeftWheelSource =
        "Assets/VehicleAssets/Hybrid/wheels1.obj";

    private const string RightWheelSource =
        "Assets/VehicleAssets/Hybrid/wheels2.obj";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string OutputPrefab =
        OutputDirectory + "/Hybrid.prefab";

    private const string BuildSessionKey =
        "MotorCity.HybridVehicleBuilt.V3";

    static HybridVehicleImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Hybrid")]
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

        GameObject leftWheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                LeftWheelSource);

        GameObject rightWheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                RightWheelSource);

        if (bodySource == null ||
            leftWheelSource == null ||
            rightWheelSource == null)
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Hybrid source files are missing. Expected " +
                    BodySource + ", " +
                    LeftWheelSource + " and " +
                    RightWheelSource + ".");
            }

            return false;
        }

        Directory.CreateDirectory(
            OutputDirectory);

        GameObject root =
            new GameObject(
                "HybridVisual");

        try
        {
            GameObject body =
                InstantiateSource(
                    bodySource,
                    root.transform,
                    "Body");

            if (body == null)
                return false;

            body.transform.localPosition =
                Vector3.zero;

            body.transform.localRotation =
                Quaternion.identity;

            body.transform.localScale =
                Vector3.one;

            StripImportedRuntimeComponents(
                body);

            BuildWheelSet(
                root.transform,
                leftWheelSource,
                rightWheelSource);

            StripImportedRuntimeComponents(
                root);

            EnsureRenderersEnabled(
                root);

            GameObject saved =
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    OutputPrefab);

            if (saved == null)
            {
                if (verbose)
                {
                    Debug.LogError(
                        "Motor City: failed to save rebuilt Hybrid prefab.");
                }

                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (verbose)
            {
                Selection.activeObject =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        OutputPrefab);

                Debug.Log(
                    "Motor City: Hybrid rebuilt from body.obj + wheels1.obj + wheels2.obj. " +
                    "Runtime path: MotorCity/Vehicles/Player/Hybrid");
            }

            return true;
        }
        finally
        {
            Object.DestroyImmediate(
                root);
        }
    }

    private static void BuildWheelSet(
        Transform parent,
        GameObject leftWheelSource,
        GameObject rightWheelSource)
    {
        // Measured from the committed Hybrid source:
        // body bounds 1.401 x 0.778 x 3.135 m;
        // wheel diameter ~= 0.484 m.
        //
        // wheels1.obj is the left-side wheel mesh and wheels2.obj is the
        // mirrored right-side mesh. Both are already centered almost exactly
        // on their pivots. The axle positions below match the authored Hybrid
        // proportions and keep every wheel as a separate steering/spinning
        // transform for the runtime installer.
        const float wheelX = 0.540f;
        const float wheelY = 0.242f;
        const float frontZ = 1.253f;
        const float rearZ = -0.654f;

        CreateWheel(
            parent,
            leftWheelSource,
            "front_left",
            new Vector3(
                -wheelX,
                wheelY,
                frontZ),
            new Vector3(
                0.033111f,
                -0.0000515f,
                0f));

        CreateWheel(
            parent,
            rightWheelSource,
            "front_right",
            new Vector3(
                wheelX,
                wheelY,
                frontZ),
            new Vector3(
                -0.033111f,
                -0.0000515f,
                0f));

        CreateWheel(
            parent,
            leftWheelSource,
            "rear_left",
            new Vector3(
                -wheelX,
                wheelY,
                rearZ),
            new Vector3(
                0.033111f,
                -0.0000515f,
                0f));

        CreateWheel(
            parent,
            rightWheelSource,
            "rear_right",
            new Vector3(
                wheelX,
                wheelY,
                rearZ),
            new Vector3(
                -0.033111f,
                -0.0000515f,
                0f));
    }

    private static void CreateWheel(
        Transform parent,
        GameObject source,
        string name,
        Vector3 localPosition,
        Vector3 sourceCenterOffset)
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
            Quaternion.identity;

        holder.transform.localScale =
            Vector3.one;

        GameObject visual =
            InstantiateSource(
                source,
                holder.transform,
                name + "_visual");

        if (visual == null)
            return;

        // Cancel the tiny source-pivot offsets measured from the OBJ bounds.
        visual.transform.localPosition =
            sourceCenterOffset;

        visual.transform.localRotation =
            Quaternion.identity;

        visual.transform.localScale =
            Vector3.one;

        StripImportedRuntimeComponents(
            visual);
    }

    private static GameObject InstantiateSource(
        GameObject source,
        Transform parent,
        string objectName)
    {
        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                source,
                parent) as GameObject;

        if (instance == null)
        {
            instance =
                Object.Instantiate(
                    source,
                    parent);
        }

        if (instance == null)
            return null;

        instance.name =
            objectName;

        return instance;
    }

    private static void StripImportedRuntimeComponents(
        GameObject root)
    {
        foreach (Collider collider in
                 root.GetComponentsInChildren<Collider>(
                     true))
        {
            Object.DestroyImmediate(
                collider);
        }

        foreach (Rigidbody body in
                 root.GetComponentsInChildren<Rigidbody>(
                     true))
        {
            Object.DestroyImmediate(
                body);
        }

        foreach (MonoBehaviour behaviour in
                 root.GetComponentsInChildren<MonoBehaviour>(
                     true))
        {
            if (behaviour == null)
                continue;

            Object.DestroyImmediate(
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
