using UnityEngine;

namespace MotorCity.World
{
    public sealed class PlayerHeadlights : MonoBehaviour
    {
        private DayNightCycleController dayNight;
        private Light left;
        private Light right;

        private void Awake()
        {
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
                dayNight =
                    Object.FindAnyObjectByType<DayNightCycleController>();
            }

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
            Configure(
                left,
                amount);

            Configure(
                right,
                amount);
        }

        private static void Configure(
            Light light,
            float amount)
        {
            if (light == null)
                return;

            light.enabled =
                amount >
                0.02f;

            light.intensity =
                8.5f *
                amount;
        }
    }
}
