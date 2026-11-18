using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MotorCity.Diagnostics
{
    internal static class MotorCityWebRenderDiagnostics
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private static int reports;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            reports = 0;
            if (System.Array.Exists(Application.absoluteURL.Split('?', '&', '#'), x => x == "motorcity_metrics=on"))
            {
                var host = new GameObject("Motor City Performance Metrics");
                Object.DontDestroyOnLoad(host);
                host.AddComponent<MotorCityPerformanceMetrics>();
            }
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (reports >= 2 || !message.Contains("RenderTextureDesc") ||
                (!message.Contains("width must be greater than zero") &&
                 !message.Contains("height must be greater than zero")))
                return;

            reports++;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var info = new System.Text.StringBuilder("[MotorCity][RenderDiagnostic] ");
            info.Append("screen=").Append(Screen.width).Append("x").Append(Screen.height);
            if (urp != null) info.Append(" renderScale=").Append(urp.renderScale);
            foreach (Camera camera in Camera.allCameras)
            {
                info.Append("\nCamera ").Append(camera.name)
                    .Append(" pixels=").Append(camera.pixelWidth).Append("x").Append(camera.pixelHeight)
                    .Append(" scaled=").Append(camera.scaledPixelWidth).Append("x").Append(camera.scaledPixelHeight)
                    .Append(" rect=").Append(camera.rect)
                    .Append(" target=").Append(camera.targetTexture == null ? "screen" :
                        camera.targetTexture.width + "x" + camera.targetTexture.height);
            }
            Debug.LogWarning(info.ToString());
        }
#endif
    }
#if UNITY_WEBGL && !UNITY_EDITOR
    internal sealed class MotorCityPerformanceMetrics : MonoBehaviour
    {
        private float elapsed;
        private int frames;
        private float worst;
        private int slowFrames;

        private void Update()
        {
            if (!Application.isFocused || Time.timeScale == 0f)
            {
                elapsed = worst = 0f;
                frames = slowFrames = 0;
                return;
            }
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            frames++;
            worst = Mathf.Max(worst, dt);
            if (dt > 0.05f) slowFrames++;
            if (elapsed < 10f) return;
            const float mib = 1048576f;
            Debug.Log($"[MotorCity][Perf] preset={MotorCity.Platform.MotorCityQualityRuntime.CurrentPreset} fps={frames / elapsed:F1} worstMs={worst * 1000f:F0} framesOver50ms={slowFrames}/{frames} unityAllocatedMiB={UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / mib:F1} unityReservedMiB={UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / mib:F1} screen={Screen.width}x{Screen.height}");
            elapsed = worst = 0f;
            frames = slowFrames = 0;
        }
    }
#endif
}
