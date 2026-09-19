using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    public sealed class DayNightCycleController : MonoBehaviour
    {
        private const string SettingsResourcePath =
            "MotorCity/Environment/DayNightSettings";

        [SerializeField] private float fullCycleSeconds =
            480f;

        [SerializeField] private float startTime01 =
            0.38f;

        [SerializeField] private float sunYawDegrees =
            -28f;

        private readonly List<Light> streetLights =
            new();

        private DayNightSettings settings;
        private Light directionalLight;
        private Light moonLight;
        private Material runtimeDaySkybox;
        private Material runtimeNightSkybox;

        private float time01;
        private float streetLightRefreshTimer;
        private bool lastNightState;
        private bool initialized;

        public bool IsNight { get; private set; }
        public float NightAmount { get; private set; }
        public float TimeOfDay01 => time01;

        public void Initialize(
            Light sun)
        {
            directionalLight =
                sun;

            settings =
                Resources.Load<DayNightSettings>(
                    SettingsResourcePath);

            CreateMoonLight();

            time01 =
                Mathf.Repeat(
                    startTime01,
                    1f);

            BuildRuntimeSkyboxes();
            RefreshStreetLights();

            Debug.Log(
                "Motor City: day/night prepared. " +
                $"FCG spot lights={streetLights.Count}.");

            ApplyEnvironment(
                true);

            initialized =
                true;
        }

        private void Update()
        {
            if (!initialized)
                return;

            if (fullCycleSeconds > 0.1f)
            {
                time01 =
                    Mathf.Repeat(
                        time01 +
                        Time.deltaTime /
                        fullCycleSeconds,
                        1f);
            }

            ApplyEnvironment(
                false);

            streetLightRefreshTimer -=
                Time.deltaTime;

            if (streetLightRefreshTimer <= 0f)
            {
                streetLightRefreshTimer =
                    0.5f;

                RefreshStreetLights();
                ApplyStreetLights();
            }
        }

        private void OnDestroy()
        {
            if (runtimeDaySkybox != null)
                Destroy(
                    runtimeDaySkybox);

            if (runtimeNightSkybox != null)
                Destroy(
                    runtimeNightSkybox);
        }

        private void CreateMoonLight()
        {
            GameObject moonObject =
                new("Moon");

            moonObject.transform.SetParent(
                transform,
                false);

            moonLight =
                moonObject.AddComponent<Light>();

            moonLight.type =
                LightType.Directional;

            moonLight.shadows =
                LightShadows.None;

            moonLight.intensity =
                0f;

            moonLight.enabled =
                false;
        }

        private void BuildRuntimeSkyboxes()
        {
            if (settings == null)
                return;

            if (settings.DaySkybox != null)
            {
                runtimeDaySkybox =
                    new Material(
                        settings.DaySkybox)
                    {
                        name =
                            settings.DaySkybox.name +
                            "_MotorCityRuntime"
                    };
            }

            if (settings.NightSkybox != null)
            {
                runtimeNightSkybox =
                    new Material(
                        settings.NightSkybox)
                    {
                        name =
                            settings.NightSkybox.name +
                            "_MotorCityRuntime"
                    };
            }
        }

        private void ApplyEnvironment(
            bool force)
        {
            if (directionalLight == null)
                return;

            float solarAngle =
                time01 * 360f -
                90f;

            directionalLight.transform.rotation =
                Quaternion.Euler(
                    solarAngle,
                    sunYawDegrees,
                    0f);

            float solarHeight =
                -directionalLight.transform.forward.y;

            float daylight =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(
                        -0.12f,
                        0.18f,
                        solarHeight));

            NightAmount =
                1f -
                daylight;

            IsNight =
                NightAmount >=
                0.58f;

            Color daySky =
                settings != null
                    ? settings.DaySkyColor
                    : new Color(
                        0.34f,
                        0.40f,
                        0.48f);

            Color dayEquator =
                settings != null
                    ? settings.DayEquatorColor
                    : new Color(
                        0.19f,
                        0.20f,
                        0.22f);

            Color nightSky =
                settings != null
                    ? settings.NightSkyColor *
                      0.22f
                    : new Color(
                        0.045f,
                        0.06f,
                        0.11f);

            nightSky.a =
                1f;

            Color nightEquator =
                settings != null
                    ? settings.NightEquatorColor *
                      0.18f
                    : new Color(
                        0.018f,
                        0.022f,
                        0.04f);

            nightEquator.a =
                1f;

            RenderSettings.ambientMode =
                AmbientMode.Trilight;

            RenderSettings.ambientSkyColor =
                Color.Lerp(
                    nightSky,
                    daySky,
                    daylight);

            RenderSettings.ambientEquatorColor =
                Color.Lerp(
                    nightEquator,
                    dayEquator,
                    daylight);

            RenderSettings.ambientGroundColor =
                Color.Lerp(
                    new Color(
                        0.01f,
                        0.012f,
                        0.02f),
                    new Color(
                        0.075f,
                        0.072f,
                        0.07f),
                    daylight);

            RenderSettings.fog =
                true;

            RenderSettings.fogMode =
                FogMode.Linear;

            RenderSettings.fogColor =
                Color.Lerp(
                    new Color(
                        0.035f,
                        0.05f,
                        0.085f),
                    new Color(
                        0.55f,
                        0.61f,
                        0.67f),
                    daylight);

            RenderSettings.fogStartDistance =
                Mathf.Lerp(
                    180f,
                    260f,
                    daylight);

            RenderSettings.fogEndDistance =
                Mathf.Lerp(
                    760f,
                    980f,
                    daylight);

            Color sunColor =
                settings != null
                    ? settings.SunColor
                    : new Color(
                        1f,
                        0.94f,
                        0.84f);

            Color moonColor =
                settings != null
                    ? settings.MoonColor
                    : new Color(
                        0.56f,
                        0.62f,
                        0.82f);

            float sunIntensity =
                settings != null
                    ? settings.SunIntensity
                    : 1.05f;

            float moonIntensity =
                settings != null
                    ? settings.MoonIntensity
                    : 0.28f;

            directionalLight.color =
                sunColor;

            directionalLight.intensity =
                sunIntensity *
                daylight;

            directionalLight.shadows =
                daylight > 0.12f
                    ? LightShadows.Soft
                    : LightShadows.None;

            if (moonLight != null)
            {
                moonLight.transform.rotation =
                    Quaternion.Euler(
                        solarAngle +
                        180f,
                        sunYawDegrees +
                        180f,
                        0f);

                moonLight.color =
                    moonColor;

                moonLight.intensity =
                    moonIntensity *
                    NightAmount;

                moonLight.enabled =
                    NightAmount >
                    0.04f;
            }

            bool useNightSkybox =
                NightAmount >=
                0.5f;

            Material targetSkybox =
                useNightSkybox
                    ? runtimeNightSkybox
                    : runtimeDaySkybox;

            if (targetSkybox != null &&
                (force ||
                 RenderSettings.skybox !=
                 targetSkybox))
            {
                RenderSettings.skybox =
                    targetSkybox;

                DynamicGI.UpdateEnvironment();
            }

            if (force ||
                IsNight !=
                lastNightState)
            {
                lastNightState =
                    IsNight;

                ApplyStreetLights();
            }
        }

        private void RefreshStreetLights()
        {
            streetLights.Clear();

            Light[] all =
                UnityEngine.Object.FindObjectsByType<Light>(
                    FindObjectsInactive.Include);

            foreach (Light light in all)
            {
                if (light == null ||
                    light == directionalLight ||
                    light == moonLight)
                    continue;

                if (!IsRuntimeCityHierarchy(
                        light.transform))
                    continue;

                if (!IsFcgSpotLight(
                        light.transform))
                    continue;

                light.lightmapBakeType =
                    LightmapBakeType.Realtime;

                light.shadows =
                    LightShadows.None;

                streetLights.Add(
                    light);
            }
        }

        private void ApplyStreetLights()
        {
            for (int i =
                     streetLights.Count -
                     1;
                 i >= 0;
                 i--)
            {
                Light light =
                    streetLights[i];

                if (light == null)
                {
                    streetLights.RemoveAt(
                        i);

                    continue;
                }

                bool shouldEnable =
                    IsNight;

                if (shouldEnable)
                {
                    ActivateLightHierarchy(
                        light.transform);
                }

                light.enabled =
                    shouldEnable;
            }
        }

        private static void ActivateLightHierarchy(
            Transform lightTransform)
        {
            Transform current =
                lightTransform;

            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(
                        true);
                }

                if (IsLampRoot(
                        current))
                    return;

                if (IsRuntimeCityRoot(
                        current))
                    return;

                current =
                    current.parent;
            }
        }

        private static bool IsFcgSpotLight(
            Transform item)
        {
            if (item == null)
                return false;

            string normalized =
                NormalizeName(
                    item.name);

            return
                normalized.StartsWith(
                    "spotlight",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLampRoot(
            Transform item)
        {
            if (item == null)
                return false;

            string normalized =
                NormalizeName(
                    item.name);

            return
                normalized.StartsWith(
                    "streetlight") ||
                normalized.StartsWith(
                    "parklamp");
        }

        private static bool IsRuntimeCityHierarchy(
            Transform item)
        {
            Transform current =
                item;

            while (current != null)
            {
                if (IsRuntimeCityRoot(
                        current))
                    return true;

                current =
                    current.parent;
            }

            return false;
        }

        private static bool IsRuntimeCityRoot(
            Transform item)
        {
            if (item == null)
                return false;

            return
                string.Equals(
                    item.name,
                    "MotorCity_FCGCity",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    item.name,
                    "City-Maker",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
                return string.Empty;

            char[] source =
                value.ToLowerInvariant()
                    .ToCharArray();

            var chars =
                new List<char>(
                    source.Length);

            foreach (char character in source)
            {
                if (char.IsLetterOrDigit(
                        character))
                {
                    chars.Add(
                        character);
                }
            }

            return
                new string(
                    chars.ToArray());
        }
    }
}
