#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MotorCityLocalProjectAudit
{
    private const string ReportPath =
        "MotorCity_LocalProjectAudit.txt";

    [MenuItem("Motor City/Diagnostics/Export Local Project Audit")]
    public static void ExportAudit()
    {
        string projectRoot =
            Directory.GetParent(
                    Application.dataPath)
                ?.FullName;

        if (string.IsNullOrWhiteSpace(
                projectRoot))
        {
            Debug.LogError(
                "Motor City: could not resolve project root for local audit.");
            return;
        }

        var builder =
            new StringBuilder(
                128 * 1024);

        builder.AppendLine(
            "MOTOR CITY — LOCAL PROJECT AUDIT");

        builder.AppendLine(
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        builder.AppendLine(
            $"Unity: {Application.unityVersion}");

        builder.AppendLine(
            $"Project: {projectRoot}");

        builder.AppendLine();

        AppendSection(
            builder,
            "TOP-LEVEL ASSETS",
            EnumerateTopLevelAssets());

        AppendSection(
            builder,
            "ALL ASSET FOLDERS",
            EnumerateAssetFolders());

        AppendSection(
            builder,
            "LOCAL / IGNORED-LIKE PACKAGE CANDIDATES",
            EnumerateLocalPackageCandidates());

        AppendSection(
            builder,
            "SCENES",
            EnumerateByExtension(
                ".unity"));

        AppendSection(
            builder,
            "PREFABS",
            EnumerateByExtension(
                ".prefab"));

        AppendSection(
            builder,
            "AUDIO FILES",
            EnumerateExtensions(
                ".wav",
                ".mp3",
                ".ogg",
                ".aif",
                ".aiff"));

        AppendSection(
            builder,
            "ARCHIVES",
            EnumerateExtensions(
                ".zip",
                ".rar",
                ".7z",
                ".unitypackage"));

        AppendSection(
            builder,
            "LARGE FILES >= 20 MB",
            EnumerateLargeFiles(
                20L * 1024L * 1024L));

        AppendSection(
            builder,
            "RESOURCES ASSETS",
            EnumerateResources());

        AppendSection(
            builder,
            "BUILD SCENES",
            EnumerateBuildScenes());

        AppendSection(
            builder,
            "ROOT OBJECTS IN SAVED SCENES",
            EnumerateSceneRoots());

        AppendSection(
            builder,
            "DEPENDENCY ROOTS",
            EnumerateDependencyRoots());

        string absolute =
            Path.Combine(
                projectRoot,
                ReportPath);

        File.WriteAllText(
            absolute,
            builder.ToString(),
            Encoding.UTF8);

        AssetDatabase.Refresh();

        Debug.Log(
            "Motor City: local project audit exported to " +
            absolute);

        EditorUtility.RevealInFinder(
            absolute);
    }

    private static IEnumerable<string> EnumerateTopLevelAssets()
    {
        if (!Directory.Exists(
                Application.dataPath))
            yield break;

        foreach (string directory in
                 Directory
                     .GetDirectories(
                         Application.dataPath)
                     .OrderBy(
                         path => path,
                         StringComparer.OrdinalIgnoreCase))
        {
            yield return
                "DIR  " +
                Path.GetFileName(
                    directory);
        }

        foreach (string file in
                 Directory
                     .GetFiles(
                         Application.dataPath)
                     .Where(
                         path =>
                             !path.EndsWith(
                                 ".meta",
                                 StringComparison.OrdinalIgnoreCase))
                     .OrderBy(
                         path => path,
                         StringComparer.OrdinalIgnoreCase))
        {
            yield return
                "FILE " +
                Path.GetFileName(
                    file);
        }
    }

    private static IEnumerable<string> EnumerateAssetFolders()
    {
        if (!Directory.Exists(
                Application.dataPath))
            yield break;

        foreach (string directory in
                 Directory
                     .GetDirectories(
                         Application.dataPath,
                         "*",
                         SearchOption.AllDirectories)
                     .OrderBy(
                         path => path,
                         StringComparer.OrdinalIgnoreCase))
        {
            yield return
                ToAssetPath(
                    directory);
        }
    }

    private static IEnumerable<string> EnumerateLocalPackageCandidates()
    {
        string[] keywords =
        {
            "fantastic",
            "prometeo",
            "arcade",
            "mena",
            "mcp",
            "cubex",
            "versatile",
            "zrn",
            "otaku",
            "city",
            "vehicle",
            "nox",
            "audio",
            "kenney",
            "assetstore"
        };

        foreach (string directory in
                 Directory
                     .GetDirectories(
                         Application.dataPath)
                     .OrderBy(
                         path => path,
                         StringComparer.OrdinalIgnoreCase))
        {
            string name =
                Path.GetFileName(
                    directory);

            string lower =
                name.ToLowerInvariant();

            if (!keywords.Any(
                    lower.Contains))
                continue;

            long size =
                DirectorySize(
                    directory);

            yield return
                $"{ToAssetPath(directory)} | {FormatBytes(size)}";
        }
    }

    private static IEnumerable<string> EnumerateByExtension(
        string extension)
    {
        return
            EnumerateExtensions(
                extension);
    }

    private static IEnumerable<string> EnumerateExtensions(
        params string[] extensions)
    {
        var allowed =
            new HashSet<string>(
                extensions,
                StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(
                Application.dataPath))
            yield break;

        foreach (string file in
                 Directory
                     .GetFiles(
                         Application.dataPath,
                         "*",
                         SearchOption.AllDirectories)
                     .Where(
                         path =>
                             allowed.Contains(
                                 Path.GetExtension(
                                     path)))
                     .OrderBy(
                         path => path,
                         StringComparer.OrdinalIgnoreCase))
        {
            FileInfo info =
                new(file);

            yield return
                $"{ToAssetPath(file)} | {FormatBytes(info.Length)}";
        }
    }

    private static IEnumerable<string> EnumerateLargeFiles(
        long minimumBytes)
    {
        if (!Directory.Exists(
                Application.dataPath))
            yield break;

        foreach (string file in
                 Directory.GetFiles(
                     Application.dataPath,
                     "*",
                     SearchOption.AllDirectories))
        {
            if (file.EndsWith(
                    ".meta",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            FileInfo info;

            try
            {
                info =
                    new FileInfo(
                        file);
            }
            catch
            {
                continue;
            }

            if (info.Length <
                minimumBytes)
                continue;

            yield return
                $"{ToAssetPath(file)} | {FormatBytes(info.Length)}";
        }
    }

    private static IEnumerable<string> EnumerateResources()
    {
        foreach (string guid in
                 AssetDatabase.FindAssets(
                     string.Empty,
                     new[]
                     {
                         "Assets/Resources"
                     }))
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (AssetDatabase.IsValidFolder(
                    path))
                continue;

            yield return path;
        }
    }

    private static IEnumerable<string> EnumerateBuildScenes()
    {
        foreach (EditorBuildSettingsScene scene in
                 EditorBuildSettings.scenes)
        {
            yield return
                $"{(scene.enabled ? "ENABLED" : "DISABLED")} | {scene.path}";
        }
    }

    private static IEnumerable<string> EnumerateSceneRoots()
    {
        string[] sceneGuids =
            AssetDatabase.FindAssets(
                "t:Scene",
                new[]
                {
                    "Assets"
                });

        foreach (string guid in
                 sceneGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            var sceneAsset =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    path);

            if (sceneAsset == null)
                continue;

            yield return path;
        }
    }

    private static IEnumerable<string> EnumerateDependencyRoots()
    {
        var roots =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (EditorBuildSettingsScene scene in
                 EditorBuildSettings.scenes)
        {
            if (scene.enabled &&
                !string.IsNullOrWhiteSpace(
                    scene.path))
            {
                roots.Add(
                    scene.path);
            }
        }

        foreach (string guid in
                 AssetDatabase.FindAssets(
                     "t:Scene",
                     new[]
                     {
                         "Assets/Scenes",
                         "Assets/LocalGenerated"
                     }))
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (!string.IsNullOrWhiteSpace(
                    path))
            {
                roots.Add(
                    path);
            }
        }

        foreach (string root in
                 roots.OrderBy(
                     value => value,
                     StringComparer.OrdinalIgnoreCase))
        {
            string[] dependencies =
                AssetDatabase.GetDependencies(
                    root,
                    true);

            yield return
                $"{root} | dependencies={dependencies.Length}";
        }
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IEnumerable<string> lines)
    {
        builder.AppendLine(
            "============================================================");

        builder.AppendLine(
            title);

        builder.AppendLine(
            "============================================================");

        int count =
            0;

        foreach (string line in lines)
        {
            builder.AppendLine(
                line);

            count++;
        }

        if (count == 0)
        {
            builder.AppendLine(
                "<none>");
        }

        builder.AppendLine();
    }

    private static string ToAssetPath(
        string absolutePath)
    {
        string normalized =
            absolutePath.Replace(
                '\\',
                '/');

        string normalizedAssets =
            Application.dataPath.Replace(
                '\\',
                '/');

        if (!normalized.StartsWith(
                normalizedAssets,
                StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return
            "Assets" +
            normalized.Substring(
                normalizedAssets.Length);
    }

    private static long DirectorySize(
        string directory)
    {
        long total =
            0;

        try
        {
            foreach (string file in
                     Directory.GetFiles(
                         directory,
                         "*",
                         SearchOption.AllDirectories))
            {
                if (file.EndsWith(
                        ".meta",
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    total +=
                        new FileInfo(
                            file).Length;
                }
                catch
                {
                    // Ignore files being rewritten by Unity/import workers.
                }
            }
        }
        catch
        {
            // Keep audit usable even if a package folder contains a locked file.
        }

        return total;
    }

    private static string FormatBytes(
        long bytes)
    {
        double value =
            bytes;

        string[] units =
        {
            "B",
            "KB",
            "MB",
            "GB"
        };

        int unit =
            0;

        while (value >= 1024d &&
               unit <
               units.Length - 1)
        {
            value /=
                1024d;

            unit++;
        }

        return
            $"{value:0.##} {units[unit]}";
    }
}
#endif
