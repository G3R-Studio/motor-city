using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Single source of truth for Fantastic City Generator architectural glass.
    /// Editor conversion and runtime fallback materials must use the same values.
    /// </summary>
    public static class FcgGlassProfile
    {
        public const float BumpScale = 0.52f;
        public const float DayGlassLift = 0.24f;
        public const float NightGlassLift = 0.10f;
        public const float Roughness = 0.28f;
        public const float ReflectionStrength = 0.52f;
        public const float AuthoredCubeStrength = 0f;
        public const float FresnelStrength = 0.72f;
        public const float SpecularStrength = 0.06f;

        public static readonly Color DayGlassTint =
            new(
                0.12f,
                0.13f,
                0.14f,
                1f);

        public static readonly Color NightGlassTint =
            new(
                0.025f,
                0.03f,
                0.04f,
                1f);
    }
}
