#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MotorCityEmptyLegacyFolderCleaner
{
    private static readonly string[] LegacyFolders =
    {
        "Assets/City",
        "Assets/FCG",
        "Assets/LocalAudio",
        "Assets/Vehicle_Essentials",
        "Assets/Resources/MotorCity/Audio",
        "Assets/Resources/MotorCity/Environment/ModernCityMaterials",
        "Assets/Scripts/Audio",
        "Assets/Fantastic City Generator/Documentation",
        "Assets/Fantastic City Generator/Player",
        "Assets/PROMETEO - Car Controller/Sounds"
    };

    public static void RemoveEmptyLegacyFolders()
    {
        bool changed =
            false;

        foreach (string assetPath in
                 LegacyFolders)
        {
            string absolute =
                ToAbsolutePath(
                    assetPath);

            if (!Directory.Exists(
                    absolute))
                continue;

            if (ContainsRealFiles(
                    absolute))
                continue;

            try
            {
                FileUtil.DeleteFileOrDirectory(
                    assetPath);

                FileUtil.DeleteFileOrDirectory(
                    assetPath +
                    ".meta");

                changed =
                    true;

                Debug.Log(
                    "Motor City: removed empty legacy folder " +
                    assetPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Motor City: could not remove empty legacy folder " +
                    assetPath +
                    ". " +
                    exception.Message);
            }
        }

        if (changed)
        {
            AssetDatabase.Refresh();
        }
    }

    private static bool ContainsRealFiles(
        string directory)
    {
        try
        {
            return
                Directory
                    .GetFiles(
                        directory,
                        "*",
                        SearchOption.AllDirectories)
                    .Any(
                        file =>
                            !file.EndsWith(
                                ".meta",
                                StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return true;
        }
    }

    private static string ToAbsolutePath(
        string assetPath)
    {
        string projectRoot =
            Directory.GetParent(
                    Application.dataPath)
                ?.FullName ??
            string.Empty;

        return
            Path.Combine(
                projectRoot,
                assetPath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
    }
}
#endif
