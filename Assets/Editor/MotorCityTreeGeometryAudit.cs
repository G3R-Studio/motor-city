using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MotorCity.World;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Strictly read-only inventory of tree-like city renderers. Matches
    /// the live CityDistantTreeCullingExperiment candidate-selection rules,
    /// while explaining which gates reject the rest. No rendering mutations.
    /// </summary>
    public static class MotorCityTreeGeometryAudit
    {
        private const string CityName = "MotorCity_FCGCity";
        private const string Output = "Temp/MotorCity_TreeGeometryAudit.txt";
        private const float HighDistance = 500f;
        private const float HighPixels = 12f;
        private const float ReferencePixels = 1080f;
        private const float MaxTreeRadius = 12f;
        private const int ExampleLimit = 4;

        private sealed class Entry
        {
            public string Name;
            public string Family;
            public string Path;
            public string Materials;
            public string MeshName;
            public string Reason;
            public long Triangles;
            public long TreeMaterialTriangles;
            public int Submeshes;
            public float Radius;
            public float Distance;
            public float Pixels;
            public bool HasTreeMaterial;
            public bool PureTreeMaterial;
            public bool TreeName;
            public bool CurrentDistanceCandidate;
            public bool PotentialWithoutNameGate;
            public bool Eligible;
            public bool Active;
            public bool StaticBatch;
            public bool HasLod;
        }

        private sealed class Tally
        {
            public int Count;
            public long Triangles;
            public long TreeTriangles;
            public readonly List<string> Examples = new List<string>();

            public void Add(Entry item)
            {
                Count++;
                Triangles += item.Triangles;
                TreeTriangles += item.TreeMaterialTriangles;
                if (Examples.Count < ExampleLimit)
                    Examples.Add(Describe(item));
            }
        }

        [MenuItem("Motor City/Optimization/16 - Diagnose Tree Geometry (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[MotorCity][TreeAudit] Enter Play Mode " +
                    "and wait for MotorCity_FCGCity. No benchmarking needed.");
                return;
            }

            GameObject city = GameObject.Find(CityName);
            if (city == null)
            {
                Debug.LogWarning("[MotorCity][TreeAudit] Runtime city has " +
                    "not loaded. Wait until the player can drive.");
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[MotorCity][TreeAudit] Camera.main is missing.");
                return;
            }

            try
            {
                string results = Inspect(city, camera);
                Directory.CreateDirectory("Temp");
                File.WriteAllText(Output, results, Encoding.UTF8);
                Debug.Log("[MotorCity][TreeAudit] Read-only live city tree " +
                    "diagnostic saved: " + Output +
                    ". Send the TXT file; no assets or renderers changed.");
            }
            catch (Exception error)
            {
                Debug.LogError("[MotorCity][TreeAudit] Diagnostic failed: " +
                    error);
            }
        }

        private static string Inspect(GameObject city, Camera camera)
        {
            MeshRenderer[] renderers =
                city.GetComponentsInChildren<MeshRenderer>(true);
            var inspected = new List<Entry>();
            var reasons = new Dictionary<string, Tally>();
            var observations = new Dictionary<string, Tally>();

            Vector3 position = camera.transform.position;
            float tanHalf = Mathf.Max(.001f, Mathf.Tan(
                Mathf.Max(1f, camera.fieldOfView) * Mathf.Deg2Rad * .5f));

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                bool nameLooksLikeTree = TreeName(renderer.name);
                bool hasTreeMaterial = materials != null &&
                    materials.Any(IsTreesMaterial);

                // Inspect the union: all renderers containing Trees-01 and
                // all tree/palm-named renderers, even with wrong materials.
                if (!nameLooksLikeTree && !hasTreeMaterial)
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter == null ? null : filter.sharedMesh;
                Bounds bounds = renderer.bounds;
                float radius = bounds.extents.magnitude;
                float distance = Mathf.Max(.1f,
                    Vector3.Distance(position, bounds.center) - radius);
                float projectedPixels = radius * ReferencePixels /
                    (tanHalf * distance);
                bool allTreeMaterials =
                    materials != null && materials.Length > 0 &&
                    materials.All(IsTreesMaterial);
                bool inActiveHierarchy =
                    renderer.gameObject.activeInHierarchy;
                bool staticBatch = renderer.isPartOfStaticBatch;
                bool hasLod =
                    renderer.GetComponentInParent<LODGroup>() != null;
                bool hasCollider = renderer.GetComponent<Collider>() != null;
                bool hasBehaviour = renderer.GetComponent<MonoBehaviour>() != null;

                long triangles = 0;
                long treeTriangles = 0;
                if (mesh != null)
                {
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {
                        long tris = (long)mesh.GetIndexCount(sub) / 3;
                        triangles += tris;
                        if (materials != null && sub < materials.Length &&
                            IsTreesMaterial(materials[sub]))
                            treeTriangles += tris;
                    }
                }

                string reason = null;
                // Mirrors CityDistantTreeCullingExperiment.Discover's
                // first failed test, in exactly the same order.
                if (!renderer.enabled)
                    reason = "Renderer.enabled is false";
                else if (!inActiveHierarchy)
                    reason = "GameObject inactive in live hierarchy";
                else if (renderer.forceRenderingOff)
                    reason = "Already forceRenderingOff (another culler?)";
                else if (staticBatch)
                    reason = "Static-batched renderer";
                else if (hasLod)
                    reason = "Existing LODGroup in hierarchy";
                else if (hasCollider)
                    reason = "Collider attached to renderer";
                else if (hasBehaviour)
                    reason = "MonoBehaviour attached to renderer";
                else if (!nameLooksLikeTree && hasTreeMaterial)
                    reason = "Tree material but name is not Tree*/Palm*";
                else if (nameLooksLikeTree && !allTreeMaterials)
                    reason = "Tree/Palm name but missing/mixed Trees-01 material";
                else if (mesh == null)
                    reason = "No MeshFilter/sharedMesh";
                else if (radius < .1f || radius > MaxTreeRadius)
                    reason = "Tree bounds radius outside 0.1..12m";
                else if (triangles <= 0)
                    reason = "Mesh has no triangle indices";

                var item = new Entry
                {
                    Name = renderer.name,
                    Family = Family(renderer.name),
                    Path = HierarchyPath(renderer.transform, city.transform),
                    Materials = MaterialNames(materials),
                    MeshName = mesh == null ? "(none)" : mesh.name,
                    Reason = reason ?? "ELIGIBLE",
                    Triangles = triangles,
                    TreeMaterialTriangles = treeTriangles,
                    Submeshes = mesh == null ? 0 : mesh.subMeshCount,
                    Radius = radius,
                    Distance = distance,
                    Pixels = projectedPixels,
                    HasTreeMaterial = hasTreeMaterial,
                    PureTreeMaterial = allTreeMaterials,
                    TreeName = nameLooksLikeTree,
                    CurrentDistanceCandidate = distance > HighDistance &&
                        projectedPixels < HighPixels,
                    PotentialWithoutNameGate =
                        renderer.enabled && inActiveHierarchy &&
                        !renderer.forceRenderingOff && !staticBatch &&
                        !hasLod && !hasCollider && !hasBehaviour &&
                        allTreeMaterials && mesh != null &&
                        radius >= .1f && radius <= MaxTreeRadius &&
                        triangles > 0,
                    Eligible = reason == null,
                    Active = renderer.enabled && inActiveHierarchy,
                    StaticBatch = staticBatch,
                    HasLod = hasLod
                };
                inspected.Add(item);
                Add(reasons, item.Reason, item);

                // Overlapping flags, independent of first-rejection reason.
                if (item.Active) Add(observations, "Active/enabled", item);
                if (item.TreeName) Add(observations, "Tree/Palm name", item);
                if (item.HasTreeMaterial)
                    Add(observations, "At least one Trees-01 material", item);
                if (item.PureTreeMaterial)
                    Add(observations, "All materials Trees-01", item);
                if (item.StaticBatch) Add(observations, "Static batching", item);
                if (item.HasLod) Add(observations, "LODGroup", item);
                if (item.PotentialWithoutNameGate)
                    Add(observations, "Eligible if name-only gate removed", item);
                if (item.PotentialWithoutNameGate &&
                    item.CurrentDistanceCandidate)
                    Add(observations,
                        "Name-relaxed + beyond 500m + under 12px", item);
                if (item.Eligible && item.CurrentDistanceCandidate)
                    Add(observations,
                        "Current rules + beyond 500m + under 12px", item);
            }

            int eligible = inspected.Count(e => e.Eligible);
            long eligibleTriangles = inspected.Where(e => e.Eligible)
                .Sum(e => e.Triangles);
            int hideable = inspected.Count(e =>
                e.Eligible && e.CurrentDistanceCandidate);
            long hideableTriangles = inspected.Where(e =>
                e.Eligible && e.CurrentDistanceCandidate)
                .Sum(e => e.Triangles);
            var report = new StringBuilder(24000);
            report.AppendLine("MotorCity LIVE tree renderer census (READ-ONLY)");
            report.AppendLine("Source: " + CityName +
                ", Unity Editor Play Mode, Camera.main.");
            report.AppendLine("Camera at " + position.ToString("F2") +
                ", FOV=" + camera.fieldOfView.ToString("F1") +
                ", far=" + camera.farClipPlane.ToString("F0") + "m.");
            report.AppendLine("Tree filter: names Tree*/Palm*, every material " +
                "starts Trees-01, enabled/active, no forced-off, static batching, " +
                "LODGroup, renderer-attached Collider/MonoBehaviour.");
            report.AppendLine("Radius 0.1..12m; High distance >500m and " +
                "projected 1080p size <12 pixels.");
            report.AppendLine("This is geometry referenced by renderers, " +
                "NOT actual GPU-visible triangles. No objects were changed.");
            report.AppendLine();
            report.AppendLine("Total city MeshRenderers: " + renderers.Length);
            report.AppendLine("Tree-like/material-bearing renderers checked: " +
                inspected.Count + " (tree names OR Trees-01 materials)");
            report.AppendLine("Exactly eligible by current runtime rules: " +
                eligible + " renderers, " + eligibleTriangles + " tris");
            report.AppendLine("Expected potentially hidden here (High): " +
                hideable + " renderers, " + hideableTriangles + " tris");
            report.AppendLine("Tree-bearing submesh references (all checked): " +
                inspected.Sum(e => e.TreeMaterialTriangles) + " tris");
            report.AppendLine();

            report.AppendLine("=== FIRST FAILED RULE (exclusive counts) ===");
            foreach (var group in reasons.OrderByDescending(p =>
                p.Value.Triangles))
                AppendTally(report, group.Key, group.Value, true);

            report.AppendLine();
            report.AppendLine("=== INDEPENDENT ATTRIBUTES (counts overlap) ===");
            foreach (var group in observations.OrderByDescending(p =>
                p.Value.Triangles))
                AppendTally(report, group.Key, group.Value, false);

            report.AppendLine();
            report.AppendLine("=== TOP 25 TREE/PALM NAME FAMILIES BY TRIANGLES ===");
            foreach (var group in inspected.GroupBy(e => e.Family)
                .OrderByDescending(g => g.Sum(e => e.Triangles)).Take(25))
            {
                report.AppendLine(group.Count() + " renderers / " +
                    group.Sum(e => e.Triangles) +
                    " tris (tree material tris=" +
                    group.Sum(e => e.TreeMaterialTriangles) +
                    "), " + group.Count(e => e.Eligible) +
                    " eligible; family=" + group.Key);
                report.AppendLine("  e.g. " + Describe(group.First()));
            }

            report.AppendLine();
            report.AppendLine("=== TOP 20 MATERIAL COMBINATIONS BY TRIANGLES ===");
            foreach (var group in inspected.GroupBy(e => e.Materials)
                .OrderByDescending(g => g.Sum(e => e.Triangles)).Take(20))
            {
                report.AppendLine(group.Count() + " renderers / " +
                    group.Sum(e => e.Triangles) + " tris (" +
                    group.Sum(e => e.TreeMaterialTriangles) +
                    " tree material tris); " + group.Key);
                report.AppendLine("  e.g. " + Describe(group.First()));
            }

            report.AppendLine();
            report.AppendLine("=== TOP 25 EXPENSIVE TREE-MATERIAL OBJECTS ===");
            foreach (Entry item in inspected.Where(e => e.HasTreeMaterial)
                .OrderByDescending(e => e.TreeMaterialTriangles).Take(25))
                report.AppendLine(Describe(item));

            report.AppendLine();
            report.AppendLine("=== TOP 25 NEAR-MISS OBJECTS (NAME-ONLY GATE) ===");
            foreach (Entry item in inspected.Where(e =>
                e.PotentialWithoutNameGate && !e.TreeName)
                .OrderByDescending(e => e.Triangles).Take(25))
                report.AppendLine(Describe(item));

            report.AppendLine();
            report.AppendLine("Notes: first failed rule counts partition all " +
                "inspected renderers; independent attributes intentionally " +
                "overlap. 'Name-only gate removed' is NOT a proposed " +
                "optimization. Inspect paths, hierarchy and material " +
                "combinations before relaxing any protection.");
            return report.ToString();
        }

        private static bool IsTreesMaterial(Material material)
        {
            return material != null &&
                material.name.StartsWith(
                    "Trees-01", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TreeName(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                (name.StartsWith("Tree", StringComparison.OrdinalIgnoreCase) ||
                 name.StartsWith("Palm", StringComparison.OrdinalIgnoreCase));
        }

        private static string Family(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "(empty)";
            int split = name.IndexOfAny(new[] { '-', '_', ' ', '(' });
            return split > 0 ? name.Substring(0, split) : name;
        }

        private static string MaterialNames(Material[] materials)
        {
            return materials == null ? "(null)" :
                string.Join("+", materials.Select(m =>
                    m == null ? "(missing)" : m.name));
        }

        private static string HierarchyPath(Transform item, Transform root)
        {
            var nodes = new List<string>(16);
            for (Transform t = item; t != null && nodes.Count < 24;
                 t = t.parent)
            {
                nodes.Add(t.name);
                if (t == root)
                    break;
            }
            nodes.Reverse();
            return string.Join("/", nodes);
        }

        private static string Describe(Entry item)
        {
            return item.TreeMaterialTriangles + " tree material tris / " +
                item.Triangles + " total tris, radius=" +
                item.Radius.ToString("F1") + "m, distance=" +
                item.Distance.ToString("F1") + "m, projected=" +
                item.Pixels.ToString("F1") + "px, submeshes=" +
                item.Submeshes + ", reason=" + item.Reason +
                ", mesh=" + item.MeshName + ", material=" +
                item.Materials + ", path=" + item.Path;
        }

        private static void Add(
            Dictionary<string, Tally> groups, string key, Entry item)
        {
            if (!groups.TryGetValue(key, out Tally tally))
            {
                tally = new Tally();
                groups.Add(key, tally);
            }
            tally.Add(item);
        }

        private static void AppendTally(
            StringBuilder report, string title, Tally tally,
            bool printExamples)
        {
            report.AppendLine(tally.Count + " renderers / " +
                tally.Triangles + " total tris / " +
                tally.TreeTriangles + " tree material tris: " + title);
            if (printExamples)
                foreach (string example in tally.Examples)
                    report.AppendLine("  e.g. " + example);
        }
    }
}
