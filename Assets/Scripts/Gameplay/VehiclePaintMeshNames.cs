using System;

namespace MotorCity.Gameplay
{
    // Names are a contract with the artist, independent of vehicle ID/materials.
    public static class VehiclePaintMeshNames
    {
        public static string Normalize(string name)
        {
            return (name ?? string.Empty)
                .Replace(" (Clone)", string.Empty)
                .Replace(" (Instance)", string.Empty)
                .Trim().ToLowerInvariant()
                .Replace(' ', '_').Replace('.', '_');
        }

        public static bool IsBody(string name) => Matches(name, "body");

        public static bool IsWheelPaint(string name) =>
            Matches(name, "front_wheels_misc") ||
            Matches(name, "rear_wheels_misc") ||
            Matches(name, "all_wheels_misc");

        private static bool Matches(string name, string expected)
        {
            string normalized = Normalize(name);
            if (normalized == expected)
                return true;

            // Blender's duplicate suffix, e.g. body.001. Never match body_misc.
            if (!normalized.StartsWith(expected + "_", StringComparison.Ordinal))
                return false;

            string suffix = normalized.Substring(expected.Length + 1);
            if (suffix.Length == 0)
                return false;
            foreach (char character in suffix)
                if (character < '0' || character > '9')
                    return false;
            return true;
        }
    }
}
