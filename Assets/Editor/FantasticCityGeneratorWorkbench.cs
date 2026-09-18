#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorWorkbench
{
    public const string LocalRoot =
        "Assets/LocalGenerated";

    public const string WorkbenchScene =
        LocalRoot + "/FCG_Workbench.unity";

    [MenuItem("Motor City/Fantastic City Generator/Create or Open Safe Workbench")]
    public static void CreateOrOpen()
    {
        EnsureFolder(
            LocalRoot);

        if (File.Exists(
                WorkbenchScene))
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(
                WorkbenchScene,
                OpenSceneMode.Single);

            Debug.Log(
                "Motor City: opened local FCG workbench " +
                WorkbenchScene);

            return;
        }

        Scene active =
            SceneManager.GetActiveScene();

        if (!active.IsValid() ||
            !active.isLoaded)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Workbench",
                "Нет активной сцены, из которой можно создать workbench.",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!EditorSceneManager.SaveScene(
                active,
                WorkbenchScene,
                true))
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Workbench",
                "Не удалось создать локальную копию сцены.",
                "OK");
            return;
        }

        EditorSceneManager.OpenScene(
            WorkbenchScene,
            OpenSceneMode.Single);

        Debug.Log(
            "Motor City: created local FCG workbench at " +
            WorkbenchScene);

        EditorUtility.DisplayDialog(
            "Motor City — FCG Workbench",
            "Локальная рабочая сцена создана:\n\n" +
            WorkbenchScene +
            "\n\nГенерируй Fantastic City Generator только здесь. " +
            "Эта сцена игнорируется Git и не пропадёт после git reset.",
            "OK");
    }

    public static bool IsSafeWorkbench(
        Scene scene)
    {
        if (!scene.IsValid())
            return false;

        string path =
            scene.path.Replace(
                '\\',
                '/');

        return path.StartsWith(
            LocalRoot + "/",
            StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureFolder(
        string assetPath)
    {
        string normalized =
            assetPath.Replace(
                '\\',
                '/');

        if (AssetDatabase.IsValidFolder(
                normalized))
            return;

        string parent =
            Path.GetDirectoryName(
                    normalized)
                ?.Replace(
                    '\\',
                    '/');

        string name =
            Path.GetFileName(
                normalized);

        if (string.IsNullOrWhiteSpace(
                parent) ||
            string.IsNullOrWhiteSpace(
                name))
            return;

        if (parent != "Assets")
        {
            EnsureFolder(
                parent);
        }

        if (!AssetDatabase.IsValidFolder(
                normalized))
        {
            AssetDatabase.CreateFolder(
                parent,
                name);
        }
    }
}
#endif
