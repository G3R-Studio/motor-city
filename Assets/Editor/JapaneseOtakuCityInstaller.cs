#if UNITY_EDITOR
using System;
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
    private const string BuildVersion = "otaku-city-v1";
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
            $"Source: '{sourcePath}'.");
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
