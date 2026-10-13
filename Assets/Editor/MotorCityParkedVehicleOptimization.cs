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
