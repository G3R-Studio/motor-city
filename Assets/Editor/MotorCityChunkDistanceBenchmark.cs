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
    // Editor-only rendering distance experiment. No prefab, collider, traffic or build changes.
    public static class MotorCityChunkDistanceBenchmarkMenu
    {
        [MenuItem("Motor City/Optimization/20 - Benchmark Static City Chunks (Play Mode)")]
        private static void Run()
        {
            if (!Application.isPlaying || EditorApplication.isPaused ||
                Time.timeScale < .5f)
            {
                Debug.LogWarning("[MotorCity][ChunkTest] Enter unpaused Play Mode.");
                return;
            }
            if (UnityEngine.Object.FindFirstObjectByType<MotorCityChunkDistanceBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<MotorCityMovingCameraBenchmarkRunner>(
                    FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("[MotorCity][ChunkTest] Another moving-camera test is active.");
                return;
            }
            GameObject city = GameObject.Find("MotorCity_FCGCity");
            Camera camera = Camera.main;
            if (city == null || camera == null ||
                MotorCityQualityRuntime.CurrentPreset != MotorCityQualityPreset.High)
            {
                Debug.LogWarning("[MotorCity][ChunkTest] Load Prototype city and select High graphics.");
                return;
            }
            OpeningCinematicCamera intro = camera.GetComponent<OpeningCinematicCamera>();
            if (intro != null && intro.IsPendingOrActive)
            {
                Debug.LogWarning("[MotorCity][ChunkTest] Wait for opening cinematic.");
                return;
            }
            if (!MotorCityMovingCameraBenchmarkRunner.TryPlanRoadRoute(city, camera,
                    out List<Vector3> road, out float distance, out Vector3 focus,
                    out string reason))
            {
                Debug.LogWarning("[MotorCity][ChunkTest] Route unavailable: " + reason);
                return;
            }
            GameObject host = new GameObject("MotorCity - Chunk Distance Benchmark (temporary)");
            host.AddComponent<MotorCityChunkDistanceBenchmarkRunner>()
                .Begin(city, camera, road, focus);
        }
    }

    [DefaultExecutionOrder(-750)]
    public sealed class MotorCityChunkDistanceBenchmarkRunner : MonoBehaviour
    {
        private const string Output = "Temp/MotorCity_ChunkDistanceBenchmark.txt";
        private const float CellSize = 100f;
        private const float Warmup = 3f;
        private const float Replay = 12f;
        private const int ChangeBudget = 256;
        // Repeated baseline brackets the distance variants to expose drift.
        private static readonly int[] Sequence = { 0, 1, 2, 3, 0 };
        private static readonly string[] Labels =
            { "BASE", "LOW 300m", "MEDIUM 500m", "HIGH 800m" };
        private static readonly float[] Distances = { float.PositiveInfinity, 300f, 500f, 800f };

        private struct Entry
        {
            public MeshRenderer Renderer;
            public bool OriginalEnabled;
            public Bounds Bounds;
            public int CellX;
            public int CellZ;
            public long Triangles;
        }

        private readonly List<Entry> entries = new List<Entry>(3000);
        private readonly List<Vector3> road = new List<Vector3>();
        private readonly List<float>[] samples =
            { new List<float>(), new List<float>(), new List<float>(), new List<float>() };
        private readonly int[] transitions = new int[4];
        private readonly StringBuilder report = new StringBuilder(10000);
        private Camera observer;
        private ChaseCamera chase;
        private bool chaseWasEnabled;
        private Transform cameraTransform;
        private Vector3 oldPosition;
        private Quaternion oldRotation;
        private float oldFov;
        private bool restored;
        private bool moving;
        private double movingStart;
        private float roadLength;
        private float[] cumulative;
        private Vector3 focus;
        private int mode;
        private int cursor;
        private float lastX = float.NaN;
        private float lastZ = float.NaN;
        private int pending;
        private long eligibleTriangles;
        private long hiddenTriangles;
        private int hidden;
        private int switchesThisPhase;
        private string problem;

        public void Begin(GameObject city, Camera camera, List<Vector3> points, Vector3 cityFocus)
        {
            observer = camera;
            cameraTransform = camera.transform;
            oldPosition = cameraTransform.position;
            oldRotation = cameraTransform.rotation;
            oldFov = camera.fieldOfView;
            focus = cityFocus;
            road.AddRange(points);
            cumulative = new float[road.Count];
            for (int i = 1; i < road.Count; i++)
            {
                Vector3 step = road[i] - road[i - 1];
                step.y = 0;
                cumulative[i] = cumulative[i - 1] + step.magnitude;
            }
            roadLength = cumulative[cumulative.Length - 1];
            chase = camera.GetComponent<ChaseCamera>();
            chaseWasEnabled = chase != null && chase.enabled;
            if (chase != null) chase.enabled = false;

            MeshRenderer[] candidates = city.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer renderer in candidates)
            {
                if (!Suitable(renderer, city.transform)) continue;
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Bounds bounds = renderer.bounds;
                long triangles = 0L;
                for (int s = 0; s < mesh.subMeshCount; s++)
                    triangles += (long)mesh.GetIndexCount(s) / 3L;
                entries.Add(new Entry
                {
                    Renderer = renderer,
                    OriginalEnabled = renderer.enabled,
                    Bounds = bounds,
                    CellX = Mathf.FloorToInt(bounds.center.x / CellSize),
                    CellZ = Mathf.FloorToInt(bounds.center.z / CellSize),
                    Triangles = triangles
                });
                eligibleTriangles += triangles;
            }
            report.AppendLine("MOTOR CITY STATIC BUILDING DISTANCE TEST - EDITOR ONLY");
            report.AppendLine("Identical authored road route; High quality in ALL phases.");
            report.AppendLine("BASE vs 300m / 500m / 800m static building visibility.");
            report.AppendLine("Only eligible opaque architectural MeshRenderers under Buildings.");
            report.AppendLine("Excluded: traffic, parked cars, scripts, lights, roads, trees and transparent materials.");
            report.AppendLine("Distance is planar XZ from camera to 100m cell; renderer bounds used for edge safety.");
            report.AppendLine("No new city assets, no WebGL changes, no GameObject/physics toggles.");
            report.AppendLine("WARNING: no distant replacement silhouettes, so popping is expected.");
            report.AppendLine("Editor frame intervals, NOT device FPS or actual GPU triangle counts.");
            report.AppendLine("Eligible renderers=" + entries.Count +
                ", triangle refs=" + eligibleTriangles + ", road=" +
                roadLength.ToString("F1") + "m.");
            report.AppendLine("Camera origin=" + oldPosition.ToString("F1") +
                ", center=" + focus.ToString("F1"));
            report.AppendLine("Sequence BASE / LOW / MEDIUM / HIGH / BASE; " +
                Warmup + "s warmup and " + Replay + "s replay each.");
            Debug.Log("[MotorCity][ChunkTest] Begin five identical road sweeps. Output: " + Output);
            StartCoroutine(Measure());
        }

        private static bool Suitable(MeshRenderer renderer, Transform city)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                renderer.forceRenderingOff || renderer.GetComponent<MeshFilter>() == null ||
                renderer.GetComponent<MeshFilter>().sharedMesh == null ||
                renderer.GetComponentInParent<LODGroup>() != null ||
                renderer.GetComponent<MonoBehaviour>() != null)
                return false;

            Material[] mats = renderer.sharedMaterials;
            if (mats.Length == 0) return false;
            foreach (Material mat in mats)
                if (mat == null || mat.renderQueue >= 2450 ||
                    mat.HasProperty("_Surface") && mat.GetFloat("_Surface") > .5f)
                    return false;

            bool underBuildings = false;
            Transform node = renderer.transform;
            for (int depth = 0; node != null && depth < 32; depth++, node = node.parent)
            {
                string n = node.name;
                if (n == "Buildings") underBuildings = true;
                if (n == "CarContainer" || n == "Traffic System" ||
                    n.StartsWith("Park-", StringComparison.OrdinalIgnoreCase) ||
                    n.StartsWith("TrafficLight", StringComparison.OrdinalIgnoreCase) ||
                    n.IndexOf("waypoint", StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
                if (node == city) break;
            }
            if (!underBuildings) return false;
            Bounds b = renderer.bounds;
            // Long structures can straddle several cells; keep them always visible.
            return b.size.x > 0.1f && b.size.z > 0.1f &&
                   b.size.x < 90f && b.size.z < 90f &&
                   b.extents.magnitude > 5f;
        }

        private void Update()
        {
            if (observer == null) { problem = "Camera destroyed"; return; }
            if (!moving) return;
            float t = Mathf.Clamp01((float)((Time.realtimeSinceStartupAsDouble - movingStart) / Replay));
            Place(t);
        }

        private void Place(float fraction)
        {
            float at = roadLength * fraction;
            int segment = 1;
            while (segment < cumulative.Length - 1 && cumulative[segment] < at)
                segment++;
            float start = cumulative[segment - 1];
            float length = Mathf.Max(.001f, cumulative[segment] - start);
            Vector3 point = Vector3.Lerp(road[segment - 1], road[segment],
                Mathf.Clamp01((at - start) / length));
            Vector3 travel = road[segment] - road[segment - 1];
            travel.y = 0;
            point.y += 3.2f;
            Vector3 towardCity = focus - point;
            towardCity.y = 0;
            Vector3 aim = towardCity.sqrMagnitude > 1f ? towardCity.normalized :
                travel.sqrMagnitude > .01f ? travel.normalized : Vector3.forward;
            cameraTransform.SetPositionAndRotation(point,
                Quaternion.LookRotation(aim, Vector3.up) *
                Quaternion.Euler(8f, 0f, 0f));
        }

        private void ApplyDistances(bool all)
        {
            if (entries.Count == 0) return;
            Vector3 pos = cameraTransform.position;
            if (!all && !float.IsNaN(lastX) &&
                (pos.x - lastX) * (pos.x - lastX) +
                (pos.z - lastZ) * (pos.z - lastZ) < 64f &&
                pending == 0) return;
            lastX = pos.x; lastZ = pos.z;
            int count = all ? entries.Count : Mathf.Min(ChangeBudget, entries.Count);
            for (int n = 0; n < count; n++)
            {
                int index = all ? n : (cursor + n) % entries.Count;
                Entry item = entries[index];
                if (item.Renderer == null) continue;
                float radius = Distances[mode];
                float dx = Mathf.Max(0f, Mathf.Abs(pos.x -
                    (item.CellX + .5f) * CellSize) - CellSize * .5f);
                float dz = Mathf.Max(0f, Mathf.Abs(pos.z -
                    (item.CellZ + .5f) * CellSize) - CellSize * .5f);
                float edge = Mathf.Sqrt(dx * dx + dz * dz);
                // Preserve close edge renderers near a cell boundary.
                float allowance = Mathf.Max(item.Bounds.extents.x, item.Bounds.extents.z);
                bool visible = edge <= radius + allowance;
                if (item.Renderer.enabled != visible)
                {
                    item.Renderer.enabled = visible;
                    transitions[mode]++;
                    switchesThisPhase++;
                }
            }
            if (!all) cursor = (cursor + count) % entries.Count;
        }

        private IEnumerator Measure()
        {
            try
            {
                for (int phase = 0; phase < Sequence.Length; phase++)
                {
                    mode = Sequence[phase];
                    switchesThisPhase = 0;
                    cursor = 0; lastX = float.NaN; lastZ = float.NaN;
                    Place(0f);
                    // Full visibility convergence only between measured phases.
                    ApplyDistances(true);
                    double warm = Time.realtimeSinceStartupAsDouble;
                    while (Time.realtimeSinceStartupAsDouble - warm < Warmup)
                    {
                        if (problem != null) break;
                        ApplyDistances(false);
                        yield return null;
                    }
                    if (problem != null) break;
                    List<float> frames = new List<float>(1500);
                    int long33 = 0; int long50 = 0;
                    double previous = Time.realtimeSinceStartupAsDouble;
                    movingStart = previous;
                    moving = true;
                    while (Time.realtimeSinceStartupAsDouble - movingStart < Replay)
                    {
                        yield return null;
                        ApplyDistances(false);
                        double now = Time.realtimeSinceStartupAsDouble;
                        float ms = (float)((now - previous) * 1000.0);
                        previous = now;
                        frames.Add(ms);
                        if (ms > 33.3f) long33++;
                        if (ms > 50f) long50++;
                        if (problem != null) break;
                    }
                    moving = false;
                    if (problem != null) break;
                    frames.Sort();
                    double avg = frames.Count > 0 ? frames.Average(v => (double)v) : 0;
                    float p95 = frames.Count > 0 ?
                        frames[Mathf.Clamp(Mathf.CeilToInt(frames.Count * .95f) - 1, 0, frames.Count - 1)] : 0;
                    int countHidden = 0;
                    long refsHidden = 0;
                    foreach (Entry e in entries)
                        if (e.Renderer != null && !e.Renderer.enabled)
                        {
                            countHidden++;
                            refsHidden += e.Triangles;
                        }
                    report.AppendLine("Phase " + (phase + 1) + " " + Labels[mode] +
                        ": frames=" + frames.Count + ", avg=" + avg.ToString("F2") +
                        "ms, derived fps=" + (avg > 0 ? 1000d / avg : 0).ToString("F1") +
                        ", p95=" + p95.ToString("F2") + "ms, >33ms=" + long33 +
                        ", >50ms=" + long50 + ", switches=" + switchesThisPhase +
                        ", hidden at end=" + countHidden +
                        ", hidden triangle refs at end=" + refsHidden);
                    Debug.Log("[MotorCity][ChunkTest] " + Labels[mode] +
                        " complete: " + avg.ToString("F2") + "ms");
                    yield return null;
                }
            }
            finally
            {
                Restore();
                report.AppendLine(problem == null ? "Completed" : "ABORTED: " + problem);
                Directory.CreateDirectory("Temp");
                File.WriteAllText(Output, report.ToString());
                Debug.Log("[MotorCity][ChunkTest] Report saved: " + Output);
                Destroy(gameObject);
            }
        }

        private void Restore()
        {
            if (restored) return;
            restored = true;
            moving = false;
            foreach (Entry item in entries)
                if (item.Renderer != null)
                    item.Renderer.enabled = item.OriginalEnabled;
            if (cameraTransform != null)
                cameraTransform.SetPositionAndRotation(oldPosition, oldRotation);
            if (observer != null) observer.fieldOfView = oldFov;
            if (chase != null) chase.enabled = chaseWasEnabled;
        }

        private void OnDisable() { Restore(); }
        private void OnDestroy() { Restore(); }
    }
}
