using UnityEngine;

namespace MotorCity.World
{
    public static class CityAssetRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/CityVisual";

        private const string RoadRootName =
            "- roads";

        private const string TerrainRootName =
            "- terrain";

        private const float DefaultSurfaceY =
            0.12f;

        private static readonly Vector3[] DeliveryRouteRaw =
        {
            new(-295f, DefaultSurfaceY, 155f),
            new(-300f, DefaultSurfaceY, 255f),
            new(-245f, DefaultSurfaceY, 255f),
            new(-180f, DefaultSurfaceY, 255f),
            new(-180f, DefaultSurfaceY, 185f),
            new(-125f, DefaultSurfaceY, 185f),
            new(-70f, DefaultSurfaceY, 185f),
            new(-70f, DefaultSurfaceY, 110f)
        };

        private static readonly Vector3[] SprintRouteRaw =
        {
            new(-300f, DefaultSurfaceY, 110f),
            new(-300f, DefaultSurfaceY, 185f),
            new(-300f, DefaultSurfaceY, 255f),
            new(-300f, DefaultSurfaceY, 337.5f),
            new(-180f, DefaultSurfaceY, 337.5f),
            new(-180f, DefaultSurfaceY, 255f),
            new(-70f, DefaultSurfaceY, 255f),
            new(-70f, DefaultSurfaceY, 185f),
            new(-70f, DefaultSurfaceY, 110f),
            new(-180f, DefaultSurfaceY, 110f)
        };

        private static Vector3[] deliveryRoute =
            (Vector3[])DeliveryRouteRaw.Clone();

        private static Vector3[] sprintRoute =
            (Vector3[])SprintRouteRaw.Clone();

        public static Vector3 PlayerSpawnPoint { get; private set; } =
            new(-175f, DefaultSurfaceY, 145f);

        public static Quaternion PlayerSpawnRotation { get; private set; } =
            Quaternion.identity;

        // Open paved/sidewalk tile immediately west of the north road.
        // This keeps the garage off the driving lane while still reachable
        // from the street.
        public static Vector3 GaragePoint { get; private set; } =
            new(-197.5f, DefaultSurfaceY, 322.5f);

        // Large four-way intersection from mcp_roads_cross_02.
        public static Vector3 DriftChallengePoint { get; private set; } =
            new(-180f, DefaultSurfaceY, 110f);

        public static Vector3[] DeliveryRoute =>
            (Vector3[])deliveryRoute.Clone();

        public static Vector3[] SprintRoute =>
            (Vector3[])sprintRoute.Clone();

        public static bool TryInstall()
        {
            GameObject prefab =
                Resources.Load<GameObject>(
                    ResourcePath);

            if (prefab == null)
                return false;

            GameObject city =
                Object.Instantiate(prefab);

            city.name =
                "Motor City — Modern City Pack";

            EnsureFallbackRoadColliders(
                city);

            Physics.SyncTransforms();

            PlayerSpawnPoint =
                ResolveSurfaceHeight(
                    new Vector3(
                        -175f,
                        DefaultSurfaceY,
                        145f),
                    RoadRootName);

            PlayerSpawnRotation =
                Quaternion.LookRotation(
                    Vector3.forward,
                    Vector3.up);

            GaragePoint =
                ResolveSurfaceHeight(
                    new Vector3(
                        -197.5f,
                        DefaultSurfaceY,
                        322.5f),
                    TerrainRootName);

            DriftChallengePoint =
                ResolveSurfaceHeight(
                    new Vector3(
                        -180f,
                        DefaultSurfaceY,
                        110f),
                    RoadRootName);

            deliveryRoute =
                ResolveRoute(
                    DeliveryRouteRaw,
                    RoadRootName);

            sprintRoute =
                ResolveRoute(
                    SprintRouteRaw,
                    RoadRootName);

            Debug.Log(
                "Motor City: Modern City Pack gameplay layout loaded from " +
                "fixed mcp_day road-grid coordinates. " +
                $"Spawn={PlayerSpawnPoint}, " +
                $"Garage={GaragePoint}, " +
                $"Drift={DriftChallengePoint}.");

            return true;
        }

