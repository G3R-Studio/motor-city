using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MotorCity.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Offline HLOD bake from the imported CityVisual prefab. Does not rewrite
    /// the source prefab, scene objects, renderers, physics, or materials.
    /// Produces one Resources asset with subasset proxy meshes.
    /// </summary>
    public static class MotorCityHlodBaker
    {
        private const string CityPath =
            "Assets/Resources/MotorCity/Environment/CityVisual.prefab";
        private const string Output =
            "Assets/Resources/MotorCity/Environment/HLOD/CityHlodData.asset";
        private const int CellSize = 120;
        private const int MinimumRenderers = 3;
        private const int MaximumVerticesPerCell = 45000;
        private const int MaximumSourceVertices = 18000;
        private const int MaximumSubmeshesPerRenderer = 8;
        private const int MaximumMaterialsPerProxy = 8;
        // Cap additional geometry so an unreviewed bake cannot double
        // WebGL memory usage across the entire authored city.
        private const int MaximumProxyCells = 320;
        private const int MaximumProxyTriangles = 1000000;

        private readonly struct Source
        {
            public readonly MeshRenderer Renderer;
            public readonly Mesh Mesh;
            public readonly int Index;
            public readonly int Triangles;
            public Source(MeshRenderer r, Mesh mesh, int index, int tris)
            {
                Renderer = r; Mesh = mesh; Index = index; Triangles = tris;
            }
        }

        private sealed class Batch
        {
            public readonly List<Source> Sources = new List<Source>();
        }

        // Conservative scope: only opaque, sizable architecture with no
        // attached gameplay MonoBehaviour or existing LOD; never vegetation,
        // vehicles, roads, glass/night windows, or small decorative objects.
        private static readonly string[] ExcludedNames =
        {
            "road", "street", "lane", "sidewalk", "curb", "asphalt",
            "ground", "terrain", "water", "river", "bridge", "rail",
            "car", "vehicle", "traffic", "truck", "bus", "garage",
            "park", "tree", "plant", "grass", "fern", "flower",
            "bush", "foliage", "vegetation", "fence", "sign",
            "light", "lamp", "signal", "pole", "wire", "vfx",
            "particle", "marker", "checkpoint", "beacon",
            "decal", "smoke", "effect", "billboard", "pedestrian"
        };
        private static readonly string[] ExcludedMaterials =
        {
            "glass", "winglass", "wins", "window", "emiss",
            "night", "light", "alpha", "transparent", "leaf"
        };

        [MenuItem("Motor City/Optimization/HLOD/1 - Audit Candidates (Edit Mode)")]
        public static void Audit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[MotorCity][HLOD] Audit in Edit Mode.");
                return;
            }

            MeshRenderer[] all;
            string diagnostic;
            var groups = Collect(out all, out diagnostic);
            int eligible = groups.Values.Sum(g => g.Sources.Count);
            int bakeGroups = groups.Values.Sum(g => Partition(g.Sources).Count());
            int maxGroup = groups.Count == 0 ? 0 : groups.Values.Max(g => g.Sources.Count);
            List<List<Source>> planned = groups.Values
                .SelectMany(g => Partition(g.Sources)).ToList();
            int sourceSlots = planned.Sum(chunk =>
                chunk.Sum(item => item.Mesh.subMeshCount));
            int proxySlots = planned.Sum(chunk =>
                chunk.SelectMany(item => item.Renderer.sharedMaterials)
                    .Distinct().Count());
            string samples = string.Join(", ", groups.Values
                .SelectMany(g => g.Sources)
                .Take(6).Select(item => item.Renderer.name));
            int triangles = groups.Values.Sum(g => g.Sources.Sum(s => s.Triangles));
            Debug.Log("[MotorCity][HLOD] Candidate audit: " + eligible +
                "/" + all.Length + " eligible architecture MeshRenderers, " +
                bakeGroups + " bakeable 120m proxy chunks with >=" +
                MinimumRenderers + " renderers (largest sector=" + maxGroup +
                "), " + triangles +
                " eligible triangle references (not GPU-visible triangles). " +
                "Potential material draw slots: " + sourceSlots +
                " original -> " + proxySlots +
                " proxy (not actual GPU draw calls). Examples: [" +
                samples + "]. No assets changed.\n" + diagnostic);
        }

        [MenuItem("Motor City/Optimization/HLOD/2 - Bake Proxies (Edit Mode)")]
        public static void Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[MotorCity][HLOD] Stop Play Mode before baking.");
                return;
            }

            MeshRenderer[] all;
            string diagnostic;
            var groups = Collect(out all, out diagnostic);
            if (groups.Count == 0)
            {
                Debug.LogWarning("[MotorCity][HLOD] Nothing can be baked. " +
                    "Check the rejection breakdown below:\n" + diagnostic);
                return;
            }
            var cells = new List<CityHlodCell>();
            var meshes = new List<Mesh>();
            int sourceTris = 0, proxyTris = 0;
            int failed = 0;
            try
            {
                foreach (var entry in groups.OrderBy(p => p.Key))
                {
                    if (cells.Count >= MaximumProxyCells ||
                        proxyTris >= MaximumProxyTriangles)
                        break;

                    List<Source> sources = entry.Value.Sources;
                    if (sources.Count < MinimumRenderers)
                        continue;

                    foreach (List<Source> chunk in Partition(sources))
                    {
                        if (cells.Count >= MaximumProxyCells ||
                            proxyTris >= MaximumProxyTriangles)
                            break;

                        try
                        {
                            Material[] proxyMaterials;
                            Mesh combined = CombineChunk(
                                chunk, cells.Count, out proxyMaterials);

                            // Preserve every material/UV seam when multiple
                            // materials share one proxy mesh. For a single
                            // material the old conservative simplifier applies.
                            Mesh simplified = proxyMaterials.Length == 1
                                ? SimplifyConservatively(combined)
                                : combined;
                            if (simplified != combined)
                                UnityEngine.Object.DestroyImmediate(combined);

                            int sourceCount = chunk.Sum(s => s.Triangles);
                            int proxyCount = 0;
                            for (int sub = 0; sub < simplified.subMeshCount; sub++)
                                proxyCount += (int)(simplified.GetIndexCount(sub) / 3);
                            if (proxyCount == 0)
                            {
                                UnityEngine.Object.DestroyImmediate(simplified);
                                continue;
                            }

                            Bounds bounds = simplified.bounds;
                            cells.Add(new CityHlodCell
                            {
                                Center = bounds.center,
                                ProxyMesh = simplified,
                                ProxyMaterials = proxyMaterials,
                                SourceRendererIndices =
                                    chunk.Select(s => s.Index).ToArray(),
                                SourceRendererNames =
                                    chunk.Select(s => s.Renderer.name).ToArray(),
                                SourceRendererCenters =
                                    chunk.Select(s => s.Renderer.bounds.center).ToArray(),
                                SourceTriangles = sourceCount,
                                ProxyTriangles = proxyCount
                            });
                            sourceTris += sourceCount;
                            proxyTris += proxyCount;
                            meshes.Add(simplified);
                        }
                        catch (Exception error)
                        {
                            failed++;
                            Debug.LogWarning("[MotorCity][HLOD] Skipped one " +
                                "material cell: " + error.Message);
                        }
                    }
                }

                if (cells.Count == 0)
                {
                    Debug.LogWarning("[MotorCity][HLOD] No safe proxy cells " +
                        "produced. City prefab remains unchanged; use Audit " +
                        "to inspect candidate count.");
                    return;
                }

                // Generate everything in memory before replacing the previous
                // bake so a failed scan does not damage existing data.
                Directory.CreateDirectory(Path.GetDirectoryName(Output));
                AssetDatabase.Refresh();
                CityHlodData data = ScriptableObject.CreateInstance<CityHlodData>();
                data.SourceRendererCount = all.Length;
                data.SourceTriangles = sourceTris;
                data.ProxyTriangles = proxyTris;
                data.Cells = cells.ToArray();

                if (AssetDatabase.LoadAssetAtPath<CityHlodData>(Output) != null)
                    AssetDatabase.DeleteAsset(Output);
                AssetDatabase.CreateAsset(data, Output);
                foreach (Mesh mesh in meshes)
                    AssetDatabase.AddObjectToAsset(mesh, data);

                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceUpdate);
                Debug.Log("[MotorCity][HLOD] Saved " + cells.Count +
                    " proxy cells (" + sourceTris + " original => " +
                    proxyTris + " proxy triangle references). " +
                    failed + " batches skipped. Original city prefab untouched. " +
                    "Enter Play Mode and use HLOD A/B controls.");
            }
            finally
            {
                // Meshes saved as subassets must survive. Unsaved, temporary
                // proxy meshes are cleaned up if generation produced no asset.
                if (AssetDatabase.LoadAssetAtPath<CityHlodData>(Output) == null)
                    foreach (Mesh mesh in meshes)
                        if (mesh != null)
                            UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static Dictionary<string, Batch> Collect(
            out MeshRenderer[] all, out string diagnostic)
        {
            GameObject city = AssetDatabase.LoadAssetAtPath<GameObject>(CityPath);
            if (city == null)
                throw new InvalidOperationException("Missing city prefab: " + CityPath);

            // An imported prefab asset is NOT a scene instance. Its children
            // often return activeInHierarchy=false even though their local
            // activeSelf flags are all true. The old check rejected the
            // whole prefab (0/25634), before looking at any actual meshes.
            all = city.GetComponentsInChildren<MeshRenderer>(true);
            var batches = new Dictionary<string, Batch>();
            var rejected = new Dictionary<string, int>();
            var examples = new Dictionary<string, string>();

            void Reject(string reason, MeshRenderer renderer)
            {
                rejected.TryGetValue(reason, out int count);
                rejected[reason] = count + 1;
                if (!examples.ContainsKey(reason))
                {
                    string material = renderer.sharedMaterial != null
                        ? renderer.sharedMaterial.name : "(no material)";
                    examples.Add(reason, renderer.name + " [" + material + "]");
                }
            }

            for (int i = 0; i < all.Length; i++)
            {
                MeshRenderer renderer = all[i];
                if (renderer == null)
                    continue;

                if (!renderer.enabled)
                {
                    Reject("Renderer disabled", renderer);
                    continue;
                }

                if (!ActiveInAuthoredPrefab(renderer.transform, city.transform))
                {
                    Reject("Disabled in prefab hierarchy", renderer);
                    continue;
                }

                if (renderer.GetComponentInParent<LODGroup>() != null)
                {
                    Reject("Has existing LODGroup", renderer);
                    continue;
                }

                if (renderer.isPartOfStaticBatch)
                {
                    Reject("Already static batched", renderer);
                    continue;
                }

                if (renderer.HasPropertyBlock())
                {
                    Reject("Custom renderer property block", renderer);
                    continue;
                }

                if (renderer.GetComponent<MonoBehaviour>() != null)
                {
                    Reject("Gameplay MonoBehaviour attached", renderer);
                    continue;
                }

                // Imported prefabs can use 65534/65535 for unmapped lightmaps.
                // Do not treat those sentinel values as baked lightmapping.
                if (renderer.lightmapIndex >= 0 &&
                    renderer.lightmapIndex < 65534)
                {
                    Reject("Has baked lightmap", renderer);
                    continue;
                }

                if (IsExcluded(renderer.gameObject.name, ExcludedNames))
                {
                    Reject("Protected object name", renderer);
                    continue;
                }

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter == null ? null : filter.sharedMesh;
                if (mesh == null)
                {
                    Reject("Missing MeshFilter or mesh", renderer);
                    continue;
                }

                if (!mesh.isReadable)
                {
                    Reject("Source mesh is not Read/Write enabled", renderer);
                    continue;
                }

                if (mesh.vertexCount > MaximumSourceVertices)
                {
                    Reject("Source mesh exceeds vertex limit", renderer);
                    continue;
                }

                if (mesh.subMeshCount < 1 ||
                    mesh.subMeshCount > MaximumSubmeshesPerRenderer)
                {
                    Reject("Unsupported source submesh count", renderer);
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null ||
                    materials.Length != mesh.subMeshCount)
                {
                    Reject("Submesh/material slot count mismatch", renderer);
                    continue;
                }

                bool missingMaterial = false;
                bool forbiddenMaterial = false;
                foreach (Material material in materials)
                {
                    if (material == null)
                    {
                        missingMaterial = true;
                        break;
                    }
                    if (material.renderQueue >= 2450 ||
                        IsExcluded(material.name, ExcludedMaterials))
                    {
                        forbiddenMaterial = true;
                        break;
                    }
                }
                if (missingMaterial)
                {
                    Reject("Missing source submesh material", renderer);
                    continue;
                }
                if (forbiddenMaterial)
                {
                    Reject("Transparent or protected submesh", renderer);
                    continue;
                }

                Bounds bounds = renderer.bounds;
                float radius = bounds.extents.magnitude;
                if (radius < 7.1f || radius > 65f ||
                    bounds.size.y < 3.5f || bounds.size.x > 120f ||
                    bounds.size.z > 120f)
                {
                    Reject("Outside architecture size limits", renderer);
                    continue;
                }

                Transform parent = renderer.transform.parent;
                int depth = 0;
                bool protectedAncestor = false;
                while (parent != null && parent != city.transform &&
                       depth++ < 3)
                {
                    if (IsExcluded(parent.name, ExcludedNames))
                    {
                        protectedAncestor = true;
                        break;
                    }
                    parent = parent.parent;
                }
                if (protectedAncestor)
                {
                    Reject("Protected parent name", renderer);
                    continue;
                }

                int x = Mathf.FloorToInt(bounds.center.x / CellSize);
                int z = Mathf.FloorToInt(bounds.center.z / CellSize);
                string key = x + ":" + z;
                if (!batches.TryGetValue(key, out Batch batch))
                {
                    batch = new Batch();
                    batches.Add(key, batch);
                }

                int triangles = 0;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    triangles += (int)(mesh.GetIndexCount(sub) / 3);
                batch.Sources.Add(new Source(renderer, mesh, i, triangles));
            }

            // Each object gets exactly one dominant rejection reason, so
            // the reason counts plus eligible count equal all renderers.
            int eligible = batches.Values.Sum(group => group.Sources.Count);
            var report = new System.Text.StringBuilder();
            report.Append("HLOD filter diagnostic: ");
            report.Append(eligible).Append(" eligible / ")
                .Append(all.Length).Append(" inspected");
            foreach (var pair in rejected.OrderByDescending(p => p.Value))
            {
                report.Append("\n  ")
                    .Append(pair.Value).Append(" - ")
                    .Append(pair.Key).Append("; e.g. ")
                    .Append(examples[pair.Key]);
            }
            diagnostic = report.ToString();
            return batches;
        }

        /// <summary>
        /// Use identical batching limits for Audit and Bake so a positive
        /// audit never promises groups that Bake silently discards.
        /// </summary>
        private static List<List<Source>> Partition(List<Source> sources)
        {
            var result = new List<List<Source>>();
            int offset = 0;
            while (offset < sources.Count)
            {
                var chunk = new List<Source>();
                var materials = new HashSet<Material>();
                int vertices = 0;
                while (offset < sources.Count)
                {
                    Source next = sources[offset];
                    Material[] nextMaterials = next.Renderer.sharedMaterials;
                    var incoming = new HashSet<Material>();
                    for (int sub = 0; sub < next.Mesh.subMeshCount; sub++)
                        if (!materials.Contains(nextMaterials[sub]))
                            incoming.Add(nextMaterials[sub]);

                    if (chunk.Count > 0 &&
                        (vertices + next.Mesh.vertexCount >
                             MaximumVerticesPerCell ||
                         materials.Count + incoming.Count >
                             MaximumMaterialsPerProxy))
                        break;

                    chunk.Add(next);
                    offset++;
                    vertices += next.Mesh.vertexCount;
                    for (int sub = 0; sub < next.Mesh.subMeshCount; sub++)
                        materials.Add(nextMaterials[sub]);
                }
                if (chunk.Count >= MinimumRenderers)
                    result.Add(chunk);
            }
            return result;
        }

        /// <summary>
        /// Bake a single material/cell mesh with a single material slot for
        /// each distinct source material. Each original source renderer is
        /// replaced in its ENTIRETY, including all opaque submeshes.
        /// </summary>
        private static Mesh CombineChunk(
            List<Source> chunk, int cellIndex, out Material[] materials)
        {
            var order = new List<Material>();
            var byMaterial = new Dictionary<Material, List<CombineInstance>>();
            foreach (Source source in chunk)
            {
                Material[] original = source.Renderer.sharedMaterials;
                for (int sub = 0; sub < source.Mesh.subMeshCount; sub++)
                {
                    Material material = original[sub];
                    if (!byMaterial.TryGetValue(
                        material, out List<CombineInstance> entries))
                    {
                        entries = new List<CombineInstance>();
                        byMaterial.Add(material, entries);
                        order.Add(material);
                    }
                    entries.Add(new CombineInstance
                    {
                        mesh = source.Mesh,
                        subMeshIndex = sub,
                        transform = source.Renderer.localToWorldMatrix
                    });
                }
            }

            materials = order.ToArray();
            var temporary = new List<Mesh>();
            try
            {
                foreach (Material material in order)
                {
                    Mesh part = new Mesh
                    {
                        name = "HLOD_" + cellIndex + "_material",
                        indexFormat = IndexFormat.UInt32
                    };
                    temporary.Add(part);
                    part.CombineMeshes(byMaterial[material].ToArray(),
                        true, true, false);
                }

                if (temporary.Count == 1)
                {
                    Mesh result = temporary[0];
                    temporary.Clear();
                    result.name = "HLOD_" + cellIndex;
                    return result;
                }

                Mesh combined = new Mesh
                {
                    name = "HLOD_" + cellIndex,
                    indexFormat = IndexFormat.UInt32
                };
                try
                {
                    var submeshes = new CombineInstance[temporary.Count];
                    for (int i = 0; i < submeshes.Length; i++)
                        submeshes[i] = new CombineInstance
                        {
                            mesh = temporary[i],
                            subMeshIndex = 0,
                            transform = Matrix4x4.identity
                        };
                    combined.CombineMeshes(submeshes, false, false, false);
                    return combined;
                }
                catch
                {
                    UnityEngine.Object.DestroyImmediate(combined);
                    throw;
                }
            }
            finally
            {
                foreach (Mesh mesh in temporary)
                    if (mesh != null)
                        UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static bool ActiveInAuthoredPrefab(
            Transform target, Transform root)
        {
            // Unlike activeInHierarchy, activeSelf is reliable for prefab
            // assets not instantiated into a loaded Unity scene.
            while (target != null)
            {
                if (!target.gameObject.activeSelf)
                    return false;
                if (target == root)
                    return true;
                target = target.parent;
            }
            return false;
        }

        private static bool IsExcluded(string name, string[] tokens)
        {
            if (string.IsNullOrEmpty(name))
                return true;
            foreach (string token in tokens)
                if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        // A very conservative offline vertex-cluster pass. Positions must
        // coincide within <0.75m AND UV/normal neighborhoods must match:
        // preserves UV seams and facade hard edges. If the mesh does not
        // shrink by >=5%, retain the exact merged source geometry instead.
        private static Mesh SimplifyConservatively(Mesh source)
        {
            Vector3[] positions = source.vertices;
            Vector3[] normals = source.normals;
            Vector2[] uv = source.uv;
            Color[] colors = source.colors;
            Vector4[] tangents = source.tangents;
            int[] indices = source.triangles;
            if (positions.Length == 0 ||
                normals.Length != positions.Length ||
                uv.Length != positions.Length)
                return source;

            var vertexMap = new Dictionary<string, int>(positions.Length);
            var newPositions = new List<Vector3>(positions.Length);
            var newNormals = new List<Vector3>(positions.Length);
            var newUv = new List<Vector2>(positions.Length);
            var newColors = new List<Color>(positions.Length);
            var newTangents = new List<Vector4>(positions.Length);
            int[] remap = new int[positions.Length];
            bool hasColors = colors.Length == positions.Length;
            bool hasTangents = tangents.Length == positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 p = positions[i];
                Vector3 n = normals[i];
                Vector2 t = uv[i];
                string key =
                    Mathf.RoundToInt(p.x / .75f) + "/" +
                    Mathf.RoundToInt(p.y / .75f) + "/" +
                    Mathf.RoundToInt(p.z / .75f) + "/" +
                    Mathf.RoundToInt(t.x / .03f) + "/" +
                    Mathf.RoundToInt(t.y / .03f) + "/" +
                    Mathf.RoundToInt(n.x / .12f) + "/" +
                    Mathf.RoundToInt(n.y / .12f) + "/" +
                    Mathf.RoundToInt(n.z / .12f) + "/" +
                    (hasColors ? colors[i].GetHashCode() : 0);

                if (!vertexMap.TryGetValue(key, out int mapped))
                {
                    mapped = newPositions.Count;
                    vertexMap.Add(key, mapped);
                    newPositions.Add(p);
                    newNormals.Add(n);
                    newUv.Add(t);
                    if (hasColors) newColors.Add(colors[i]);
                    if (hasTangents) newTangents.Add(tangents[i]);
                }
                remap[i] = mapped;
            }

            var newIndices = new List<int>(indices.Length);
            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                int a = remap[indices[i]];
                int b = remap[indices[i + 1]];
                int c = remap[indices[i + 2]];
                if (a == b || b == c || a == c)
                    continue;
                newIndices.Add(a);
                newIndices.Add(b);
                newIndices.Add(c);
            }

            if (newIndices.Count >= indices.Length * .95f ||
                newIndices.Count < 3)
                return source;

            Mesh result = new Mesh
            {
                name = source.name + "_simplified",
                indexFormat = IndexFormat.UInt32
            };
            result.SetVertices(newPositions);
            result.SetNormals(newNormals);
            result.SetUVs(0, newUv);
            if (hasColors) result.SetColors(newColors);
            if (hasTangents) result.SetTangents(newTangents);
            result.SetTriangles(newIndices, 0);
            result.RecalculateBounds();
            return result;
        }
    }
}
