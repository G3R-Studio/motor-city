using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ToyotaAE86VehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/ToyotaAE86/ae86.obj";

    private const string WheelSource =
        "Assets/VehicleAssets/ToyotaAE86/all_wheels.obj";
    private const string StandardFrontWheelSource =
        "Assets/VehicleAssets/ToyotaAE86/front_wheels.obj";

    private const string StandardRearWheelSource =
        "Assets/VehicleAssets/ToyotaAE86/rear_wheels.obj";


    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string MaterialDirectory =
        OutputDirectory + "/ToyotaAE86Materials";

    private const string OutputPrefab =
        OutputDirectory + "/ToyotaAE86.prefab";

    private const string BuildSessionKey =
        "MotorCity.ToyotaAE86VehicleBuilt.V1";

    private const string SourceHashKey =
        "MotorCity.ToyotaAE86VehicleSourceHash.V1";

    static ToyotaAE86VehicleImporter()
    {
        EditorApplication.delayCall += TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild ToyotaAE86")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        string dependencyHash =
            StandardVehicleImportUtility.DependencyHash(
                BodySource,
                    StandardFrontWheelSource,
                    StandardRearWheelSource,
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

    private static bool Build(bool verbose)
    {
        GameObject bodySource =
            AssetDatabase.LoadAssetAtPath<GameObject>(BodySource);

        GameObject standardFrontWheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                StandardFrontWheelSource);

        GameObject standardRearWheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                StandardRearWheelSource);

        bool useStandardWheelLayout =
            standardFrontWheelSource != null &&
            standardRearWheelSource != null;

        GameObject wheelSource =
            useStandardWheelLayout
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(
                    WheelSource);

        if (bodySource == null ||
            (!useStandardWheelLayout &&
             wheelSource == null))
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: ToyotaAE86 body/wheel OBJ source is missing.");
            }

            return false;
        }

        Directory.CreateDirectory(OutputDirectory);
        Directory.CreateDirectory(MaterialDirectory);

        Material paint = BuildMaterial(
            "ToyotaAE86Body",
            new Color(0.665676f, 0.409637f, 0.000902f, 1f),
            0.62f);

        Material glass = BuildMaterial(
            "ToyotaAE86Glass",
            new Color(0.015f, 0.018f, 0.024f, 1f),
            0.90f);

        Material chrome = BuildMaterial(
            "ToyotaAE86Chrome",
            new Color(0.30f, 0.31f, 0.33f, 1f),
            0.76f);

        Material dark = BuildMaterial(
            "ToyotaAE86Dark",
            new Color(0.012f, 0.012f, 0.014f, 1f),
            0.34f);

        Material plastic = BuildMaterial(
            "ToyotaAE86Plastic",
            new Color(0.047f, 0.047f, 0.047f, 1f),
            0.32f);

        Material indicators = BuildMaterial(
            "ToyotaAE86Indicators",
            new Color(0.2509f, 0.0445f, 0.015f, 1f),
            0.46f);

        Material rearLights = BuildMaterial(
            "ToyotaAE86RearLights",
            new Color(0.119f, 0.009f, 0.009f, 1f),
            0.46f);

        Material rearPlate = BuildMaterial(
            "ToyotaAE86RearPlate",
            new Color(0.80f, 0.347f, 0.062f, 1f),
            0.25f);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(bodySource) as GameObject;

        if (instance == null)
            instance = Object.Instantiate(bodySource);

        if (instance == null)
            return false;

        instance.name = "ToyotaAE86";

        try
        {
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            StripImportedPhysics(instance);
            if (useStandardWheelLayout)
            {
                StandardVehicleImportUtility.BuildStandardWheelSet(
                    instance.transform,
                    standardFrontWheelSource,
                    standardRearWheelSource,
                    new Vector3(-0.655f, 0.303f, 1.300f),
                    new Vector3(0.655f, 0.303f, 1.300f),
                    new Vector3(-0.655f, 0.303f, -1.170f),
                    new Vector3(0.655f, 0.303f, -1.170f));
            }
            else
            {
                BuildWheelSet(
                    instance.transform,
                    wheelSource);
            }

            if (!StandardVehicleImportUtility.UsesStandardBodyLayout(
                    instance))
            {
            AssignMaterials(
                instance,
                paint,
                glass,
                chrome,
                dark,
                plastic,
                indicators,
                rearLights,
                rearPlate);
            }

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
                    "Motor City: ToyotaAE86 runtime visual rebuilt. " +
                    "Runtime path: MotorCity/Vehicles/Player/ToyotaAE86");
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
        // Body: 1.863 x 1.157 x 4.312 m, Z-forward.
        // Wheel mesh: 0.245 x 0.606 x 0.606 m and centered on its pivot.
        // Axle centers below are measured from the authored wheel arches.
        CreateWheel(
            parent,
            wheelSource,
            "front_left",
            new Vector3(-0.655f, 0.303f, 1.300f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "front_right",
            new Vector3(0.655f, 0.303f, 1.300f),
            true);

        CreateWheel(
            parent,
            wheelSource,
            "rear_left",
            new Vector3(-0.655f, 0.303f, -1.170f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "rear_right",
            new Vector3(0.655f, 0.303f, -1.170f),
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
        // Keep the mesh centered on the steering holder. The inward
        // offset belongs on the holder itself; otherwise the visual orbits
        // around an off-centre pivot when the front wheels steer.
        visual.transform.localPosition =
            Vector3.zero;
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
        Material indicators,
        Material rearLights,
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
                else if (name.Contains("indicator"))
                    replacement = indicators;
                else if (name.Contains("rearlight"))
                    replacement = rearLights;
                else if (name.Contains("yellowplate"))
                    replacement = rearPlate;
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
