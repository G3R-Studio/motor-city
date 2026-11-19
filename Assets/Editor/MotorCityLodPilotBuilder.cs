using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    public static class MotorCityLodPilotBuilder
    {

        [MenuItem("Tools/Motor City/Audit/Generate Expanded Building LOD Pilot")]
        private static void Expand()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var city = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/MotorCity/Environment/CityVisual.prefab");
            if (city == null) { Debug.LogError("CityVisual missing"); return; }
            const string expanded = "Assets/Resources/MotorCityLodExpanded";
            var report = new StringBuilder("MOTOR CITY EXPANDED BUILDING LOD PILOT\n");
            if (!AssetDatabase.IsValidFolder(expanded))
                AssetDatabase.CreateFolder("Assets/Resources", "MotorCityLodExpanded");
            var protectedMeshes = new HashSet<Mesh>(city.GetComponentsInChildren<MeshCollider>(true).Select(c => c.sharedMesh));
            foreach (var skin in city.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                protectedMeshes.Add(skin.sharedMesh);
            var all = city.GetComponentsInChildren<MeshFilter>(true);
            var counts = new Dictionary<Mesh, int>();
            foreach (var f in all)
            {
                var m = f.sharedMesh;
                if (m == null || protectedMeshes.Contains(m) || !m.isReadable ||
                    !AssetDatabase.GetAssetPath(m).StartsWith(Source, StringComparison.Ordinal) ||
                    f.GetComponentInParent<Rigidbody>(true) != null ||
                    f.GetComponentInParent<Animator>(true) != null ||
                    f.GetComponentInParent<LODGroup>(true) != null) continue;
                var renderer = f.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || renderer.sharedMaterials.Any(mat => mat == null || mat.renderQueue >= 2450)) continue;
                counts.TryGetValue(m, out int n);
                counts[m] = n + 1;
            }
            // Match runtime copies by name only when the city has a single source mesh with that name.
            var uniqueNames = new HashSet<string>(all.Select(f => f.sharedMesh).Where(m => m != null)
                .Distinct().GroupBy(m => m.name).Where(g => g.Count() == 1).Select(g => g.Key));
            var ranked = counts.Where(kv => kv.Value >= 2 && uniqueNames.Contains(kv.Key.name) &&
                    kv.Key.name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                    Enumerable.Range(0, kv.Key.subMeshCount).All(i => kv.Key.GetTopology(i) == MeshTopology.Triangles))
                .Select(kv => new { Mesh = kv.Key, Count = kv.Value, Triangles = Enumerable.Range(0, kv.Key.subMeshCount).Sum(i => (long)kv.Key.GetIndexCount(i) / 3) })
                .Where(x => x.Triangles >= 1000)
                .OrderByDescending(x => x.Triangles * x.Count).ThenBy(x => x.Mesh.name, StringComparer.Ordinal)
                .Take(30).ToArray();
            int generated = 0;
            foreach (var item in ranked)
            {
                var source = item.Mesh;
                string path = expanded + "/" + source.name + ".asset";
                Mesh clone = null;
                try
                {
                    if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    { report.AppendLine("EXISTS " + path + " (not overwritten)"); continue; }
                    clone = UnityEngine.Object.Instantiate(source);
                    clone.name = source.name;
                    MeshLodUtility.GenerateMeshLods(clone, 2);
                    if (clone.GetLods(0).Length < 2 || clone.vertexCount != source.vertexCount ||
                        clone.subMeshCount != source.subMeshCount || clone.bounds != source.bounds)
                    { report.AppendLine("SKIP " + source.name + " topology mismatch"); continue; }
                    AssetDatabase.CreateAsset(clone, path);
                    generated++;
                    report.AppendLine($"GENERATED {source.name} instances={item.Count} originalTriangles={item.Triangles} levels={clone.GetLods(0).Length} weightedTriangles={item.Count * item.Triangles}");
                }
                catch (Exception ex) { report.AppendLine("ERROR " + source.name + ": " + ex.Message); }
                finally { if (clone != null && !AssetDatabase.Contains(clone)) UnityEngine.Object.DestroyImmediate(clone); }
            }
            AssetDatabase.SaveAssets();
            report.AppendLine($"Selected={ranked.Length} Generated={generated}");
            File.WriteAllText("Library/MotorCityLodExpanded.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        private const string Source = "Assets/Fantastic City Generator/Buildings/FBX/";
        private const string Folder = "Assets/Resources/MotorCityLodPilot";
        [MenuItem("Tools/Motor City/Audit/Generate Building LOD Pilot")]
        private static void Generate()
        {
            var specs = new (string name, int vertices, int submeshes)[]
            {
                ("BR-003", 13061, 3),
                ("EC-009", 12969, 3),
                ("BR-002", 11586, 3),
                ("QD-14", 11580, 3),
                ("BR-014", 11099, 3),
                ("BR-027", 11048, 3),
                ("BC-P07", 10765, 4),
                ("BR-025", 10037, 3),
                ("BC-053", 9810, 5),
                ("EC-006", 9974, 3)
            };
            var report = new StringBuilder("MOTOR CITY BUILDING LOD PILOT\n");
            int generated = 0;
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Resources", "MotorCityLodPilot");
            foreach (var spec in specs)
            {
                string path = Folder + "/" + spec.name + ".asset";
                try
                {
                    var meshes = AssetDatabase.LoadAllAssetsAtPath(Source + spec.name + ".fbx")
                        .OfType<Mesh>().Where(m => m.name == spec.name &&
                            m.vertexCount == spec.vertices && m.subMeshCount == spec.submeshes &&
                            m.isReadable).ToArray();
                    if (meshes.Length != 1) { report.AppendLine("SKIP " + spec.name + " source mismatch"); continue; }
                    if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    {
                        report.AppendLine("EXISTS " + path + " (not overwritten)");
                        continue;
                    }
                    var original = meshes[0];
                    var clone = UnityEngine.Object.Instantiate(original);
                    clone.name = original.name;
                    try
                    {
                        MeshLodUtility.GenerateMeshLods(clone, 2);
                        if (clone.GetLods(0).Length < 2 || clone.vertexCount != original.vertexCount ||
                            clone.subMeshCount != original.subMeshCount)
                        {
                            report.AppendLine("SKIP " + spec.name + " no usable LOD");
                            continue;
                        }
                        AssetDatabase.CreateAsset(clone, path);
                        generated++;
                        report.AppendLine("GENERATED " + spec.name + " levels=" + clone.GetLods(0).Length);
                    }
                    finally
                    {
                        if (!AssetDatabase.Contains(clone)) UnityEngine.Object.DestroyImmediate(clone);
                    }
                }
                catch (Exception ex) { report.AppendLine("ERROR " + spec.name + ": " + ex.Message); }
            }
            AssetDatabase.SaveAssets();
            report.AppendLine("Generated=" + generated);
            File.WriteAllText("Library/MotorCityLodPilot.txt", report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
