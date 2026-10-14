using MotorCity.Diagnostics;
using MotorCity.World;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Non-persistent city parking render cost A/B measurement in Play Mode.
    /// Disabled by default. No source mesh/prefab changes.
    /// </summary>
    public static class MotorCityParkedVehicleOptimization
    {
        [MenuItem("Motor City/Optimization/10 - Enable Parked Vehicles A-B (Play Mode)")]
        private static void Enable()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[MotorCity][ParkedVehicles] Enter Play Mode first.");
                return;
            }

            GameObject city = GameObject.Find("MotorCity_FCGCity");
            Camera camera = Camera.main;
            if (city == null || camera == null)
            {
                Debug.LogWarning("[MotorCity][ParkedVehicles] Wait for city " +
                    "and main camera to load before enabling.");
                return;
            }

            CityParkedVehicleCullingExperiment existing =
                Object.FindFirstObjectByType<CityParkedVehicleCullingExperiment>(
                    FindObjectsInactive.Include);
            if (existing == null)
            {
                GameObject helper =
                    new GameObject("MotorCity - Parked Vehicles A-B");
                existing = helper.AddComponent<CityParkedVehicleCullingExperiment>();
                existing.Initialize(city, camera);
            }

            existing.enabled = true;
            existing.RequestRefresh();
            Debug.Log("[MotorCity][ParkedVehicles] A/B ON: wait for initial " +
                "scan to finish, then open menu 12 to see hidden vehicle " +
                "groups and triangle references. Compare at the same position.");
        }

        [MenuItem("Motor City/Optimization/11 - Disable Parked Vehicles A-B (Play Mode)")]
        private static void Disable()
        {
            CityParkedVehicleCullingExperiment experiment =
                Object.FindFirstObjectByType<CityParkedVehicleCullingExperiment>(
                    FindObjectsInactive.Include);
            if (experiment == null)
            {
                Debug.Log("[MotorCity][ParkedVehicles] Already OFF.");
                return;
            }

            int count = experiment.HiddenVehicles;
            experiment.enabled = false;
            Debug.Log("[MotorCity][ParkedVehicles] A/B OFF: restored all " +
                count + " decorative parked vehicle groups. " +
                "Collider and city prefab state unchanged.");
        }

        [MenuItem("Motor City/Optimization/13 - Benchmark Parked Vehicles A-B (Play Mode)")]
        private static void Benchmark()
        {
            if (!Application.isPlaying || EditorApplication.isPaused)
            {
                Debug.LogWarning("[MotorCity][ParkedBenchmark] Start Play Mode " +
                    "and unpause the game first.");
                return;
            }

            MotorCityParkedVehicleBenchmark running =
                Object.FindFirstObjectByType<MotorCityParkedVehicleBenchmark>(
                    FindObjectsInactive.Include);
            if (running != null ||
                Object.FindFirstObjectByType<
                    MotorCityTreeCullingBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                Object.FindFirstObjectByType<
                    MotorCityCombinedCullingBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                Object.FindFirstObjectByType<
                    MotorCityMovingCameraBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                Object.FindFirstObjectByType<
                    MotorCityVegetationFourWayBenchmarkRunner>(
                    FindObjectsInactive.Include) != null ||
                Object.FindFirstObjectByType<
                    MotorCityVegetationOptimizationBenchmarkRunner>(
                    FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("[MotorCity][ParkedBenchmark] A benchmark " +
                    "is already running. Wait for FINAL in the Console.");
                return;
            }

            CityDistanceDetailCuller details =
                Object.FindFirstObjectByType<CityDistanceDetailCuller>(
                    FindObjectsInactive.Include);
            CityFoliageCullingExperiment foliage =
                Object.FindFirstObjectByType<CityFoliageCullingExperiment>(
                    FindObjectsInactive.Include);
            CityHlodRuntime hlod =
                Object.FindFirstObjectByType<CityHlodRuntime>(
                    FindObjectsInactive.Include);

            if ((details != null && details.enabled) ||
                (foliage != null && foliage.enabled) ||
                (hlod != null && hlod.enabled))
            {
                Debug.LogWarning("[MotorCity][ParkedBenchmark] Disable other " +
                    "visibility A/B experiments first (detail menu 8, " +
                    "foliage menu 3, HLOD menu 4) for an isolated comparison.");
                return;
            }

            GameObject city = GameObject.Find("MotorCity_FCGCity");
            Camera camera = Camera.main;
            if (city == null || camera == null)
            {
                Debug.LogWarning("[MotorCity][ParkedBenchmark] Wait for the " +
                    "playable city and main camera before benchmarking.");
                return;
            }

            CityParkedVehicleCullingExperiment culler =
                Object.FindFirstObjectByType<CityParkedVehicleCullingExperiment>(
                    FindObjectsInactive.Include);
            bool created = culler == null;
            if (created)
            {
                GameObject helper = new GameObject(
                    "MotorCity - Parked Vehicles Benchmark Culler");
                culler = helper.AddComponent<CityParkedVehicleCullingExperiment>();
                culler.Initialize(city, camera);
            }

            GameObject host = new GameObject(
                "MotorCity - Parked Vehicles Benchmark Runner");
            MotorCityParkedVehicleBenchmark benchmark =
                host.AddComponent<MotorCityParkedVehicleBenchmark>();
            benchmark.Begin(camera, culler, created);
        }

        [MenuItem("Motor City/Optimization/12 - Report Parked Vehicles (Play Mode)")]
        private static void Report()
        {
            CityParkedVehicleCullingExperiment experiment =
                Object.FindFirstObjectByType<CityParkedVehicleCullingExperiment>(
                    FindObjectsInactive.Include);
            if (experiment == null)
            {
                Debug.Log("[MotorCity][ParkedVehicles] Not initialized.");
                return;
            }

            Debug.Log("[MotorCity][ParkedVehicles] " +
                (experiment.enabled ? "ON" : "OFF") +
                ": " + experiment.HiddenVehicles + "/" +
                experiment.VehicleCount + " parked vehicle groups hidden, " +
                experiment.HiddenTriangleReferences + "/" +
                experiment.EligibleTriangleReferences +
                " triangle references hidden/eligible; scan complete=" +
                experiment.ScanReady + ". This is NOT a measured FPS improvement.");
        }
    }
}
