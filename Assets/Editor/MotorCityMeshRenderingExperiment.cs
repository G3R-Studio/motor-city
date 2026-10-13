using System.Collections.Generic;
using MotorCity.World;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Opt-in Play Mode A/B experiment. Does NOT edit imported assets,
    /// world coordinates, colliders, scenes or URP settings.
    /// </summary>
    public static class MotorCityMeshRenderingExperiment
    {
        private const string CityName = "MotorCity_FCGCity";
        private const string ExperimentName =
            "MotorCity - Foliage Mesh Culling Experiment";

        [MenuItem("Motor City/Optimization/1 - Report City Mesh Stats (Play Mode)")]
        private static void Report()
        {
            GameObject city = GetCity();
            if (city == null)
                return;

            MeshRenderer[] renderers =
                city.GetComponentsInChildren<MeshRenderer>(true);
            Dictionary<Mesh, int> meshes = new Dictionary<Mesh, int>();
            HashSet<Material> materials = new HashSet<Material>();

            int active = 0;
            int staticBatch = 0;
            int missingMesh = 0;
            int materialSlots = 0;
            long triangleReferences = 0;

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                    active++;

                if (renderer.isPartOfStaticBatch)
                    staticBatch++;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter == null ? null : filter.sharedMesh;
                if (mesh == null)
                {
                    missingMesh++;
                    continue;
                }

                meshes.TryGetValue(mesh, out int references);
                meshes[mesh] = references + 1;

                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    triangleReferences += mesh.GetIndexCount(submesh) / 3;

                foreach (Material material in renderer.sharedMaterials)
                {
                    materialSlots++;
                    if (material != null)
                        materials.Add(material);
                }
            }

            int repeats = 0;
            foreach (int references in meshes.Values)
                if (references > 1)
                    repeats += references;

            CityFoliageCullingExperiment experiment =
                Object.FindFirstObjectByType<CityFoliageCullingExperiment>();

            Debug.Log(
                "[MotorCity][Optimization] Read-only city mesh audit: " +
                renderers.Length + " MeshRenderers (" + active +
                " enabled), " + meshes.Count + " distinct meshes, " +
                repeats + " repeated-mesh renderer references, " +
                materials.Count + " distinct materials, " +
                materialSlots + " material slots, " +
                staticBatch + " renderers in static batches, " +
                missingMesh + " missing MeshFilters, " +
                triangleReferences + " referenced triangles (NOT visible triangles). " +
                city.GetComponentsInChildren<LODGroup>(true).Length +
                " LODGroups. Foliage experiment: " +
                (experiment != null && experiment.enabled
                    ? experiment.HiddenCount + "/" +
                      experiment.CandidateCount + " hidden/eligible"
                    : "OFF") +
                ". Compare Game view Stats / Profiler before and after.");
        }

        [MenuItem("Motor City/Optimization/2 - Enable Foliage A-B (Play Mode)")]
        private static void Enable()
        {
            GameObject city = GetCity();
            if (city == null)
                return;

            CityFoliageCullingExperiment experiment =
                Object.FindFirstObjectByType<CityFoliageCullingExperiment>(
                    FindObjectsInactive.Include);

            if (experiment == null)
            {
                GameObject helper = new GameObject(ExperimentName);
                experiment = helper.AddComponent<CityFoliageCullingExperiment>();
            }

            experiment.Initialize(city);
            experiment.enabled = true;
            Debug.Log("[MotorCity][Optimization] Experiment ON. " +
                "Drive around, then compare Game view Stats, Frame Debugger " +
                "and Profiler Rendering. No asset or prefab changes.");
        }

        [MenuItem("Motor City/Optimization/3 - Disable Foliage A-B (Play Mode)")]
        private static void Disable()
        {
            CityFoliageCullingExperiment experiment =
                Object.FindFirstObjectByType<CityFoliageCullingExperiment>(
                    FindObjectsInactive.Include);

            if (experiment == null)
            {
                Debug.Log("[MotorCity][Optimization] Experiment already OFF.");
                return;
            }

            int previouslyHidden = experiment.HiddenCount;
            experiment.enabled = false; // OnDisable restores every hidden mesh
            Debug.Log("[MotorCity][Optimization] Experiment OFF. Restored " +
                previouslyHidden + " decorative renderers; city unchanged.");
        }

        [MenuItem("Motor City/Optimization/4 - Preview Browser View Distance ON (Play Mode)")]
        private static void PreviewBrowser()
        {
            if (GetCity() == null)
                return;

            MotorCityVisibilityRuntime.SetEditorPreview(true);
            Debug.Log("[MotorCity][Optimization] WebGL visibility preview ON: " +
                "camera=" + MotorCityVisibilityRuntime.FarClipDistance + "m, " +
                "fog=" + MotorCityVisibilityRuntime.FogStartDistance + "-" +
                MotorCityVisibilityRuntime.FogEndDistance + "m, preset=" +
                MotorCity.Platform.MotorCityQualityRuntime.CurrentPreset +
                ". Editor-only preview; foliage A/B is a separate switch.");
        }

        [MenuItem("Motor City/Optimization/5 - Restore Editor View Distance (Play Mode)")]
        private static void RestoreEditor()
        {
            if (GetCity() == null)
                return;

            MotorCityVisibilityRuntime.SetEditorPreview(false);
            Debug.Log("[MotorCity][Optimization] Editor visibility restored: " +
                "camera 2200m, fog 120-520m. No scene assets modified.");
        }

        [MenuItem("Motor City/Optimization/6 - Report Visibility (Play Mode)")]
        private static void ReportVisibility()
        {
            if (GetCity() == null)
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            Debug.Log("[MotorCity][Optimization] Visibility: " +
                "camera far=" + camera.farClipPlane + "m, " +
                "fog=" + RenderSettings.fogStartDistance + "-" +
                RenderSettings.fogEndDistance + "m, " +
                "browser preview=" + MotorCityVisibilityRuntime.BrowserProfileActive +
                ", preset=" + MotorCity.Platform.MotorCityQualityRuntime.CurrentPreset +
                ". Compare the same street in Game view Stats / Profiler.");
        }

        [MenuItem("Motor City/Optimization/7 - Enable Distant Detail A-B (Play Mode)")]
        private static void EnableDetail()
        {
            GameObject city = GetCity();
            if (city == null || Camera.main == null)
                return;

            CityDistanceDetailCuller culler =
                Object.FindFirstObjectByType<CityDistanceDetailCuller>(
                    FindObjectsInactive.Include);
            if (culler == null)
            {
                GameObject host = new GameObject("MotorCity - Distant Detail A-B");
                culler = host.AddComponent<CityDistanceDetailCuller>();
                culler.Initialize(city, Camera.main);
            }

            culler.enabled = true;
            culler.RequestRefresh();
            Debug.Log("[MotorCity][Optimization] Distant detail ON. " +
                "Wait for scan completion; use menu 9 for diagnostics.");
        }

        [MenuItem("Motor City/Optimization/8 - Disable Distant Detail A-B (Play Mode)")]
        private static void DisableDetail()
        {
            if (GetCity() == null)
                return;
            CityDistanceDetailCuller culler =
                Object.FindFirstObjectByType<CityDistanceDetailCuller>(
                    FindObjectsInactive.Include);
            if (culler == null)
                return;
            int hidden = culler.HiddenCount;
            culler.enabled = false;
            Debug.Log("[MotorCity][Optimization] Distant detail OFF; restored " +
                hidden + " renderers.");
        }

        [MenuItem("Motor City/Optimization/9 - Report Distant Detail (Play Mode)")]
        private static void ReportDetail()
        {
            if (GetCity() == null)
                return;
            CityDistanceDetailCuller culler =
                Object.FindFirstObjectByType<CityDistanceDetailCuller>(
                    FindObjectsInactive.Include);
            Debug.Log(culler == null
                ? "[MotorCity][Optimization] Distant detail not initialized."
                : "[MotorCity][Optimization] Distant detail " +
                  (culler.enabled ? "ON" : "OFF") + ": " +
                  culler.HiddenCount + "/" + culler.CandidateCount +
                  " hidden, " + culler.HiddenTriangleReferences +
                  " triangle references excluded (not measured GPU triangles). " +
                  "Scan finished=" + culler.ScanReady);
        }

        [MenuItem("Motor City/Optimization/HLOD/3 - Enable HLOD A-B (Play Mode)")]
        private static void EnableHlod()
        {
            GameObject city = GetCity();
            Camera camera = Camera.main;
            if (city == null || camera == null)
                return;

            CityHlodData data = CityHlodRuntime.LoadData();
            if (data == null)
            {
                Debug.LogWarning("[MotorCity][HLOD] No baked HLOD asset. " +
                    "Exit Play Mode and choose HLOD/2 - Bake Proxies.");
                return;
            }

            CityHlodRuntime hlod =
                Object.FindFirstObjectByType<CityHlodRuntime>(
                    FindObjectsInactive.Include);
            if (hlod == null)
            {
                GameObject host = new GameObject("MotorCity - HLOD A-B");
                hlod = host.AddComponent<CityHlodRuntime>();
                if (!hlod.Initialize(city, camera, data))
                {
                    Object.Destroy(host);
                    return;
                }
            }
            hlod.enabled = true;
            hlod.RequestRefresh();
            Debug.Log("[MotorCity][HLOD] A/B ON. Use menu HLOD/5 " +
                "to report proxy counts. Only renderer visibility changes.");
        }

        [MenuItem("Motor City/Optimization/HLOD/4 - Disable HLOD A-B (Play Mode)")]
        private static void DisableHlod()
        {
            if (GetCity() == null)
                return;
            CityHlodRuntime hlod =
                Object.FindFirstObjectByType<CityHlodRuntime>(
                    FindObjectsInactive.Include);
            if (hlod != null)
            {
                hlod.enabled = false;
                Debug.Log("[MotorCity][HLOD] A/B OFF: all swapped " +
                    "original renderers restored.");
            }
        }

        [MenuItem("Motor City/Optimization/HLOD/5 - Report HLOD (Play Mode)")]
        private static void ReportHlod()
        {
            if (GetCity() == null)
                return;
            CityHlodRuntime hlod =
                Object.FindFirstObjectByType<CityHlodRuntime>(
                    FindObjectsInactive.Include);
            Debug.Log(hlod == null || !hlod.Initialized
                ? "[MotorCity][HLOD] Not active / no valid baked data."
                : "[MotorCity][HLOD] " + (hlod.enabled ? "ON" : "OFF") +
                  ": active proxies " + hlod.ActiveProxyCount +
                  "/" + hlod.CellCount +
                  ". Compare identical viewpoints using Game Stats and Profiler.");
        }

        private static GameObject GetCity()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[MotorCity][Optimization] Start Play Mode " +
                    "and wait for the city to load before measuring.");
                return null;
            }

            GameObject city = GameObject.Find(CityName);
            if (city == null)
                Debug.LogWarning("[MotorCity][Optimization] The runtime FCG " +
                    "city has not loaded yet. Try again after loading.");
            return city;
        }
    }
}
