#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Repairs legacy Fantastic City Generator FBX importer metadata left by old
/// Unity versions. Unity 6 no longer supports ModelImporter materialLocation
/// value 0 (External), and prints "MaterialLocation.External is obsolete".
///
/// The migration is intentionally text-based: touching only the obsolete
/// serialized enum keeps every existing externalObjects material GUID binding,
/// mesh setting and importer option intact.
/// </summary>
public static class FantasticCityGeneratorLegacyImporterFixer
{
    private static readonly string[] LegacyAssetRoots =
    {
        "Assets/Fantastic City Generator",
        "Assets/Simple Garage"
    };

    private const string LegacyLine =
        "    materialLocation: 0";

    private const string SupportedLine =
        "    materialLocation: 1";

    [MenuItem("Motor City/Fantastic City Generator/0 - Repair Legacy FBX Importers")]
    public static void RepairAll()
    {
        RepairLegacyMaterialLocations(
            true);
    }

    private static void RepairLegacyMaterialLocations(
        bool showDialog)
    {
        int changed =
            0;

        var metaFiles =
            new System.Collections.Generic.List<string>();

        foreach (string assetRoot in LegacyAssetRoots)
        {
            string absoluteRoot =
                Path.GetFullPath(
                    assetRoot);

            if (!Directory.Exists(
                    absoluteRoot))
            {
                continue;
            }

            metaFiles.AddRange(
                Directory.GetFiles(
                    absoluteRoot,
                    "*.fbx.meta",
                    SearchOption.AllDirectories));
        }

        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (string metaPath in
                     metaFiles)
            {
                string text;

                try
                {
                    text =
                        File.ReadAllText(
                            metaPath);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[MotorCity][FCG] Could not inspect FBX meta: " +
                        metaPath +
                        "\n" +
                        exception.Message);

                    continue;
                }

                if (!text.Contains(
                        LegacyLine,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                string repaired =
                    text.Replace(
                        LegacyLine,
                        SupportedLine,
                        StringComparison.Ordinal);

                try
                {
                    File.WriteAllText(
                        metaPath,
                        repaired,
                        new UTF8Encoding(
                            false));

                    changed++;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[MotorCity][FCG] Could not repair FBX meta: " +
                        metaPath +
                        "\n" +
                        exception.Message);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        if (changed > 0)
        {
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceUpdate);

            if (showDialog)
            {
                Debug.Log(
                    "[MotorCity][Import] Repaired " +
                    changed +
                    " legacy FBX material-location settings.");
            }
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Motor City - FCG Legacy Importers",
                changed > 0
                    ? "Исправлено устаревших FBX importer-настроек: " +
                      changed +
                      ".\n\nUnity переимпортирует затронутые модели автоматически."
                    : "Устаревших materialLocation: External больше не найдено.",
                "OK");
        }
    }
}
#endif
