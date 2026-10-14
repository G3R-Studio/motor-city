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
    /// <summary>
    /// One-click, editor-only, reversible four-mode rendering benchmark.
    /// No WebGL build, prefabs, mesh files, materials or gameplay edits.
    /// </summary>
    public static class MotorCityCombinedCullingBenchmarkMenu
    {
        [MenuItem("Motor City/Optimization/14 - Run All Culling A-B Tests (Play Mode)")]
        private static void StartTest()
        {
            if (!Application.isPlaying || EditorApplication.isPaused)
            {
                Debug.LogWarning("[MotorCity][CombinedBenchmark] Enter Play Mode, " +
                    "unpause and wait for the city to load, then run menu 14.");
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<
                    MotorCityCombinedCullingBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCity.Diagnostics.MotorCityParkedVehicleBenchmark>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityTreeCullingBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityMovingCameraBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityVegetationFourWayBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityVegetationOptimizationBenchmarkRunner>(
                    FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("[MotorCity][CombinedBenchmark] Another " +
                    "benchmark is already running; wait for it to finish.");
                return;
            }

            GameObject city = GameObject.Find("MotorCity_FCGCity");
            Camera camera = Camera.main;
            if (city == null || camera == null)
            {
                Debug.LogWarning("[MotorCity][CombinedBenchmark] City or " +
                    "Camera.main not loaded. Wait until driving starts.");
                return;
            }

            if (MotorCityQualityRuntime.CurrentPreset !=
                MotorCityQualityPreset.High)
            {
                Debug.LogWarning("[MotorCity][CombinedBenchmark] Select " +
                    "High quality first. Changing live quality by script " +
                    "could change traffic budgets and bias the results.");
                return;
            }

            var host = new GameObject(
                "MotorCity - Combined A-B Benchmark (temporary)");
            var runner =
                host.AddComponent<MotorCityCombinedCullingBenchmarkRunner>();
            runner.Begin(city, camera);
        }
    }

    /// <summary>
    /// Repeats all four renderer modes in forward/reverse order:
    /// baseline, details, parked, both, both, parked, details, baseline.
    /// Reports aggregated frame times (not one-off FPS screen snapshots).
    /// </summary>
    public sealed class MotorCityCombinedCullingBenchmarkRunner : MonoBehaviour
    {
        private const float WarmupSeconds = 2f;
        private const float MeasureSeconds = 8f;
        private const float ScanTimeoutSeconds = 35f;
        private const float MaximumCameraMove = 2f;
        private const float MaximumCameraTurn = 4f;
        private const string ReportFile =
            "Temp/MotorCity_CombinedCullingBenchmark.txt";

        private static readonly int[] ModeSequence = { 0, 1, 2, 3, 3, 2, 1, 0 };
        private static readonly string[] ModeNames =
        {
            "BASELINE (both off)",
            "DETAILS only",
            "PARKED CARS only",
            "BOTH enabled"
        };

        private readonly List<float>[] samples =
        {
            new List<float>(3200),
            new List<float>(3200),
            new List<float>(3200),
            new List<float>(3200)
        };

        private readonly StringBuilder report = new StringBuilder(6000);
        private CityDistanceDetailCuller details;
        private CityParkedVehicleCullingExperiment parked;
        private CityFoliageCullingExperiment foliage;
        private CityHlodRuntime hlod;
        private Camera observer;
        private bool ownsDetails;
        private bool ownsParked;
        private bool wasDetailsEnabled;
        private bool wasParkedEnabled;
        private bool wasFoliageEnabled;
        private bool wasHlodEnabled;
        private bool wasBrowserPreview;
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
            wasBrowserPreview = MotorCityVisibilityRuntime.BrowserProfileActive;

            details = UnityEngine.Object.FindFirstObjectByType<
                CityDistanceDetailCuller>(FindObjectsInactive.Include);
            parked = UnityEngine.Object.FindFirstObjectByType<
                CityParkedVehicleCullingExperiment>(FindObjectsInactive.Include);
            foliage = UnityEngine.Object.FindFirstObjectByType<
                CityFoliageCullingExperiment>(FindObjectsInactive.Include);
            hlod = UnityEngine.Object.FindFirstObjectByType<
                CityHlodRuntime>(FindObjectsInactive.Include);

            wasDetailsEnabled = details != null && details.enabled;
            wasParkedEnabled = parked != null && parked.enabled;
            wasFoliageEnabled = foliage != null && foliage.enabled;
            wasHlodEnabled = hlod != null && hlod.enabled;

            initialized = true;
            if (details != null) details.enabled = false;
            if (parked != null) parked.enabled = false;
            if (foliage != null) foliage.enabled = false;
            if (hlod != null) hlod.enabled = false;

            if (!wasBrowserPreview)
                MotorCityVisibilityRuntime.SetEditorPreview(true);

            // Scan each experiment separately so neither discovers meshes
            // hidden by the other. Existing instances also get a fresh scan.
            if (details == null)
            {
                ownsDetails = true;
                var host = new GameObject("MotorCity - Benchmark Details");
                details = host.AddComponent<CityDistanceDetailCuller>();
                details.enabled = false;
            }
            if (parked == null)
            {
                ownsParked = true;
                var host = new GameObject("MotorCity - Benchmark Parked Cars");
                parked = host.AddComponent<CityParkedVehicleCullingExperiment>();
                parked.enabled = false;
            }

            details.Initialize(city, camera);
            parked.Initialize(city, camera);

            report.AppendLine("MotorCity combined rendering A/B (Unity Editor only)");
            report.AppendLine("Camera position: " + startPosition.ToString("F2"));
            report.AppendLine("Camera rotation: " + startRotation.eulerAngles.ToString("F1"));
            report.AppendLine("Preset: " + MotorCityQualityRuntime.CurrentPreset);
            report.AppendLine("Camera far: " + camera.farClipPlane.ToString("F0") +
                " m, fog end: " + RenderSettings.fogEndDistance.ToString("F0") + " m");
            report.AppendLine("Sequence: OFF/OFF, DETAILS, PARKED, BOTH, BOTH, " +
                "PARKED, DETAILS, OFF/OFF.");
            report.AppendLine("2 seconds warmup + 8 seconds measurement per phase.");
            report.AppendLine("Frame intervals, NOT WebGL or GPU-specific timings.");
            report.AppendLine();

            Debug.Log("[MotorCity][CombinedBenchmark] Starting automated " +
                "8-phase test (~80-95 seconds). Camera and Game view must " +
                "stay STILL and focused. Details/parked/combined/baseline " +
                "will be switched automatically. Output: " + ReportFile);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            try
            {
                // Only park is enabled while its 25k-renderer scan runs.
                parked.enabled = true;
                yield return WaitForScan(() => parked != null && parked.ScanReady,
                    "parked vehicle");
                if (problem == null)
                {
                    parked.enabled = false;
                    details.enabled = true;
                    yield return WaitForScan(() => details != null && details.ScanReady,
                        "distant details");
                    if (problem == null)
                        details.enabled = false;
                }

                if (problem == null)
                {
                    report.AppendLine("Eligible parked groups: " +
                        parked.VehicleCount + ", triangle references=" +
                        parked.EligibleTriangleReferences);
                    report.AppendLine("Eligible distant small meshes: " +
                        details.CandidateCount);
                    report.AppendLine();

                    for (int phase = 0; phase < ModeSequence.Length &&
                        problem == null; phase++)
                    {
                        int mode = ModeSequence[phase];
                        SetMode(mode);

                        double warmupStart = Time.realtimeSinceStartupAsDouble;
                        while (Time.realtimeSinceStartupAsDouble -
                               warmupStart < WarmupSeconds)
                        {
                            if (!ValidateCameraAndPreset())
                                break;
                            yield return null;
                        }
                        if (problem != null)
                            break;

                        int hiddenParked = parked.HiddenVehicles;
                        long hiddenParkedTriangles =
                            parked.HiddenTriangleReferences;
                        int hiddenDetail = details.HiddenCount;
                        long hiddenDetailTriangles =
                            details.HiddenTriangleReferences;

                        var phaseSamples = new List<float>(1600);
                        double sampleStart = Time.realtimeSinceStartupAsDouble;
                        while (Time.realtimeSinceStartupAsDouble -
                               sampleStart < MeasureSeconds)
                        {
                            if (!ValidateCameraAndPreset())
                                break;

                            float frameMs = Time.unscaledDeltaTime * 1000f;
                            if (frameMs > 0f)
                                phaseSamples.Add(frameMs);
                            yield return null;
                        }
                        if (problem != null)
                            break;

                        samples[mode].AddRange(phaseSamples);
                        string phaseText = "Phase " + (phase + 1) + "/8 " +
                            ModeNames[mode] + ": " + phaseSamples.Count +
                            " frames, mean=" + Mean(phaseSamples).ToString("F2") +
                            "ms, p95=" + Percentile(phaseSamples, 0.95f)
                                .ToString("F2") + "ms; hidden details=" +
                            hiddenDetail + " (" + hiddenDetailTriangles +
                            " triangle refs), hidden parked=" + hiddenParked +
                            " (" + hiddenParkedTriangles + " triangle refs)";
                        report.AppendLine(phaseText);
                        Debug.Log("[MotorCity][CombinedBenchmark] " + phaseText);
                    }
                }

                if (problem != null)
                {
                    report.AppendLine("ABORTED: " + problem);
                    Debug.LogWarning("[MotorCity][CombinedBenchmark] ABORTED: " +
                        problem);
                }
                else
                {
                    report.AppendLine();
                    report.AppendLine("=== FINAL aggregated two samples/mode ===");
                    for (int mode = 0; mode < samples.Length; mode++)
                        report.AppendLine(ModeNames[mode] + ": " +
                            Describe(samples[mode]));

                    double baseline = Mean(samples[0]);
                    for (int mode = 1; mode < samples.Length; mode++)
                    {
                        double current = Mean(samples[mode]);
                        double improvement = baseline > 0d ?
                            (baseline - current) / baseline * 100d : 0d;
                        report.AppendLine(ModeNames[mode] +
                            " vs baseline mean frame time: " +
                            improvement.ToString("+0.0;-0.0;0.0") + "%");
                    }

                    double expected = Mean(samples[0]) - Mean(samples[1]) -
                        Mean(samples[2]) + Mean(samples[3]);
                    report.AppendLine("Combined interaction (ms, negative " +
                        "means combined saves extra frame time): " +
                        expected.ToString("+0.000;-0.000;0.000"));
                    report.AppendLine("Individual 8s phases can vary due to " +
                        "Editor load. Judge mean, p95/p99 and visual quality " +
                        "together; WebGL requires a final separate test.");
                    Debug.Log("[MotorCity][CombinedBenchmark] FINAL\n" +
                        report.ToString());
                }

                try
                {
                    Directory.CreateDirectory("Temp");
                    File.WriteAllText(ReportFile, report.ToString(),
                        Encoding.UTF8);
                    Debug.Log("[MotorCity][CombinedBenchmark] Report saved: " +
                        ReportFile + ". Send this TXT file, not screenshots.");
                }
                catch (Exception error)
                {
                    Debug.LogError("[MotorCity][CombinedBenchmark] Could not " +
                        "write report: " + error.Message);
                }
            }
            finally
            {
                RestoreEverything();
                Destroy(gameObject);
            }
        }

        private IEnumerator WaitForScan(Func<bool> isReady, string label)
        {
            double start = Time.realtimeSinceStartupAsDouble;
            while (!isReady())
            {
                if (!ValidateCameraAndPreset())
                    yield break;
                if (Time.realtimeSinceStartupAsDouble - start >
                    ScanTimeoutSeconds)
                {
                    problem = label + " scan timeout (35 seconds)";
                    yield break;
                }
                yield return null;
            }
        }

        private void SetMode(int mode)
        {
            // Release both groups first, then request a full pass for the
            // selected culler(s). Guard ownership at discovery (car wheels
            // are excluded from CityDistanceDetailCuller).
            details.enabled = false;
            parked.enabled = false;
            if ((mode & 1) != 0)
            {
                details.enabled = true;
                details.RequestRefresh();
            }
            if ((mode & 2) != 0)
            {
                parked.enabled = true;
                parked.RequestRefresh();
            }
        }

        private bool ValidateCameraAndPreset()
        {
            if (observer == null ||
                !observer.isActiveAndEnabled)
                problem = "Main camera disabled/destroyed";
            else if (Vector3.Distance(startPosition,
                    observer.transform.position) > MaximumCameraMove ||
                     Quaternion.Angle(startRotation,
                         observer.transform.rotation) > MaximumCameraTurn)
                problem = "Camera moved >2m or rotated >4 degrees";
            else if (MotorCityQualityRuntime.CurrentPreset !=
                     MotorCityQualityPreset.High)
                problem = "Quality preset changed during test";
            else if (!MotorCityVisibilityRuntime.BrowserProfileActive)
                problem = "Browser visibility preview changed during test";
            return problem == null;
        }

        private static double Mean(List<float> frames)
        {
            if (frames.Count == 0)
                return 0;
            double total = 0;
            foreach (float frame in frames) total += frame;
            return total / frames.Count;
        }

        private static float Percentile(List<float> frames, float portion)
        {
            if (frames.Count == 0)
                return 0;
            float[] sorted = frames.ToArray();
            Array.Sort(sorted);
            int index = Mathf.Clamp(
                Mathf.CeilToInt(sorted.Length * portion) - 1, 0,
                sorted.Length - 1);
            return sorted[index];
        }

        private static string Describe(List<float> frames)
        {
            double ms = Mean(frames);
            int slow25 = frames.Count(f => f > 25f);
            int slow50 = frames.Count(f => f > 50f);
            return frames.Count + " frames, mean=" +
                ms.ToString("F2") + "ms, derived FPS=" +
                (ms > 0 ? 1000.0 / ms : 0.0).ToString("F1") +
                ", median=" + Percentile(frames, .5f).ToString("F2") +
                "ms, p95=" + Percentile(frames, .95f).ToString("F2") +
                "ms, p99=" + Percentile(frames, .99f).ToString("F2") +
                "ms, >25ms=" + slow25 + ", >50ms=" + slow50;
        }

        private void RestoreEverything()
        {
            if (!initialized || restored)
                return;
            restored = true;

            // Never modify authored city components. We only restore the
            // toggles that existed before the experiment, or destroy hosts
            // created by this temporary benchmark.
            if (details != null) details.enabled = false;
            if (parked != null) parked.enabled = false;

            if (ownsDetails && details != null)
                Destroy(details.gameObject);
            else if (details != null)
            {
                details.enabled = wasDetailsEnabled;
                if (wasDetailsEnabled) details.RequestRefresh();
            }

            if (ownsParked && parked != null)
                Destroy(parked.gameObject);
            else if (parked != null)
            {
                parked.enabled = wasParkedEnabled;
                if (wasParkedEnabled) parked.RequestRefresh();
            }

            if (foliage != null) foliage.enabled = wasFoliageEnabled;
            if (hlod != null) hlod.enabled = wasHlodEnabled;

            if (!wasBrowserPreview)
                MotorCityVisibilityRuntime.SetEditorPreview(false);
        }

        private void OnDisable() => RestoreEverything();
        private void OnDestroy() => RestoreEverything();
    }
}
