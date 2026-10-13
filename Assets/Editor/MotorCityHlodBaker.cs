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
        private const int MinimumRenderers = 5;
        private const int MaximumVerticesPerCell = 45000;
        private const int MaximumSourceVertices = 18000;
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
            public Material Material;
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
            var groups = Collect(out all);
            int eligible = groups.Values.Sum(g => g.Sources.Count);
            int bakeGroups = groups.Values.Count(g => g.Sources.Count >= MinimumRenderers);
            int triangles = groups.Values.Sum(g => g.Sources.Sum(s => s.Triangles));
            Debug.Log("[MotorCity][HLOD] Candidate audit: " + eligible +
                "/" + all.Length + " eligible architecture MeshRenderers, " +
                bakeGroups + " material/120m cell groups with >=" +
                MinimumRenderers + " renderers, " + triangles +
                " eligible triangle references (not GPU-visible triangles). " +
                "No assets changed. If eligible count is too low, inspect " +
                "city renderer names before relaxing protections.");
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
            var groups = Collect(out all);
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

                    int offset = 0;
                    while (offset < sources.Count &&
                           cells.Count < MaximumProxyCells &&
                           proxyTris < MaximumProxyTriangles)
                    {
                        var chunk = new List<Source>();
                        int vertices = 0;
                        while (offset < sources.Count &&
                            (vertices + sources[offset].Mesh.vertexCount <=
                             MaximumVerticesPerCell || chunk.Count == 0))
                        {
                            Source s = sources[offset++];
                            chunk.Add(s);
                            vertices += s.Mesh.vertexCount;
                        }
                        if (chunk.Count < MinimumRenderers)
                            continue;

                        try
                        {
                            CombineInstance[] instances = chunk.Select(s =>
                                new CombineInstance
                                {
                                    mesh = s.Mesh,
                                    subMeshIndex = 0,
                                    transform = s.Renderer.localToWorldMatrix
                                }).ToArray();
                            Mesh combined = new Mesh
                            {
                                name = "HLOD_" + cells.Count,
                                indexFormat = IndexFormat.UInt32
                            };
                            combined.CombineMeshes(instances, true, true, false);
                            Mesh simplified = SimplifyConservatively(combined);
                            if (simplified != combined)
                                UnityEngine.Object.DestroyImmediate(combined);

                            int sourceCount = chunk.Sum(s => s.Triangles);
                            int proxyCount = simplified.triangles.Length / 3;
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
                                ProxyMaterial = entry.Value.Material,
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

        private static Dictionary<string, Batch> Collect(out MeshRenderer[] all)
        {
            GameObject city = AssetDatabase.LoadAssetAtPath<GameObject>(CityPath);
            if (city == null)
                throw new InvalidOperationException("Missing city prefab: " + CityPath);

            all = city.GetComponentsInChildren<MeshRenderer>(true);
            var batches = new Dictionary<string, Batch>();
            for (int i = 0; i < all.Length; i++)
            {
                MeshRenderer renderer = all[i];
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy ||
                    renderer.GetComponentInParent<LODGroup>() != null ||
                    renderer.isPartOfStaticBatch ||
                    renderer.HasPropertyBlock() ||
                    renderer.GetComponent<MonoBehaviour>() != null ||
                    renderer.lightmapIndex >= 0 ||
                    IsExcluded(renderer.gameObject.name, ExcludedNames))
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter == null ? null : filter.sharedMesh;
                Material[] materials = renderer.sharedMaterials;
                if (mesh == null || !mesh.isReadable ||
                    mesh.vertexCount > MaximumSourceVertices ||
                    mesh.subMeshCount != 1 || materials.Length != 1 ||
                    materials[0] == null || materials[0].renderQueue >= 2450 ||
                    IsExcluded(materials[0].name, ExcludedMaterials))
                    continue;

                Bounds bounds = renderer.bounds;
                float radius = bounds.extents.magnitude;
                if (radius < 7.1f || radius > 65f ||
                    bounds.size.y < 3.5f || bounds.size.x > 120f ||
                    bounds.size.z > 120f)
                    continue;

                // Upper parents can host city infrastructure; reject clearly
                // non-architectural hierarchies without requiring a name list
                // that may vary among FCG generations.
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
                    continue;

                int x = Mathf.FloorToInt(bounds.center.x / CellSize);
                int z = Mathf.FloorToInt(bounds.center.z / CellSize);
                string key = x + ":" + z + ":" + materials[0].GetEntityId();
                if (!batches.TryGetValue(key, out Batch batch))
                {
                    batch = new Batch { Material = materials[0] };
                    batches.Add(key, batch);
                }

                int triangles = (int)(mesh.GetIndexCount(0) / 3);
                batch.Sources.Add(new Source(renderer, mesh, i, triangles));
            }
            return batches;
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
