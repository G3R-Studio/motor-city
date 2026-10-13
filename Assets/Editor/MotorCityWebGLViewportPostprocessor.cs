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
            // Unity's default WebGL template assigns inline 1920x1080
            // dimensions to the canvas. Its own JavaScript also preserves
            // that resolution unless explicitly told to follow CSS size.
            const string resizeScript = @"<script>
(function () {
  function fitUnityCanvas() {
    var canvas = document.getElementById('unity-canvas');
    if (!canvas) return;
    var width = Math.max(1, document.documentElement.clientWidth);
    var height = Math.max(1, document.documentElement.clientHeight);
    if (canvas.style.width !== width + 'px' ||
        canvas.style.getPropertyPriority('width') !== 'important')
      canvas.style.setProperty('width', width + 'px', 'important');
    if (canvas.style.height !== height + 'px' ||
        canvas.style.getPropertyPriority('height') !== 'important')
      canvas.style.setProperty('height', height + 'px', 'important');
    if (canvas.width !== width) canvas.width = width;
    if (canvas.height !== height) canvas.height = height;
  }
  document.addEventListener('DOMContentLoaded', function () {
    fitUnityCanvas();
    window.addEventListener('resize', fitUnityCanvas);
    var canvas = document.getElementById('unity-canvas');
    if (canvas && window.ResizeObserver)
      new ResizeObserver(fitUnityCanvas).observe(document.documentElement);
    if (canvas && window.MutationObserver)
      new MutationObserver(fitUnityCanvas).observe(canvas, {
        attributes: true,
        attributeFilter: ['style', 'width', 'height']
      });
    // Unity's startup loader may assign fixed dimensions after DOMContentLoaded.
    // Recheck when the engine is first initialized.
    window.addEventListener('load', fitUnityCanvas);
  });
})();
</script>";
            int closingHead = html.IndexOf(
                "</head>", StringComparison.OrdinalIgnoreCase);
            if (closingHead < 0)
                throw new BuildFailedException(
                    "WebGL index.html has no </head>; cannot install iframe layout");
            html = html.Insert(closingHead, css + Environment.NewLine + resizeScript + Environment.NewLine);
            File.WriteAllText(path, html);
            Debug.Log("Motor City WebGL: canvas fitted to iframe viewport at build time.");
        }
    }
}
