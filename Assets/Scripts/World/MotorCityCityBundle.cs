using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace MotorCity.World
{
    public static class MotorCityCityBundle
    {
        [Serializable] public sealed class DistrictCatalog { public District[] districts; public int rendererCount; }
        [Serializable] public sealed class District
        {
            public string file, hash;
            public int[] rendererIndices;
            public string[] meshNames;
            public Bounds bounds;
            public Bounds[] localBounds;
        }
        private static DistrictCatalog catalog;
        private static bool warmFailed;
        private static bool? enabledCache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            enabledCache = null;
            packageVersions = null;
            catalog = null;
            warmFailed = false;
            Warm.Clear();
            gameplayBundles.Clear();
            fleet = null;
            supporter = null;
            bundle = null;
            prefab = null;
            running = false;
            Failed = false;
            Progress = 0f;
        }
        internal sealed class WarmDistrict
        {
            public AssetBundle Bundle;
            public Dictionary<string, Mesh> Meshes;
        }
        internal static readonly Dictionary<string, WarmDistrict> Warm = new();
        public static bool DistrictStreamingEnabled => catalog != null;

        private static IEnumerator WarmSpawnDistricts()
        {
            warmFailed = false;
            if (catalog?.districts == null) yield break;
            var position = CityAssetRuntimeInstaller.PlayerSpawnPoint;
            float radius = MotorCityVisibilityRuntime.FarClipDistance + 160f;
            var nearby = catalog.districts.Where(d => d.bounds.SqrDistance(position) <= radius * radius)
                .OrderBy(d => d.bounds.SqrDistance(position)).ToArray();
            foreach (var district in nearby)
            {
                string url = Application.streamingAssetsPath.TrimEnd('/') + "/" + district.file;
                using (var request = UnityWebRequestAssetBundle.GetAssetBundle(url, Hash128.Parse(district.hash)))
                {
                    request.timeout = 120;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) { warmFailed = true; yield break; }
                    var loaded = DownloadHandlerAssetBundle.GetContent(request);
                    if (loaded == null) { warmFailed = true; yield break; }
                    var assets = loaded.LoadAllAssetsAsync<Mesh>();
                    yield return assets;
                    var meshes = new Dictionary<string, Mesh>(StringComparer.Ordinal);
                    foreach (var asset in assets.allAssets)
                        if (asset is Mesh mesh) meshes[mesh.name] = mesh;
                    if (district.meshNames.All(meshes.ContainsKey))
                        Warm[district.file] = new WarmDistrict { Bundle = loaded, Meshes = meshes };
                    else { loaded.Unload(true); warmFailed = true; yield break; }
                }
            }
        }
        public static void AttachDistricts(GameObject city)
        {
            if (catalog == null || catalog.districts == null || city == null) return;
            var host = city.AddComponent<DistrictMeshStreaming>();
            host.Initialize(city, catalog);
        }

        private const string Marker = "MotorCityCityBundleVersion";
        private static AssetBundle bundle;
        private static readonly List<AssetBundle> gameplayBundles = new List<AssetBundle>();
        private static bool running;
        private static GameObject prefab;
        public static bool Failed { get; private set; }
        public static float Progress { get; private set; }
        public static bool Enabled
        {
            get
            {
                if (!enabledCache.HasValue)
                    enabledCache = Application.platform == RuntimePlatform.WebGLPlayer &&
                        Resources.Load<TextAsset>(Marker) != null;
                return enabledCache.Value;
            }
        }
        public static bool Ready => prefab != null;
        public static GameObject Prefab => prefab;

        public static void Begin(MonoBehaviour host)
        {
            if (!Enabled || running || Ready) return;
            Failed = false;
            Progress = 0f;
            running = true;
            host.StartCoroutine(Download());
        }

        private const string SupporterName = "motorcity-supporter.bundle";
        private static AssetBundle supporter;
        public static bool SupporterReady => supporter != null || string.IsNullOrEmpty(SupporterVersion());
        private static Dictionary<string, string> packageVersions;
        private static string PackageVersion(string name)
        {
            if (!Enabled) return null;
            if (packageVersions == null)
            {
                packageVersions = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string line in Resources.Load<TextAsset>(Marker).text.Split(
                    new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length == 2) packageVersions[parts[0]] = parts[1];
                }
            }
            return packageVersions.TryGetValue(name, out string version) ? version : null;
        }
        private static string SupporterVersion() => PackageVersion(SupporterName);
        public static IEnumerator LoadSupporter()
        {
            string version = SupporterVersion();
            if (SupporterReady) yield break;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                using (var request = UnityWebRequestAssetBundle.GetAssetBundle(Application.streamingAssetsPath.TrimEnd('/') + "/motorcity-supporter-" + version + ".bundle", Hash128.Parse(version)))
                {
                    request.timeout = 120;
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success) supporter = DownloadHandlerAssetBundle.GetContent(request);
                    if (supporter != null) yield break;
                }
                yield return new WaitForSecondsRealtime(2f);
            }
            Debug.LogWarning("Motor City: supporter visual download failed.");
        }
        private const string FleetName = "motorcity-fleet.bundle";
        private static AssetBundle fleet;
        private static string FleetVersion() => PackageVersion(FleetName);

        public static bool FleetReady => fleet != null || string.IsNullOrEmpty(FleetVersion());

        public static IEnumerator LoadFleet(Action<float> progress)
        {
            string version = FleetVersion();
            if (FleetReady) yield break;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string url = Application.streamingAssetsPath.TrimEnd('/') + "/motorcity-fleet-" + version + ".bundle";
                using (var request = UnityWebRequestAssetBundle.GetAssetBundle(url, Hash128.Parse(version)))
                {
                    request.timeout = 120;
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        progress?.Invoke(Mathf.Clamp01(request.downloadProgress));
                        yield return null;
                    }
                    if (request.result == UnityWebRequest.Result.Success)
                        fleet = DownloadHandlerAssetBundle.GetContent(request);
                    if (fleet != null) { progress?.Invoke(1f); yield break; }
                }
                yield return new WaitForSecondsRealtime(2f);
            }
        }

        private static IEnumerator Download()
        {
            // Legacy marker: city hash only. New marker: dependency-ordered name|hash lines.
            string marker = Resources.Load<TextAsset>(Marker).text.Trim();
            string[] files = marker.Contains("|") ? marker.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                : new[] { "motorcity-city.bundle|" + marker };
            if (files.Any(line =>
            {
                var parts = line.Split('|');
                return parts.Length != 2 || !parts[0].EndsWith(".bundle", StringComparison.Ordinal) ||
                    parts[1].Length != 32 || parts[1].Any(c => !Uri.IsHexDigit(c));
            }) || !files.Any(line => line.StartsWith("motorcity-city.bundle|", StringComparison.Ordinal)))
            {
                Failed = true;
                running = false;
                Debug.LogError("Motor City: invalid streaming package marker.");
                yield break;
            }
            files = files.Where(line => !line.StartsWith(SupporterName + "|", StringComparison.Ordinal)).ToArray();
            // Only a new or already migrated starter save can defer the fleet.
            // Old roster indices must be resolved by the existing migration.
            bool starter = MotorCity.Persistence.MotorCitySaveService.GetInt("MotorCity.Vehicle.Selected", 0) == 0 &&
                MotorCity.Persistence.MotorCitySaveService.GetInt("MotorCity.Vehicle.RosterOrderVersion", 0) >= 2;
            bool fresh = MotorCity.Persistence.MotorCitySaveService.GetInt("MotorCity.Vehicle.Selected", -1) == -1;
            if (starter || fresh)
                files = files.Where(line => !line.StartsWith(FleetName + "|", StringComparison.Ordinal)).ToArray();
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Progress = 0f;
                for (int i = 0; i < files.Length; i++)
                {
                    string[] entry = files[i].Split('|');
                    string name = entry[0];
                    string version = entry[1];
                    string url = Application.streamingAssetsPath.TrimEnd('/') + "/" +
                        name.Substring(0, name.Length - ".bundle".Length) + "-" + version + ".bundle";
                    using (var request = UnityWebRequestAssetBundle.GetAssetBundle(url, Hash128.Parse(version)))
                    {
                        request.timeout = 120;
                        var operation = request.SendWebRequest();
                        while (!operation.isDone)
                        {
                            Progress = (i + Mathf.Clamp01(request.downloadProgress)) / files.Length * 0.85f;
                            yield return null;
                        }
                        var loaded = request.result == UnityWebRequest.Result.Success
                            ? DownloadHandlerAssetBundle.GetContent(request) : null;
                        if (loaded == null)
                        {
                            Debug.LogWarning("Motor City package download: " + name + " " + request.error);
                            if (bundle != null) { bundle.Unload(true); bundle = null; }
                            foreach (var other in gameplayBundles) other.Unload(true);
                            gameplayBundles.Clear();
                            if (fleet != null) { fleet.Unload(true); fleet = null; }
                            break;
                        }
                        if (name == "motorcity-city.bundle") bundle = loaded;
                        else if (name == FleetName) fleet = loaded;
                        else gameplayBundles.Add(loaded);
                    }
                }
                if (bundle != null)
                {
                    var load = bundle.LoadAssetAsync<GameObject>("MotorCity/Environment/CityVisual");
                    while (!load.isDone)
                    {
                        Progress = 0.85f + 0.15f * load.progress;
                        yield return null;
                    }
                    var loadedPrefab = load.asset as GameObject;
                    if (loadedPrefab != null)
                    {
                        var metadata = bundle.LoadAsset<TextAsset>("MotorCity/DistrictCatalog");
                        catalog = metadata != null ? JsonUtility.FromJson<DistrictCatalog>(metadata.text) : null;
                        if (metadata != null && !ValidateCatalog(loadedPrefab))
                            warmFailed = true;
                        else
                            yield return WarmSpawnDistricts();
                        if (!warmFailed)
                        {
                            prefab = loadedPrefab;
                            Progress = 1f;
                            running = false;
                            yield break;
                        }
                        // Never display Ready with missing initial district geometry.
                        foreach (var warm in Warm.Values)
                            if (warm.Bundle != null) warm.Bundle.Unload(true);
                        Warm.Clear();
                        catalog = null;
                        Debug.LogWarning("Motor City: district preload failed; retrying city download.");

                    }
                    bundle.Unload(true);
                    bundle = null;
                }
                foreach (var other in gameplayBundles) other.Unload(true);
                gameplayBundles.Clear();
                if (fleet != null) { fleet.Unload(true); fleet = null; }
                yield return new WaitForSecondsRealtime(2f);
            }
            Failed = true;
            running = false;
            Debug.LogError("Motor City: city bundle unavailable. Return to menu and retry.");
        }

        private static bool ValidateCatalog(GameObject city)
        {
            if (catalog?.districts == null || catalog.districts.Length == 0) return false;
            var renderers = city.GetComponentsInChildren<MeshRenderer>(true);
            if (catalog.rendererCount != renderers.Length) return false;
            var indices = new HashSet<int>();
            foreach (var district in catalog.districts)
            {
                if (district == null || string.IsNullOrEmpty(district.file) ||
                    district.meshNames == null || district.rendererIndices == null || district.localBounds == null ||
                    district.rendererIndices.Length != district.meshNames.Length ||
                    district.rendererIndices.Length != district.localBounds.Length) return false;
                foreach (int index in district.rendererIndices)
                    if (index < 0 || index >= renderers.Length || !indices.Add(index) ||
                        !renderers[index].TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh != null)
                        return false;
            }
            return true;
        }

        public static T Load<T>(string path) where T : UnityEngine.Object =>
            Enabled ? (bundle == null ? null : bundle.LoadAsset<T>(path)) : Resources.Load<T>(path);

        // Gameplay visuals may be packed with the city or retained in Resources.
        // Native players and Editor use the same original resource addresses.
        public static bool HasGameplayAsset(string path)
        {
            if (fleet != null && fleet.Contains(path)) return true;
            if (!FleetReady && (path == "MotorCity/PlayerCarVisual" ||
                path.StartsWith("MotorCity/Vehicles/Player/", StringComparison.Ordinal) &&
                path != "MotorCity/Vehicles/Player/Beatall")) return true;
            if (Enabled)
            {
                foreach (var other in gameplayBundles)
                    if (other.Contains(path)) return true;
                if (bundle != null && bundle.Contains(path)) return true;
            }
            return Resources.Load<GameObject>(path) != null;
        }

        public static T LoadGameplay<T>(string path) where T : UnityEngine.Object
        {
            if (supporter != null && supporter.Contains(path)) return supporter.LoadAsset<T>(path);
            if (fleet != null && fleet.Contains(path)) return fleet.LoadAsset<T>(path);
            if (Enabled)
                foreach (var other in gameplayBundles)
                    if (other.Contains(path)) return other.LoadAsset<T>(path);
            if (Enabled && bundle != null && bundle.Contains(path))
                return bundle.LoadAsset<T>(path);
            return Resources.Load<T>(path);
        }

        public static T[] LoadAll<T>(string path) where T : UnityEngine.Object =>
            !Enabled ? Resources.LoadAll<T>(path) :
            bundle == null ? Array.Empty<T>() :
            bundle.GetAllAssetNames().Where(n => n.StartsWith(path.ToLowerInvariant() + "/", StringComparison.Ordinal))
                .Select(n => bundle.LoadAsset<T>(n)).Where(a => a != null).ToArray();
    }

    // Only visual meshes are streamed. The city hierarchy, scripts and colliders
    // remain resident. Never unload a bundle until every assigned mesh is detached.
    public sealed class DistrictMeshStreaming : MonoBehaviour
    {
        private sealed class State
        {
            public MotorCityCityBundle.District Data;
            public AssetBundle Bundle;
            public bool Busy;
            public float RetryAt;
        }
        private MeshRenderer[] renderers;
        private readonly List<State> states = new();
        private bool loading;
        private bool initialized;
        private float nextCheck;

        public void Initialize(GameObject city, MotorCityCityBundle.DistrictCatalog catalog)
        {
            if (initialized) return;
            renderers = city.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var data in catalog.districts)
            {
                if (data.rendererIndices == null || data.meshNames == null ||
                    data.localBounds == null || data.rendererIndices.Length != data.meshNames.Length ||
                    data.rendererIndices.Length != data.localBounds.Length ||
                    Array.Exists(data.rendererIndices, i => i < 0 || i >= renderers.Length ||
                        renderers[i].GetComponent<MeshFilter>() == null))
                {
                    Debug.LogError("District catalog does not match city.");
                    return;
                }
            }
            foreach (var data in catalog.districts)
            {
                for (int i = 0; i < data.rendererIndices.Length; i++)
                    renderers[data.rendererIndices[i]].localBounds = data.localBounds[i];
                var state = new State { Data = data };
                if (MotorCityCityBundle.Warm.TryGetValue(data.file, out var warm))
                {
                    state.Bundle = warm.Bundle;
                    for (int i = 0; i < data.rendererIndices.Length; i++)
                        renderers[data.rendererIndices[i]].GetComponent<MeshFilter>().sharedMesh = warm.Meshes[data.meshNames[i]];
                    MotorCityCityBundle.Warm.Remove(data.file);
                }
                states.Add(state);
            }
            initialized = true;
        }

        private void Update()
        {
            if (!initialized || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .2f;
            var camera = Camera.main;
            if (camera == null) return;
            // Full 360-degree reserve, independent of camera heading.
            float radius = camera.farClipPlane + 160f;
            State nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (var state in states)
            {
                float distance = Mathf.Sqrt(state.Data.bounds.SqrDistance(camera.transform.position));
                if (state.Bundle != null && !state.Busy && distance > radius + 128f)
                    Release(state);
                if (state.Bundle == null && !state.Busy && Time.unscaledTime >= state.RetryAt &&
                    distance <= radius && distance < nearestDistance)
                {
                    nearest = state;
                    nearestDistance = distance;
                }
            }
            if (!loading && nearest != null) StartCoroutine(Load(nearest));
        }

        private IEnumerator Load(State state)
        {
            loading = state.Busy = true;
            try
            {
                string url = Application.streamingAssetsPath.TrimEnd('/') + "/" + state.Data.file;
                using (var request = UnityWebRequestAssetBundle.GetAssetBundle(url, Hash128.Parse(state.Data.hash)))
                {
                    request.timeout = 120;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning("District download failed: " + request.error);
                        yield break;
                    }
                    state.Bundle = DownloadHandlerAssetBundle.GetContent(request);
                }
                if (state.Bundle == null) yield break;
                var assets = state.Bundle.LoadAllAssetsAsync<Mesh>();
                yield return assets;
                var meshes = new Dictionary<string, Mesh>(StringComparer.Ordinal);
                foreach (var asset in assets.allAssets)
                    if (asset is Mesh mesh) meshes[mesh.name] = mesh;
                foreach (string name in state.Data.meshNames)
                    if (!meshes.ContainsKey(name))
                    {
                        Debug.LogError("District mesh missing: " + name);
                        Release(state);
                        yield break;
                    }
                for (int i = 0; i < state.Data.rendererIndices.Length; i++)
                {
                    var renderer = renderers[state.Data.rendererIndices[i]];
                    if (renderer != null) renderer.GetComponent<MeshFilter>().sharedMesh = meshes[state.Data.meshNames[i]];
                }
            }
            finally
            {
                state.Busy = loading = false;
                state.RetryAt = Time.unscaledTime + 5f;
            }
        }

        private void Release(State state)
        {
            foreach (int index in state.Data.rendererIndices)
            {
                var renderer = renderers[index];
                if (renderer != null) renderer.GetComponent<MeshFilter>().sharedMesh = null;
            }
            if (state.Bundle != null) state.Bundle.Unload(true);
            state.Bundle = null;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            foreach (var state in states) Release(state);
        }
    }
}
