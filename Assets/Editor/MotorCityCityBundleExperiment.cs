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
    // Standard WebGL split build and optional size diagnostics.
    public static class MotorCityCityBundleExperiment
    {
        private const string City = "Assets/Resources/MotorCity/Environment/CityVisual.prefab";
        private const string TemporaryCity = "Assets/MotorCity_CityBundleExperiment_CityVisual.prefab";
        private const string EnvironmentRoot = "Assets/Resources/MotorCity/Environment/";
        private static readonly string[] ExtraRoots = { "FCGMaterials", "FCGTrafficCars", "TrafficVehicleCatalog.prefab" };
        private static string TemporaryRoot(string name) => "Assets/MotorCity_CityBundleExperiment_" + name;
        private static readonly string[] GameplayPaths = {
            "Vehicles/Player/Beatall",
            "Vehicles/Player/Peugeot306",
            "Vehicles/Player/ToyotaAE86",
            "Vehicles/Player/Hybrid",
            "Vehicles/Player/Porsche996",
            "Vehicles/Player/AmgGT",
            "Vehicles/Player/Camaro",
            "Vehicles/Player/Delorean",
            "Vehicles/Player/Bus",
            "Pixie/AmaneKisoraVisual",
            "Byte/HaonByteVisual",
            "PlayerCarVisual",
            "Markers/CarWashMarkerVfx",
            "Markers/TowMarkerVfx",
            "Markers/ProfessionMarkerVfx",
            "Markers/DiscoveryMarkerVfx",
            "Markers/DeliveryMarkerVfx",
            "Markers/SprintMarkerVfx",
            "Markers/UndergroundMarkerVfx",
            "Markers/CircuitMarkerVfx",
            "Markers/DriftMarkerVfx",
            "UI/Characters/avatar_vitya",
            "UI/Characters/avatar_turbo",
            "UI/Characters/avatar_nika",
            "UI/Characters/avatar_bublik",
            "VehiclePaints/StarterPaint_0",
            "VehiclePaints/StarterPaint_1",
            "VehiclePaints/StarterPaint_2",
            "VehiclePaints/StarterPaint_3",
            "VehiclePaints/StarterPaint_4",
            "Audio/CarEngine",
            "Audio/TireSkid"
        };
        private static string GameplayExtension(string name) =>
            name == "Audio/CarEngine" ? ".wav" :
            name == "Audio/TireSkid" ? ".ogg" :
            name.StartsWith("UI/Characters/", StringComparison.Ordinal) ? ".png" :
            name.StartsWith("VehiclePaints/", StringComparison.Ordinal) ? ".mat" : ".prefab";
        private static string GameplayOriginal(string name) => "Assets/Resources/MotorCity/" + name + GameplayExtension(name);
        private static string GameplayParked(string name) => "Assets/MotorCity_GameplayStream_" + name.Replace('/', '_') + GameplayExtension(name);

        private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);
        private const string Profile = "Assets/Settings/Build Profiles/Web - Desktop - Release.asset";
        private const string Report = "Library/MotorCityCityBundleExperiment.txt";
        private static readonly string[] LocalPilotRoots =
            { "Assets/Resources/MotorCityLodPilot", "Assets/Resources/MotorCityLodExpanded" };
        private static string ParkedPilotRoot(string path) =>
            "Assets/MotorCity_BuildExcluded_" + Path.GetFileName(path);

        private const string IntroRoot = "Assets/Resources/MotorCity/Intro/";
        private const string ParkedIntro = "Assets/MotorCity_BuildExcluded_Intro";
        private static bool IsIntroSlide(string path) =>
            Enumerable.Range(1, 6).Any(i => Path.GetFileNameWithoutExtension(path) == "Intro_" + i.ToString("00")) &&
            AssetDatabase.LoadMainAssetAtPath(path) is Texture2D;

        private static void MoveIntro(string from, string to)
        {
            if (Exists(to)) throw new InvalidOperationException("Conflicting intro path: " + to);
            string guid = AssetDatabase.AssetPathToGUID(from);
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error) || AssetDatabase.AssetPathToGUID(to) != guid)
                throw new InvalidOperationException("Intro move failed: " + from + " " + error);
        }

        private static void RestoreIntroSlides()
        {
            if (!Exists(ParkedIntro)) return;
            if (!AssetDatabase.IsValidFolder(ParkedIntro))
                throw new InvalidOperationException("Unexpected intro recovery path.");
            foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p =>
                p.StartsWith(ParkedIntro + "/", StringComparison.Ordinal)).ToArray())
            {
                if (!IsIntroSlide(path) || path.Substring(ParkedIntro.Length + 1).Contains("/"))
                    throw new InvalidOperationException("Unexpected parked intro asset: " + path);
                MoveIntro(path, IntroRoot + Path.GetFileName(path));
            }
            if (Directory.EnumerateFileSystemEntries(ParkedIntro).Any())
                throw new InvalidOperationException("Unexpected parked intro files.");
            AssetDatabase.DeleteAsset(ParkedIntro);
        }

        // WebGL already skips the lore slides. MainMenu is deliberately retained.
        private static void ParkIntroSlides(StringBuilder log)
        {
            var paths = AssetDatabase.GetAllAssetPaths().Where(p =>
                p.StartsWith(IntroRoot, StringComparison.Ordinal) &&
                !p.Substring(IntroRoot.Length).Contains("/") && IsIntroSlide(p)).ToArray();
            if (paths.Length == 0) return;
            if (Exists(ParkedIntro)) throw new InvalidOperationException("Restore intro first.");
            AssetDatabase.CreateFolder("Assets", "MotorCity_BuildExcluded_Intro");
            foreach (string path in paths)
            {
                MoveIntro(path, ParkedIntro + "/" + Path.GetFileName(path));
                log.AppendLine("Excluded WebGL intro slide: " + path);
            }
        }

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
        [MenuItem("Tools/Motor City/Build/Build Split Release Candidate (Fallback)")]
        private static void BuildSplit() => Run(true);

        private const string DistrictRoot = "Assets/MotorCity_DistrictBuildTemp";
        private const string DistrictCity = DistrictRoot + "/CityVisual.prefab";
        private const string DistrictCatalogPath = DistrictRoot + "/DistrictCatalog.json";
        private const string HlodPath = DistrictRoot + "/CityHlodData.asset";
        [MenuItem("Tools/Motor City/Build/Build HLOD Split City Candidate")]
        private static void BuildHlod() => Run(true, false, true);
        [MenuItem("Tools/Motor City/Build/Build District Streaming Test")]
        [MenuItem("Tools/Motor City/Build/Build District Release Candidate")]
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

        // Extract a real reduced triangle index buffer, not a native LOD
        // whose selection depends on renderer/platform support.
        private static Mesh SimplifyProxy(Mesh combined)
        {
            Mesh generated = UnityEngine.Object.Instantiate(combined);
            Mesh reduced = null;
            try
            {
                MeshLodUtility.GenerateMeshLods(generated, 2);
                int original = (int)combined.GetIndexCount(0);
                var ranges = generated.GetLods(0);
                for (int level = 1; level < ranges.Length; level++)
                {
                    int count = (int)generated.GetIndexCount(0, level);
                    if (count < original * .45f || count > original * .85f || count % 3 != 0) continue;
                    int[] indices = generated.GetIndices(0, level, true);
                    if (indices.Length != count || indices.Any(i => i < 0 || i >= generated.vertexCount)) continue;
                    reduced = new Mesh { name = combined.name, indexFormat = generated.indexFormat };
                    reduced.vertices = generated.vertices;
                    var normals = generated.normals;
                    if (normals.Length == generated.vertexCount) reduced.normals = normals;
                    var tangents = generated.tangents;
                    if (tangents.Length == generated.vertexCount) reduced.tangents = tangents;
                    var colors = generated.colors;
                    if (colors.Length == generated.vertexCount) reduced.colors = colors;
                    var uv = new List<Vector4>();
                    for (int channel = 0; channel < 8; channel++)
                    {
                        generated.GetUVs(channel, uv);
                        if (uv.Count == generated.vertexCount) reduced.SetUVs(channel, uv);
                    }
                    reduced.SetIndices(indices, MeshTopology.Triangles, 0);
                    reduced.bounds = combined.bounds;
                    UnityEngine.Object.DestroyImmediate(combined);
                    return reduced;
                }
            }
            catch (Exception ex)
            {
                if (reduced != null) UnityEngine.Object.DestroyImmediate(reduced);
                Debug.LogWarning("HLOD reduction skipped: " + ex.Message);
            }
            finally { UnityEngine.Object.DestroyImmediate(generated); }
            return combined;
        }

        // In a separate candidate, merge distant geometry, not bounds boxes.
        // Only one opaque material, immutable transforms and no lightmap/LOD owners.
        // The authored prefab and all collider meshes remain intact.
        private static bool PrepareHlod()
        {
            AssetDatabase.CreateFolder("Assets", "MotorCity_DistrictBuildTemp");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(TemporaryCity);
            var clone = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                var renderers = clone.GetComponentsInChildren<MeshRenderer>(true);
                var groups = new Dictionary<Vector2Int, List<int>>();
                Material atlas = null;
                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    var f = r.GetComponent<MeshFilter>();
                    var mesh = f != null ? f.sharedMesh : null;
                    if (mesh == null || !mesh.isReadable || mesh.subMeshCount != 1 ||
                        mesh.GetTopology(0) != MeshTopology.Triangles || mesh.vertexCount > 60000 ||
                        !r.enabled || !r.gameObject.activeInHierarchy || r.forceRenderingOff ||
                        r.HasPropertyBlock() || r.lightmapIndex >= 0 && r.lightmapIndex < 65534 ||
                        r.bounds.extents.magnitude <= 7f || r.bounds.extents.magnitude > 32f ||
                        r.GetComponentInParent<Rigidbody>() != null || r.GetComponentInParent<Animator>() != null ||
                        r.GetComponentInParent<LODGroup>() != null || r.GetComponent<MonoBehaviour>() != null ||
                        r.localToWorldMatrix.determinant <= 0f) continue;
                    var mats = r.sharedMaterials;
                    if (mats.Length != 1 || mats[0] == null ||
                        mats[0].name != "Atlas-1_59aa769b" || mats[0].renderQueue != 2000) continue;
                    if (atlas == null) atlas = mats[0];
                    if (mats[0] != atlas) continue;
                    bool protectedPath = false;
                    for (var t = r.transform; t != clone.transform && t != null; t = t.parent)
                    {
                        string n = t.name.ToLowerInvariant();
                        if (n.Contains("park-") || n.Contains("window") || n.Contains("light") ||
                            n.Contains("traffic") || n.Contains("tree") || t.GetComponent<MonoBehaviour>() != null)
                        { protectedPath = true; break; }
                    }
                    if (protectedPath) continue;
                    var c = r.bounds.center;
                    var key = new Vector2Int(Mathf.FloorToInt(c.x / 128f), Mathf.FloorToInt(c.z / 128f));
                    if (!groups.TryGetValue(key, out var group)) groups.Add(key, group = new List<int>());
                    group.Add(i);
                }
                var cells = new List<CityHlodCell>();
                foreach (var group in groups.Values)
                {
                    // Cap both peak bake memory and individual proxy index size.
                    var batch = new List<int>();
                    int vertices = 0;
                    void Flush()
                    {
                        if (batch.Count < 3) { batch.Clear(); vertices = 0; return; }
                        var combine = new CombineInstance[batch.Count];
                        var bounds = renderers[batch[0]].bounds;
                        int triangles = 0;
                        for (int j = 0; j < batch.Count; j++)
                        {
                            var r = renderers[batch[j]];
                            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                            combine[j] = new CombineInstance { mesh = mesh, subMeshIndex = 0, transform = r.localToWorldMatrix };
                            bounds.Encapsulate(r.bounds);
                            triangles += (int)mesh.GetIndexCount(0) / 3;
                        }
                        var proxy = new Mesh { name = "HLOD_" + cells.Count };
                        proxy.CombineMeshes(combine, true, true);
                        proxy = SimplifyProxy(proxy);
                        AssetDatabase.CreateAsset(proxy, DistrictRoot + "/" + proxy.name + ".asset");
                        cells.Add(new CityHlodCell
                        {
                            Center = bounds.center, ProxyMesh = proxy, ProxyMaterials = new[] { atlas },
                            SourceRendererIndices = batch.ToArray(),
                            SourceRendererNames = batch.Select(i => renderers[i].name).ToArray(),
                            SourceRendererCenters = batch.Select(i => renderers[i].bounds.center).ToArray(),
                            SourceTriangles = triangles, ProxyTriangles = (int)proxy.GetIndexCount(0) / 3
                        });
                        batch.Clear();
                        vertices = 0;
                    }
                    foreach (int index in group)
                    {
                        int count = renderers[index].GetComponent<MeshFilter>().sharedMesh.vertexCount;
                        if (vertices + count > 60000) Flush();
                        batch.Add(index);
                        vertices += count;
                    }
                    Flush();
                }
                if (cells.Count == 0) return false;
                var data = ScriptableObject.CreateInstance<CityHlodData>();
                data.SourceRendererCount = renderers.Length;
                data.Cells = cells.ToArray();
                data.SourceTriangles = cells.Sum(c => c.SourceTriangles);
                data.ProxyTriangles = cells.Sum(c => c.ProxyTriangles);
                AssetDatabase.CreateAsset(data, HlodPath);
                AssetDatabase.SaveAssets();
                return true;
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }

        private static string Address(string path)
        {
            if (path == HlodPath) return "MotorCity/Environment/HLOD/CityHlodData";
            if (path == DistrictCatalogPath) return "MotorCity/DistrictCatalog";
            if (path == TemporaryCity || path == DistrictCity) return "MotorCity/Environment/CityVisual";
            foreach (string name in GameplayPaths)
                if (path == GameplayParked(name)) return "MotorCity/" + name;
            foreach (string name in ExtraRoots)
            {
                string root = TemporaryRoot(name);
                if (path == root || path.StartsWith(root + "/", StringComparison.Ordinal))
                    return Path.ChangeExtension("MotorCity/Environment/" + name + path.Substring(root.Length), null);
            }
            throw new InvalidOperationException("Unrecognized bundle root: " + path);
        }

        [InitializeOnLoadMethod]
        private static void RegisterPlayerBuild()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildFromWindow);
        }

        private static void BuildFromWindow(BuildPlayerOptions options)
        {
            if (options.target != BuildTarget.WebGL)
            {
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
                return;
            }
            if ((options.options & BuildOptions.AutoRunPlayer) != 0)
                throw new InvalidOperationException("Use Build for the complete Playgama package, then serve the output over HTTP.");
            if (PlayerSettings.WebGL.template != "APPLICATION:Default")
                throw new InvalidOperationException("Motor City: select Default WebGL Template in the active release profile. Playgama's analysis window must not replace the Motor City integration.");
            MotorCityReleaseAudio.Prepare();
            Run(true, true, false, options.locationPathName);
        }

        private static void Run(bool split, bool districts = false, bool hlod = false, string output = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Exit Play Mode and finish any current build.");
            // Recover interrupted builds automatically, but never overwrite an
            // original asset or delete a temporary folder with ambiguous ownership.
            var pairs = ExtraRoots.Select(n => (original: EnvironmentRoot + n, moved: TemporaryRoot(n)))
                .Concat(GameplayPaths.Select(n => (original: GameplayOriginal(n), moved: GameplayParked(n))))
                .Concat(LocalPilotRoots.Select(p => (original: p, moved: ParkedPilotRoot(p))))
                .Append((original: City, moved: TemporaryCity)).ToArray();
            var conflicts = pairs.Where(p => Exists(p.original) && Exists(p.moved)).ToArray();
            if (conflicts.Length != 0)
                throw new InvalidOperationException("Conflicting asset paths; neither copy was changed: " +
                    string.Join("; ", conflicts.Select(p => p.original + " / " + p.moved)));
            var missing = pairs.Where(p => !LocalPilotRoots.Contains(p.original) &&
                !Exists(p.original) && !Exists(p.moved)).Select(p => p.original).ToArray();
            if (missing.Length != 0)
                throw new InvalidOperationException("Missing original assets: " + string.Join("; ", missing));
            if (pairs.Any(p => Exists(p.moved)) || Exists(DistrictRoot) || Exists(MarkerPath) || Exists(ParkedIntro))
            {
                Debug.Log("Motor City: recovering temporary assets from interrupted build.");
                Restore();
                AssetDatabase.Refresh();
            }
            var unresolved = pairs.Where(p => Exists(p.moved) ||
                (!LocalPilotRoots.Contains(p.original) && !Exists(p.original))).Select(p => p.original).ToArray();
            if (unresolved.Length != 0 || Exists(DistrictRoot) || Exists(MarkerPath))
                throw new InvalidOperationException("Build recovery incomplete: " +
                    string.Join("; ", unresolved) + "; temporary district folder=" + Exists(DistrictRoot) +
                    "; marker=" + Exists(MarkerPath));
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile == null || AssetDatabase.GetAssetPath(profile) != Profile ||
                EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL ||
                EditorUserBuildSettings.development)
                throw new InvalidOperationException("Activate Web - Desktop - Release first.");

            if (output == null && !EditorUtility.DisplayDialog("City bundle size experiment",
                split ? "Builds a playable split-city test. All original assets will be restored. Not for publication until tested." : "Builds two diagnostic players and one city bundle. CityVisual, FCGMaterials, FCGTrafficCars and the traffic catalog are temporarily moved out of Resources and restored in finally. No textures are deleted. Do not publish the stripped player. Save your scenes first.",
                "Run experiment", "Cancel"))
                return;

            if (output != null)
            {
                if (string.IsNullOrWhiteSpace(output)) return;
                output = Path.GetFullPath(output);
                if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
                    throw new InvalidOperationException("Choose an empty output folder for the complete Playgama build.");
            }
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

                if (split)
                {
                    ParkIntroSlides(log);
                    foreach (string name in GameplayPaths)
                    {
                        MoveIntro(GameplayOriginal(name), GameplayParked(name));
                        log.AppendLine("Streaming gameplay prefab: " + name);
                    }
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
                if (hlod)
                {
                    if (!PrepareHlod()) throw new InvalidOperationException("No safe HLOD groups found.");
                    roots.Add(HlodPath);
                }
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
                var map = new List<AssetBundleBuild> { new AssetBundleBuild {
                    assetBundleName = "motorcity-city.bundle",
                    assetNames = bundleRoots,
                    addressableNames = bundleRoots.Select(Address).ToArray()
                }};
                if (split)
                {
                    string[] fleetRoots = GameplayPaths.Where(n => n == "PlayerCarVisual" ||
                        (n.StartsWith("Vehicles/Player/", StringComparison.Ordinal) && n != "Vehicles/Player/Beatall"))
                        .Select(GameplayParked).ToArray();
                    string[] supporterRoots = { GameplayParked("Pixie/AmaneKisoraVisual") };
                    string[] gameplayRoots = GameplayPaths.Select(GameplayParked).Except(fleetRoots).Except(supporterRoots).ToArray();
                    map.Add(new AssetBundleBuild {
                        assetBundleName = "motorcity-supporter.bundle",
                        assetNames = supporterRoots,
                        addressableNames = supporterRoots.Select(Address).ToArray()
                    });
                    map.Add(new AssetBundleBuild {
                        assetBundleName = "motorcity-fleet.bundle",
                        assetNames = fleetRoots,
                        addressableNames = fleetRoots.Select(Address).ToArray()
                    });
                    map.Add(new AssetBundleBuild {
                        assetBundleName = "motorcity-gameplay.bundle",
                        assetNames = gameplayRoots,
                        addressableNames = gameplayRoots.Select(Address).ToArray()
                    });
                    // Assign shared imported assets explicitly once within this build.
                    // Keep scripts and package/builtin resources under Unity's normal handling.
                    var common = new[] { bundleRoots, gameplayRoots, fleetRoots, supporterRoots }
                        .SelectMany(rootsForPackage => AssetDatabase.GetDependencies(rootsForPackage, true).Distinct(StringComparer.Ordinal))
                        .GroupBy(p => p, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key)
                        .Except(bundleRoots.Concat(gameplayRoots).Concat(fleetRoots).Concat(supporterRoots), StringComparer.Ordinal)
                        .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
                            !AssetDatabase.IsValidFolder(p) &&
                            (AssetDatabase.LoadMainAssetAtPath(p) is Texture ||
                             AssetDatabase.LoadMainAssetAtPath(p) is Material ||
                             AssetDatabase.LoadMainAssetAtPath(p) is Mesh ||
                             AssetDatabase.LoadMainAssetAtPath(p) is AudioClip ||
                             AssetDatabase.LoadMainAssetAtPath(p) is AnimationClip))
                        .Distinct(StringComparer.Ordinal).ToArray();
                    if (common.Length > 0)
                        map.Add(new AssetBundleBuild { assetBundleName = "motorcity-shared.bundle", assetNames = common });
                    log.AppendLine("Explicit shared asset paths: " + common.Length);
                    // Audit remaining implicit cross-package dependencies, including
                    // imported model containers whose main object is not a Mesh.
                    var dependencyRows = new StringBuilder("AssetPath\tPackages\tExplicitShared\n");
                    var groups = map.SelectMany(m => AssetDatabase.GetDependencies(m.assetNames, true)
                        .Distinct(StringComparer.Ordinal).Select(path => new { Path = path, Package = m.assetBundleName }))
                        .GroupBy(x => x.Path, StringComparer.Ordinal)
                        .Where(g => g.Select(x => x.Package).Distinct().Count() > 1)
                        .OrderBy(g => g.Key, StringComparer.Ordinal);
                    var explicitShared = new HashSet<string>(common, StringComparer.Ordinal);
                    foreach (var group in groups)
                        dependencyRows.AppendLine(group.Key + "\t" +
                            string.Join(",", group.Select(x => x.Package).Distinct().OrderBy(n => n)) +
                            "\t" + explicitShared.Contains(group.Key));
                    File.WriteAllText("Library/MotorCityBundleDependencies.tsv", dependencyRows.ToString(), Encoding.UTF8);
                }
                var manifest = BuildPipeline.BuildAssetBundles(bundles, map.ToArray(),
                    BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                    BuildTarget.WebGL);
                if (manifest == null)
                    throw new InvalidOperationException("City AssetBundle build failed.");
                long bundle = new FileInfo(Path.Combine(bundles, "motorcity-city.bundle")).Length;
                log.AppendLine("City bundle LZ4 bytes: " + bundle);

                string stripped = output ?? Path.Combine(root, split ? (districts ? "DistrictStreamingTest" : (hlod ? "HlodSplitCandidate" : "SplitCityTest")) : "NOT-FOR-RELEASE-NoCity");
                if (split)
                {
                    var ordered = new List<string>();
                    var visiting = new HashSet<string>(StringComparer.Ordinal);
                    void Visit(string name)
                    {
                        if (ordered.Contains(name)) return;
                        if (!visiting.Add(name)) throw new InvalidOperationException("Cyclic bundle dependency: " + name);
                        foreach (string dependency in manifest.GetDirectDependencies(name)) Visit(dependency);
                        visiting.Remove(name);
                        ordered.Add(name);
                    }
                    foreach (string name in manifest.GetAllAssetBundles()) Visit(name);
                    // The fleet may depend on resident packages, but no resident
                    // package may depend on the fleet if its download is deferred.
                    if (ordered.Where(n => n != "motorcity-fleet.bundle").Any(n =>
                        manifest.GetAllDependencies(n).Contains("motorcity-fleet.bundle")))
                        throw new InvalidOperationException("A startup bundle depends on the deferred fleet; cannot build safely.");
                    if (ordered.Where(n => n != "motorcity-supporter.bundle").Any(n =>
                        manifest.GetAllDependencies(n).Contains("motorcity-supporter.bundle")))
                        throw new InvalidOperationException("A resident bundle depends on the deferred supporter package.");
                    string FileName(string name) => Path.GetFileNameWithoutExtension(name) + "-" +
                        manifest.GetAssetBundleHash(name) + ".bundle";
                    File.WriteAllText(MarkerPath, string.Join("\n", ordered.Select(name =>
                        name + "|" + manifest.GetAssetBundleHash(name))));
                    AssetDatabase.ImportAsset(MarkerPath, ImportAssetOptions.ForceSynchronousImport);
                    BuildReport releaseReport = Build(profile, stripped, Path.Combine(bundles, "Bundles.manifest"));
                    // Record real player overlap before original resource paths are restored.
                    var streamedDependencies = new HashSet<string>(
                        AssetDatabase.GetDependencies(map.SelectMany(m => m.assetNames).ToArray(), true), StringComparer.Ordinal);
                    var overlap = releaseReport.packedAssets.SelectMany(p => p.contents)
                        .Where(c => streamedDependencies.Contains(c.sourceAssetPath))
                        .GroupBy(c => c.sourceAssetPath)
                        .Select(g => new { Path = g.Key, Bytes = g.Aggregate(0UL, (sum, c) => sum + c.packedSize) })
                        .OrderByDescending(x => x.Bytes).ToArray();
                    var overlapReport = new StringBuilder("PackedPlayerBytes\tGUID\tAssetPath\n");
                    foreach (var item in overlap)
                        overlapReport.AppendLine(item.Bytes + "\t" + AssetDatabase.AssetPathToGUID(item.Path) + "\t" + item.Path);
                    File.WriteAllText("Library/MotorCityStreamOverlap.tsv", overlapReport.ToString(), Encoding.UTF8);
                    log.AppendLine("Stream dependency paths also packed in player: " + overlap.Length);
                    log.AppendLine("Overlap player packed bytes (not transfer savings): " +
                        overlap.Aggregate(0UL, (sum, item) => sum + item.Bytes));
                    string streaming = Path.Combine(stripped, "StreamingAssets");
                    Directory.CreateDirectory(streaming);
                    foreach (string name in ordered)
                        File.Copy(Path.Combine(bundles, name), Path.Combine(streaming, FileName(name)));
                    if (catalog != null)
                    {
                        foreach (var district in catalog.districts)
                            File.Copy(Path.Combine(districtDirectory, district.file), Path.Combine(streaming, district.file));
                        log.AppendLine("District bundles: " + catalog.districts.Length);
                        log.AppendLine("District streamed renderers: " + catalog.districts.Sum(d => d.rendererIndices.Length));
                    }
                    var expected = new HashSet<string>(ordered.Select(FileName), StringComparer.Ordinal);
                    if (catalog != null)
                        foreach (var district in catalog.districts) expected.Add(district.file);
                    var actual = Directory.GetFiles(streaming, "*.bundle").Select(Path.GetFileName).ToArray();
                    if (!expected.SetEquals(actual) || expected.Any(n => new FileInfo(Path.Combine(streaming, n)).Length == 0))
                        throw new InvalidOperationException("Release candidate bundle inventory mismatch.");
                    if (!File.Exists(Path.Combine(stripped, "index.html")))
                        throw new InvalidOperationException("Release candidate missing index.html.");
                    foreach (string name in expected.OrderBy(n => n))
                        log.AppendLine("Streaming bundle: " + name + " bytes=" +
                            new FileInfo(Path.Combine(streaming, name)).Length);
                    log.AppendLine("Split test output: " + stripped);
                    log.AppendLine("Split data bytes: " + DataBytes(stripped));
                    log.AppendLine("Split wasm bytes: " + WasmBytes(stripped));
                    long startupBytes = DataBytes(stripped) + WasmBytes(stripped);
                    long streamBytes = expected.Sum(n => new FileInfo(Path.Combine(streaming, n)).Length);
                    log.AppendLine("Startup DATA + WASM bytes: " + startupBytes);
                    log.AppendLine("Startup DATA + WASM MiB: " + (startupBytes / 1048576.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                    log.AppendLine("All streamed bundle bytes: " + streamBytes);
                    log.AppendLine("Startup target (10 MiB): " + (startupBytes <= 10L * 1048576L ? "MET" : "NOT MET"));
                    log.AppendLine("Bundle bytes: " + bundle);
                    Debug.Log("Motor City: complete Playgama district build: " + stripped);
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
            foreach (string name in GameplayPaths.Reverse())
                if (Exists(GameplayParked(name)))
                    MoveIntro(GameplayParked(name), GameplayOriginal(name));
            RestoreIntroSlides();
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
                options = BuildOptions.DetailedBuildReport
            });
            if (report == null || report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Diagnostic WebGL build failed: " + directory);
            // Capture packed assets while temporary streaming roots are still in place.
            // These are packed (pre-transfer) sizes, not compressed network bytes.
            var rows = report.packedAssets.SelectMany(p => p.contents)
                .GroupBy(c => c.sourceAssetPath ?? "")
                .Select(g => new { Path = g.Key, Bytes = g.Aggregate(0UL, (sum, c) => sum + c.packedSize) })
                .OrderByDescending(x => x.Bytes);
            var inventory = new StringBuilder("PackedBytes\tAssetPath\n");
            foreach (var row in rows)
                inventory.AppendLine(row.Bytes + "\t" + row.Path);
            File.WriteAllText("Library/MotorCityPlayerPackedAssets.tsv", inventory.ToString(), Encoding.UTF8);
            Debug.Log("Motor City: player asset inventory: Library/MotorCityPlayerPackedAssets.tsv");
            // Optional editor-only bridge to Playgama 2.4.4's report UI.
            // Never depend on the package for the production build.
            if (File.Exists(MarkerPath))
            {
                try
                {
                    var analyzer = Type.GetType("Playgama.Editor.BuildAnalyzer, Playgama.Bridge.Editor", false);
                    if (analyzer != null)
                    {
                        var method = analyzer.GetMethod("AnalyzeReport",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                            null, new[] { typeof(BuildReport), typeof(TimeSpan), typeof(bool) }, null);
                        if (method == null)
                            Debug.LogWarning("Motor City: Playgama analyzer API changed; use the TSV report instead.");
                        else
                            method.Invoke(null, new object[] { report, report.summary.totalTime, true });
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Motor City: optional Playgama report export failed: " + ex.Message);
                }
            }
            return report;
        }

        private static long DataBytes(string directory) => PayloadBytes(directory, ".data");
        private static long WasmBytes(string directory) => PayloadBytes(directory, ".wasm");
        private static long PayloadBytes(string directory, string suffix)
        {
            var files = Directory.GetFiles(Path.Combine(directory, "Build")).Where(p => new[] { "", ".br", ".gz", ".unityweb" }.Any(ext => p.EndsWith(suffix + ext, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (files.Length != 1)
                throw new InvalidOperationException("Expected exactly one " + suffix + " payload; check WebGL compression settings.");
            return new FileInfo(files[0]).Length;
        }
    }
}
