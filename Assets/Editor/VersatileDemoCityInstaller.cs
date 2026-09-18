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
public static class VersatileDemoCityInstaller
{
    private static readonly string[] PreferredSourceRoots =
    {
        "Assets/Versatile Studio Assets/Demo City By Versatile Studio",
        "Assets/Versatile Studio Assets",
        "Assets/Demo City By Versatile Studio"
    };

    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Environment";

    private const string RuntimeCityPrefab =
        RuntimeRoot + "/CityVisual.prefab";

    private const string RuntimeMaterialRoot =
        RuntimeRoot + "/VersatileMaterials";

    private const string BuildVersion =
        "versatile-demo-city-urp-v2";

    private const string BuildVersionKey =
        "MotorCity.VersatileDemoCity.BuildVersion";

    private const string CleanupVersionKey =
        "MotorCity.CityMigration.VersatileDemoCityV1";

    static VersatileDemoCityInstaller()
    {
        EditorApplication.delayCall += Initialize;
    }

    [MenuItem("Motor City/Rebuild Versatile Demo City")]
    public static void RebuildFromMenu()
    {
        CleanupLegacyCityAssets();
        Build(true);
    }

    private static void Initialize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        CleanupLegacyCityAssets();

        if (string.IsNullOrEmpty(FindBestDemoScene()))
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

    private static void CleanupLegacyCityAssets()
    {
        if (EditorPrefs.GetBool(CleanupVersionKey, false))
            return;

        DeleteAssetIfPresent(
            "Assets/ZRNAssets");

        DeleteAssetIfPresent(
            "Assets/ThirdParty/CommunityCoreCity02");

        DeleteAssetIfPresent(
            RuntimeRoot);

        EditorPrefs.SetBool(
            CleanupVersionKey,
            true);

        AssetDatabase.Refresh();
    }

    private static void DeleteAssetIfPresent(
        string path)
    {
        if (!AssetDatabase.IsValidFolder(path) &&
            AssetDatabase.LoadMainAssetAtPath(path) == null)
            return;

        AssetDatabase.DeleteAsset(path);
    }

    private static void Build(bool force)
    {
        string scenePath =
            FindBestDemoScene();

        if (string.IsNullOrEmpty(scenePath))
        {
            if (force)
            {
                Debug.LogWarning(
                    "Motor City: Demo City By Versatile Studio was not found. " +
                    "Import the Asset Store package first.");
            }

            return;
        }

        Directory.CreateDirectory(RuntimeRoot);

        Scene previousScene =
            SceneManager.GetActiveScene();

        Scene sourceScene =
            EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Additive);

        GameObject wrapper =
            new("MotorCity_VersatileDemoCity");

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

            RemoveRuntimeConflicts(wrapper);
            ConvertMaterialsForUrp(wrapper);
            MarkStatic(wrapper);

            if (wrapper.GetComponentsInChildren<Renderer>(true).Length == 0)
                throw new InvalidOperationException(
                    "The selected demo city scene contains no renderers.");

            PrefabUtility.SaveAsPrefabAsset(
                wrapper,
                RuntimeCityPrefab);

            AssetDatabase.SaveAssets();

            EditorPrefs.SetString(
                BuildVersionKey,
                BuildVersion);

            Debug.Log(
                "Motor City: Demo City By Versatile Studio is now the runtime city. " +
                $"Source scene: '{scenePath}'.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: failed to build the Versatile demo city. " +
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

    private static string FindBestDemoScene()
    {
        string[] preferredRoots =
            PreferredSourceRoots
                .Where(AssetDatabase.IsValidFolder)
                .ToArray();

        string[] sceneGuids =
            preferredRoots.Length > 0
                ? AssetDatabase.FindAssets(
                    "t:Scene",
                    preferredRoots)
                : AssetDatabase.FindAssets(
                    "t:Scene",
                    new[] { "Assets" });

        return sceneGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(IsVersatileDemoCityScene)
            .Select(
                path => new
                {
                    Path = path,
                    Score = ScoreScene(path)
                })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Path.Length)
            .Select(item => item.Path)
            .FirstOrDefault();
    }

    private static bool IsVersatileDemoCityScene(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string lower =
            path.Replace('\\', '/')
                .ToLowerInvariant();

        bool versatile =
            lower.Contains("versatile");

        bool city =
            lower.Contains("demo city") ||
            lower.Contains("democity") ||
            lower.Contains("/city") ||
            lower.Contains("city ");

        return versatile && city;
    }

    private static int ScoreScene(
        string path)
    {
        string lower =
            path.ToLowerInvariant();

        int score = 0;

        if (lower.Contains("demo"))
            score += 100;

        if (lower.Contains("city"))
            score += 80;

        if (lower.Contains("night"))
            score += 60;

        if (lower.Contains("scene"))
            score += 20;

        if (lower.Contains("example"))
            score += 10;

        return score;
    }

    private static bool ShouldSkipRoot(
        GameObject root)
    {
        if (root == null)
            return true;

        string lower =
            root.name.ToLowerInvariant();

        if (lower.Contains("camera"))
            return true;

        if (lower.Contains("canvas") ||
            lower.Contains("ui") ||
            lower.Contains("eventsystem"))
            return true;

        return false;
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

    }

    private static void ConvertMaterialsForUrp(
        GameObject root)
    {
        Shader urpLit =
            Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            Debug.LogError(
                "Motor City: URP/Lit shader was not found. " +
                "Versatile city materials cannot be converted.");
            return;
        }

