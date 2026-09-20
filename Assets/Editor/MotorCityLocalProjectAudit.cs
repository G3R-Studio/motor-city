#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MotorCityLocalProjectAudit
{
    private const string ReportPath =
        "MotorCity_LocalProjectAudit.txt";

    private const string ResourcesRoot =
        "Assets/Resources";

    private const string FcgRoot =
        "Assets/Fantastic City Generator";

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

        Dictionary<string, string[]> resourceMap =
            BuildResourceMap();

        HashSet<string> codeResourceRoots =
            FindResourceAssetsReferencedByMotorCityCode(
                resourceMap,
                out List<string> resourceMatches);

        HashSet<string> runtimeRoots =
            CollectRuntimeDependencyRoots(
                codeResourceRoots);

        HashSet<string> runtimeReachable =
            CollectDependencies(
                runtimeRoots);

        HashSet<string> savedCityRoots =
            CollectSavedCityRoots();

        HashSet<string> savedCityReachable =
            CollectDependencies(
                savedCityRoots);

        var builder =
            new StringBuilder(
                256 * 1024);

        builder.AppendLine(
            "MOTOR CITY — LOCAL PROJECT AUDIT");

        builder.AppendLine(
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        builder.AppendLine(
            $"Unity: {Application.unityVersion}");

        builder.AppendLine(
            $"Project: {projectRoot}");

        builder.AppendLine(
            "Deep cleanup mode: dependency closure + Motor City source-string Resources scan");

        builder.AppendLine();

        AppendSection(
            builder,
            "TOP-LEVEL ASSETS",
            EnumerateTopLevelAssets());

        AppendSection(
            builder,
            "LOCAL / IGNORED-LIKE PACKAGE CANDIDATES",
            EnumerateLocalPackageCandidates());

        AppendSection(
            builder,
            "EMPTY ASSET DIRECTORIES (META FILES IGNORED)",
            EnumerateEmptyAssetDirectories());

        AppendSection(
            builder,
            "KNOWN LEGACY / STALE PATHS",
            EnumerateKnownLegacyPaths());

        AppendSection(
            builder,
            "SCENES",
            EnumerateByExtension(
                ".unity"));

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
            "BUILD SCENES",
            EnumerateBuildScenes());

        AppendSection(
            builder,
            "DEPENDENCY ROOTS — RUNTIME + SAVED CITY",
            runtimeRoots
                .OrderBy(
                    value => value,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    root =>
                        $"{root} | dependencies={AssetDatabase.GetDependencies(root, true).Length}"));

        AppendSection(
            builder,
            "MOTOR CITY CODE -> RESOURCES STRING MATCHES",
            resourceMatches);

        AppendSection(
            builder,
            "RUNTIME REACHABLE SUMMARY",
            new[]
            {
                $"Roots={runtimeRoots.Count}",
                $"Reachable Assets={runtimeReachable.Count}",
                $"Reachable Resources Assets={runtimeReachable.Count(path => IsUnder(path, ResourcesRoot))}"
            });

        AppendSection(
            builder,
            "UNREFERENCED RESOURCES CANDIDATES",
            EnumerateUnreferencedResources(
                runtimeReachable));

        AppendSection(
            builder,
            "SAVED FCG CITY DEPENDENCY SUMMARY",
            EnumerateSavedCitySummary(
                savedCityRoots,
                savedCityReachable));

        AppendSection(
            builder,
            "FCG PACKAGE USAGE BY TOP-LEVEL FOLDER",
            EnumerateFcgUsageByFolder(
                savedCityReachable));

        AppendSection(
            builder,
            "FCG LARGE FILES NOT REFERENCED BY CURRENT SAVED CITY >= 1 MB",
            EnumerateUnusedFcgLargeFiles(
                savedCityReachable,
                1L * 1024L * 1024L));

        AppendSection(
            builder,
            "FCG DEMO / DOC / PLAYER CANDIDATES NOT USED BY CURRENT SAVED CITY",
            EnumerateUnusedFcgDemoCandidates(
                savedCityReachable));

        builder.AppendLine(
            "NOTES");

        builder.AppendLine(
            "- 'UNREFERENCED RESOURCES CANDIDATES' means no dependency from the enabled build scene, saved local city, or resource keys found as string literals in Motor City Scripts/Editor code.");

        builder.AppendLine(
            "- Resources can be loaded through computed strings, so this section is a cleanup candidate list, not an automatic deletion list.");

        builder.AppendLine(
            "- FCG unused sections describe the CURRENT saved city only. Deleting unused FCG package content can remove options needed to generate a different city later.");

        builder.AppendLine();

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
            "Motor City: deep local project audit exported to " +
            absolute);

        EditorUtility.RevealInFinder(
            absolute);
    }

    private static Dictionary<string, string[]> BuildResourceMap()
    {
        var grouped =
            new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);

        if (!AssetDatabase.IsValidFolder(
                ResourcesRoot))
        {
            return new Dictionary<string, string[]>(
                StringComparer.OrdinalIgnoreCase);
        }

        foreach (string guid in
                 AssetDatabase.FindAssets(
                     string.Empty,
                     new[]
                     {
                         ResourcesRoot
                     }))
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (string.IsNullOrWhiteSpace(
                    path) ||
                AssetDatabase.IsValidFolder(
                    path))
                continue;

            string key =
                GetResourceKey(
                    path);

            if (string.IsNullOrWhiteSpace(
                    key))
                continue;

            if (!grouped.TryGetValue(
                    key,
                    out List<string> values))
            {
                values =
                    new List<string>();

                grouped.Add(
                    key,
                    values);
            }

            values.Add(
                path);
        }

        return grouped.ToDictionary(
            pair => pair.Key,
            pair => pair.Value
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> FindResourceAssetsReferencedByMotorCityCode(
        Dictionary<string, string[]> resourceMap,
        out List<string> matches)
    {
        var result =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        matches =
            new List<string>();

        if (resourceMap.Count == 0)
            return result;

        string[] sourceRoots =
        {
            Path.Combine(
                Application.dataPath,
                "Scripts"),
            Path.Combine(
                Application.dataPath,
                "Editor")
        };

        var literalRegex =
            new Regex(
                "\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",
                RegexOptions.Compiled);

        foreach (string root in sourceRoots)
        {
            if (!Directory.Exists(
                    root))
                continue;

            foreach (string file in
                     Directory.GetFiles(
                         root,
                         "*.cs",
                         SearchOption.AllDirectories))
            {
                string source;

                try
                {
                    source =
                        File.ReadAllText(
                            file);
                }
                catch
                {
                    continue;
                }

                int lineNumber =
                    1;

                int previousIndex =
                    0;

                foreach (Match match in
                         literalRegex.Matches(
                             source))
                {
                    for (int i = previousIndex;
                         i < match.Index;
                         i++)
                    {
                        if (source[i] == '\n')
                            lineNumber++;
                    }

                    previousIndex =
                        match.Index;

                    string value =
                        match.Groups["value"].Value
                            .Replace(
                                "\\/",
                                "/");

                    foreach (KeyValuePair<string, string[]> pair in
                             resourceMap)
                    {
                        bool exact =
                            string.Equals(
                                value,
                                pair.Key,
                                StringComparison.OrdinalIgnoreCase);

                        bool folder =
                            pair.Key.StartsWith(
                                value + "/",
                                StringComparison.OrdinalIgnoreCase);

                        if (!exact &&
                            !folder)
                            continue;

                        foreach (string path in pair.Value)
                            result.Add(path);

                        matches.Add(
                            $"{ToAssetPath(file)}:{lineNumber} | \"{value}\" -> {string.Join(", ", pair.Value)}");
                    }
                }
            }
        }

        matches =
            matches
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    value => value,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        return result;
    }

    private static HashSet<string> CollectRuntimeDependencyRoots(
        HashSet<string> codeResourceRoots)
    {
        var roots =
            new HashSet<string>(
                codeResourceRoots,
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

        foreach (string root in
                 CollectSavedCityRoots())
        {
            roots.Add(
                root);
        }

        return roots;
    }

    private static HashSet<string> CollectSavedCityRoots()
    {
        var roots =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        if (!AssetDatabase.IsValidFolder(
                "Assets/LocalGenerated"))
            return roots;

        foreach (string guid in
                 AssetDatabase.FindAssets(
                     "t:Scene",
                     new[]
                     {
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

        return roots;
    }

    private static HashSet<string> CollectDependencies(
        IEnumerable<string> roots)
    {
        var dependencies =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (string root in roots)
        {
            if (string.IsNullOrWhiteSpace(
                    root))
                continue;

            dependencies.Add(
                root);

            foreach (string dependency in
                     AssetDatabase.GetDependencies(
                         root,
                         true))
            {
                dependencies.Add(
                    dependency);
            }
        }

        return dependencies;
    }

    private static IEnumerable<string> EnumerateUnreferencedResources(
        HashSet<string> reachable)
    {
        if (!AssetDatabase.IsValidFolder(
                ResourcesRoot))
            yield break;

        var candidates =
            new List<FileEntry>();

        foreach (string guid in
                 AssetDatabase.FindAssets(
                     string.Empty,
                     new[]
                     {
                         ResourcesRoot
                     }))
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (string.IsNullOrWhiteSpace(
                    path) ||
                AssetDatabase.IsValidFolder(
                    path) ||
                reachable.Contains(
                    path))
                continue;

            candidates.Add(
                new FileEntry(
                    path,
                    GetAssetFileSize(
                        path)));
        }

        foreach (FileEntry entry in
                 candidates
                     .OrderByDescending(
                         value => value.Bytes)
                     .ThenBy(
                         value => value.Path,
                         StringComparer.OrdinalIgnoreCase))
        {
            yield return
                $"{entry.Path} | {FormatBytes(entry.Bytes)}";
        }
    }

    private static IEnumerable<string> EnumerateSavedCitySummary(
        HashSet<string> roots,
        HashSet<string> dependencies)
    {
        yield return
            $"Saved city roots={roots.Count}";

        yield return
            $"Saved city dependency closure={dependencies.Count}";

        int fcgCount =
            dependencies.Count(
                path =>
                    IsUnder(
                        path,
                        FcgRoot));

        long fcgBytes =
            dependencies
                .Where(
                    path =>
                        IsUnder(
                            path,
                            FcgRoot))
                .Sum(
                    GetAssetFileSize);

        yield return
            $"Referenced FCG assets={fcgCount} | {FormatBytes(fcgBytes)}";
    }

    private static IEnumerable<string> EnumerateFcgUsageByFolder(
        HashSet<string> savedCityReachable)
    {
        if (!AssetDatabase.IsValidFolder(
                FcgRoot))
            yield break;

        List<FileEntry> all =
            EnumerateAssetFiles(
                    FcgRoot)
                .ToList();

        foreach (IGrouping<string, FileEntry> group in
                 all.GroupBy(
                         entry =>
                             GetTopLevelBucket(
                                 entry.Path,
                                 FcgRoot),
                         StringComparer.OrdinalIgnoreCase)
                     .OrderBy(
                         group => group.Key,
                         StringComparer.OrdinalIgnoreCase))
        {
            int used =
                group.Count(
                    entry =>
                        savedCityReachable.Contains(
                            entry.Path));

            int unused =
                group.Count() -
                used;

            long usedBytes =
                group
                    .Where(
                        entry =>
                            savedCityReachable.Contains(
                                entry.Path))
                    .Sum(
                        entry => entry.Bytes);

            long unusedBytes =
                group
                    .Where(
                        entry =>
                            !savedCityReachable.Contains(
                                entry.Path))
                    .Sum(
                        entry => entry.Bytes);

            yield return
                $"{group.Key} | used={used} ({FormatBytes(usedBytes)}) | current-city-unreferenced={unused} ({FormatBytes(unusedBytes)})";
        }
    }

    private static IEnumerable<string> EnumerateUnusedFcgLargeFiles(
        HashSet<string> savedCityReachable,
        long minimumBytes)
    {
        if (!AssetDatabase.IsValidFolder(
                FcgRoot))
            yield break;

        foreach (FileEntry entry in
                 EnumerateAssetFiles(
                         FcgRoot)
                     .Where(
                         entry =>
                             entry.Bytes >= minimumBytes &&
                             !savedCityReachable.Contains(
                                 entry.Path))
                     .OrderByDescending(
                         entry => entry.Bytes)
                     .ThenBy(
                         entry => entry.Path,
                         StringComparer.OrdinalIgnoreCase)
                     .Take(
                         150))
        {
            yield return
                $"{entry.Path} | {FormatBytes(entry.Bytes)}";
        }
    }

    private static IEnumerable<string> EnumerateUnusedFcgDemoCandidates(
        HashSet<string> savedCityReachable)
    {
        if (!AssetDatabase.IsValidFolder(
                FcgRoot))
            yield break;

        string[] prefixes =
        {
            FcgRoot + "/Documentation/",
            FcgRoot + "/Player/",
            FcgRoot + "/Scenes/Scene-Demo",
            FcgRoot + "/Scenes/Scene-Mobile"
        };

        foreach (FileEntry entry in
                 EnumerateAssetFiles(
                         FcgRoot)
                     .Where(
                         entry =>
                             prefixes.Any(
                                 prefix =>
                                     entry.Path.StartsWith(
                                         prefix,
                                         StringComparison.OrdinalIgnoreCase)) &&
                             !savedCityReachable.Contains(
                                 entry.Path))
                     .OrderByDescending(
                         entry => entry.Bytes)
                     .ThenBy(
                         entry => entry.Path,
                         StringComparer.OrdinalIgnoreCase))
        {
            yield return
                $"{entry.Path} | {FormatBytes(entry.Bytes)}";
        }
    }

    private static IEnumerable<FileEntry> EnumerateAssetFiles(
        string assetRoot)
    {
        string absoluteRoot =
            ToAbsolutePath(
                assetRoot);

        if (!Directory.Exists(
                absoluteRoot))
            yield break;

        foreach (string file in
                 Directory.GetFiles(
                     absoluteRoot,
                     "*",
                     SearchOption.AllDirectories))
        {
            if (file.EndsWith(
                    ".meta",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            long bytes;

            try
            {
                bytes =
                    new FileInfo(
                        file).Length;
            }
            catch
            {
                continue;
            }

            yield return
                new FileEntry(
                    ToAssetPath(
                        file),
                    bytes);
        }
    }

    private static IEnumerable<string> EnumerateEmptyAssetDirectories()
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
                     .OrderByDescending(
                         path => path.Length)
                     .ThenBy(
                         path => path,
                         StringComparer.OrdinalIgnoreCase))
        {
            bool hasRealFiles;

            try
            {
                hasRealFiles =
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
                continue;
            }

            if (!hasRealFiles)
            {
                yield return
                    ToAssetPath(
                        directory);
            }
        }
    }

    private static IEnumerable<string> EnumerateKnownLegacyPaths()
    {
        string[] paths =
        {
            "Assets/City",
            "Assets/FCG",
            "Assets/LocalAudio",
            "Assets/Vehicle_Essentials",
            "Assets/Resources/MotorCity/Audio",
            "Assets/Resources/MotorCity/Environment/ModernCityMaterials",
            "Assets/Scripts/Audio"
        };

        foreach (string path in paths)
        {
            string absolute =
                ToAbsolutePath(
                    path);

            if (!Directory.Exists(
                    absolute) &&
                !File.Exists(
                    absolute))
                continue;

            long bytes =
                Directory.Exists(
                        absolute)
                    ? DirectorySize(
                        absolute)
                    : new FileInfo(
                        absolute).Length;

            yield return
                $"{path} | exists | {FormatBytes(bytes)}";
        }
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
                    keyword =>
                        lower.Contains(
                            keyword)))
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

    private static IEnumerable<string> EnumerateBuildScenes()
    {
        foreach (EditorBuildSettingsScene scene in
                 EditorBuildSettings.scenes)
        {
            yield return
                $"{(scene.enabled ? "ENABLED" : "DISABLED")} | {scene.path}";
        }
    }

    private static string GetResourceKey(
        string assetPath)
    {
        const string marker =
            "/Resources/";

        int markerIndex =
            assetPath.IndexOf(
                marker,
                StringComparison.OrdinalIgnoreCase);

        if (markerIndex < 0)
            return string.Empty;

        string relative =
            assetPath.Substring(
                markerIndex +
                marker.Length);

        string extension =
            Path.GetExtension(
                relative);

        if (!string.IsNullOrWhiteSpace(
                extension))
        {
            relative =
                relative.Substring(
                    0,
                    relative.Length -
                    extension.Length);
        }

        return
            relative.Replace(
                '\\',
                '/');
    }

    private static string GetTopLevelBucket(
        string assetPath,
        string root)
    {
        string prefix =
            root.TrimEnd('/') +
            "/";

        if (!assetPath.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            return "<outside>";

        string relative =
            assetPath.Substring(
                prefix.Length);

        int slash =
            relative.IndexOf('/');

        return
            slash >= 0
                ? relative.Substring(
                    0,
                    slash)
                : "<root>";
    }

    private static bool IsUnder(
        string assetPath,
        string root)
    {
        return
            string.Equals(
                assetPath,
                root,
                StringComparison.OrdinalIgnoreCase) ||
            assetPath.StartsWith(
                root.TrimEnd('/') + "/",
                StringComparison.OrdinalIgnoreCase);
    }

    private static long GetAssetFileSize(
        string assetPath)
    {
        if (string.IsNullOrWhiteSpace(
                assetPath) ||
            !assetPath.StartsWith(
                "Assets/",
                StringComparison.OrdinalIgnoreCase))
            return 0;

        string absolute =
            ToAbsolutePath(
                assetPath);

        if (!File.Exists(
                absolute))
            return 0;

        try
        {
            return
                new FileInfo(
                    absolute).Length;
        }
        catch
        {
            return 0;
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

    private readonly struct FileEntry
    {
        public string Path { get; }
        public long Bytes { get; }

        public FileEntry(
            string path,
            long bytes)
        {
            Path =
                path;

            Bytes =
                bytes;
        }
    }
}
#endif
