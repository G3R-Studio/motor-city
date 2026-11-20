using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Conservative physics-based visibility test for streamed city districts.
    /// This does not replace Unity's renderer frustum culling: it answers whether
    /// a district has at least one camera-visible sample not blocked by another
    /// building collider. Missing/overflowing physics data fails open.
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
            // Fail open if the expected camera/physics data is unavailable.
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

            Vector3 center = targetBounds.center;
            Vector3 extents = targetBounds.extents;
            bool testedSample = false;

            if (SampleIsVisible(camera, center, targetBounds, buildingOccluders, raycastHits, ref testedSample))
                return true;

            if (SampleIsVisible(
                camera,
                center + Vector3.up * extents.y * 0.75f,
                targetBounds, buildingOccluders, raycastHits, ref testedSample))
                return true;

            if (SampleIsVisible(
                camera,
                center + Vector3.right * extents.x * 0.7f,
                targetBounds, buildingOccluders, raycastHits, ref testedSample))
                return true;

            if (SampleIsVisible(
                camera,
                center - Vector3.right * extents.x * 0.7f,
                targetBounds, buildingOccluders, raycastHits, ref testedSample))
                return true;

            if (SampleIsVisible(
                camera,
                center + Vector3.forward * extents.z * 0.7f,
                targetBounds, buildingOccluders, raycastHits, ref testedSample))
                return true;

            if (SampleIsVisible(
                camera,
                center - Vector3.forward * extents.z * 0.7f,
                targetBounds, buildingOccluders, raycastHits, ref testedSample))
                return true;

            // A thin/oddly shaped bound may intersect the frustum even though
            // none of the fixed sample points does. Never hide it in that case.
            return !testedSample;
        }

        private static bool SampleIsVisible(
            Camera camera,
            Vector3 sample,
            Bounds targetBounds,
            HashSet<Collider> buildingOccluders,
            RaycastHit[] raycastHits,
            ref bool testedSample)
        {
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
            float rayLength = distance - 2.5f;

            if (rayLength <= 0.1f)
                return true;

            int hitCount = Physics.RaycastNonAlloc(
                new Ray(origin, delta / distance),
                raycastHits,
                rayLength,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            // If the buffer filled, the nearest hit may have been omitted.
            // Fail open rather than accidentally hiding visible geometry.
            if (hitCount >= raycastHits.Length)
                return true;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = raycastHits[i].collider;
                if (hitCollider == null || !buildingOccluders.Contains(hitCollider))
                    continue;

                // Ignore colliders belonging to the target cell/district itself.
                if (hitCollider.bounds.Intersects(targetBounds))
                    continue;

                if (raycastHits[i].distance < rayLength - 0.5f)
                    return false;
            }

            // No other building collider blocks this sample.
            return true;
        }
    }
}