        public static Vector3 SnapToNearestRoad(
            Vector3 approximate)
        {
            Vector3 best =
                PlayerSpawnPoint;

            float bestDistance =
                HorizontalSqrDistance(
                    approximate,
                    best);

            ConsiderRoute(
                deliveryRoute,
                approximate,
                ref best,
                ref bestDistance);

            ConsiderRoute(
                sprintRoute,
                approximate,
                ref best,
                ref bestDistance);

            return best;
        }

        private static Vector3[] ResolveRoute(
            Vector3[] source,
            string requiredRoot)
        {
            Vector3[] result =
                new Vector3[source.Length];

            for (int i = 0;
                 i < source.Length;
                 i++)
            {
                result[i] =
                    ResolveSurfaceHeight(
                        source[i],
                        requiredRoot);
            }

            return result;
        }

        private static Vector3 ResolveSurfaceHeight(
            Vector3 point,
            string requiredRoot)
        {
            RaycastHit[] hits =
                Physics.RaycastAll(
                    new Vector3(
                        point.x,
                        40f,
                        point.z),
                    Vector3.down,
                    80f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

            float bestY =
                point.y;

            bool found =
                false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (!IsUnderNamedRoot(
                        hit.collider.transform,
                        requiredRoot))
                    continue;

                if (!found ||
                    hit.point.y > bestY)
                {
                    bestY =
                        hit.point.y;

                    found =
                        true;
                }
            }

            point.y =
                found
                    ? bestY + 0.04f
                    : point.y;

            if (!found)
            {
                Debug.LogWarning(
                    $"Motor City: no '{requiredRoot}' surface found below " +
                    $"{point.x:0.##}, {point.z:0.##}; using fallback Y={point.y:0.##}.");
            }

            return point;
        }

        private static bool IsUnderNamedRoot(
            Transform transform,
            string requiredRoot)
        {
            Transform current =
                transform;

            while (current != null)
            {
                if (string.Equals(
                        current.name,
                        requiredRoot,
                        System.StringComparison.OrdinalIgnoreCase))
                    return true;

                current =
                    current.parent;
            }

            return false;
        }

        private static void EnsureFallbackRoadColliders(
            GameObject city)
        {
            AddMeshCollidersUnder(
                FindTransform(
                    city.transform,
                    RoadRootName));

            AddMeshCollidersUnder(
                FindTransform(
                    city.transform,
                    TerrainRootName));
        }

        private static void AddMeshCollidersUnder(
            Transform root)
        {
            if (root == null)
                return;

            foreach (MeshFilter filter in
                     root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null ||
                    filter.sharedMesh == null ||
                    filter.GetComponent<Collider>() != null)
                    continue;

                MeshCollider collider =
                    filter.gameObject.AddComponent<MeshCollider>();

                collider.sharedMesh =
                    filter.sharedMesh;

                collider.convex =
                    false;
            }
        }

        private static Transform FindTransform(
            Transform root,
            string name)
        {
            if (root == null)
                return null;

            if (string.Equals(
                    root.name,
                    name,
                    System.StringComparison.OrdinalIgnoreCase))
                return root;

            foreach (Transform child in root)
            {
                Transform match =
                    FindTransform(
                        child,
                        name);

                if (match != null)
                    return match;
            }

            return null;
        }

        private static void ConsiderRoute(
            Vector3[] route,
            Vector3 approximate,
            ref Vector3 best,
            ref float bestDistance)
        {
            if (route == null)
                return;

            foreach (Vector3 point in route)
            {
                float distance =
                    HorizontalSqrDistance(
                        approximate,
                        point);

                if (distance >= bestDistance)
                    continue;

                bestDistance =
                    distance;

                best =
                    point;
            }
        }

        private static float HorizontalSqrDistance(
            Vector3 a,
            Vector3 b)
        {
            float x =
                a.x - b.x;

            float z =
                a.z - b.z;

            return
                x * x +
                z * z;
        }
    }
}
