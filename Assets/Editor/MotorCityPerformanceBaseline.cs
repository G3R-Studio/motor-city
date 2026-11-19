using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MotorCity.EditorTools
{
    [InitializeOnLoad]
    public static class MotorCityPerformanceBaseline
    {
        private const string Output = "Library/MotorCityPerformanceBaseline.txt";
        private static readonly List<double> frames = new();
        private static readonly List<Counter> counters = new();
        private static bool running;
        private static double start, last;
        private static int lastFrame;
        private static string context;
        private static ScriptableRendererFeature ssaoOverride;
        private static bool ssaoOriginal;

        private static ScriptableRendererFeature FindSsao()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) return null;
            var data = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
            if (data == null || data.arraySize != 1) return null;
            var renderer = data.GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData;
            if (renderer == null) return null;
            var matches = renderer.rendererFeatures.Where(f => f != null && f.GetType().FullName ==
                "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion").ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        [MenuItem("Tools/Motor City/Audit/SSAO Pilot OFF")]
        private static void SsaoOff() => SetSsao(false);
        [MenuItem("Tools/Motor City/Audit/SSAO Pilot ON")]
        private static void SsaoOn() => SetSsao(true);
        [MenuItem("Tools/Motor City/Audit/SSAO Pilot OFF", true)]
        [MenuItem("Tools/Motor City/Audit/SSAO Pilot ON", true)]
        private static bool CanSetSsao() => CanBegin();

        private static void SetSsao(bool active)
        {
            if (!CanSetSsao()) return;
            var feature = FindSsao();
            if (feature == null || (ssaoOverride != null && ssaoOverride != feature))
            {
                Debug.LogWarning("SSAO pilot: expected exactly one SSAO feature in the active single-renderer URP asset.");
                return;
            }
            if (ssaoOverride == null)
            {
                ssaoOverride = feature;
                ssaoOriginal = feature.isActive;
            }
            feature.SetActive(active);
            Debug.Log("SSAO pilot: " + (feature.isActive ? "ON" : "OFF") + "; verify passes in Frame Debugger.");
        }

        private static void RestoreSsao()
        {
            if (ssaoOverride != null) ssaoOverride.SetActive(ssaoOriginal);
            ssaoOverride = null;
        }

        private static string SsaoState()
        {
            var feature = FindSsao();
            return feature == null ? "N/A" : feature.isActive ? "ON" : "OFF";
        }

        private sealed class Counter
        {
            public string Name;
            public ProfilerRecorder Recorder;
            public long Sum, Peak;
            public int Samples;
        }

        static MotorCityPerformanceBaseline()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    if (running) Finish(false);
                    RestoreSsao();
                }
            };
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                if (running) Finish(false);
                RestoreSsao();
            };
            EditorApplication.quitting += RestoreSsao;
        }

        [MenuItem("Tools/Motor City/Audit/Start Performance Baseline (60s)")]
        private static void Begin()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || running) return;
            frames.Clear();
            counters.Clear();
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
            context = $"ssao={SsaoState()} preset={MotorCity.Platform.MotorCityQualityRuntime.CurrentPreset} renderScale={(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset)?.renderScale} lampInstancingRenderers={MotorCityLampInstancingPilot.Count} lodPilot={MotorCity.Platform.MotorCityBuildingLodPilot.Enabled} lodRenderers={MotorCity.Platform.MotorCityBuildingLodPilot.RendererCount} Unity={Application.unityVersion} target={EditorUserBuildSettings.activeBuildTarget} screen={Screen.width}x{Screen.height} qualityIndex={QualitySettings.GetQualityLevel()} targetFPS={Application.targetFrameRate} vSync={QualitySettings.vSyncCount} GPU={SystemInfo.graphicsDeviceName}";
            start = last = EditorApplication.timeSinceStartup;
            lastFrame = Time.frameCount;
            running = true;
            EditorApplication.update += Tick;
            Debug.Log("Motor City baseline started. Return focus to Game view and drive for 60 seconds.");
        }

        [MenuItem("Tools/Motor City/Audit/Start Performance Baseline (60s)", true)]
        private static bool CanBegin() => EditorApplication.isPlaying && !EditorApplication.isPaused && !running;

        [MenuItem("Tools/Motor City/Audit/Stop Performance Baseline")]
        private static void Stop()
        {
            if (running) Finish(false);
        }

        private static void Add(ProfilerCategory category, string name)
        {
            var counter = new Counter { Name = name };
            try { counter.Recorder = ProfilerRecorder.StartNew(category, name, 1); }
            catch (Exception) { }
            counters.Add(counter);
        }

        private static void Tick()
        {
            if (!running) return;
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            {
                Finish(false);
                return;
            }
            double now = EditorApplication.timeSinceStartup;
            int frame = Time.frameCount;
            if (frame != lastFrame)
            {
                // Update is an editor callback: sample the latest game frame,
                // not the elapsed interval between editor callbacks.
                double ms = Time.unscaledDeltaTime * 1000.0;
                if (ms > 0 && !double.IsNaN(ms) && !double.IsInfinity(ms))
                    frames.Add(ms);
                lastFrame = frame;
                foreach (var counter in counters)
                {
                    if (!counter.Recorder.Valid || counter.Recorder.Count == 0) continue;
                    long value = counter.Recorder.LastValue;
                    counter.Sum += value;
                    counter.Peak = Math.Max(counter.Peak, value);
                    counter.Samples++;
                }
            }
            last = now;
            if (now - start >= 60) Finish(true);
        }

        private static double Percentile(List<double> sorted, double p)
        {
            return sorted[Mathf.Clamp((int)Math.Ceiling(sorted.Count * p) - 1, 0, sorted.Count - 1)];
        }

        private static string F(double value) => value.ToString("F3", CultureInfo.InvariantCulture);

        private static void Finish(bool complete)
        {
            running = false;
            EditorApplication.update -= Tick;
            var report = new StringBuilder();
            report.AppendLine("MOTOR CITY PERFORMANCE BASELINE");
            report.AppendLine(DateTime.UtcNow.ToString("u"));
            report.AppendLine("EDITOR PLAY MODE ONLY. Not WebGL release performance. Latest-frame samples; editor callbacks may miss frames.");
            report.AppendLine(context);
            report.AppendLine($"Duration={F(EditorApplication.timeSinceStartup - start)}s samples={frames.Count}");
            if (frames.Count > 0)
            {
                frames.Sort();
                report.AppendLine($"Mean frame ms={F(frames.Average())} median={F(Percentile(frames, .5))} p95={F(Percentile(frames, .95))} p99={F(Percentile(frames, .99))} max={F(frames.Last())}");
                report.AppendLine($"Mean sampled FPS={F(1000 / frames.Average())}");
            }
            else report.AppendLine("Frame timing: N/A");
            foreach (var counter in counters)
            {
                string units = counter.Name.EndsWith("Thread") ? "ns" :
                    counter.Name.Contains("Memory") || counter.Name.Contains("Allocated") ? "bytes" : "count";
                report.AppendLine(counter.Name + ": " + (counter.Samples == 0 ? "N/A" :
                    $"mean={F((double)counter.Sum / counter.Samples)} peak={counter.Peak} {units} samples={counter.Samples}"));
                if (counter.Recorder.Valid) counter.Recorder.Dispose();
            }
            counters.Clear();
            report.AppendLine("GPU time: N/A (not measured)");
            report.AppendLine(complete ? "COMPLETE" : "INCOMPLETE (stopped early)");
            File.WriteAllText(Output, report.ToString(), Encoding.UTF8);
            string archive = Path.Combine("Library", "MotorCityPerformanceBaseline-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + ".txt");
            File.WriteAllText(archive, report.ToString(), Encoding.UTF8);
            Debug.Log("Archived baseline: " + archive);
            Debug.Log("Performance baseline: " + Output);
        }
    }
}
