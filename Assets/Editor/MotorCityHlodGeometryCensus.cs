using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>Read-only inspection of authored mesh costs and hierarchy.</summary>
    public static class MotorCityHlodGeometryCensus
    {
        private const string CityPath =
            "Assets/Resources/MotorCity/Environment/CityVisual.prefab";
        private const string Output = "Temp/MotorCity_HLOD_GeometryCensus.txt";

        private sealed class Item
        {
            public string Path;
            public string Mesh;
            public string Material;
            public string Family;
            public long Triangles;
            public int Vertices;
            public int Submeshes;
            public float Radius;
            public bool Opaque;
            public bool Readable;
        }

        [MenuItem("Motor City/Optimization/HLOD/0 - City Geometry Census (Edit Mode)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[MotorCity][HLOD] Stop Play Mode first.");
                return;
            }

            GameObject city = AssetDatabase.LoadAssetAtPath<GameObject>(CityPath);
            if (city == null)
            {
                Debug.LogError("[MotorCity][HLOD] Missing CityVisual.prefab.");
                return;
            }

            MeshRenderer[] renderers =
                city.GetComponentsInChildren<MeshRenderer>(true);
            var entries = new List<Item>(renderers.Length);
            int active = 0;
            int missing = 0;
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null)
                    continue;
                if (!renderer.enabled ||
                    !IsActiveInPrefab(renderer.transform, city.transform))
                    continue;
                active++;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter == null ? null : filter.sharedMesh;
                if (mesh == null)
                {
                    missing++;
                    continue;
                }

                Material[] mats = renderer.sharedMaterials;
                bool opaque = mats != null && mats.Length > 0 &&
                    mats.Length == mesh.subMeshCount &&
                    mats.All(m => m != null && m.renderQueue < 2450 &&
                        !ProtectedMaterial(m.name));
                long tris = 0;
                for (int i = 0; i < mesh.subMeshCount; i++)
                    tris += (long)mesh.GetIndexCount(i) / 3;
                Bounds worldBounds = TransformBounds(mesh.bounds,
                    renderer.transform.localToWorldMatrix);
                string name = renderer.name;
                int split = name.IndexOfAny(new[] { '-', '_', ' ', '(' });
                string family = split > 0 ? name.Substring(0, split) : name;

                entries.Add(new Item
                {
                    Path = RelativePath(renderer.transform, city.transform),
                    Mesh = mesh.name,
                    Material = mats == null ? "(none)" :
                        string.Join("+", mats.Select(m =>
                            m == null ? "(null)" : m.name)),
                    Family = family,
                    Triangles = tris,
                    Vertices = mesh.vertexCount,
                    Submeshes = mesh.subMeshCount,
                    Radius = worldBounds.extents.magnitude,
                    Opaque = opaque,
                    Readable = mesh.isReadable
                });
            }

            var report = new StringBuilder(16000);
            report.AppendLine("MotorCity CityVisual geometry census (read-only)");
            report.AppendLine("Renderers: " + renderers.Length +
                " total; " + active + " active/enabled; " +
                missing + " active missing mesh.");
            report.AppendLine("Active triangle references: " +
                entries.Sum(e => e.Triangles).ToString("N0") +
                " (not camera-visible triangles)");
            report.AppendLine("Active opaque candidates: " +
                entries.Count(e => e.Opaque) +
                "; opaque + readable: " +
                entries.Count(e => e.Opaque && e.Readable));
            report.AppendLine("Active multi-submesh: " +
                entries.Count(e => e.Submeshes > 1));
            report.AppendLine();

            report.AppendLine("== Radius bins of active meshes ==");
            float[] bounds = { 1f, 3f, 7f, 15f, 35f, 65f, 150f, float.PositiveInfinity };
            float previous = 0f;
            foreach (float limit in bounds)
            {
                var group = entries.Where(e => e.Radius >= previous &&
                    e.Radius < limit).ToList();
                report.AppendLine(previous + " .. " + limit + "m: " +
                    group.Count + " renderers / " +
                    group.Sum(e => e.Triangles).ToString("N0") + " tris");
                previous = limit;
            }
            report.AppendLine();

            report.AppendLine("== Top 30 expensive active renderers ==");
            foreach (Item e in entries.OrderByDescending(e => e.Triangles).Take(30))
                report.AppendLine(Describe(e));
            report.AppendLine();

            report.AppendLine("== Top 20 largest opaque, readable meshes ==");
            foreach (Item e in entries.Where(e => e.Opaque && e.Readable)
                         .OrderByDescending(e => e.Triangles).Take(20))
                report.AppendLine(Describe(e));
            report.AppendLine();

            report.AppendLine("== Top 25 material combinations by triangle references ==");
            foreach (var g in entries.GroupBy(e => e.Material)
                         .OrderByDescending(g => g.Sum(e => e.Triangles)).Take(25))
            {
                report.AppendLine(g.Count() + " renderers / " +
                    g.Sum(e => e.Triangles).ToString("N0") +
                    " triangles: " + g.Key +
                    "; example=" + g.First().Path);
            }
            report.AppendLine();

            report.AppendLine("== Top 25 object name families ==");
            foreach (var g in entries.GroupBy(e => e.Family)
                         .OrderByDescending(g => g.Sum(e => e.Triangles)).Take(25))
            {
                report.AppendLine(g.Count() + " renderers / " +
                    g.Sum(e => e.Triangles).ToString("N0") +
                    " triangles: " + g.Key +
                    "; example=" + g.First().Path);
            }

            Directory.CreateDirectory("Temp");
            File.WriteAllText(Output, report.ToString(), Encoding.UTF8);
            Debug.Log("[MotorCity][HLOD] Read-only city census ready: " +
                Output + ". Active " + active + "/" + renderers.Length +
                ", triangle references=" + entries.Sum(e => e.Triangles) +
                ". Send the report to identify safe HLOD candidates. " +
                "No Unity assets or scene objects were changed.");
        }

        private static bool IsActiveInPrefab(Transform t, Transform root)
        {
            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    return false;
                if (t == root)
                    return true;
                t = t.parent;
            }
            return false;
        }

        private static bool ProtectedMaterial(string name)
        {
            string[] tokens = { "glass", "winglass", "wins", "window",
                "emiss", "night", "light", "alpha", "transparent", "leaf" };
            return tokens.Any(t => name.IndexOf(t,
                StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
        {
            Vector3 center = matrix.MultiplyPoint3x4(local.center);
            Vector3 x = matrix.MultiplyVector(new Vector3(local.extents.x, 0, 0));
            Vector3 y = matrix.MultiplyVector(new Vector3(0, local.extents.y, 0));
            Vector3 z = matrix.MultiplyVector(new Vector3(0, 0, local.extents.z));
            Vector3 e = new Vector3(
                Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            return new Bounds(center, 2f * e);
        }

        private static string RelativePath(Transform t, Transform root)
        {
            var pieces = new List<string>();
            while (t != null && t != root && pieces.Count < 12)
            {
                pieces.Add(t.name);
                t = t.parent;
            }
            pieces.Reverse();
            return string.Join("/", pieces);
        }

        private static string Describe(Item e)
        {
            return e.Triangles.ToString("N0") + " tris, " +
                e.Vertices.ToString("N0") + " verts, radius " +
                e.Radius.ToString("F1") + "m, " + e.Submeshes +
                " submeshes, " + (e.Opaque ? "opaque" : "protected material") +
                ", " + (e.Readable ? "readable" : "unreadable") +
                ", mesh=" + e.Mesh + ", material=" + e.Material +
                ", path=" + e.Path;
        }
    }
}
