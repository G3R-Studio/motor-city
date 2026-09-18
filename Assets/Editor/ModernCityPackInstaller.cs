#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class ModernCityPackInstaller
{
    private const string SourceScene =
        "Assets/MCP/Demo/Scenes/mcp_day.unity";

    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Environment";

    private const string RuntimeCityPrefab =
        RuntimeRoot + "/CityVisual.prefab";

    private const string RuntimeMaterialRoot =
        RuntimeRoot + "/ModernCityMaterials";

    private const string BuildVersion =
        "modern-city-pack-day-v1";

    private const string BuildVersionKey =
        "MotorCity.ModernCityPack.BuildVersion";

    private static readonly HashSet<string> IncludedRoots =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "- decals",
            "- buildings",
            "- distant_buildings",
            "- roads",
            "- street props",
            "- street lights",
            "- terrain",
            "- traffic lights",
            "- trees"
        };

    static ModernCityPackInstaller()
    {
        EditorApplication.delayCall += Initialize;
    }

    [MenuItem("Motor City/Rebuild Modern City Pack")]
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
                    "Motor City: Modern City Pack was not found at Assets/MCP. " +
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
            RuntimeMaterialRoot);

        Scene previousScene =
            SceneManager.GetActiveScene();

        Scene sourceScene =
            EditorSceneManager.OpenScene(
                SourceScene,
                OpenSceneMode.Additive);

        GameObject wrapper =
            new("MotorCity_ModernCity");

        try
        {
            foreach (GameObject root in
                     sourceScene.GetRootGameObjects())
            {
                if (!IncludedRoots.Contains(
                        root.name))
                    continue;

                GameObject clone =
                    UnityEngine.Object.Instantiate(
                        root);

                clone.name =
                    root.name;

                clone.transform.SetParent(
                    wrapper.transform,
                    true);
            }

            StripLegacyRuntimeComponents(
                wrapper);

            ConvertMaterialsForUrp(
                wrapper);

            EnsureRoadAndTerrainColliders(
                wrapper);

            MarkStatic(
                wrapper);

            int rendererCount =
                wrapper.GetComponentsInChildren<Renderer>(true).Length;

            if (rendererCount == 0)
            {
                throw new InvalidOperationException(
                    "Modern City Pack day scene produced no renderers.");
            }

            PrefabUtility.SaveAsPrefabAsset(
                wrapper,
                RuntimeCityPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorPrefs.SetString(
                BuildVersionKey,
                BuildVersion);

            Debug.Log(
                "Motor City: Modern City Pack day scene is now the runtime city. " +
                $"Copied {rendererCount} renderers from the selected environment roots.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: failed to build Modern City Pack runtime city. " +
                exception);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                wrapper);

            if (sourceScene.IsValid() &&
                sourceScene.isLoaded)
            {
                EditorSceneManager.CloseScene(
                    sourceScene,
                    true);
            }

            if (previousScene.IsValid() &&
                previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(
                    previousScene);
            }

            EditorUtility.ClearProgressBar();
        }
    }

    private static void StripLegacyRuntimeComponents(
        GameObject root)
    {
        Component[] components =
            root.GetComponentsInChildren<Component>(true);

        foreach (Component component in components)
        {
            if (component == null ||
                component is Transform ||
                component is Renderer ||
                component is MeshFilter ||
                component is Collider ||
                component is LODGroup)
                continue;

            bool remove =
                component is MonoBehaviour ||
                component is Camera ||
                component is AudioListener ||
                component is AudioSource ||
                component is Light ||
                component is Rigidbody ||
                component is CharacterController ||
                component is ParticleSystem ||
                component.GetType().Name == "FlareLayer";

            if (!remove)
                continue;

            UnityEngine.Object.DestroyImmediate(
                component);
        }
    }

    private static void EnsureRoadAndTerrainColliders(
        GameObject root)
    {
        foreach (string rootName in
                 new[] { "- roads", "- terrain" })
        {
            Transform target =
                FindChildRecursive(
                    root.transform,
                    rootName);

            if (target == null)
                continue;

            foreach (MeshFilter filter in
                     target.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null ||
                    filter.sharedMesh == null ||
                    filter.GetComponent<Collider>() != null)
                    continue;

                MeshCollider collider =
                    filter.gameObject.AddComponent<MeshCollider>();

                collider.sharedMesh =
                    filter.sharedMesh;

                collider.convex =
                    false;
            }
        }
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

        var converted =
            new Dictionary<Material, Material>();

        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        for (int rendererIndex = 0;
             rendererIndex < renderers.Length;
             rendererIndex++)
        {
            Renderer renderer =
                renderers[rendererIndex];

            EditorUtility.DisplayProgressBar(
                "Motor City",
                "Converting Modern City Pack materials...",
                renderers.Length > 0
                    ? (rendererIndex + 1f) / renderers.Length
                    : 1f);

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
            $"Motor City: converted {converted.Count} Modern City Pack materials to URP.");
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
                    "MCP_" +
                    SanitizeFileName(
                        source.name) +
                    "_" +
                    index,
                enableInstancing =
                    true
            };

        CopyBaseMap(
            source,
            material);

        Color baseColor =
            source.HasProperty("_Color")
                ? source.GetColor("_Color")
                : Color.white;

        if (material.HasProperty("_BaseColor"))
            material.SetColor(
                "_BaseColor",
                baseColor);

        Texture normal =
            GetTexture(
                source,
                "_BumpMap",
                "_NormalMap");

        if (normal != null &&
            material.HasProperty("_BumpMap"))
        {
            material.SetTexture(
                "_BumpMap",
                normal);

            material.EnableKeyword(
                "_NORMALMAP");
        }

        float smoothness =
            source.HasProperty("_Glossiness")
                ? source.GetFloat("_Glossiness")
                : source.HasProperty("_Shininess")
                    ? source.GetFloat("_Shininess")
                    : 0.28f;

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat(
                "_Smoothness",
                Mathf.Clamp01(
                    smoothness));
        }

        string shaderName =
            source.shader != null
                ? source.shader.name
                : string.Empty;

        bool isCutout =
            shaderName.IndexOf(
                "cutout",
                StringComparison.OrdinalIgnoreCase) >= 0 ||
            source.renderQueue == (int)RenderQueue.AlphaTest;

        bool isTransparent =
            !isCutout &&
            (shaderName.IndexOf(
                 "transparent",
                 StringComparison.OrdinalIgnoreCase) >= 0 ||
             source.renderQueue >= (int)RenderQueue.Transparent);

        if (isCutout)
        {
            material.SetFloat(
                "_AlphaClip",
                1f);

            material.SetFloat(
                "_Cutoff",
                source.HasProperty("_Cutoff")
                    ? source.GetFloat("_Cutoff")
                    : 0.45f);

            material.EnableKeyword(
                "_ALPHATEST_ON");

            material.renderQueue =
                (int)RenderQueue.AlphaTest;
        }
        else if (isTransparent)
        {
            material.SetFloat(
                "_Surface",
                1f);

            material.SetFloat(
                "_ZWrite",
                0f);

            material.SetOverrideTag(
                "RenderType",
                "Transparent");

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");

            material.renderQueue =
                (int)RenderQueue.Transparent;
        }

        bool selfIlluminated =
            shaderName.IndexOf(
                "self-illumin",
                StringComparison.OrdinalIgnoreCase) >= 0;

        Texture emission =
            GetTexture(
                source,
                "_Illum",
                "_EmissionMap");

        if (selfIlluminated ||
            emission != null)
        {
            if (emission != null &&
                material.HasProperty("_EmissionMap"))
            {
                material.SetTexture(
                    "_EmissionMap",
                    emission);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor(
                    "_EmissionColor",
                    Color.white * 0.8f);
            }

            material.EnableKeyword(
                "_EMISSION");
        }

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

    private static void CopyBaseMap(
        Material source,
        Material destination)
    {
        string sourceProperty =
            source.HasProperty("_MainTex")
                ? "_MainTex"
                : source.HasProperty("_BaseMap")
                    ? "_BaseMap"
                    : null;

        if (sourceProperty == null)
            return;

        Texture texture =
            source.GetTexture(
                sourceProperty);

        if (texture == null ||
            !destination.HasProperty("_BaseMap"))
            return;

        destination.SetTexture(
            "_BaseMap",
            texture);

        destination.SetTextureScale(
            "_BaseMap",
            source.GetTextureScale(
                sourceProperty));

        destination.SetTextureOffset(
            "_BaseMap",
            source.GetTextureOffset(
                sourceProperty));
    }

    private static Texture GetTexture(
        Material material,
        params string[] properties)
    {
        foreach (string property in properties)
        {
            if (!material.HasProperty(property))
                continue;

            try
            {
                Texture texture =
                    material.GetTexture(
                        property);

                if (texture != null)
                    return texture;
            }
            catch
            {
                // Old custom shaders occasionally expose properties with
                // unexpected types. Ignore them and continue conversion.
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(
        Transform root,
        string name)
    {
        if (root == null)
            return null;

        if (string.Equals(
                root.name,
                name,
                StringComparison.OrdinalIgnoreCase))
            return root;

        foreach (Transform child in root)
        {
            Transform match =
                FindChildRecursive(
                    child,
                    name);

            if (match != null)
                return match;
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
