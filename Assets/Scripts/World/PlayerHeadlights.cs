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
        private float anchorRefreshTimer;
        private ArcadeCarController car;
        private Transform currentVisual;

        private void Awake()
        {
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
        }

        private void RefreshAnchorsIfNeeded()
        {
            anchorRefreshTimer -=
                Time.unscaledDeltaTime;

            Transform visual =
                transform.Find(
                    RuntimeVisualName);

            if (visual == currentVisual &&
                anchorRefreshTimer > 0f)
            {
                return;
            }

            currentVisual =
                visual;
            anchorRefreshTimer =
                0.35f;

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

            float y =
                Mathf.Lerp(
                    localBounds.min.y,
                    localBounds.max.y,
                    0.34f);

            float z =
                localBounds.max.z +
                0.08f;

            left.transform.localPosition =
                new Vector3(
                    -xOffset,
                    y,
                    z);

            right.transform.localPosition =
                new Vector3(
                    xOffset,
                    y,
                    z);
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

            light.lightmapBakeType =
                LightmapBakeType.Realtime;

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
