using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    public sealed class DayNightCycleController : MonoBehaviour
    {
        private const string SettingsResourcePath =
            "MotorCity/Environment/DayNightSettings";

        private const float LampUpdateInterval =
            0.25f;

        private const float LampEnableDistance =
            110f;

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
        private float lampUpdateTimer;
        private float observerResolveTimer;
        private int autoCreatedStreetLights;
        private Transform lampObserver;
        private bool lastNightState;
        private bool initialized;

        public bool IsNight { get; private set; }
        public float NightAmount { get; private set; }
        public float TimeOfDay01 => time01;
        public int StreetLightCount => streetLights.Count;
        public int AutoCreatedStreetLightCount => autoCreatedStreetLights;
        public int EnabledStreetLightCount { get; private set; }

        public void SetTimeOfDay(
            float normalizedTime)
        {
            time01 =
                Mathf.Repeat(
                    normalizedTime,
                    1f);

            if (!initialized)
                return;

            ApplyEnvironment(
                true);

            ResolveLampObserver();
            ApplyStreetLights();
        }

        public void SetDay()
        {
            SetTimeOfDay(
                0.50f);
        }

        public void SetNight()
        {
            SetTimeOfDay(
                0.00f);
        }

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
            ForceAdditionalLightsSupport();
            RefreshStreetLights();

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

            lampUpdateTimer -=
                Time.deltaTime;

            if (lampUpdateTimer <= 0f)
            {
                lampUpdateTimer =
                    LampUpdateInterval;

                ResolveLampObserver();
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

            float twilight =
                1f -
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(
                        0.015f,
                        0.20f,
                        Mathf.Abs(
                            solarHeight)));

            Shader.SetGlobalFloat(
                "_MotorCityNightEmission",
                NightAmount);

            // Keep daytime reflections intact, but reduce environment
            // reflections at night so URP Lit surfaces do not look like
            // wet plastic under dense realtime street lighting.
            RenderSettings.reflectionIntensity =
                Mathf.Lerp(
                    1f,
                    0.22f,
                    NightAmount);

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
                      0.16f
                    : new Color(
                        0.045f,
                        0.06f,
                        0.11f);

            nightSky.a =
                1f;

            Color nightEquator =
                settings != null
                    ? settings.NightEquatorColor *
                      0.12f
                    : new Color(
                        0.018f,
                        0.022f,
                        0.04f);

            nightEquator.a =
                1f;

            RenderSettings.ambientMode =
                AmbientMode.Trilight;

            Color ambientSky =
                Color.Lerp(
                    nightSky,
                    daySky,
                    daylight);

            Color ambientEquator =
                Color.Lerp(
                    nightEquator,
                    dayEquator,
                    daylight);

            Color duskSky =
                new Color(
                    0.48f,
                    0.24f,
                    0.12f);

            Color duskEquator =
                new Color(
                    0.34f,
                    0.16f,
                    0.09f);

            RenderSettings.ambientSkyColor =
                Color.Lerp(
                    ambientSky,
                    duskSky,
                    twilight * 0.30f);

            RenderSettings.ambientEquatorColor =
                Color.Lerp(
                    ambientEquator,
                    duskEquator,
                    twilight * 0.36f);

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

            Color fogColor =
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

            RenderSettings.fogColor =
                Color.Lerp(
                    fogColor,
                    new Color(
                        0.40f,
                        0.20f,
                        0.12f),
                    twilight * 0.24f);

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
                0.62f;

            UpdateSkyboxTransition(
                twilight,
                useNightSkybox);

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

        private void UpdateSkyboxTransition(
            float twilight,
            bool useNightSkybox)
        {
            if (runtimeDaySkybox != null)
            {
                if (runtimeDaySkybox.HasProperty(
                        "_Exposure"))
                {
                    runtimeDaySkybox.SetFloat(
                        "_Exposure",
                        Mathf.Lerp(
                            1f,
                            0.20f,
                            Mathf.SmoothStep(
                                0f,
                                1f,
                                Mathf.InverseLerp(
                                    0.22f,
                                    0.62f,
                                    NightAmount))));
                }

                if (runtimeDaySkybox.HasProperty(
                        "_Tint"))
                {
                    runtimeDaySkybox.SetColor(
                        "_Tint",
                        Color.Lerp(
                            Color.white,
                            new Color(
                                1f,
                                0.58f,
                                0.34f,
                                1f),
                            twilight * 0.30f));
                }
            }

            if (runtimeNightSkybox != null &&
                runtimeNightSkybox.HasProperty(
                    "_Exposure"))
            {
                runtimeNightSkybox.SetFloat(
                    "_Exposure",
                    Mathf.Lerp(
                        0.24f,
                        1f,
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                0.48f,
                                0.90f,
                                NightAmount))));
            }
        }

        private void RefreshStreetLights()
        {
            streetLights.Clear();
            autoCreatedStreetLights = 0;

            GameObject cityRoot =
                GameObject.Find(
                    "MotorCity_FCGCity") ??
                GameObject.Find(
                    "City-Maker");

            if (cityRoot == null)
                return;

            Transform[] transforms =
                cityRoot.GetComponentsInChildren<Transform>(true);

            var usedAnchors =
                new HashSet<int>();

            foreach (Transform item in
                     transforms)
            {
                if (item == null ||
                    !IsNamedFcgLampNode(
                        item) ||
                    HasNamedLampDescendant(
                        item))
                {
                    continue;
                }

                if (!usedAnchors.Add(
                        item.GetInstanceID()))
                {
                    continue;
                }

                Light light =
                    CreateRuntimeLampLight(
                        item.position);

                if (light != null)
                {
                    streetLights.Add(
                        light);
                }
            }

            foreach (Transform item in
                     transforms)
            {
                if (item == null ||
                    !IsLampRoot(item) ||
                    HasLampRootAncestor(item) ||
                    HasNamedLampDescendant(item))
                {
                    continue;
                }

                if (!usedAnchors.Add(
                        item.GetInstanceID()))
                {
                    continue;
                }

                Light light =
                    CreateRuntimeLampLight(
                        ResolveLampWorldPosition(
                            item));

                if (light != null)
                {
                    streetLights.Add(
                        light);
                }
            }

            ResolveLampObserver();
            ApplyStreetLights();
        }

        private void ApplyStreetLights()
        {
            Vector3 observerPosition =
                lampObserver != null
                    ? lampObserver.position
                    : Vector3.zero;

            float maximumDistanceSquared =
                LampEnableDistance *
                LampEnableDistance;

            int enabledCount = 0;

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

                bool nearObserver =
                    lampObserver == null ||
                    (light.transform.position -
                     observerPosition).sqrMagnitude <=
                    maximumDistanceSquared;

                bool shouldEnable =
                    NightAmount >= 0.38f &&
                    nearObserver;

                light.enabled =
                    shouldEnable;

                if (shouldEnable)
                    enabledCount++;
            }

            EnabledStreetLightCount =
                enabledCount;
        }

        private void ResolveLampObserver()
        {
            if (lampObserver != null)
                return;

            observerResolveTimer -=
                Time.deltaTime;

            if (observerResolveTimer >
                0f)
                return;

            observerResolveTimer =
                1f;

            MotorCity.Vehicle.ArcadeCarController car =
                UnityEngine.Object.FindAnyObjectByType<MotorCity.Vehicle.ArcadeCarController>();

            if (car != null)
            {
                lampObserver =
                    car.transform;

                return;
            }

            Camera mainCamera =
                Camera.main;

            if (mainCamera != null)
            {
                lampObserver =
                    mainCamera.transform;
            }
        }

        private Light CreateRuntimeLampLight(
            Vector3 worldPosition)
        {
            GameObject lightObject =
                new("MotorCity_LampLight");

            lightObject.transform.SetParent(
                transform,
                false);

            lightObject.transform.position =
                worldPosition;

            Light runtimeLight =
                lightObject.AddComponent<Light>();

            ConfigureLampLight(
                runtimeLight);

            autoCreatedStreetLights++;

            return runtimeLight;
        }

        private static Vector3 ResolveLampWorldPosition(
            Transform lampRoot)
        {
            if (lampRoot == null)
                return Vector3.zero;

            Renderer[] renderers =
                lampRoot.GetComponentsInChildren<Renderer>(true);

            if (renderers == null ||
                renderers.Length == 0)
            {
                return lampRoot.position;
            }

            bool hasBounds = false;
            Bounds bounds =
                default;

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds =
                        renderer.bounds;

                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(
                        renderer.bounds);
                }
            }

            if (!hasBounds)
                return lampRoot.position;

            return
                new Vector3(
                    bounds.center.x,
                    bounds.max.y - 0.08f,
                    bounds.center.z);
        }

        private static void ConfigureLampLight(
            Light light)
        {
            if (light == null)
                return;

            light.type =
                LightType.Point;

            light.lightmapBakeType =
                LightmapBakeType.Realtime;

            light.shadows =
                LightShadows.None;

            light.cullingMask =
                ~0;

            light.color =
                new Color(
                    1f,
                    0.72f,
                    0.42f);

            light.intensity =
                Mathf.Max(
                    light.intensity,
                    66f);

            light.range =
                Mathf.Max(
                    light.range,
                    32f);

            light.bounceIntensity =
                0f;

            // Point lights avoid relying on FCG source rotations after bake.
            // The runtime anchor/fallback is placed at the luminaire itself,
            // so the road and nearby sidewalk receive a visible pool of light.
            light.enabled =
                false;
        }

        private static void ForceAdditionalLightsSupport()
        {
            RenderPipelineAsset pipeline =
                GraphicsSettings.currentRenderPipeline;

            if (pipeline == null)
                return;

            Type type =
                pipeline.GetType();

            TrySetEnumMember(
                pipeline,
                type,
                "additionalLightsRenderingMode",
                "m_AdditionalLightsRenderingMode",
                "PerPixel");

            TrySetIntegerMember(
                pipeline,
                type,
                "maxAdditionalLightsCount",
                "m_AdditionalLightsPerObjectLimit",
                6);
        }

        private static void TrySetEnumMember(
            object target,
            Type type,
            string propertyName,
            string fieldName,
            string enumValue)
        {
            try
            {
                PropertyInfo property =
                    type.GetProperty(
                        propertyName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                if (property != null &&
                    property.CanWrite &&
                    property.PropertyType.IsEnum)
                {
                    object value =
                        Enum.Parse(
                            property.PropertyType,
                            enumValue,
                            true);

                    property.SetValue(
                        target,
                        value);

                    return;
                }

                FieldInfo field =
                    type.GetField(
                        fieldName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                if (field != null &&
                    field.FieldType.IsEnum)
                {
                    object value =
                        Enum.Parse(
                            field.FieldType,
                            enumValue,
                            true);

                    field.SetValue(
                        target,
                        value);
                }
            }
            catch
            {
                // Keep the cycle functional on URP versions whose internals
                // use different member names.
            }
        }

        private static void TrySetIntegerMember(
            object target,
            Type type,
            string propertyName,
            string fieldName,
            int value)
        {
            try
            {
                PropertyInfo property =
                    type.GetProperty(
                        propertyName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                if (property != null &&
                    property.CanWrite &&
                    property.PropertyType ==
                    typeof(int))
                {
                    property.SetValue(
                        target,
                        value);

                    return;
                }

                FieldInfo field =
                    type.GetField(
                        fieldName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                if (field != null &&
                    field.FieldType ==
                    typeof(int))
                {
                    field.SetValue(
                        target,
                        value);
                }
            }
            catch
            {
                // Optional optimization only.
            }
        }

        private static bool HasNamedLampDescendant(
            Transform item)
        {
            if (item == null)
                return false;

            foreach (Transform child in
                     item.GetComponentsInChildren<Transform>(true))
            {
                if (child == null ||
                    child == item)
                    continue;

                if (IsNamedFcgLampNode(
                        child))
                    return true;
            }

            return false;
        }

        private static bool HasLampRootAncestor(
            Transform item)
        {
            if (item == null)
                return false;

            Transform current =
                item.parent;

            while (current != null)
            {
                if (IsLampRoot(
                        current))
                    return true;

                if (IsRuntimeCityRoot(
                        current))
                    return false;

                current =
                    current.parent;
            }

            return false;
        }

        private static bool IsNamedFcgLampNode(
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
