using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    public static class MotorCityFontAudit
    {
        private const string Root = "Assets/Resources/MotorCity/Fonts/";
        [MenuItem("Tools/Motor City/Audit/Font Dependencies")]
        private static void Scan()
        {
            var paths = AssetDatabase.GetAllAssetPaths();
            var fonts = paths.Where(p => p.StartsWith(Root, StringComparison.Ordinal) &&
                (p.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                 p.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))).OrderBy(p => p).ToArray();
            var users = fonts.ToDictionary(p => p, p => new List<string>(), StringComparer.Ordinal);
            var report = new StringBuilder("MOTOR CITY FONT DEPENDENCY AUDIT\n");
            report.AppendLine("Read-only. Static references do not prove absence of dynamic Resources loads.");
            try
            {
                var candidates = paths.Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
                    !AssetDatabase.IsValidFolder(p) && !users.ContainsKey(p)).ToArray();
                for (int i = 0; i < candidates.Length; i++)
                {
                    string root = candidates[i];
                    if (i % 100 == 0 && EditorUtility.DisplayCancelableProgressBar(
                        "Font dependency audit", root, (float)i / candidates.Length))
                        throw new OperationCanceledException("Audit cancelled; results incomplete.");
                    foreach (string dependency in AssetDatabase.GetDependencies(root, false))
                        if (users.TryGetValue(dependency, out var references))
                            references.Add(root);
                }
                foreach (string font in fonts)
                {
                    report.AppendLine("\n" + font);
                    report.AppendLine("GUID: " + AssetDatabase.AssetPathToGUID(font));
                    report.AppendLine("Source bytes: " + new FileInfo(font).Length);
                    report.AppendLine("Direct asset references: " + users[font].Count);
                    foreach (string user in users[font].OrderBy(x => x))
                        report.AppendLine("  " + user);
                    report.AppendLine("Text references in scripts and text assets (name/GUID; heuristic):");
                    string name = Path.GetFileNameWithoutExtension(font);
                    string guid = AssetDatabase.AssetPathToGUID(font);
                    foreach (string file in candidates.Where(p =>
                        p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                        p.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                        p.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!File.Exists(file)) continue;
                        string content = File.ReadAllText(file);
                        if (content.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            content.IndexOf(guid, StringComparison.OrdinalIgnoreCase) >= 0)
                            report.AppendLine("  " + file);
                    }
                }
                report.AppendLine("\nCOMPLETE. Review dynamic paths and language coverage before moving any font.");
            }
            catch (Exception ex)
            {
                report.AppendLine("\nINCOMPLETE: " + ex.Message);
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Directory.CreateDirectory("Library");
                File.WriteAllText("Library/MotorCityFontAudit.txt", report.ToString(), Encoding.UTF8);
                Debug.Log("Font audit report: Library/MotorCityFontAudit.txt");
            }
        }
    }
}
