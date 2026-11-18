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
}
