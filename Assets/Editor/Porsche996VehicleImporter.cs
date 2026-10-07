using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Porsche996VehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/Porsche996/996.obj";

    private const string FrontWheelSource =
        "Assets/VehicleAssets/Porsche996/front_wheels.obj";

    private const string RearWheelSource =
        "Assets/VehicleAssets/Porsche996/rear_wheels.obj";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string MaterialDirectory =
        OutputDirectory + "/Porsche996Materials";

    private const string OutputPrefab =
        OutputDirectory + "/Porsche996.prefab";

    private const string BuildSessionKey =
        "MotorCity.Porsche996VehicleBuilt.V1";

    private const string SourceHashKey =
        "MotorCity.Porsche996VehicleSourceHash.V1";

    static Porsche996VehicleImporter()
    {
        EditorApplication.delayCall += TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Porsche996")]
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

    private static bool Build(bool verbose)
    {
        GameObject bodySource =
            AssetDatabase.LoadAssetAtPath<GameObject>(BodySource);

        GameObject frontWheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(FrontWheelSource);

        GameObject rearWheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(RearWheelSource);

        if (bodySource == null ||
            frontWheelSource == null ||
            rearWheelSource == null)
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Porsche996 body/front/rear OBJ source is missing.");
            }

            return false;
        }

        Directory.CreateDirectory(OutputDirectory);
        Directory.CreateDirectory(MaterialDirectory);

        Material paint = BuildMaterial(
            "Porsche996Body",
            new Color(0.665676f, 0.409637f, 0.000902f, 1f),
            0.72f);

        Material glass = BuildMaterial(
            "Porsche996Glass",
            new Color(0.015f, 0.018f, 0.024f, 1f),
            0.92f);

        Material chrome = BuildMaterial(
            "Porsche996Chrome",
            new Color(0.36f, 0.37f, 0.39f, 1f),
            0.82f);

        Material dark = BuildMaterial(
            "Porsche996Dark",
            new Color(0.018f, 0.018f, 0.02f, 1f),
            0.42f);

        Material plastic = BuildMaterial(
            "Porsche996Plastic",
            new Color(0.047f, 0.047f, 0.047f, 1f),
            0.36f);

        Material headlights = BuildMaterial(
            "Porsche996Headlights",
            new Color(0.8f, 0.8f, 0.8f, 1f),
            0.82f);

        Material indicators = BuildMaterial(
            "Porsche996Indicators",
            new Color(0.2509f, 0.0445f, 0.015f, 1f),
            0.50f);

        Material rearLights = BuildMaterial(
            "Porsche996RearLights",
            new Color(0.1191f, 0.0087f, 0.0087f, 1f),
            0.50f);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(bodySource) as GameObject;

        if (instance == null)
            instance = Object.Instantiate(bodySource);

        if (instance == null)
            return false;

        instance.name = "Porsche996";

        try
        {
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            StripImportedPhysics(instance);

            BuildWheelSet(
                instance.transform,
                frontWheelSource,
                rearWheelSource);

            // Keep body/body_misc material slots authored in OBJ/MTL.
            // The shared runtime installer upgrades them for URP, matching Beatall.

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
                    "Motor City: Porsche996 runtime visual rebuilt. " +
                    "Runtime path: MotorCity/Vehicles/Player/Porsche996");
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
        GameObject frontSource,
        GameObject rearSource)
    {
        // Source body is 4.204 m long and Z-forward. Both wheel exports are
        // already centered on their pivots and measure 0.664 m in diameter.
        // The axle centers below were measured from the authored wheel arches.
        // X is pulled 4.5 cm inward per side so the tyres sit inside the body
        // instead of protruding past the fenders.
        CreateWheel(
            parent,
            frontSource,
            "front_left",
            new Vector3(-0.700f, 0.332f, 1.165f),
            false);

        CreateWheel(
            parent,
            frontSource,
            "front_right",
            new Vector3(0.700f, 0.332f, 1.165f),
            true);

        CreateWheel(
            parent,
            rearSource,
            "rear_left",
            new Vector3(-0.700f, 0.332f, -1.070f),
            false);

        CreateWheel(
            parent,
            rearSource,
            "rear_right",
            new Vector3(0.700f, 0.332f, -1.070f),
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
        visual.transform.localPosition =
            new Vector3(0.0000655f, 0f, 0f);
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
        Material indicators,
        Material rearLights)
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
                else if (name.Contains("indicator"))
                    replacement = indicators;
                else if (name.Contains("rearlight"))
                    replacement = rearLights;
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
