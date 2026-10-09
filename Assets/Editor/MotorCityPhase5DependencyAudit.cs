using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Report material reachability without deleting anything. Dynamic Resources.Load
    /// and editor builders mean an unreferenced asset is only a review candidate.
    /// </summary>
    public static class MotorCityPhase5DependencyAudit
    {
        public static void Validate()
        {
            string[] allMaterials = AssetDatabase.FindAssets("t:Material", new[]
            {
                "Assets/Resources/MotorCity"
            }).Select(AssetDatabase.GUIDToAssetPath)
              .Where(path => !string.IsNullOrWhiteSpace(path))
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .OrderBy(path => path, StringComparer.Ordinal)
              .ToArray();

            // Keep scene dependencies separate from Resources. Including every
            // Resources asset as a root would mark every material as reachable
            // and make the audit unable to distinguish load-only materials.
            var sceneRoots = EditorBuildSettings.scenes
                .Where(scene => scene != null && scene.enabled &&
                                !string.IsNullOrEmpty(scene.path))
                .Select(scene => scene.path)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var sceneDependencies = new HashSet<string>(
                AssetDatabase.GetDependencies(sceneRoots, true),
                StringComparer.OrdinalIgnoreCase);

            string[] resourceRoots = AssetDatabase.FindAssets("", new[]
            {
                "Assets/Resources/MotorCity"
            }).Select(AssetDatabase.GUIDToAssetPath)
              .Where(path => !string.IsNullOrWhiteSpace(path) &&
                             !AssetDatabase.IsValidFolder(path))
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .ToArray();
            var resourceDependencies = new HashSet<string>(
                AssetDatabase.GetDependencies(resourceRoots, true),
                StringComparer.OrdinalIgnoreCase);

            int sceneCount = 0;
            int resourceOnlyCount = 0;
            int protectedCount = 0;
            var lines = new List<string>
            {
                "Phase 5 material dependency inventory (read-only)",
                "SCENE_DEPENDENCY = linked from an enabled build scene.",
                "RESOURCE_DEPENDENCY = reachable via Resources content, but not enabled scenes.",
                "DYNAMIC_RESOURCE = in Resources, possibly loaded by runtime string even without static references.",
                "Every listed material is protected; no status proves safe deletion.",
                "Enabled scene roots: " + sceneRoots.Length,
                "Resources roots: " + resourceRoots.Length,
                "Material assets checked: " + allMaterials.Length
            };
            foreach (string material in allMaterials)
            {
                string category;
                if (sceneDependencies.Contains(material))
                {
                    category = "SCENE_DEPENDENCY";
                    sceneCount++;
                }
                else if (resourceDependencies.Contains(material))
                {
                    category = "RESOURCE_DEPENDENCY";
                    resourceOnlyCount++;
                }
                else
                {
                    category = "DYNAMIC_RESOURCE";
                    protectedCount++;
                }
                lines.Add(category + " | " + material);
            }
            lines.Add("Summary: scene=" + sceneCount +
                      ", resource-only=" + resourceOnlyCount +
                      ", dynamic=" + protectedCount);

            string folder = Path.GetFullPath("Temp/MotorCityAudit/UnityPhase2");
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "phase5-material-dependencies.txt"), lines);
            Debug.Log("Motor City Phase 5 material reachability audit succeeded: " + allMaterials.Length);
        }
    }
}
