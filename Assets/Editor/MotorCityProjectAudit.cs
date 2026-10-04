using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Read-only audit. Candidates require inspection: Resources/string-based loads,
// FBX embedded dependencies and source assets cannot be deleted by reachability alone.
public static class MotorCityProjectAudit
{
    [MenuItem("Motor City/Audit/Write project and loaded scene report")]
    public static void WriteReport()
    {
        var report = new StringBuilder();
        report.AppendLine("Motor City project audit — " + DateTime.UtcNow.ToString("u"));
        report.AppendLine("Read-only report. No assets, scenes or objects were changed.");
        report.AppendLine();
        string[] paths = AssetDatabase.GetAllAssetPaths();
        var inbound = new Dictionary<string, int>(StringComparer.Ordinal);
        int assets = 0;
        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || AssetDatabase.IsValidFolder(path)) continue;
                assets++;
                if (i % 100 == 0 && EditorUtility.DisplayCancelableProgressBar("Motor City audit", path, (float)i / paths.Length))
                    throw new OperationCanceledException("Audit cancelled; project unchanged.");
                if (path.EndsWith(".prefab", StringComparison.Ordinal))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    foreach (Transform transform in prefab.GetComponentsInChildren<Transform>(true))
                    {
                        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                        if (missing > 0)
                            report.AppendLine("  ERROR missing prefab scripts: " + path + " / " + HierarchyPath(transform) + ": " + missing);
                    }
                }
                foreach (string dependency in AssetDatabase.GetDependencies(path, false))
                {
                    if (dependency == path) continue;
                    inbound.TryGetValue(dependency, out int count);
                    inbound[dependency] = count + 1;
                }
            }
            report.AppendLine("Imported project assets: " + assets);
            report.AppendLine("Build scenes:");
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                report.AppendLine("  " + scene.path);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null)
                    report.AppendLine("  ERROR: enabled scene is missing.");
            }
            report.AppendLine();
            report.AppendLine("Assets without incoming imported dependencies (candidates, NOT a deletion list):");
            foreach (string path in paths)
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || AssetDatabase.IsValidFolder(path) || inbound.ContainsKey(path)) continue;
                if (path.Contains("/Resources/") || path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".unity", StringComparison.Ordinal)) continue;
                report.AppendLine("  " + path);
            }
            report.AppendLine();
            report.AppendLine("Loaded scenes: missing scripts, repeated MonoBehaviour components, light and renderer counts:");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                int lights = 0, renderers = 0, scripts = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject obj = transform.gameObject;
                    lights += obj.GetComponents<Light>().Length;
                    renderers += obj.GetComponents<Renderer>().Length;
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
                    if (missing > 0) report.AppendLine("  ERROR: " + HierarchyPath(transform) + ": " + missing + " missing scripts");
                    var seen = new HashSet<Type>();
                    foreach (MonoBehaviour component in obj.GetComponents<MonoBehaviour>())
                    {
                        if (component == null) continue;
                        scripts++;
                        if (!seen.Add(component.GetType()))
                            report.AppendLine("  REVIEW repeated component: " + HierarchyPath(transform) + " / " + component.GetType().FullName);
                    }
                }
                report.AppendLine("  " + scene.name + ": " + scripts + " scripts, " + lights + " lights, " + renderers + " renderers");
            }
            string output = Path.GetFullPath("Temp/MotorCityAudit/project-audit.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, report.ToString(), new UTF8Encoding(false));
            Debug.Log("Motor City audit saved: " + output);
        }
        catch (OperationCanceledException exception)
        {
            Debug.Log(exception.Message);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static string HierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
