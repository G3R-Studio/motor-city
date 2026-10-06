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
            0.10f;

        private const float LampUpdateInterval =
            0.25f;

        private const float LampEnableDistance =
            170f;

        private const float LampGridCellSize =
            64f;

        private const float SunriseTime01 = 0.18f;
        private const float SunsetTime01 = 0.82f;

        public const float MorningTime01 = 0.28f;
        public const float DayTime01 = 0.50f;
        public const float EveningTime01 = 0.72f;
        public const float NightTime01 = 0.00f;

        [SerializeField] private float fullCycleSeconds =
            480f;

        [SerializeField] private float startTime01 =
            0.38f;

        [SerializeField] private float sunYawDegrees =
            -28f;

        private readonly List<LampSource> lampSources =
            new();

        private readonly Dictionary<Vector2Int, List<int>> lampGrid =
            new();

        private readonly HashSet<Light> enabledLampLights =
            new();

        private readonly HashSet<Light> desiredLampLights =
            new();

        private readonly HashSet<Light> shadowedLampLights =
            new();

        private readonly List<Light> shadowToggleBuffer =
            new();

        private readonly List<LampCandidate> lampCandidates =
            new();

        private readonly List<Light> lampToggleBuffer =
            new();

        private DayNightSettings settings;
        private Light directionalLight;
        private Light moonLight;
        private Material runtimeMorningSkybox;
        private Material runtimeDaySkybox;
        private Material runtimeEveningSkybox;
        private Material runtimeNightSkybox;
        private Material runtimeCrossfadeSkybox;
        private Material lastReflectionSkybox;

        private float time01;
        private double cloudTimeSeconds;
        private static readonly int CloudTimeShaderId = Shader.PropertyToID("_MotorCityCloudTime");
        private float environmentUpdateTimer;
        private float lampUpdateTimer;
        private float observerResolveTimer;
        private int streetLightSourceCount;
        private int parkLampSourceCount;
        private Light garageLight;
        private Texture2D streetLampCookie;
        private Texture2D parkLampCookie;
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
        public float TimeSpeed { get; private set; } = 1f;
        public void SetTimeSpeed(float multiplier) => TimeSpeed = Mathf.Clamp(multiplier, 0f, 60f);
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
            // Replace the authored scene sun with the cycle's own light.
            GameObject sunObject = new("MotorCity Dynamic Sun");
            sunObject.transform.SetParent(transform, false);
            directionalLight = sunObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            RenderSettings.sun = directionalLight;
            if (sun != null && sun != directionalLight) Destroy(sun.gameObject);

            settings =
                Resources.Load<DayNightSettings>(
                    SettingsResourcePath);

            CreateMoonLight();

            time01 =
                Mathf.Repeat(
                    startTime01,
                    1f);

            BuildRuntimeSkyboxes();
            cloudTimeSeconds = 0.0;
            Shader.SetGlobalFloat(CloudTimeShaderId, 0f);
            BuildCityPostProcessing();

            MotorCityQualityRuntime.PresetChanged -=
                HandleQualityPresetChanged;

            MotorCityQualityRuntime.PresetChanged +=
                HandleQualityPresetChanged;

            RefreshStreetLights();

            ApplyEnvironment(
                true);

            CityAssetRuntimeInstaller
                .RefreshCityReflectionProbes();

            initialized =
                true;
        }

        private void OnDisable()
        {
            // Shader globals survive long enough in the Editor to leave
            // Scene View/material previews in the last runtime night state.
            // Always return authored/editor previews to their daytime state
            // when the runtime controller is disabled or Play Mode stops.
            Shader.SetGlobalFloat(
                "_MotorCityNightEmission",
                0f);
        }

        private void Update()
        {
            if (!initialized)
                return;

            // Accumulate scaled time so changing speed never jumps cloud positions.
            float worldDeltaTime = Time.deltaTime * TimeSpeed;
            cloudTimeSeconds += worldDeltaTime;
            Shader.SetGlobalFloat(CloudTimeShaderId, (float)cloudTimeSeconds);

            if (fullCycleSeconds > 0.1f)
            {
                time01 =
                    Mathf.Repeat(
                        time01 +
                        worldDeltaTime /
                        fullCycleSeconds,
                        1f);
            }

            // Move the celestial lights every frame. Environment/post FX can
            // remain throttled, but shadow direction must not advance in 0.1 s
            // steps or the sun visibly "ticks" across the sky.
            ApplyCelestialRotation();
            ApplySkyboxTransition(
                false);

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
            Shader.SetGlobalFloat(
                "_MotorCityNightEmission",
                0f);

            MotorCityQualityRuntime.PresetChanged -=
                HandleQualityPresetChanged;

            DisableLampShadows();
            if (streetLampCookie != null) Destroy(streetLampCookie);
            if (parkLampCookie != null) Destroy(parkLampCookie);

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

            if (runtimeCrossfadeSkybox != null)
                Destroy(
                    runtimeCrossfadeSkybox);

            if (cityPostFxProfile != null)
                Destroy(cityPostFxProfile);
        }

        private void BuildCityPostProcessing()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Keep the browser path fill-rate friendly. The scene still uses
            // its authored sky, fog and lighting without full-screen post FX.
            return;
