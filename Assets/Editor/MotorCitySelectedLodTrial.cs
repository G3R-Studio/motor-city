using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Opt-in scene-instance LOD0/LOD1 trial. Never alters CityVisual.prefab,
    /// original meshes, render materials, colliders or WebGL startup.
    /// </summary>
    public static class MotorCitySelectedLodTrial
    {
        private const string Folder = "Assets/MotorCityGenerated/LODTrials";
        private const int MaximumObjects = 8;
        private const float GridFraction = 0.025f;
        private const float MinimumTriangleSaving = 0.15f;

        private readonly struct VertexKey : IEquatable<VertexKey>
        {
            private readonly int x, y, z, u, v, nx, ny, nz;
            public VertexKey(Vector3 p, Vector2 uv, Vector3 normal, float cell)
            {
                x = Mathf.RoundToInt(p.x / cell);
                y = Mathf.RoundToInt(p.y / cell);
                z = Mathf.RoundToInt(p.z / cell);
                // Keep UV islands and hard-edge normals separate.
                u = Mathf.RoundToInt(uv.x * 1024f);
                v = Mathf.RoundToInt(uv.y * 1024f);
                nx = Mathf.RoundToInt(normal.x * 32f);
                ny = Mathf.RoundToInt(normal.y * 32f);
                nz = Mathf.RoundToInt(normal.z * 32f);
            }
            public bool Equals(VertexKey other) =>
                x == other.x && y == other.y && z == other.z &&
                u == other.u && v == other.v &&
                nx == other.nx && ny == other.ny && nz == other.nz;
            public override bool Equals(object obj) => obj is VertexKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + x; hash = hash * 31 + y;
                    hash = hash * 31 + z; hash = hash * 31 + u;
                    hash = hash * 31 + v; hash = hash * 31 + nx;
                    hash = hash * 31 + ny; hash = hash * 31 + nz;
                    return hash;
                }
            }
        }

        [MenuItem("Motor City/Optimization/LOD Trial/1 - Audit Selection (Edit Mode)")]
        private static void Audit()
        {
            int valid = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                string reason = Validate(go, out Mesh mesh, out _);
                if (reason == null)
                {
                    valid++;
                    Debug.Log("[MotorCity][LOD Trial] Candidate: " + go.name +
                              ", triangles=" + mesh.triangles.Length / 3, go);
                }
                else
                    Debug.Log("[MotorCity][LOD Trial] Skipped " + go.name + ": " + reason, go);
            }
            Debug.Log("[MotorCity][LOD Trial] " + valid +
                      " safe candidates. Select at most " + MaximumObjects +
                      " scene objects; do not select the source prefab.");
        }

        [MenuItem("Motor City/Optimization/LOD Trial/2 - Generate Selected LOD1 (Edit Mode)")]
        private static void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[MotorCity][LOD Trial] Exit Play Mode first.");
                return;
            }
            GameObject[] selection = Selection.gameObjects;
            if (selection.Length == 0 || selection.Length > MaximumObjects)
            {
                Debug.LogWarning("[MotorCity][LOD Trial] Select 1-" +
                                 MaximumObjects + " scene objects.");
                return;
            }

            // Validate everything before writing any assets.
            foreach (GameObject go in selection)
            {
                string reason = Validate(go, out _, out _);
                if (reason != null)
                {
                    Debug.LogWarning("[MotorCity][LOD Trial] Aborted: " +
                                     go.name + " - " + reason, go);
                    return;
                }
            }

            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            int made = 0;
            foreach (GameObject go in selection)
            {
                MeshRenderer original = go.GetComponent<MeshRenderer>();
                Mesh source = go.GetComponent<MeshFilter>().sharedMesh;
                Mesh lower = Reduce(source);
                int before = source.triangles.Length / 3;
                int after = lower.triangles.Length / 3;
                if (after >= before * (1f - MinimumTriangleSaving))
                {
                    UnityEngine.Object.DestroyImmediate(lower);
                    Debug.LogWarning("[MotorCity][LOD Trial] No useful safe reduction for " +
                                     go.name + " (" + before + " -> " + after +
                                     "). Nothing changed.", go);
                    continue;
                }

                string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    Folder + "/" + Sanitize(go.name) + "_LOD1.asset");
                AssetDatabase.CreateAsset(lower, assetPath);
                GameObject child = new GameObject("__MotorCity_LOD1_Preview");
                Undo.RegisterCreatedObjectUndo(child, "Create Motor City LOD1");
                child.transform.SetParent(go.transform, false);
                child.layer = go.layer;
                child.isStatic = false;
                MeshFilter filter = Undo.AddComponent<MeshFilter>(child);
                filter.sharedMesh = lower;
                MeshRenderer proxy = Undo.AddComponent<MeshRenderer>(child);
                proxy.sharedMaterials = original.sharedMaterials;
                proxy.shadowCastingMode = ShadowCastingMode.Off;
                proxy.receiveShadows = false;
                proxy.lightProbeUsage = original.lightProbeUsage;
                proxy.reflectionProbeUsage = original.reflectionProbeUsage;

                LODGroup group = Undo.AddComponent<LODGroup>(go);
                group.SetLODs(new[]
                {
                    new LOD(0.18f, new Renderer[] { original }),
                    new LOD(0.01f, new Renderer[] { proxy })
                });
                group.animateCrossFading = false;
                group.RecalculateBounds();
                EditorUtility.SetDirty(go);
                made++;
                Debug.Log("[MotorCity][LOD Trial] " + go.name + ": " + before +
                          " -> " + after + " triangles (" +
                          Mathf.RoundToInt(100f * (1f - (float)after / before)) +
                          "% saved). Mesh: " + assetPath, go);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[MotorCity][LOD Trial] Created " + made +
                      " LODGroups. Test Game view and Profiler before expanding. " +
                      "Generated assets are not deleted by scene Undo.");
        }

        private static string Validate(GameObject go, out Mesh mesh,
            out MeshRenderer renderer)
        {
            mesh = null;
            renderer = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Edit Mode only";
            if (go == null || EditorUtility.IsPersistent(go))
                return "must be a scene instance";
            if (!go.activeInHierarchy)
                return "inactive scene object";
            if (PrefabUtility.IsPartOfPrefabAsset(go))
                return "source prefab is protected";
            renderer = go.GetComponent<MeshRenderer>();
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (renderer == null || mf == null || mf.sharedMesh == null ||
                !renderer.enabled)
                return "needs an enabled MeshRenderer with MeshFilter";
            mesh = mf.sharedMesh;
            if (!mesh.isReadable || mesh.subMeshCount != 1 ||
                mesh.vertexCount < 150 || mesh.vertexCount > 18000)
                return "requires readable 1-submesh mesh with 150-18000 vertices";
            if (mesh.blendShapeCount != 0)
                return "blend shapes are excluded";
            if (mesh.uv.Length != mesh.vertexCount ||
                mesh.normals.Length != mesh.vertexCount)
                return "requires UV0 and normals for each vertex";
            if (mesh.uv2.Length != 0 || renderer.lightmapIndex >= 0)
                return "lightmapped meshes are excluded";
            if (mesh.colors.Length > 0 || mesh.colors32.Length > 0 ||
                mesh.tangents.Length > 0)
                return "vertex-color and tangent-dependent materials excluded";
            if (mesh.GetTopology(0) != MeshTopology.Triangles)
                return "triangle topology required";
            if (renderer.isPartOfStaticBatch)
                return "static-batched renderer";
            if (go.GetComponentInParent<Rigidbody>() != null)
                return "Rigidbody hierarchy excluded";
            if (go.GetComponentInParent<LODGroup>() != null ||
                go.GetComponentInChildren<LODGroup>(true) != null)
                return "existing LODGroup";
            if (go.GetComponentsInChildren<Renderer>(true).Length != 1)
                return "only single-renderer objects supported";
            if (go.GetComponents<MonoBehaviour>().Length > 0)
                return "scripted object excluded";
            if (renderer.sharedMaterials.Length != 1 ||
                renderer.sharedMaterial == null)
                return "exactly one material required";
            Material material = renderer.sharedMaterial;
            string name = (go.name + " " + material.name).ToLowerInvariant();
            string[] blocked = { "glass", "win", "emiss", "night", "alpha",
                "transparent", "tree", "palm", "fern", "plant", "grass",
                "road", "bridge", "traffic", "car", "truck", "bus",
                "light", "water", "terrain", "park" };
            foreach (string part in blocked)
                if (name.Contains(part))
                    return "protected name/material category: " + part;
            if (material.renderQueue >= 2450 ||
                material.HasProperty("_Surface") &&
                material.GetFloat("_Surface") > 0.5f)
                return "transparent/cutout material";
            if (renderer.bounds.extents.magnitude < 7.1f)
                return "small object already handled by distance culling";
            return null;
        }

        internal static Mesh Reduce(Mesh source)
        {
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector2[] uv = source.uv;
            int[] indices = source.triangles;
            Vector3 size = source.bounds.size;
            float cell = Mathf.Max(0.005f,
                Mathf.Max(size.x, Mathf.Max(size.y, size.z)) * GridFraction);
            var byKey = new Dictionary<VertexKey, int>();
            var remap = new int[vertices.Length];
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<Vector2>();
            for (int i = 0; i < vertices.Length; ++i)
            {
                var key = new VertexKey(vertices[i], uv[i], normals[i], cell);
                if (!byKey.TryGetValue(key, out int j))
                {
                    j = v.Count;
                    byKey.Add(key, j);
                    v.Add(vertices[i]);
                    n.Add(normals[i]);
                    t.Add(uv[i]);
                }
                remap[i] = j;
            }
            var tris = new List<int>(indices.Length);
            for (int i = 0; i < indices.Length; i += 3)
            {
                int a = remap[indices[i]];
                int b = remap[indices[i + 1]];
                int c = remap[indices[i + 2]];
                if (a == b || b == c || c == a)
                    continue;
                // Do not retain almost-zero triangles after remapping.
                if (Vector3.Cross(v[b] - v[a], v[c] - v[a]).sqrMagnitude < 1e-12f)
                    continue;
                tris.Add(a); tris.Add(b); tris.Add(c);
            }
            var reduced = new Mesh
            {
                name = source.name + "_MotorCity_LOD1",
                indexFormat = v.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            reduced.SetVertices(v);
            reduced.SetNormals(n);
            reduced.SetUVs(0, t);
            reduced.SetTriangles(tris, 0);
            reduced.RecalculateBounds();
            return reduced;
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
