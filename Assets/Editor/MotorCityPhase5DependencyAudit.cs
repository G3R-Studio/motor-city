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

            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene != null && scene.enabled && !string.IsNullOrEmpty(scene.path))
                    roots.Add(scene.path);

            // Any asset inside Resources can be loaded by string at runtime.
            foreach (string guid in AssetDatabase.FindAssets("", new[]
            {
                "Assets/Resources/MotorCity"
            }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrWhiteSpace(path) &&
                    !AssetDatabase.IsValidFolder(path))
                    roots.Add(path);
            }

            var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string asset in AssetDatabase.GetDependencies(roots.ToArray(), true))
                reachable.Add(asset);

            var lines = new List<string>
            {
                "Phase 5 material dependency inventory (read-only)",
                "All Resources material assets are protected from deletion even if not linked from an enabled scene.",
                "Root assets checked: " + roots.Count,
                "Material assets checked: " + allMaterials.Length
            };
            foreach (string material in allMaterials)
            {
                // A material in Resources is loadable dynamically regardless of the
                // static reachability result. Do not label it an orphan.
                lines.Add((reachable.Contains(material) ? "DEPENDENCY" : "DYNAMIC_RESOURCE")
                    + " | " + material);
            }

            string folder = Path.GetFullPath("Temp/MotorCityAudit/UnityPhase2");
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "phase5-material-dependencies.txt"), lines);
            Debug.Log("Motor City Phase 5 material reachability audit succeeded: " + allMaterials.Length);
        }
    }
}
