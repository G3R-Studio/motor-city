using UnityEngine;

namespace MotorCity.World
{
    internal static class VehicleLampMaterialUtility
    {
        internal static Texture ResolveBaseTexture(
            Material material)
        {
            if (material == null)
                return null;

            if (material.HasProperty(
                    "_BaseMap"))
            {
                Texture texture =
                    material.GetTexture(
                        "_BaseMap");

                if (texture != null)
                    return texture;
            }

            if (material.HasProperty(
                    "_MainTex"))
            {
                return
                    material.GetTexture(
                        "_MainTex");
            }

            return null;
        }

        internal static void ResolveProjectionRange(
            Bounds bounds,
            Vector3 axis,
            out float minimum,
            out float maximum)
        {
            minimum = float.PositiveInfinity;
            maximum = float.NegativeInfinity;

            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner =
                            center +
                            Vector3.Scale(
                                extents,
                                new Vector3(x, y, z));

                        float projection =
                            Vector3.Dot(
                                corner,
                                axis);

                        minimum =
                            Mathf.Min(
                                minimum,
                                projection);

                        maximum =
                            Mathf.Max(
                                maximum,
                                projection);
                    }
                }
            }
        }

        internal static void CopyTextureTransform(
            Material source,
            Material destination)
        {
            if (source == null ||
                destination == null)
            {
                return;
            }

            string property =
                source.HasProperty(
                    "_BaseMap")
                    ? "_BaseMap"
                    : "_MainTex";

            if (!source.HasProperty(
                    property))
            {
                return;
            }

            destination.SetTextureScale(
                "_BaseMap",
                source.GetTextureScale(
                    property));

            destination.SetTextureOffset(
                "_BaseMap",
                source.GetTextureOffset(
                    property));
        }

        internal static bool IsWheelRenderer(
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
    }
}
