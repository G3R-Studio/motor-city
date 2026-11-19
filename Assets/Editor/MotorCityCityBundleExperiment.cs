using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MotorCity.World;
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
        private static readonly string[] LocalPilotRoots =
            { "Assets/Resources/MotorCityLodPilot", "Assets/Resources/MotorCityLodExpanded" };
        private static string ParkedPilotRoot(string path) =>
            "Assets/MotorCity_BuildExcluded_" + Path.GetFileName(path);

        private static void RestorePilotRoots()
        {
            foreach (var path in LocalPilotRoots)
            {
                string parked = ParkedPilotRoot(path);
                if (!Exists(parked)) continue;
                if (Exists(path)) throw new InvalidOperationException("Both LOD pilot paths exist: " + path);
                string error = AssetDatabase.MoveAsset(parked, path);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
        }

        private static void ParkPilotRoots()
        {
            foreach (var path in LocalPilotRoots)
            {
                if (Exists(ParkedPilotRoot(path)))
                    throw new InvalidOperationException("Restore interrupted build first.");
                if (!Exists(path)) continue;
                if (!AssetDatabase.IsValidFolder(path))
                    throw new InvalidOperationException("Unexpected LOD pilot asset: " + path);
                // Only generated standalone mesh assets; no deletion or GUID change.
                var files = AssetDatabase.GetAllAssetPaths().Where(p =>
                    p.StartsWith(path + "/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p));
                if (files.Any(p => !p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                    !(AssetDatabase.LoadMainAssetAtPath(p) is Mesh)))
                    throw new InvalidOperationException("Unexpected non-pilot asset in " + path);
                string error = AssetDatabase.MoveAsset(path, ParkedPilotRoot(path));
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
        }

        [MenuItem("Tools/Motor City/Build/Measure City AssetBundle")]
        private static void Measure() => Run(false);

        private const string MarkerPath = "Assets/Resources/MotorCityCityBundleVersion.txt";
        [MenuItem("Tools/Motor City/Build/Build Split City Test")]
        private static void BuildSplit() => Run(true);

        private const string DistrictRoot = "Assets/MotorCity_DistrictBuildTemp";
        private const string DistrictCity = DistrictRoot + "/CityVisual.prefab";
        private const string DistrictCatalogPath = DistrictRoot + "/DistrictCatalog.json";
        [MenuItem("Tools/Motor City/Build/Build District Streaming Test")]
        private static void BuildDistricts() => Run(true, true);

        private static MotorCityCityBundle.DistrictCatalog PrepareDistricts(string directory)
        {
            AssetDatabase.CreateFolder("Assets", "MotorCity_DistrictBuildTemp");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(TemporaryCity);
            var clone = (GameObject)PrefabUtility.InstantiatePrefab(source);
            int rendererCount = 0;
            var districts = new List<MotorCityCityBundle.District>();
            var builds = new List<AssetBundleBuild>();
            try
            {
                PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var renderers = clone.GetComponentsInChildren<MeshRenderer>(true);
                rendererCount = renderers.Length;
                var groups = new Dictionary<Transform, List<int>>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    var f = r.GetComponent<MeshFilter>();
                    if (f == null || f.sharedMesh == null || !r.enabled ||
                        !r.gameObject.activeInHierarchy || r.isPartOfStaticBatch ||
                        r.GetComponentInParent<LODGroup>() != null ||
                        r.GetComponentInParent<Rigidbody>() != null ||
                        r.GetComponentInParent<Animator>() != null ||
                        r.GetComponent<MonoBehaviour>() != null ||
                        r.bounds.extents.magnitude <= 7f) continue;
                    if (r.sharedMaterials.Length == 0 || r.sharedMaterials.Any(m =>
                        m == null || m.name != "Atlas-1_59aa769b" || m.renderQueue != 2000)) continue;
                    Transform district = r.transform;
                    while (district != clone.transform && !district.name.StartsWith("Double-Block-", StringComparison.Ordinal))
                        district = district.parent;
                    if (district == null || district == clone.transform) continue;
                    // Do not touch scripted descendants of a district.
                    bool scripted = false;
                    for (Transform t = r.transform; t != district; t = t.parent)
                        if (t.GetComponent<MonoBehaviour>() != null) { scripted = true; break; }
                    if (scripted) continue;
                    if (!groups.TryGetValue(district, out var indices))
                        groups.Add(district, indices = new List<int>());
                    indices.Add(i);
                }
                // Keep meshes used by physics or other districts in the base city.
                // Otherwise per-district copies can increase total memory.
                var owners = new Dictionary<Mesh, Transform>();
                var shared = new HashSet<Mesh>();
                var candidates = new Dictionary<MeshFilter, Transform>();
                foreach (var pair in groups)
                    foreach (int index in pair.Value)
                        candidates[renderers[index].GetComponent<MeshFilter>()] = pair.Key;
                foreach (var f in clone.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = f.sharedMesh;
                    if (mesh == null) continue;
                    if (!candidates.TryGetValue(f, out var owner))
                    {
                        shared.Add(mesh);
                        continue;
                    }
                    if (owners.TryGetValue(mesh, out var previous) && previous != owner)
                        shared.Add(mesh);
                    else owners[mesh] = owner;
                }
                foreach (var c in clone.GetComponentsInChildren<MeshCollider>(true))
                    if (c.sharedMesh != null) shared.Add(c.sharedMesh);
                foreach (var r in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (r.sharedMesh != null) shared.Add(r.sharedMesh);

                foreach (var pair in groups)
                {
                    pair.Value.RemoveAll(i => shared.Contains(renderers[i].GetComponent<MeshFilter>().sharedMesh));
                    if (pair.Value.Count == 0) continue;
                    var data = new MotorCityCityBundle.District
                    {
                        file = "motorcity-district-" + districts.Count + ".bundle",
                        rendererIndices = pair.Value.ToArray(),
                        meshNames = new string[pair.Value.Count],
                        localBounds = new Bounds[pair.Value.Count]
                    };
                    var copies = new Dictionary<Mesh, string>();
                    var paths = new List<string>();
                    for (int j = 0; j < pair.Value.Count; j++)
                    {
                        var r = renderers[pair.Value[j]];
                        var filter = r.GetComponent<MeshFilter>();
                        var mesh = filter.sharedMesh;
                        if (j == 0) data.bounds = r.bounds; else data.bounds.Encapsulate(r.bounds);
                        data.localBounds[j] = r.localBounds;
                        if (!copies.TryGetValue(mesh, out string name))
                        {
                            name = "district_" + districts.Count + "_mesh_" + copies.Count;
                            var copy = UnityEngine.Object.Instantiate(mesh);
                            copy.name = name;
                            string path = DistrictRoot + "/" + name + ".asset";
                            AssetDatabase.CreateAsset(copy, path);
                            copies.Add(mesh, name);
                            paths.Add(path);
                        }
                        data.meshNames[j] = name;
                        filter.sharedMesh = null;
                    }
                    districts.Add(data);
                    builds.Add(new AssetBundleBuild { assetBundleName = data.file, assetNames = paths.ToArray() });
                }
                if (districts.Count == 0)
                    throw new InvalidOperationException("No eligible Double-Block district meshes found; original split build remains available.");
                if (PrefabUtility.SaveAsPrefabAsset(clone, DistrictCity) == null)
                    throw new InvalidOperationException("Could not save district city skeleton.");
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(directory);
            var manifest = BuildPipeline.BuildAssetBundles(directory, builds.ToArray(),
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.WebGL);
            if (manifest == null) throw new InvalidOperationException("District bundles failed.");
            foreach (var district in districts)
            {
                district.hash = manifest.GetAssetBundleHash(district.file).ToString();
                string versioned = Path.GetFileNameWithoutExtension(district.file) + "-" + district.hash + ".bundle";
                File.Move(Path.Combine(directory, district.file), Path.Combine(directory, versioned));
                district.file = versioned;
            }
            var catalog = new MotorCityCityBundle.DistrictCatalog { districts = districts.ToArray(), rendererCount = rendererCount };
            File.WriteAllText(DistrictCatalogPath, JsonUtility.ToJson(catalog));
            AssetDatabase.ImportAsset(DistrictCatalogPath, ImportAssetOptions.ForceSynchronousImport);
            return catalog;
        }

        private static string Address(string path)
        {
            if (path == DistrictCatalogPath) return "MotorCity/DistrictCatalog";
            if (path == TemporaryCity || path == DistrictCity) return "MotorCity/Environment/CityVisual";
            foreach (string name in ExtraRoots)
            {
                string root = TemporaryRoot(name);
                if (path == root || path.StartsWith(root + "/", StringComparison.Ordinal))
                    return Path.ChangeExtension("MotorCity/Environment/" + name + path.Substring(root.Length), null);
            }
            throw new InvalidOperationException("Unrecognized bundle root: " + path);
        }

        private static void Run(bool split, bool districts = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Exit Play Mode and finish any current build.");
            if (LocalPilotRoots.Any(p => Exists(ParkedPilotRoot(p))) || AssetDatabase.IsValidFolder(DistrictRoot) || File.Exists(MarkerPath) || File.Exists(TemporaryCity) || !File.Exists(City) || ExtraRoots.Any(n => Exists(TemporaryRoot(n)) || !Exists(EnvironmentRoot + n)))
                throw new InvalidOperationException("City path unexpected. Use Restore City After Experiment before building.");
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile == null || AssetDatabase.GetAssetPath(profile) != Profile ||
                EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL ||
                EditorUserBuildSettings.development)
                throw new InvalidOperationException("Activate Web - Desktop - Release first.");

            if (!EditorUtility.DisplayDialog("City bundle size experiment",
                split ? "Builds a playable split-city test. All original assets will be restored. Not for publication until tested." : "Builds two diagnostic players and one city bundle. CityVisual, FCGMaterials, FCGTrafficCars and the traffic catalog are temporarily moved out of Resources and restored in finally. No textures are deleted. Do not publish the stripped player. Save your scenes first.",
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
                ParkPilotRoots();
                string baseline = Path.Combine(root, "Baseline");
                long before = 0;
                if (!split)
                {
                    Build(profile, baseline);
                    before = DataBytes(baseline);
                    log.AppendLine("Baseline data bytes: " + before);
                    log.AppendLine("Baseline wasm bytes: " + WasmBytes(baseline));
                }

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
                string districtDirectory = Path.Combine(root, "DistrictBundles");
                var catalog = districts ? PrepareDistricts(districtDirectory) : null;
                var roots = new System.Collections.Generic.List<string> { districts ? DistrictCity : TemporaryCity };
                if (districts) roots.Add(DistrictCatalogPath);
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
                    addressableNames = bundleRoots.Select(Address).ToArray()
                }};
                var manifest = BuildPipeline.BuildAssetBundles(bundles, map,
                    BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                    BuildTarget.WebGL);
                if (manifest == null)
                    throw new InvalidOperationException("City AssetBundle build failed.");
                long bundle = new FileInfo(Path.Combine(bundles, "motorcity-city.bundle")).Length;
                log.AppendLine("City bundle LZ4 bytes: " + bundle);

                string stripped = Path.Combine(root, split ? "SplitCityTest" : "NOT-FOR-RELEASE-NoCity");
                if (split)
                {
                    string hash = manifest.GetAssetBundleHash("motorcity-city.bundle").ToString();
                    File.WriteAllText(MarkerPath, hash);
                    AssetDatabase.ImportAsset(MarkerPath, ImportAssetOptions.ForceSynchronousImport);
                    Build(profile, stripped, Path.Combine(bundles, "Bundles.manifest"));
                    string streaming = Path.Combine(stripped, "StreamingAssets");
                    Directory.CreateDirectory(streaming);
                    File.Copy(Path.Combine(bundles, "motorcity-city.bundle"),
                        Path.Combine(streaming, "motorcity-city-" + hash + ".bundle"));
                    if (catalog != null)
                    {
                        foreach (var district in catalog.districts)
                            File.Copy(Path.Combine(districtDirectory, district.file), Path.Combine(streaming, district.file));
                        log.AppendLine("District bundles: " + catalog.districts.Length);
                        log.AppendLine("District streamed renderers: " + catalog.districts.Sum(d => d.rendererIndices.Length));
                    }
                    log.AppendLine("Split test output: " + stripped);
                    log.AppendLine("Split data bytes: " + DataBytes(stripped));
                    log.AppendLine("Split wasm bytes: " + WasmBytes(stripped));
                    log.AppendLine("Bundle bytes: " + bundle);
                    return;
                }
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
                    if (File.Exists(MarkerPath)) AssetDatabase.DeleteAsset(MarkerPath);
                    if (AssetDatabase.IsValidFolder(DistrictRoot)) AssetDatabase.DeleteAsset(DistrictRoot);
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
                    File.WriteAllText(split ? "Library/MotorCitySplitCityTest.txt" : Report, log.ToString(), Encoding.UTF8);
                    Debug.Log("City bundle experiment report: " + Report);
                }
            }
        }

        [MenuItem("Tools/Motor City/Build/Restore City After Experiment")]
        private static void Restore()
        {
            RestorePilotRoots();
            if (AssetDatabase.IsValidFolder(DistrictRoot)) AssetDatabase.DeleteAsset(DistrictRoot);
            if (File.Exists(MarkerPath)) AssetDatabase.DeleteAsset(MarkerPath);
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

        private static BuildReport Build(BuildProfile profile, string directory, string manifestPath = null)
        {
            Directory.CreateDirectory(directory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions {
                buildProfile = profile,
                locationPathName = directory,
                assetBundleManifestPath = manifestPath,
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
