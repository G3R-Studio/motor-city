using System;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// WebGL view-distance policy for the runtime city. Never edits authored
    /// meshes, colliders, materials, layers, asset import settings or prefabs.
    /// Editor preview is opt-in; the standard Editor camera stays at 2200m.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class MotorCityVisibilityRuntime : MonoBehaviour
    {
        private const float EditorFarClip = 2200f;
        private const float EditorFogStart = 120f;
        private const float EditorFogEnd = 520f;
        private static bool editorPreview;

        private Camera targetCamera;
        private CityFoliageCullingExperiment browserFoliage;
        private CityDistanceDetailCuller browserDetails;
        private CityParkedVehicleCullingExperiment browserParked;
        private CityDistantTreeCullingExperiment browserVegetation;
        private CityHlodRuntime browserHlod;
        private bool hlodChecked;
        private bool hlodAvailable;
        private float nextHlodCheck;
        private float nextCityCheck;
        private float nextDetailCheck;
        private float nextExtraCullCheck;
        private bool detailUrlChecked;
        private bool extraCullUrlChecked;
#if UNITY_WEBGL && !UNITY_EDITOR
        private bool allowParkedCull = true;
        private bool allowVegetationCull = true;
#endif
        private MotorCityQualityPreset appliedPreset;
        private bool appliedBrowserMode;

        public static bool BrowserProfileActive
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return editorPreview;
#endif
            }
        }

        public static float FarClipDistance =>
            !BrowserProfileActive ? EditorFarClip :
            MotorCityQualityRuntime.CurrentPreset switch
            {
                MotorCityQualityPreset.Low => 900f,
                MotorCityQualityPreset.High => 1700f,
                _ => 1250f
            };

        public static float FogStartDistance =>
            !BrowserProfileActive ? EditorFogStart :
            MotorCityQualityRuntime.CurrentPreset switch
            {
                MotorCityQualityPreset.Low => 160f,
                MotorCityQualityPreset.High => 310f,
                _ => 230f
            };

        public static float FogEndDistance =>
            !BrowserProfileActive ? EditorFogEnd :
            MotorCityQualityRuntime.CurrentPreset switch
            {
                MotorCityQualityPreset.Low => 820f,
                MotorCityQualityPreset.High => 1560f,
                _ => 1140f
            };

        public static float FoliageHideDistance =>
            !BrowserProfileActive ? 145f :
            MotorCityQualityRuntime.CurrentPreset switch
            {
                MotorCityQualityPreset.Low => 245f,
                MotorCityQualityPreset.High => 460f,
                _ => 345f
            };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEditorPreview()
        {
            editorPreview = false;
        }

        /// <summary>Editor Play Mode A/B test; not serialized to the project.</summary>
        public static void SetEditorPreview(bool enabled)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;

            editorPreview = enabled;

            MotorCityVisibilityRuntime runtime =
                UnityEngine.Object.FindFirstObjectByType<MotorCityVisibilityRuntime>();
            if (runtime != null)
                runtime.ApplyDistances();

            // DayNightCycleController refreshes these every environment tick.
            // Update immediately too so the A/B button is responsive.
            RenderSettings.fogStartDistance = FogStartDistance;
            RenderSettings.fogEndDistance = FogEndDistance;
#endif
        }

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            MotorCityQualityRuntime.PresetChanged += ApplyDistances;
            ApplyDistances();
#if UNITY_WEBGL && !UNITY_EDITOR
            // Components may survive a temporary camera/runtime disable.
            // Restore only our own WebGL cullers, respecting URL opt-outs.
            if (browserDetails != null)
                browserDetails.enabled =
                    nextDetailCheck != float.PositiveInfinity;
            bool high = MotorCityQualityRuntime.CurrentPreset ==
                MotorCityQualityPreset.High;
            if (browserParked != null)
                browserParked.enabled = high && allowParkedCull;
            if (browserVegetation != null)
                browserVegetation.enabled = high && allowVegetationCull;
