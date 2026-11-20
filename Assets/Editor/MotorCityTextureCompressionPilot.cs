using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Reversible WebGL texture compression pass for large city, skybox and UI textures.
/// Applies per-texture WebGL size caps and Crunch where appropriate; normal maps
/// use standard compression. Saves the original importer settings before changes.
/// </summary>
public static class MotorCityTextureCompressionPilot
{
    private const string BackupPath = "Library/MotorCityTextureCompressionPilotBackup.json";

    private sealed class TextureTarget
    {
        public string path;
        public int maxSize;
        public bool crunch;
    }

    private static List<TextureTarget> GetTargetTextures()
    {
        var targets = new List<TextureTarget>
        {
            new TextureTarget { path = "Assets/Fantastic City Generator/Textures/Atlas-1.tga", maxSize = 2048, crunch = true },
            new TextureTarget { path = "Assets/Fantastic City Generator/Textures/Atlas-1-N.png", maxSize = 1024, crunch = false },
            new TextureTarget { path = "Assets/Fantastic City Generator/Textures/Wins.tga", maxSize = 1024, crunch = true },
            new TextureTarget { path = "Assets/Fantastic City Generator/Textures/Roads.tga", maxSize = 1024, crunch = true },
            new TextureTarget { path = "Assets/Fantastic City Generator/Textures/Roads_N.png", maxSize = 1024, crunch = false },
            new TextureTarget { path = "Assets/Fantastic City Generator/Textures/Water.png", maxSize = 512, crunch = true },
            new TextureTarget { path = "Assets/Fantastic City Generator/Textures/Trees-01.png", maxSize = 512, crunch = true },
            new TextureTarget { path = "Assets/Fantastic City Generator/Textures/Vegetation.png", maxSize = 512, crunch = true },
            new TextureTarget { path = "Assets/Fantastic City Generator/BackGrounds/Materials/Back-01.png", maxSize = 1024, crunch = true },
            new TextureTarget { path = "Assets/Fantastic City Generator/SkyBox/Sky.png", maxSize = 1024, crunch = true },
            new TextureTarget { path = "Assets/Fantastic City Generator/SkyBox/Sky2.png", maxSize = 1024, crunch = true },
            new TextureTarget { path = "Assets/Resources/MotorCity/UI/Characters/avatar_turbo.png", maxSize = 512, crunch = true }
        };

        // Sky panoramas are distant background imagery. A 1024px WebGL cap
        // with Crunch keeps them substantially smaller without touching source
        // images or the desktop import settings.
        const string panoramaRoot = "Assets/Fantasy Skybox FREE/Panoramics";
        if (Directory.Exists(panoramaRoot))
        {
            string[] panoramas = Directory.GetFiles(
                panoramaRoot, "*.png", SearchOption.AllDirectories);
            Array.Sort(panoramas, StringComparer.OrdinalIgnoreCase);
            foreach (string panorama in panoramas)
            {
                targets.Add(new TextureTarget
                {
                    path = panorama.Replace('\\', '/'),
                    maxSize = 1024,
                    crunch = true
                });
            }
        }

        // Intro and menu artwork remains at up to 2048px, but uses Crunch.
        const string introRoot = "Assets/Resources/MotorCity/Intro";
        if (Directory.Exists(introRoot))
        {
            string[] introImages = Directory.GetFiles(
                introRoot, "*.png", SearchOption.TopDirectoryOnly);
            Array.Sort(introImages, StringComparer.OrdinalIgnoreCase);
            foreach (string introImage in introImages)
            {
                targets.Add(new TextureTarget
                {
                    path = introImage.Replace('\\', '/'),
                    maxSize = 2048,
                    crunch = true
                });
            }
        }

        return targets;
    }

    [Serializable]
    private class BackupFile
    {
        public List<TextureBackup> items = new List<TextureBackup>();
    }

    [Serializable]
    private class TextureBackup
    {
        public string path;
        public bool hadOverride;
        public int maxTextureSize;
        public int resizeAlgorithm;
        public int format;
        public int textureCompression;
        public int compressionQuality;
        public bool crunchedCompression;
        public bool allowsAlphaSplitting;
    }

