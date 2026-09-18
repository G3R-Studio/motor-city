using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    public static class CityAssetRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/CityVisual";

        private struct RoadAnchor
        {
            public Vector3 Position;
            public Vector3 Direction;
        }

        private static readonly List<Vector3> roadPoints = new();
        private static readonly List<RoadAnchor> roadAnchors = new();

        private static Vector3[] deliveryRoute =
        {
            new(-80f, 0f, -40f),
            new(-20f, 0f, -40f),
            new(40f, 0f, -10f),
            new(80f, 0f, 40f),
            new(10f, 0f, 70f)
        };

        private static Vector3[] sprintRoute =
        {
            new(-90f, 0f, -60f),
            new(-90f, 0f, 50f),
            new(-20f, 0f, 90f),
            new(90f, 0f, 70f),
            new(90f, 0f, -40f),
            new(10f, 0f, -80f)
        };

        public static Vector3 PlayerSpawnPoint { get; private set; } =
            Vector3.zero;

        public static Quaternion PlayerSpawnRotation { get; private set; } =
            Quaternion.identity;

        public static Vector3 GaragePoint { get; private set; } =
            new(-40f, 0f, 40f);

        public static Vector3 DriftChallengePoint { get; private set; } =
            Vector3.zero;

        public static Vector3[] DeliveryRoute =>
            (Vector3[])deliveryRoute.Clone();

        public static Vector3[] SprintRoute =>
            (Vector3[])sprintRoute.Clone();

        public static bool TryInstall()
        {
            roadPoints.Clear();
            roadAnchors.Clear();

            GameObject prefab =
                Resources.Load<GameObject>(ResourcePath);

            if (prefab == null)
                return false;

            GameObject city =
                Object.Instantiate(prefab);

            city.name =
                "Motor City — Versatile Demo City";

            CacheRoadPoints(city);
            AddBuildingColliders(city);
            CreateGroundCollider(city);
            ResolveGameplayLayout();

            return true;
        }

        public static Vector3 SnapToNearestRoad(
            Vector3 approximate)
        {
            if (roadPoints.Count == 0)
                return approximate;

            Vector3 flatApprox =
                new(
                    approximate.x,
                    0f,
                    approximate.z);

            float bestDistance =
                float.PositiveInfinity;

            Vector3 best =
                flatApprox;

            foreach (Vector3 point in roadPoints)
            {
                float distance =
                    (point - flatApprox).sqrMagnitude;

                if (distance >= bestDistance)
                    continue;

                bestDistance =
                    distance;

                best =
                    point;
            }

            return new Vector3(
                best.x,
                approximate.y,
                best.z);
        }

        private static void ResolveGameplayLayout()
        {
            if (roadPoints.Count == 0)
            {
                PlayerSpawnPoint =
                    Vector3.zero;

                PlayerSpawnRotation =
                    Quaternion.identity;

                GaragePoint =
                    new Vector3(-40f, 0f, 40f);

                DriftChallengePoint =
                    Vector3.zero;

                return;
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;

            foreach (Vector3 point in roadPoints)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            Vector3 center =
                new(
                    (minX + maxX) * 0.5f,
                    0f,
                    (minZ + maxZ) * 0.5f);

            float halfX =
                Mathf.Max(
                    40f,
                    (maxX - minX) * 0.5f);

            float halfZ =
                Mathf.Max(
                    40f,
                    (maxZ - minZ) * 0.5f);

            PlayerSpawnPoint =
                SnapToNearestRoad(
                    center +
                    new Vector3(
                        0f,
                        0f,
                        -halfZ * 0.18f));

            PlayerSpawnRotation =
                ResolveRoadRotation(
                    PlayerSpawnPoint);

            GaragePoint =
                SnapToNearestRoad(
                    center +
                    new Vector3(
                        -halfX * 0.42f,
                        0f,
                        halfZ * 0.18f));

            DriftChallengePoint =
                SnapToNearestRoad(
                    center);

            deliveryRoute =
                BuildRoute(
                    center,
                    halfX,
                    halfZ,
                    new[]
                    {
                        new Vector2(-0.62f, -0.35f),
                        new Vector2(-0.18f, -0.35f),
                        new Vector2(0.28f, -0.08f),
                        new Vector2(0.62f, 0.32f),
                        new Vector2(0.08f, 0.58f)
                    });

            sprintRoute =
                BuildRoute(
                    center,
                    halfX,
                    halfZ,
                    new[]
                    {
                        new Vector2(-0.72f, -0.55f),
                        new Vector2(-0.74f, 0.42f),
                        new Vector2(-0.18f, 0.70f),
                        new Vector2(0.72f, 0.55f),
                        new Vector2(0.72f, -0.34f),
                        new Vector2(0.08f, -0.68f)
                    });

            Debug.Log(
                "Motor City layout resolved from the current runtime city. " +
                $"Spawn={PlayerSpawnPoint}, " +
                $"Garage={GaragePoint}, " +
                $"Drift={DriftChallengePoint}, " +
                $"DeliveryStart={deliveryRoute[0]}, " +
                $"SprintStart={sprintRoute[0]}");
        }

        private static Vector3[] BuildRoute(
            Vector3 center,
            float halfX,
            float halfZ,
            Vector2[] normalizedPoints)
        {
            Vector3[] route =
                new Vector3[normalizedPoints.Length];

            for (int i = 0;
                 i < normalizedPoints.Length;
                 i++)
            {
                Vector2 point =
                    normalizedPoints[i];

                Vector3 approximate =
                    center +
                    new Vector3(
                        point.x * halfX,
                        0f,
                        point.y * halfZ);

                route[i] =
                    SnapToNearestRoad(
                        approximate);
            }

            return route;
        }

        private static Quaternion ResolveRoadRotation(
            Vector3 position)
        {
            if (roadAnchors.Count == 0)
                return Quaternion.identity;

            float bestDistance =
                float.PositiveInfinity;

            Vector3 bestDirection =
                Vector3.forward;

            foreach (RoadAnchor anchor in roadAnchors)
            {
                float distance =
                    (anchor.Position - position)
                    .sqrMagnitude;

                if (distance >= bestDistance)
                    continue;

                bestDistance =
                    distance;

                bestDirection =
                    anchor.Direction;
            }

            bestDirection.y = 0f;

            if (bestDirection.sqrMagnitude < 0.01f)
                bestDirection =
                    Vector3.forward;

            return Quaternion.LookRotation(
                bestDirection.normalized,
                Vector3.up);
        }

        private static void CacheRoadPoints(
            GameObject city)
        {
            Renderer[] renderers =
                city.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                string hierarchyName =
                    BuildHierarchyName(
                        renderer.transform);

                bool roadLike =
                    hierarchyName.Contains("road") ||
                    hierarchyName.Contains("street") ||
                    hierarchyName.Contains("highway") ||
                    hierarchyName.Contains("asphalt") ||
                    hierarchyName.Contains("intersection") ||
                    hierarchyName.Contains("lane");

                if (!roadLike)
                    continue;

                Bounds bounds =
                    renderer.bounds;

                Vector3 center =
                    new(
                        bounds.center.x,
                        0f,
                        bounds.center.z);

                Vector3 direction =
                    renderer.localBounds.size.x >=
                    renderer.localBounds.size.z
                        ? renderer.transform.right
                        : renderer.transform.forward;

                direction =
                    Vector3.ProjectOnPlane(
                        direction,
                        Vector3.up);

                if (direction.sqrMagnitude < 0.01f)
                    direction =
                        Vector3.forward;

                direction.Normalize();

                roadPoints.Add(
                    center);

                roadAnchors.Add(
                    new RoadAnchor
                    {
                        Position = center,
                        Direction = direction
                    });

                float sampleDistance =
                    Mathf.Clamp(
                        Mathf.Max(
                            bounds.extents.x,
                            bounds.extents.z) * 0.55f,
                        4f,
                        28f);

                roadPoints.Add(
                    center +
                    direction *
                    sampleDistance);

                roadPoints.Add(
                    center -
                    direction *
                    sampleDistance);
            }

            if (roadPoints.Count > 0)
                return;

            foreach (Collider collider in
                     city.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null)
                    continue;

                string hierarchyName =
                    BuildHierarchyName(
                        collider.transform);

                if (!hierarchyName.Contains("road") &&
                    !hierarchyName.Contains("street") &&
                    !hierarchyName.Contains("highway"))
                    continue;

                Vector3 center =
                    collider.bounds.center;

                center.y = 0f;

                roadPoints.Add(
                    center);
            }
        }

        private static void AddBuildingColliders(
            GameObject city)
        {
            Renderer[] renderers =
                city.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                string hierarchyName =
                    BuildHierarchyName(
                        renderer.transform);

                bool buildingLike =
                    hierarchyName.Contains("building") ||
                    hierarchyName.Contains("house") ||
                    hierarchyName.Contains("shop");

                if (!buildingLike)
                    continue;

                if (renderer.GetComponent<Collider>() != null)
                    continue;

                BoxCollider collider =
                    renderer.gameObject
                        .AddComponent<BoxCollider>();

                collider.center =
                    renderer.localBounds.center;

                collider.size =
                    renderer.localBounds.size;
            }
        }

        private static string BuildHierarchyName(
            Transform transform)
        {
            string value =
                string.Empty;

            Transform current =
                transform;

            for (int i = 0;
                 current != null && i < 5;
                 i++)
            {
                value +=
                    " " +
                    current.name.ToLowerInvariant();

                current =
                    current.parent;
            }

            return value;
        }

        private static void CreateGroundCollider(
            GameObject city)
        {
            GameObject ground =
                new("City Ground Physics");

            Renderer[] renderers =
                city.GetComponentsInChildren<Renderer>(true);

            Bounds bounds =
                renderers.Length > 0
                    ? renderers[0].bounds
                    : new Bounds(
                        Vector3.zero,
                        new Vector3(
                            1200f,
                            1f,
                            1200f));

            for (int i = 1;
                 i < renderers.Length;
                 i++)
            {
                bounds.Encapsulate(
                    renderers[i].bounds);
            }

            ground.transform.position =
                new Vector3(
                    bounds.center.x,
                    bounds.min.y - 0.22f,
                    bounds.center.z);

            BoxCollider collider =
                ground.AddComponent<BoxCollider>();

            collider.size =
                new Vector3(
                    Mathf.Max(
                        200f,
                        bounds.size.x + 40f),
                    0.4f,
                    Mathf.Max(
                        200f,
                        bounds.size.z + 40f));
        }
    }
}