#endif
        }

        private void OnDisable()
        {
            MotorCityQualityRuntime.PresetChanged -= ApplyDistances;
            if (browserFoliage != null)
                browserFoliage.enabled = false;
            if (browserDetails != null)
                browserDetails.enabled = false;
            if (browserParked != null)
                browserParked.enabled = false;
            if (browserVegetation != null)
                browserVegetation.enabled = false;
            if (browserHlod != null)
                browserHlod.enabled = false;
        }

        private void ApplyDistances()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();

            if (targetCamera != null)
                targetCamera.farClipPlane = FarClipDistance;

            appliedPreset = MotorCityQualityRuntime.CurrentPreset;
            appliedBrowserMode = BrowserProfileActive;
            if (browserDetails != null)
                browserDetails.RequestRefresh();
            if (browserParked != null)
                browserParked.RequestRefresh();
            if (browserVegetation != null)
                browserVegetation.RequestRefresh();
            if (browserHlod != null)
                browserHlod.RequestRefresh();

            if (BrowserProfileActive)
            {
                RenderSettings.fogStartDistance = FogStartDistance;
                RenderSettings.fogEndDistance = FogEndDistance;
            }
#if UNITY_EDITOR
            else
            {
                RenderSettings.fogStartDistance = EditorFogStart;
                RenderSettings.fogEndDistance = EditorFogEnd;
            }
