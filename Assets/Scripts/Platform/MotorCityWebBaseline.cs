#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class MotorCityWebBaseline : MonoBehaviour
    {
        [DllImport("__Internal")] private static extern void MotorCityBaselineInstall();
        [DllImport("__Internal")] private static extern int MotorCityBaselineRequest();
        [DllImport("__Internal")] private static extern void MotorCityBaselinePublish(string report);
        private readonly List<double> frames = new();
        private readonly List<Counter> counters = new();
        private double started;
        private bool recording;
        private int meshPilotReleased;
        private bool armed;
        private double readyAt;
        private string context;
        private sealed class Counter
        {
            public string Name;
            public ProfilerRecorder Recorder;
            public double Sum;
            public long Peak;
            public int Samples;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("Motor City Web Baseline");
            DontDestroyOnLoad(go);
            go.AddComponent<MotorCityWebBaseline>();
            MotorCityBaselineInstall();
        }

        private void Add(ProfilerCategory category, string name)
        {
            var c = new Counter { Name = name };
            try { c.Recorder = ProfilerRecorder.StartNew(category, name, 1); }
            catch (Exception) { }
            counters.Add(c);
        }

        private void Begin()
        {
            frames.Clear();
            Add(ProfilerCategory.Internal, "Main Thread");
            Add(ProfilerCategory.Internal, "Render Thread");
            Add(ProfilerCategory.Render, "Draw Calls Count");
            Add(ProfilerCategory.Render, "Batches Count");
            Add(ProfilerCategory.Render, "SetPass Calls Count");
            Add(ProfilerCategory.Render, "Triangles Count");
            Add(ProfilerCategory.Render, "Vertices Count");
            Add(ProfilerCategory.Memory, "Total Used Memory");
            Add(ProfilerCategory.Memory, "GC Used Memory");
            Add(ProfilerCategory.Memory, "GC Allocated In Frame");
            context = $"lodPilot={MotorCityBuildingLodPilot.Enabled} lodRenderers={MotorCityBuildingLodPilot.RendererCount} meshPilotReleased={meshPilotReleased} Unity={Application.unityVersion} screen={Screen.width}x{Screen.height} preset={MotorCityQualityRuntime.CurrentPreset} targetFPS={Application.targetFrameRate} GPU={SystemInfo.graphicsDeviceName}";
            started = Time.realtimeSinceStartupAsDouble;
            recording = true;
        }

        private void Update()
        {
            int request = MotorCityBaselineRequest();
            if (request == 4 || request == 5)
            {
                if (!recording && !armed) MotorCityBuildingLodPilot.SetEnabled(request == 4);
                else Debug.LogWarning("Stop baseline first.");
                return;
            }
            if (request == 3)
            {
                if (!recording && !armed) RunMeshPilot();
                else Debug.LogWarning("Mesh pilot: stop baseline first.");
                return;
            }
            if (request == 1 && !recording)
            {
                armed = true;
                readyAt = Time.realtimeSinceStartupAsDouble + 5;
            }
            if (request == 2)
            {
                armed = false;
                if (recording) Finish(false);
                return;
            }
            if (!recording)
            {
                if (armed && Application.isFocused &&
                    Time.realtimeSinceStartupAsDouble >= readyAt)
                {
                    armed = false;
                    Begin();
                }
                return;
            }
            if (!Application.isFocused) { Finish(false); return; }
            double elapsed = Time.realtimeSinceStartupAsDouble - started;
            double ms = Time.unscaledDeltaTime * 1000.0;
            if (ms > 0 && !double.IsNaN(ms) && !double.IsInfinity(ms)) frames.Add(ms);
            foreach (var c in counters)
            {
                if (!c.Recorder.Valid || c.Recorder.Count == 0) continue;
                long v = c.Recorder.LastValue;
                c.Sum += v;
                c.Peak = Math.Max(c.Peak, v);
                c.Samples++;
            }
            if (elapsed >= 60) Finish(true);
        }

        // Explicit diagnostic only. Preserve FBX import settings and all loaded
        // collider/skin mesh references. Reload the page to restore CPU copies.
        private void RunMeshPilot()
        {
            var city = GameObject.Find("MotorCity_FCGCity");
            if (city == null)
            {
                Debug.LogWarning("Mesh pilot: enter the loaded city first.");
                return;
            }
            var expected = new Dictionary<string, (int vertices, int submeshes)>
            {
                { "BR-003", (13061, 3) }, { "EC-009", (12969, 3) },
                { "BR-002", (11586, 3) }, { "QD-14", (11580, 3) },
                { "BR-014", (11099, 3) }, { "BR-027", (11048, 3) },
                { "BC-P07", (10765, 4) }, { "BR-025", (10037, 3) },
                { "BC-053", (9810, 5) }, { "EC-006", (9974, 3) }
            };
            var protectedMeshes = new HashSet<Mesh>();
            foreach (var c in Resources.FindObjectsOfTypeAll<MeshCollider>())
                if (c != null && c.sharedMesh != null) protectedMeshes.Add(c.sharedMesh);
            foreach (var skin in Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>())
                if (skin != null && skin.sharedMesh != null) protectedMeshes.Add(skin.sharedMesh);
            var candidates = new Dictionary<string, HashSet<Mesh>>();
            foreach (var filter in city.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null) continue;
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable || protectedMeshes.Contains(mesh) ||
                    !expected.TryGetValue(mesh.name, out var spec) ||
                    mesh.vertexCount != spec.vertices || mesh.subMeshCount != spec.submeshes ||
                    filter.GetComponent<MeshRenderer>() == null ||
                    filter.GetComponentInParent<Rigidbody>(true) != null ||
                    filter.GetComponentInParent<Animator>(true) != null) continue;
                if (!candidates.TryGetValue(mesh.name, out var set))
                    candidates[mesh.name] = set = new HashSet<Mesh>();
                set.Add(mesh);
            }
            // Protect loaded animated/dynamic users as well as ambiguous matches.
            foreach (var filter in Resources.FindObjectsOfTypeAll<MeshFilter>())
            {
                if (filter == null || filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh;
                if (filter.GetComponentInParent<Rigidbody>(true) != null ||
                    filter.GetComponentInParent<Animator>(true) != null)
                    protectedMeshes.Add(mesh);
            }
            long before = 0, after = 0;
            int released = 0;
            foreach (var pair in candidates)
            {
                if (pair.Value.Count != 1) continue;
                foreach (var mesh in pair.Value)
                {
                    if (protectedMeshes.Contains(mesh) || !mesh.isReadable) continue;
                    try
                    {
                        long bytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(mesh);
                        mesh.UploadMeshData(true);
                        if (mesh.isReadable) continue;
                        before += bytes;
                        after += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(mesh);
                        released++;
                        Debug.Log("Mesh pilot released: " + mesh.name);
                    }
                    catch (Exception ex) { Debug.LogWarning("Mesh pilot skipped " + mesh.name + ": " + ex.Message); }
                }
            }
            meshPilotReleased += released;
            Debug.Log($"Mesh pilot: released={released}, cumulative={meshPilotReleased}, estimated mesh bytes before={before}, after={after}. Reload page to reset.");
        }

        private static string F(double x) => x.ToString("F3", CultureInfo.InvariantCulture);
        private double P(double p) => frames[Mathf.Clamp((int)Math.Ceiling(frames.Count * p) - 1, 0, frames.Count - 1)];

        private void Finish(bool complete)
        {
            recording = false;
            var s = new StringBuilder("MOTOR CITY WEBGL PERFORMANCE BASELINE\n");
            s.AppendLine(context);
            s.AppendLine($"Duration={F(Time.realtimeSinceStartupAsDouble - started)}s samples={frames.Count} endScreen={Screen.width}x{Screen.height} endPreset={MotorCityQualityRuntime.CurrentPreset}");
            if (frames.Count > 0)
            {
                frames.Sort();
                s.AppendLine($"Mean FPS={F(1000 / frames.Average())} frame ms mean={F(frames.Average())} median={F(P(.5))} p95={F(P(.95))} p99={F(P(.99))} max={F(frames.Last())}");
            }
            foreach (var c in counters)
            {
                string unit = c.Name.EndsWith("Thread") ? "ns" : c.Name.Contains("Memory") || c.Name.Contains("Allocated") ? "bytes" : "count";
                s.AppendLine(c.Name + ": " + (c.Samples == 0 || c.Peak == 0 ? "N/A (no nonzero samples)" : $"mean={F(c.Sum / c.Samples)} peak={c.Peak} {unit}"));
            }
            DisposeCounters();
            s.AppendLine("GPU time: N/A; Unity counters are not total browser memory.");
            s.AppendLine(complete ? "COMPLETE" : "INCOMPLETE");
            MotorCityBaselinePublish(s.ToString());
        }

        private void DisposeCounters()
        {
            foreach (var c in counters) if (c.Recorder.Valid) c.Recorder.Dispose();
            counters.Clear();
        }
        private void OnDestroy() => DisposeCounters();
    }
}
#endif
