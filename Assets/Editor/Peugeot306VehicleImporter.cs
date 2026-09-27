using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Peugeot306VehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/Peugeot306/306.obj";

    private const string WheelSource =
        "Assets/VehicleAssets/Peugeot306/all_wheels.obj";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string MaterialDirectory =
        OutputDirectory + "/Peugeot306Materials";

    private const string OutputPrefab =
        OutputDirectory + "/Peugeot306.prefab";

    private const string BuildSessionKey =
        "MotorCity.Peugeot306VehicleBuilt.V1";

    static Peugeot306VehicleImporter()
    {
        EditorApplication.delayCall += TryAutoBuild;
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
                    "Motor City: Peugeot306 body/wheel OBJ source is missing.");
            }

            return false;
        }

        Directory.CreateDirectory(OutputDirectory);
        Directory.CreateDirectory(MaterialDirectory);

        Material paint = BuildMaterial(
            "Peugeot306Body",
            new Color(0.665676f, 0.409637f, 0.000902f, 1f),
            0.64f);

        Material glass = BuildMaterial(
            "Peugeot306Glass",
            new Color(0.015f, 0.018f, 0.024f, 1f),
            0.90f);

        Material chrome = BuildMaterial(
            "Peugeot306Chrome",
            new Color(0.31f, 0.32f, 0.34f, 1f),
            0.78f);

        Material dark = BuildMaterial(
            "Peugeot306Dark",
            new Color(0.012f, 0.012f, 0.014f, 1f),
            0.34f);

        Material plastic = BuildMaterial(
            "Peugeot306Plastic",
            new Color(0.047f, 0.047f, 0.047f, 1f),
            0.32f);

        Material headlights = BuildMaterial(
            "Peugeot306Headlights",
            new Color(0.80f, 0.80f, 0.80f, 1f),
            0.76f);

        Material rearLights = BuildMaterial(
            "Peugeot306RearLights",
            new Color(0.119f, 0.009f, 0.009f, 1f),
            0.46f);

        Material frontPlate = BuildMaterial(
            "Peugeot306FrontPlate",
            new Color(0.80f, 0.80f, 0.80f, 1f),
            0.25f);

        Material rearPlate = BuildMaterial(
            "Peugeot306RearPlate",
            new Color(0.80f, 0.347f, 0.062f, 1f),
            0.25f);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(bodySource) as GameObject;

        if (instance == null)
            instance = Object.Instantiate(bodySource);

        if (instance == null)
            return false;

        instance.name = "Peugeot306";

        try
        {
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            StripImportedPhysics(instance);
            BuildWheelSet(instance.transform, wheelSource);

            AssignMaterials(
                instance,
                paint,
                glass,
                chrome,
                dark,
                plastic,
                headlights,
                rearLights,
                frontPlate,
                rearPlate);

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
                    "Motor City: Peugeot306 runtime visual rebuilt. " +
                    "Runtime path: MotorCity/Vehicles/Player/Peugeot306");
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
        // Body: 1.954 x 1.293 x 4.209 m, Z-forward.
        // Wheel mesh: 0.292 x 0.672 x 0.672 m and nearly centered on its pivot.
        // Axle centers are measured from the authored wheel arches.
        CreateWheel(
            parent,
            wheelSource,
            "front_left",
            new Vector3(-0.780f, 0.336f, 1.240f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "front_right",
            new Vector3(0.780f, 0.336f, 1.240f),
            true);

        CreateWheel(
            parent,
            wheelSource,
            "rear_left",
            new Vector3(-0.780f, 0.336f, -1.395f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "rear_right",
            new Vector3(0.780f, 0.336f, -1.395f),
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

        // Cancel the tiny residual X offset of the exported wheel pivot.
        visual.transform.localPosition =
            new Vector3(0.0012375f, 0f, 0f);

        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        StripImportedPhysics(visual);
    }

    private static Material BuildMaterial(
        string materialName,
        Color color,
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

        material.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AssignMaterials(
        GameObject root,
        Material paint,
        Material glass,
        Material chrome,
        Material dark,
        Material plastic,
        Material headlights,
        Material rearLights,
        Material frontPlate,
        Material rearPlate)
    {
        foreach (Renderer renderer in
                 root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                string name =
                    materials[i] != null
                        ? materials[i].name.ToLowerInvariant()
                        : string.Empty;

                Material replacement = null;

                if (name.Contains("blackglass"))
                    replacement = glass;
                else if (name.Contains("carpaint"))
                    replacement = paint;
                else if (name.Contains("chrome"))
                    replacement = chrome;
                else if (name.Contains("headlight"))
                    replacement = headlights;
                else if (name.Contains("rearlight"))
                    replacement = rearLights;
                else if (name.Contains("yellowplate"))
                    replacement = rearPlate;
                else if (name.Contains("numberplate"))
                    replacement = frontPlate;
                else if (name.Contains("plastic"))
                    replacement = plastic;
                else if (name.Contains("empty"))
                    replacement = dark;

                if (replacement == null)
                    continue;

                materials[i] = replacement;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }
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
