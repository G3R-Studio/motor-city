using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MotorCity.EditorTools
{
    // Unity's default desktop WebGL template hardcodes a 960x600 canvas.
    // In Yandex Games the actual iframe viewport may shrink for sticky ads
    // or the platform header. Override the template before first render.
    public sealed class MotorCityWebGLViewportPostprocessor :
        IPostprocessBuildWithReport
    {
        public int callbackOrder => 500;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            string path = Path.Combine(
                report.summary.outputPath, "index.html");
            if (!File.Exists(path))
                throw new BuildFailedException(
                    "Missing WebGL entrypoint: " + path);

            string html = File.ReadAllText(path);
            const string marker = "motor-city-iframe-viewport-v1";
            if (html.Contains(marker))
                return;

            const string css = @"<style id=""motor-city-iframe-viewport-v1"">
html, body {
  width: 100% !important;
  height: 100% !important;
  min-width: 0 !important;
  min-height: 0 !important;
  margin: 0 !important;
  padding: 0 !important;
  overflow: hidden !important;
}
#unity-container,
#unity-container.unity-desktop,
#unity-container.unity-mobile {
  position: absolute !important;
  inset: 0 !important;
  left: 0 !important;
  top: 0 !important;
  transform: none !important;
  width: 100% !important;
  height: 100% !important;
  max-width: 100% !important;
  max-height: 100% !important;
  overflow: hidden !important;
}
#unity-canvas,
#unity-canvas.unity-desktop,
#unity-canvas.unity-mobile {
  display: block !important;
  width: 100% !important;
  height: 100% !important;
  max-width: 100% !important;
  max-height: 100% !important;
  margin: 0 !important;
  padding: 0 !important;
}
</style>";
            int closingHead = html.IndexOf(
                "</head>", StringComparison.OrdinalIgnoreCase);
            if (closingHead < 0)
                throw new BuildFailedException(
                    "WebGL index.html has no </head>; cannot install iframe layout");
            html = html.Insert(closingHead, css + Environment.NewLine);
            File.WriteAllText(path, html);
            Debug.Log("Motor City WebGL: canvas fitted to iframe viewport at build time.");
        }
    }
}
