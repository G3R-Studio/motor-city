using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DeloreanVehicleImporter
{
    private const string BodySource =
        "Assets/VehicleAssets/Delorean/delorean.obj";

    private const string FrontWheelSource =
        "Assets/VehicleAssets/Delorean/front_wheels.obj";

    private const string RearWheelSource =
        "Assets/VehicleAssets/Delorean/rear_wheels.obj";

    private const string SourceTexture =
        "Assets/VehicleAssets/Delorean/all.png";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string MaterialDirectory =
        OutputDirectory + "/DeloreanMaterials";

    private const string OutputPrefab =
        OutputDirectory + "/Delorean.prefab";

    private const string BuildSessionKey =
        "MotorCity.DeloreanVehicleBuilt.V1";

    static DeloreanVehicleImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Delorean")]
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
                    "Motor City: Delorean body/front/rear OBJ source is missing.");
            }

            return false;
        }

        Directory.CreateDirectory(
            OutputDirectory);

        Directory.CreateDirectory(
            MaterialDirectory);

        Material bodyMaterial =
            BuildMaterial(
                "DeloreanBody",
                new Color(0.72f, 0.74f, 0.76f, 1f),
                0.65f,
                false);

        Material glassMaterial =
            BuildMaterial(
                "DeloreanGlass",
                new Color(0.025f, 0.03f, 0.04f, 1f),
                0.9f,
                false);

        Material emissionMaterial =
            BuildMaterial(
                "DeloreanEmission",
                Color.white,
                0.35f,
                true);

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
            "Delorean";

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

            BuildWheelSet(
                instance.transform,
                frontWheelSource,
                rearWheelSource);

            AssignMaterials(
                instance,
                bodyMaterial,
                glassMaterial,
                emissionMaterial);

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
                    "Motor City: Delorean runtime visual rebuilt. " +
                    "Runtime path: MotorCity/Vehicles/Player/Delorean");
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
        // Corrected Delorean export is already Z-forward.
        // Wheel source meshes are centered at their own pivots, so only the
        // four authored wheel-center locations are applied here. Runtime
        // WheelColliders are still generated automatically from these meshes.
        CreateWheel(
            parent,
            frontSource,
            "front_left",
            new Vector3(
                -0.980f,
                0.33646f,
                1.445f),
            false);

        CreateWheel(
            parent,
            frontSource,
            "front_right",
            new Vector3(
                0.980f,
                0.33646f,
                1.445f),
            true);

        CreateWheel(
            parent,
            rearSource,
            "rear_left",
            new Vector3(
                -0.967f,
                0.37497f,
                -1.185f),
            false);

        CreateWheel(
            parent,
            rearSource,
            "rear_right",
            new Vector3(
                0.967f,
                0.37497f,
                -1.185f),
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

        visual.transform.localPosition =
            Vector3.zero;
        visual.transform.localRotation =
            Quaternion.identity;
        visual.transform.localScale =
            Vector3.one;

        StripImportedPhysics(
            visual);
    }

    private static Material BuildMaterial(
        string materialName,
        Color baseColor,
        float smoothness,
        bool emission)
    {
        string path =
            MaterialDirectory +
            "/" +
            materialName +
            ".mat";

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                path);

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (material == null)
        {
            material =
                new Material(
                    shader);

            material.name =
                materialName;

            AssetDatabase.CreateAsset(
                material,
                path);
        }
        else if (shader != null &&
                 material.shader != shader)
        {
            material.shader =
                shader;
        }

        Texture texture =
            AssetDatabase.LoadAssetAtPath<Texture>(
                SourceTexture);

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", baseColor);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", baseColor);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        if (emission)
        {
            if (material.HasProperty("_EmissionMap"))
                material.SetTexture("_EmissionMap", texture);

            if (material.HasProperty("_EmissionColor"))
                material.SetColor(
                    "_EmissionColor",
                    Color.white * 1.6f);

            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.None;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
        }

        EditorUtility.SetDirty(
            material);

        return material;
    }

    private static void AssignMaterials(
        GameObject root,
        Material body,
        Material glass,
        Material emission)
    {
        foreach (Renderer renderer in
                 root.GetComponentsInChildren<Renderer>(
                     true))
        {
            if (renderer == null)
                continue;

            Material[] materials =
                renderer.sharedMaterials;

            bool changed = false;

            for (int i = 0;
                 i < materials.Length;
                 i++)
            {
                string materialName =
                    materials[i] != null
                        ? materials[i].name.ToLowerInvariant()
                        : string.Empty;

                if (materialName.Contains("blackglass"))
                {
                    materials[i] = glass;
                    changed = true;
                }
                else if (materialName.Contains("emmisive") ||
                         materialName.Contains("emissive"))
                {
                    materials[i] = emission;
                    changed = true;
                }
                else if (materialName.Contains("basegradient"))
                {
                    materials[i] = body;
                    changed = true;
                }
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }
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

    private static void EnsureRenderersEnabled(
        GameObject root)
    {
        foreach (Renderer renderer in
                 root.GetComponentsInChildren<Renderer>(
                     true))
        {
            if (renderer == null)
                continue;

            renderer.enabled = true;

            if (!renderer.gameObject.activeSelf)
                renderer.gameObject.SetActive(true);
        }
    }
}
