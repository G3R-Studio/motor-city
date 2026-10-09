using System;
using UnityEngine;

namespace MotorCity.Vehicle
{
    /// <summary>
    /// Shared fallback classification for imported vehicle visuals.
    ///
    /// Explicit authored roles (VehiclePaintMeshNames, wheel bindings, etc.)
    /// remain the preferred contract. These helpers exist only for legacy or
    /// third-party models that do not expose explicit roles yet.
    /// </summary>
    public static class VehicleVisualRoleUtility
    {
        // Canonical normalization for imported renderer and mesh names.
        // Keep authored wheel/material roles authoritative; this is a fallback.
        public static string NormalizeVisualName(string value)
        {
            return (value ?? string.Empty)
                .Replace(" (Clone)", string.Empty)
                .Replace(" (Instance)", string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace(' ', '_')
                .Replace('.', '_');
        }

        public static bool IsWheelHierarchy(
            Transform transform,
            string stopAtName = null,
            int maxDepth = 16,
            bool includeAlloy = true)
        {
            Transform current =
                transform;

            int depth =
                0;

            while (current != null &&
                   depth++ < Mathf.Max(
                       1,
                       maxDepth))
            {
                if (IsWheelLikeName(
                        current.name,
                        includeAlloy))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(
                        stopAtName) &&
                    current.name.Equals(
                        stopAtName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current =
                    current.parent;
            }

            return false;
        }

        public static bool IsWheelLikeName(
            string name,
            bool includeAlloy = true)
        {
            string lower =
                (name ?? string.Empty)
                .ToLowerInvariant();

            return
                lower.Contains("wheel") ||
                lower.Contains("tire") ||
                lower.Contains("tyre") ||
                lower.Contains("rim") ||
                (includeAlloy &&
                 lower.Contains("alloy"));
        }

        public static bool IsRimMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string lower =
                NormalizeMaterialName(
                    material.name);

            return
                lower.Contains("rim") ||
                lower.Contains("wheel") ||
                lower.Contains("alloy") ||
                lower.Contains("disc") ||
                lower.Contains("disk");
        }

        public static bool IsRubberMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string lower =
                NormalizeMaterialName(
                    material.name);

            return
                lower.Contains("tire") ||
                lower.Contains("tyre") ||
                lower.Contains("rubber");
        }

        private static string NormalizeMaterialName(
            string name)
        {
            return NormalizeVisualName(name);
        }
    }
}
