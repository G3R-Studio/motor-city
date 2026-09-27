using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CamaroVehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/Camaro/camaro.obj";

    private const string WheelSource =
        "Assets/VehicleAssets/Camaro/all_wheels.obj";

    private const string ColorTextureSource =
        "Assets/VehicleAssets/Camaro/Color.png";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string MaterialDirectory =
        OutputDirectory + "/CamaroMaterials";

    private const string OutputPrefab =
        OutputDirectory + "/Camaro.prefab";

    private const string BuildSessionKey =
        "MotorCity.CamaroVehicleBuilt.V1";

    static CamaroVehicleImporter()
    {
        EditorApplication.delayCall += TryAutoBuild;
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

        if (bodySource == null || wheelSource == null)
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Camaro body/wheel OBJ source is missing.");
            }

            return false;
        }

        Directory.CreateDirectory(OutputDirectory);
        Directory.CreateDirectory(MaterialDirectory);

        Texture2D colorTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                ColorTextureSource);

        if (colorTexture == null)
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Camaro Color.png texture is missing.");
            }

            return false;
        }

        // The source OBJ uses a single Color.png atlas for both the body and
        // wheel material. Keep the base tint white so the authored UV colors
        // remain visible instead of flattening the whole car to one red tone.
        Material paint = BuildMaterial(
            "CamaroBody",
            Color.white,
            0.68f,
            colorTexture,
            false);

        Material bloom = BuildMaterial(
            "CamaroBloom",
            Color.white,
            0.44f,
            colorTexture,
            true);

        Material wheel = BuildMaterial(
            "CamaroWheel",
            Color.white,
            0.42f,
            colorTexture,
            false);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(bodySource) as GameObject;

        if (instance == null)
            instance = Object.Instantiate(bodySource);

        if (instance == null)
            return false;

        instance.name = "Camaro";

        try
        {
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            StripImportedPhysics(instance);
            BuildWheelSet(instance.transform, wheelSource);
            AssignMaterials(instance, paint, bloom, wheel);
            EnsureRenderersEnabled(instance);

            GameObject saved =
                PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefab);

            if (saved == null)
                return false;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (verbose)
            {
                Debug.Log(
                    "Motor City: Camaro runtime visual rebuilt. " +
                    "Runtime path: MotorCity/Vehicles/Player/Camaro");
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
        // Body: 2.029 x 1.169 x 4.643 m, Z-forward.
        // Wheel mesh: 0.314 x 0.783 x 0.783 m.
        // Axle centers are measured from the authored wheel arches.
        CreateWheel(
            parent,
            wheelSource,
            "front_left",
            new Vector3(-0.855f, 0.392f, 1.340f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "front_right",
            new Vector3(0.855f, 0.392f, 1.340f),
            true);

        CreateWheel(
            parent,
            wheelSource,
            "rear_left",
            new Vector3(-0.855f, 0.392f, -1.335f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "rear_right",
            new Vector3(0.855f, 0.392f, -1.335f),
            true);
    }

    private static void CreateWheel(
        Transform parent,
        GameObject source,
        string name,
        Vector3 localPosition,
        bool oppositeSide)
    {
        GameObject holder = new GameObject(name);

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

        // Recenter the exported wheel mesh so steering/spin happens around the
        // true wheel centre rather than the tiny residual source-pivot offset.
        visual.transform.localPosition =
            new Vector3(-0.0145415f, -0.0004725f, 0.0001875f);

        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        StripImportedPhysics(visual);
    }

    private static Material BuildMaterial(
        string materialName,
        Color color,
        float smoothness,
        Texture2D baseTexture,
        bool emission)
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
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", baseTexture);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", baseTexture);

        if (emission)
        {
            if (material.HasProperty("_EmissionMap"))
                material.SetTexture("_EmissionMap", baseTexture);

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor(
                    "_EmissionColor",
                    new Color(1.35f, 1.35f, 1.35f, 1f));
            }

            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.black);

            material.DisableKeyword("_EMISSION");
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AssignMaterials(
        GameObject root,
        Material paint,
        Material bloom,
        Material wheel)
    {
        foreach (Renderer renderer in
                 root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            bool wheelRenderer =
                IsWheelHierarchy(renderer.transform);

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                string name =
                    materials[i] != null
                        ? materials[i].name.ToLowerInvariant()
                        : string.Empty;

                Material replacement = null;

                if (wheelRenderer)
                    replacement = wheel;
                else if (name.Contains("color_bloom"))
                    replacement = bloom;
                else if (name.Contains("color"))
                    replacement = paint;

                if (replacement == null)
                    continue;

                materials[i] = replacement;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }
    }

    private static bool IsWheelHierarchy(Transform item)
    {
        Transform cursor = item;

        while (cursor != null)
        {
            string name = cursor.name.ToLowerInvariant();

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

    private static void StripImportedPhysics(GameObject root)
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

    private static void EnsureRenderersEnabled(GameObject root)
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
