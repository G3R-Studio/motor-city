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

        private const int MaxRuntimeStreetLights =
            16;

        private const int MaxRuntimeLampGlows =
            64;

        [SerializeField] private float fullCycleSeconds =
            480f;

        [SerializeField] private float startTime01 =
            0.38f;

        [SerializeField] private float sunYawDegrees =
            -28f;

        private readonly List<Light> streetLights =
            new();

        private readonly List<Renderer> streetLampGlowRenderers =
            new();

        private readonly List<Renderer> runtimeLampGlowRenderers =
            new();

        private readonly List<LampAnchor> streetLampAnchors =
            new();

        private readonly List<LampCandidate> lampCandidates =
            new();

        private DayNightSettings settings;
        private Light directionalLight;
        private Light moonLight;
        private Material runtimeDaySkybox;
        private Material runtimeNightSkybox;
        private Material runtimeLampGlowMaterial;

        private float time01;
        private float environmentUpdateTimer;
        private float lampUpdateTimer;
        private float observerResolveTimer;
        private int autoCreatedStreetLights;
        private Transform lampObserver;
        private bool lastNightState;
        private bool initialized;

        public bool IsNight { get; private set; }
        public float NightAmount { get; private set; }
        public float TimeOfDay01 => time01;
        public int StreetLightCount => streetLampAnchors.Count;
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

            if (runtimeLampGlowMaterial != null)
                Destroy(
                    runtimeLampGlowMaterial);

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
            foreach (Light light in
                     streetLights)
            {
                if (light != null)
                {
                    Destroy(
                        light.gameObject);
                }
            }

            streetLights.Clear();

            foreach (Renderer renderer in
                     runtimeLampGlowRenderers)
            {
                if (renderer != null)
                {
                    Destroy(
                        renderer.gameObject);
                }
            }

            runtimeLampGlowRenderers.Clear();
            streetLampGlowRenderers.Clear();
            streetLampAnchors.Clear();
            lampCandidates.Clear();
            autoCreatedStreetLights = 0;

            GameObject cityRoot =
                GameObject.Find(
                    "MotorCity_FCGCity") ??
                GameObject.Find(
                    "City-Maker");

            if (cityRoot == null)
                return;

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
                        sourceLight))
                {
                    continue;
                }

                // The authored FCG light is used only as a transform
                // anchor. Motor City owns the active night lighting through
                // the bounded realtime pool below, so leaving the source
                // component enabled can double-light the street and defeat
                // the WebGL light budget.
                sourceLight.enabled = false;

                if (!usedLights.Add(
                        sourceLight.GetEntityId()))
                {
                    continue;
                }

                bool isParkLamp =
                    IsParkLamp(
                        sourceLight.transform);

                Vector3 glowPosition =
                    sourceLight.transform.position;

                if (isParkLamp)
                {
                    glowPosition.y =
                        3.17f;
                }

                streetLampAnchors.Add(
                    new LampAnchor
                    {
                        Position =
                            sourceLight.transform.position,
                        GlowPosition =
                            glowPosition,
                        Rotation =
                            sourceLight.transform.rotation,
                        IsParkLamp =
                            isParkLamp
                    });
            }

            int poolSize =
                Mathf.Min(
                    MaxRuntimeStreetLights,
                    streetLampAnchors.Count);

            for (int i = 0;
                 i < poolSize;
                 i++)
            {
                Light light =
                    CreateRuntimeLampLight(
                        Vector3.zero);

                if (light != null)
                {
                    streetLights.Add(
                        light);
                }
            }

            int glowPoolSize =
                Mathf.Min(
                    MaxRuntimeLampGlows,
                    streetLampAnchors.Count);

            for (int i = 0;
                 i < glowPoolSize;
                 i++)
            {
                Renderer glow =
                    CreateRuntimeLampGlow();

                if (glow != null)
                {
                    runtimeLampGlowRenderers.Add(
                        glow);
                }
            }

            ResolveLampObserver();
            ApplyStreetLights();
        }

        private void ApplyStreetLights()
        {
            // FCG LightV helper meshes are disabled once during refresh.
            if (streetLights.Count == 0 &&
                runtimeLampGlowRenderers.Count == 0)
            {
                EnabledStreetLightCount = 0;
                return;
            }

            bool nightActive =
                NightAmount >= 0.38f &&
                lampObserver != null;

            if (!nightActive)
            {
                foreach (Light light in
                         streetLights)
                {
                    if (light != null)
                    {
                        light.enabled = false;
                    }
                }

                foreach (Renderer glow in
                         runtimeLampGlowRenderers)
                {
                    if (glow != null)
                    {
                        glow.enabled = false;
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

            lampCandidates.Clear();

            foreach (LampAnchor anchor in
                     streetLampAnchors)
            {
                float distanceSquared =
                    (anchor.Position -
                     observerPosition).sqrMagnitude;

                if (distanceSquared >
                    maximumDistanceSquared)
                {
                    continue;
                }

                lampCandidates.Add(
                    new LampCandidate
                    {
                        Position =
                            anchor.Position,
                        GlowPosition =
                            anchor.GlowPosition,
                        Rotation =
                            anchor.Rotation,
                        IsParkLamp =
                            anchor.IsParkLamp,
                        DistanceSquared =
                            distanceSquared
                    });
            }

            lampCandidates.Sort(
                CompareLampCandidates);

            int qualityLightBudget =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low =>
                        6,

                    MotorCityQualityPreset.High =>
                        MaxRuntimeStreetLights,

                    _ =>
                        10
                };

            int enabledCount =
                Mathf.Min(
                    qualityLightBudget,
                    streetLights.Count,
                    lampCandidates.Count);

            for (int i = 0;
                 i < streetLights.Count;
                 i++)
            {
                Light light =
                    streetLights[i];

                if (light == null)
                    continue;

                bool enable =
                    i <
                    enabledCount;

                if (enable)
                {
                    light.transform.SetPositionAndRotation(
                        lampCandidates[i].Position,
                        lampCandidates[i].Rotation);
                }

                light.enabled =
                    enable;
            }

            EnabledStreetLightCount =
                enabledCount;

            int glowBudget =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low =>
                        20,

                    MotorCityQualityPreset.High =>
                        MaxRuntimeLampGlows,

                    _ =>
                        40
                };

            int glowCount =
                Mathf.Min(
                    glowBudget,
                    runtimeLampGlowRenderers.Count,
                    lampCandidates.Count);

            for (int i = 0;
                 i < runtimeLampGlowRenderers.Count;
                 i++)
            {
                Renderer glow =
                    runtimeLampGlowRenderers[i];

                if (glow == null)
                    continue;

                bool enable =
                    i <
                    glowCount;

                if (enable)
                {
                    LampCandidate candidate =
                        lampCandidates[i];

                    glow.transform.position =
                        candidate.GlowPosition;

                    glow.transform.rotation =
                        Quaternion.identity;

                    glow.transform.localScale =
                        Vector3.one *
                        (candidate.IsParkLamp
                            ? 0.47f
                            : 0.19f);
                }

                glow.enabled =
                    enable;
            }
        }

        private Renderer CreateRuntimeLampGlow()
        {
            if (runtimeLampGlowMaterial == null)
            {
                Shader shader =
                    Shader.Find(
                        "MotorCity/StreetLampBulbGlow");

                if (shader == null)
                    return null;

                runtimeLampGlowMaterial =
                    new Material(
                        shader)
                    {
                        name =
                            "MotorCity_LampBulbGlow_Runtime"
                    };

                runtimeLampGlowMaterial.SetColor(
                    "_GlowColor",
                    new Color(
                        1f,
                        0.64f,
                        0.31f,
                        1f));

                runtimeLampGlowMaterial.SetFloat(
                    "_Intensity",
                    3.5f);
            }

            GameObject glowObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);

            glowObject.name =
                "MotorCity_LampBulbGlow";

            glowObject.transform.SetParent(
                transform,
                false);

            glowObject.transform.localScale =
                Vector3.one *
                0.19f;

            Collider collider =
                glowObject.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(
                    collider);
            }

            Renderer renderer =
                glowObject.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    runtimeLampGlowMaterial;

                renderer.shadowCastingMode =
                    ShadowCastingMode.Off;

                renderer.receiveShadows =
                    false;

                renderer.lightProbeUsage =
                    LightProbeUsage.Off;

                renderer.reflectionProbeUsage =
                    ReflectionProbeUsage.Off;

                renderer.enabled =
                    false;
            }

            return renderer;
        }

        private static int CompareLampCandidates(
            LampCandidate a,
            LampCandidate b)
        {
            return
                a.DistanceSquared.CompareTo(
                    b.DistanceSquared);
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

        private static void ConfigureLampLight(
            Light light)
        {
            if (light == null)
                return;

            light.type =
                LightType.Spot;

            light.shadows =
                LightShadows.None;

            light.cullingMask =
                ~0;

            // Fantastic City Generator URP StreetLight-01 reference.
            light.color =
                new Color(
                    1f,
                    0.82f,
                    0.62f);

            light.intensity =
                15f;

            light.range =
                20f;

            light.spotAngle =
                105.661575f;

            light.innerSpotAngle =
                87.358116f;

            light.bounceIntensity =
                1f;

            light.useColorTemperature =
                false;

            light.enabled =
                false;
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

        private struct LampAnchor
        {
            public Vector3 Position;
            public Vector3 GlowPosition;
            public Quaternion Rotation;
            public bool IsParkLamp;
        }

        private struct LampCandidate
        {
            public Vector3 Position;
            public Vector3 GlowPosition;
            public Quaternion Rotation;
            public bool IsParkLamp;
            public float DistanceSquared;
        }

    }
}
