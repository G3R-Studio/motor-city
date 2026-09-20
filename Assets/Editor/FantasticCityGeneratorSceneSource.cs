#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorSceneSource
{
    public const string CityRootName =
        "City-Maker";

    private const string SourceSceneKey =
        "MotorCity.FCG.SourceScene";

    [MenuItem("Motor City/Fantastic City Generator/3 - Choose Saved Source Scene...")]
    public static void ChooseSavedSourceScene()
    {
        string initialDirectory =
            Path.Combine(
                Application.dataPath,
                "LocalGenerated");

        string absolutePath =
            EditorUtility.OpenFilePanel(
                "Choose Fantastic City Generator source scene",
                Directory.Exists(initialDirectory)
                    ? initialDirectory
                    : Application.dataPath,
                "unity");

        if (string.IsNullOrWhiteSpace(
                absolutePath))
            return;

        string assetPath =
            AbsoluteToAssetPath(
                absolutePath);

        if (string.IsNullOrWhiteSpace(
                assetPath))
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Source",
                "Выбранная сцена должна находиться внутри текущего Unity-проекта.",
                "OK");
            return;
        }

        Scene previous =
            SceneManager.GetActiveScene();

        Scene opened =
            EditorSceneManager.OpenScene(
                assetPath,
                OpenSceneMode.Additive);

        bool valid =
            FindCityRoot(
                opened) != null;

        EditorSceneManager.CloseScene(
            opened,
            true);

        if (previous.IsValid() &&
            previous.isLoaded)
        {
            SceneManager.SetActiveScene(
                previous);
        }

        if (!valid)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Source",
                "В выбранной сцене нет корневого объекта City-Maker.",
                "OK");
            return;
        }

        SetPreferredSourceScene(
            assetPath);

        EditorUtility.DisplayDialog(
            "Motor City — FCG Source",
            "Источник города выбран:\n\n" +
            assetPath +
            "\n\nFix Materials и Build Runtime City теперь всегда будут использовать именно эту сцену.",
            "OK");
    }

    [MenuItem("Motor City/Fantastic City Generator/2 - Use Open City as Source")]
    public static void UseOpenCitySceneAsSource()
    {
        Scene active =
            SceneManager.GetActiveScene();

        if (!active.IsValid() ||
            !active.isLoaded ||
            FindCityRoot(active) == null)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Source",
                "В активной сцене нет City-Maker.",
                "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(
                active.path))
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Source",
                "Сначала сохрани сцену на диск.",
                "OK");
            return;
        }

        EditorSceneManager.SaveScene(
            active);

        SetPreferredSourceScene(
            active.path);

        EditorUtility.DisplayDialog(
            "Motor City — FCG Source",
            "Эта сцена теперь является источником города:\n\n" +
            active.path,
            "OK");
    }

    public static bool TryOpenSourceScene(
        out Scene sourceScene,
        out bool openedTemporarily,
        out Scene previousActiveScene)
    {
        previousActiveScene =
            SceneManager.GetActiveScene();

        sourceScene =
            default;

        openedTemporarily =
            false;

        // If the user has the actual generated city scene open, always use it
        // and remember it for future builds from Prototype.
        Scene active =
            SceneManager.GetActiveScene();

        if (active.IsValid() &&
            active.isLoaded &&
            FindCityRoot(active) != null)
        {
            sourceScene =
                active;

            if (!string.IsNullOrWhiteSpace(
                    active.path))
            {
                SetPreferredSourceScene(
                    active.path);
            }

            Debug.Log(
                "Motor City: using currently open FCG source scene " +
                active.path);

            return true;
        }

        string preferred =
            EditorPrefs.GetString(
                SourceSceneKey,
                string.Empty);

        if (TryOpenScenePath(
                preferred,
                out sourceScene))
        {
            openedTemporarily =
                true;

            SceneManager.SetActiveScene(
                sourceScene);

            Debug.Log(
                "Motor City: using selected FCG source scene " +
                preferred);

            return true;
        }

        // Fall back to the most recently modified local generated scene.
        string[] localScenes =
            AssetDatabase.FindAssets(
                    "t:Scene",
                    new[]
                    {
                        FantasticCityGeneratorWorkbench.LocalRoot
                    })
                .Select(
                    AssetDatabase.GUIDToAssetPath)
                .Where(
                    path =>
                        !string.IsNullOrWhiteSpace(path))
                .OrderByDescending(
                    GetLastWriteTimeUtcSafe)
                .ThenBy(
                    path =>
                        path,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        foreach (string path in localScenes)
        {
            if (!TryOpenScenePath(
                    path,
                    out Scene opened))
                continue;

            sourceScene =
                opened;

            openedTemporarily =
                true;

            SetPreferredSourceScene(
                path);

            SceneManager.SetActiveScene(
                sourceScene);

            Debug.LogWarning(
                "Motor City: no explicit FCG source scene was selected. " +
                "Using the newest saved scene with City-Maker: " +
                path);

            return true;
        }

        if (previousActiveScene.IsValid() &&
            previousActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(
                previousActiveScene);
        }

        return false;
    }

    public static GameObject FindCityRoot(
        Scene scene)
    {
        if (!scene.IsValid() ||
            !scene.isLoaded)
            return null;

        return scene
            .GetRootGameObjects()
            .FirstOrDefault(
                root =>
                    string.Equals(
                        root.name,
                        CityRootName,
                        StringComparison.OrdinalIgnoreCase));
    }

    public static string GetPreferredSourceScene()
    {
        return EditorPrefs.GetString(
            SourceSceneKey,
            string.Empty);
    }

    public static void FinishSourceScene(
        Scene sourceScene,
        bool openedTemporarily,
        Scene previousActiveScene,
        bool saveSource)
    {
        if (saveSource &&
            sourceScene.IsValid() &&
            sourceScene.isLoaded &&
            !string.IsNullOrWhiteSpace(sourceScene.path) &&
            sourceScene.isDirty)
        {
            EditorSceneManager.SaveScene(
                sourceScene);
        }

        if (openedTemporarily &&
            sourceScene.IsValid() &&
            sourceScene.isLoaded)
        {
            EditorSceneManager.CloseScene(
                sourceScene,
                true);
        }

        if (previousActiveScene.IsValid() &&
            previousActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(
                previousActiveScene);
        }
    }

    private static bool TryOpenScenePath(
        string assetPath,
        out Scene scene)
    {
        scene =
            default;

        if (string.IsNullOrWhiteSpace(
                assetPath))
            return false;

        SceneAsset asset =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(
                assetPath);

        if (asset == null)
            return false;

        Scene opened =
            EditorSceneManager.OpenScene(
                assetPath,
                OpenSceneMode.Additive);

        if (FindCityRoot(
                opened) == null)
        {
            EditorSceneManager.CloseScene(
                opened,
                true);

            return false;
        }

        scene =
            opened;

        return true;
    }

    private static void SetPreferredSourceScene(
        string assetPath)
    {
        if (string.IsNullOrWhiteSpace(
                assetPath))
            return;

        EditorPrefs.SetString(
            SourceSceneKey,
            assetPath.Replace(
                '\\',
                '/'));

        Debug.Log(
            "Motor City: FCG source scene selected: " +
            assetPath);
    }

    private static string AbsoluteToAssetPath(
        string absolutePath)
    {
        string normalizedAbsolute =
            Path.GetFullPath(
                    absolutePath)
                .Replace(
                    '\\',
                    '/');

        string projectRoot =
            Directory.GetParent(
                    Application.dataPath)
                ?.FullName
                .Replace(
                    '\\',
                    '/');

        if (string.IsNullOrWhiteSpace(
                projectRoot))
            return null;

        string prefix =
            projectRoot.TrimEnd('/') +
            "/";

        if (!normalizedAbsolute.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            return null;

        return normalizedAbsolute.Substring(
            prefix.Length);
    }

    private static DateTime GetLastWriteTimeUtcSafe(
        string assetPath)
    {
        try
        {
            string full =
                Path.GetFullPath(
                    assetPath);

            return File.Exists(full)
                ? File.GetLastWriteTimeUtc(
                    full)
                : DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
#endif
