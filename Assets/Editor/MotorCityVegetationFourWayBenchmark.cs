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
    public static class MotorCityVegetationFourWayBenchmarkMenu
    {
        [MenuItem("Motor City/Optimization/18 - Benchmark Four Vegetation Modes (Play Mode)")]
        private static void Run()
        {
            if (!Application.isPlaying || EditorApplication.isPaused)
            {
                Debug.LogWarning("[MotorCity][Vegetation4Way] Enter unpaused " +
                    "Play Mode first.");
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<
                    MotorCityVegetationFourWayBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityVegetationOptimizationBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityTreeCullingBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityCombinedCullingBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCity.Diagnostics.MotorCityParkedVehicleBenchmark>(
                    FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("[MotorCity][Vegetation4Way] " +
                    "Another automatic benchmark is running.");
                return;
            }

            GameObject city = GameObject.Find("MotorCity_FCGCity");
            Camera camera = Camera.main;
            if (city == null || camera == null)
            {
                Debug.LogWarning("[MotorCity][Vegetation4Way] " +
                    "Wait for the playable city and main camera.");
                return;
            }

            if (MotorCityQualityRuntime.CurrentPreset !=
                MotorCityQualityPreset.High)
            {
                Debug.LogWarning("[MotorCity][Vegetation4Way] " +
                    "Set graphics preset High before the test.");
                return;
            }

            var host = new GameObject(
                "MotorCity - Vegetation Four-Mode Benchmark");
            var runner =
                host.AddComponent<MotorCityVegetationFourWayBenchmarkRunner>();
            runner.Begin(city, camera);
        }
    }

    /// <summary>
    /// Four variants, measured twice in a mirrored order:
    /// baseline, ground cover only, trees/palms only, and both categories.
    /// No changes to colliders, city prefab, or build settings.
    /// </summary>
    public sealed class MotorCityVegetationFourWayBenchmarkRunner :
        MonoBehaviour
    {
        private const string Output =
            "Temp/MotorCity_VegetationFourWayBenchmark.txt";
        private const float WarmupSeconds = 3f;
        private const float SampleSeconds = 10f;
        private const float ScanTimeoutSeconds = 35f;
        private const float MaxPositionDrift = 2f;
        private const float MaxRotationDrift = 4f;

        private static readonly int[] Sequence = { 0, 1, 2, 3, 3, 2, 1, 0 };
        private static readonly string[] Labels =
        {
            "BASE (details + parked)",
            "GROUND (base + ferns/plants/vegetation)",
            "WOODY (base + trees/palms)",
            "ALL (ground + woody)"
        };

        private readonly List<float>[] measurements =
        {
            new List<float>(4000),
            new List<float>(4000),
            new List<float>(4000),
            new List<float>(4000)
        };
        private readonly StringBuilder report = new StringBuilder(6000);

        private Camera observer;
        private CityDistanceDetailCuller details;
        private CityParkedVehicleCullingExperiment parked;
        private CityDistantTreeCullingExperiment vegetation;
        private CityFoliageCullingExperiment foliage;
        private CityHlodRuntime hlod;

        private bool createdDetails;
        private bool createdParked;
        private bool createdVegetation;
        private bool initialDetailsEnabled;
        private bool initialParkedEnabled;
        private bool initialVegetationEnabled;
        private bool initialIncludeGroundCover;
        private bool initialIncludeWoody;
        private bool initialFoliageEnabled;
        private bool initialHlodEnabled;
        private bool initialBrowserPreview;
        private bool initialized;
        private bool restored;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private string error;

        public void Begin(GameObject city, Camera camera)
        {
            observer = camera;
            initialPosition = camera.transform.position;
            initialRotation = camera.transform.rotation;
            initialBrowserPreview =
                MotorCityVisibilityRuntime.BrowserProfileActive;

            details = UnityEngine.Object.FindFirstObjectByType<
                CityDistanceDetailCuller>(FindObjectsInactive.Include);
            parked = UnityEngine.Object.FindFirstObjectByType<
                CityParkedVehicleCullingExperiment>(FindObjectsInactive.Include);
            vegetation = UnityEngine.Object.FindFirstObjectByType<
                CityDistantTreeCullingExperiment>(FindObjectsInactive.Include);
            foliage = UnityEngine.Object.FindFirstObjectByType<
                CityFoliageCullingExperiment>(FindObjectsInactive.Include);
            hlod = UnityEngine.Object.FindFirstObjectByType<
                CityHlodRuntime>(FindObjectsInactive.Include);

            initialDetailsEnabled = details != null && details.enabled;
            initialParkedEnabled = parked != null && parked.enabled;
            initialVegetationEnabled =
                vegetation != null && vegetation.enabled;
            initialIncludeGroundCover =
                vegetation != null && vegetation.IncludeGroundCover;
            initialIncludeWoody =
                vegetation != null && vegetation.IncludeWoody;
            initialFoliageEnabled = foliage != null && foliage.enabled;
            initialHlodEnabled = hlod != null && hlod.enabled;

            initialized = true;

            // Isolate candidate discovery: forceRenderingOff belonging to
            // other experiments would cause silent missing candidates.
            if (details != null) details.enabled = false;
            if (parked != null) parked.enabled = false;
            if (vegetation != null) vegetation.enabled = false;
            if (foliage != null) foliage.enabled = false;
            if (hlod != null) hlod.enabled = false;

            if (!initialBrowserPreview)
                MotorCityVisibilityRuntime.SetEditorPreview(true);

            if (details == null)
            {
                createdDetails = true;
                GameObject helper = new GameObject(
                    "MotorCity - Vegetation Test Tiny Detail");
                details = helper.AddComponent<CityDistanceDetailCuller>();
                details.enabled = false;
            }

            if (parked == null)
            {
                createdParked = true;
                GameObject helper = new GameObject(
                    "MotorCity - Vegetation Test Parked Cars");
                parked = helper.AddComponent<CityParkedVehicleCullingExperiment>();
                parked.enabled = false;
            }

            if (vegetation == null)
            {
                createdVegetation = true;
                GameObject helper = new GameObject(
                    "MotorCity - Vegetation Test Renderers");
                vegetation =
                    helper.AddComponent<CityDistantTreeCullingExperiment>();
                vegetation.enabled = false;
            }

            details.Initialize(city, camera);
            parked.Initialize(city, camera);
            vegetation.Initialize(city, camera);

            report.AppendLine("MotorCity FOUR-WAY vegetation A/B (Editor only)");
            report.AppendLine("Preset=High, camera position=" +
                initialPosition.ToString("F2") + ", rotation=" +
                initialRotation.eulerAngles.ToString("F1"));
            report.AppendLine("Camera far=" +
                camera.farClipPlane.ToString("F0") +
                "m, fog end=" +
                RenderSettings.fogEndDistance.ToString("F0") + "m.");
            report.AppendLine("Mode order: BASE, GROUND, WOODY, ALL, " +
                "ALL, WOODY, GROUND, BASE.");
            report.AppendLine("Per phase: 3 seconds warmup, 10 seconds of frames.");
            report.AppendLine("High foliage thresholds: beyond 500m and " +
                "projected diameter under 12 pixels at 1080p.");
            report.AppendLine("Colliders remain enabled and operational. " +
                "Mixed-material vegetation and moving objects are excluded.");
            report.AppendLine("Frame intervals are Editor-only, " +
                "not GPU or WebGL timings.");
            report.AppendLine();

            Debug.Log("[MotorCity][Vegetation4Way] Started eight-phase " +
                "automatic comparison (~104 seconds plus candidate scans). " +
                "Keep Game view focused, camera STILL. Report: " + Output);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            try
            {
                parked.enabled = true;
                yield return WaitReady(
                    () => parked != null && parked.ScanReady,
                    "parked vehicles");
                if (parked != null) parked.enabled = false;

                if (error == null)
                {
                    details.enabled = true;
                    yield return WaitReady(
                        () => details != null && details.ScanReady,
                        "distant detail");
                    if (details != null) details.enabled = false;
                }

                if (error == null)
                {
                    vegetation.IncludeWoody = true;
                    vegetation.IncludeGroundCover = false;
                    vegetation.enabled = true;
                    yield return WaitReady(
                        () => vegetation != null && vegetation.ScanReady,
                        "vegetation");
                    if (vegetation != null) vegetation.enabled = false;
                }

                if (error == null)
                {
                    report.AppendLine("Woody candidate renderers=" +
                        vegetation.WoodyCandidateCount +
                        ", ground cover candidates=" +
                        vegetation.GroundCoverCandidateCount +
                        ", all category tris=" +
                        vegetation.EligibleTriangleReferences);
                    report.AppendLine("Small-detail candidates=" +
                        details.CandidateCount +
                        ", parked car groups=" + parked.VehicleCount);
                    report.AppendLine();

                    if (vegetation.WoodyCandidateCount == 0)
                        error = "No woody tree/palm candidates in loaded city";
                }

                for (int phase = 0;
                    phase < Sequence.Length && error == null;
                    phase++)
                {
                    int mode = Sequence[phase];
                    SetMode(mode);

                    double startWarmup = Time.realtimeSinceStartupAsDouble;
                    while (Time.realtimeSinceStartupAsDouble - startWarmup <
                           WarmupSeconds)
                    {
                        if (!CheckScene())
                            break;
                        yield return null;
                    }
                    if (error != null)
                        break;

                    int hiddenDetails = details.HiddenCount;
                    int hiddenParked = parked.HiddenVehicles;
                    int hiddenTrees = vegetation.HiddenWoodyCount;
                    int hiddenGround = vegetation.HiddenGroundCoverCount;
                    long hiddenTreeTris = vegetation.HiddenWoodyTriangles;
                    long hiddenGroundTris = vegetation.HiddenGroundCoverTriangles;

                    var phaseFrames = new List<float>(1900);
                    double startSample = Time.realtimeSinceStartupAsDouble;
                    while (Time.realtimeSinceStartupAsDouble - startSample <
                           SampleSeconds)
                    {
                        if (!CheckScene())
                            break;
                        float ms = Time.unscaledDeltaTime * 1000f;
                        if (ms > 0f)
                            phaseFrames.Add(ms);
                        yield return null;
                    }
                    if (error != null)
                        break;

                    measurements[mode].AddRange(phaseFrames);
                    string phaseLine = "Phase " + (phase + 1) +
                        "/8 " + Labels[mode] + ": " +
                        Describe(phaseFrames) +
                        ", hidden details=" + hiddenDetails +
                        ", parked=" + hiddenParked +
                        ", trees=" + hiddenTrees +
                        ", ground=" + hiddenGround +
                        ", woody tris=" + hiddenTreeTris +
                        ", ground tris=" + hiddenGroundTris;
                    report.AppendLine(phaseLine);
                    Debug.Log("[MotorCity][Vegetation4Way] " + phaseLine);
                }

                if (error != null)
                {
                    report.AppendLine("ABORTED: " + error);
                    Debug.LogWarning("[MotorCity][Vegetation4Way] " +
                        "ABORTED: " + error);
                }
                else if (measurements.Any(x => x.Count < 100))
                {
                    report.AppendLine("ABORTED: too few frame samples.");
                    Debug.LogWarning("[MotorCity][Vegetation4Way] " +
                        "Too few samples.");
                }
                else
                {
                    report.AppendLine();
                    report.AppendLine("=== FINAL two measured phases per variant ===");
                    for (int i = 0; i < measurements.Length; i++)
                        report.AppendLine(Labels[i] + ": " +
                            Describe(measurements[i]));

                    double baselineMs = Mean(measurements[0]);
                    for (int i = 1; i < measurements.Length; i++)
                    {
                        double modeMs = Mean(measurements[i]);
                        double gain = (baselineMs - modeMs) /
                            Math.Max(0.0001d, baselineMs) * 100d;
                        report.AppendLine(Labels[i] +
                            " vs BASE mean frame-time change=" +
                            gain.ToString("+0.0;-0.0;0.0") +
                            "% (positive means faster).");
                    }

                    double groundMs = Mean(measurements[1]);
                    double woodyMs = Mean(measurements[2]);
                    double allMs = Mean(measurements[3]);
                    double allVersusGround = (groundMs - allMs) /
                        Math.Max(0.0001d, groundMs) * 100d;
                    double allVersusWoody = (woodyMs - allMs) /
                        Math.Max(0.0001d, woodyMs) * 100d;
                    report.AppendLine("ALL vs GROUND mean frame-time change=" +
                        allVersusGround.ToString("+0.0;-0.0;0.0") + "%.");
                    report.AppendLine("ALL vs WOODY mean frame-time change=" +
                        allVersusWoody.ToString("+0.0;-0.0;0.0") + "%.");
                    report.AppendLine("Do not ship foliage culling merely " +
                        "because FPS improves: also check skyline, park " +
                        "trees and visual pop-in while driving.");
                    Debug.Log("[MotorCity][Vegetation4Way] FINAL\n" +
                        report.ToString());
                }

                try
                {
                    Directory.CreateDirectory("Temp");
                    File.WriteAllText(Output, report.ToString(), Encoding.UTF8);
                    Debug.Log("[MotorCity][Vegetation4Way] " +
                        "Saved " + Output + ". Send the TXT, not screenshots.");
                }
                catch (Exception problem)
                {
                    Debug.LogError("[MotorCity][Vegetation4Way] " +
                        "Could not write TXT: " + problem.Message);
                }
            }
            finally
            {
                RestoreAll();
                Destroy(gameObject);
            }
        }

        private IEnumerator WaitReady(Func<bool> isReady, string label)
        {
            double start = Time.realtimeSinceStartupAsDouble;
            while (!isReady())
            {
                if (!CheckScene())
                    yield break;
                if (Time.realtimeSinceStartupAsDouble - start >
                    ScanTimeoutSeconds)
                {
                    error = label + " scan exceeded 35 seconds";
                    yield break;
                }
                yield return null;
            }
        }

        private void SetMode(int mode)
        {
            details.enabled = false;
            parked.enabled = false;
            vegetation.enabled = false;

            // Both render categories remain disjoint from tiny details.
            // Flip options only after OnDisable restores its renderers.
            vegetation.IncludeGroundCover = (mode & 1) != 0;
            vegetation.IncludeWoody = (mode & 2) != 0;
            details.enabled = true;
            details.RequestRefresh();
            parked.enabled = true;
            parked.RequestRefresh();

            if (mode > 0)
            {
                vegetation.enabled = true;
                vegetation.RequestRefresh();
            }
        }

        private bool CheckScene()
        {
            if (observer == null || !observer.isActiveAndEnabled)
                error = "Camera missing/disabled";
            else if (Vector3.Distance(initialPosition,
                     observer.transform.position) > MaxPositionDrift ||
                     Quaternion.Angle(initialRotation,
                     observer.transform.rotation) > MaxRotationDrift)
                error = "Camera moved >2m or rotated >4 degrees";
            else if (MotorCityQualityRuntime.CurrentPreset !=
                     MotorCityQualityPreset.High)
                error = "Quality preset changed from High";
            else if (!MotorCityVisibilityRuntime.BrowserProfileActive)
                error = "Browser view-distance preview was disabled";
            return error == null;
        }

        private static double Mean(List<float> frames)
        {
            if (frames.Count == 0)
                return 0d;
            double sum = 0d;
            foreach (float frame in frames)
                sum += frame;
            return sum / frames.Count;
        }

        private static float Percentile(List<float> frames, float percentile)
        {
            if (frames.Count == 0)
                return 0f;
            float[] sorted = frames.ToArray();
            Array.Sort(sorted);
            int position = Mathf.Clamp(
                Mathf.CeilToInt(sorted.Length * percentile) - 1,
                0, sorted.Length - 1);
            return sorted[position];
        }

        private static string Describe(List<float> frames)
        {
            double mean = Mean(frames);
            return frames.Count + " frames, mean=" +
                mean.ToString("F2") + "ms, derived FPS=" +
                (mean > 0d ? 1000d / mean : 0d).ToString("F1") +
                ", median=" + Percentile(frames, .5f).ToString("F2") +
                "ms, p95=" + Percentile(frames, .95f).ToString("F2") +
                "ms, p99=" + Percentile(frames, .99f).ToString("F2") +
                "ms, >16.67ms=" +
                frames.Count(f => f > 16.67f) +
                ", >25ms=" + frames.Count(f => f > 25f) +
                ", >50ms=" + frames.Count(f => f > 50f);
        }

        private void RestoreAll()
        {
            if (!initialized || restored)
                return;
            restored = true;

            if (vegetation != null) vegetation.enabled = false;
            if (details != null) details.enabled = false;
            if (parked != null) parked.enabled = false;

            if (createdVegetation && vegetation != null)
                Destroy(vegetation.gameObject);
            else if (vegetation != null)
            {
                vegetation.IncludeGroundCover = initialIncludeGroundCover;
                vegetation.IncludeWoody = initialIncludeWoody;
                vegetation.enabled = initialVegetationEnabled;
                if (initialVegetationEnabled)
                    vegetation.RequestRefresh();
            }

            if (createdDetails && details != null)
                Destroy(details.gameObject);
            else if (details != null)
            {
                details.enabled = initialDetailsEnabled;
                if (initialDetailsEnabled)
                    details.RequestRefresh();
            }

            if (createdParked && parked != null)
                Destroy(parked.gameObject);
            else if (parked != null)
            {
                parked.enabled = initialParkedEnabled;
                if (initialParkedEnabled)
                    parked.RequestRefresh();
            }

            if (foliage != null)
                foliage.enabled = initialFoliageEnabled;
            if (hlod != null)
                hlod.enabled = initialHlodEnabled;

            if (!initialBrowserPreview)
                MotorCityVisibilityRuntime.SetEditorPreview(false);
        }

        private void OnDisable() => RestoreAll();
        private void OnDestroy() => RestoreAll();
    }
}
