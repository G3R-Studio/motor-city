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
        private float nextCityCheck;
        private float nextDetailCheck;
        private bool detailUrlChecked;
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
                Object.FindFirstObjectByType<MotorCityVisibilityRuntime>();
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
        }

        private void OnDisable()
        {
            MotorCityQualityRuntime.PresetChanged -= ApplyDistances;
            if (browserFoliage != null)
                browserFoliage.enabled = false;
            if (browserDetails != null)
                browserDetails.enabled = false;
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

        private void Update()
        {
            if (appliedPreset != MotorCityQualityRuntime.CurrentPreset ||
                appliedBrowserMode != BrowserProfileActive)
                ApplyDistances();

#if UNITY_WEBGL && !UNITY_EDITOR
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

            // Only decorative vegetation, and only on WebGL Low/Medium.
            // The Editor's existing foliage A/B menu remains independent.
            // Important buildings, roads, traffic and colliders stay intact.
            bool shouldCull =
                MotorCityQualityRuntime.CurrentPreset != MotorCityQualityPreset.High;

            if (!shouldCull)
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
