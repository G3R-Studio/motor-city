using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>Installs the Bridge before any engine/asset requests in the default Unity Web template.</summary>
    public sealed class MotorCityPlaygamaPostprocessor : IPostprocessBuildWithReport, IPreprocessBuildWithReport
    {
        public int callbackOrder => 600;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;
            if (!File.Exists("Assets/Resources/MotorCityCityBundleVersion.txt") &&
                !File.Exists("Assets/Resources/MotorCity/Environment/CityVisual.prefab"))
                throw new BuildFailedException("Motor City: original city missing. Run Restore City After Experiment.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;
            string root = report.summary.outputPath;
            string path = Path.Combine(root, "index.html");
            string html = File.ReadAllText(path);
            const string marker = "motor-city-playgama-v1";
            // Incremental builds can reuse an already patched HTML page.
            // Always refresh the bridge files; never silently accept a partial patch.
            File.Copy("Assets/Editor/Playgama/motor-city-bridge.js.txt", Path.Combine(root, "motor-city-bridge.js"), true);
            File.Copy("Assets/Editor/Playgama/playgama-bridge-config.json", Path.Combine(root, "playgama-bridge-config.json"), true);
            if (html.Contains(marker))
            {
                if (!html.Contains("window.motorCityBridgeReady.then(function () {") ||
                    !html.Contains("src=\"motor-city-bridge.js\"") ||
                    !html.Contains("Bridge save state unavailable"))
                    throw new BuildFailedException("Playgama: incomplete existing HTML patch. Build into an empty folder.");
                return;
            }
            var head = Regex.Match(html, @"<head\b[^>]*>", RegexOptions.IgnoreCase);
            if (!head.Success) throw new BuildFailedException("Playgama: WebGL page has no head");
            var scripts = new Regex(@"(<script\b[^>]*>)([\s\S]*?)(</script\s*>)", RegexOptions.IgnoreCase);
            int count = 0;
            html = scripts.Replace(html, match =>
            {
                string body = match.Groups[2].Value;
                if (!body.Contains("createUnityInstance(")) return match.Value;
                count++;
                // Default template uses an inline engine startup block. Delay the whole
                // block (including creation of the loader script), not just the WASM launch.
                return match.Groups[1].Value +
                    "\nwindow.motorCityBridgeReady.then(function () {\n" +
                    "if (!window.motorCityBridge || !window.motorCityBridge.initialized || !window.motorCityBridge.prefs) throw new Error('Motor City: Bridge save state unavailable');\n" + body +
                    "\n}).catch(window.motorCityBridgeStartupError);\n" + match.Groups[3].Value;
            });
            if (count != 1) throw new BuildFailedException("Playgama: expected exactly one Unity startup script, found " + count);
            html = html.Insert(head.Index + head.Length,
                "\n<script id=\"" + marker + "\" src=\"https://bridge.playgama.com/v2/stable/playgama-bridge.js\"></script>\n" +
                "<script src=\"motor-city-bridge.js\"></script>\n");
            File.WriteAllText(path, html);
            Debug.Log("Motor City: Playgama Web build prepared at " + root + ". Archive its contents including index.html.");
        }
    }
}
