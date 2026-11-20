using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Reversible WebGL Crunch-compression pilot for four large city textures.
/// Saves original WebGL importer settings before applying changes.
/// Use Restore to return the exact previous WebGL platform settings.
/// </summary>
public static class MotorCityTextureCompressionPilot
{
    private const string BackupPath = "Library/MotorCityTextureCompressionPilotBackup.json";

    private static readonly string[] TargetTextures =
    {
        "Assets/Fantastic City Generator/Textures/Atlas-1.tga",
        "Assets/Fantastic City Generator/Textures/Atlas-1-N.png",
        "Assets/Fantastic City Generator/Textures/Wins.tga",
        "Assets/Fantastic City Generator/Textures/Roads.tga"
    };

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

    [MenuItem("Tools/Motor City/Texture Compression Pilot/1 - Apply Crunch to Test Textures")]
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

        foreach (string path in TargetTextures)
        {
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
                settings.textureCompression = TextureImporterCompression.Compressed;
                settings.crunchedCompression = true;

                importer.SetPlatformTextureSettings(settings);
                EditorUtility.SetDirty(importer);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (TextureImporter importer in importers)
                importer.SaveAndReimport();

            EditorUtility.DisplayDialog(
                "Texture Compression Pilot Applied",
                "Crunch compression was enabled for four test textures.\n\n" +
                "Original WebGL settings are backed up in Library.\n" +
                "Now make a WebGL build and compare its actual download size and visual quality.\n\n" +
                "To undo: Tools > Motor City > Texture Compression Pilot > 2 - Restore Original Settings.",
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

    [MenuItem("Tools/Motor City/Texture Compression Pilot/2 - Restore Original Settings")]
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

    [MenuItem("Tools/Motor City/Texture Compression Pilot/2 - Restore Original Settings", true)]
    private static bool ValidateRestore()
    {
        return File.Exists(BackupPath);
    }
}
