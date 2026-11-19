using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace MotorCity.EditorTools
{
    public static class MotorCityMeshReadabilityAudit
    {
        private const string Output = "Library/MotorCityMeshReadabilityAudit.txt";
        private sealed class Entry
        {
            public string Path;
            public int Meshes, Readable, RenderUses, ColliderUses, SkinUses;
            public long Bytes;
            public readonly HashSet<string> Users = new();
            public readonly HashSet<string> Flags = new();
        }

        [MenuItem("Tools/Motor City/Audit/Mesh Readability Audit")]
        private static void Audit()
        {
            var report = new StringBuilder("MOTOR CITY MESH READABILITY AUDIT\n");
            var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
            try
            {
                // Import settings apply to the whole model, not individual meshes.
                // Restrict the experiment to city scenery; exclude roads, buildings,
                // characters, vehicles, traffic lights and other gameplay assets.
                foreach (string path in AssetDatabase.GetAllAssetPaths())
                {
                    if (!path.StartsWith("Assets/Fantastic City Generator/Objects/", StringComparison.Ordinal) ||
                        !path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!(AssetImporter.GetAtPath(path) is ModelImporter mi) || !mi.isReadable) continue;
                    var e = new Entry { Path = path };
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (!(obj is Mesh mesh)) continue;
                        e.Meshes++;
                        if (mesh.isReadable) e.Readable++;
                        e.Bytes += Profiler.GetRuntimeMemorySizeLong(mesh);
                    }
                    if (e.Readable > 0) entries[path] = e;
                }

                // Include authored prefabs across Assets, not only Resources. A shared
                // FBX could be used outside the city or by a currently unused prefab.
                var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
                    .Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
                for (int i = 0; i < paths.Length; i++)
                {
                    string path = paths[i];
                    if (i % 20 == 0 && EditorUtility.DisplayCancelableProgressBar(
                        "Mesh Readability Audit", path, (float)i / Math.Max(1, paths.Length)))
                        throw new OperationCanceledException();
                    if (!AssetDatabase.GetDependencies(path, true).Any(entries.ContainsKey)) continue;
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null) Inspect(go, path, entries);
                }

                // Scene dependency check also covers closed scenes conservatively:
                // any candidate referenced there is withheld from the pilot.
                foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
                {
                    string scene = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (string path in AssetDatabase.GetDependencies(scene, true))
                        if (entries.TryGetValue(path, out var e))
                            e.Flags.Add("Scene dependency: " + scene);
                }
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    foreach (var go in scene.GetRootGameObjects())
                        Inspect(go, "OPEN SCENE " + scene.path, entries);
                }

                report.AppendLine("Candidates are review-only, not automatically safe. Editor memory estimate is not predicted savings.");
                report.AppendLine("Includes all submeshes in readable scenery FBX, all prefab dependencies and scene dependency checks.");
                foreach (var e in entries.Values.OrderByDescending(x => x.Bytes))
                {
                    bool review = e.RenderUses > 0 && e.ColliderUses == 0 && e.SkinUses == 0 && e.Flags.Count == 0;
                    report.AppendLine($"{(review ? "REVIEW" : "HOLD")} | {e.Path} | meshes={e.Meshes} readable={e.Readable} editorBytes={e.Bytes} renderUses={e.RenderUses} colliderUses={e.ColliderUses} skinUses={e.SkinUses}");
                    foreach (string flag in e.Flags.OrderBy(x => x)) report.AppendLine("  FLAG " + flag);
                    foreach (string user in e.Users.OrderBy(x => x)) report.AppendLine("  USER " + user);
                }
                report.AppendLine("COMPLETE");
            }
            catch (Exception ex) { report.AppendLine("INCOMPLETE: " + ex); Debug.LogError(ex); }
            finally
            {
                EditorUtility.ClearProgressBar();
                File.WriteAllText(Output, report.ToString(), Encoding.UTF8);
                Debug.Log("Mesh readability audit: " + Output);
            }
        }

        private static Entry Find(Mesh mesh, Dictionary<string, Entry> entries)
        {
            if (mesh == null) return null;
            return entries.TryGetValue(AssetDatabase.GetAssetPath(mesh), out var e) ? e : null;
        }

        private static void Inspect(GameObject root, string owner, Dictionary<string, Entry> entries)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var e = Find(filter.sharedMesh, entries);
                if (e == null) continue;
                e.RenderUses++;
                e.Users.Add(owner);
                if (filter.GetComponentInParent<Rigidbody>(true) != null)
                    e.Flags.Add("Rigidbody ancestor: " + owner);
                if (filter.GetComponentInParent<Animator>(true) != null)
                    e.Flags.Add("Animator ancestor: " + owner);
                if (filter.GetComponentInParent<LODGroup>(true) != null)
                    e.Flags.Add("LOD ancestor: " + owner);
            }
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true))
            {
                var e = Find(collider.sharedMesh, entries);
                if (e == null) continue;
                e.ColliderUses++;
                e.Users.Add(owner);
            }
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var e = Find(skin.sharedMesh, entries);
                if (e == null) continue;
                e.SkinUses++;
                e.Users.Add(owner);
            }
        }
    }
}
