using System;
using System.Collections.Generic;
using MotorCity.Platform;
using UnityEngine;
using UnityEngine.Rendering;

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
            110f;


        [SerializeField] private float fullCycleSeconds =
            480f;

        [SerializeField] private float startTime01 =
            0.38f;

        [SerializeField] private float sunYawDegrees =
            -28f;

        private readonly List<LampSource> lampSources =
            new();

        private readonly List<LampCandidate> lampCandidates =
            new();

        private readonly List<Renderer> streetLampGlowRenderers =
            new();

        private DayNightSettings settings;
        private Light directionalLight;
        private Light moonLight;
        private Material runtimeDaySkybox;
        private Material runtimeNightSkybox;

        private float time01;
        private float environmentUpdateTimer;
        private float lampUpdateTimer;
        private float observerResolveTimer;
        private int streetLightSourceCount;
        private int parkLampSourceCount;
        private Transform lampObserver;
        private bool lastNightState;
        private bool initialized;

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
            lampSources.Clear();
            lampCandidates.Clear();
            streetLampGlowRenderers.Clear();
            streetLightSourceCount = 0;
            parkLampSourceCount = 0;

            GameObject cityRoot =
                GameObject.Find(
                    "MotorCity_FCGCity") ??
                GameObject.Find(
                    "City-Maker");

            if (cityRoot == null)
                return;

            // Keep the FCG visual lamp helpers authored in the scene. They are
            // toggled together with their own nearby Light component.
            foreach (Renderer renderer in
                     cityRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null ||
                    NormalizeName(
                        renderer.gameObject.name) !=
                    "lightv")
                {
                    continue;
                }

                renderer.enabled =
                    false;

                streetLampGlowRenderers.Add(
                    renderer);
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
                    LightType.Directional ||
                    !IsFcgStreetLampLight(
                        sourceLight) ||
                    !usedLights.Add(
                        sourceLight.GetEntityId()))
                {
                    continue;
                }

                bool isParkLamp =
                    IsParkLamp(
                        sourceLight.transform);

                ConfigureAuthoredLampLight(
                    sourceLight,
                    isParkLamp);

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
                    "[MotorCity][Lighting] Expected 489 StreetLight and 288 ParkLamp sources, found " +
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
            if (lampSources.Count == 0)
            {
                EnabledStreetLightCount = 0;
                return;
            }

            bool nightActive =
                NightAmount >= 0.38f &&
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

                foreach (Renderer renderer in
                         streetLampGlowRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled =
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
                        82f,

                    MotorCityQualityPreset.High =>
                        LampEnableDistance,

                    _ =>
                        98f
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

            // StreetLight's authored _LightV mesh is the visible luminous
            // plafond. Enable it only when its nearby authored Spot Light is
            // also close enough to the player.
            foreach (Renderer renderer in
                     streetLampGlowRenderers)
            {
                if (renderer == null)
                    continue;

                float distanceSquared =
                    (renderer.transform.position -
                     observerPosition).sqrMagnitude;

                renderer.enabled =
                    distanceSquared <=
                    maximumDistanceSquared;
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

        private static void ConfigureAuthoredLampLight(
            Light light,
            bool isParkLamp)
        {
            if (light == null)
                return;

            light.type =
                LightType.Spot;

            light.shadows =
                LightShadows.None;

            light.cullingMask =
                ~0;

            light.bounceIntensity =
                1f;

            light.useColorTemperature =
                false;

            if (isParkLamp)
            {
                // ParkLamp / _Spot_Light
                light.intensity =
                    12f;
            }
            else
            {
                // StreetLight / Spot Light
                light.innerSpotAngle =
                    90f;

                light.spotAngle =
                    179f;

                light.intensity =
                    50f;

                light.range =
                    20f;
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
