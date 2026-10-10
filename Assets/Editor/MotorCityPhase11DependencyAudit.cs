using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Read-only Unity import/dependency check for exact Git duplicate sources.
    /// Absence from serialized dependency graphs is not proof of safe deletion.
    /// </summary>
    public static class MotorCityPhase11DependencyAudit
    {
        private const string ResourceCircle =
            "Assets/Resources/MotorCity/UI/Loading/circle2.PNG";
        private const string OriginalCircle =
            "Assets/Eric VFX Studio/Resource/Textures/circle2.PNG";

        private static readonly string[] Candidates =
        {
            OriginalCircle,
            ResourceCircle,
            "Assets/VehicleAssets/AmgGT/all.png",
            "Assets/VehicleAssets/Beatall/all.png",
            "Assets/VehicleAssets/Delorean/all.png",
            "Assets/VehicleAssets/Porsche996/rear_wheels.mtl",
            "Assets/VehicleAssets/ToyotaAE86/front_wheels.mtl"
        };

        public static void Validate(string reportFolder)
        {
            var lines = new List<string>
            {
                "Phase 11 Unity read-only dependency and importer audit",
                "NO ASSET DELETIONS / NO GUID REWRITES",
                "AssetDatabase.GetDependencies covers serialized import links;",
                "it does not certify Resources.Load names, OBJ/MTL sidecars,",
                "Editor importer script paths, package licenses or shipped WebGL.",
                ""
            };

            var candidateSet =
                new HashSet<string>(Candidates, StringComparer.OrdinalIgnoreCase);
            var guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Candidates)
            {
                if (!File.Exists(Path.GetFullPath(path)))
                    throw new InvalidOperationException(
                        "Missing Phase 11 source asset: " + path);

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrWhiteSpace(guid) || !guids.Add(guid))
                    throw new InvalidOperationException(
                        "Missing/duplicate Phase 11 asset GUID: " + path);

                AssetImporter importer = AssetImporter.GetAtPath(path);
                lines.Add("ASSET | " + path);
                lines.Add("  GUID | " + guid);
                lines.Add("  Importer | " +
                    (importer != null
                        ? importer.GetType().Name
                        : "<no AssetImporter - check source/sidecar semantics>"));
            }

            // The two PNGs are byte-identical but have intentionally distinct
            // Unity import settings. In particular, do not replace the
            // runtime Resources copy with the author's mipmapped VFX copy.
            var runtimeImporter =
                AssetImporter.GetAtPath(ResourceCircle) as TextureImporter;
            var originalImporter =
                AssetImporter.GetAtPath(OriginalCircle) as TextureImporter;
            if (runtimeImporter == null || originalImporter == null)
                throw new InvalidOperationException(
                    "Phase 11 circle2 source imports are not TextureImporters");
            lines.Add("CIRCLE_IMPORT | runtime mipmap=" +
                runtimeImporter.mipmapEnabled +
                "; VFX source mipmap=" + originalImporter.mipmapEnabled);
            if (runtimeImporter.mipmapEnabled ||
                !originalImporter.mipmapEnabled)
                throw new InvalidOperationException(
                    "Phase 11 circle2 authored mipmap contracts changed");

            string frontendPath =
                "Assets/Scripts/UI/MotorCityFrontEndFlow.cs";
            string frontend = File.ReadAllText(frontendPath);
            if (!frontend.Contains("\"MotorCity/UI/Loading/circle2\""))
                throw new InvalidOperationException(
                    "Phase 11 runtime loading wheel resource path changed");
            lines.Add("DYNAMIC_RESOURCE | MotorCityFrontEndFlow loads " +
                "MotorCity/UI/Loading/circle2 (not a serialized reference)");

            // A single dependency query per category avoids enumerating all
            // 4,000 assets and repeatedly opening the large city prefab.
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene != null &&
                                scene.enabled &&
                                !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            var resourceRoots =
                AssetDatabase.FindAssets("", new[] { "Assets/Resources/MotorCity" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => !string.IsNullOrWhiteSpace(path) &&
                                   !AssetDatabase.IsValidFolder(path) &&
                                   !candidateSet.Contains(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            var vehiclePrefabs =
                AssetDatabase.FindAssets(
                    "t:Prefab",
                    new[] { "Assets/Resources/MotorCity/Vehicles/Player" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .ToArray();
            var vfxRoots =
                AssetDatabase.FindAssets(
                    "",
                    new[] { "Assets/Eric VFX Studio" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => !string.IsNullOrWhiteSpace(path) &&
                                   !AssetDatabase.IsValidFolder(path) &&
                                   (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                                    path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) &&
                                   !candidateSet.Contains(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            var categories = new[]
            {
                new { Name = "BUILD_SCENES", Roots = scenes },
                new { Name = "RESOURCES_OTHER_ASSETS", Roots = resourceRoots },
                new { Name = "PLAYER_VEHICLE_PREFABS", Roots = vehiclePrefabs },
                new { Name = "VFX_PREFABS_MATERIALS", Roots = vfxRoots }
            };
            foreach (var category in categories)
            {
                string[] dependencies = category.Roots.Length > 0
                    ? AssetDatabase.GetDependencies(category.Roots, true)
                    : Array.Empty<string>();
                var found = new HashSet<string>(
                    dependencies, StringComparer.OrdinalIgnoreCase);
                lines.Add("");
                lines.Add("CATEGORY | " + category.Name +
                    " | roots=" + category.Roots.Length +
                    " | transitive_dependencies=" + dependencies.Length);
                foreach (string candidate in Candidates)
                {
                    lines.Add("  " + (found.Contains(candidate)
                        ? "REACHABLE"
                        : "NOT_IN_SERIALIZED_GRAPH") +
                        " | " + candidate);
                }
            }

            // OBJ/MTL references exist outside Unity's serialized dependency
            // graph; never use an empty graph to delete these materials.
            AssertSidecar(
                lines,
                "Assets/VehicleAssets/Porsche996/rear_wheels.obj",
                "mtllib rear_wheels.mtl");
            AssertSidecar(
                lines,
                "Assets/VehicleAssets/ToyotaAE86/front_wheels.obj",
                "mtllib front_wheels.mtl");
            AssertSidecar(
                lines,
                "Assets/VehicleAssets/AmgGT/amggt.mtl",
                "map_Kd all.png");
            AssertSidecar(
                lines,
                "Assets/VehicleAssets/Beatall/beatall.mtl",
                "map_Kd all.png");
            AssertSidecar(
                lines,
                "Assets/VehicleAssets/Delorean/delorean.mtl",
                "map_Kd all.png");

            lines.Add("");
            lines.Add("VERDICT | 0 assets certified for deletion.");
            lines.Add("NEXT | Verify importer rebuild and runtime visual before" +
                " considering any GUID migration; preserve source licenses.");

            Directory.CreateDirectory(reportFolder);
            File.WriteAllLines(
                Path.Combine(reportFolder, "unity-phase11-asset-dependencies.txt"),
                lines);
            Debug.Log("Motor City Phase 11 Unity duplicate/dependency audit: " +
                Candidates.Length + " sources checked, no edits.");
        }

        private static void AssertSidecar(
            List<string> lines,
            string sourcePath,
            string expected)
        {
            if (!File.Exists(Path.GetFullPath(sourcePath)))
                throw new InvalidOperationException(
                    "Phase 11 importer source missing: " + sourcePath);

            bool found = File.ReadLines(sourcePath)
                .Take(80)
                .Any(line => line.Trim().Equals(
                    expected, StringComparison.OrdinalIgnoreCase));
            if (!found)
                throw new InvalidOperationException(
                    "Phase 11 importer sidecar changed: " +
                    sourcePath + " / " + expected);
            lines.Add("SOURCE_SIDECAR | " + sourcePath + " => " + expected);
        }
    }
}
