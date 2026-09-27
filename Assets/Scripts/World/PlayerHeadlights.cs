using System.Collections.Generic;
using MotorCity.Platform;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class PlayerHeadlights : MonoBehaviour
    {
        private DayNightCycleController dayNight;
        private Light left;
        private Light right;
        private const string RuntimeVisualName =
            "MotorCityVehicleVisual_Runtime";

        private float dayNightResolveTimer;
        private ArcadeCarController car;
        private Transform currentVisual;
        private bool anchorsDirty = true;
        private string vehicleId = "street";

        private sealed class NightEmissionBinding
        {
            public Renderer Renderer;
            public int MaterialIndex;
        }

        private readonly List<NightEmissionBinding>
            nightEmissionBindings = new();

        private MaterialPropertyBlock
            nightEmissionBlock;

        private void Awake()
        {
            nightEmissionBlock =
                new MaterialPropertyBlock();

            car =
                GetComponent<ArcadeCarController>();

            dayNight =
                Object.FindAnyObjectByType<DayNightCycleController>();

            left =
                CreateHeadlight(
                    "Headlight Left",
                    new Vector3(
                        -0.62f,
                        0.62f,
                        1.88f));

            right =
                CreateHeadlight(
                    "Headlight Right",
                    new Vector3(
                        0.62f,
                        0.62f,
                        1.88f));

            ApplyLights(
                0f);
        }

        private void Update()
        {
            if (dayNight == null)
            {
                dayNightResolveTimer -=
                    Time.unscaledDeltaTime;

                if (dayNightResolveTimer <= 0f)
                {
                    dayNightResolveTimer = 1f;

                    dayNight =
                        Object.FindAnyObjectByType<DayNightCycleController>();
                }
            }

            RefreshAnchorsIfNeeded();

            float night =
                dayNight != null
                    ? dayNight.NightAmount
                    : 0f;

            float amount =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(
                        0.34f,
                        0.72f,
                        night));

            ApplyLights(
                amount);

            ApplyNightVisualEmission(
                night);
        }

        public void SetVehicleId(
            string id)
        {
            vehicleId =
                string.IsNullOrWhiteSpace(id)
                    ? "street"
                    : id.ToLowerInvariant();

            // Positioning stays geometry-based for every vehicle, while some
            // authored visuals (currently Delorean) also expose emissive
            // headlamp/neon geometry that is driven by the night cycle.
            anchorsDirty = true;
            RefreshAnchorsIfNeeded();
            RefreshNightEmissionBindings();
        }

        private void RefreshAnchorsIfNeeded()
        {
            Transform visual =
                transform.Find(
                    RuntimeVisualName);

            if (visual == currentVisual &&
                !anchorsDirty)
            {
                return;
            }

            currentVisual =
                visual;
            anchorsDirty =
                false;

            if (currentVisual == null ||
                left == null ||
                right == null)
            {
                return;
            }

            Renderer[] renderers =
                currentVisual.GetComponentsInChildren<Renderer>(
                    true);

            bool initialized =
                false;

            Bounds localBounds =
                new(
                    Vector3.zero,
                    Vector3.zero);

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    IsWheelRenderer(
                        renderer.transform))
                {
                    continue;
                }

                Bounds world =
                    renderer.bounds;

                Vector3 min =
                    world.min;
                Vector3 max =
                    world.max;

                for (int x = 0;
                     x < 2;
                     x++)
                {
                    for (int y = 0;
                         y < 2;
                         y++)
                    {
                        for (int z = 0;
                             z < 2;
                             z++)
                        {
                            Vector3 corner =
                                new(
                                    x == 0
                                        ? min.x
                                        : max.x,
                                    y == 0
                                        ? min.y
                                        : max.y,
                                    z == 0
                                        ? min.z
                                        : max.z);

                            Vector3 local =
                                transform.InverseTransformPoint(
                                    corner);

                            if (!initialized)
                            {
                                localBounds =
                                    new Bounds(
                                        local,
                                        Vector3.zero);

                                initialized =
                                    true;
                            }
                            else
                            {
                                localBounds.Encapsulate(
                                    local);
                            }
                        }
                    }
                }
            }

            if (!initialized)
                return;

            float halfWidth =
                Mathf.Max(
                    0.45f,
                    localBounds.extents.x);

            float xOffset =
                Mathf.Clamp(
                    halfWidth * 0.58f,
                    0.52f,
                    0.92f);

            float lightY =
                Mathf.Lerp(
                    localBounds.min.y,
                    localBounds.max.y,
                    0.34f);

            // Renderer bounds end at the outermost front body surface
            // (usually the bumper). Placing a Spot Light at max.z or beyond it
            // makes the source visibly float in front of the car. Keep both
            // lamps slightly inside the front fascia instead. Scaling the inset
            // with body length works for compact cars, the six-wheel Apex and
            // the longer bus without per-vehicle magic numbers.
            float frontInset =
                Mathf.Clamp(
                    localBounds.size.z * 0.10f,
                    0.20f,
                    0.48f);

            float lightZ =
                localBounds.max.z -
                frontInset;

            left.transform.localPosition =
                new Vector3(
                    -xOffset,
                    lightY,
                    lightZ);

            right.transform.localPosition =
                new Vector3(
                    xOffset,
                    lightY,
                    lightZ);

            RefreshNightEmissionBindings();
        }

        private void RefreshNightEmissionBindings()
        {
            nightEmissionBindings.Clear();

            if (currentVisual == null ||
                vehicleId != "delorean")
            {
                return;
            }

            Renderer[] renderers =
                currentVisual.GetComponentsInChildren<Renderer>(
                    true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    IsWheelRenderer(
                        renderer.transform))
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material material =
                        materials[i];

                    if (material == null)
                        continue;

                    string materialName =
                        material.name
                            .ToLowerInvariant();

                    if (!materialName.Contains(
                            "deloreanemission"))
                    {
                        continue;
                    }

                    nightEmissionBindings.Add(
                        new NightEmissionBinding
                        {
                            Renderer = renderer,
                            MaterialIndex = i
                        });
                }
            }
        }

        private void ApplyNightVisualEmission(
            float night)
        {
            if (vehicleId != "delorean" ||
                nightEmissionBindings.Count == 0)
            {
                return;
            }

            float intensity =
                Mathf.SmoothStep(
                    0f,
                    2.4f,
                    Mathf.InverseLerp(
                        0.28f,
                        0.68f,
                        night));

            Color emissionColor =
                Color.white *
                intensity;

            for (int i = 0;
                 i < nightEmissionBindings.Count;
                 i++)
            {
                NightEmissionBinding binding =
                    nightEmissionBindings[i];

                if (binding?.Renderer == null)
                    continue;

                if (nightEmissionBlock == null)
                {
                    nightEmissionBlock =
                        new MaterialPropertyBlock();
                }

                binding.Renderer.GetPropertyBlock(
                    nightEmissionBlock,
                    binding.MaterialIndex);

                nightEmissionBlock.SetColor(
                    "_EmissionColor",
                    emissionColor);

                binding.Renderer.SetPropertyBlock(
                    nightEmissionBlock,
                    binding.MaterialIndex);

                nightEmissionBlock.Clear();
            }
        }

        private static bool IsWheelRenderer(
            Transform item)
        {
            Transform cursor =
                item;

            while (cursor != null)
            {
                string name =
                    cursor.name
                        .ToLowerInvariant();

                if (name.Contains("wheel") ||
                    name.Contains("tire") ||
                    name.Contains("tyre") ||
                    name.Contains("rim"))
                {
                    return true;
                }

                cursor =
                    cursor.parent;
            }

            return false;
        }

        private Light CreateHeadlight(
            string lightName,
            Vector3 localPosition)
        {
            GameObject lightObject =
                new(lightName);

            lightObject.transform.SetParent(
                transform,
                false);

            lightObject.transform.localPosition =
                localPosition;

            lightObject.transform.localRotation =
                Quaternion.Euler(
                    7f,
                    0f,
                    0f);

            Light light =
                lightObject.AddComponent<Light>();

            light.type =
                LightType.Spot;

            light.shadows =
                LightShadows.None;

            light.color =
                new Color(
                    0.88f,
                    0.93f,
                    1f);

            light.range =
                46f;

            light.spotAngle =
                54f;

            light.innerSpotAngle =
                28f;

            light.bounceIntensity =
                0f;

            light.cullingMask =
                ~0;

            light.enabled =
                false;

            return light;
        }

        private void ApplyLights(
            float amount)
        {
            float speed01 =
                car == null
                    ? 0f
                    : Mathf.InverseLerp(
                        25f,
                        180f,
                        car.SpeedKph);

            Configure(
                left,
                amount,
                speed01);

            Configure(
                right,
                amount,
                speed01);
        }

        private static void Configure(
            Light light,
            float amount,
            float speed01)
        {
            if (light == null)
                return;

            light.enabled =
                amount >
                0.02f;

            float qualityIntensity =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low =>
                        0.72f,

                    MotorCityQualityPreset.High =>
                        1.08f,

                    _ =>
                        0.90f
                };

            float qualityRange =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low =>
                        0.78f,

                    MotorCityQualityPreset.High =>
                        1.08f,

                    _ =>
                        0.92f
                };

            light.intensity =
                Mathf.Lerp(
                    8.0f,
                    10.2f,
                    speed01) *
                amount *
                qualityIntensity;

            light.range =
                Mathf.Lerp(
                    46f,
                    72f,
                    speed01) *
                qualityRange;

            light.spotAngle =
                Mathf.Lerp(
                    54f,
                    44f,
                    speed01);

            light.innerSpotAngle =
                Mathf.Lerp(
                    28f,
                    24f,
                    speed01);
        }
    }
}
