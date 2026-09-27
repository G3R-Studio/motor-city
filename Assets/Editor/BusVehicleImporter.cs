using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BusVehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/Bus/bus.obj";

    private const string WheelSource =
        "Assets/VehicleAssets/Bus/all_wheels.obj";

    private const string PaletteSource =
        "Assets/VehicleAssets/Bus/citytransportpalette.png";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string MaterialDirectory =
        OutputDirectory + "/BusMaterials";

    private const string OutputPrefab =
        OutputDirectory + "/Bus.prefab";

    private const string BuildSessionKey =
        "MotorCity.BusVehicleBuilt.V1";

    static BusVehicleImporter()
    {
        EditorApplication.delayCall += TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Bus")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefab) != null)
            return;

        if (SessionState.GetBool(BuildSessionKey, false))
            return;

        SessionState.SetBool(BuildSessionKey, true);
        Build(false);
    }

    private static bool Build(bool verbose)
    {
        GameObject bodySource =
            AssetDatabase.LoadAssetAtPath<GameObject>(BodySource);

        GameObject wheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(WheelSource);

        Texture2D palette =
            AssetDatabase.LoadAssetAtPath<Texture2D>(PaletteSource);

        if (bodySource == null ||
            wheelSource == null ||
            palette == null)
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Bus body/wheel OBJ or citytransportpalette.png is missing.");
            }

            return false;
        }

        Directory.CreateDirectory(OutputDirectory);
        Directory.CreateDirectory(MaterialDirectory);

        // Both source meshes use the same UV palette. Keep tint white so the
        // authored bus colors, glass, lights and trim remain exactly as baked
        // into citytransportpalette.png.
        Material bodyMaterial =
            BuildAtlasMaterial(
                "BusAtlas",
                palette,
                0.48f);

        Material wheelMaterial =
            BuildAtlasMaterial(
                "BusWheelAtlas",
                palette,
                0.38f);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(bodySource) as GameObject;

        if (instance == null)
            instance = Object.Instantiate(bodySource);

        if (instance == null)
            return false;

        instance.name = "Bus";

        try
        {
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale =
                Vector3.one * 1.25f;

            StripImportedPhysics(instance);
            BuildWheelSet(instance.transform, wheelSource);
            AssignMaterials(instance, bodyMaterial, wheelMaterial);
            EnsureRenderersEnabled(instance);

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
                    "Motor City: Bus runtime visual rebuilt. " +
                    "Runtime path: MotorCity/Vehicles/Player/Bus");
            }

            return true;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void BuildWheelSet(
        Transform parent,
        GameObject wheelSource)
    {
        // Authored dimensions:
        // body 2.012 x 1.934 x 6.350 m, Z-forward;
        // wheel 0.210 x 0.681 x 0.681 m.
        // Axle centres are measured from the body wheel arches.
        CreateWheel(
            parent,
            wheelSource,
            "front_left",
            new Vector3(0.71f, 0.340f, 1.765f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "front_right",
            new Vector3(-0.71f, 0.340f, 1.765f),
            true);

        CreateWheel(
            parent,
            wheelSource,
            "rear_left",
            new Vector3(0.71f, 0.340f, -2.045f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "rear_right",
            new Vector3(-0.71f, 0.340f, -2.045f),
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
            new GameObject(name);

        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localPosition;
        holder.transform.localRotation =
            oppositeSide
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);
        holder.transform.localScale = Vector3.one;

        GameObject visual =
            PrefabUtility.InstantiatePrefab(
                source,
                holder.transform) as GameObject;

        if (visual == null)
            visual = Object.Instantiate(source, holder.transform);

        if (visual == null)
            return;

        visual.name = name + "_visual";

        // Cancel the tiny source-pivot residual while keeping the wheel mesh
        // centred on the steering holder.
        visual.transform.localPosition =
            new Vector3(
                -0.002288f,
                0.000979f,
                0.000017f);

        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        StripImportedPhysics(visual);
    }

    private static Material BuildAtlasMaterial(
        string materialName,
        Texture2D palette,
        float smoothness)
    {
        string path =
            MaterialDirectory + "/" + materialName + ".mat";

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(path);

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            material.name = materialName;
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null &&
                 material.shader != shader)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", palette);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", palette);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", Color.black);

        material.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);

        return material;
    }

    private static void AssignMaterials(
        GameObject root,
        Material bodyMaterial,
        Material wheelMaterial)
    {
        foreach (Renderer renderer in
                 root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            bool wheelRenderer =
                IsWheelHierarchy(renderer.transform);

            Material[] materials =
                renderer.sharedMaterials;

            for (int i = 0;
                 i < materials.Length;
                 i++)
            {
                materials[i] =
                    wheelRenderer
                        ? wheelMaterial
                        : bodyMaterial;
            }

            renderer.sharedMaterials =
                materials;
        }
    }

    private static bool IsWheelHierarchy(
        Transform item)
    {
        Transform cursor = item;

        while (cursor != null)
        {
            string name =
                cursor.name.ToLowerInvariant();

            if (name.Contains("front_left") ||
                name.Contains("front_right") ||
                name.Contains("rear_left") ||
                name.Contains("rear_right") ||
                name.Contains("all_wheels"))
            {
                return true;
            }

            cursor = cursor.parent;
        }

        return false;
    }

    private static void StripImportedPhysics(
        GameObject root)
    {
        foreach (Collider collider in
                 root.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }

        foreach (Rigidbody body in
                 root.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(body);
        }

        foreach (MonoBehaviour behaviour in
                 root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null)
                Object.DestroyImmediate(behaviour);
        }
    }

    private static void EnsureRenderersEnabled(
        GameObject root)
    {
        foreach (Renderer renderer in
                 root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            renderer.enabled = true;

            if (!renderer.gameObject.activeSelf)
                renderer.gameObject.SetActive(true);
        }
    }
}
