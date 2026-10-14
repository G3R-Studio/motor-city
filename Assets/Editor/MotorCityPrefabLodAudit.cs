using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Read-only prefab census + offline LOD mesh trial output.
    /// Never changes CityVisual, scene objects, WebGL runtime or materials.
    /// Generated mesh assets are NOT referenced by any runtime code.
    /// </summary>
    public static class MotorCityPrefabLodAudit
    {
        private const string PrefabPath =
            "Assets/Resources/MotorCity/Environment/CityVisual.prefab";
        private const string OutputFolder = "Assets/MotorCityGenerated/LODTrials/Prefab";
        private const string ReportPath = "Temp/MotorCity_PrefabLOD_Audit.txt";
        private const int MaxTrials = 8;
        private const float MinimumSaving = 0.15f;

        private sealed class Candidate
        {
            public Mesh Mesh;
            public string Example;
            public int Count;
            public long ReferencedTriangles;
        }

        [MenuItem("Motor City/Optimization/LOD Trial/3 - Audit Entire City Prefab (Edit Mode)")]
        private static void Audit()
        {
            if (!Ready()) return;
            Dictionary<string, int> rejected;
            int active;
            List<Candidate> candidates = Collect(out rejected, out active);
            var report = new StringBuilder();
            report.AppendLine("Motor City prefab LOD candidate audit (read-only)");
            report.AppendLine("Prefab: " + PrefabPath);
            report.AppendLine("Active/enabled mesh renderers: " + active);
            report.AppendLine("Unique eligible mesh assets: " + candidates.Count);
            report.AppendLine("Total eligible instances: " + candidates.Sum(c => c.Count));
            report.AppendLine("These are references, NOT visible-frame triangles.");
            report.AppendLine();
            report.AppendLine("Rejections by first matching reason:");
            foreach (var reason in rejected.OrderByDescending(p => p.Value))
                report.AppendLine(reason.Key + ": " + reason.Value);
            report.AppendLine();
            report.AppendLine("Top repeated eligible meshes:");
            foreach (Candidate c in candidates.Take(50))
                report.AppendLine(c.Count + " instances / " +
                    c.ReferencedTriangles + " triangle references: " +
                    c.Mesh.name + "; example=" + c.Example);
            Directory.CreateDirectory("Temp");
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log("[MotorCity][LOD Trial] Entire prefab audited. " +
                candidates.Count + " unique safe mesh candidates, " +
                candidates.Sum(c => c.Count) + " instances. Report: " + ReportPath +
                ". No assets changed.");
        }

        [MenuItem("Motor City/Optimization/LOD Trial/4 - Generate Up To 8 Prefab Mesh Trials (Edit Mode)")]
        private static void Bake()
        {
            if (!Ready()) return;
            Dictionary<string, int> rejected;
            int active;
            List<Candidate> candidates = Collect(out rejected, out active);
            if (candidates.Count == 0)
            {
                Debug.LogWarning("[MotorCity][LOD Trial] No eligible prefab mesh assets. " +
                    "Run menu 3 for rejection reasons; nothing changed.");
                return;
            }
            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();
            int created = 0, unchanged = 0;
            foreach (Candidate c in candidates.Take(MaxTrials))
            {
                Mesh reduced = null;
                try
                {
                    reduced = MotorCitySelectedLodTrial.Reduce(c.Mesh);
                    long oldTriangles = c.Mesh.GetIndexCount(0) / 3;
                    long newTriangles = reduced.GetIndexCount(0) / 3;
                    if (newTriangles >= oldTriangles * (1f - MinimumSaving))
                    {
                        unchanged++;
                        UnityEngine.Object.DestroyImmediate(reduced);
                        continue;
                    }
                    string safeName = new string(c.Mesh.name.Select(ch =>
                        char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_')
                        .ToArray());
                    string path = AssetDatabase.GenerateUniqueAssetPath(
                        OutputFolder + "/" + safeName + "_Trial_LOD1.asset");
                    AssetDatabase.CreateAsset(reduced, path);
                    created++;
                    Debug.Log("[MotorCity][LOD Trial] Offline LOD mesh: " +
                        c.Mesh.name + " (instances=" + c.Count + "), " +
                        oldTriangles + " -> " + newTriangles +
                        " triangles. Saved " + path +
                        ". NOT attached to the city or WebGL.");
                    reduced = null;
                }
                catch (Exception error)
                {
                    if (reduced != null && !AssetDatabase.Contains(reduced))
                        UnityEngine.Object.DestroyImmediate(reduced);
                    Debug.LogWarning("[MotorCity][LOD Trial] Could not reduce " +
                        c.Mesh.name + ": " + error.Message);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[MotorCity][LOD Trial] Prefab offline trial finished: " +
                created + " new unreferenced mesh assets, " + unchanged +
                " without sufficient triangle reduction. " +
                "CityVisual and WebGL are unchanged.");
        }

        private static bool Ready()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[MotorCity][LOD Trial] Exit Play Mode.");
                return false;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return true;
            Debug.LogError("[MotorCity][LOD Trial] Missing " + PrefabPath);
            return false;
        }

        private static List<Candidate> Collect(
            out Dictionary<string, int> rejected, out int active)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            rejected = new Dictionary<string, int>();
            var byMesh = new Dictionary<Mesh, Candidate>();
            active = 0;
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r == null || !r.enabled || !ActiveInPrefab(r.transform, root.transform))
                    continue;
                active++;
                string reason = Validate(r, out Mesh mesh);
                if (reason != null)
                {
                    rejected.TryGetValue(reason, out int count);
                    rejected[reason] = count + 1;
                    continue;
                }
                if (!byMesh.TryGetValue(mesh, out Candidate c))
                {
                    c = new Candidate { Mesh = mesh, Example = PathOf(r.transform, root.transform) };
                    byMesh.Add(mesh, c);
                }
                c.Count++;
                c.ReferencedTriangles += (long)mesh.GetIndexCount(0) / 3;
            }
            return byMesh.Values.OrderByDescending(c => c.ReferencedTriangles).ToList();
        }

        private static string Validate(MeshRenderer r, out Mesh mesh)
        {
            MeshFilter filter = r.GetComponent<MeshFilter>();
            mesh = filter == null ? null : filter.sharedMesh;
            if (mesh == null) return "no mesh";
            if (!mesh.isReadable) return "unreadable mesh";
            if (mesh.subMeshCount != 1 || r.sharedMaterials.Length != 1 ||
                r.sharedMaterial == null) return "multiple submeshes/materials";
            if (mesh.vertexCount < 150 || mesh.vertexCount > 18000)
                return "vertex count outside trial range";
            if (mesh.GetTopology(0) != MeshTopology.Triangles)
                return "non-triangle topology";
            if (mesh.normals.Length != mesh.vertexCount ||
                mesh.uv.Length != mesh.vertexCount) return "missing normals/UV0";
            if (mesh.tangents.Length != 0 || mesh.colors.Length != 0 ||
                mesh.uv2.Length != 0 || mesh.blendShapeCount != 0)
                return "tangents/colors/uv2/blendshapes";
            if (r.lightmapIndex >= 0 || r.isPartOfStaticBatch)
                return "lightmap/static batch";
            if (r.GetComponentInParent<LODGroup>() != null)
                return "existing LOD";
            if (r.GetComponentInParent<Rigidbody>() != null ||
                r.GetComponents<MonoBehaviour>().Length != 0)
                return "dynamic/scripted";
            Material material = r.sharedMaterial;
            if (material.renderQueue >= 2450 ||
                material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
                return "transparent/cutout";
            string label = (r.name + " " + material.name).ToLowerInvariant();
            string[] protectedWords = { "glass", "win", "emiss", "night",
                "alpha", "transparent", "road", "bridge", "terrain", "water",
                "car", "truck", "traffic", "bus", "park", "tree", "palm",
                "plant", "fern", "foliage", "light" };
            foreach (string word in protectedWords)
                if (label.Contains(word))
                    return "protected category: " + word;
            Bounds b = mesh.bounds;
            float radius = b.extents.magnitude *
                Mathf.Max(Mathf.Abs(r.transform.lossyScale.x),
                    Mathf.Max(Mathf.Abs(r.transform.lossyScale.y),
                        Mathf.Abs(r.transform.lossyScale.z)));
            if (radius < 7.1f || radius > 150f)
                return "outside architectural radius range";
            return null;
        }

        private static bool ActiveInPrefab(Transform node, Transform root)
        {
            for (Transform t = node; t != null; t = t.parent)
            {
                if (!t.gameObject.activeSelf) return false;
                if (t == root) return true;
            }
            return false;
        }

        private static string PathOf(Transform node, Transform root)
        {
            var names = new List<string>();
            for (Transform t = node; t != null; t = t.parent)
            {
                names.Add(t.name);
                if (t == root) break;
            }
            names.Reverse();
            return string.Join("/", names);
        }
    }
}
