#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CubexCubeCityInstaller
{
    private const string SourceScene =
        "Assets/CubexCube - Free City Pack I/Scenes/Free_City_Scene_I.unity";

    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Environment";

    private const string RuntimeCityPrefab =
        RuntimeRoot + "/CityVisual.prefab";

    private const string RuntimeMaterialRoot =
        RuntimeRoot + "/CubexMaterials";

    private const string BuildVersion =
        "cubexcube-free-city-i-v1";

    private const string BuildVersionKey =
        "MotorCity.CubexCubeCity.BuildVersion";

    static CubexCubeCityInstaller()
    {
        EditorApplication.delayCall += Initialize;
    }

    [MenuItem("Motor City/Rebuild CubexCube City")]
    public static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void Initialize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                SourceScene) == null)
            return;

        bool currentBuild =
            EditorPrefs.GetString(
                BuildVersionKey,
                string.Empty) == BuildVersion;

        if (currentBuild &&
            AssetDatabase.LoadAssetAtPath<GameObject>(
                RuntimeCityPrefab) != null)
            return;

        Build(false);
    }

    private static void Build(
        bool force)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                SourceScene) == null)
        {
            if (force)
            {
                Debug.LogWarning(
                    "Motor City: CubexCube - Free City Pack I was not found. " +
                    "Import the package first.");
            }

            return;
        }

        if (AssetDatabase.IsValidFolder(
                RuntimeRoot))
        {
            AssetDatabase.DeleteAsset(
                RuntimeRoot);
        }

        Directory.CreateDirectory(
            RuntimeRoot);

        Scene previousScene =
            SceneManager.GetActiveScene();

        Scene sourceScene =
            EditorSceneManager.OpenScene(
                SourceScene,
                OpenSceneMode.Additive);

        GameObject wrapper =
            new("MotorCity_CubexCubeCity");

        try
        {
            foreach (GameObject root in
                     sourceScene.GetRootGameObjects())
            {
                if (ShouldSkipRoot(root))
                    continue;

                GameObject clone =
                    UnityEngine.Object.Instantiate(root);

                clone.name =
                    root.name;

                clone.transform.SetParent(
                    wrapper.transform,
                    true);
            }

            RemoveRuntimeConflicts(
                wrapper);

            ConvertMaterialsForUrp(
                wrapper);

            AddCityColliders(
                wrapper);

            MarkStatic(
                wrapper);

            if (wrapper.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                throw new InvalidOperationException(
                    "CubexCube city scene contains no renderers.");
            }

            PrefabUtility.SaveAsPrefabAsset(
                wrapper,
                RuntimeCityPrefab);

            AssetDatabase.SaveAssets();

            EditorPrefs.SetString(
                BuildVersionKey,
                BuildVersion);

            Debug.Log(
                "Motor City: CubexCube Free City Pack I is now the runtime city. " +
                "Gameplay layout uses hand-picked coordinates from the demo scene.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: failed to build CubexCube city. " +
                exception);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                wrapper);

            EditorSceneManager.CloseScene(
                sourceScene,
                true);

            if (previousScene.IsValid() &&
                previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(
                    previousScene);
            }

            AssetDatabase.Refresh();
        }
    }

    private static bool ShouldSkipRoot(
        GameObject root)
    {
        if (root == null)
            return true;

        string lower =
            root.name.ToLowerInvariant();

        return
            lower.Contains("camera") ||
            lower.Contains("directional light") ||
            lower == "light" ||
            lower.Contains("canvas") ||
            lower.Contains("eventsystem");
    }

    private static void RemoveRuntimeConflicts(
        GameObject root)
    {
        foreach (Camera camera in
                 root.GetComponentsInChildren<Camera>(true))
        {
            UnityEngine.Object.DestroyImmediate(
                camera.gameObject);
        }

        foreach (AudioListener listener in
                 root.GetComponentsInChildren<AudioListener>(true))
        {
            UnityEngine.Object.DestroyImmediate(
                listener);
        }

        foreach (Light light in
                 root.GetComponentsInChildren<Light>(true))
        {
            UnityEngine.Object.DestroyImmediate(
                light.gameObject);
        }
    }

    private static void AddCityColliders(
        GameObject root)
    {
        Transform roads =
            FindDirectChild(
                root.transform,
                "Roads");

        Transform roadsGrass =
            FindDirectChild(
                root.transform,
                "Roads_Grass");

        Transform roadsSand =
            FindDirectChild(
                root.transform,
                "Roads_Sand");

        Transform buildings =
            FindDirectChild(
                root.transform,
                "Buildings");

        AddMeshColliders(
            roads);

        AddMeshColliders(
            roadsGrass);

        AddMeshColliders(
            roadsSand);

        if (buildings != null)
        {
            foreach (Renderer renderer in
                     buildings.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null ||
                    renderer.GetComponent<Collider>() != null)
                    continue;

                BoxCollider collider =
                    renderer.gameObject.AddComponent<BoxCollider>();

                collider.center =
                    renderer.localBounds.center;

                collider.size =
                    renderer.localBounds.size;
            }
        }
    }

    private static void AddMeshColliders(
        Transform root)
    {
        if (root == null)
            return;

        foreach (MeshFilter filter in
                 root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter == null ||
                filter.sharedMesh == null)
                continue;

            if (filter.GetComponent<Collider>() != null)
                continue;

            MeshCollider collider =
                filter.gameObject.AddComponent<MeshCollider>();

            collider.sharedMesh =
                filter.sharedMesh;

            collider.convex =
                false;
        }
    }

    private static Transform FindDirectChild(
        Transform root,
        string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root)
        {
            if (string.Equals(
                    child.name,
                    childName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static void ConvertMaterialsForUrp(
        GameObject root)
    {
        Shader urpLit =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            Debug.LogError(
                "Motor City: URP/Lit shader was not found.");
            return;
        }

        Directory.CreateDirectory(
            RuntimeMaterialRoot);

        var converted =
            new Dictionary<Material, Material>();

        foreach (Renderer renderer in
                 root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] sourceMaterials =
                renderer.sharedMaterials;

            if (sourceMaterials == null ||
                sourceMaterials.Length == 0)
                continue;

            Material[] runtimeMaterials =
                new Material[sourceMaterials.Length];

            for (int i = 0;
                 i < sourceMaterials.Length;
                 i++)
            {
                Material source =
                    sourceMaterials[i];

                if (source == null)
                {
                    runtimeMaterials[i] =
                        null;
                    continue;
                }

                if (source.shader != null &&
                    source.shader.name.StartsWith(
                        "Universal Render Pipeline/",
                        StringComparison.Ordinal))
                {
                    runtimeMaterials[i] =
                        source;
                    continue;
                }

                if (!converted.TryGetValue(
                        source,
                        out Material runtime))
                {
                    runtime =
                        CreateUrpMaterial(
                            source,
                            urpLit,
                            converted.Count);

                    converted.Add(
                        source,
                        runtime);
                }

                runtimeMaterials[i] =
                    runtime;
            }

            renderer.sharedMaterials =
                runtimeMaterials;
        }

        Debug.Log(
            $"Motor City: converted {converted.Count} CubexCube materials to URP.");
    }

    private static Material CreateUrpMaterial(
        Material source,
        Shader urpLit,
        int index)
    {
        Material material =
            new(urpLit)
            {
                name =
                    "Cubex_" +
                    SanitizeFileName(
                        source.name) +
                    "_" +
                    index,
                enableInstancing =
                    true
            };

        Texture baseMap =
            GetTexture(
                source,
                "_BaseMap",
                "_MainTex");

        if (baseMap != null)
        {
            material.SetTexture(
                "_BaseMap",
                baseMap);

            string sourceProperty =
                source.HasProperty("_BaseMap")
                    ? "_BaseMap"
                    : "_MainTex";

            material.SetTextureScale(
                "_BaseMap",
                source.GetTextureScale(
                    sourceProperty));

            material.SetTextureOffset(
                "_BaseMap",
                source.GetTextureOffset(
                    sourceProperty));
        }

        Color color =
            source.HasProperty("_BaseColor")
                ? source.GetColor("_BaseColor")
                : source.HasProperty("_Color")
                    ? source.GetColor("_Color")
                    : Color.white;

        material.SetColor(
            "_BaseColor",
            color);

        if (source.HasProperty("_Metallic"))
        {
            material.SetFloat(
                "_Metallic",
                source.GetFloat("_Metallic"));
        }

        float smoothness =
            source.HasProperty("_Smoothness")
                ? source.GetFloat("_Smoothness")
                : source.HasProperty("_Glossiness")
                    ? source.GetFloat("_Glossiness")
                    : 0.25f;

        material.SetFloat(
            "_Smoothness",
            Mathf.Clamp01(
                smoothness));

        string path =
            RuntimeMaterialRoot +
            "/" +
            material.name +
            ".mat";

        AssetDatabase.CreateAsset(
            material,
            path);

        return material;
    }

    private static Texture GetTexture(
        Material material,
        params string[] properties)
    {
        foreach (string property in properties)
        {
            if (!material.HasProperty(property))
                continue;

            Texture texture =
                material.GetTexture(property);

            if (texture != null)
                return texture;
        }

        return null;
    }

    private static string SanitizeFileName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Material";

        char[] invalid =
            Path.GetInvalidFileNameChars();

        return new string(
            value
                .Select(
                    character =>
                        invalid.Contains(character)
                            ? '_'
                            : character)
                .ToArray());
    }

    private static void MarkStatic(
        GameObject root)
    {
        foreach (Transform item in
                 root.GetComponentsInChildren<Transform>(true))
        {
            item.gameObject.isStatic =
                true;
        }
    }
}
#endif
