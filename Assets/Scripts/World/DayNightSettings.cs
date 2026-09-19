using UnityEngine;

namespace MotorCity.World
{
    public sealed class DayNightSettings : ScriptableObject
    {
        [SerializeField] private Material daySkybox;
        [SerializeField] private Material nightSkybox;

        [SerializeField] private Color daySkyColor =
            new(0.68f, 0.67f, 0.64f, 1f);

        [SerializeField] private Color dayEquatorColor =
            new(0.82f, 0.82f, 0.82f, 1f);

        [SerializeField] private Color nightSkyColor =
            new(0.10f, 0.13f, 0.22f, 1f);

        [SerializeField] private Color nightEquatorColor =
            new(0.035f, 0.045f, 0.075f, 1f);

        [SerializeField] private Color sunColor =
            new(1f, 0.83f, 0.58f, 1f);

        [SerializeField] private Color moonColor =
            new(0.66f, 0.68f, 0.82f, 1f);

        [SerializeField] private float sunIntensity =
            1.04f;

        [SerializeField] private float moonIntensity =
            0.32f;

        public Material DaySkybox => daySkybox;
        public Material NightSkybox => nightSkybox;
        public Color DaySkyColor => daySkyColor;
        public Color DayEquatorColor => dayEquatorColor;
        public Color NightSkyColor => nightSkyColor;
        public Color NightEquatorColor => nightEquatorColor;
        public Color SunColor => sunColor;
        public Color MoonColor => moonColor;
        public float SunIntensity => sunIntensity;
        public float MoonIntensity => moonIntensity;
    }
}
