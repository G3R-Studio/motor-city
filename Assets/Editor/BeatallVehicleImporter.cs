using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BeatallVehicleImporter
{
    private const string SourceModel =
        "Assets/VehicleAssets/Beatall/beatall.obj";

    private const string SourceTexture =
        "Assets/VehicleAssets/Beatall/all.png";

    private const string SourceWheelModel =
        "Assets/VehicleAssets/Beatall/beatall_wheels.obj";

    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles/Player";

    private const string MaterialDirectory =
        OutputDirectory + "/BeatallMaterials";

    private const string OutputPrefab =
        OutputDirectory + "/Beatall.prefab";

    private const string BuildSessionKey =
        "MotorCity.BeatallVehicleBuilt.V2";

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
        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                SourceModel);

        GameObject wheelSource =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                SourceWheelModel);

        if (source == null ||
            wheelSource == null)
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: Beatall body or wheel OBJ is missing.");
            }

            return false;
        }

        Directory.CreateDirectory(
            OutputDirectory);

        Directory.CreateDirectory(
            MaterialDirectory);

        Material bodyMaterial =
            BuildMaterial(
                "BeatallBody",
                new Color(0.92f, 0.92f, 0.92f, 1f),
                0.55f,
                false);

        Material glassMaterial =
            BuildMaterial(
                "BeatallGlass",
                new Color(0.025f, 0.03f, 0.04f, 1f),
                0.88f,
                false);

        Material emissionMaterial =
            BuildMaterial(
                "BeatallEmission",
                Color.white,
                0.35f,
                true);

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

            BuildWheelSet(
                instance.transform,
                wheelSource);

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
        {
            shader =
                Shader.Find(
                    "Standard");
        }

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

        if (material.HasProperty(
                "_BaseMap"))
        {
            material.SetTexture(
                "_BaseMap",
                texture);
        }

        if (material.HasProperty(
                "_MainTex"))
        {
            material.SetTexture(
                "_MainTex",
                texture);
        }

        if (material.HasProperty(
                "_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                baseColor);
        }

        if (material.HasProperty(
                "_Color"))
        {
            material.SetColor(
                "_Color",
                baseColor);
        }

        if (material.HasProperty(
                "_Smoothness"))
        {
            material.SetFloat(
                "_Smoothness",
                smoothness);
        }

        if (emission)
        {
            Color emissionColor =
                Color.white * 1.6f;

            if (material.HasProperty(
                    "_EmissionMap"))
            {
                material.SetTexture(
                    "_EmissionMap",
                    texture);
            }

            if (material.HasProperty(
                    "_EmissionColor"))
            {
                material.SetColor(
                    "_EmissionColor",
                    emissionColor);
            }

            material.EnableKeyword(
                "_EMISSION");

            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.None;
        }
        else
        {
            material.DisableKeyword(
                "_EMISSION");
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

            bool changed =
                false;

            for (int i = 0;
                 i < materials.Length;
                 i++)
            {
                string name =
                    materials[i] != null
                        ? materials[i].name.ToLowerInvariant()
                        : string.Empty;

                if (name.Contains(
                        "blackglass"))
                {
                    materials[i] =
                        glass;
                    changed =
                        true;
                }
                else if (name.Contains(
                             "emmisive") ||
                         name.Contains(
                             "emissive"))
                {
                    materials[i] =
                        emission;
                    changed =
                        true;
                }
                else if (name.Contains(
                             "basegradient"))
                {
                    materials[i] =
                        body;
                    changed =
                        true;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials =
                    materials;
            }
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

        // These are the original authored Beatall wheel centers from the
        // source model before the wheels were exported to their own OBJ.
        // The body itself is not translated, rotated or re-scaled here.
        CreateWheel(
            parent,
            wheelSource,
            "front_left",
            new Vector3(
                -0.573373f,
                0.2620855f,
                1.057121f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "front_right",
            new Vector3(
                0.573373f,
                0.2620855f,
                1.057121f),
            true);

        CreateWheel(
            parent,
            wheelSource,
            "rear_left",
            new Vector3(
                -0.600407f,
                0.2620855f,
                -0.968537f),
            false);

        CreateWheel(
            parent,
            wheelSource,
            "rear_right",
            new Vector3(
                0.600407f,
                0.2620855f,
                -0.968537f),
            true);
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

        // The supplied wheel is the authored FRONT-LEFT wheel.
        // Keep that exact orientation for both left wheels. Right-side
        // wheels need only face across the car; rotating around Y preserves
        // wheel up/down instead of rolling the mesh upside-down.
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
