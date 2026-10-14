#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Diagnostics
{
    /// <summary>
    /// Editor Play Mode only. Measures A/B frame time for parked-car culling
    /// with the same camera view and graphics settings. No player/build edits.
    /// </summary>
    public sealed class MotorCityParkedVehicleBenchmark : MonoBehaviour
    {
        private const float WarmupSeconds = 2.5f;
        private const float SampleSeconds = 12f;
        private const float MaxCameraDrift = 2f;
        private const float MaxCameraAngle = 4f;
        private const float ScanTimeoutSeconds = 30f;

        private readonly List<float> offSamples = new List<float>(6000);
        private readonly List<float> onSamples = new List<float>(6000);
        private readonly List<float> phaseSamples = new List<float>(1800);
        private Camera observer;
        private CityParkedVehicleCullingExperiment experiment;
        private bool previouslyEnabled;
        private bool createdExperiment;
        private bool restored;
        private Vector3 startingPosition;
        private Quaternion startingRotation;
        private string abortReason;

        public void Begin(Camera camera,
            CityParkedVehicleCullingExperiment culler, bool ownsExperiment)
        {
            observer = camera;
            experiment = culler;
            createdExperiment = ownsExperiment;
            previouslyEnabled = !ownsExperiment && culler.enabled;
            startingPosition = camera.transform.position;
            startingRotation = camera.transform.rotation;
            StartCoroutine(RunBenchmark());
        }

        private IEnumerator RunBenchmark()
        {
            Debug.Log("[MotorCity][ParkedBenchmark] Preparing OFF/ON/ON/OFF " +
                "12-second samples, with 2.5-second warmups (~58 seconds). " +
                "Keep Game view focused and camera STILL. Other cullers must " +
                "stay OFF. Timing is Unity Editor frame time, not WebGL GPU.");

            // Discovery runs incrementally. Finish it before switching the
            // experiment OFF for the baseline; never time city scanning.
            experiment.enabled = true;
            double scanStart = Time.realtimeSinceStartupAsDouble;
            while (experiment != null && !experiment.ScanReady)
            {
                if (Time.realtimeSinceStartupAsDouble - scanStart >
                    ScanTimeoutSeconds)
                {
                    abortReason = "parked vehicle scan exceeded 30 seconds";
                    break;
                }
                if (!CameraStable())
                {
                    abortReason = "camera moved during initial scan";
                    break;
                }
                yield return null;
            }

            bool[] phases = { false, true, true, false };
            for (int phase = 0; phase < phases.Length &&
                abortReason == null; phase++)
            {
                bool enabled = phases[phase];
                if (experiment == null)
                {
                    abortReason = "culler was destroyed";
                    break;
                }

                experiment.enabled = enabled;
                if (enabled)
                    experiment.RequestRefresh();

                // Warmup includes renderer updates and the usual first-frame
                // costs after changing a rendering experiment. No collection.
                double warmupAt = Time.realtimeSinceStartupAsDouble;
                while (Time.realtimeSinceStartupAsDouble - warmupAt <
                       WarmupSeconds)
                {
                    if (!CameraStable())
                    {
                        abortReason = "camera moved/rotated";
                        break;
                    }
                    yield return null;
                }
                if (abortReason != null)
                    break;

                int hidden = enabled ? experiment.HiddenVehicles : 0;
                long tris = enabled
                    ? experiment.HiddenTriangleReferences : 0L;
                phaseSamples.Clear();
                double sampleStart = Time.realtimeSinceStartupAsDouble;
                while (Time.realtimeSinceStartupAsDouble - sampleStart <
                       SampleSeconds)
                {
                    if (!CameraStable())
                    {
                        abortReason = "camera moved/rotated";
                        break;
                    }

                    // unscaledDeltaTime reflects the actual game frame
                    // interval; it does NOT isolate GPU/CPU subcomponents.
                    float frameMs = Time.unscaledDeltaTime * 1000f;
                    if (frameMs > 0f)
                        phaseSamples.Add(frameMs);
                    yield return null;
                }

                if (abortReason != null)
                    break;

                List<float> destination = enabled ? onSamples : offSamples;
                destination.AddRange(phaseSamples);
                Debug.Log("[MotorCity][ParkedBenchmark] Phase " +
                    (phase + 1) + "/4 " + (enabled ? "ON" : "OFF") +
                    ", frames=" + phaseSamples.Count +
                    ", mean=" + Mean(phaseSamples).ToString("F2") +
                    " ms, p95=" + Percentile(phaseSamples, 0.95f)
                        .ToString("F2") + " ms, hidden vehicles=" +
                    hidden + ", hidden triangle references=" + tris + ".");
            }

            if (abortReason != null)
            {
                Debug.LogWarning("[MotorCity][ParkedBenchmark] ABORTED: " +
                    abortReason + ". Keep camera stationary and retry.");
            }
            else if (offSamples.Count < 100 || onSamples.Count < 100)
            {
                Debug.LogWarning("[MotorCity][ParkedBenchmark] Not enough " +
                    "samples. Try again with active Game view.");
            }
            else
            {
                float off = Mean(offSamples);
                float on = Mean(onSamples);
                float change = off > 0f ? 100f * (off - on) / off : 0f;
                var report = new StringBuilder(1000);
                report.Append("[MotorCity][ParkedBenchmark] FINAL 12s x 2, ");
                report.Append("ABBA stationary camera, preset=");
                report.Append(MotorCity.Platform.MotorCityQualityRuntime
                    .CurrentPreset);
                report.Append(".\n  OFF: ");
                AppendMetrics(report, offSamples);
                report.Append("\n  ON:  ");
                AppendMetrics(report, onSamples);
                report.Append("\n  Mean frame-time improvement: ");
                report.Append(change.ToString("+0.0;-0.0;0.0"));
                report.Append("% (positive means ON is faster).");
                report.Append("\n  These are Editor frame intervals, NOT " +
                    "browser FPS or GPU-only timings. Keep visual quality " +
                    "and static-camera conditions identical.");
                Debug.Log(report.ToString());
            }

            RestoreInitialState();
            Destroy(gameObject);
        }

        private static float Mean(List<float> samples)
        {
            if (samples.Count == 0)
                return 0f;
            double sum = 0d;
            foreach (float ms in samples)
                sum += ms;
            return (float)(sum / samples.Count);
        }

        private static float Percentile(List<float> samples, float percentile)
        {
            if (samples.Count == 0)
                return 0f;
            float[] ordered = samples.ToArray();
            Array.Sort(ordered);
            int index = Mathf.Clamp(
                Mathf.CeilToInt(ordered.Length * percentile) - 1,
                0, ordered.Length - 1);
            return ordered[index];
        }

        private static void AppendMetrics(
            StringBuilder result, List<float> samples)
        {
            float mean = Mean(samples);
            int spikes25 = 0;
            int spikes50 = 0;
            foreach (float value in samples)
            {
                if (value > 25f) spikes25++;
                if (value > 50f) spikes50++;
            }

            result.Append(samples.Count).Append(" frames");
            result.Append(", mean=").Append(mean.ToString("F2")).Append("ms");
            result.Append(", FPS=").Append(
                (mean > 0f ? 1000f / mean : 0f).ToString("F1"));
            result.Append(", median=").Append(
                Percentile(samples, .5f).ToString("F2")).Append("ms");
            result.Append(", p95=").Append(
                Percentile(samples, .95f).ToString("F2")).Append("ms");
            result.Append(", p99=").Append(
                Percentile(samples, .99f).ToString("F2")).Append("ms");
            result.Append(", >25ms=").Append(spikes25);
            result.Append(", >50ms=").Append(spikes50);
        }

        private bool CameraStable()
        {
            return observer != null &&
                Vector3.Distance(startingPosition,
                    observer.transform.position) <= MaxCameraDrift &&
                Quaternion.Angle(startingRotation,
                    observer.transform.rotation) <= MaxCameraAngle;
        }

        private void RestoreInitialState()
        {
            if (restored)
                return;
            restored = true;
            if (experiment != null)
            {
                experiment.enabled = previouslyEnabled;
                if (previouslyEnabled)
                    experiment.RequestRefresh();
                else if (createdExperiment)
                    Destroy(experiment.gameObject);
            }
        }

        private void OnDisable() => RestoreInitialState();
        private void OnDestroy() => RestoreInitialState();
    }
}
#endif