#endif

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
            cityBloom.intensity.Override(0.05f);
            cityBloom.scatter.Override(0.34f);
            cityBloom.clamp.Override(6f);
            cityBloom.highQualityFiltering.Override(false);

            cityColor = cityPostFxProfile.Add<ColorAdjustments>(true);
            cityColor.postExposure.Override(0f);
            cityColor.contrast.Override(8f);
            cityColor.saturation.Override(2f);

            WhiteBalance whiteBalance = cityPostFxProfile.Add<WhiteBalance>(true);
            whiteBalance.temperature.Override(2f);
            whiteBalance.tint.Override(0f);

            citySplitToning = cityPostFxProfile.Add<SplitToning>(true);
            citySplitToning.shadows.Override(new Color(0.50f, 0.49f, 0.47f, 1f));
            citySplitToning.highlights.Override(new Color(0.57f, 0.53f, 0.47f, 1f));
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

            Shader crossfadeShader =
                Resources.Load<Shader>(
                    "MotorCity/Shaders/MotorCitySkyboxCrossfade");

            if (crossfadeShader != null)
            {
                runtimeCrossfadeSkybox =
                    new Material(
                        crossfadeShader)
                    {
                        name =
                            "MotorCity_RuntimeSkyboxCrossfade"
                    };
            }
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

        private void ApplyCelestialRotation()
        {
            if (directionalLight == null)
                return;

            float normalizedTime =
                Mathf.Repeat(
                    time01,
                    1f);

            float solarAngle;

            if (normalizedTime >= SunriseTime01 &&
                normalizedTime <= SunsetTime01)
            {
                float daylightPhase =
                    Mathf.InverseLerp(
                        SunriseTime01,
                        SunsetTime01,
                        normalizedTime);

                solarAngle =
                    daylightPhase * 180f;
            }
            else
            {
                float nightPhase =
                    normalizedTime > SunsetTime01
                        ? Mathf.InverseLerp(
                            SunsetTime01,
                            1f + SunriseTime01,
                            normalizedTime)
                        : Mathf.InverseLerp(
                            SunsetTime01,
                            1f + SunriseTime01,
                            normalizedTime + 1f);

                solarAngle =
                    180f +
                    nightPhase * 180f;
            }

            directionalLight.transform.rotation =
                Quaternion.Euler(
                    solarAngle,
                    sunYawDegrees,
                    0f);

            if (moonLight != null)
            {
                moonLight.transform.rotation =
                    Quaternion.Euler(
                        solarAngle + 180f,
                        sunYawDegrees,
                        0f);
            }
            Vector3 sunDirection = -directionalLight.transform.forward;
            Vector3 moonDirection = moonLight != null ? -moonLight.transform.forward : -sunDirection;
            Shader.SetGlobalVector("_MotorCitySunDirection", sunDirection);
            Shader.SetGlobalVector("_MotorCityMoonDirection", moonDirection);
            float sunset = 1f - Mathf.Clamp01(Mathf.Abs(sunDirection.y) / .35f);
            Shader.SetGlobalColor("_MotorCitySunDiscColor",
                Color.Lerp(new Color(1f,.95f,.78f), new Color(1f,.32f,.08f), sunset) *
                (sunDirection.y > -.025f ? 4f : 0f));
            Shader.SetGlobalColor("_MotorCityMoonDiscColor", new Color(.62f,.72f,.92f) *
                Mathf.SmoothStep(1f,0f,Mathf.InverseLerp(-.05f,.15f,sunDirection.y)));
        }

        private void ApplyEnvironment(
            bool force)
        {
            if (directionalLight == null)
                return;

            ApplyCelestialRotation();

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

            FcgRuntimeGlassMaterialFactory.UpdateWindowEmission(NightAmount);

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
            {
                cityBloom.intensity.value =
                    0.05f +
                    twilight * 0.035f +
                    NightAmount * 0.12f;
            }

            if (cityColor != null)
            {
                cityColor.postExposure.value = 0f;
                cityColor.contrast.value = Mathf.Lerp(8f, 10f, NightAmount);
                cityColor.saturation.value = Mathf.Lerp(2f, 0f, NightAmount);
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
            // Material roughness now controls whether a surface reads as
            // dry asphalt or reflective glass. Do not globally suppress
            // reflections to fix the road: that also flattens every building
            // window at sunrise/sunset.
            float baseReflectionIntensity =
                Mathf.Lerp(
                    0.18f,
                    0.95f,
                    daylight);

            RenderSettings.reflectionIntensity =
                Mathf.Clamp(
                    baseReflectionIntensity *
                    Mathf.Lerp(
                        1f,
                        0.82f,
                        twilight),
                    0.16f,
                    0.95f);

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
                daylight <= 0.12f
                    ? LightShadows.None
                    : MotorCityQualityRuntime.CurrentPreset switch
                    {
                        MotorCityQualityPreset.Low =>
                            LightShadows.None,

                        MotorCityQualityPreset.High =>
                            LightShadows.Soft,

                        _ =>
                            LightShadows.Hard
                    };

            if (moonLight != null)
            {
                moonLight.color =
                    moonColor;

                moonLight.intensity =
                    moonIntensity *
                    NightAmount;

                moonLight.enabled =
                    NightAmount >
                    0.04f;
            }

            ApplySkyboxTransition(
                force);

            if (force ||
                IsNight !=
                lastNightState)
            {
                bool nightChanged =
                    IsNight !=
                    lastNightState;

                lastNightState =
                    IsNight;

                ApplyStreetLights();

                if (nightChanged &&
                    initialized)
                {
                    CityAssetRuntimeInstaller
                        .RefreshCityReflectionProbes();
                }
            }
        }

        private void ApplySkyboxTransition(
            bool force)
        {
            // The procedural sky owns both celestial bodies at every time.
            // Never switch back to a panorama with an embedded static sun.
            if (runtimeCrossfadeSkybox != null)
            {
                if (RenderSettings.skybox != runtimeCrossfadeSkybox)
                    RenderSettings.skybox = runtimeCrossfadeSkybox;
                return;
            }
            float t =
                Mathf.Repeat(
                    time01,
                    1f);

            Material targetSkybox =
                ResolveSkyboxForTime(
                    t);

            if (targetSkybox != null &&
                (force ||
                 RenderSettings.skybox !=
                 targetSkybox))
            {
                RenderSettings.skybox =
                    targetSkybox;

                if (force)
                {
                    DynamicGI.UpdateEnvironment();
                }

                if (targetSkybox !=
                    runtimeCrossfadeSkybox &&
                    targetSkybox !=
                    lastReflectionSkybox)
                {
                    lastReflectionSkybox =
                        targetSkybox;

                    if (initialized)
                    {
                        CityAssetRuntimeInstaller
                            .RefreshCityReflectionProbes();
                    }
                }
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
            lampGrid.Clear();
            enabledLampLights.Clear();
            desiredLampLights.Clear();
            lampCandidates.Clear();
            lampToggleBuffer.Clear();
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

                ConfigureStreetLamp(sourceLight, isParkLamp);
                sourceLight.enabled =
                    false;

                int sourceIndex =
                    lampSources.Count;

                Vector3 sourcePosition =
                    sourceLight.transform.position;

                lampSources.Add(
                    new LampSource
                    {
                        Light =
                            sourceLight,
                        Position =
                            sourcePosition,
                        BaseIntensity = sourceLight.intensity,
                        IsParkLamp =
                            isParkLamp
                    });

                Vector2Int cell =
                    LampCell(
                        sourcePosition);

                if (!lampGrid.TryGetValue(
                        cell,
                        out List<int> cellSources))
                {
                    cellSources =
                        new List<int>();

                    lampGrid.Add(
                        cell,
                        cellSources);
                }

                cellSources.Add(
                    sourceIndex);

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
                NightAmount >= 0.12f;

            if (garageLight != null &&
                garageLight.enabled !=
                    night)
            {
                garageLight.enabled =
                    night;
            }

            if (lampSources.Count == 0)
            {
                DisableEnabledLampLights();
                EnabledStreetLightCount = 0;
                return;
            }

            bool nightActive =
                night &&
                lampObserver != null;

            if (!nightActive)
            {
                DisableEnabledLampLights();
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

            int lightBudget =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low =>
                        48,

                    MotorCityQualityPreset.High =>
                        112,

                    _ =>
                        80
                };

            int shadowBudget =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low =>
                        0,

                    MotorCityQualityPreset.High =>
                        4,

                    _ =>
                        2
                };

#if UNITY_WEBGL && !UNITY_EDITOR
            lampDistance =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low => 60f,
                    MotorCityQualityPreset.High => 95f,
                    _ => 75f
                };

            lightBudget =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low => 12,
                    MotorCityQualityPreset.High => 32,
                    _ => 20
                };

            shadowBudget = 0;
