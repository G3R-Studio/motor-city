using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;

namespace MotorCity.World
{
    public static class MotorCityCityBundle
    {
        private const string Marker = "MotorCityCityBundleVersion";
        private static AssetBundle bundle;
        private static bool running;
        private static GameObject prefab;
        public static bool Failed { get; private set; }
        public static float Progress { get; private set; }
        public static bool Enabled => Application.platform == RuntimePlatform.WebGLPlayer &&
            Resources.Load<TextAsset>(Marker) != null;
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

        private static IEnumerator Download()
        {
            string[] settings = Resources.Load<TextAsset>(Marker).text.Trim().Split('\n');
            string version = settings[0].Trim();
            bool packed = settings.Length > 1 && settings[1].Trim() == "gzip";
            string url = Application.streamingAssetsPath.TrimEnd('/') +
                "/motorcity-city-" + version + ".bundle";
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Progress = 0f;
                if (packed && attempt == 0)
                    yield return DownloadPacked(url + ".pack");
                else
                using (var request = UnityWebRequestAssetBundle.GetAssetBundle(url, Hash128.Parse(version)))
                {
                    request.timeout = 120;
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        Progress = Mathf.Clamp01(request.downloadProgress) * 0.85f;
                        yield return null;
                    }
                    if (request.result == UnityWebRequest.Result.Success)
                        bundle = DownloadHandlerAssetBundle.GetContent(request);
                    else
                        Debug.LogWarning("Motor City city download: " + request.error);
                }
                if (bundle != null)
                {
                    var load = bundle.LoadAssetAsync<GameObject>("MotorCity/Environment/CityVisual");
                    while (!load.isDone)
                    {
                        Progress = 0.85f + 0.15f * load.progress;
                        yield return null;
                    }
                    prefab = load.asset as GameObject;
                    if (prefab != null)
                    {
                        Progress = 1f;
                        running = false;
                        yield break;
                    }
                    bundle.Unload(true);
                    bundle = null;
                }
                yield return new WaitForSecondsRealtime(2f);
            }
            Failed = true;
            running = false;
            Debug.LogError("Motor City: city bundle unavailable. Return to menu and retry.");
        }

        // The .pack file is ordinary binary data, not HTTP content-encoded.
        // This deliberately bypasses Unity's AssetBundle disk cache; the
        // versioned URL can still use the browser's normal HTTP cache.
        private static IEnumerator DownloadPacked(string url)
        {
            byte[] compressed;
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 120;
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    Progress = Mathf.Clamp01(request.downloadProgress) * 0.70f;
                    yield return null;
                }
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Motor City packed bundle: " + request.error);
                    yield break;
                }
                compressed = request.downloadHandler.data;
            }

            byte[] decoded;
            try
            {
                if (compressed.Length < 18 || compressed[0] != 0x1f || compressed[1] != 0x8b)
                    throw new InvalidDataException("Invalid gzip payload");
                uint size = BitConverter.ToUInt32(compressed, compressed.Length - 4);
                if (size == 0 || size > 128 * 1024 * 1024)
                    throw new InvalidDataException("Unexpected decoded bundle size");
                decoded = new byte[(int)size];
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Motor City packed bundle: " + ex.Message);
                yield break;
            }
            using (var input = new MemoryStream(compressed, false))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            {
                int offset = 0;
                while (offset < decoded.Length)
                {
                    int read;
                    try { read = gzip.Read(decoded, offset, Math.Min(512 * 1024, decoded.Length - offset)); }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("Motor City gzip: " + ex.Message);
                        yield break;
                    }
                    if (read == 0) yield break;
                    offset += read;
                    Progress = 0.70f + 0.10f * offset / decoded.Length;
                    yield return null;
                }
            }
            compressed = null;
            var load = AssetBundle.LoadFromMemoryAsync(decoded);
            while (!load.isDone) yield return null;
            bundle = load.assetBundle;
        }

        public static T Load<T>(string path) where T : UnityEngine.Object =>
            Enabled ? (bundle == null ? null : bundle.LoadAsset<T>(path)) : Resources.Load<T>(path);

        public static T[] LoadAll<T>(string path) where T : UnityEngine.Object =>
            !Enabled ? Resources.LoadAll<T>(path) :
            bundle == null ? Array.Empty<T>() :
            bundle.GetAllAssetNames().Where(n => n.StartsWith(path.ToLowerInvariant() + "/", StringComparison.Ordinal))
                .Select(n => bundle.LoadAsset<T>(n)).Where(a => a != null).ToArray();
    }
}
