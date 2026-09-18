#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class KenneyMarkerAssetInstaller
{
    private const string MarkerUrl =
        "https://raw.githubusercontent.com/shorepine/kenney/main/icons/Game%20Icons%20Expansion/White/2x/flag.png";

    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Markers";

    private const string MarkerPath =
        RuntimeRoot + "/flag.png";

    private const string VersionKey =
        "MotorCity.KenneyMarkers.Version";

    private const string Version =
        "kenney-marker-flag-v1";

    private static bool installing;

    static KenneyMarkerAssetInstaller()
    {
        EditorApplication.delayCall += AutoInstall;
    }

    [MenuItem("Motor City/Install or Rebuild Kenney Markers")]
    public static void InstallFromMenu()
    {
        Install(true);
    }

    private static void AutoInstall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool exists =
            AssetDatabase.LoadAssetAtPath<Sprite>(MarkerPath) != null;
        bool current =
            EditorPrefs.GetString(VersionKey, string.Empty) == Version;

        if (exists && current)
            return;

        Install(false);
    }

    private static void Install(bool force)
    {
        if (installing)
            return;

        if (!force &&
            AssetDatabase.LoadAssetAtPath<Sprite>(MarkerPath) != null &&
            EditorPrefs.GetString(VersionKey, string.Empty) == Version)
            return;

        installing = true;

        try
        {
            Directory.CreateDirectory(RuntimeRoot);

            string absolutePath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    MarkerPath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));

            using (WebClient client = new())
            {
                client.Headers.Add(
                    HttpRequestHeader.UserAgent,
                    "MotorCity-Unity-Editor");
                client.DownloadFile(
                    MarkerUrl,
                    absolutePath);
            }

            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer =
                AssetImporter.GetAtPath(MarkerPath) as TextureImporter;

            if (importer != null)
            {
                importer.textureType =
                    TextureImporterType.Sprite;
                importer.spriteImportMode =
                    SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 64f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorPrefs.SetString(
                VersionKey,
                Version);

            Debug.Log(
                "Motor City: Kenney CC0 world marker asset is installed and ready.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: failed to install Kenney world marker asset. " +
                exception);
        }
        finally
        {
            installing = false;
        }
    }
}
#endif
