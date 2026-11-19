using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MotorCity.EditorTools
{
    // Size experiment only. The stripped player cannot enter the city.
    public static class MotorCityCityBundleExperiment
    {
        private const string City = "Assets/Resources/MotorCity/Environment/CityVisual.prefab";
        private const string TemporaryCity = "Assets/MotorCity_CityBundleExperiment_CityVisual.prefab";
        private const string EnvironmentRoot = "Assets/Resources/MotorCity/Environment/";
        private static readonly string[] ExtraRoots = { "FCGMaterials", "FCGTrafficCars", "TrafficVehicleCatalog.prefab" };
        private static string TemporaryRoot(string name) => "Assets/MotorCity_CityBundleExperiment_" + name;
        private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);
        private const string Profile = "Assets/Settings/Build Profiles/Web - Desktop - Release.asset";
        private const string Report = "Library/MotorCityCityBundleExperiment.txt";

        [MenuItem("Tools/Motor City/Build/Measure City AssetBundle")]
        private static void Measure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Exit Play Mode and finish any current build.");
            if (File.Exists(TemporaryCity) || !File.Exists(City) || ExtraRoots.Any(n => Exists(TemporaryRoot(n)) || !Exists(EnvironmentRoot + n)))
                throw new InvalidOperationException("City path unexpected. Use Restore City After Experiment before building.");
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile == null || AssetDatabase.GetAssetPath(profile) != Profile ||
                EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL ||
                EditorUserBuildSettings.development)
                throw new InvalidOperationException("Activate Web - Desktop - Release first.");

            if (!EditorUtility.DisplayDialog("City bundle size experiment",
                "Builds two diagnostic players and one city bundle. CityVisual, FCGMaterials, FCGTrafficCars and the traffic catalog are temporarily moved out of Resources and restored in finally. No textures are deleted. Do not publish the stripped player. Save your scenes first.",
                "Run experiment", "Cancel"))
                return;

            AssetDatabase.SaveAssets();
            string root = Path.GetFullPath("Builds/CityBundleExperiment/" +
                DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(root);
            var log = new StringBuilder();
            log.AppendLine("MOTOR CITY CITY BUNDLE SIZE EXPERIMENT — NOT FOR RELEASE");
            log.AppendLine("Output: " + root);
            string guid = AssetDatabase.AssetPathToGUID(City);
            var originalGuids = ExtraRoots.ToDictionary(n => n, n => AssetDatabase.AssetPathToGUID(EnvironmentRoot + n));
            try
            {
                string baseline = Path.Combine(root, "Baseline");
                Build(profile, baseline);
                long before = DataBytes(baseline);
                log.AppendLine("Baseline data bytes: " + before);
                log.AppendLine("Baseline wasm bytes: " + WasmBytes(baseline));

                string error = AssetDatabase.MoveAsset(City, TemporaryCity);
                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException(error);

                foreach (string name in ExtraRoots)
                {
                    error = AssetDatabase.MoveAsset(EnvironmentRoot + name, TemporaryRoot(name));
                    if (!string.IsNullOrEmpty(error))
                        throw new InvalidOperationException(error);
                    if (AssetDatabase.AssetPathToGUID(TemporaryRoot(name)) != originalGuids[name])
                        throw new InvalidOperationException("Moved asset GUID mismatch: " + name);
                }

                // Preserve every original reference. Unity still includes assets
                // referenced by the remaining Resources or scenes in the player.
                var roots = new System.Collections.Generic.List<string> { TemporaryCity };
                foreach (string name in ExtraRoots)
                {
                    string moved = TemporaryRoot(name);
                    if (AssetDatabase.IsValidFolder(moved))
                        roots.AddRange(AssetDatabase.GetAllAssetPaths().Where(path =>
                            path.StartsWith(moved + "/", StringComparison.Ordinal) &&
                            !AssetDatabase.IsValidFolder(path) &&
                            AssetDatabase.LoadMainAssetAtPath(path) != null));
                    else
                        roots.Add(moved);
                }
                string[] bundleRoots = roots.Distinct(StringComparer.Ordinal).ToArray();
                log.AppendLine("Extended bundle roots: " + bundleRoots.Length);
                string bundles = Path.Combine(root, "Bundles");
                Directory.CreateDirectory(bundles);
                var map = new[] { new AssetBundleBuild {
                    assetBundleName = "motorcity-city.bundle",
                    assetNames = bundleRoots,
                    addressableNames = bundleRoots.Select(path => path == TemporaryCity ? "CityVisual" : path).ToArray()
                }};
                var manifest = BuildPipeline.BuildAssetBundles(bundles, map,
                    BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                    BuildTarget.WebGL);
                if (manifest == null)
                    throw new InvalidOperationException("City AssetBundle build failed.");
                long bundle = new FileInfo(Path.Combine(bundles, "motorcity-city.bundle")).Length;
                log.AppendLine("City bundle LZ4 bytes: " + bundle);

                string stripped = Path.Combine(root, "NOT-FOR-RELEASE-NoCity");
                BuildReport result = Build(profile, stripped);
                File.WriteAllText(Path.Combine(stripped, "NOT-FOR-RELEASE.txt"),
                    "Diagnostic only: CityVisual is absent and the runtime loader is unchanged.");
                long after = DataBytes(stripped);
                log.AppendLine("Without city data bytes: " + after);
                log.AppendLine("Without city wasm bytes: " + WasmBytes(stripped));
                log.AppendLine("Startup data savings bytes: " + (before - after));
                log.AppendLine("Startup data savings percent: " +
                    (100.0 * (before - after) / before).ToString("F2"));
                log.AppendLine("No-city data plus separate bundle bytes: " + (after + bundle));

                var cityDependencies = new System.Collections.Generic.HashSet<string>(
                    AssetDatabase.GetDependencies(bundleRoots, true), StringComparer.Ordinal);
                var retained = result.packedAssets.SelectMany(p => p.contents)
                    .Where(c => cityDependencies.Contains(c.sourceAssetPath))
                    .GroupBy(c => c.sourceAssetPath)
                    .Select(g => new { Path = g.Key, Bytes = g.Aggregate(0UL, (s, c) => s + c.packedSize) })
                    .OrderByDescending(x => x.Bytes).ToArray();
                log.AppendLine("City dependency paths still packed in player: " + retained.Length);
                foreach (var item in retained)
                    log.AppendLine(item.Bytes + "\t" + item.Path + "\tGUID=" + AssetDatabase.AssetPathToGUID(item.Path));
            }
            catch (Exception ex)
            {
                log.AppendLine("FAILED: " + ex);
                throw;
            }
            finally
            {
                try
                {
                    Restore();
                    if (AssetDatabase.AssetPathToGUID(City) != guid)
                        throw new InvalidOperationException("Restored city GUID mismatch.");
                    foreach (string name in ExtraRoots)
                        if (AssetDatabase.AssetPathToGUID(EnvironmentRoot + name) != originalGuids[name])
                            throw new InvalidOperationException("Restored GUID mismatch: " + name);
                    log.AppendLine("City and extra roots restored; original root GUIDs verified.");
                    log.AppendLine("City restored: " + City);
                }
                finally
                {
                    File.WriteAllText(Report, log.ToString(), Encoding.UTF8);
                    Debug.Log("City bundle experiment report: " + Report);
                }
            }
        }

        [MenuItem("Tools/Motor City/Build/Restore City After Experiment")]
        private static void Restore()
        {
            foreach (string name in ExtraRoots.Reverse())
            {
                string moved = TemporaryRoot(name);
                string original = EnvironmentRoot + name;
                if (!Exists(moved)) continue;
                if (Exists(original))
                    throw new InvalidOperationException("Both paths exist; do not overwrite: " + name);
                string moveError = AssetDatabase.MoveAsset(moved, original);
                if (!string.IsNullOrEmpty(moveError))
                    throw new InvalidOperationException("RESTORE FAILED: " + moveError);
            }
            if (File.Exists(TemporaryCity))
            {
                if (File.Exists(City))
                    throw new InvalidOperationException("Both city paths exist; do not overwrite either.");
                string error = AssetDatabase.MoveAsset(TemporaryCity, City);
                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException("RESTORE FAILED: " + error);
            }
            if (!File.Exists(City))
                throw new InvalidOperationException("CityVisual not found; restore it before regular builds.");
        }

        private static BuildReport Build(BuildProfile profile, string directory)
        {
            Directory.CreateDirectory(directory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions {
                buildProfile = profile,
                locationPathName = directory,
                options = BuildOptions.None
            });
            if (report == null || report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Diagnostic WebGL build failed: " + directory);
            return report;
        }

        private static long DataBytes(string directory) => PayloadBytes(directory, ".data.br");
        private static long WasmBytes(string directory) => PayloadBytes(directory, ".wasm.br");
        private static long PayloadBytes(string directory, string suffix)
        {
            var files = Directory.GetFiles(Path.Combine(directory, "Build"), "*" + suffix);
            if (files.Length != 1)
                throw new InvalidOperationException("Expected exactly one " + suffix + " file; check Brotli settings.");
            return new FileInfo(files[0]).Length;
        }
    }
}
