using System;
using System.Collections;
using System.Linq;
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
            string version = Resources.Load<TextAsset>(Marker).text.Trim();
            string url = Application.streamingAssetsPath.TrimEnd('/') +
                "/motorcity-city-" + version + ".bundle";
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Progress = 0f;
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

        public static T Load<T>(string path) where T : UnityEngine.Object =>
            Enabled ? (bundle == null ? null : bundle.LoadAsset<T>(path)) : Resources.Load<T>(path);

        public static T[] LoadAll<T>(string path) where T : UnityEngine.Object =>
            !Enabled ? Resources.LoadAll<T>(path) :
            bundle == null ? Array.Empty<T>() :
            bundle.GetAllAssetNames().Where(n => n.StartsWith(path.ToLowerInvariant() + "/", StringComparison.Ordinal))
                .Select(n => bundle.LoadAsset<T>(n)).Where(a => a != null).ToArray();
    }
}
