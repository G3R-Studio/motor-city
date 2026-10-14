using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MotorCity.Platform;
using MotorCity.World;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    public static class MotorCityTreeCullingBenchmarkMenu
    {
        [MenuItem("Motor City/Optimization/15 - Benchmark Trees + Existing Optimizations (Play Mode)")]
        private static void Run()
        {
            if (!Application.isPlaying || EditorApplication.isPaused)
            {
                Debug.LogWarning("[MotorCity][TreeBenchmark] Start Play Mode " +
                    "and unpause before running this menu.");
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<
                    MotorCityTreeCullingBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityMovingCameraBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityVegetationFourWayBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityCombinedCullingBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCity.Diagnostics.MotorCityParkedVehicleBenchmark>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityVegetationOptimizationBenchmarkRunner>(
                    FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("[MotorCity][TreeBenchmark] Another benchmark " +
                    "is running. Wait for the previous report.");
                return;
            }

            GameObject city = GameObject.Find("MotorCity_FCGCity");
            Camera camera = Camera.main;
            if (city == null || camera == null)
            {
                Debug.LogWarning("[MotorCity][TreeBenchmark] Wait until the " +
                    "playable city and main camera are loaded.");
                return;
            }

            if (MotorCityQualityRuntime.CurrentPreset !=
                MotorCityQualityPreset.High)
            {
                Debug.LogWarning("[MotorCity][TreeBenchmark] Set High quality " +
                    "first; do not change traffic budgets during benchmarking.");
                return;
            }

            var host = new GameObject(
                "MotorCity - Tree + Existing A-B Benchmark");
            var runner = host.AddComponent<MotorCityTreeCullingBenchmarkRunner>();
            runner.Begin(city, camera);
        }
    }

    /// <summary>
    /// 4 phases, ABBA: successful detail+parked base vs detail+parked+
    /// experimental distant Trees-01. Offline Editor only, no build assets.
    /// </summary>
    public sealed class MotorCityTreeCullingBenchmarkRunner : MonoBehaviour
    {
        private const string Output =
            "Temp/MotorCity_TreeCullingBenchmark.txt";
        private const float WarmupSeconds = 3f;
        private const float SampleSeconds = 12f;
        private const float ScanTimeoutSeconds = 35f;
        private const float MaximumCameraMove = 2f;
        private const float MaximumCameraTurn = 4f;

        private readonly List<float> baseFrames = new List<float>(5000);
        private readonly List<float> treeFrames = new List<float>(5000);
        private readonly StringBuilder report = new StringBuilder(5000);
        private static readonly bool[] Sequence =
        {
            false, true, true, false
        };

        private Camera observer;
        private CityDistanceDetailCuller details;
        private CityParkedVehicleCullingExperiment parked;
        private CityDistantTreeCullingExperiment trees;
        private CityFoliageCullingExperiment foliage;
        private CityHlodRuntime hlod;

        private bool hadDetails;
        private bool hadParked;
        private bool hadTrees;
        private bool detailsOriginallyEnabled;
        private bool parkedOriginallyEnabled;
        private bool treesOriginallyEnabled;
        private bool woodyOriginallyEnabled;
        private bool foliageOriginallyEnabled;
        private bool hlodOriginallyEnabled;
        private bool previewOriginallyEnabled;
        private bool initialized;
        private bool restored;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private string problem;

        public void Begin(GameObject city, Camera camera)
        {
            observer = camera;
            startPosition = camera.transform.position;
            startRotation = camera.transform.rotation;
            previewOriginallyEnabled =
                MotorCityVisibilityRuntime.BrowserProfileActive;

            details = UnityEngine.Object.FindFirstObjectByType<
                CityDistanceDetailCuller>(FindObjectsInactive.Include);
            parked = UnityEngine.Object.FindFirstObjectByType<
                CityParkedVehicleCullingExperiment>(FindObjectsInactive.Include);
            trees = UnityEngine.Object.FindFirstObjectByType<
                CityDistantTreeCullingExperiment>(FindObjectsInactive.Include);
            foliage = UnityEngine.Object.FindFirstObjectByType<
                CityFoliageCullingExperiment>(FindObjectsInactive.Include);
            hlod = UnityEngine.Object.FindFirstObjectByType<
                CityHlodRuntime>(FindObjectsInactive.Include);

            hadDetails = details != null;
            hadParked = parked != null;
            hadTrees = trees != null;
            detailsOriginallyEnabled = hadDetails && details.enabled;
            parkedOriginallyEnabled = hadParked && parked.enabled;
            treesOriginallyEnabled = hadTrees && trees.enabled;
            woodyOriginallyEnabled = hadTrees && trees.IncludeWoody;
            foliageOriginallyEnabled = foliage != null && foliage.enabled;
            hlodOriginallyEnabled = hlod != null && hlod.enabled;

            initialized = true;
            if (details != null) details.enabled = false;
            if (parked != null) parked.enabled = false;
            if (trees != null) trees.enabled = false;
            if (foliage != null) foliage.enabled = false;
            if (hlod != null) hlod.enabled = false;

            if (!previewOriginallyEnabled)
                MotorCityVisibilityRuntime.SetEditorPreview(true);

            if (details == null)
            {
                var host = new GameObject("MotorCity - Tree Test Details");
                details = host.AddComponent<CityDistanceDetailCuller>();
                details.enabled = false;
            }
            if (parked == null)
            {
                var host = new GameObject("MotorCity - Tree Test Parked");
                parked = host.AddComponent<CityParkedVehicleCullingExperiment>();
                parked.enabled = false;
            }
            if (trees == null)
            {
                var host = new GameObject("MotorCity - Tree Test Trees");
                trees = host.AddComponent<CityDistantTreeCullingExperiment>();
                trees.enabled = false;
            }

            // Discovery occurs with all other cullers disabled, so no
            // candidate gets lost due to another culler's visibility flag.
            details.Initialize(city, camera);
            parked.Initialize(city, camera);
            trees.Initialize(city, camera);
            trees.IncludeWoody = true;

            report.AppendLine("MotorCity distant Trees-01 incremental benchmark");
            report.AppendLine("Unity Editor only: no WebGL measurements.");
            report.AppendLine("Preset=" + MotorCityQualityRuntime.CurrentPreset +
                ", position=" + startPosition.ToString("F2") +
                ", rotation=" + startRotation.eulerAngles.ToString("F1"));
            report.AppendLine("Camera far=" +
                camera.farClipPlane.ToString("F0") +
                "m, fog end=" +
                RenderSettings.fogEndDistance.ToString("F0") + "m.");
            report.AppendLine("BASE: tiny details + parked vehicle culling.");
            report.AppendLine("TREES: BASE plus projected-size tree culling.");
            report.AppendLine("ABBA: BASE, TREES, TREES, BASE.");
            report.AppendLine("3s warmup then 12s sample per phase.");
            report.AppendLine();

            Debug.Log("[MotorCity][TreeBenchmark] Starting automatic " +
                "60-second A/B test after initial scans. Do not move " +
                "or rotate camera; keep Game view focused. Output: " + Output);
            StartCoroutine(Measure());
        }

        private IEnumerator Measure()
        {
            try
            {
                // Sequential, fully scanned initialization of each system.
                // Culler discovery skips forceRenderingOff meshes.
                parked.enabled = true;
                yield return WaitReady(
                    () => parked != null && parked.ScanReady,
                    "parked vehicle");
                parked.enabled = false;

                if (problem == null)
                {
                    details.enabled = true;
                    yield return WaitReady(
                        () => details != null && details.ScanReady,
                        "small details");
                    details.enabled = false;
                }

                if (problem == null)
                {
                    trees.enabled = true;
                    yield return WaitReady(
                        () => trees != null && trees.ScanReady,
                        "trees");
                    trees.enabled = false;
                }

                if (problem == null && trees.CandidateCount == 0)
                    problem = "No eligible Trees-01 renderers. No comparison possible.";

                if (problem == null)
                {
                    report.AppendLine("Eligible tree renderers=" +
                        trees.CandidateCount + ", triangle references=" +
                        trees.EligibleTriangleReferences);
                    report.AppendLine("Eligible small detail renderers=" +
                        details.CandidateCount + ".");
                    report.AppendLine("Eligible parked vehicle groups=" +
                        parked.VehicleCount + ".");
                    report.AppendLine();

                    for (int phase = 0; phase < Sequence.Length &&
                        problem == null; phase++)
                    {
                        bool treeOn = Sequence[phase];
                        SetMode(treeOn);

                        double warmup = Time.realtimeSinceStartupAsDouble;
                        while (Time.realtimeSinceStartupAsDouble - warmup <
                            WarmupSeconds)
                        {
                            if (!ValidateScene())
                                break;
                            yield return null;
                        }
                        if (problem != null)
                            break;

                        int hiddenDetails = details.HiddenCount;
                        int hiddenCars = parked.HiddenVehicles;
                        int hiddenTrees = trees.HiddenCount;
                        long hiddenTreeTris = trees.HiddenTriangleReferences;
                        var frames = new List<float>(2500);
                        double sampledAt = Time.realtimeSinceStartupAsDouble;

                        while (Time.realtimeSinceStartupAsDouble - sampledAt <
                            SampleSeconds)
                        {
                            if (!ValidateScene())
                                break;
                            float milliseconds =
                                Time.unscaledDeltaTime * 1000f;
                            if (milliseconds > 0f)
                                frames.Add(milliseconds);
                            yield return null;
                        }
                        if (problem != null)
                            break;

                        (treeOn ? treeFrames : baseFrames).AddRange(frames);
                        string label = treeOn ? "TREES ON" : "BASE";
                        string phaseResult = "Phase " + (phase + 1) +
                            "/4 " + label + ": " +
                            Describe(frames) + "; hidden details=" +
                            hiddenDetails + ", hidden parked=" + hiddenCars +
                            ", hidden trees=" + hiddenTrees + ", tree tris=" +
                            hiddenTreeTris;
                        report.AppendLine(phaseResult);
                        Debug.Log("[MotorCity][TreeBenchmark] " + phaseResult);
                    }
                }

                if (problem != null)
                {
                    report.AppendLine("ABORTED: " + problem);
                    Debug.LogWarning("[MotorCity][TreeBenchmark] ABORTED: " +
                        problem);
                }
                else if (baseFrames.Count < 100 || treeFrames.Count < 100)
                {
                    report.AppendLine("ABORTED: not enough frame samples.");
                    Debug.LogWarning("[MotorCity][TreeBenchmark] Too few samples.");
                }
                else
                {
                    double baseline = Mean(baseFrames);
                    double optimized = Mean(treeFrames);
                    double pct = baseline > 0d
                        ? (baseline - optimized) / baseline * 100d : 0d;
                    report.AppendLine();
                    report.AppendLine("=== FINAL (two 12s samples per mode) ===");
                    report.AppendLine("BASE (details + parked): " +
                        Describe(baseFrames));
                    report.AppendLine("TREES (base + distant trees): " +
                        Describe(treeFrames));
                    report.AppendLine("Mean frame-time improvement: " +
                        pct.ToString("+0.0;-0.0;0.0") +
                        "% (positive means TREES is faster).");
                    report.AppendLine("This is Editor frame time, not " +
                        "browser FPS or GPU time. Inspect skyline for tree pop-in.");
                    Debug.Log("[MotorCity][TreeBenchmark] FINAL\n" +
                        report.ToString());
                }

                try
                {
                    Directory.CreateDirectory("Temp");
                    File.WriteAllText(Output, report.ToString(), Encoding.UTF8);
                    Debug.Log("[MotorCity][TreeBenchmark] Report saved: " +
                        Output + ". Send TXT instead of screenshots.");
                }
                catch (Exception error)
                {
                    Debug.LogError("[MotorCity][TreeBenchmark] Report " +
                        "write failed: " + error.Message);
                }
            }
            finally
            {
                RestoreOriginalState();
                Destroy(gameObject);
            }
        }

        private IEnumerator WaitReady(Func<bool> predicate, string label)
        {
            double began = Time.realtimeSinceStartupAsDouble;
            while (!predicate())
            {
                if (!ValidateScene())
                    yield break;
                if (Time.realtimeSinceStartupAsDouble - began >
                    ScanTimeoutSeconds)
                {
                    problem = label + " discovery exceeded 35 seconds";
                    yield break;
                }
                yield return null;
            }
        }

        private void SetMode(bool treeOn)
        {
            // Release flags in a known order, then apply disjoint ownership.
            details.enabled = false;
            parked.enabled = false;
            trees.enabled = false;

            details.enabled = true;
            details.RequestRefresh();
            parked.enabled = true;
            parked.RequestRefresh();

            if (treeOn)
            {
                trees.enabled = true;
                trees.RequestRefresh();
            }
        }

        private bool ValidateScene()
        {
            if (observer == null || !observer.isActiveAndEnabled)
                problem = "main camera unavailable";
            else if (Vector3.Distance(startPosition,
                     observer.transform.position) > MaximumCameraMove ||
                     Quaternion.Angle(startRotation,
                     observer.transform.rotation) > MaximumCameraTurn)
                problem = "camera moved more than 2m or turned more than 4 degrees";
            else if (MotorCityQualityRuntime.CurrentPreset !=
                     MotorCityQualityPreset.High)
                problem = "quality preset changed from High";
            else if (!MotorCityVisibilityRuntime.BrowserProfileActive)
                problem = "browser visibility preview was turned off";
            return problem == null;
        }

        private static double Mean(List<float> ms)
        {
            if (ms.Count == 0)
                return 0d;
            double total = 0d;
            foreach (float value in ms)
                total += value;
            return total / ms.Count;
        }

        private static float Percentile(List<float> ms, float portion)
        {
            if (ms.Count == 0)
                return 0f;
            float[] copy = ms.ToArray();
            Array.Sort(copy);
            int index = Mathf.Clamp(
                Mathf.CeilToInt(copy.Length * portion) - 1,
                0, copy.Length - 1);
            return copy[index];
        }

        private static string Describe(List<float> frames)
        {
            double mean = Mean(frames);
            int over16 = frames.Count(x => x > 16.67f);
            int over25 = frames.Count(x => x > 25f);
            int over50 = frames.Count(x => x > 50f);
            return frames.Count + " frames, mean=" +
                mean.ToString("F2") + "ms, derived FPS=" +
                (mean > 0 ? 1000d / mean : 0d).ToString("F1") +
                ", median=" + Percentile(frames, .5f).ToString("F2") +
                "ms, p95=" + Percentile(frames, .95f).ToString("F2") +
                "ms, p99=" + Percentile(frames, .99f).ToString("F2") +
                "ms, >16.67ms=" + over16 +
                ", >25ms=" + over25 + ", >50ms=" + over50;
        }

        private void RestoreOriginalState()
        {
            if (!initialized || restored)
                return;
            restored = true;

            if (trees != null) trees.enabled = false;
            if (details != null) details.enabled = false;
            if (parked != null) parked.enabled = false;

            if (!hadTrees && trees != null)
                Destroy(trees.gameObject);
            else if (trees != null)
            {
                trees.IncludeWoody = woodyOriginallyEnabled;
                trees.enabled = treesOriginallyEnabled;
                if (treesOriginallyEnabled)
                    trees.RequestRefresh();
            }

            if (!hadDetails && details != null)
                Destroy(details.gameObject);
            else if (details != null)
            {
                details.enabled = detailsOriginallyEnabled;
                if (detailsOriginallyEnabled)
                    details.RequestRefresh();
            }

            if (!hadParked && parked != null)
                Destroy(parked.gameObject);
            else if (parked != null)
            {
                parked.enabled = parkedOriginallyEnabled;
                if (parkedOriginallyEnabled)
                    parked.RequestRefresh();
            }

            if (foliage != null)
                foliage.enabled = foliageOriginallyEnabled;
            if (hlod != null)
                hlod.enabled = hlodOriginallyEnabled;
            if (!previewOriginallyEnabled)
                MotorCityVisibilityRuntime.SetEditorPreview(false);
        }

        private void OnDisable() => RestoreOriginalState();
        private void OnDestroy() => RestoreOriginalState();
    }
}
