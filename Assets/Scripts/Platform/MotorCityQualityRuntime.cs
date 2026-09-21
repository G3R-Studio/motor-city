using UnityEngine;

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

        public static MotorCityQualityPreset CurrentPreset { get; private set; }

        public static void Initialize()
        {
            int stored =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
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
        }

        public static void Apply(
            MotorCityQualityPreset preset,
            bool save = true)
        {
            CurrentPreset =
                preset;

            QualitySettings.vSyncCount = 0;

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

            if (!save)
                return;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                SaveKey,
                (int)preset);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private static MotorCityQualityPreset DetectRecommendedPreset()
        {
            int memoryMb =
                SystemInfo.systemMemorySize;

            int graphicsMemoryMb =
                SystemInfo.graphicsMemorySize;

            if (Application.isMobilePlatform)
            {
                if ((memoryMb > 0 &&
                     memoryMb <= 4096) ||
                    (graphicsMemoryMb > 0 &&
                     graphicsMemoryMb <= 1024))
                {
                    return
                        MotorCityQualityPreset.Low;
                }

                return
                    MotorCityQualityPreset.Medium;
            }

            if ((memoryMb > 0 &&
                 memoryMb <= 4096) ||
                (graphicsMemoryMb > 0 &&
                 graphicsMemoryMb <= 1024))
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
            Application.targetFrameRate = 30;
            QualitySettings.shadows =
                ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.pixelLightCount = 0;
            QualitySettings.lodBias = 0.65f;
            QualitySettings.maximumLODLevel = 1;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.anisotropicFiltering =
                AnisotropicFiltering.Disable;
        }

        private static void ApplyMedium()
        {
            Application.targetFrameRate = 60;
            QualitySettings.shadows =
                ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = 55f;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.lodBias = 0.85f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.anisotropicFiltering =
                AnisotropicFiltering.Enable;
        }

        private static void ApplyHigh()
        {
            Application.targetFrameRate = 60;
            QualitySettings.shadows =
                ShadowQuality.All;
            QualitySettings.shadowDistance = 95f;
            QualitySettings.pixelLightCount = 2;
            QualitySettings.lodBias = 1f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.realtimeReflectionProbes = true;
            QualitySettings.softParticles = true;
            QualitySettings.anisotropicFiltering =
                AnisotropicFiltering.ForceEnable;
        }
    }
}
