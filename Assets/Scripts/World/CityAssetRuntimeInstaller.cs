using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    public static class CityAssetRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/CityVisual";

        private static readonly List<Vector3> roadPoints = new();

        public static bool TryInstall()
        {
            roadPoints.Clear();

            GameObject prefab =
                Resources.Load<GameObject>(ResourcePath);

            if (prefab == null)
                return false;

            GameObject city =
                Object.Instantiate(prefab);
            city.name = "Motor City — CC0 Asset City";

            CacheRoadPoints(city);
            AddBuildingColliders(city);
            CreateGroundCollider();

            return true;
        }

        public static Vector3 SnapToNearestRoad(Vector3 approximate)
        {
            if (roadPoints.Count == 0)
                return approximate;

            Vector3 flatApprox =
                new(approximate.x, 0f, approximate.z);

            float bestDistance = float.PositiveInfinity;
            Vector3 best = flatApprox;

            foreach (Vector3 point in roadPoints)
            {
                float distance =
                    (point - flatApprox).sqrMagnitude;

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = point;
            }

            return new Vector3(
                best.x,
                approximate.y,
                best.z);
        }

        private static void CacheRoadPoints(GameObject city)
        {
            Renderer[] renderers =
                city.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                string hierarchyName =
                    BuildHierarchyName(renderer.transform);

                bool roadLike =
                    hierarchyName.Contains("road") ||
                    hierarchyName.Contains("street") ||
                    hierarchyName.Contains("intersection");

                if (!roadLike)
                    continue;

                Bounds bounds = renderer.bounds;
                Vector3 center =
                    new(bounds.center.x, 0f, bounds.center.z);

                roadPoints.Add(center);

                Vector3 right =
                    renderer.transform.right *
                    Mathf.Min(bounds.extents.x, 8f);

                Vector3 forward =
                    renderer.transform.forward *
                    Mathf.Min(bounds.extents.z, 8f);

                roadPoints.Add(
                    new Vector3(
                        center.x + right.x,
                        0f,
                        center.z + right.z));

                roadPoints.Add(
                    new Vector3(
                        center.x - right.x,
                        0f,
                        center.z - right.z));

                roadPoints.Add(
                    new Vector3(
                        center.x + forward.x,
                        0f,
                        center.z + forward.z));

                roadPoints.Add(
                    new Vector3(
                        center.x - forward.x,
                        0f,
                        center.z - forward.z));
            }
        }

        private static void AddBuildingColliders(
            GameObject city)
        {
            Renderer[] renderers =
                city.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                string hierarchyName =
                    BuildHierarchyName(renderer.transform);

                if (!hierarchyName.Contains("building"))
                    continue;

                if (renderer.GetComponent<Collider>() != null)
                    continue;

                BoxCollider collider =
                    renderer.gameObject.AddComponent<BoxCollider>();

                collider.center = renderer.localBounds.center;
                collider.size = renderer.localBounds.size;
            }
        }

        private static string BuildHierarchyName(
            Transform transform)
        {
            string value = string.Empty;
            Transform current = transform;

            for (int i = 0;
                 current != null && i < 5;
                 i++)
            {
                value += " " +
                         current.name.ToLowerInvariant();
                current = current.parent;
            }

            return value;
        }

        private static void CreateGroundCollider()
        {
            GameObject ground =
                new("City Ground Physics");

            ground.transform.position =
                new Vector3(0f, -0.22f, 0f);

            BoxCollider collider =
                ground.AddComponent<BoxCollider>();

            collider.size =
                new Vector3(1214.4f, 0.4f, 1214.4f);
        }
    }
}
