using UnityEngine;

namespace MotorCity.World
{
    public static class CityAssetRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/CityVisual";

        private const float RoadY =
            0.28f;

        // These coordinates are taken from the actual CubexCube demo scene.
        // They intentionally reference specific road modules instead of
        // calculating positions from renderer bounds.
        private static readonly Vector3[] deliveryRoute =
        {
            new(17.779f, RoadY, 20.200f),
            new(43.779f, RoadY, 20.200f),
            new(69.779f, RoadY, 33.200f),
            new(69.779f, RoadY, 72.200f),
            new(56.779f, RoadY, 85.200f),
            new(17.779f, RoadY, 85.200f)
        };

        private static readonly Vector3[] sprintRoute =
        {
            new(4.779f, RoadY, 85.200f),
            new(43.779f, RoadY, 85.200f),
            new(69.779f, RoadY, 85.200f),
            new(69.779f, RoadY, 72.200f),
            new(69.779f, RoadY, 46.200f),
            new(69.779f, RoadY, 20.200f),
            new(43.779f, RoadY, 20.200f),
            new(17.779f, RoadY, 20.200f)
        };

        public static Vector3 PlayerSpawnPoint { get; private set; } =
            new(69.779f, RoadY, 33.200f);

        public static Quaternion PlayerSpawnRotation { get; private set; } =
            Quaternion.identity;

        // Road_Grass_Porch next to the western apartment building.
        // The point is offset to the open side of the porch/driveway rather
        // than sitting in the middle of an asphalt lane.
        public static Vector3 GaragePoint { get; private set; } =
            new(22.300f, RoadY, 45.450f);

        // Road_Turn_90_1 at the north-east corner of the main connected grid.
        // Four drift cones fit within the 13 x 13 m road tile.
        public static Vector3 DriftChallengePoint { get; private set; } =
            new(69.779f, RoadY, 85.200f);

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
                "Motor City — CubexCube Free City";

            EnsureFallbackColliders(
                city);

            PlayerSpawnPoint =
                ResolveSurfaceHeight(
                    new Vector3(
                        69.779f,
                        RoadY,
                        33.200f),
                    "Roads");

            PlayerSpawnRotation =
                Quaternion.LookRotation(
                    Vector3.forward,
                    Vector3.up);

            GaragePoint =
                ResolveSurfaceHeight(
                    new Vector3(
                        22.300f,
                        RoadY,
                        45.450f),
                    "Roads_Grass");

            DriftChallengePoint =
                ResolveSurfaceHeight(
                    new Vector3(
                        69.779f,
                        RoadY,
                        85.200f),
                    "Roads");

            Debug.Log(
                "Motor City: CubexCube gameplay layout loaded from fixed demo-scene road coordinates. " +
                $"Spawn={PlayerSpawnPoint}, Garage={GaragePoint}, Drift={DriftChallengePoint}.");

            return true;
        }

        public static Vector3 SnapToNearestRoad(
            Vector3 approximate)
        {
            Vector3 best =
                deliveryRoute[0];

            float bestDistance =
                HorizontalSqrDistance(
                    approximate,
                    best);

            foreach (Vector3 point in deliveryRoute)
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

            foreach (Vector3 point in sprintRoute)
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

            return best;
        }

        private static Vector3 ResolveSurfaceHeight(
            Vector3 point,
            string requiredRoot)
        {
            Physics.SyncTransforms();

            RaycastHit[] hits =
                Physics.RaycastAll(
                    new Vector3(
                        point.x,
                        20f,
                        point.z),
                    Vector3.down,
                    40f,
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

        private static void EnsureFallbackColliders(
            GameObject city)
        {
            AddMeshCollidersUnder(
                FindTransform(
                    city.transform,
                    "Roads"));

            AddMeshCollidersUnder(
                FindTransform(
                    city.transform,
                    "Roads_Grass"));

            AddMeshCollidersUnder(
                FindTransform(
                    city.transform,
                    "Roads_Sand"));

            Transform buildings =
                FindTransform(
                    city.transform,
                    "Buildings");

            if (buildings == null)
                return;

            foreach (Renderer renderer in
                     buildings.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null ||
                    renderer.GetComponent<Collider>() != null)
                    continue;

                BoxCollider collider =
                    renderer.gameObject.AddComponent<BoxCollider>();

                collider.center =
                    renderer.localBounds.center;

                collider.size =
                    renderer.localBounds.size;
            }
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
