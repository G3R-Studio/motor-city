#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class KenneyUiAssetInstaller
{
    private const string DownloadUrl =
        "https://github.com/ereborstudios/kenney-ui-pack/archive/refs/heads/main.zip";

    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/UI";

    private const string VersionKey =
        "MotorCity.KenneyUI.Version";

    private const string Version =
        "kenney-ui-pack-v1";

    private static bool installing;

    static KenneyUiAssetInstaller()
    {
        EditorApplication.delayCall += AutoInstall;
    }

    public static void InstallFromMenu()
    {
        Install(true);
    }

    private static void AutoInstall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        string panel =
            RuntimeRoot + "/grey_panel.png";

        bool exists =
            AssetDatabase.LoadAssetAtPath<Sprite>(panel) != null;
        bool current =
            EditorPrefs.GetString(VersionKey, string.Empty) == Version;

        if (exists && current)
            return;

        Install(false);
    }

    private static void Install(bool force)
    {
        if (installing) return;

        string panel =
            RuntimeRoot + "/grey_panel.png";

        if (!force &&
            AssetDatabase.LoadAssetAtPath<Sprite>(panel) != null &&
            EditorPrefs.GetString(VersionKey, string.Empty) == Version)
            return;

        installing = true;

        try
        {
            Directory.CreateDirectory(RuntimeRoot);

            string zipPath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Library",
                    "MotorCityKenneyUi.zip");

            SafeDelete(zipPath);

            using (WebClient client = new())
            {
                client.Headers.Add(
                    HttpRequestHeader.UserAgent,
                    "MotorCity-Unity-Editor");
                client.DownloadFile(DownloadUrl, zipPath);
            }

            string[] wanted =
            {
                "sprites/grey_panel.png"
            };

            using (FileStream zipStream = File.OpenRead(zipPath))
            using (ZipArchive archive = new(zipStream, ZipArchiveMode.Read))
            {
                foreach (string relative in wanted)
                {
                    ZipArchiveEntry entry = null;

                    foreach (ZipArchiveEntry candidate in archive.Entries)
                    {
                        string normalized =
                            candidate.FullName.Replace('\\', '/');

                        if (normalized.EndsWith(
                                "/" + relative,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            entry = candidate;
                            break;
                        }
                    }

                    if (entry == null)
                    {
                        Debug.LogWarning(
                            "Motor City: Kenney UI file not found: " +
                            relative);
                        continue;
                    }

                    string fileName =
                        Path.GetFileName(relative);

                    string destination =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            RuntimeRoot,
                            fileName);

                    using Stream source = entry.Open();
                    using FileStream target =
                        new(
                            destination,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None);

                    source.CopyTo(target);
                }
            }

            SafeDelete(zipPath);

            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport);

            ConfigureSprite("grey_panel.png", 12f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorPrefs.SetString(VersionKey, Version);

            Debug.Log(
                "Motor City: Kenney CC0 UI Pack is installed and ready.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: failed to install Kenney UI Pack. " +
                exception);
        }
        finally
        {
            installing = false;
        }
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // The generated ZIP is only a temporary cache. If another process
            // still holds it briefly, leave it for the next installer pass.
        }
    }

    private static void ConfigureSprite(
        string fileName,
        float border)
    {
        string assetPath =
            RuntimeRoot + "/" + fileName;

        TextureImporter importer =
            AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder =
            new Vector4(border, border, border, border);
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }
}
#endif
