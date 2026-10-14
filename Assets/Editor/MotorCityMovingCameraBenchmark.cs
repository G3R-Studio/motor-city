using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MotorCity.CameraSystem;
using MotorCity.Platform;
using MotorCity.World;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    public static class MotorCityMovingCameraBenchmarkMenu
    {
        [MenuItem("Motor City/Optimization/19 - Benchmark Moving Camera BASE vs ALL (Play Mode)")]
        private static void Run()
        {
            if (!Application.isPlaying || EditorApplication.isPaused ||
                Time.timeScale < .5f)
            {
                Debug.LogWarning("[MotorCity][MovingBenchmark] Start an " +
                    "unpaused Play Mode session and wait for the city.");
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<
                    MotorCityMovingCameraBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
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
                Debug.LogWarning("[MotorCity][MovingBenchmark] Wait for " +
                    "the existing A/B benchmark to finish.");
                return;
            }

            GameObject city = GameObject.Find("MotorCity_FCGCity");
            Camera camera = Camera.main;
            if (city == null || camera == null ||
                MotorCityQualityRuntime.CurrentPreset !=
                MotorCityQualityPreset.High)
            {
                Debug.LogWarning("[MotorCity][MovingBenchmark] Load " +
                    "the playable city and select High graphics first.");
                return;
            }

            OpeningCinematicCamera cinematic =
                camera.GetComponent<OpeningCinematicCamera>();
            if (cinematic != null && cinematic.IsPendingOrActive)
            {
                Debug.LogWarning("[MotorCity][MovingBenchmark] Wait " +
                    "until the opening cinematic finishes.");
                return;
            }

            // Use the actual authored road graph. Never invent straight
            // world-space moves through buildings if no valid road route.
            if (!MotorCityMovingCameraBenchmarkRunner.TryPlanRoadRoute(
                    camera, out List<Vector3> waypoints,
                    out float totalDistance, out string explanation))
            {
                Debug.LogWarning("[MotorCity][MovingBenchmark] No safe " +
                    "road-following route near camera: " + explanation +
                    ". Drive to a road near the dense city and retry.");
                return;
            }

            GameObject host = new GameObject(
                "MotorCity - Moving Camera Benchmark (temporary)");
            var runner =
                host.AddComponent<MotorCityMovingCameraBenchmarkRunner>();
            runner.Begin(city, camera, waypoints, totalDistance);
        }
    }

    /// <summary>
    /// Replays the SAME road-derived virtual-camera route in both modes.
    /// It does not drive, teleport or modify the player's car. Benchmark
    /// is EDITOR-ONLY and never attaches a component to a prefab.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class MotorCityMovingCameraBenchmarkRunner : MonoBehaviour
    {
        private const string Output =
            "Temp/MotorCity_MovingCameraBenchmark.txt";
        private const float WarmupSeconds = 2.5f;
        private const float SampleSeconds = 12f;
        private const float CameraHeightAboveRoad = 3.2f;
        private const float CameraPitchDown = 8f;
        private const float BinMeters = 30f;
        private const float ScanTimeoutSeconds = 35f;
        private static readonly int[] Sequence = { 0, 1, 1, 0 };
        private static readonly string[] Labels =
        {
            "BASE (small details + parked cars)",
            "ALL (BASE + trees + ground cover)"
        };

        private struct FrameSample
        {
            public float Milliseconds;
            public float Distance;
            public Vector3 Position;
            public int Pass;
        }

        private struct Counts
        {
            public long DetailHides;
            public long DetailRestores;
            public long ParkedHides;
            public long ParkedRestores;
            public long PlantHides;
            public long PlantRestores;
        }

        private readonly List<float>[] frameTimes =
        {
            new List<float>(6000), new List<float>(6000)
        };
        private readonly List<FrameSample>[] slowSamples =
        {
            new List<FrameSample>(), new List<FrameSample>()
        };
        private readonly StringBuilder report = new StringBuilder(10000);

        private List<float>[][] bins;
        private List<Vector3> roadPoints;
        private float[] roadDistances;
        private float routeLength;
        private float distanceOnRoad;
        private double movingStart;
        private bool moving;
        private Camera cameraToMove;
        private ChaseCamera chaseCamera;
        private bool chaseInitiallyEnabled;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float originalFieldOfView;
        private bool originalBrowserPreview;
        private bool setupStarted;
        private bool restored;

        private CityDistanceDetailCuller details;
        private CityParkedVehicleCullingExperiment parked;
        private CityDistantTreeCullingExperiment vegetation;
        private CityFoliageCullingExperiment foliage;
        private CityHlodRuntime hlod;

        private bool madeDetails;
        private bool madeParked;
        private bool madeVegetation;
        private bool hadDetails;
        private bool hadParked;
        private bool hadVegetation;
        private bool hadFoliage;
        private bool hadHlod;
        private bool oldWoody;
        private bool oldGround;
        private bool wasDetailsOn;
        private bool wasParkedOn;
        private bool wasVegetationOn;
        private bool wasFoliageOn;
        private bool wasHlodOn;
        private string failure;

        public static bool TryPlanRoadRoute(Camera camera,
            out List<Vector3> route, out float distance,
            out string explanation)
        {
            route = null;
            distance = 0f;
            explanation = "no candidate route";
            if (camera == null)
                return false;

            // Try road endpoints in several directions; graph projections
            // must remain near both the current camera and chosen endpoint.
            Vector3 direction = camera.transform.forward;
            direction.y = 0f;
            direction = direction.sqrMagnitude > .01f ?
                direction.normalized : Vector3.forward;
            Vector3 right = new Vector3(direction.z, 0f, -direction.x);
            Vector3[] directions =
                { direction, right, -direction, -right };

            float bestScore = float.PositiveInfinity;
            foreach (Vector3 axis in directions)
            {
                Vector3 destination = camera.transform.position + axis * 185f;
                List<Vector3> candidate =
                    CityRoadNavigator.BuildRoute(
                        camera.transform.position, destination);

                if (candidate == null || candidate.Count < 2)
                    continue;

                float length = 0f;
                for (int i = 1; i < candidate.Count; i++)
                    length += FlatDistance(candidate[i - 1], candidate[i]);

                float startGap = FlatDistance(
                    camera.transform.position, candidate[0]);
                float endGap = FlatDistance(
                    destination, candidate[candidate.Count - 1]);
                if (startGap > 35f || endGap > 65f ||
                    length < 115f || length > 370f)
                    continue;

                float score = Mathf.Abs(length - 185f) +
                    startGap * .4f + endGap * .3f;
                if (score < bestScore)
                {
                    bestScore = score;
                    route = candidate;
                    distance = length;
                }
            }

            if (route == null)
            {
                explanation = "no road graph path 115-370m with projected " +
                    "start/end within 35m/65m of camera/target";
                return false;
            }

            explanation = "found " + route.Count + " road points over " +
                distance.ToString("F0") + "m";
            return true;
        }

        public void Begin(GameObject city, Camera camera,
            List<Vector3> waypoints, float length)
        {
            cameraToMove = camera;
            originalPosition = camera.transform.position;
            originalRotation = camera.transform.rotation;
            originalFieldOfView = camera.fieldOfView;
            originalBrowserPreview =
                MotorCityVisibilityRuntime.BrowserProfileActive;

            roadPoints = waypoints;
            routeLength = length;
            roadDistances = new float[waypoints.Count];
            for (int i = 1; i < waypoints.Count; i++)
                roadDistances[i] = roadDistances[i - 1] +
                    FlatDistance(waypoints[i - 1], waypoints[i]);

            routeLength = roadDistances[roadDistances.Length - 1];
            int binCount = Mathf.Max(1, Mathf.CeilToInt(routeLength / BinMeters));
            bins = new List<float>[2][];
            for (int mode = 0; mode < 2; mode++)
            {
                bins[mode] = new List<float>[binCount];
                for (int k = 0; k < binCount; k++)
                    bins[mode][k] = new List<float>(300);
            }

            chaseCamera = camera.GetComponent<ChaseCamera>();
            chaseInitiallyEnabled = chaseCamera != null && chaseCamera.enabled;

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

            hadDetails = details != null;
            hadParked = parked != null;
            hadVegetation = vegetation != null;
            hadFoliage = foliage != null;
            hadHlod = hlod != null;

            wasDetailsOn = hadDetails && details.enabled;
            wasParkedOn = hadParked && parked.enabled;
            wasVegetationOn = hadVegetation && vegetation.enabled;
            wasFoliageOn = hadFoliage && foliage.enabled;
            wasHlodOn = hadHlod && hlod.enabled;
            oldWoody = hadVegetation && vegetation.IncludeWoody;
            oldGround = hadVegetation && vegetation.IncludeGroundCover;
            setupStarted = true;

            if (chaseCamera != null) chaseCamera.enabled = false;
            if (details != null) details.enabled = false;
            if (parked != null) parked.enabled = false;
            if (vegetation != null) vegetation.enabled = false;
            if (foliage != null) foliage.enabled = false;
            if (hlod != null) hlod.enabled = false;
            if (!originalBrowserPreview)
                MotorCityVisibilityRuntime.SetEditorPreview(true);

            if (details == null)
            {
                madeDetails = true;
                GameObject helper = new GameObject("MotorCity - Moving Detail Test");
                details = helper.AddComponent<CityDistanceDetailCuller>();
                details.enabled = false;
            }
            if (parked == null)
            {
                madeParked = true;
                GameObject helper = new GameObject("MotorCity - Moving Parked Test");
                parked = helper.AddComponent<CityParkedVehicleCullingExperiment>();
                parked.enabled = false;
            }
            if (vegetation == null)
            {
                madeVegetation = true;
                GameObject helper = new GameObject("MotorCity - Moving Vegetation Test");
                vegetation = helper.AddComponent<CityDistantTreeCullingExperiment>();
                vegetation.enabled = false;
            }

            details.Initialize(city, camera);
            parked.Initialize(city, camera);
            vegetation.Initialize(city, camera);
            vegetation.IncludeWoody = true;
            vegetation.IncludeGroundCover = true;

            report.AppendLine("MotorCity MOVING-CAMERA A/B - UNITY EDITOR ONLY");
            report.AppendLine("Preset=High, far=" + camera.farClipPlane.ToString("F0") +
                "m, fog end=" + RenderSettings.fogEndDistance.ToString("F0") +
                "m, FOV=" + camera.fieldOfView.ToString("F1"));
            report.AppendLine("Camera origin: " + originalPosition.ToString("F2") +
                ", rotation " + originalRotation.eulerAngles.ToString("F1"));
            report.AppendLine("Virtual camera only; player's car and " +
                "Rigidbody physics are NOT moved or disabled.");
            report.AppendLine("Road path: " + roadPoints.Count +
                " points, " + routeLength.ToString("F1") +
                "m, camera height +" + CameraHeightAboveRoad +
                "m, pitched " + CameraPitchDown + " degrees down.");
            for (int i = 0; i < roadPoints.Count; i++)
                report.AppendLine("  waypoint " + i + ": " +
                    roadPoints[i].ToString("F2"));
            report.AppendLine("Sequence: BASE, ALL, ALL, BASE. " +
                "Each phase: 2.5s stationary warmup, 12s moving replay.");
            report.AppendLine("Performance data: frame intervals and " +
                "renderer visibility transitions, NOT browser FPS/GPU cost.");
            report.AppendLine();

            PlaceCamera(0f);
            Debug.Log("[MotorCity][MovingBenchmark] Recording identical " +
                "road-route camera sweeps, ~60 seconds after scans. " +
                "Do not use camera controls, pause, or switch Game view. " +
                "The car stays parked. Result: " + Output);
            StartCoroutine(Measure());
        }

        private void Update()
        {
            if (!moving || cameraToMove == null)
                return;
            double elapsed = Time.realtimeSinceStartupAsDouble - movingStart;
            float progress = Mathf.Clamp01((float)(elapsed / SampleSeconds));
            PlaceCamera(progress);
        }

        private IEnumerator Measure()
        {
            try
            {
                // Run each initial renderer discovery with all other
                // cullers disabled to avoid losing candidates.
                parked.enabled = true;
                yield return WaitForScan(
                    () => parked != null && parked.ScanReady, "parked cars");
                if (parked != null) parked.enabled = false;

                if (failure == null)
                {
                    details.enabled = true;
                    yield return WaitForScan(
                        () => details != null && details.ScanReady, "small details");
                    if (details != null) details.enabled = false;
                }

                if (failure == null)
                {
                    vegetation.enabled = true;
                    yield return WaitForScan(
                        () => vegetation != null && vegetation.ScanReady,
                        "distant vegetation");
                    if (vegetation != null) vegetation.enabled = false;
                }

                if (failure == null)
                {
                    report.AppendLine("Eligible tiny-detail renderers=" +
                        details.CandidateCount + ", parked groups=" +
                        parked.VehicleCount + ", woody=" +
                        vegetation.WoodyCandidateCount + ", ground=" +
                        vegetation.GroundCoverCandidateCount);
                    report.AppendLine();

                    for (int phase = 0; phase < Sequence.Length &&
                        failure == null; phase++)
                    {
                        int mode = Sequence[phase];
                        SetMode(mode);
                        PlaceCamera(0f);

                        double warmupStart = Time.realtimeSinceStartupAsDouble;
                        while (Time.realtimeSinceStartupAsDouble - warmupStart <
                            WarmupSeconds)
                        {
                            if (!CheckState())
                                break;
                            yield return null;
                        }
                        if (failure != null)
                            break;

                        Counts before = ReadCounts();
                        var phaseFrames = new List<float>(2400);
                        movingStart = Time.realtimeSinceStartupAsDouble;
                        moving = true;

                        while (Time.realtimeSinceStartupAsDouble - movingStart <
                            SampleSeconds)
                        {
                            if (!CheckState())
                                break;
                            float ms = Time.unscaledDeltaTime * 1000f;
                            if (ms > 0f)
                            {
                                phaseFrames.Add(ms);
                                RecordFrame(mode, phase, ms);
                            }
                            yield return null;
                        }

                        moving = false;
                        PlaceCamera(1f);
                        if (failure != null)
                            break;

                        Counts after = ReadCounts();
                        frameTimes[mode].AddRange(phaseFrames);
                        string line = "Phase " + (phase + 1) + "/4 " +
                            Labels[mode] + ": " +
                            Describe(phaseFrames) + ", " +
                            FormatTransitions(before, after) +
                            ", final hidden details=" + details.HiddenCount +
                            ", parked=" + parked.HiddenVehicles +
                            ", vegetation=" + vegetation.HiddenCount;
                        report.AppendLine(line);
                        Debug.Log("[MotorCity][MovingBenchmark] " + line);
                    }
                }

                if (failure != null)
                {
                    report.AppendLine("ABORTED: " + failure);
                    Debug.LogWarning("[MotorCity][MovingBenchmark] " +
                        "ABORTED: " + failure);
                }
                else if (frameTimes[0].Count < 200 ||
                    frameTimes[1].Count < 200)
                {
                    report.AppendLine("ABORTED: insufficient frame samples");
                }
                else
                {
                    report.AppendLine();
                    report.AppendLine("=== FINAL - two route passes per mode ===");
                    for (int mode = 0; mode < 2; mode++)
                        report.AppendLine(Labels[mode] + ": " +
                            Describe(frameTimes[mode]));

                    double baseMean = Mean(frameTimes[0]);
                    double allMean = Mean(frameTimes[1]);
                    double gain = baseMean > 0d ?
                        (baseMean - allMean) / baseMean * 100d : 0d;
                    report.AppendLine("ALL mean frame-time improvement " +
                        "over BASE=" + gain.ToString("+0.0;-0.0;0.0") +
                        "% (positive = ALL faster).");
                    report.AppendLine();

                    report.AppendLine("=== ROAD BINS: 30-meter intervals ===");
                    for (int i = 0; i < bins[0].Length; i++)
                    {
                        List<float> left = bins[0][i];
                        List<float> right = bins[1][i];
                        double leftMean = Mean(left);
                        double rightMean = Mean(right);
                        double difference = leftMean - rightMean;
                        report.AppendLine(
                            (i * BinMeters).ToString("F0") + ".." +
                            Mathf.Min((i + 1) * BinMeters, routeLength)
                                .ToString("F0") +
                            "m: BASE " + leftMean.ToString("F2") +
                            "ms n=" + left.Count +
                            ", ALL " + rightMean.ToString("F2") +
                            "ms n=" + right.Count +
                            ", saved=" +
                            difference.ToString("+0.00;-0.00;0.00") +
                            "ms (+ means ALL faster)");
                    }

                    report.AppendLine();
                    report.AppendLine("=== SLOWEST FRAMES AND POSITIONS ===");
                    for (int mode = 0; mode < 2; mode++)
                    {
                        report.AppendLine(Labels[mode]);
                        foreach (FrameSample sample in slowSamples[mode]
                            .OrderByDescending(x => x.Milliseconds).Take(8))
                        {
                            report.AppendLine("  " +
                                sample.Milliseconds.ToString("F2") +
                                "ms at " + sample.Distance.ToString("F0") +
                                "m, pos=" + sample.Position.ToString("F2") +
                                ", pass " + (sample.Pass + 1));
                        }
                    }
                    report.AppendLine("These are reproduced virtual-camera " +
                        "views along graph-authored roads. This does not " +
                        "benchmark driving physics, traffic collisions or " +
                        "the physical player car in motion.");
                    Debug.Log("[MotorCity][MovingBenchmark] FINAL\n" +
                        report.ToString());
                }

                try
                {
                    Directory.CreateDirectory("Temp");
                    File.WriteAllText(Output, report.ToString(), Encoding.UTF8);
                    Debug.Log("[MotorCity][MovingBenchmark] Saved: " +
                        Output + ". Send the TXT, not screenshots.");
                }
                catch (Exception exception)
                {
                    Debug.LogError("[MotorCity][MovingBenchmark] " +
                        "Could not write report: " + exception.Message);
                }
            }
            finally
            {
                RestoreState();
                Destroy(gameObject);
            }
        }

        private void RecordFrame(int mode, int phase, float ms)
        {
            int bin = Mathf.Clamp(
                Mathf.FloorToInt(distanceOnRoad / BinMeters),
                0, bins[mode].Length - 1);
            bins[mode][bin].Add(ms);

            // Keep only the 24 slowest frames per mode: no large
            // allocations or logging inside the measurement hot path.
            List<FrameSample> slow = slowSamples[mode];
            if (slow.Count >= 24 && ms <= slow[slow.Count - 1].Milliseconds)
                return;

            FrameSample item = new FrameSample
            {
                Milliseconds = ms,
                Distance = distanceOnRoad,
                Position = cameraToMove.transform.position,
                Pass = phase
            };
            int insertion = 0;
            while (insertion < slow.Count &&
                slow[insertion].Milliseconds > ms)
                insertion++;
            slow.Insert(insertion, item);
            if (slow.Count > 24)
                slow.RemoveAt(24);
        }

        private IEnumerator WaitForScan(Func<bool> ready, string title)
        {
            double start = Time.realtimeSinceStartupAsDouble;
            while (!ready())
            {
                if (!CheckState())
                    yield break;
                if (Time.realtimeSinceStartupAsDouble - start >
                    ScanTimeoutSeconds)
                {
                    failure = title + " scan exceeded 35 seconds";
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
            vegetation.IncludeWoody = true;
            vegetation.IncludeGroundCover = true;

            details.enabled = true;
            details.RequestRefresh();
            parked.enabled = true;
            parked.RequestRefresh();
            if (mode == 1)
            {
                vegetation.enabled = true;
                vegetation.RequestRefresh();
            }
        }

        private void PlaceCamera(float progress)
        {
            if (cameraToMove == null || roadPoints == null)
                return;
            float travel = Mathf.Clamp01(progress) * routeLength;
            distanceOnRoad = travel;

            int index = 0;
            while (index + 2 < roadDistances.Length &&
                travel > roadDistances[index + 1])
                index++;
            float a = roadDistances[index];
            float b = roadDistances[index + 1];
            float alpha = b - a > .001f ?
                Mathf.Clamp01((travel - a) / (b - a)) : 0f;

            Vector3 position = Vector3.Lerp(
                roadPoints[index], roadPoints[index + 1], alpha);
            Vector3 flatForward = roadPoints[index + 1] - roadPoints[index];
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < .01f)
                flatForward = Vector3.forward;

            Quaternion rotation = Quaternion.LookRotation(
                flatForward.normalized, Vector3.up) *
                Quaternion.Euler(CameraPitchDown, 0f, 0f);

            cameraToMove.transform.SetPositionAndRotation(
                position + Vector3.up * CameraHeightAboveRoad, rotation);
        }

        private bool CheckState()
        {
            if (cameraToMove == null || !cameraToMove.isActiveAndEnabled)
                failure = "Main camera disappeared or disabled";
            else if (MotorCityQualityRuntime.CurrentPreset !=
                     MotorCityQualityPreset.High)
                failure = "Graphics preset changed from High";
            else if (!MotorCityVisibilityRuntime.BrowserProfileActive)
                failure = "Editor browser visibility preview turned off";
            else if (Time.timeScale < .5f || EditorApplication.isPaused)
                failure = "Play Mode was paused";
            else if (chaseCamera != null && chaseCamera.enabled)
                failure = "ChaseCamera re-enabled during test";
            return failure == null;
        }

        private Counts ReadCounts()
        {
            return new Counts
            {
                DetailHides = details.VisibilityHides,
                DetailRestores = details.VisibilityRestores,
                ParkedHides = parked.VisibilityHides,
                ParkedRestores = parked.VisibilityRestores,
                PlantHides = vegetation.VisibilityHides,
                PlantRestores = vegetation.VisibilityRestores
            };
        }

        private static string FormatTransitions(Counts before, Counts after)
        {
            return "renderer switches (hide/restore): details=" +
                (after.DetailHides - before.DetailHides) + "/" +
                (after.DetailRestores - before.DetailRestores) +
                ", parked car groups=" +
                (after.ParkedHides - before.ParkedHides) + "/" +
                (after.ParkedRestores - before.ParkedRestores) +
                ", vegetation=" +
                (after.PlantHides - before.PlantHides) + "/" +
                (after.PlantRestores - before.PlantRestores);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static double Mean(List<float> samples)
        {
            if (samples.Count == 0)
                return 0d;
            double sum = 0d;
            foreach (float value in samples)
                sum += value;
            return sum / samples.Count;
        }

        private static float Percentile(List<float> samples, float part)
        {
            if (samples.Count == 0)
                return 0f;
            float[] sorted = samples.ToArray();
            Array.Sort(sorted);
            int index = Mathf.Clamp(
                Mathf.CeilToInt(sorted.Length * part) - 1,
                0, sorted.Length - 1);
            return sorted[index];
        }

        private static string Describe(List<float> ms)
        {
            double mean = Mean(ms);
            return ms.Count + " frames, mean=" +
                mean.ToString("F2") + "ms, derived FPS=" +
                (mean > .0001d ? 1000d / mean : 0d).ToString("F1") +
                ", median=" + Percentile(ms, .5f).ToString("F2") +
                "ms, p95=" + Percentile(ms, .95f).ToString("F2") +
                "ms, p99=" + Percentile(ms, .99f).ToString("F2") +
                "ms, >16.67ms=" + ms.Count(f => f > 16.67f) +
                ", >25ms=" + ms.Count(f => f > 25f) +
                ", >50ms=" + ms.Count(f => f > 50f);
        }

        private void RestoreState()
        {
            if (!setupStarted || restored)
                return;
            restored = true;
            moving = false;

            if (vegetation != null) vegetation.enabled = false;
            if (details != null) details.enabled = false;
            if (parked != null) parked.enabled = false;

            if (madeVegetation && vegetation != null)
                Destroy(vegetation.gameObject);
            else if (vegetation != null)
            {
                vegetation.IncludeWoody = oldWoody;
                vegetation.IncludeGroundCover = oldGround;
                vegetation.enabled = wasVegetationOn;
                if (wasVegetationOn) vegetation.RequestRefresh();
            }

            if (madeDetails && details != null)
                Destroy(details.gameObject);
            else if (details != null)
            {
                details.enabled = wasDetailsOn;
                if (wasDetailsOn) details.RequestRefresh();
            }

            if (madeParked && parked != null)
                Destroy(parked.gameObject);
            else if (parked != null)
            {
                parked.enabled = wasParkedOn;
                if (wasParkedOn) parked.RequestRefresh();
            }

            if (hadFoliage && foliage != null) foliage.enabled = wasFoliageOn;
            if (hadHlod && hlod != null) hlod.enabled = wasHlodOn;

            if (cameraToMove != null)
            {
                cameraToMove.transform.SetPositionAndRotation(
                    originalPosition, originalRotation);
                cameraToMove.fieldOfView = originalFieldOfView;
            }

            if (chaseCamera != null)
                chaseCamera.enabled = chaseInitiallyEnabled;
            if (!originalBrowserPreview)
                MotorCityVisibilityRuntime.SetEditorPreview(false);
        }

        private void OnDisable() => RestoreState();
        private void OnDestroy() => RestoreState();
    }
}
