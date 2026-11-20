using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Temporary Editor-only decimation experiment.
    /// Runs only while Unity Editor is in Play Mode. Original meshes are restored
    /// when stopped or when Play Mode exits. No FBX, prefab or asset is modified.
    /// </summary>
    [InitializeOnLoad]
    public static class MotorCityPlayModeDecimateExperiment
    {
        private const float TargetQuality = 0.8f;
        private const double SceneScanIntervalSeconds = 0.75;
        private const string ReportPath = "Library/MotorCityPlayModeDecimate_0_8.txt";

        private static readonly Dictionary<Renderer, Mesh> OriginalMeshes =
            new Dictionary<Renderer, Mesh>();

        private static readonly Dictionary<Mesh, Mesh> SimplifiedBySource =
            new Dictionary<Mesh, Mesh>();

        private static readonly Dictionary<Mesh, List<Renderer>> WaitingRenderers =
            new Dictionary<Mesh, List<Renderer>>();

        private static readonly Dictionary<Mesh, long> SourceTriangleCounts =
            new Dictionary<Mesh, long>();

        private static readonly Dictionary<Mesh, long> SimplifiedTriangleCounts =
            new Dictionary<Mesh, long>();

        private static readonly HashSet<Mesh> GeneratedMeshes = new HashSet<Mesh>();
        private static readonly HashSet<Mesh> QueuedMeshes = new HashSet<Mesh>();
        private static readonly HashSet<Mesh> FailedMeshes = new HashSet<Mesh>();
        private static readonly HashSet<Renderer> StaticBatchedRenderers = new HashSet<Renderer>();
        private static readonly Queue<Mesh> PendingMeshes = new Queue<Mesh>();

        private static bool running;
        private static bool selectedOnly;
        private static GameObject selectedRoot;
        private static double nextSceneScanTime;
        private static int processedMeshCount;
        private static int skippedUnreadable;
        private static int skippedNonTriangle;
        private static int skippedTooSmall;
        private static int failedMeshCount;
        private static int unchangedMeshCount;

        static MotorCityPlayModeDecimateExperiment()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Motor City/Play Mode Decimate 0.8/Start - all loaded meshes")]
        private static void Start()
        {
            StartInternal(false);
        }

        [MenuItem("Tools/Motor City/Play Mode Decimate 0.8/Start - selected object only")]
        private static void StartSelected()
        {
            StartInternal(true);
        }

        [MenuItem("Tools/Motor City/Play Mode Decimate 0.8/Start - all loaded meshes", true)]
        [MenuItem("Tools/Motor City/Play Mode Decimate 0.8/Start - selected object only", true)]
        private static bool ValidateStart()
        {
            return EditorApplication.isPlaying && !running;
        }

        private static void StartInternal(bool onlySelected)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Motor City Decimate] Enter Play Mode first.");
                return;
            }

            if (running)
            {
                Debug.Log("[Motor City Decimate] Experiment is already running.");
                return;
            }

            if (MotorCity.Platform.MotorCityBuildingLodPilot.Enabled)
            {
                Debug.LogWarning(
                    "[Motor City Decimate] Turn OFF the old Building LOD Pilot first. " +
                    "The two experiments must not replace the same meshes simultaneously.");
                return;
            }

            if (onlySelected && Selection.activeGameObject == null)
            {
                Debug.LogWarning("[Motor City Decimate] Select one building or object in the Hierarchy first.");
                return;
            }

            ClearState();
            selectedOnly = onlySelected;
            selectedRoot = onlySelected ? Selection.activeGameObject : null;
            running = true;
            nextSceneScanTime = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            string scope = onlySelected
                ? "selected hierarchy object: " + selectedRoot.name
                : "all loaded scenes, including objects spawned later";

            Debug.Log(
                "[Motor City Decimate] STARTED. Target quality=0.8 (about 80% of source triangles). " +
                "Scope: " + scope + ". UV seams, UV foldovers and borders are protected. " +
                "Only temporary in-memory meshes are changed. Stop and restore or exit Play Mode.");
        }

        [MenuItem("Tools/Motor City/Play Mode Decimate 0.8/Stop and restore")]
        private static void Stop()
        {
            if (!running)
                return;

            StopInternal(true);
        }

        [MenuItem("Tools/Motor City/Play Mode Decimate 0.8/Stop and restore", true)]
        private static bool ValidateStop()
        {
            return EditorApplication.isPlaying && running;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && running)
                StopInternal(true);
        }

        private static void Tick()
        {
            if (!running || !EditorApplication.isPlaying)
            {
                if (running)
                    StopInternal(true);
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now >= nextSceneScanTime)
            {
                ScanLoadedScenes();
                nextSceneScanTime = now + SceneScanIntervalSeconds;
            }

            ProcessNextMesh();
        }

        private static void ScanLoadedScenes()
        {
            if (selectedOnly)
            {
                if (selectedRoot != null)
                    ScanRoot(selectedRoot);
                return;
            }

            int sceneCount = SceneManager.sceneCount;
            for (int sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                    ScanRoot(roots[rootIndex]);
            }
        }

        private static void ScanRoot(GameObject root)
        {
            if (root == null)
                return;

            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null)
                    continue;

                MeshRenderer meshRenderer = filter.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    TrackRenderer(meshRenderer, filter.sharedMesh);
            }

            SkinnedMeshRenderer[] skinnedRenderers =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                SkinnedMeshRenderer skinned = skinnedRenderers[i];
                if (skinned != null)
                    TrackRenderer(skinned, skinned.sharedMesh);
            }

            ParticleSystemRenderer[] particleRenderers =
                root.GetComponentsInChildren<ParticleSystemRenderer>(true);

            for (int i = 0; i < particleRenderers.Length; i++)
            {
                ParticleSystemRenderer particleRenderer = particleRenderers[i];
                if (particleRenderer != null &&
                    particleRenderer.renderMode == ParticleSystemRenderMode.Mesh)
                    TrackRenderer(particleRenderer, particleRenderer.mesh);
            }
        }

        private static void TrackRenderer(Renderer renderer, Mesh source)
        {
            if (renderer == null || source == null || GeneratedMeshes.Contains(source))
                return;

            if (renderer.isPartOfStaticBatch)
            {
                StaticBatchedRenderers.Add(renderer);
                return;
            }

            if (OriginalMeshes.TryGetValue(renderer, out Mesh knownSource) &&
                knownSource == source)
                return;

            // If gameplay swaps a mesh while Play Mode is running, remember the
            // new source so Stop restores the latest gameplay-owned mesh.
            OriginalMeshes[renderer] = source;

            if (SimplifiedBySource.TryGetValue(source, out Mesh cached))
            {
                AssignMesh(renderer, cached);
                return;
            }

            if (FailedMeshes.Contains(source))
                return;

            if (!WaitingRenderers.TryGetValue(source, out List<Renderer> waiting))
            {
                waiting = new List<Renderer>();
                WaitingRenderers.Add(source, waiting);
            }

            waiting.Add(renderer);

            if (QueuedMeshes.Add(source))
                PendingMeshes.Enqueue(source);
        }

        private static void ProcessNextMesh()
        {
            while (PendingMeshes.Count > 0)
            {
                Mesh source = PendingMeshes.Dequeue();
                QueuedMeshes.Remove(source);

                if (source == null)
                    continue;

                ProcessMesh(source);
                return; // At most one simplification per Editor update tick.
            }
        }

        private static void ProcessMesh(Mesh source)
        {
            if (!source.isReadable)
            {
                skippedUnreadable++;
                FailedMeshes.Add(source);
                WaitingRenderers.Remove(source);
                return;
            }

            if (!TryGetTriangleCount(source, out long sourceTriangles))
            {
                skippedNonTriangle++;
                FailedMeshes.Add(source);
                WaitingRenderers.Remove(source);
                return;
            }

            if (source.vertexCount < 4 || sourceTriangles < 4)
            {
                skippedTooSmall++;
                FailedMeshes.Add(source);
                WaitingRenderers.Remove(source);
                return;
            }

            Mesh simplified = null;
            try
            {
                // Preserve atlas UV island borders and UV foldovers. The library defaults
                // these protections to false, which can badly distort city-building textures.
                var options = SimplificationOptions.Default;
                options.PreserveBorderEdges = true;
                options.PreserveUVSeamEdges = true;
                options.PreserveUVFoldoverEdges = true;

                var simplifier = new MeshSimplifier
                {
                    SimplificationOptions = options
                };

                simplifier.Initialize(source);
                simplifier.SimplifyMesh(TargetQuality);

                simplified = simplifier.ToMesh();
                if (simplified == null)
                    throw new InvalidOperationException("Simplifier returned a null mesh.");

                if (simplified.subMeshCount != source.subMeshCount ||
                    !TryGetTriangleCount(simplified, out long simplifiedTriangles) ||
                    simplifiedTriangles <= 0 ||
                    simplifiedTriangles >= sourceTriangles ||
                    !HasCompatibleSubMeshes(source, simplified) ||
                    !HasCompatibleUvChannels(source, simplified))
                {
                    UnityEngine.Object.DestroyImmediate(simplified);
                    simplified = null;
                    unchangedMeshCount++;
                    FailedMeshes.Add(source);
                    WaitingRenderers.Remove(source);
                    return;
                }

                simplified.name = source.name + "_PlayModeDecimate_08";
                simplified.hideFlags = HideFlags.DontSave;

                SimplifiedBySource.Add(source, simplified);
                GeneratedMeshes.Add(simplified);
                SourceTriangleCounts.Add(source, sourceTriangles);
                SimplifiedTriangleCounts.Add(source, simplifiedTriangles);
                processedMeshCount++;

                if (WaitingRenderers.TryGetValue(source, out List<Renderer> waiting))
                {
                    for (int i = 0; i < waiting.Count; i++)
                    {
                        Renderer renderer = waiting[i];
                        if (renderer != null && GetCurrentMesh(renderer) == source)
                            AssignMesh(renderer, simplified);
                    }

                    WaitingRenderers.Remove(source);
                }

                simplified = null; // The generated-mesh registry now owns it.

                if (processedMeshCount == 1 || processedMeshCount % 25 == 0)
                {
                    Debug.Log(
                        "[Motor City Decimate] Processed unique meshes: " + processedMeshCount +
                        ", remaining queue: " + PendingMeshes.Count +
                        ", latest: " + source.name + " (" + sourceTriangles + " -> " +
                        simplifiedTriangles + " triangles).");
                }
            }
            catch (Exception exception)
            {
                if (simplified != null && !GeneratedMeshes.Contains(simplified))
                    UnityEngine.Object.DestroyImmediate(simplified);

                failedMeshCount++;
                FailedMeshes.Add(source);
                WaitingRenderers.Remove(source);
                Debug.LogWarning(
                    "[Motor City Decimate] Could not simplify mesh '" + source.name +
                    "': " + exception.Message);
            }
        }

        private static bool HasCompatibleSubMeshes(Mesh source, Mesh simplified)
        {
            if (source == null || simplified == null ||
                source.subMeshCount != simplified.subMeshCount)
                return false;

            for (int i = 0; i < source.subMeshCount; i++)
            {
                if (source.GetTopology(i) != MeshTopology.Triangles ||
                    simplified.GetTopology(i) != MeshTopology.Triangles)
                    return false;

                if (source.GetIndexCount(i) > 0 && simplified.GetIndexCount(i) == 0)
                    return false;
            }

            return true;
        }

        private static bool HasCompatibleUvChannels(Mesh source, Mesh simplified)
        {
            if (source == null || simplified == null)
                return false;

            // Unity supports eight UV channels. Preserve every populated source channel
            // and require the generated values to match the generated vertex count.
            for (int channel = 0; channel < 8; channel++)
            {
                var sourceUvs = new List<Vector4>();
                var simplifiedUvs = new List<Vector4>();
                source.GetUVs(channel, sourceUvs);
                simplified.GetUVs(channel, simplifiedUvs);

                if (sourceUvs.Count > 0 && simplifiedUvs.Count != simplified.vertexCount)
                    return false;

                if (sourceUvs.Count == 0 && simplifiedUvs.Count > 0)
                    return false;
            }

            return true;
        }

        private static bool TryGetTriangleCount(Mesh mesh, out long triangles)
        {
            triangles = 0;

            if (mesh == null || mesh.subMeshCount <= 0)
                return false;

            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (mesh.GetTopology(subMesh) != MeshTopology.Triangles)
                    return false;

                triangles += (long)mesh.GetIndexCount(subMesh) / 3L;
            }

            return triangles > 0;
        }

        private static Mesh GetCurrentMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;

            if (renderer is ParticleSystemRenderer particleRenderer &&
                particleRenderer.renderMode == ParticleSystemRenderMode.Mesh)
                return particleRenderer.mesh;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        private static void AssignMesh(Renderer renderer, Mesh mesh)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                skinned.sharedMesh = mesh;
                return;
            }

            if (renderer is ParticleSystemRenderer particleRenderer &&
                particleRenderer.renderMode == ParticleSystemRenderMode.Mesh)
            {
                particleRenderer.mesh = mesh;
                return;
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null)
                filter.sharedMesh = mesh;
        }

        private static void StopInternal(bool writeReport)
        {
            running = false;
            EditorApplication.update -= Tick;

            int affectedRenderers = 0;
            long weightedSourceTriangles = 0;
            long weightedSimplifiedTriangles = 0;

            foreach (KeyValuePair<Renderer, Mesh> pair in OriginalMeshes)
            {
                Renderer renderer = pair.Key;
                Mesh source = pair.Value;

                if (renderer == null)
                    continue;

                Mesh current = GetCurrentMesh(renderer);
                if (current != null && GeneratedMeshes.Contains(current))
                {
                    affectedRenderers++;

                    if (SourceTriangleCounts.TryGetValue(source, out long before))
                        weightedSourceTriangles += before;

                    if (SimplifiedTriangleCounts.TryGetValue(source, out long after))
                        weightedSimplifiedTriangles += after;

                    AssignMesh(renderer, source);
                }
            }

            foreach (Mesh generated in GeneratedMeshes)
            {
                if (generated != null)
                    UnityEngine.Object.DestroyImmediate(generated);
            }

            double reduction = weightedSourceTriangles > 0
                ? 100.0 * (weightedSourceTriangles - weightedSimplifiedTriangles) /
                    weightedSourceTriangles
                : 0.0;

            var report = new StringBuilder();
            report.AppendLine("MOTOR CITY PLAY MODE DECIMATE EXPERIMENT");
            report.AppendLine("Target quality: " + TargetQuality.ToString("F2"));
            report.AppendLine("Editor Play Mode only: YES");
            report.AppendLine("Original FBX/prefabs/assets modified: NO");
            report.AppendLine("Unique meshes simplified: " + processedMeshCount);
            report.AppendLine("Renderers currently using simplified meshes: " + affectedRenderers);
            report.AppendLine("Weighted source triangles: " + weightedSourceTriangles);
            report.AppendLine("Weighted simplified triangles: " + weightedSimplifiedTriangles);
            report.AppendLine("Weighted triangle reduction: " + reduction.ToString("F1") + "%");
            report.AppendLine("Skipped unreadable meshes: " + skippedUnreadable);
            report.AppendLine("Skipped non-triangle meshes: " + skippedNonTriangle);
            report.AppendLine("Skipped tiny meshes (<4 triangles): " + skippedTooSmall);
            report.AppendLine("Unchanged meshes: " + unchangedMeshCount);
            report.AppendLine("Failed meshes: " + failedMeshCount);
            report.AppendLine("Static-batched renderers skipped: " + StaticBatchedRenderers.Count);
            report.AppendLine("Pending meshes not processed before stop: " + PendingMeshes.Count);
            report.AppendLine("All changed renderer meshes restored: YES");

            if (writeReport)
            {
                try
                {
                    File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[Motor City Decimate] Could not write report: " + exception.Message);
                }
            }

            Debug.Log("[Motor City Decimate] STOPPED. Original meshes restored.\n" + report);

            ClearState();
        }

        private static void ClearState()
        {
            OriginalMeshes.Clear();
            SimplifiedBySource.Clear();
            WaitingRenderers.Clear();
            SourceTriangleCounts.Clear();
            SimplifiedTriangleCounts.Clear();
            GeneratedMeshes.Clear();
            QueuedMeshes.Clear();
            FailedMeshes.Clear();
            StaticBatchedRenderers.Clear();
            PendingMeshes.Clear();
            selectedOnly = false;
            selectedRoot = null;

            processedMeshCount = 0;
            skippedUnreadable = 0;
            skippedNonTriangle = 0;
            skippedTooSmall = 0;
            failedMeshCount = 0;
            unchangedMeshCount = 0;
        }
    }
}
