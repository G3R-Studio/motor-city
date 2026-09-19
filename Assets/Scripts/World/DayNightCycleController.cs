using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly List<Material> runtimeNightMaterials =
            new();

        private readonly List<MaterialSlot> cityMaterialSlots =
            new();

        private readonly Dictionary<string, Material> nightMaterialByDayName =
            new(
                StringComparer.OrdinalIgnoreCase);

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
            BuildNightMaterialMap();
            RefreshCityMaterialSlots();
            RefreshStreetLights();

            Debug.Log(
                "Motor City: day/night prepared. " +
                $"material slots={cityMaterialSlots.Count}, " +
                $"local city lights={streetLights.Count}.");

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

            foreach (Material material in
                     runtimeNightMaterials)
            {
                if (material != null)
                {
                    Destroy(
                        material);
                }
            }
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

        private void BuildNightMaterialMap()
        {
            nightMaterialByDayName.Clear();

            if (settings == null)
                return;

            Material[] day =
                settings.DayMaterials;

            Material[] night =
                settings.NightMaterials;

            if (day == null ||
                night == null)
                return;

            int count =
                Mathf.Min(
                    day.Length,
                    night.Length);

            for (int i = 0;
                 i < count;
                 i++)
            {
                Material dayMaterial =
                    day[i];

                Material nightSource =
                    night[i];

                if (dayMaterial == null ||
                    nightSource == null)
                    continue;

                string key =
                    NormalizeMaterialName(
                        dayMaterial.name);

                if (string.IsNullOrWhiteSpace(
                        key))
                    continue;

                Material runtimeNight =
                    CreateRuntimeNightMaterial(
                        nightSource);

                if (runtimeNight == null)
                    continue;

                runtimeNightMaterials.Add(
                    runtimeNight);

                nightMaterialByDayName[key] =
                    runtimeNight;
            }
        }

        private void RefreshCityMaterialSlots()
        {
            cityMaterialSlots.Clear();

            if (nightMaterialByDayName.Count == 0)
                return;

            Renderer[] renderers =
                UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    !IsRuntimeCityHierarchy(
                        renderer.transform))
                    continue;

                Material[] materials =
                    renderer.sharedMaterials;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material dayMaterial =
                        materials[i];

                    if (dayMaterial == null)
                        continue;

                    string key =
                        NormalizeMaterialName(
                            dayMaterial.name);

                    if (!nightMaterialByDayName.TryGetValue(
                            key,
                            out Material nightMaterial))
                        continue;

                    cityMaterialSlots.Add(
                        new MaterialSlot(
                            renderer,
                            i,
                            dayMaterial,
                            nightMaterial));
                }
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
                ApplyCityNightMaterials();
            }
        }

        private void RefreshStreetLights()
        {
            streetLights.Clear();

            Light[] all =
                UnityEngine.Object.FindObjectsByType<Light>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (Light light in all)
            {
                if (light == null ||
                    light == directionalLight ||
                    light == moonLight ||
                    light.type ==
                    LightType.Directional)
                    continue;

                if (!IsRuntimeCityHierarchy(
                        light.transform))
                    continue;

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

                BreakableStreetProp breakable =
                    light.GetComponentInParent<BreakableStreetProp>(
                        true);

                bool broken =
                    breakable != null &&
                    breakable.IsBroken;

                bool shouldEnable =
                    IsNight &&
                    !broken;

                if (light.gameObject.activeSelf !=
                    shouldEnable)
                {
                    light.gameObject.SetActive(
                        shouldEnable);
                }

                light.enabled =
                    shouldEnable;
            }
        }

        private void ApplyCityNightMaterials()
        {
            foreach (MaterialSlot slot in
                     cityMaterialSlots)
            {
                if (slot.Renderer == null)
                    continue;

                BreakableStreetProp breakable =
                    slot.Renderer.GetComponentInParent<BreakableStreetProp>(
                        true);

                if (breakable != null &&
                    breakable.IsBroken)
                    continue;

                Material[] materials =
                    slot.Renderer.sharedMaterials;

                if (slot.Index < 0 ||
                    slot.Index >=
                    materials.Length)
                    continue;

                materials[slot.Index] =
                    IsNight
                        ? slot.NightMaterial
                        : slot.DayMaterial;

                slot.Renderer.sharedMaterials =
                    materials;
            }
        }

        private static Material CreateRuntimeNightMaterial(
            Material source)
        {
            if (source == null)
                return null;

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            Material runtime =
                shader != null
                    ? new Material(
                        shader)
                    : new Material(
                        source);

            runtime.name =
                "MotorCity_Night_" +
                source.name;

            Texture baseTexture =
                FirstTexture(
                    source,
                    "_BaseMap",
                    "_MainTex",
                    "_BaseColorMap",
                    "_Albedo",
                    "_AlbedoMap",
                    "_Diffuse",
                    "_DiffuseMap");

            if (baseTexture != null &&
                runtime.HasProperty(
                    "_BaseMap"))
            {
                runtime.SetTexture(
                    "_BaseMap",
                    baseTexture);
            }

            Color baseColor =
                source.HasProperty(
                    "_BaseColor")
                    ? source.GetColor(
                        "_BaseColor")
                    : source.HasProperty(
                          "_Color")
                        ? source.GetColor(
                            "_Color")
                        : Color.white;

            if (runtime.HasProperty(
                    "_BaseColor"))
            {
                runtime.SetColor(
                    "_BaseColor",
                    baseColor);
            }

            Texture emissionTexture =
                FirstTexture(
                    source,
                    "_EmissionMap",
                    "_Illum",
                    "_Emission");

            if (emissionTexture == null)
            {
                emissionTexture =
                    baseTexture;
            }

            if (emissionTexture != null &&
                runtime.HasProperty(
                    "_EmissionMap"))
            {
                runtime.SetTexture(
                    "_EmissionMap",
                    emissionTexture);
            }

            Color emissionColor =
                source.HasProperty(
                    "_EmissionColor")
                    ? source.GetColor(
                        "_EmissionColor")
                    : Color.white;

            if (emissionColor.maxColorComponent <
                0.1f)
            {
                emissionColor =
                    Color.white;
            }

            emissionColor *=
                1.8f;

            if (runtime.HasProperty(
                    "_EmissionColor"))
            {
                runtime.SetColor(
                    "_EmissionColor",
                    emissionColor);
            }

            runtime.EnableKeyword(
                "_EMISSION");

            runtime.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;

            if (runtime.HasProperty(
                    "_Metallic") &&
                source.HasProperty(
                    "_Metallic"))
            {
                runtime.SetFloat(
                    "_Metallic",
                    source.GetFloat(
                        "_Metallic"));
            }

            if (runtime.HasProperty(
                    "_Smoothness"))
            {
                float smoothness =
                    source.HasProperty(
                        "_Smoothness")
                        ? source.GetFloat(
                            "_Smoothness")
                        : source.HasProperty(
                              "_Glossiness")
                            ? source.GetFloat(
                                "_Glossiness")
                            : 0.35f;

                runtime.SetFloat(
                    "_Smoothness",
                    smoothness);
            }

            return runtime;
        }

        private static Texture FirstTexture(
            Material material,
            params string[] properties)
        {
            if (material == null)
                return null;

            foreach (string property in
                     properties)
            {
                if (!material.HasProperty(
                        property))
                    continue;

                Texture texture =
                    material.GetTexture(
                        property);

                if (texture != null)
                    return texture;
            }

            return null;
        }

        private static bool IsRuntimeCityHierarchy(
            Transform item)
        {
            Transform current =
                item;

            while (current != null)
            {
                if (string.Equals(
                        current.name,
                        "MotorCity_FCGCity",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        current.name,
                        "City-Maker",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static string NormalizeMaterialName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
                return string.Empty;

            string normalized =
                value.Trim();

            if (normalized.StartsWith(
                    "FCG_",
                    StringComparison.OrdinalIgnoreCase))
            {
                normalized =
                    normalized.Substring(
                        4);
            }

            normalized =
                normalized.Replace(
                    "(Instance)",
                    string.Empty);

            return new string(
                normalized
                    .ToLowerInvariant()
                    .Where(
                        char.IsLetterOrDigit)
                    .ToArray());
        }

        private readonly struct MaterialSlot
        {
            public Renderer Renderer { get; }
            public int Index { get; }
            public Material DayMaterial { get; }
            public Material NightMaterial { get; }

            public MaterialSlot(
                Renderer renderer,
                int index,
                Material dayMaterial,
                Material nightMaterial)
            {
                Renderer =
                    renderer;

                Index =
                    index;

                DayMaterial =
                    dayMaterial;

                NightMaterial =
                    nightMaterial;
            }
        }
    }
}
