using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MotorCity.EditorTools
{
    public static class MotorCityRenderAudit
    {
        private const string Output = "Library/MotorCityRenderAudit.txt";
        private static string Id(UnityEngine.Object obj)
        {
            if (obj == null) return "-";
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long local)
                ? guid + ":" + local : obj.name;
        }

        private static string PathOf(UnityEngine.Object obj) =>
            obj == null ? "-" : AssetDatabase.GetAssetPath(obj);

        private static void Count(Dictionary<string, int> counts, string key)
        {
            counts.TryGetValue(key, out int count);
            counts[key] = count + 1;
        }

        private static long Triangles(Mesh mesh)
        {
            long total = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                if (mesh.GetTopology(i) == MeshTopology.Triangles)
                    total += mesh.GetIndexCount(i) / 3;
            return total;
        }

        [MenuItem("Tools/Motor City/Audit/Render Audit")]
        private static void Scan()
        {
            var report = new StringBuilder();
            var details = new StringBuilder();
            var roots = new HashSet<string>(StringComparer.Ordinal);
            var usage = new Dictionary<string, int>();
            var colliders = new Dictionary<string, int>();
            var groups = new Dictionary<string, int>();
            var renderers = new Dictionary<string, int>();
            var textureUsers = new Dictionary<string, HashSet<string>>();
            long textureBytes = 0, meshBytes = 0, uniqueTriangles = 0;
            int textureCount = 0, meshCount = 0;
            report.AppendLine("MOTOR CITY RENDER AUDIT | " + DateTime.UtcNow.ToString("u"));
            report.AppendLine("Read-only editor inventory. Not runtime draw calls, GPU memory, or build bytes.");
            report.AppendLine("Active import target: " + EditorUserBuildSettings.activeBuildTarget);
            report.AppendLine("Resources + enabled build scenes + recursive static dependencies. Dynamic references outside these roots may be missing.");
            try
            {
                foreach (var scene in EditorBuildSettings.scenes)
                    if (scene.enabled) roots.Add(scene.path);
                foreach (string path in AssetDatabase.GetAllAssetPaths())
                    if (path.StartsWith("Assets/", StringComparison.Ordinal) &&
                        path.Contains("/Resources/") && !AssetDatabase.IsValidFolder(path))
                        roots.Add(path);
                var paths = AssetDatabase.GetDependencies(roots.ToArray(), true)
                    .Concat(roots).Distinct().OrderBy(x => x).ToArray();
                report.AppendLine("Roots: " + roots.Count + " | dependency paths: " + paths.Length);
                foreach (string root in roots.OrderBy(x => x))
                    details.AppendLine("ROOT " + root);

                for (int i = 0; i < paths.Length; i++)
                {
                    string path = paths[i];
                    if (i % 25 == 0 && EditorUtility.DisplayCancelableProgressBar(
                        "Motor City Render Audit", path, (float)i / Math.Max(1, paths.Length)))
                        throw new OperationCanceledException("Cancelled; inventory incomplete.");
                    if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) continue;
                    var importer = AssetImporter.GetAtPath(path);
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (obj is Texture2D tex)
                        {
                            textureCount++;
                            long bytes = Profiler.GetRuntimeMemorySizeLong(tex);
                            textureBytes += bytes;
                            details.AppendLine($"TEXTURE {Id(tex)} | {path} | {tex.name} | {tex.width}x{tex.height} | {tex.format} | mips={tex.mipmapCount} | readable={tex.isReadable} | editorRuntimeBytes={bytes}");
                            if (importer is TextureImporter ti)
                            {
                                details.AppendLine($"  type={ti.textureType} sRGB={ti.sRGBTexture} alpha={ti.alphaSource} alphaTransparency={ti.alphaIsTransparency} mipmaps={ti.mipmapEnabled}");
                                foreach (string platform in new[] { "Standalone", "WebGL", "Android", "iPhone" })
                                {
                                    var ps = ti.GetPlatformTextureSettings(platform);
                                    details.AppendLine($"  {platform}: override={ps.overridden} max={ps.maxTextureSize} format={ps.format} compression={ps.textureCompression}");
                                }
                            }
                        }
                        else if (obj is Mesh mesh)
                        {
                            meshCount++;
                            long bytes = Profiler.GetRuntimeMemorySizeLong(mesh);
                            long tris = Triangles(mesh);
                            meshBytes += bytes;
                            uniqueTriangles += tris;
                            details.AppendLine($"MESH {Id(mesh)} | {path} | {mesh.name} | vertices={mesh.vertexCount} triangles={tris} submeshes={mesh.subMeshCount} readable={mesh.isReadable} bounds={mesh.bounds.size} editorRuntimeBytes={bytes}");
                            if (importer is ModelImporter mi)
                                details.AppendLine($"  modelReadable={mi.isReadable} meshCompression={mi.meshCompression}");
                        }
                        else if (obj is Material mat)
                        {
                            details.AppendLine($"MATERIAL {Id(mat)} | {path} | shader={(mat.shader != null ? mat.shader.name : "-")} queue={mat.renderQueue} instancing={mat.enableInstancing}");
                            foreach (string prop in mat.GetTexturePropertyNames())
                            {
                                Texture texRef = mat.GetTexture(prop);
                                if (texRef == null) continue;
                                string key = Id(texRef);
                                if (!textureUsers.TryGetValue(key, out var users))
                                    textureUsers[key] = users = new HashSet<string>();
                                users.Add(path + " | " + mat.name + " | " + prop);
                            }
                        }
                    }
                    // Inspect prefab roots only, not every nested subasset; instances
                    // in separate prefabs are separate authored occurrences.
                    if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (go != null) Inspect(go, path, details, usage, colliders, groups, renderers);
                    }
                }
                // Do not open/alter scenes. Scan currently loaded scene objects only.
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    foreach (var go in scene.GetRootGameObjects())
                        Inspect(go, "OPEN SCENE " + scene.path, details, usage, colliders, groups, renderers);
                }
                report.AppendLine($"Texture2D objects={textureCount} editorRuntimeBytes={textureBytes}");
                report.AppendLine($"Unique mesh subassets={meshCount} triangles={uniqueTriangles} editorRuntimeBytes={meshBytes}");
                report.AppendLine("Counts below are authored occurrences across prefabs/open scenes, NOT simultaneously rendered instances.");
                WriteCounts(report, "RENDERER/COMPONENT COUNTS", renderers);
                WriteCounts(report, "RENDER MESH OCCURRENCES", usage);
                WriteCounts(report, "COLLIDER MESH OCCURRENCES", colliders);
                WriteCounts(report, "SHARED MESH/MATERIAL/SUBMESH CANDIDATES", groups);
                details.AppendLine("\nTEXTURE MATERIAL REFERENCES");
                foreach (var entry in textureUsers.OrderBy(x => x.Key))
                    details.AppendLine(entry.Key + "\n  " + string.Join("\n  ", entry.Value.OrderBy(x => x)));
                report.AppendLine("\nCOMPLETE (asset inventory only; runtime profiling remains pending).");
            }
            catch (Exception ex)
            {
                report.AppendLine("\nINCOMPLETE: " + ex);
                Debug.LogError(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Directory.CreateDirectory("Library");
                File.WriteAllText(Output, report.AppendLine("\nDETAILS").Append(details).ToString(), Encoding.UTF8);
                Debug.Log("Render audit report: " + Output);
            }
        }

        private static void WriteCounts(StringBuilder report, string heading, Dictionary<string, int> counts)
        {
            report.AppendLine("\n" + heading);
            foreach (var item in counts.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
                report.AppendLine(item.Value + " | " + item.Key);
        }

        private static void Inspect(GameObject go, string path, StringBuilder details,
            Dictionary<string, int> usage, Dictionary<string, int> colliders,
            Dictionary<string, int> groups, Dictionary<string, int> counts)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var cs = go.GetComponentsInChildren<Collider>(true);
            int lod = go.GetComponentsInChildren<LODGroup>(true).Length;
            int lights = go.GetComponentsInChildren<Light>(true).Length;
            int bodies = go.GetComponentsInChildren<Rigidbody>(true).Length;
            int animators = go.GetComponentsInChildren<Animator>(true).Length;
            int particles = go.GetComponentsInChildren<ParticleSystem>(true).Length;
            details.AppendLine($"OBJECT ROOT {path} | {go.name} | renderers={rs.Length} colliders={cs.Length} lodGroups={lod} lights={lights} rigidbodies={bodies} animators={animators} particles={particles}");
            foreach (var c in cs)
            {
                Count(counts, "Collider " + c.GetType().Name);
                if (c is MeshCollider mc && mc.sharedMesh != null)
                    Count(colliders, Id(mc.sharedMesh) + " | " + PathOf(mc.sharedMesh));
            }
            foreach (var renderer in rs)
            {
                Count(counts, renderer.GetType().Name + " | enabled=" + renderer.enabled + " | active=" + renderer.gameObject.activeInHierarchy + " | shadow=" + renderer.shadowCastingMode + " | receive=" + renderer.receiveShadows);
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skin && skin != null)
                    mesh = skin.sharedMesh;
                else if (renderer is MeshRenderer &&
                         renderer.TryGetComponent<MeshFilter>(out var filter) &&
                         filter != null)
                    mesh = filter.sharedMesh;
                if (mesh == null) continue;
                Count(usage, Id(mesh) + " | " + PathOf(mesh) + " | " + mesh.name);
                var mats = renderer.sharedMaterials;
                for (int sub = 0; sub < Math.Min(mesh.subMeshCount, mats.Length); sub++)
                {
                    var mat = mats[sub];
                    if (mat == null) continue;
                    Count(groups, Id(mesh) + " | " + PathOf(mesh) + " | " + mesh.name +
                        " | sub=" + sub + " | " + Id(mat) + " | " + PathOf(mat) +
                        " | " + mat.name + " | queue=" + mat.renderQueue +
                        " | staticFlags=" + GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) +
                        " | renderer=" + renderer.GetType().Name);
                }
            }
        }
    }
}
