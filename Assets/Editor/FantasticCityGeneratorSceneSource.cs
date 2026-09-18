#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorSceneSource
{
    public const string CityRootName =
        "City-Maker";

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

        // Prefer any already loaded scene that actually contains City-Maker.
        for (int i = 0;
             i < SceneManager.sceneCount;
             i++)
        {
            Scene loaded =
                SceneManager.GetSceneAt(i);

            if (!loaded.IsValid() ||
                !loaded.isLoaded)
                continue;

            if (FindCityRoot(loaded) == null)
                continue;

            sourceScene =
                loaded;

            SceneManager.SetActiveScene(
                sourceScene);

            return true;
        }

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
                    path =>
                        string.Equals(
                            path,
                            FantasticCityGeneratorWorkbench.WorkbenchScene,
                            StringComparison.OrdinalIgnoreCase))
                .ThenBy(
                    path =>
                        path,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        foreach (string path in localScenes)
        {
            Scene opened =
                EditorSceneManager.OpenScene(
                    path,
                    OpenSceneMode.Additive);

            if (FindCityRoot(opened) != null)
            {
                sourceScene =
                    opened;

                openedTemporarily =
                    true;

                SceneManager.SetActiveScene(
                    sourceScene);

                Debug.Log(
                    "Motor City: automatically opened saved FCG source scene " +
                    path);

                return true;
            }

            EditorSceneManager.CloseScene(
                opened,
                true);
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

    public static void FinishSourceScene(
        Scene sourceScene,
        bool openedTemporarily,
        Scene previousActiveScene,
        bool saveSource)
    {
        if (saveSource &&
            sourceScene.IsValid() &&
            sourceScene.isLoaded &&
            !string.IsNullOrWhiteSpace(sourceScene.path))
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
}
#endif
