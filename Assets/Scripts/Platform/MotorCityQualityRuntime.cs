using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MotorCity.Platform
{
    public enum MotorCityQualityPreset
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public static class MotorCityQualityRuntime
    {
        private const string SaveKey =
            "MotorCity.Settings.QualityPreset";

        private static bool initialized;

        public static MotorCityQualityPreset CurrentPreset { get; private set; }

        public static event Action PresetChanged;

        public static int TrafficVehicleBudget
        {
            get
            {
                return
                    CurrentPreset switch
                    {
                        MotorCityQualityPreset.Low =>
#if UNITY_WEBGL && !UNITY_EDITOR
                            10,
#else
                            16,
#endif

                        MotorCityQualityPreset.High =>
#if UNITY_WEBGL && !UNITY_EDITOR
                            24,
#else
                            32,
#endif

                        _ =>
#if UNITY_WEBGL && !UNITY_EDITOR
                            18
#else
                            28
#endif
                    };
            }
        }

        public static float TrafficRadius
        {
            get
            {
                return
                    CurrentPreset switch
                    {
                        MotorCityQualityPreset.Low =>
#if UNITY_WEBGL && !UNITY_EDITOR
                            105f,
#else
                            120f,
#endif

                        MotorCityQualityPreset.High =>
#if UNITY_WEBGL && !UNITY_EDITOR
                            140f,
#else
                            165f,
#endif

                        _ =>
#if UNITY_WEBGL && !UNITY_EDITOR
                            125f
#else
                            150f
#endif
                    };
            }
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            int stored =
                PlayerPrefs.GetInt(
                    SaveKey,
                    -1);

            MotorCityQualityPreset preset =
                stored >= 0 &&
                stored <= 2
                    ? (MotorCityQualityPreset)stored
                    : DetectRecommendedPreset();

            Apply(
                preset,
                false);

            initialized =
                true;
        }

        public static void Apply(
            MotorCityQualityPreset preset,
            bool save = true)
        {
            CurrentPreset =
                preset;

            QualitySettings.vSyncCount = 0;

            // Let the browser/device run as fast as it can. Quality presets
            // should change visual cost, not impose an artificial FPS cap.
            Application.targetFrameRate = -1;

            switch (preset)
            {
                case MotorCityQualityPreset.Low:
                    ApplyLow();
                    break;

                case MotorCityQualityPreset.High:
                    ApplyHigh();
                    break;

                default:
                    ApplyMedium();
                    break;
            }

            PresetChanged?.Invoke();

            if (!save)
                return;

            PlayerPrefs.SetInt(
                SaveKey,
                (int)preset);

            PlayerPrefs.Save();
        }

        private static MotorCityQualityPreset DetectRecommendedPreset()
        {
            int memoryMb =
                SystemInfo.systemMemorySize;

            int graphicsMemoryMb =
                SystemInfo.graphicsMemorySize;

            bool lowMemory =
                (memoryMb > 0 &&
                 memoryMb <= 4096) ||
                (graphicsMemoryMb > 0 &&
                 graphicsMemoryMb <= 1024);

            if (Application.isMobilePlatform ||
                Application.platform ==
                RuntimePlatform.WebGLPlayer)
            {
                if (lowMemory)
                {
                    return
                        MotorCityQualityPreset.Low;
                }

                // Browsers often report zero/unknown hardware memory values.
                // Default WebGL to Medium instead of accidentally selecting
                // expensive High rendering on unknown devices.
                return
                    MotorCityQualityPreset.Medium;
            }

            if (lowMemory)
            {
                return
                    MotorCityQualityPreset.Low;
            }

            if ((memoryMb > 0 &&
                 memoryMb <= 8192) ||
                (graphicsMemoryMb > 0 &&
                 graphicsMemoryMb <= 2048))
            {
                return
                    MotorCityQualityPreset.Medium;
            }

            return
                MotorCityQualityPreset.High;
        }

        private static void ApplyLow()
        {
            QualitySettings.shadows =
                UnityEngine.ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.pixelLightCount = 0;
            QualitySettings.lodBias = 0.65f;
            QualitySettings.maximumLODLevel = 1;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.anisotropicFiltering =
                AnisotropicFiltering.Disable;

            ApplyUrpQuality(
                0.75f,
                1,
                0f,
                1,
                512);
        }

        private static void ApplyMedium()
        {
            QualitySettings.shadows =
                UnityEngine.ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = 55f;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.lodBias = 0.85f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.anisotropicFiltering =
                AnisotropicFiltering.Enable;

            ApplyUrpQuality(
                0.90f,
                2,
                75f,
                2,
                1024);
        }

        private static void ApplyHigh()
        {
            QualitySettings.shadows =
                UnityEngine.ShadowQuality.All;
            QualitySettings.shadowDistance = 95f;
            QualitySettings.pixelLightCount = 2;
            QualitySettings.lodBias = 1f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.realtimeReflectionProbes = true;
            QualitySettings.softParticles = true;
            QualitySettings.anisotropicFiltering =
                AnisotropicFiltering.ForceEnable;

            ApplyUrpQuality(
                1.00f,
                4,
                120f,
                4,
                2048);
        }

        private static void ApplyUrpQuality(
            float renderScale,
            int msaaSamples,
            float shadowDistance,
            int shadowCascadeCount,
            int mainShadowResolution)
        {
            UniversalRenderPipelineAsset urp =
                GraphicsSettings.currentRenderPipeline
                    as UniversalRenderPipelineAsset;

            if (urp == null)
                return;

            urp.renderScale =
                Mathf.Clamp(
                    renderScale,
                    0.5f,
                    1.5f);

            urp.msaaSampleCount =
                msaaSamples;

            urp.shadowDistance =
                Mathf.Max(
                    0f,
                    shadowDistance);

            urp.shadowCascadeCount =
                Mathf.Clamp(
                    shadowCascadeCount,
                    1,
                    4);

            urp.mainLightShadowmapResolution =
                Mathf.Max(
                    256,
                    mainShadowResolution);

        }
    }
}
