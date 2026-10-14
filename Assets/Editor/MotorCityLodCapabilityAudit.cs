using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>Read-only, nonexclusive capability audit of CityVisual meshes.</summary>
    public static class MotorCityLodCapabilityAudit
    {
        private const string CityPath = "Assets/Resources/MotorCity/Environment/CityVisual.prefab";
        private const string Output = "Temp/MotorCity_LOD_Capabilities.txt";

        private sealed class Row
        {
            public Mesh Mesh;
            public string Name;
            public string Path;
            public string Category;
            public string Signature;
            public int Instances;
            public long Tris;
            public int Vertices;
        }

        [MenuItem("Motor City/Optimization/LOD Trial/5 - Detailed Capability Audit (Edit Mode)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[MotorCity][LOD Audit] Stop Play Mode.");
                return;
            }
            GameObject city = AssetDatabase.LoadAssetAtPath<GameObject>(CityPath);
            if (city == null)
            {
                Debug.LogError("[MotorCity][LOD Audit] Missing " + CityPath);
                return;
            }
            var categories = new Dictionary<string, long[]>(StringComparer.Ordinal);
            var flags = new Dictionary<string, int>(StringComparer.Ordinal);
            var groups = new Dictionary<string, Row>(StringComparer.Ordinal);
            int active = 0, readable = 0, noMesh = 0, singleMaterial = 0;
            int eligibleOriginal = 0, candidateRelaxed = 0;
            long allTris = 0, relaxedTris = 0;
            foreach (MeshRenderer r in city.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r == null || !r.enabled || !IsActive(r.transform, city.transform))
                    continue;
                active++;
                MeshFilter filter = r.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null) { noMesh++; Increment(flags, "missing mesh"); continue; }
                long tris = 0;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    if (mesh.GetTopology(sub) == MeshTopology.Triangles)
                        tris += (long)mesh.GetIndexCount(sub) / 3;
                allTris += tris;
                Material[] materials = r.sharedMaterials;
                string cat = Classify(r, materials);
                if (!categories.TryGetValue(cat, out long[] totals))
                    categories[cat] = totals = new long[2];
                totals[0]++;
                totals[1] += tris;

                bool meshReadable = mesh.isReadable;
                bool oneMat = mesh.subMeshCount == 1 && materials.Length == 1 && materials[0] != null;
                if (meshReadable) readable++;
                if (oneMat) singleMaterial++;
                if (!meshReadable) Increment(flags, "unreadable");
                if (!oneMat) Increment(flags, "multi-submesh/material");
                if (mesh.vertexCount < 150) Increment(flags, "vertices below 150");
                if (mesh.vertexCount > 18000) Increment(flags, "vertices above 18000");
                if (mesh.blendShapeCount > 0) Increment(flags, "blend shapes");
                if (meshReadable)
                {
                    if (mesh.tangents.Length > 0) Increment(flags, "tangents present");
                    if (mesh.colors32.Length > 0) Increment(flags, "vertex colors present");
                    if (mesh.uv2.Length > 0) Increment(flags, "UV2 present");
                    if (mesh.normals.Length != mesh.vertexCount) Increment(flags, "normals missing");
                    if (mesh.uv.Length != mesh.vertexCount) Increment(flags, "UV0 missing");
                }
                if (r.lightmapIndex >= 0) Increment(flags, "lightmapped renderer");
                if (r.isPartOfStaticBatch) Increment(flags, "static batch");
                if (r.GetComponentInParent<LODGroup>() != null) Increment(flags, "already LOD");
                if (r.GetComponentInParent<Rigidbody>() != null) Increment(flags, "Rigidbody hierarchy");
                if (r.GetComponents<MonoBehaviour>().Length > 0) Increment(flags, "scripted renderer object");
                bool protectedMaterial = materials.Length == 0;
                foreach (Material m in materials)
                    if (m == null || m.renderQueue >= 2450 ||
                        Protected(m.name) || m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f)
                        protectedMaterial = true;
                if (protectedMaterial) Increment(flags, "protected/alpha materials");

                // Reproduce important original audit gates, for comparison only.
                bool basic = meshReadable && oneMat && mesh.vertexCount >= 150 &&
                             mesh.vertexCount <= 18000 && mesh.blendShapeCount == 0 &&
                             r.GetComponentInParent<LODGroup>() == null && !r.isPartOfStaticBatch &&
                             r.lightmapIndex < 0 && r.GetComponentInParent<Rigidbody>() == null &&
                             r.GetComponents<MonoBehaviour>().Length == 0 && !protectedMaterial &&
                             !Protected(r.name);
                bool uvNormal = meshReadable && mesh.uv.Length == mesh.vertexCount &&
                                mesh.normals.Length == mesh.vertexCount;
                bool topology = mesh.subMeshCount > 0;
                for (int i = 0; i < mesh.subMeshCount; i++)
                    if (mesh.GetTopology(i) != MeshTopology.Triangles) topology = false;
                if (basic && uvNormal && topology &&
                    mesh.tangents.Length == 0 && mesh.colors32.Length == 0 &&
                    mesh.uv2.Length == 0) eligibleOriginal++;
                // Exploration only: tangent + UV2 retention could be implemented later.
                bool relaxed = basic && uvNormal && topology && mesh.vertexCount >= 300;
                if (relaxed)
                {
                    candidateRelaxed++;
                    relaxedTris += tris;
                }
                // Group by actual mesh AND material signature to avoid merging
                // visually incompatible instances that share a mesh asset.
                string matKey = string.Join("|", materials.Select(m =>
                    m == null ? "<null>" : m.GetEntityId().ToString()));
                string key = mesh.GetEntityId() + ":" + matKey;
                if (!groups.TryGetValue(key, out Row row))
                {
                    row = new Row { Mesh = mesh, Name = mesh.name,
                        Path = BuildPath(r.transform, city.transform),
                        Category = cat, Signature = matKey, Vertices = mesh.vertexCount };
                    groups.Add(key, row);
                }
                row.Instances++;
                row.Tris += tris;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Motor City detailed LOD capabilities - READ ONLY");
            sb.AppendLine("These are active prefab triangle references, not visible camera triangles.");
            sb.AppendLine("Total active renderers: " + active);
            sb.AppendLine("Missing meshes: " + noMesh);
            sb.AppendLine("Readable renderers: " + readable);
            sb.AppendLine("Single material/submesh: " + singleMaterial);
            sb.AppendLine("Triangle references: " + allTris);
            sb.AppendLine("Previous restrictive gate approximation: " + eligibleOriginal);
            sb.AppendLine("Relaxed diagnostic ONLY (tangents/colors/UV2 allowed): " +
                candidateRelaxed + " renderers / " + relaxedTris + " triangle references");
            sb.AppendLine("Relaxed count DOES NOT authorize automatic mesh generation.");
            sb.AppendLine();
            sb.AppendLine("NONEXCLUSIVE FLAGS (each renderer may count multiple times):");
            foreach (var entry in flags.OrderByDescending(x => x.Value))
                sb.AppendLine(entry.Key + ": " + entry.Value);
            sb.AppendLine();
            sb.AppendLine("CATEGORY / RENDERERS / TRIANGLE REFERENCES:");
            foreach (var entry in categories.OrderByDescending(x => x.Value[1]))
                sb.AppendLine(entry.Key + ": " + entry.Value[0] + " / " + entry.Value[1]);
            sb.AppendLine();
            sb.AppendLine("TOP 60 REPEATED MESH+MATERIAL COMBINATIONS:");
            foreach (Row row in groups.OrderByDescending(x => x.Value.Tris).Take(60).Select(x => x.Value))
                sb.AppendLine(row.Category + " | " + row.Instances + " instances | " +
                    row.Tris + " tris | " + row.Vertices + " verts | " +
                    row.Name + " | " + row.Path);
            sb.AppendLine();
            sb.AppendLine("TOP 30 MOST REPEATED MESH+MATERIAL COMBINATIONS:");
            foreach (Row row in groups.OrderByDescending(x => x.Value.Instances).Take(30).Select(x => x.Value))
                sb.AppendLine(row.Category + " | " + row.Instances + " instances | " +
                    row.Tris + " tris | " + row.Name + " | " + row.Path);
            Directory.CreateDirectory("Temp");
            File.WriteAllText(Output, sb.ToString(), Encoding.UTF8);
            Debug.Log("[MotorCity][LOD Audit] Saved " + Output +
                " (" + active + " active renderers). Read-only; no prefab/assets changed.");
        }

        private static void Increment(Dictionary<string, int> flags, string key)
        {
            flags.TryGetValue(key, out int count);
            flags[key] = count + 1;
        }

        private static bool IsActive(Transform t, Transform root)
        {
            for (; t != null; t = t.parent)
            {
                if (!t.gameObject.activeSelf) return false;
                if (t == root) return true;
            }
            return false;
        }

        private static bool Protected(string name)
        {
            string x = name.ToLowerInvariant();
            string[] words = { "glass", "win", "emiss", "night", "alpha",
                "transparent", "road", "bridge", "water", "terrain",
                "traffic", "car", "truck", "bus", "tree", "palm",
                "park", "plant", "fern", "light" };
            return words.Any(x.Contains);
        }

        private static string Classify(MeshRenderer renderer, Material[] mats)
        {
            string name = renderer.name.ToLowerInvariant();
            string path = BuildPath(renderer.transform, null).ToLowerInvariant();
            string materials = string.Join(" ", mats.Select(m => m == null ? "" : m.name)).ToLowerInvariant();
            if (path.Contains("carcontainer") || path.Contains("traffic") ||
                materials.Contains("tire") || materials.Contains("traffic-car"))
                return "vehicles and parts";
            if (name.Contains("tree") || name.Contains("palm") || name.Contains("fern") ||
                name.Contains("vegetation") || materials.Contains("trees-01"))
                return "vegetation";
            if (path.Contains("building") || name.StartsWith("bb-") ||
                name.StartsWith("bc-") || name.StartsWith("br-") ||
                name.StartsWith("ec-"))
                return "buildings and attached decoration";
            if (path.Contains("road") || name.Contains("road") ||
                name.Contains("bridge")) return "roads / bridges";
            return "other city objects";
        }

        private static string BuildPath(Transform t, Transform stop)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent)
            {
                parts.Add(t.name);
                if (t == stop) break;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
