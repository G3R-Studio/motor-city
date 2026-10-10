using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Builds the exact Desktop/Mobile WebGL Release Build Profile selected by
    /// -activeBuildProfile. Never modifies the stored profile or PlayerSettings.
    /// </summary>
    public static class MotorCityPhase12ReleaseBuild
    {
        public static void Build()
        {
            string kind =
                Environment.GetEnvironmentVariable(
                    "MOTORCITY_PHASE12_PROFILE");
            if (kind != "Desktop" && kind != "Mobile")
                throw new InvalidOperationException(
                    "MOTORCITY_PHASE12_PROFILE must be Desktop or Mobile");

            string expected =
                "Assets/Settings/Build Profiles/Web - " +
                kind +
                " - Release.asset";

            BuildProfile active =
                BuildProfile.GetActiveBuildProfile();
            if (active == null)
                throw new InvalidOperationException(
                    "Phase 12 requires -activeBuildProfile " + expected);

            string actual =
                AssetDatabase.GetAssetPath(active).Replace('\\', '/');
            if (!string.Equals(
                    actual,
                    expected,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Wrong active Build Profile: " + actual +
                    "; expected: " + expected);
            }

            if (active.overrideGlobalScenes)
                throw new InvalidOperationException(
                    "Phase 12 profiles must use shared enabled build scenes");

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene =>
                    scene != null &&
                    scene.enabled &&
                    !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException(
                    "No enabled scenes for release");

            foreach (string scene in scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                    throw new InvalidOperationException(
                        "Missing release scene: " + scene);
            }

            if (EditorUserBuildSettings.development)
                throw new InvalidOperationException(
                    "Development Build enabled; refusing Phase 12 Release");

            string folder =
                Environment.GetEnvironmentVariable(
                    "MOTORCITY_PHASE12_REPORT_DIR");

            if (string.IsNullOrWhiteSpace(folder))
                folder = "Temp/MotorCityAudit/Phase12";

            folder = Path.GetFullPath(folder);
            Directory.CreateDirectory(folder);
            string destination =
                Path.Combine(folder, "WebGL-" + kind + "-Release");

            Directory.CreateDirectory(destination);

            string sha =
                Environment.GetEnvironmentVariable("GITHUB_SHA") ??
                "local-unversioned";

            Debug.Log(
                "Motor City Phase 12: Building " + expected +
                " to " + destination +
                " (commit " + sha + ")");

            var options = new BuildPlayerWithProfileOptions
            {
                buildProfile = active,
                locationPathName = destination,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report == null)
                throw new InvalidOperationException(
                    "BuildPipeline returned no BuildReport: " + expected);

            BuildSummary summary = report.summary;
            string reportFile =
                Path.Combine(
                    folder,
                    "unity-phase12-" + kind.ToLowerInvariant() + "-build.txt");

            File.WriteAllText(
                reportFile,
                "Motor City Phase 12 WebGL Release\n" +
                "Profile: " + expected + "\n" +
                "Build output: " + destination + "\n" +
                "Commit: " + sha + "\n" +
                "Unity: " + Application.unityVersion + "\n" +
                "Build result: " + summary.result + "\n" +
                "Build target: " + summary.platform + "\n" +
                "Development: " +
                    EditorUserBuildSettings.development + "\n" +
                "Output size (bytes): " + summary.totalSize + "\n" +
                "Duration: " + summary.totalTime + "\n" +
                "Scenes: " + string.Join(", ", scenes) + "\n");

            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    "Phase 12 " + kind +
                    " Release build failed: " + summary.result);

            if (summary.platform != BuildTarget.WebGL)
                throw new InvalidOperationException(
                    "Wrong build target: " + summary.platform);

            string index =
                Path.Combine(destination, "index.html");
            if (!File.Exists(index))
                throw new InvalidOperationException(
                    "Build reported success but index.html missing: " +
                    destination);

            Debug.Log(
                "Motor City Phase 12 " + kind +
                " WebGL Release build succeeded: " + reportFile);
        }
    }
}
