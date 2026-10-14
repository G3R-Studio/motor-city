using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using MotorCity.Platform;
using MotorCity.World;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Editor Play Mode preflight for the exact three reversible render
    /// cullers selected by WebGL High. Never invokes a browser build.
    /// </summary>
    public static class MotorCityWebGLCullingPreflightMenu
    {
        [MenuItem("Motor City/Optimization/20 - Verify WebGL High Culling Setup (Play Mode)")]
        private static void Start()
        {
            if (!Application.isPlaying || EditorApplication.isPaused)
            {
                Debug.LogWarning("[MotorCity][WebGLPreflight] " +
                    "Start unpaused Play Mode first.");
                return;
            }
            if (MotorCityQualityRuntime.CurrentPreset !=
                MotorCityQualityPreset.High)
            {
                Debug.LogWarning("[MotorCity][WebGLPreflight] " +
                    "Select High graphics before verification.");
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<
                    MotorCityWebGLCullingPreflightRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityMovingCameraBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                UnityEngine.Object.FindFirstObjectByType<
                    MotorCityCombinedCullingBenchmarkRunner>(
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
                    MotorCity.Diagnostics.MotorCityParkedVehicleBenchmark>(
                    FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("[MotorCity][WebGLPreflight] " +
                    "Another culling test is active.");
                return;
            }

            GameObject city = GameObject.Find("MotorCity_FCGCity");
            Camera camera = Camera.main;
            if (city == null || camera == null)
            {
                Debug.LogWarning("[MotorCity][WebGLPreflight] " +
                    "Wait until the city and player camera load.");
                return;
            }
            var host = new GameObject("MotorCity - WebGL High Culling Preflight");
            host.AddComponent<MotorCityWebGLCullingPreflightRunner>()
                .Begin(city, camera);
        }
    }

    public sealed class MotorCityWebGLCullingPreflightRunner : MonoBehaviour
    {
        private const string Output =
            "Temp/MotorCity_WebGLHighCullingPreflight.txt";
        private const double ScanTimeoutSeconds = 35d;
        private readonly StringBuilder report = new StringBuilder(3000);

        private Camera observer;
        private CityDistanceDetailCuller details;
        private CityParkedVehicleCullingExperiment parked;
        private CityDistantTreeCullingExperiment vegetation;
        private CityFoliageCullingExperiment oldFoliage;
        private CityHlodRuntime hlod;

        private bool ownsDetails;
        private bool ownsParked;
        private bool ownsVegetation;
        private bool previousDetails;
        private bool previousParked;
        private bool previousVegetation;
        private bool previousOldFoliage;
        private bool previousHlod;
        private bool previousWoody;
        private bool previousGround;
        private bool previousPreview;
        private bool initialized;
        private bool restored;
        private int errors;
        private Vector3 startPosition;

        public void Begin(GameObject city, Camera camera)
        {
            observer = camera;
            startPosition = camera.transform.position;
            previousPreview = MotorCityVisibilityRuntime.BrowserProfileActive;
            details = UnityEngine.Object.FindFirstObjectByType<
                CityDistanceDetailCuller>(FindObjectsInactive.Include);
            parked = UnityEngine.Object.FindFirstObjectByType<
                CityParkedVehicleCullingExperiment>(FindObjectsInactive.Include);
            vegetation = UnityEngine.Object.FindFirstObjectByType<
                CityDistantTreeCullingExperiment>(FindObjectsInactive.Include);
            oldFoliage = UnityEngine.Object.FindFirstObjectByType<
                CityFoliageCullingExperiment>(FindObjectsInactive.Include);
            hlod = UnityEngine.Object.FindFirstObjectByType<
                CityHlodRuntime>(FindObjectsInactive.Include);

            previousDetails = details != null && details.enabled;
            previousParked = parked != null && parked.enabled;
            previousVegetation =
                vegetation != null && vegetation.enabled;
            previousOldFoliage =
                oldFoliage != null && oldFoliage.enabled;
            previousHlod = hlod != null && hlod.enabled;
            previousWoody = vegetation != null && vegetation.IncludeWoody;
            previousGround =
                vegetation != null && vegetation.IncludeGroundCover;

            initialized = true;
            if (details != null) details.enabled = false;
            if (parked != null) parked.enabled = false;
            if (vegetation != null) vegetation.enabled = false;
            if (oldFoliage != null) oldFoliage.enabled = false;
            if (hlod != null) hlod.enabled = false;

            if (!previousPreview)
                MotorCityVisibilityRuntime.SetEditorPreview(true);

            if (details == null)
            {
                ownsDetails = true;
                var host = new GameObject("MotorCity - Preflight Detail");
                details = host.AddComponent<CityDistanceDetailCuller>();
                details.enabled = false;
            }
            if (parked == null)
            {
                ownsParked = true;
                var host = new GameObject("MotorCity - Preflight Parked");
                parked =
                    host.AddComponent<CityParkedVehicleCullingExperiment>();
                parked.enabled = false;
            }
            if (vegetation == null)
            {
                ownsVegetation = true;
                var host = new GameObject("MotorCity - Preflight Vegetation");
                vegetation =
                    host.AddComponent<CityDistantTreeCullingExperiment>();
                vegetation.enabled = false;
            }
            details.Initialize(city, camera);
            parked.Initialize(city, camera);
            vegetation.Initialize(city, camera);
            vegetation.IncludeWoody = true;
            vegetation.IncludeGroundCover = true;

            report.AppendLine("MotorCity WebGL High culling integration preflight");
            report.AppendLine("Unity Editor Play Mode only; no WebGL build.");
            report.AppendLine("Camera=" + startPosition.ToString("F2") +
                ", preset=" + MotorCityQualityRuntime.CurrentPreset +
                ", far=" + camera.farClipPlane.ToString("F0") + "m.");
            report.AppendLine("High thresholds: parked hide=390m restore=335m; " +
                "vegetation >500m and <12px (1080p reference).");
            report.AppendLine("Three independent components: detail, parked, " +
                "pure Trees-01 vegetation. All previous states restored.");
            report.AppendLine();
            StartCoroutine(Verify());
        }

        private IEnumerator Verify()
        {
            bool completed = false;
            try
            {
                CheckUrlParser();
                details.enabled = true;
                parked.enabled = true;
                vegetation.enabled = true;

                double started = Time.realtimeSinceStartupAsDouble;
                while (!(details.ScanReady &&
                         parked.ScanReady && vegetation.ScanReady))
                {
                    if (!CheckScene())
                        break;
                    if (Time.realtimeSinceStartupAsDouble - started >
                        ScanTimeoutSeconds)
                    {
                        Fail("Culler candidate discovery exceeded 35 seconds");
                        break;
                    }
                    yield return null;
                }

                if (errors == 0)
                {
                    double settle = Time.realtimeSinceStartupAsDouble;
                    while (Time.realtimeSinceStartupAsDouble - settle < 3d)
                    {
                        if (!CheckScene()) break;
                        yield return null;
                    }
                }

                if (errors == 0)
                {
                    report.AppendLine("Eligible detail meshes=" +
                        details.CandidateCount + ", parked vehicle groups=" +
                        parked.VehicleCount + ", woody=" +
                        vegetation.WoodyCandidateCount + ", ground=" +
                        vegetation.GroundCoverCandidateCount);
                    report.AppendLine("Hidden with all ON: details=" +
                        details.HiddenCount + ", parked groups=" +
                        parked.HiddenVehicles + ", woody=" +
                        vegetation.HiddenWoodyCount + ", ground=" +
                        vegetation.HiddenGroundCoverCount);
                    Ensure(details.CandidateCount > 0,
                        "detail candidate scan has results");
                    Ensure(parked.VehicleCount > 0,
                        "parked vehicle scan has results");
                    Ensure(vegetation.WoodyCandidateCount > 0 &&
                        vegetation.GroundCoverCandidateCount > 0,
                        "both categories of vegetation discovered");
                    Ensure(vegetation.IncludeWoody &&
                        vegetation.IncludeGroundCover,
                        "High ALL enables BOTH vegetation categories");

                    // Disabling a single culler must restore only the
                    // renderers it owns; other cullers stay enabled.
                    int detailBefore = details.HiddenCount;
                    int vegetationBefore = vegetation.HiddenCount;
                    parked.enabled = false;
                    Ensure(parked.HiddenVehicles == 0,
                        "parked culler restores all hidden cars");
                    Ensure(details.HiddenCount == detailBefore &&
                        vegetation.HiddenCount == vegetationBefore,
                        "parked restoration leaves others untouched");

                    int detailBeforeVegetation = details.HiddenCount;
                    vegetation.enabled = false;
                    Ensure(vegetation.HiddenCount == 0,
                        "vegetation restores hidden trees and plants");
                    Ensure(details.HiddenCount == detailBeforeVegetation,
                        "vegetation restoration leaves detail culling untouched");

                    details.enabled = false;
                    Ensure(details.HiddenCount == 0,
                        "detail culler restores its hidden meshes");
                    report.AppendLine(
                        "All three cullers individually disabled and restored.");
                    report.AppendLine();
                }
                completed = true;
            }
            finally
            {
                if (!completed)
                    Fail("Preflight was interrupted before completing checks.");
                report.AppendLine(errors == 0 ?
                    "RESULT: PASS (Editor lifecycle + URL parser)." :
                    "RESULT: FAIL (" + errors + " checks).");
                report.AppendLine("WebGL compilation and browser FPS " +
                    "still require a separate final build.");
                try
                {
                    Directory.CreateDirectory("Temp");
                    File.WriteAllText(Output,
                        report.ToString(), Encoding.UTF8);
                    Debug.Log("[MotorCity][WebGLPreflight] FINAL\n" +
                        report.ToString() + "\nSaved: " + Output);
                }
                catch (Exception e)
                {
                    Debug.LogError("[MotorCity][WebGLPreflight] " +
                        "Could not write TXT: " + e.Message);
                }
                RestoreEverything();
                Destroy(gameObject);
            }
        }

        private bool CheckScene()
        {
            if (observer == null || !observer.isActiveAndEnabled)
            {
                Fail("Main camera disappeared or was disabled");
                return false;
            }
            if (Vector3.Distance(startPosition,
                    observer.transform.position) > 3f)
            {
                Fail("Camera moved during preflight");
                return false;
            }
            if (MotorCityQualityRuntime.CurrentPreset !=
                MotorCityQualityPreset.High)
            {
                Fail("Preset changed from High");
                return false;
            }
            if (!MotorCityVisibilityRuntime.BrowserProfileActive)
            {
                Fail("Editor browser preview disabled during verification");
                return false;
            }
            return true;
        }

        private void CheckUrlParser()
        {
            MethodInfo method = typeof(MotorCityVisibilityRuntime)
                .GetMethod("UrlOptedOut",
                    BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                Fail("Missing URL opt-out parser");
                return;
            }

            string[] urls =
            {
                "https://host.example/game?motorcity_parked_cull=off",
                "https://host.example/game?motorcity_detail_cull=off" +
                    "&motorcity_vegetation_cull=off",
                "https://host.example/game?x=1&motorcity_parked_cull=off#top",
                "https://host.example/game?motorcity_parked_cull=on",
                "https://host.example/motorcity_parked_cull=off?x=1"
            };
            string[] keys =
            {
                "motorcity_parked_cull", "motorcity_vegetation_cull",
                "motorcity_parked_cull", "motorcity_parked_cull",
                "motorcity_parked_cull"
            };
            bool[] expected = { true, true, true, false, false };
            for (int i = 0; i < urls.Length; i++)
            {
                bool actual = (bool)method.Invoke(null,
                    new object[] { urls[i], keys[i] });
                Ensure(actual == expected[i],
                    "URL parser case " + (i + 1) + " (" +
                    keys[i] + "=" + (expected[i] ? "off" : "on") + ")");
            }
        }

        private void Ensure(bool okay, string description)
        {
            report.AppendLine((okay ? "PASS: " : "FAIL: ") + description);
            if (!okay)
                errors++;
        }

        private void Fail(string description)
        {
            errors++;
            report.AppendLine("FAIL: " + description);
            Debug.LogWarning("[MotorCity][WebGLPreflight] " + description);
        }

        private void RestoreEverything()
        {
            if (!initialized || restored)
                return;
            restored = true;
            if (details != null) details.enabled = false;
            if (parked != null) parked.enabled = false;
            if (vegetation != null) vegetation.enabled = false;

            if (ownsDetails && details != null)
                Destroy(details.gameObject);
            else if (details != null)
            {
                details.enabled = previousDetails;
                if (previousDetails) details.RequestRefresh();
            }

            if (ownsParked && parked != null)
                Destroy(parked.gameObject);
            else if (parked != null)
            {
                parked.enabled = previousParked;
                if (previousParked) parked.RequestRefresh();
            }

            if (ownsVegetation && vegetation != null)
                Destroy(vegetation.gameObject);
            else if (vegetation != null)
            {
                vegetation.IncludeWoody = previousWoody;
                vegetation.IncludeGroundCover = previousGround;
                vegetation.enabled = previousVegetation;
                if (previousVegetation) vegetation.RequestRefresh();
            }

            if (oldFoliage != null)
                oldFoliage.enabled = previousOldFoliage;
            if (hlod != null)
                hlod.enabled = previousHlod;
            if (!previousPreview)
                MotorCityVisibilityRuntime.SetEditorPreview(false);
        }

        private void OnDisable() => RestoreEverything();
        private void OnDestroy() => RestoreEverything();
    }
}