    [MenuItem("Tools/Motor City/Texture Compression/1 - Apply Bulk WebGL Texture Compression")]
    private static void ApplyPilot()
    {
        if (File.Exists(BackupPath))
        {
            EditorUtility.DisplayDialog(
                "Texture Compression Pilot",
                "A backup already exists. Restore the previous settings before applying another pilot.",
                "OK");
            return;
        }

        BackupFile backup = new BackupFile();
        List<TextureImporter> importers = new List<TextureImporter>();
        List<TextureTarget> targets = GetTargetTextures();

        foreach (TextureTarget target in targets)
        {
            string path = target.path;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                EditorUtility.DisplayDialog(
                    "Texture Compression Pilot",
                    "Texture not found or has no TextureImporter:\n" + path +
                    "\nNo settings were changed.",
                    "OK");
                return;
            }

            TextureImporterPlatformSettings current = importer.GetPlatformTextureSettings("WebGL");
            backup.items.Add(new TextureBackup
            {
                path = path,
                hadOverride = current != null && current.overridden,
                maxTextureSize = current.maxTextureSize,
                resizeAlgorithm = (int)current.resizeAlgorithm,
                format = (int)current.format,
                textureCompression = (int)current.textureCompression,
                compressionQuality = current.compressionQuality,
                crunchedCompression = current.crunchedCompression,
                allowsAlphaSplitting = current.allowsAlphaSplitting,
            });
            importers.Add(importer);
        }

        File.WriteAllText(BackupPath, JsonUtility.ToJson(backup, true));

        try
        {
            foreach (TextureImporter importer in importers)
            {
                TextureImporterPlatformSettings settings =
                    importer.GetPlatformTextureSettings("WebGL");

                settings.name = "WebGL";
                settings.overridden = true;
                TextureTarget target = targets.Find(item =>
                    string.Equals(item.path, importer.assetPath, StringComparison.OrdinalIgnoreCase));
                if (target == null)
                    throw new InvalidOperationException(
                        "Compression target disappeared: " + importer.assetPath);

                settings.maxTextureSize = target.maxSize;
                settings.textureCompression = TextureImporterCompression.Compressed;
                settings.compressionQuality = 50;
                settings.crunchedCompression = target.crunch;

                importer.SetPlatformTextureSettings(settings);
                EditorUtility.SetDirty(importer);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (TextureImporter importer in importers)
                importer.SaveAndReimport();

            EditorUtility.DisplayDialog(
                "Texture Compression Pilot Applied",
                "WebGL compression settings were applied to " + targets.Count + " large textures.\n\n" +
                "City atlases keep their key 2048px resolution; sky panoramas are capped at 1024px; " +
                "intro artwork stays up to 2048px. Normal maps use standard compression without Crunch.\n\n" +
                "Original WebGL importer settings are backed up in Library.\n" +
                "To undo: Tools > Motor City > Texture Compression > 2 - Restore Original Settings.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "Texture Compression Pilot",
                "An error occurred while applying settings. Use Restore Original Settings if the backup was created.\n" +
                exception.Message,
                "OK");
        }
    }

    [MenuItem("Tools/Motor City/Texture Compression/2 - Restore Original Settings")]
    private static void RestorePilot()
    {
        if (!File.Exists(BackupPath))
        {
            EditorUtility.DisplayDialog(
                "Texture Compression Pilot",
                "No pilot backup was found. Nothing to restore.",
                "OK");
            return;
        }

        BackupFile backup = JsonUtility.FromJson<BackupFile>(File.ReadAllText(BackupPath));
        if (backup == null || backup.items == null)
        {
            EditorUtility.DisplayDialog(
                "Texture Compression Pilot",
                "The backup file could not be read. It was left untouched.",
                "OK");
            return;
        }

        foreach (TextureBackup item in backup.items)
        {
            TextureImporter importer = AssetImporter.GetAtPath(item.path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Texture Compression Pilot] Missing texture: " + item.path);
                continue;
            }

            if (!item.hadOverride)
            {
                importer.ClearPlatformTextureSettings("WebGL");
            }
            else
            {
                TextureImporterPlatformSettings settings =
                    importer.GetPlatformTextureSettings("WebGL");

                settings.name = "WebGL";
                settings.overridden = true;
                settings.maxTextureSize = item.maxTextureSize;
                settings.resizeAlgorithm = (TextureResizeAlgorithm)item.resizeAlgorithm;
                settings.format = (TextureImporterFormat)item.format;
                settings.textureCompression = (TextureImporterCompression)item.textureCompression;
                settings.compressionQuality = item.compressionQuality;
                settings.crunchedCompression = item.crunchedCompression;
                settings.allowsAlphaSplitting = item.allowsAlphaSplitting;

                importer.SetPlatformTextureSettings(settings);
            }

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        File.Delete(BackupPath);

        EditorUtility.DisplayDialog(
            "Texture Compression Pilot",
            "Original WebGL texture settings have been restored.",
            "OK");
    }

    [MenuItem("Tools/Motor City/Texture Compression/2 - Restore Original Settings", true)]
    private static bool ValidateRestore()
    {
        return File.Exists(BackupPath);
    }
}
