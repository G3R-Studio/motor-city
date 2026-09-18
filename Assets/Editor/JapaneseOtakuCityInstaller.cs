#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JapaneseOtakuCityInstaller
{
    private const string SourceRoot = "Assets/ZRNAssets";
    private const string RuntimeRoot = "Assets/Resources/MotorCity/Environment";
    private const string RuntimeCityPrefab = RuntimeRoot + "/CityVisual.prefab";
    private const string RuntimeMaterialRoot = RuntimeRoot + "/JapaneseMaterials";
    private const string BuildVersion = "otaku-city-urp-materials-v3";
    private const float CityScaleMultiplier = 3.0f;
    private const string BuildVersionKey = "MotorCity.JapaneseOtakuCity.BuildVersion";

    private static readonly string[] ClutterKeywords =
    {
        "car",
        "vehicle",
        "automobile",
        "tree",
        "bicycle",
        "bike",
        "garbage",
        "trash"
    };

    static JapaneseOtakuCityInstaller()
    {
        EditorApplication.delayCall += AutoBuildIfAvailable;
    }

    [MenuItem("Motor City/Rebuild Japanese Otaku City")]
    public static void RebuildFromMenu()
    {
        Build(true);
    }

    public static bool HasSourceAsset()
    {
        return AssetDatabase.IsValidFolder(SourceRoot) &&
               !string.IsNullOrEmpty(FindSourceAssetPath());
    }

    private static void AutoBuildIfAvailable()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!HasSourceAsset())
            return;

        bool currentBuild =
            EditorPrefs.GetString(BuildVersionKey, string.Empty) == BuildVersion;

        if (currentBuild &&
            AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeCityPrefab) != null)
            return;

        Build(false);
    }

    private static void Build(bool force)
    {
        string sourcePath = FindSourceAssetPath();

        if (string.IsNullOrEmpty(sourcePath))
        {
            if (force)
                Debug.LogWarning(
                    "Motor City: Japanese Otaku City source was not found. " +
                    "Import the Asset Store package so Assets/ZRNAssets exists.");
            return;
        }

        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);

        if (source == null)
        {
            Debug.LogWarning(
                "Motor City: Japanese Otaku City source could not be loaded: " +
                sourcePath);
            return;
        }

        Directory.CreateDirectory(RuntimeRoot);

        GameObject wrapper =
            new("MotorCity_JapaneseOtakuCity");

        GameObject city =
            PrefabUtility.InstantiatePrefab(source) as GameObject;

        if (city == null)
            city = UnityEngine.Object.Instantiate(source);

        if (city == null)
        {
            UnityEngine.Object.DestroyImmediate(wrapper);
            return;
        }

        city.name = "JapaneseOtakuCity";
        city.transform.SetParent(wrapper.transform, true);

        RemoveEmbeddedCamerasAndLights(city);
        RemoveObviousClutter(city);
        ConvertMaterialsForUrp(city);

        city.transform.localScale *= CityScaleMultiplier;

        CenterOnGround(city);

        foreach (Transform item in
                 city.GetComponentsInChildren<Transform>(true))
            item.gameObject.isStatic = true;

        PrefabUtility.SaveAsPrefabAsset(
            wrapper,
            RuntimeCityPrefab);

        UnityEngine.Object.DestroyImmediate(wrapper);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorPrefs.SetString(
            BuildVersionKey,
            BuildVersion);

        Debug.Log(
            "Motor City: Japanese Otaku City is now the primary runtime city. " +
            $"Scale={CityScaleMultiplier:0.##}x. Source: '{sourcePath}'.");
    }

    private static string FindSourceAssetPath()
    {
        if (!AssetDatabase.IsValidFolder(SourceRoot))
            return null;

        string[] exact =
            AssetDatabase.FindAssets(
                "PQ_Remake_AKIHABARA t:GameObject",
                new[] { SourceRoot });

        string best =
            exact
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(
                    path =>
                        Path.GetFileNameWithoutExtension(path)
                            .Equals(
                                "PQ_Remake_AKIHABARA",
                                StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(best))
            return best;

        string[] all =
            AssetDatabase.FindAssets(
                "t:GameObject",
                new[] { SourceRoot });

        return all
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(
                path =>
                {
                    string lower =
                        path.ToLowerInvariant();

                    return
                        lower.Contains("akihabara") ||
                        lower.Contains("otaku");
                })
            .OrderByDescending(
                path =>
                    path.IndexOf(
                        "models",
                        StringComparison.OrdinalIgnoreCase) >= 0)
            .ThenBy(path => path.Length)
            .FirstOrDefault();
    }

    private static void RemoveEmbeddedCamerasAndLights(
        GameObject city)
    {
        foreach (Camera camera in
                 city.GetComponentsInChildren<Camera>(true))
            UnityEngine.Object.DestroyImmediate(
                camera.gameObject);

        foreach (Light light in
                 city.GetComponentsInChildren<Light>(true))
            UnityEngine.Object.DestroyImmediate(
                light.gameObject);
    }

    private static void RemoveObviousClutter(
        GameObject city)
    {
        Transform[] transforms =
            city.GetComponentsInChildren<Transform>(true);

        foreach (Transform item in transforms)
        {
            if (item == null ||
                item == city.transform ||
                item.parent == null)
                continue;

            if (item.GetComponentInChildren<Renderer>(true) == null)
                continue;

            string lower =
                item.name.ToLowerInvariant();

            bool clutter =
                ClutterKeywords.Any(
                    keyword =>
                        lower.Contains(keyword));

            if (!clutter)
                continue;

            Renderer[] renderers =
                item.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length > 8)
                continue;

            UnityEngine.Object.DestroyImmediate(
                item.gameObject);
        }
    }


    private static void ConvertMaterialsForUrp(
        GameObject city)
    {
        Shader urpLit =
            Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            Debug.LogWarning(
                "Motor City: URP/Lit shader was not found, so Japanese city materials were left unchanged.");
            return;
        }

        Directory.CreateDirectory(RuntimeMaterialRoot);

        var converted =
            new Dictionary<Material, Material>();

        foreach (Renderer renderer in
                 city.GetComponentsInChildren<Renderer>(true))
        {
            Material[] source =
                renderer.sharedMaterials;

            if (source == null ||
                source.Length == 0)
                continue;

            Material[] result =
                new Material[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                Material old =
                    source[i];

                if (old == null)
                {
                    result[i] = null;
                    continue;
                }

                if (!converted.TryGetValue(
                        old,
                        out Material replacement))
                {
                    replacement =
                        BuildUrpMaterial(
                            old,
                            urpLit,
                            converted.Count);

                    converted.Add(
                        old,
                        replacement);
                }

                result[i] =
                    replacement;
            }

            renderer.sharedMaterials =
                result;
        }
    }

    private static Material BuildUrpMaterial(
        Material source,
        Shader urpLit,
        int index)
    {
        string lowerName =
            source.name.ToLowerInvariant();

        string shaderName =
            source.shader != null
                ? source.shader.name.ToLowerInvariant()
                : string.Empty;

        bool transparent =
            lowerName.Contains("glass") ||
            lowerName.Contains("window") ||
            lowerName.Contains("transparent") ||
            shaderName.Contains("transparent");

        Texture baseTexture = null;
        Vector2 textureScale = Vector2.one;
        Vector2 textureOffset = Vector2.zero;

        if (source.HasProperty("_BaseMap"))
        {
            baseTexture =
                source.GetTexture("_BaseMap");
            textureScale =
                source.GetTextureScale("_BaseMap");
            textureOffset =
                source.GetTextureOffset("_BaseMap");
        }
        else if (source.HasProperty("_MainTex"))
        {
            baseTexture =
                source.GetTexture("_MainTex");
            textureScale =
                source.GetTextureScale("_MainTex");
            textureOffset =
                source.GetTextureOffset("_MainTex");
        }

        Color sourceColor =
            source.HasProperty("_BaseColor")
                ? source.GetColor("_BaseColor")
                : source.HasProperty("_Color")
                    ? source.GetColor("_Color")
                    : Color.white;

        if (!transparent)
            sourceColor.a = 1f;

        Material material =
            new(urpLit)
            {
                name =
                    "JOC_" +
                    SanitizeAssetName(source.name) +
                    "_" +
                    index
            };

        if (baseTexture != null &&
            material.HasProperty("_BaseMap"))
        {
            material.SetTexture(
                "_BaseMap",
                baseTexture);
            material.SetTextureScale(
                "_BaseMap",
                textureScale);
            material.SetTextureOffset(
                "_BaseMap",
                textureOffset);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor(
                "_BaseColor",
                sourceColor);

        if (material.HasProperty("_Metallic"))
            material.SetFloat(
                "_Metallic",
                0f);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat(
                "_Smoothness",
                transparent ? 0.55f : 0.18f);

        if (material.HasProperty("_Surface"))
            material.SetFloat(
                "_Surface",
                transparent ? 1f : 0f);

        if (material.HasProperty("_Blend"))
            material.SetFloat(
                "_Blend",
                0f);

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat(
                "_AlphaClip",
                0f);

        if (transparent)
        {
            material.SetOverrideTag(
                "RenderType",
                "Transparent");
            material.renderQueue = 3000;
            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");
            material.SetFloat(
                "_ZWrite",
                0f);
        }
        else
        {
            material.SetOverrideTag(
                "RenderType",
                "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");
            material.SetFloat(
                "_ZWrite",
                1f);
        }

        string assetPath =
            RuntimeMaterialRoot +
            "/" +
            material.name +
            ".mat";

        Material existing =
            AssetDatabase.LoadAssetAtPath<Material>(
                assetPath);

        if (existing != null)
        {
            EditorUtility.CopySerialized(
                material,
                existing);
            UnityEngine.Object.DestroyImmediate(
                material);
            EditorUtility.SetDirty(
                existing);
            return existing;
        }

        AssetDatabase.CreateAsset(
            material,
            assetPath);

        return material;
    }

    private static string SanitizeAssetName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Material";

        char[] invalid =
            Path.GetInvalidFileNameChars();

        string result =
            new(
                value
                    .Select(
                        ch =>
                            invalid.Contains(ch)
                                ? '_'
                                : ch)
                    .ToArray());

        return string.IsNullOrWhiteSpace(result)
            ? "Material"
            : result;
    }

    private static void CenterOnGround(
        GameObject city)
    {
        Renderer[] renderers =
            city.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return;

        Bounds bounds =
            renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(
                renderers[i].bounds);

        Vector3 offset =
            new(
                bounds.center.x,
                bounds.min.y,
                bounds.center.z);

        city.transform.position -= offset;
    }
}
#endif
