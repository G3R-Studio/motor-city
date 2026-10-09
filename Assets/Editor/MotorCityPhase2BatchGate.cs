using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>Optional local/CI Unity 6 batch-mode verification. Never changes build profiles.</summary>
    public static class MotorCityPhase2BatchGate
    {
        public static void Validate()
        {
            string[] scenes = FindScenes();
            string folder = ReportFolder();
            Directory.CreateDirectory(folder);
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled build scenes");
            foreach (string scene in scenes)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                    throw new InvalidOperationException("Missing build scene: " + scene);

            // Phase 3: load each authored vehicle prefab and check wheel contracts.
            MotorCityVehicleContractGate.Validate();
            MotorCityVehicleMaterialRoleAudit.Validate();
            MotorCityPhase5DependencyAudit.Validate();
            if (Environment.GetEnvironmentVariable("MOTORCITY_PHASE3_REBUILD") == "1")
                MotorCityImporterRebuildGate.Validate();

            // Existing dependency reporter resolves scene, vehicle and city dependencies.
            if (!EditorApplication.ExecuteMenuItem("Tools/Motor City/Build/Generate Dependency Report"))
                throw new InvalidOperationException("Dependency report menu command failed");

            string dependency = Path.GetFullPath("Library/MotorCityBuildDependencyReport.txt");
            if (!File.Exists(dependency))
                throw new InvalidOperationException("Dependency report was not generated");
            File.Copy(dependency, Path.Combine(folder, "unity-dependencies.txt"), true);
            File.WriteAllText(Path.Combine(folder, "unity-compile.txt"),
                "Unity Editor compilation and scene import completed.\n" +
                "Unity version: " + Application.unityVersion + "\n" +
                "Enabled scenes: " + string.Join(", ", scenes) + "\n");
            Debug.Log("Motor City Phase 2 Unity validation succeeded; reports: " + folder);
        }

        public static void BuildWebGL()
        {
            Validate();
            string folder = ReportFolder();
            string destination = Path.Combine(folder, "WebGL");
            Directory.CreateDirectory(destination);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = FindScenes(),
                locationPathName = destination,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            var summary = report.summary;
            File.WriteAllText(Path.Combine(folder, "unity-build.txt"),
                "Result: " + summary.result + "\n" +
                "Platform: " + summary.platform + "\n" +
                "Size (bytes): " + summary.totalSize + "\n" +
                "Duration: " + summary.totalTime + "\n" +
                "Unity version: " + Application.unityVersion + "\n");
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("WebGL build failed: " + summary.result);
            Debug.Log("Motor City Phase 2 WebGL build passed.");
        }

        private static string[] FindScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene != null && scene.enabled && !string.IsNullOrEmpty(scene.path))
                .Select(scene => scene.path)
                .ToArray();
        }

        private static string ReportFolder()
        {
            string configured = Environment.GetEnvironmentVariable("MOTORCITY_PHASE2_REPORT_DIR");
            return Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
                ? "Temp/MotorCityAudit/UnityPhase2" : configured);
        }
    }
}
