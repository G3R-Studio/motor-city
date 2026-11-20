using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Conservative physics-based visibility test for city buildings.
    /// A building is considered visible when at least two camera-facing
    /// samples are not blocked by another building collider. Missing or
    /// overflowing physics data fails open.
    /// </summary>
    public static class CityOcclusionVisibility
    {
        public static int CollectBuildingOccluders(
            GameObject city,
            HashSet<Collider> destination)
        {
            destination.Clear();
            if (city == null)
                return 0;

            Collider[] colliders = city.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (candidate == null || !candidate.enabled || candidate.isTrigger)
                    continue;

                if (!IsInsideBuildings(candidate.transform))
                    continue;

                Bounds bounds = candidate.bounds;
                if (bounds.size.y < 5f || bounds.extents.sqrMagnitude < 9f)
                    continue;

                destination.Add(candidate);
            }

            return destination.Count;
        }

        public static bool IsInsideBuildings(Transform target)
        {
            for (Transform current = target; current != null; current = current.parent)
            {
                if (string.Equals(
                    current.name,
                    "Buildings",
                    System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsBoundsVisible(
            Camera camera,
            Bounds targetBounds,
            HashSet<Collider> buildingOccluders,
            Plane[] frustumPlanes,
            RaycastHit[] raycastHits,
            float nearKeepDistance)
        {
            // Fail open if expected camera/physics data is unavailable.
            if (camera == null || buildingOccluders == null ||
                buildingOccluders.Count == 0 || frustumPlanes == null ||
                raycastHits == null || raycastHits.Length == 0)
                return true;

            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, targetBounds))
                return false;

            Vector3 cameraPosition = camera.transform.position;
            if (targetBounds.Contains(cameraPosition) ||
                targetBounds.SqrDistance(cameraPosition) <= nearKeepDistance * nearKeepDistance)
                return true;

            // Sample a 3x3 grid on the AABB face turned toward the camera.
            // This is much more stable than six isolated center/axis samples,
            // especially when the camera is low or looking upward.
            Vector3 relative = cameraPosition - targetBounds.center;
            Vector3 extents = targetBounds.extents;

            float nx = Mathf.Abs(relative.x) / Mathf.Max(extents.x, 0.01f);
            float ny = Mathf.Abs(relative.y) / Mathf.Max(extents.y, 0.01f);
            float nz = Mathf.Abs(relative.z) / Mathf.Max(extents.z, 0.01f);

            Vector3 center = targetBounds.center;
            Vector3 sampleCenter;
            Vector3 axisA;
            Vector3 axisB;

            if (nx >= ny && nx >= nz)
            {
                sampleCenter = center + Vector3.right *
                    (Mathf.Sign(relative.x) * extents.x * 0.9f);
                axisA = Vector3.up * extents.y * 0.72f;
                axisB = Vector3.forward * extents.z * 0.72f;
            }
            else if (ny >= nx && ny >= nz)
            {
                sampleCenter = center + Vector3.up *
                    (Mathf.Sign(relative.y) * extents.y * 0.9f);
                axisA = Vector3.right * extents.x * 0.72f;
                axisB = Vector3.forward * extents.z * 0.72f;
            }
            else
            {
                sampleCenter = center + Vector3.forward *
                    (Mathf.Sign(relative.z) * extents.z * 0.9f);
                axisA = Vector3.right * extents.x * 0.72f;
                axisB = Vector3.up * extents.y * 0.72f;
            }

            int visibleSamples = 0;
            int testedSamples = 0;

            for (int a = -1; a <= 1; a++)
            {
                for (int b = -1; b <= 1; b++)
                {
                    Vector3 sample = sampleCenter +
                        axisA * (a * 0.78f) +
                        axisB * (b * 0.78f);

                    bool tested;
                    bool visible = SampleIsVisible(
                        camera,
                        sample,
                        targetBounds,
                        buildingOccluders,
                        raycastHits,
                        out tested);

                    if (tested)
                    {
                        testedSamples++;
                        if (visible)
                            visibleSamples++;
                    }
                }
            }

            // If the AABB intersects the frustum but none of our samples
            // lands inside the viewport, do not risk hiding the building.
            if (testedSamples == 0)
                return true;

            // Require more than one clear sample to avoid showing an entire
            // building because of a single unstable ray at a roof/edge.
            return visibleSamples >= 2;
        }

        private static bool SampleIsVisible(
            Camera camera,
            Vector3 sample,
            Bounds targetBounds,
            HashSet<Collider> buildingOccluders,
            RaycastHit[] raycastHits,
            out bool testedSample)
        {
            testedSample = false;

            Vector3 viewport = camera.WorldToViewportPoint(sample);
            if (viewport.z <= camera.nearClipPlane ||
                viewport.z >= camera.farClipPlane ||
                viewport.x < 0f || viewport.x > 1f ||
                viewport.y < 0f || viewport.y > 1f)
                return false;

            testedSample = true;

            Vector3 origin = camera.transform.position;
            Vector3 delta = sample - origin;
            float distance = delta.magnitude;
            float rayLength = distance - 0.2f;

            if (rayLength <= 0.1f)
                return true;

            int hitCount = Physics.RaycastNonAlloc(
                new Ray(origin, delta / distance),
                raycastHits,
                rayLength,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            // A full buffer can omit relevant hits: fail open.
            if (hitCount >= raycastHits.Length)
                return true;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = raycastHits[i].collider;
                if (hitCollider == null ||
                    !buildingOccluders.Contains(hitCollider))
                    continue;

                // Ignore colliders belonging to the target building.
                if (hitCollider.bounds.Intersects(targetBounds))
                    continue;

                if (raycastHits[i].distance < rayLength - 0.1f)
                    return false;
            }

            return true;
        }
    }
}
