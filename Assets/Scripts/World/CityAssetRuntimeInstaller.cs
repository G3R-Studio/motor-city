using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    public static class CityAssetRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/CityVisual";

        private static readonly List<Vector3> roadPoints = new();
        private static readonly Dictionary<string, Transform> namedCityObjects =
            new();

        private static Vector3[] deliveryRoute =
        {
            new(0f, 0f, -42f),
            new(42f, 0f, -42f),
            new(84f, 0f, 42f),
            new(42f, 0f, 84f),
            new(-42f, 0f, 84f)
        };

        private static Vector3[] sprintRoute =
        {
            new(-42f, 0f, -42f),
            new(-84f, 0f, 0f),
            new(-84f, 0f, 84f),
            new(84f, 0f, 84f),
            new(84f, 0f, 0f),
            new(42f, 0f, -42f)
        };

        public static Vector3 PlayerSpawnPoint { get; private set; } =
            new(0f, 0f, -42f);

        public static Quaternion PlayerSpawnRotation { get; private set; } =
            Quaternion.identity;

        public static Vector3 GaragePoint { get; private set; } =
            new(-42f, 0f, 42f);

        public static Vector3 DriftChallengePoint { get; private set; } =
            Vector3.zero;

        public static Vector3[] DeliveryRoute =>
            (Vector3[])deliveryRoute.Clone();

        public static Vector3[] SprintRoute =>
            (Vector3[])sprintRoute.Clone();

        public static bool TryInstall()
        {
            roadPoints.Clear();
            namedCityObjects.Clear();

            GameObject prefab =
                Resources.Load<GameObject>(ResourcePath);

            if (prefab == null)
                return false;

            GameObject city =
                Object.Instantiate(prefab);
            city.name = "Motor City — CC0 Asset City";

            IndexCityObjects(city);
            CacheRoadPoints(city);
            AddBuildingColliders(city);
            CreateGroundCollider();
            ResolveGameplayLayout();

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

        private static void IndexCityObjects(GameObject city)
        {
            Transform[] transforms =
                city.GetComponentsInChildren<Transform>(true);

            foreach (Transform item in transforms)
            {
                if (item == null ||
                    string.IsNullOrWhiteSpace(item.name) ||
                    namedCityObjects.ContainsKey(item.name))
                    continue;

                namedCityObjects.Add(item.name, item);
            }
        }

        private static void ResolveGameplayLayout()
        {
            // These anchors were reviewed directly in the source City 02 prefab.
            // We resolve the Transform by name after the installer has uniformly
            // scaled and centered the city, so no guessed runtime coordinates are
            // involved.
            Transform player =
                FindNamed("road-straight_942");
            Transform garage =
                FindNamed("road-straight_1023");
            Transform drift =
                FindNamed("road-crossroad-path_18");

            if (player != null)
            {
                PlayerSpawnPoint = Flat(player.position);

                Vector3 forward =
                    Vector3.ProjectOnPlane(
                        player.forward,
                        Vector3.up);

                if (forward.sqrMagnitude > 0.01f)
                    PlayerSpawnRotation =
                        Quaternion.LookRotation(
                            forward.normalized,
                            Vector3.up);
            }
            else
            {
                PlayerSpawnPoint =
                    SnapToNearestRoad(
                        new Vector3(0f, 0f, -42f));
                PlayerSpawnRotation = Quaternion.identity;
            }

            GaragePoint = garage != null
                ? Flat(garage.position)
                : SnapToNearestRoad(new Vector3(-42f, 0f, 42f));

            DriftChallengePoint = drift != null
                ? Flat(drift.position)
                : SnapToNearestRoad(Vector3.zero);

            deliveryRoute = ResolveRoute(
                new[]
                {
                    "road-straight_1299",
                    "road-straight_1289",
                    "road-straight_955",
                    "road-straight_1001",
                    "road-straight_1103"
                },
                deliveryRoute);

            sprintRoute = ResolveRoute(
                new[]
                {
                    "road-straight_1307",
                    "road-straight_1395",
                    "road-straight_1391",
                    "road-straight_1245",
                    "road-straight_1260",
                    "road-straight_1387"
                },
                sprintRoute);

            Debug.Log(
                "Motor City layout resolved from City 02 prefab. " +
                $"Spawn={PlayerSpawnPoint}, " +
                $"Garage={GaragePoint}, " +
                $"Drift={DriftChallengePoint}, " +
                $"DeliveryStart={deliveryRoute[0]}, " +
                $"SprintStart={sprintRoute[0]}");
        }

        private static Vector3[] ResolveRoute(
            string[] objectNames,
            Vector3[] fallback)
        {
            Vector3[] result =
                new Vector3[objectNames.Length];

            for (int i = 0; i < objectNames.Length; i++)
            {
                Transform item =
                    FindNamed(objectNames[i]);

                result[i] = item != null
                    ? Flat(item.position)
                    : SnapToNearestRoad(
                        fallback[
                            Mathf.Min(
                                i,
                                fallback.Length - 1)]);
            }

            return result;
        }

        private static Transform FindNamed(string name)
        {
            namedCityObjects.TryGetValue(
                name,
                out Transform value);
            return value;
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
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