#endif

            float maximumDistanceSquared =
                lampDistance *
                lampDistance;

            int cellRadius =
                Mathf.CeilToInt(
                    lampDistance /
                    LampGridCellSize);

            Vector2Int observerCell =
                LampCell(
                    observerPosition);

            lampCandidates.Clear();

            for (int x = -cellRadius;
                 x <= cellRadius;
                 x++)
            {
                for (int z = -cellRadius;
                     z <= cellRadius;
                     z++)
                {
                    Vector2Int cell =
                        new(
                            observerCell.x + x,
                            observerCell.y + z);

                    if (!lampGrid.TryGetValue(
                            cell,
                            out List<int> indices))
                    {
                        continue;
                    }

                    for (int i = 0;
                         i < indices.Count;
                         i++)
                    {
                        int index =
                            indices[i];

                        if (index < 0 ||
                            index >=
                                lampSources.Count)
                        {
                            continue;
                        }

                        LampSource source =
                            lampSources[index];

                        if (source.Light == null)
                            continue;

                        float distanceSquared =
                            (source.Position -
                             observerPosition).sqrMagnitude;

                        if (distanceSquared >
                            maximumDistanceSquared)
                        {
                            continue;
                        }

                        lampCandidates.Add(
                            new LampCandidate
                            {
                                SourceIndex =
                                    index,
                                DistanceSquared =
                                    distanceSquared
                            });
                    }
                }
            }

            lampCandidates.Sort(
                static (a, b) =>
                    a.DistanceSquared.CompareTo(
                        b.DistanceSquared));

            desiredLampLights.Clear();

            int count =
                Mathf.Min(
                    lightBudget,
                    lampCandidates.Count);

            float duskFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.12f, .62f, NightAmount));
            for (int i = 0;
                 i < count;
                 i++)
            {
                Light light =
                    lampSources[
                        lampCandidates[i].SourceIndex]
                    .Light;

                if (light != null)
                {
                    LampSource source = lampSources[lampCandidates[i].SourceIndex];
                    float distance = Mathf.Sqrt(lampCandidates[i].DistanceSquared);
                    float distanceFade = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(lampDistance, lampDistance * .78f, distance));
                    light.intensity = source.BaseIntensity * duskFade * distanceFade;
                    desiredLampLights.Add(light);
                }
            }

            ApplyLampShadows(
                shadowBudget);

            lampToggleBuffer.Clear();

            foreach (Light light in
                     enabledLampLights)
            {
                if (light == null ||
                    !desiredLampLights.Contains(
                        light))
                {
                    lampToggleBuffer.Add(
                        light);
                }
            }

            for (int i = 0;
                 i < lampToggleBuffer.Count;
                 i++)
            {
                Light light =
                    lampToggleBuffer[i];

                if (light != null &&
                    light.enabled)
                {
                    light.enabled =
                        false;
                }

                enabledLampLights.Remove(
                    light);
            }

            foreach (Light light in
                     desiredLampLights)
            {
                if (light == null)
                    continue;

                if (!light.enabled)
                {
                    light.enabled =
                        true;
                }

                enabledLampLights.Add(
                    light);
            }

            EnabledStreetLightCount =
                enabledLampLights.Count;
        }

        private void DisableEnabledLampLights()
        {
            DisableLampShadows();

            if (enabledLampLights.Count == 0)
                return;

            lampToggleBuffer.Clear();

            foreach (Light light in
                     enabledLampLights)
            {
                lampToggleBuffer.Add(
                    light);
            }

            for (int i = 0;
                 i < lampToggleBuffer.Count;
                 i++)
            {
                Light light =
                    lampToggleBuffer[i];

                if (light != null &&
                    light.enabled)
                {
                    light.enabled =
                        false;
                }
            }

            enabledLampLights.Clear();
            desiredLampLights.Clear();
        }

        private void HandleQualityPresetChanged()
        {
            if (!initialized)
                return;

            ResolveLampObserver(
                true);

            ApplyStreetLights();

            CityAssetRuntimeInstaller
                .RefreshCityReflectionProbes();
        }

        private void ApplyLampShadows(
            int shadowBudget)
        {
            shadowToggleBuffer.Clear();

            foreach (Light light in
                     shadowedLampLights)
            {
                shadowToggleBuffer.Add(
                    light);
            }

            for (int i = 0;
                 i < shadowToggleBuffer.Count;
                 i++)
            {
                Light light =
                    shadowToggleBuffer[i];

                if (light != null &&
                    light.shadows !=
                        LightShadows.None)
                {
                    light.shadows =
                        LightShadows.None;
                }
            }

            shadowedLampLights.Clear();

            if (shadowBudget <= 0)
                return;

            int count =
                Mathf.Min(
                    shadowBudget,
                    lampCandidates.Count);

            LightShadows shadowMode =
                MotorCityQualityRuntime.CurrentPreset ==
                MotorCityQualityPreset.High
                    ? LightShadows.Soft
                    : LightShadows.Hard;

            for (int i = 0;
                 i < count;
                 i++)
            {
                Light light =
                    lampSources[
                        lampCandidates[i].SourceIndex]
                    .Light;

                if (light == null ||
                    !desiredLampLights.Contains(
                        light))
                {
                    continue;
                }

                light.shadows =
                    shadowMode;

                light.shadowStrength =
                    MotorCityQualityRuntime.CurrentPreset ==
                    MotorCityQualityPreset.High
                        ? 0.58f
                        : 0.42f;

                shadowedLampLights.Add(
                    light);
            }
        }

        private void DisableLampShadows()
        {
            if (shadowedLampLights.Count == 0)
                return;

            shadowToggleBuffer.Clear();

            foreach (Light light in
                     shadowedLampLights)
            {
                shadowToggleBuffer.Add(
                    light);
            }

            for (int i = 0;
                 i < shadowToggleBuffer.Count;
                 i++)
            {
                Light light =
                    shadowToggleBuffer[i];

                if (light != null)
                {
                    light.shadows =
                        LightShadows.None;
                }
            }

            shadowedLampLights.Clear();
        }

        private static Vector2Int LampCell(
            Vector3 position)
        {
            return
                new Vector2Int(
                    Mathf.FloorToInt(
                        position.x /
                        LampGridCellSize),
                    Mathf.FloorToInt(
                        position.z /
                        LampGridCellSize));
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

        private void ConfigureStreetLamp(Light light, bool park)
        {
            Transform fixture = light.transform.parent;
            for (Transform current = fixture; current != null; current = current.parent)
            {
                string name = NormalizeName(current.name);
                if (name.StartsWith("streetlight", StringComparison.Ordinal) ||
                    name.StartsWith("parklamp", StringComparison.Ordinal) ||
                    name.StartsWith("parklight", StringComparison.Ordinal))
                { fixture = current; break; }
            }
            float height = Mathf.Clamp(light.transform.position.y -
                (fixture != null ? fixture.position.y : light.transform.position.y - 8f), 3f, 18f);
            Vector3 heading = Vector3.ProjectOnPlane(light.transform.forward, Vector3.up);
            if (heading.sqrMagnitude < .01f && fixture != null)
                heading = Vector3.ProjectOnPlane(fixture.forward, Vector3.up);
            if (heading.sqrMagnitude < .01f) heading = Vector3.forward;
            heading.Normalize();
            // Small tilt toward the roadway, with the long axis across the arm.
            light.transform.rotation = Quaternion.LookRotation(
                (Vector3.down + heading * (park ? .04f : .18f)).normalized, heading);
            light.type = LightType.Spot;
            light.renderMode = LightRenderMode.ForcePixel;
            light.color = park ? new Color(1f, .84f, .66f) : new Color(1f, .91f, .79f);
            light.range = Mathf.Clamp(height * (park ? 3.0f : 3.5f), park ? 12f : 24f, park ? 24f : 48f);
            light.spotAngle = park ? 105f : 118f;
            light.innerSpotAngle = park ? 62f : 78f;
            light.intensity = (park ? 7f : 18f) * Mathf.Clamp(height * height / (park ? 25f : 81f), .65f, 2f);
            light.cullingMask = ~0;
            light.shadows = LightShadows.None;
            light.shadowBias = .035f;
            light.shadowNormalBias = .2f;
            light.shadowNearPlane = .15f;
            if (park && parkLampCookie == null) parkLampCookie = CreateLampCookie(true);
            if (!park && streetLampCookie == null) streetLampCookie = CreateLampCookie(false);
            light.cookie = park ? parkLampCookie : streetLampCookie;
        }

        private static Texture2D CreateLampCookie(bool park)
        {
            const int size = 128;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false, true);
            texture.name = park ? "MotorCity Park Light Distribution" : "MotorCity Street Light Distribution";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.DontSave;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + .5f) / size * 2f - 1f;
                    float v = ((y + .5f) / size * 2f - 1f) / (park ? .95f : .66f);
                    float radius = Mathf.Sqrt(u * u + v * v);
                    float transmission = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.28f, .98f, radius));
                    pixels[y * size + x] = new Color(transmission, transmission, transmission, 1f);
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
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
            public float BaseIntensity;
        }

        private struct LampCandidate
        {
            public int SourceIndex;
            public float DistanceSquared;
        }


    }
}
