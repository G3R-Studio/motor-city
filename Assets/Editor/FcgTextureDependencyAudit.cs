using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    internal static class FcgTextureDependencyAudit
    {
        private const string Root = "Assets/Fantastic City Generator/";
        private const string City = "Assets/Resources/MotorCity/Environment/CityVisual.prefab";
        private const string ReportPath = "Library/MotorCity-FCG-Texture-Audit.txt";

        [MenuItem("Tools/Motor City/Audit/Scan FCG Texture Dependencies")]
        private static void Scan()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(City) == null)
            {
                Debug.LogError("FCG audit stopped: CityVisual prefab not found.");
                return;
            }

            try
            {
                string[] paths = AssetDatabase.GetAllAssetPaths()
                    .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
                        !AssetDatabase.IsValidFolder(p)).ToArray();
                string[] textures = paths.Where(p =>
                    p.StartsWith(Root, StringComparison.Ordinal) &&
                    AssetDatabase.GetMainAssetTypeAtPath(p) != null &&
                    typeof(Texture).IsAssignableFrom(
                        AssetDatabase.GetMainAssetTypeAtPath(p))).ToArray();
                var candidates = new HashSet<string>(textures, StringComparer.Ordinal);

                // Include every scene, Resources asset, StreamingAssets asset and
                // preloaded asset as a conservative runtime root.
                var roots = new HashSet<string>(paths.Where(p =>
                    p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                    p.Contains("/Resources/") ||
                    p.StartsWith("Assets/StreamingAssets/", StringComparison.Ordinal)),
                    StringComparer.Ordinal);
                roots.Add(City);
                foreach (var asset in PlayerSettings.GetPreloadedAssets())
                {
                    if (asset == null) continue;
                    string path = AssetDatabase.GetAssetPath(asset);
                    if (!string.IsNullOrEmpty(path)) roots.Add(path);
                }
                EditorUtility.DisplayProgressBar("FCG texture audit", "Runtime dependencies", 0.1f);
                var cityDeps = new HashSet<string>(AssetDatabase.GetDependencies(City, true),
                    StringComparer.Ordinal);
                var runtimeDeps = new HashSet<string>(AssetDatabase.GetDependencies(roots.ToArray(), true),
                    StringComparer.Ordinal);
                var inbound = textures.ToDictionary(p => p, p => new List<string>(),
                    StringComparer.Ordinal);

                // Direct references across the entire project, including the
                // original generator, model importers and authoring workbench.
                // A runtime-unused texture is NOT automatically safe to delete.
                for (int i = 0; i < paths.Length; i++)
                {
                    string path = paths[i];
                    if (i % 100 == 0 &&
                        EditorUtility.DisplayCancelableProgressBar("FCG texture audit",
                            path, 0.15f + 0.8f * i / Math.Max(1, paths.Length)))
                        return;
                    foreach (string dep in AssetDatabase.GetDependencies(path, false))
                        if (dep != path && candidates.Contains(dep))
                            inbound[dep].Add(path);
                }

                // Heuristic for dynamic filename references in source code;
                // generated paths and native/binary references still need review.
                var code = new List<string>();
                foreach (string path in paths.Where(p =>
                    p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(".cginc", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase)))
                    if (File.Exists(path)) code.Add(File.ReadAllText(path));

                var report = new StringBuilder();
                report.AppendLine("Motor City FCG texture dependency audit");
                report.AppendLine("Read-only: no assets modified. Runtime roots are conservative, not an exact build manifest.");
                report.AppendLine("ORPHAN_CANDIDATE means manual review required, not safe-to-delete.");
                report.AppendLine("Dynamic paths, importer subassets, platform variants and authoring dependencies can require further validation.");
                int cityCount = 0, otherCount = 0, authoringCount = 0, orphanCount = 0;
                long orphanBytes = 0;
                foreach (string path in textures.OrderBy(p => p, StringComparer.Ordinal))
                {
                    string filename = Path.GetFileName(path);
                    string stem = Path.GetFileNameWithoutExtension(path);
                    bool mentioned = code.Any(c => c.IndexOf(filename, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (stem.Length >= 5 && c.IndexOf(stem, StringComparison.OrdinalIgnoreCase) >= 0));
                    long bytes = new FileInfo(path).Length;
                    string status;
                    if (cityDeps.Contains(path)) { status = "CITY"; cityCount++; }
                    else if (runtimeDeps.Contains(path)) { status = "OTHER_RUNTIME"; otherCount++; }
                    else if (inbound[path].Count > 0 || mentioned) { status = "AUTHORING_OR_REFERENCE"; authoringCount++; }
                    else { status = "ORPHAN_CANDIDATE"; orphanCount++; orphanBytes += bytes; }
                    report.AppendLine(status + "\t" + bytes + "\t" + path);
                    if (mentioned) report.AppendLine("  Possible filename reference in source");
                    foreach (string source in inbound[path]) report.AppendLine("  <- " + source);
                }
                report.AppendLine();
                report.AppendLine("Total: " + textures.Length + "; city: " + cityCount +
                    "; other runtime roots: " + otherCount + "; authoring/references: " + authoringCount +
                    "; orphan candidates: " + orphanCount + " (" + orphanBytes + " bytes)");
                File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(false));
                Debug.Log("FCG texture audit complete: " + ReportPath + " | " +
                    textures.Length + " textures, " + orphanCount + " orphan candidates.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
