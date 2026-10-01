using System;
using System.Collections.Generic;
using MotorCity.Platform;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MotorCity.World
{
    public sealed class DayNightCycleController : MonoBehaviour
    {
        private const string SettingsResourcePath =
            "MotorCity/Environment/DayNightSettings";

        private const float EnvironmentUpdateInterval =
            0.05f;

        private const float LampUpdateInterval =
            0.25f;

        private const float LampEnableDistance =
            170f;

        public const float MorningTime01 = 0.32f;
        public const float DayTime01 = 0.50f;
        public const float EveningTime01 = 0.68f;
        public const float NightTime01 = 0.00f;

        [SerializeField] private float fullCycleSeconds =
            480f;

        [SerializeField] private float startTime01 =
            0.38f;

        [SerializeField] private float sunYawDegrees =
            -28f;

        private readonly List<LampSource> lampSources =
            new();

        private DayNightSettings settings;
        private Light directionalLight;
        private Light moonLight;
        private Material runtimeMorningSkybox;
        private Material runtimeDaySkybox;
        private Material runtimeEveningSkybox;
        private Material runtimeNightSkybox;

        private float time01;
        private float environmentUpdateTimer;
        private float lampUpdateTimer;
        private float observerResolveTimer;
        private int streetLightSourceCount;
        private int parkLampSourceCount;
        private Light garageLight;
        private Transform lampObserver;
        private bool lastNightState;
        private bool initialized;
        private Volume cityPostFxVolume;
        private VolumeProfile cityPostFxProfile;
        private Bloom cityBloom;
        private ColorAdjustments cityColor;
        private Vignette cityVignette;
        private Tonemapping cityTonemapping;
        private SplitToning citySplitToning;

        public bool IsNight { get; private set; }
        public float NightAmount { get; private set; }
        public float TimeOfDay01 => time01;
        public int StreetLightCount => streetLightSourceCount;
        public int ParkLampCount => parkLampSourceCount;
        public int AutoCreatedStreetLightCount => 0;
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

            ResolveLampObserver(true);
            ApplyStreetLights();
        }

        public void SetMorning() => SetTimeOfDay(MorningTime01);

        public void SetDay() => SetTimeOfDay(DayTime01);

        public void SetEvening() => SetTimeOfDay(EveningTime01);

        public void SetNight() => SetTimeOfDay(NightTime01);

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
            BuildCityPostProcessing();
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

            environmentUpdateTimer -=
                Time.deltaTime;

            if (environmentUpdateTimer <= 0f)
            {
                environmentUpdateTimer =
                    EnvironmentUpdateInterval;

                ApplyEnvironment(
                    false);
            }

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
            if (runtimeMorningSkybox != null)
                Destroy(
                    runtimeMorningSkybox);

            if (runtimeDaySkybox != null)
                Destroy(
                    runtimeDaySkybox);

            if (runtimeEveningSkybox != null)
                Destroy(
                    runtimeEveningSkybox);

            if (runtimeNightSkybox != null)
                Destroy(
                    runtimeNightSkybox);

            if (cityPostFxProfile != null)
                Destroy(cityPostFxProfile);
        }

        private void BuildCityPostProcessing()
        {
            GameObject volumeObject = new("Motor City Global Post FX");
            volumeObject.transform.SetParent(transform, false);

            cityPostFxVolume = volumeObject.AddComponent<Volume>();
            cityPostFxVolume.isGlobal = true;
            cityPostFxVolume.priority = -10f;
            cityPostFxVolume.weight = 1f;

            cityPostFxProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            cityPostFxProfile.name = "MotorCity_RuntimePostFX";
            cityPostFxVolume.sharedProfile = cityPostFxProfile;

            cityTonemapping = cityPostFxProfile.Add<Tonemapping>(true);
            cityTonemapping.mode.Override(TonemappingMode.ACES);

            cityBloom = cityPostFxProfile.Add<Bloom>(true);
            cityBloom.threshold.Override(1.05f);
            cityBloom.intensity.Override(0.12f);
            cityBloom.scatter.Override(0.42f);
            cityBloom.clamp.Override(8f);
            cityBloom.highQualityFiltering.Override(false);

            cityColor = cityPostFxProfile.Add<ColorAdjustments>(true);
            cityColor.postExposure.Override(0f);
            cityColor.contrast.Override(10f);
            cityColor.saturation.Override(3f);

            WhiteBalance whiteBalance = cityPostFxProfile.Add<WhiteBalance>(true);
            whiteBalance.temperature.Override(-2f);
            whiteBalance.tint.Override(0f);

            citySplitToning = cityPostFxProfile.Add<SplitToning>(true);
            citySplitToning.shadows.Override(new Color(0.43f, 0.48f, 0.56f, 1f));
            citySplitToning.highlights.Override(new Color(0.57f, 0.52f, 0.45f, 1f));
            citySplitToning.balance.Override(0f);

            cityVignette = cityPostFxProfile.Add<Vignette>(true);
            cityVignette.intensity.Override(0.055f);
            cityVignette.smoothness.Override(0.30f);
            cityVignette.rounded.Override(false);
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

            runtimeMorningSkybox =
                CloneSkybox(
                    settings.MorningSkybox,
                    "Morning");

            runtimeDaySkybox =
                CloneSkybox(
                    settings.DaySkybox,
                    "Day");

            runtimeEveningSkybox =
                CloneSkybox(
                    settings.EveningSkybox,
                    "Evening");

            runtimeNightSkybox =
                CloneSkybox(
                    settings.NightSkybox,
                    "Night");
        }

        private static Material CloneSkybox(
            Material source,
            string suffix)
        {
            if (source == null)
                return null;

            Material clone =
                new(source)
                {
                    name =
                        source.name +
                        "_MotorCity_" +
                        suffix
                };

            if (clone.HasProperty("_Exposure"))
            {
                clone.SetFloat(
                    "_Exposure",
                    1f);
            }

            return clone;
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
                        -0.08f,
                        0.22f,
                        solarHeight));

            NightAmount =
                1f -
                daylight;

            float horizonAmount =
                1f - Mathf.Clamp01(Mathf.Abs(solarHeight) / 0.28f);

            float twilight =
                horizonAmount * horizonAmount;

            float morningAmount =
                Mathf.Clamp01(
                    1f -
                    Mathf.Abs(
                        time01 -
                        MorningTime01) /
                    0.12f);

            float eveningAmount =
                Mathf.Clamp01(
                    1f -
                    Mathf.Abs(
                        time01 -
                        EveningTime01) /
                    0.12f);

            morningAmount *=
                daylight;

            eveningAmount *=
                daylight;

            Shader.SetGlobalFloat(
                "_MotorCityNightEmission",
                NightAmount);

            if (cityBloom != null)
                cityBloom.intensity.value = Mathf.Lerp(0.12f, 0.20f, NightAmount);

            if (cityColor != null)
            {
                cityColor.postExposure.value = 0f;
                cityColor.contrast.value = Mathf.Lerp(10f, 12f, NightAmount);
                cityColor.saturation.value = Mathf.Lerp(3f, 1f, NightAmount);
            }

            if (cityVignette != null)
                cityVignette.intensity.value = Mathf.Lerp(0.055f, 0.065f, NightAmount);

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

            Color authoredNightSky =
                settings != null
                    ? settings.NightSkyColor
                    : new Color(
                        0.585f,
                        0.585f,
                        0.585f);

            Color nightSky =
                new Color(
                    authoredNightSky.r * 0.22f,
                    authoredNightSky.g * 0.28f,
                    authoredNightSky.b * 0.40f,
                    1f);

            Color authoredNightEquator =
                settings != null
                    ? settings.NightEquatorColor
                    : new Color(
                        0.651f,
                        0.651f,
                        0.651f);

            Color nightEquator =
                new Color(
                    authoredNightEquator.r * 0.22f,
                    authoredNightEquator.g * 0.27f,
                    authoredNightEquator.b * 0.35f,
                    1f);

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

            Color morningSky =
                new Color(
                    0.52f,
                    0.31f,
                    0.19f);

            Color morningEquator =
                new Color(
                    0.42f,
                    0.25f,
                    0.16f);

            Color eveningSky =
                new Color(
                    0.46f,
                    0.20f,
                    0.10f);

            Color eveningEquator =
                new Color(
                    0.34f,
                    0.13f,
                    0.07f);

            Color timeSky =
                Color.Lerp(
                    ambientSky,
                    morningSky,
                    morningAmount * 0.34f);

            timeSky =
                Color.Lerp(
                    timeSky,
                    eveningSky,
                    eveningAmount * 0.48f);

            Color timeEquator =
                Color.Lerp(
                    ambientEquator,
                    morningEquator,
                    morningAmount * 0.30f);

            timeEquator =
                Color.Lerp(
                    timeEquator,
                    eveningEquator,
                    eveningAmount * 0.52f);

            RenderSettings.ambientSkyColor =
                timeSky;

            RenderSettings.ambientEquatorColor =
                timeEquator;

            // Match FCG URP DayNight.UpdateColor(): 0.07 at night,
            // 0.40 during day, blended here for Motor City's smooth cycle.
            RenderSettings.ambientGroundColor =
                Color.Lerp(
                    new Color(
                        0.07f,
                        0.07f,
                        0.07f),
                    new Color(
                        0.4f,
                        0.4f,
                        0.4f),
                    daylight);

            // Keep daytime materials intact, but reduce environment reflection
            // energy at night. Without this, URP/Lit surfaces keep broad cold
            // highlights and roads, pavements and the car read like wet plastic.
            // Direct headlights, street lights and emissive windows remain
            // unaffected, which gives the night scene more material separation.
            RenderSettings.reflectionIntensity =
                Mathf.Lerp(
                    0.18f,
                    1f,
                    daylight);

            // CityAtmosphereRuntime was removed. Keep the lightweight
            // distance haze owned here so the active day/night controller
            // cannot leave the scene with stale fog settings.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 520f;

            Color fogDay =
                new Color(0.56f, 0.61f, 0.66f);
            Color fogNight =
                new Color(0.075f, 0.095f, 0.13f);
            Color fogDusk =
                new Color(0.60f, 0.34f, 0.24f);

            Color baseFog =
                Color.Lerp(
                    fogNight,
                    fogDay,
                    daylight);

            baseFog =
                Color.Lerp(
                    baseFog,
                    new Color(
                        0.62f,
                        0.42f,
                        0.30f),
                    morningAmount * 0.12f);

            RenderSettings.fogColor =
                Color.Lerp(
                    baseFog,
                    fogDusk,
                    eveningAmount * 0.52f);

            UpdateSkyboxHaze(
                RenderSettings.fogColor,
                daylight,
                twilight);

            Color daySunColor =
                new Color(
                    1.00f,
                    0.95f,
                    0.86f);

            Color morningSunColor =
                new Color(
                    1.00f,
                    0.72f,
                    0.46f);

            Color eveningSunColor =
                new Color(
                    1.00f,
                    0.50f,
                    0.24f);

            Color sunColor =
                Color.Lerp(
                    daySunColor,
                    morningSunColor,
                    morningAmount * 0.72f);

            sunColor =
                Color.Lerp(
                    sunColor,
                    eveningSunColor,
                    eveningAmount * 0.88f);

            Color moonColor =
                settings != null
                    ? settings.MoonColor
                    : new Color(
                        0.56f,
                        0.62f,
                        0.82f);

            float authoredSunIntensity =
                settings != null
                    ? settings.SunIntensity
                    : 1.05f;

            float daySunIntensity =
                Mathf.Clamp(
                    authoredSunIntensity,
                    1.02f,
                    1.16f);

            float sunIntensity =
                daySunIntensity *
                Mathf.Lerp(
                    1f,
                    0.86f,
                    morningAmount) *
                Mathf.Lerp(
                    1f,
                    0.74f,
                    eveningAmount);

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

            Material targetSkybox =
                ResolveSkyboxForTime(
                    time01);

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

        private void UpdateSkyboxHaze(
            Color fogColor,
            float daylight,
            float twilight)
        {
            float strength =
                Mathf.Lerp(
                    0.34f,
                    0.18f,
                    daylight);

            strength +=
                twilight * 0.10f;

            ApplySkyboxHaze(
                runtimeMorningSkybox,
                fogColor,
                strength);

            ApplySkyboxHaze(
                runtimeDaySkybox,
                fogColor,
                strength);

            ApplySkyboxHaze(
                runtimeEveningSkybox,
                fogColor,
                strength);

            ApplySkyboxHaze(
                runtimeNightSkybox,
                fogColor,
                Mathf.Max(
                    0.20f,
                    strength));
        }

        private static void ApplySkyboxHaze(
            Material material,
            Color color,
            float strength)
        {
            if (material == null)
                return;

            if (material.HasProperty(
                    "_HazeColor"))
            {
                material.SetColor(
                    "_HazeColor",
                    color);
            }

            if (material.HasProperty(
                    "_HazeStrength"))
            {
                material.SetFloat(
                    "_HazeStrength",
                    Mathf.Clamp01(
                        strength));
            }
        }

        private Material ResolveSkyboxForTime(
            float normalizedTime)
        {
            float t =
                Mathf.Repeat(
                    normalizedTime,
                    1f);

            if (t >= 0.24f &&
                t < 0.41f)
            {
                return
                    runtimeMorningSkybox ??
                    runtimeDaySkybox ??
                    runtimeNightSkybox;
            }

            if (t >= 0.41f &&
                t < 0.61f)
            {
                return
                    runtimeDaySkybox ??
                    runtimeMorningSkybox ??
                    runtimeEveningSkybox;
            }

            if (t >= 0.61f &&
                t < 0.82f)
            {
                return
                    runtimeEveningSkybox ??
                    runtimeDaySkybox ??
                    runtimeNightSkybox;
            }

            return
                runtimeNightSkybox ??
                runtimeEveningSkybox ??
                runtimeDaySkybox;
        }

        private void RefreshStreetLights()
        {
            lampSources.Clear();
            streetLightSourceCount = 0;
            parkLampSourceCount = 0;
            garageLight = null;

            GameObject cityRoot =
                GameObject.Find(
                    "MotorCity_FCGCity") ??
                GameObject.Find(
                    "City-Maker");

            if (cityRoot == null)
                return;

            // _LightV is the old FCG volumetric cone mesh. Keep it disabled:
            // only the actual authored Light components should illuminate the city.
            foreach (Renderer renderer in
                     cityRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null &&
                    NormalizeName(
                        renderer.gameObject.name) ==
                    "lightv")
                {
                    renderer.enabled =
                        false;
                }
            }

            Light[] sourceLights =
                cityRoot.GetComponentsInChildren<Light>(true);

            var usedLights =
                new HashSet<EntityId>();

            foreach (Light sourceLight in
                     sourceLights)
            {
                if (sourceLight == null ||
                    sourceLight.type ==
                    LightType.Directional)
                {
                    continue;
                }

                if (NormalizeName(
                        sourceLight.gameObject.name) ==
                    "garagelight")
                {
                    garageLight =
                        sourceLight;

                    garageLight.enabled =
                        false;

                    continue;
                }

                if (!IsFcgStreetLampLight(
                        sourceLight) ||
                    !usedLights.Add(
                        sourceLight.GetEntityId()))
                {
                    continue;
                }

                bool isParkLamp =
                    IsParkLamp(
                        sourceLight.transform);

                // FCG's original spot lights are tuned for its demo scene.
                // In Motor City's darker URP night they need a little more reach
                // and a warmer practical-light color to illuminate the road.
                sourceLight.color =
                    isParkLamp
                        ? new Color(1.00f, 0.84f, 0.66f)
                        : new Color(1.00f, 0.78f, 0.52f);

                sourceLight.intensity =
                    isParkLamp
                        ? 6.2f
                        : 7.8f;

                sourceLight.range =
                    isParkLamp
                        ? 17f
                        : 50f;

                sourceLight.spotAngle =
                    isParkLamp
                        ? 98f
                        : 150f;

                sourceLight.innerSpotAngle =
                    isParkLamp
                        ? 40f
                        : 50f;

                sourceLight.shadows =
                    LightShadows.None;

                sourceLight.enabled =
                    false;

                lampSources.Add(
                    new LampSource
                    {
                        Light =
                            sourceLight,
                        Position =
                            sourceLight.transform.position,
                        IsParkLamp =
                            isParkLamp
                    });

                if (isParkLamp)
                {
                    parkLampSourceCount++;
                }
                else
                {
                    streetLightSourceCount++;
                }
            }

            if (streetLightSourceCount != 489 ||
                parkLampSourceCount != 288)
            {
                Debug.LogWarning(
                    "[MotorCity][Lighting] Expected 489 StreetLight and 288 ParkLamp lights, found " +
                    streetLightSourceCount +
                    " StreetLight and " +
                    parkLampSourceCount +
                    " ParkLamp.");
            }

            ResolveLampObserver();
            ApplyStreetLights();
        }

        private void ApplyStreetLights()
        {
            bool night =
                NightAmount >= 0.38f;

            if (garageLight != null)
            {
                garageLight.enabled =
                    night;
            }

            if (lampSources.Count == 0)
            {
                EnabledStreetLightCount = 0;
                return;
            }

            bool nightActive =
                night &&
                lampObserver != null;

            if (!nightActive)
            {
                foreach (LampSource source in
                         lampSources)
                {
                    if (source.Light != null)
                    {
                        source.Light.enabled =
                            false;
                    }
                }

                EnabledStreetLightCount = 0;
                return;
            }

            Vector3 observerPosition =
                lampObserver.position;

            float lampDistance =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low =>
                        90f,

                    MotorCityQualityPreset.High =>
                        LampEnableDistance,

                    _ =>
                        130f
                };

            float maximumDistanceSquared =
                lampDistance *
                lampDistance;

            int enabledCount = 0;

            foreach (LampSource source in
                     lampSources)
            {
                if (source.Light == null)
                    continue;

                float distanceSquared =
                    (source.Position -
                     observerPosition).sqrMagnitude;

                bool enable =
                    distanceSquared <=
                    maximumDistanceSquared;

                source.Light.enabled =
                    enable;

                if (enable)
                {
                    enabledCount++;
                }
            }

            EnabledStreetLightCount =
                enabledCount;
        }

        private void ResolveLampObserver(bool force = false)
        {
            if (lampObserver != null)
                return;

            if (!force)
            {
                observerResolveTimer -=
                    Time.deltaTime;

                if (observerResolveTimer > 0f)
                    return;
            }

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

        private static bool IsParkLamp(
            Transform transform)
        {
            Transform current =
                transform;

            while (current != null)
            {
                string name =
                    NormalizeName(
                        current.name);

                if (name.StartsWith(
                        "parklamp",
                        StringComparison.Ordinal) ||
                    name.StartsWith(
                        "parklight",
                        StringComparison.Ordinal))
                {
                    return true;
                }

                if (name ==
                        "motorcityfcgcity" ||
                    name ==
                        "citymaker")
                {
                    break;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static bool IsFcgStreetLampLight(
            Light light)
        {
            if (light == null)
                return false;

            string name =
                NormalizeName(
                    light.gameObject.name);

            if (name !=
                "spotlight")
            {
                return false;
            }

            Transform current =
                light.transform.parent;

            while (current != null)
            {
                string parentName =
                    NormalizeName(
                        current.name);

                if (parentName.StartsWith(
                        "streetlight",
                        StringComparison.Ordinal) ||
                    parentName.StartsWith(
                        "parklamp",
                        StringComparison.Ordinal) ||
                    parentName.StartsWith(
                        "parklight",
                        StringComparison.Ordinal) ||
                    parentName ==
                        "lightv")
                {
                    return true;
                }

                if (parentName ==
                        "motorcityfcgcity" ||
                    parentName ==
                        "citymaker")
                {
                    break;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static string NormalizeName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return string.Empty;
            }

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

        private struct LampSource
        {
            public Light Light;
            public Vector3 Position;
            public bool IsParkLamp;
        }


    }
}
