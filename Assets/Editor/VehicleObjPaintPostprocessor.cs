using System;
using System.IO;
using System.Text;
using MotorCity.Gameplay;
using UnityEditor;

namespace MotorCity.EditorTools
{
    public sealed class VehicleObjPaintPostprocessor : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        private void OnPreprocessModel()
        {
            if (!assetPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                return;

            string source = File.ReadAllText(assetPath);
            string normalized = AddObjectGroups(source);
            if (ContainsPaintRole(source))
                ((ModelImporter)assetImporter).preserveHierarchy = true;
            if (source == normalized)
                return;

            // OBJ object declarations alone need not survive as Unity nodes.
            // Explicit groups preserve the artist's paint roles on import.
            File.WriteAllText(assetPath, normalized, new UTF8Encoding(false));
        }

        private static bool ContainsPaintRole(string source)
        {
            foreach (string line in source.Replace("\r\n", "\n").Split('\n'))
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith("o ", StringComparison.Ordinal))
                    continue;
                string name = trimmed.Substring(2).Trim();
                if (VehiclePaintMeshNames.IsBody(name) ||
                    VehiclePaintMeshNames.IsWheelPaint(name))
                    return true;
            }
            return false;
        }

        public static string AddObjectGroups(string source)
        {
            if (!ContainsPaintRole(source))
                return source;
            string newline = source.Contains("\r\n") ? "\r\n" : "\n";
            string[] lines = source.Replace("\r\n", "\n").Split('\n');

            var result = new StringBuilder();
            for (int index = 0; index < lines.Length; index++)
            {
                result.Append(lines[index]);
                if (index < lines.Length - 1)
                    result.Append(newline);
                string trimmed = lines[index].Trim();
                if (!trimmed.StartsWith("o ", StringComparison.Ordinal))
                    continue;

                // Preserve exports that already have explicit grouping.
                bool hasGroup = false;
                for (int next = index + 1; next < lines.Length; next++)
                {
                    string candidate = lines[next].Trim();
                    if (candidate.StartsWith("o ", StringComparison.Ordinal) ||
                        candidate.StartsWith("f ", StringComparison.Ordinal))
                        break;
                    if (candidate.StartsWith("g ", StringComparison.Ordinal))
                    {
                        hasGroup = true;
                        break;
                    }
                }
                if (!hasGroup)
                {
                    if (index == lines.Length - 1)
                        result.Append(newline);
                    result.Append("g ").Append(trimmed.Substring(2).Trim()).Append(newline);
                }
            }
            return result.ToString();
        }
    }
}
