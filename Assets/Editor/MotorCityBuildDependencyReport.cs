using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MotorCity.EditorTools
{
    public sealed class MotorCityBuildDependencyReport :
        IPostprocessBuildWithReport
    {
        private const string ReportPath =
            "Library/MotorCityBuildDependencyReport.txt";

        public int callbackOrder => 1000;

        [MenuItem(
            "Tools/Motor City/Build/Generate Dependency Report")]
        private static void GenerateFromMenu()
        {
            WriteReport(
                null);

            Debug.Log(
                "Motor City build dependency report written to " +
                ReportPath);
        }

        public void OnPostprocessBuild(
            BuildReport report)
        {
            WriteReport(
                report);
        }

        private static void WriteReport(
            BuildReport report)
        {
            string[] scenes =
                EditorBuildSettings.scenes
                    .Where(scene =>
                        scene != null &&
                        scene.enabled &&
                        !string.IsNullOrWhiteSpace(
                            scene.path))
                    .Select(scene =>
                        scene.path)
                    .ToArray();

            HashSet<string> dependencies =
                new(
                    StringComparer.OrdinalIgnoreCase);

            foreach (string scene in scenes)
            {
                dependencies.Add(
                    scene);

                string[] sceneDependencies =
                    AssetDatabase.GetDependencies(
                        scene,
                        true);

                foreach (string dependency in
                         sceneDependencies)
                {
                    if (!string.IsNullOrWhiteSpace(
                            dependency))
                    {
                        dependencies.Add(
                            dependency);
                    }
                }
            }

            List<AssetSize> sizedAssets =
                new();

            foreach (string dependency in
                     dependencies)
            {
                long bytes =
                    TryGetFileSize(
                        dependency);

                sizedAssets.Add(
                    new AssetSize(
                        dependency,
                        bytes));
            }

            sizedAssets.Sort(
                (left, right) =>
                    right.Bytes.CompareTo(
                        left.Bytes));

            StringBuilder builder =
                new();

            builder.AppendLine(
                "MOTOR CITY BUILD DEPENDENCY REPORT");

            builder.AppendLine(
                "Generated: " +
                DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss"));

            builder.AppendLine();

            if (report != null)
            {
                builder.AppendLine(
                    "Build result: " +
                    report.summary.result);

                builder.AppendLine(
                    "Build target: " +
                    report.summary.platform);

                builder.AppendLine(
                    "Build output size: " +
                    FormatBytes(
                        report.summary.totalSize));

                builder.AppendLine(
                    "Build duration: " +
                    report.summary.totalTime);

                builder.AppendLine();
            }

            builder.AppendLine(
                "Enabled build scenes (" +
                scenes.Length +
                "):");

            foreach (string scene in scenes)
            {
                builder.AppendLine(
                    "  " +
                    scene);
            }

            builder.AppendLine();

            builder.AppendLine(
                "Unique scene dependencies: " +
                dependencies.Count);

            long totalRawBytes =
                sizedAssets.Sum(
                    item =>
                        item.Bytes);

            builder.AppendLine(
                "Raw dependency file size: " +
                FormatBytes(
                    totalRawBytes));

            builder.AppendLine();

            builder.AppendLine(
                "Largest dependency files:");

            int count =
                Mathf.Min(
                    100,
                    sizedAssets.Count);

            for (int i = 0;
                 i < count;
                 i++)
            {
                AssetSize item =
                    sizedAssets[i];

                builder.Append(
                    (i + 1)
                    .ToString()
                    .PadLeft(3));

                builder.Append(
                    ". ");

                builder.Append(
                    FormatBytes(
                        item.Bytes)
                    .PadLeft(12));

                builder.Append(
                    "  ");

                builder.AppendLine(
                    item.Path);
            }

            builder.AppendLine();

            AppendDependencySection(
                builder,
                "FCG / CITY",
                new[]
                {
                    "Assets/Resources/MotorCity/Environment/CityVisual.prefab"
                });

            AppendDependencySection(
                builder,
                "PIXIE / BYTE",
                new[]
                {
                    "Assets/Resources/MotorCity/Byte/HaonByteVisual.prefab"
                });

            string[] vehicleRoots =
                AssetDatabase.FindAssets(
                        "t:Prefab",
                        new[]
                        {
                            "Assets/Resources/MotorCity/Vehicles/Player"
                        })
                    .Select(
                        AssetDatabase.GUIDToAssetPath)
                    .Where(
                        path =>
                            !string.IsNullOrWhiteSpace(
                                path))
                    .OrderBy(
                        path =>
                            path,
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            AppendDependencySection(
                builder,
                "PLAYER VEHICLES",
                vehicleRoots);

            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    ReportPath) ??
                "Library");

            File.WriteAllText(
                ReportPath,
                builder.ToString(),
                Encoding.UTF8);
        }

        private static void AppendDependencySection(
            StringBuilder builder,
            string title,
            IEnumerable<string> roots)
        {
            string[] rootArray =
                roots?
                    .Where(
                        path =>
                            !string.IsNullOrWhiteSpace(
                                path))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray() ??
                Array.Empty<string>();

            HashSet<string> dependencies =
                new(
                    StringComparer.OrdinalIgnoreCase);

            foreach (string root in rootArray)
            {
                if (!File.Exists(
                        Path.GetFullPath(
                            root)))
                {
                    continue;
                }

                dependencies.Add(
                    root);

                foreach (string dependency in
                         AssetDatabase.GetDependencies(
                             root,
                             true))
                {
                    if (!string.IsNullOrWhiteSpace(
                            dependency))
                    {
                        dependencies.Add(
                            dependency);
                    }
                }
            }

            List<AssetSize> sized =
                dependencies
                    .Select(
                        path =>
                            new AssetSize(
                                path,
                                TryGetFileSize(
                                    path)))
                    .OrderByDescending(
                        item =>
                            item.Bytes)
                    .ToList();

            long total =
                sized.Sum(
                    item =>
                        item.Bytes);

            builder.AppendLine(
                "============================================================");

            builder.AppendLine(
                title);

            builder.AppendLine(
                "Roots: " +
                rootArray.Length);

            builder.AppendLine(
                "Unique dependencies: " +
                dependencies.Count);

            builder.AppendLine(
                "Raw dependency file size: " +
                FormatBytes(
                    total));

            int count =
                Mathf.Min(
                    30,
                    sized.Count);

            for (int i = 0;
                 i < count;
                 i++)
            {
                AssetSize item =
                    sized[i];

                builder.Append(
                    "  ");

                builder.Append(
                    FormatBytes(
                        item.Bytes)
                    .PadLeft(12));

                builder.Append(
                    "  ");

                builder.AppendLine(
                    item.Path);
            }

            builder.AppendLine();
        }

        private static long TryGetFileSize(
            string assetPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(
                        assetPath))
                {
                    return 0L;
                }

                string fullPath =
                    Path.GetFullPath(
                        assetPath);

                return File.Exists(
                           fullPath)
                    ? new FileInfo(
                        fullPath).Length
                    : 0L;
            }
            catch
            {
                return 0L;
            }
        }

        private static string FormatBytes(
            ulong bytes)
        {
            return FormatBytes(
                (long)Math.Min(
                    bytes,
                    (ulong)long.MaxValue));
        }

        private static string FormatBytes(
            long bytes)
        {
            if (bytes >=
                1024L * 1024L * 1024L)
            {
                return
                    (bytes /
                     (1024d * 1024d * 1024d))
                    .ToString(
                        "0.00") +
                    " GiB";
            }

            if (bytes >=
                1024L * 1024L)
            {
                return
                    (bytes /
                     (1024d * 1024d))
                    .ToString(
                        "0.00") +
                    " MiB";
            }

            if (bytes >= 1024L)
            {
                return
                    (bytes /
                     1024d)
                    .ToString(
                        "0.00") +
                    " KiB";
            }

            return
                bytes +
                " B";
        }

        private readonly struct AssetSize
        {
            public readonly string Path;
            public readonly long Bytes;

            public AssetSize(
                string path,
                long bytes)
            {
                Path =
                    path;

                Bytes =
                    Math.Max(
                        0L,
                        bytes);
            }
        }
    }
}