#endif
        }

        private static bool UrlOptedOut(string url, string key)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            // Recognize only a query parameter, not a coincidental
            // substring in the path, a different value or another key.
            int query = url.IndexOf('?');
            if (query < 0)
                return false;
            int fragment = url.IndexOf('#', query + 1);
            string wanted = key + "=off";
            int searchFrom = query + 1;
            while (searchFrom < url.Length &&
                   (fragment < 0 || searchFrom < fragment))
            {
                int next = url.IndexOf('&', searchFrom);
                if (next < 0 || (fragment >= 0 && next > fragment))
                    next = fragment >= 0 ? fragment : url.Length;
                if (next - searchFrom == wanted.Length &&
                    string.Compare(url, searchFrom, wanted, 0,
                        wanted.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
                searchFrom = next + 1;
            }
            return false;
        }

        private void Update()
        {
            if (appliedPreset != MotorCityQualityRuntime.CurrentPreset ||
                appliedBrowserMode != BrowserProfileActive)
                ApplyDistances();

#if UNITY_WEBGL && !UNITY_EDITOR
            // HLOD is entirely optional: no data asset means no runtime
            // change. An explicit URL opt-out supports future WebGL A/B.
            if (!hlodChecked)
            {
                hlodChecked = true;
                string url = Application.absoluteURL;
                bool hlodDisabled = !string.IsNullOrEmpty(url) &&
                    url.IndexOf("motorcity_hlod=off",
                        StringComparison.OrdinalIgnoreCase) >= 0;
                hlodAvailable = !hlodDisabled && CityHlodRuntime.LoadData() != null;
                if (hlodDisabled)
                    Debug.Log("[MotorCity][HLOD] Disabled via URL A/B switch.");
            }
            if (hlodAvailable && browserHlod == null &&
                Time.unscaledTime >= nextHlodCheck)
            {
                nextHlodCheck = Time.unscaledTime + 1f;
                GameObject hlodCity = GameObject.Find("MotorCity_FCGCity");
                if (hlodCity != null)
                {
                    CityHlodRuntime culler = gameObject.AddComponent<CityHlodRuntime>();
                    if (culler.Initialize(hlodCity, targetCamera,
                        CityHlodRuntime.LoadData()))
                        browserHlod = culler;
                    else
                    {
                        Destroy(culler);
                        hlodAvailable = false;
                    }
                }
            }

            // Compare the same browser viewpoint with and without this
            // experimental distant detail culler using a URL query.
            if (!detailUrlChecked)
            {
                detailUrlChecked = true;
                string url = Application.absoluteURL;
                if (!string.IsNullOrEmpty(url) &&
                    url.IndexOf("motorcity_detail_cull=off",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    nextDetailCheck = float.PositiveInfinity;
                    Debug.Log("[MotorCity][Optimization] Distant detail OFF via URL.");
                }
            }

            if (browserDetails == null && Time.unscaledTime >= nextDetailCheck)
            {
                nextDetailCheck = Time.unscaledTime + 1f;
                GameObject detailCity = GameObject.Find("MotorCity_FCGCity");
                if (detailCity != null)
                {
                    browserDetails = gameObject.AddComponent<CityDistanceDetailCuller>();
                    browserDetails.Initialize(detailCity, targetCamera);
                }
            }

            // Production URL opt-outs are read once per browser run:
            // ?motorcity_detail_cull=off
            // ?motorcity_parked_cull=off
            // ?motorcity_vegetation_cull=off
            // Multiple switches may be combined using '&'.
            if (!extraCullUrlChecked)
            {
                extraCullUrlChecked = true;
                string url = Application.absoluteURL;
                allowParkedCull = !UrlOptedOut(url,
                    "motorcity_parked_cull");
                allowVegetationCull = !UrlOptedOut(url,
                    "motorcity_vegetation_cull");
                Debug.Log("[MotorCity][Optimization] WebGL culling " +
                    "switches: details=" +
                    (nextDetailCheck != float.PositiveInfinity) +
                    ", parked=" + allowParkedCull +
                    ", vegetation=" + allowVegetationCull +
                    ". High uses the Editor-validated thresholds.");
            }

            // Only High has been visually and automatically benchmarked
            // with parked cars and BOTH Trees-01 vegetation categories.
            // Do not silently apply untested thresholds on Low/Medium.
            bool highPreset = MotorCityQualityRuntime.CurrentPreset ==
                MotorCityQualityPreset.High;
            bool shouldParked = highPreset && allowParkedCull;
            bool shouldVegetation = highPreset && allowVegetationCull;

            if (browserParked != null)
            {
                if (browserParked.enabled != shouldParked)
                {
                    browserParked.enabled = shouldParked;
                    if (shouldParked)
                        browserParked.RequestRefresh();
                }
            }

            if (browserVegetation != null)
            {
                if (browserVegetation.enabled != shouldVegetation)
                {
                    browserVegetation.enabled = shouldVegetation;
                    if (shouldVegetation)
                        browserVegetation.RequestRefresh();
                }
            }

            // At most one full city discovery array is allocated per
            // interval; per-frame candidate discovery remains budgeted.
            // Preserve low startup memory pressure and scan performance:
            // wait for the existing detail scan, then parked scan, before
            // allocating/discovering the next 21k-renderer candidate array.
            bool detailsReady = nextDetailCheck == float.PositiveInfinity ||
                (browserDetails != null && browserDetails.ScanReady);
            bool parkedReady = !shouldParked ||
                (browserParked != null && browserParked.ScanReady);
            if (highPreset && detailsReady &&
                Time.unscaledTime >= nextExtraCullCheck &&
                ((shouldParked && browserParked == null) ||
                 (shouldVegetation && parkedReady &&
                  browserVegetation == null)))
            {
                nextExtraCullCheck = Time.unscaledTime + 0.5f;
                GameObject city = GameObject.Find("MotorCity_FCGCity");
                if (city != null)
                {
                    if (shouldParked && browserParked == null)
                    {
                        browserParked = gameObject.AddComponent<
                            CityParkedVehicleCullingExperiment>();
                        browserParked.Initialize(city, targetCamera);
                        Debug.Log("[MotorCity][Optimization] WebGL High " +
                            "parked vehicles ON (390m hide / 335m restore).");
                    }
                    else if (shouldVegetation && parkedReady &&
                             browserVegetation == null)
                    {
                        browserVegetation = gameObject.AddComponent<
                            CityDistantTreeCullingExperiment>();
                        browserVegetation.IncludeWoody = true;
                        browserVegetation.IncludeGroundCover = true;
                        browserVegetation.Initialize(city, targetCamera);
                        Debug.Log("[MotorCity][Optimization] WebGL High " +
                            "vegetation ON (500m, under 12px / 1080p).");
                    }
                }
            }

            // Preserve the existing, separate Low/Medium decorative
            // foliage experiment. NEVER run it alongside the new High
            // Trees-01 renderer culler: the former changes enabled,
            // while the new component uses forceRenderingOff.
            // The vegetation URL opt-out disables BOTH categories at
            // High and legacy foliage at Low/Medium.
            bool shouldCullLegacyFoliage =
                !highPreset && allowVegetationCull;

            if (!shouldCullLegacyFoliage)
            {
                if (browserFoliage != null && browserFoliage.enabled)
                    browserFoliage.enabled = false;
                return;
            }

            if (browserFoliage != null)
            {
                if (!browserFoliage.enabled)
                {
                    GameObject city = GameObject.Find("MotorCity_FCGCity");
                    if (city != null)
                    {
                        browserFoliage.Initialize(city);
                        browserFoliage.enabled = true;
                    }
                }
                return;
            }

            if (Time.unscaledTime < nextCityCheck)
                return;
            nextCityCheck = Time.unscaledTime + 1f;

            GameObject cityRoot = GameObject.Find("MotorCity_FCGCity");
            if (cityRoot == null)
                return;

            browserFoliage = gameObject.AddComponent<CityFoliageCullingExperiment>();
            browserFoliage.Initialize(cityRoot);
#endif
        }
    }
}