        if (AssetDatabase.IsValidFolder(RuntimeMaterialRoot))
            AssetDatabase.DeleteAsset(RuntimeMaterialRoot);

        Directory.CreateDirectory(RuntimeMaterialRoot);

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

            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source =
                    sourceMaterials[i];

                if (source == null)
                {
                    runtimeMaterials[i] = null;
                    continue;
                }

                if (source.shader != null &&
                    source.shader.name.StartsWith(
                        "Universal Render Pipeline/",
                        StringComparison.Ordinal))
                {
                    runtimeMaterials[i] = source;
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

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Motor City: converted {converted.Count} Versatile city materials to URP.");
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
                    "Versatile_" +
                    SanitizeFileName(source.name) +
                    "_" +
                    index,
                enableInstancing = true
            };

        Texture baseMap =
            GetTexture(
                source,
                "_BaseMap",
                "_MainTex",
                "_Albedo");

        if (baseMap != null)
        {
            material.SetTexture(
                "_BaseMap",
                baseMap);

            string textureProperty =
                source.HasProperty("_BaseMap")
                    ? "_BaseMap"
                    : source.HasProperty("_MainTex")
                        ? "_MainTex"
                        : null;

            if (textureProperty != null)
            {
                material.SetTextureScale(
                    "_BaseMap",
                    source.GetTextureScale(textureProperty));

                material.SetTextureOffset(
                    "_BaseMap",
                    source.GetTextureOffset(textureProperty));
            }
        }

        Color baseColor =
            GetColor(
                source,
                Color.white,
                "_BaseColor",
                "_Color");

        bool transparent =
            IsTransparentMaterial(source);

        bool alphaClip =
            IsCutoutMaterial(source);

        if (!transparent)
            baseColor.a = 1f;

        material.SetColor(
            "_BaseColor",
            baseColor);

        Texture normalMap =
            GetTexture(
                source,
                "_BumpMap",
                "_NormalMap");

        if (normalMap != null)
        {
            material.SetTexture(
                "_BumpMap",
                normalMap);
            material.EnableKeyword(
                "_NORMALMAP");
        }

        Texture emissionMap =
            GetTexture(
                source,
                "_EmissionMap");

        Color emissionColor =
            GetColor(
                source,
                Color.black,
                "_EmissionColor");

        if (emissionMap != null ||
            emissionColor.maxColorComponent > 0.001f)
        {
            if (emissionMap != null)
                material.SetTexture(
                    "_EmissionMap",
                    emissionMap);

            material.SetColor(
                "_EmissionColor",
                emissionColor);

            material.EnableKeyword(
                "_EMISSION");
        }

        float metallic =
            GetFloat(
                source,
                0f,
                "_Metallic");

        float smoothness =
            GetFloat(
                source,
                0.28f,
                "_Smoothness",
                "_Glossiness");

        material.SetFloat(
            "_Metallic",
            Mathf.Clamp01(metallic));

        material.SetFloat(
            "_Smoothness",
            Mathf.Clamp01(smoothness));

        material.SetFloat(
            "_Surface",
            transparent ? 1f : 0f);

        material.SetFloat(
            "_AlphaClip",
            alphaClip ? 1f : 0f);

        if (alphaClip)
        {
            float cutoff =
                GetFloat(
                    source,
                    0.5f,
                    "_Cutoff");

            material.SetFloat(
                "_Cutoff",
                cutoff);

            material.EnableKeyword(
                "_ALPHATEST_ON");
        }

        if (transparent)
        {
            material.SetOverrideTag(
                "RenderType",
                "Transparent");

            material.renderQueue =
                (int)UnityEngine.Rendering.RenderQueue.Transparent;

            material.SetFloat(
                "_ZWrite",
                0f);

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            material.SetOverrideTag(
                "RenderType",
                alphaClip
                    ? "TransparentCutout"
                    : "Opaque");

            material.renderQueue =
                alphaClip
                    ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest
                    : -1;

            material.SetFloat(
                "_ZWrite",
                1f);

            material.DisableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");
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

    private static bool IsTransparentMaterial(
        Material material)
    {
        string name =
            material.name.ToLowerInvariant();

        string shader =
            material.shader != null
                ? material.shader.name.ToLowerInvariant()
                : string.Empty;

        if (name.Contains("glass") ||
            name.Contains("window") ||
            name.Contains("transparent"))
            return true;

        if (shader.Contains("transparent") &&
            !shader.Contains("cutout"))
            return true;

        return material.renderQueue >=
               (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static bool IsCutoutMaterial(
        Material material)
    {
        string shader =
            material.shader != null
                ? material.shader.name.ToLowerInvariant()
                : string.Empty;

        return shader.Contains("cutout") ||
               shader.Contains("alphatest") ||
               material.IsKeywordEnabled("_ALPHATEST_ON");
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

    private static Color GetColor(
        Material material,
        Color fallback,
        params string[] properties)
    {
        foreach (string property in properties)
        {
            if (material.HasProperty(property))
                return material.GetColor(property);
        }

        return fallback;
    }

    private static float GetFloat(
        Material material,
        float fallback,
        params string[] properties)
    {
        foreach (string property in properties)
        {
            if (material.HasProperty(property))
                return material.GetFloat(property);
        }

        return fallback;
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
            item.gameObject.isStatic = true;
        }
    }
}
#endif
