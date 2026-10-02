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
        public const float DayGlassLift = 0.38f;
        public const float NightGlassLift = 0.14f;
        public const float Roughness = 0.36f;
        public const float ReflectionStrength = 0.38f;
        public const float AuthoredCubeStrength = 0f;
        public const float FresnelStrength = 0.65f;
        public const float SpecularStrength = 0.08f;

        public static readonly Color DayGlassTint =
            new(
                0.18f,
                0.185f,
                0.19f,
                1f);

        public static readonly Color NightGlassTint =
            new(
                0.04f,
                0.045f,
                0.05f,
                1f);
    }
}
