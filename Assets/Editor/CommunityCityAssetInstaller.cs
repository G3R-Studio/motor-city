#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CommunityCityAssetInstaller
{
    private const string DownloadUrl =
        "https://github.com/lucrybpin/unity-community-core-city-02/archive/refs/heads/master.zip";

    private const string ImportedRoot =
        "Assets/ThirdParty/CommunityCoreCity02";

    private const string CitySourcePrefab =
        ImportedRoot + "/City 02/Prefabs/City 02.prefab";

    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Environment";

    private const string RuntimeCityPrefab =
        RuntimeRoot + "/CityVisual.prefab";

    private const string BuildVersion = "city-uniform-scale-1138.5-v5";
    private const string BuildVersionKey = "MotorCity.CommunityCity.BuildVersion";

    private static bool installing;

    static CommunityCityAssetInstaller()
    {
        EditorApplication.delayCall += AutoInstallIfNeeded;
    }

    [MenuItem("Motor City/Install or Rebuild CC0 City Assets")]
    public static void InstallFromMenu()
    {
        Install(true);
    }

    private static void AutoInstallIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        bool prefabExists =
            AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeCityPrefab) != null;
        bool currentBuild =
            EditorPrefs.GetString(BuildVersionKey, string.Empty) == BuildVersion;

        if (prefabExists && currentBuild)
            return;

        Install(false);
    }

    private static void Install(bool force)
    {
        if (installing) return;
        if (!force &&
            AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeCityPrefab) != null &&
            EditorPrefs.GetString(BuildVersionKey, string.Empty) == BuildVersion)
            return;

        installing = true;

        try
        {
            EnsureSourceAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            BuildCityPrefab();
            BuildRuntimeProp(
                ImportedRoot + "/City 02/Third Party/Kenney/City Kit (Cars)/Prefabs/box.prefab",
                RuntimeRoot + "/DeliveryCrate.prefab");
            BuildRuntimeProp(
                ImportedRoot + "/City 02/Third Party/Kenney/City Kit (Cars)/Prefabs/cone.prefab",
                RuntimeRoot + "/DriftCone.prefab");
            BuildRuntimeProp(
                ImportedRoot + "/City 02/Third Party/Kenney/City Kit (Cars)/Prefabs/hatchback-sports.prefab",
                RuntimeRoot + "/SprintCar.prefab");
            BuildRuntimeProp(
                ImportedRoot + "/City 02/Third Party/Kenney/City Kit (Industrial)/Prefabs/building-a.prefab",
                RuntimeRoot + "/GarageBuilding.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorPrefs.SetString(BuildVersionKey, BuildVersion);

            Debug.Log(
                "Motor City: CC0 Community Core / Kenney city assets are installed and ready.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: failed to install CC0 city assets. " +
                exception);
        }
        finally
        {
            installing = false;
        }
    }

    private static void EnsureSourceAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CitySourcePrefab) != null)
            return;

        string zipPath =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "Library",
                "MotorCityCommunityCity02.zip");

        if (File.Exists(zipPath))
            File.Delete(zipPath);

        using (WebClient client = new())
        {
            client.Headers.Add(
                HttpRequestHeader.UserAgent,
                "MotorCity-Unity-Editor");
            client.DownloadFile(DownloadUrl, zipPath);
        }

        Directory.CreateDirectory(ImportedRoot);

        using FileStream zipStream =
            File.OpenRead(zipPath);
        using ZipArchive archive =
            new(zipStream, ZipArchiveMode.Read);

        const string assetsMarker = "/Assets/City 02/";

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string normalized =
                entry.FullName.Replace('\\', '/');

            int markerIndex =
                normalized.IndexOf(
                    assetsMarker,
                    StringComparison.Ordinal);

            if (markerIndex < 0)
                continue;

            string relative =
                normalized.Substring(
                    markerIndex + "/Assets/".Length);

            if (string.IsNullOrWhiteSpace(relative))
                continue;

            string destination =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    ImportedRoot,
                    relative.Replace('/', Path.DirectorySeparatorChar));

            if (normalized.EndsWith("/", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(destination);
                continue;
            }

            string directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using Stream sourceStream = entry.Open();
            using FileStream destinationStream =
                new(
                    destination,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);
            sourceStream.CopyTo(destinationStream);
        }

        File.Delete(zipPath);
    }

    private static void BuildCityPrefab()
    {
        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                CitySourcePrefab);

        if (source == null)
            throw new InvalidOperationException(
                "Downloaded city prefab was not found: " +
                CitySourcePrefab);

        Directory.CreateDirectory(RuntimeRoot);

        GameObject wrapper =
            new("MotorCity_CC0_CityVisual");

        GameObject city =
            PrefabUtility.InstantiatePrefab(source) as GameObject;

        if (city == null)
        {
            UnityEngine.Object.DestroyImmediate(wrapper);
            throw new InvalidOperationException(
                "Could not instantiate the downloaded city prefab.");
        }

        city.name = "CommunityCoreCity02";
        city.transform.SetParent(wrapper.transform, true);

        foreach (Camera camera in
                 city.GetComponentsInChildren<Camera>(true))
            UnityEngine.Object.DestroyImmediate(camera);

        foreach (Light light in
                 city.GetComponentsInChildren<Light>(true))
            UnityEngine.Object.DestroyImmediate(light);

        foreach (Collider collider in
                 city.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);

        Renderer[] renderers =
            city.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            UnityEngine.Object.DestroyImmediate(wrapper);
            throw new InvalidOperationException(
                "Downloaded city prefab has no renderers.");
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        const float targetHorizontalSize = 1138.5f;

        float horizontalSize =
            Mathf.Max(bounds.size.x, bounds.size.z);

        if (horizontalSize > 0.01f)
        {
            float uniformScale =
                targetHorizontalSize / horizontalSize;

            city.transform.localScale *= uniformScale;
        }

        renderers =
            city.GetComponentsInChildren<Renderer>(true);

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 centerOffset =
            new(
                bounds.center.x,
                bounds.min.y,
                bounds.center.z);

        city.transform.position -= centerOffset;

        foreach (Transform item in
                 city.GetComponentsInChildren<Transform>(true))
            item.gameObject.isStatic = true;

        PrefabUtility.SaveAsPrefabAsset(
            wrapper,
            RuntimeCityPrefab);

        UnityEngine.Object.DestroyImmediate(wrapper);
    }

    private static void BuildRuntimeProp(
        string sourcePath,
        string destinationPath)
    {
        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                sourcePath);

        if (source == null)
        {
            Debug.LogWarning(
                "Motor City: optional CC0 prop was not found: " +
                sourcePath);
            return;
        }

        string directory =
            Path.GetDirectoryName(destinationPath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(source) as GameObject;

        if (instance == null)
            return;

        foreach (Collider collider in
                 instance.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);

        PrefabUtility.SaveAsPrefabAsset(
            instance,
            destinationPath);

        UnityEngine.Object.DestroyImmediate(instance);
    }
}
#endif
