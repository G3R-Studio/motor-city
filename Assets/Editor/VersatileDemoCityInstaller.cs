#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VersatileDemoCityInstaller
{
    private const string SourceRoot =
        "Assets/Versatile Studio Assets/Demo City By Versatile Studio";

    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Environment";

    private const string RuntimeCityPrefab =
        RuntimeRoot + "/CityVisual.prefab";

    private const string BuildVersion =
        "versatile-demo-city-v1";

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

        if (!AssetDatabase.IsValidFolder(SourceRoot))
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
        if (!AssetDatabase.IsValidFolder(SourceRoot))
        {
            if (force)
            {
                Debug.LogWarning(
                    "Motor City: Demo City By Versatile Studio was not found. " +
                    "Import the Asset Store package first.");
            }

            return;
        }

        string scenePath =
            FindBestDemoScene();

        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogError(
                "Motor City: no Unity scene was found inside '" +
                SourceRoot +
                "'.");
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
        string[] sceneGuids =
            AssetDatabase.FindAssets(
                "t:Scene",
                new[] { SourceRoot });

        return sceneGuids
            .Select(AssetDatabase.GUIDToAssetPath)
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

        foreach (FlareLayer flare in
                 root.GetComponentsInChildren<FlareLayer>(true))
        {
            UnityEngine.Object.DestroyImmediate(
                flare);
        }
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
