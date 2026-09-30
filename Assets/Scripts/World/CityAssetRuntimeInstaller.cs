using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MotorCity.World
{
    public static class CityAssetRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/CityVisual";

        private const string GarageResourcePath =
            "MotorCity/Garage/SimpleGarage";

        private const string RuntimeGarageName =
            "MotorCity_SimpleGarage";

        private const string SceneCityName =
            "City-Maker";

        private const string RuntimeCityName =
            "MotorCity_FCGCity";

        private const float MarkerLift =
            0.05f;

        // These targets follow the actual generated FCG grid from the
        // 2026-09-18 city report. Every road target is validated against
        // the exact MeshCollider triangle/submesh using the FCG_Roads material.
        private static readonly Vector3[] DeliveryPreferred =
        {
            // Large-district delivery loop from the 2026-09-18 17:09 FCG
            // workbench generation. Targets intentionally follow the visible
            // city grid from the report/screenshot; each is then snapped to
            // the exact FCG_Roads triangle.
            new(-450f, 0f, -100f),
            new(-150f, 0f, -100f),
            new(150f, 0f, -100f),
            new(450f, 0f, -100f),
            new(450f, 0f, 150f),
            new(450f, 0f, 360f),
            new(150f, 0f, 450f),
            new(-150f, 0f, 450f),
            new(-450f, 0f, 360f),
            new(-450f, 0f, 150f),
            new(-150f, 0f, 150f),
            new(150f, 0f, 150f)
        };

        private static readonly Vector3[] SprintPreferred =
        {
            // Street sprint stays entirely inside the verified main district.
            // The previous route continued onto an old remote highway section
            // that is outside the current playable city.
            new(450f, 0f, 150f),
            new(450f, 0f, -100f),
            new(150f, 0f, -100f),
            new(-150f, 0f, -100f),
            new(-450f, 0f, -100f),
            new(-450f, 0f, 150f),
            new(-450f, 0f, 360f),
            new(-150f, 0f, 450f),
            new(150f, 0f, 450f),
            new(450f, 0f, 430f),
            new(450f, 0f, 150f),
            new(150f, 0f, 150f),
            new(-150f, 0f, 150f),
            new(-450f, 0f, 150f),
            new(-150f, 0f, -100f)
        };

        private static readonly Vector3[] CircuitPreferred =
        {
            // Two-lap loop around the large district. As with the other
            // activities, every target is snapped to an exact FCG road triangle.
            new(150f, 0f, -100f),
            new(450f, 0f, -100f),
            new(450f, 0f, 150f),
            new(450f, 0f, 430f),
            new(150f, 0f, 450f),
            new(-150f, 0f, 450f),
            new(-450f, 0f, 360f),
            new(-450f, 0f, 150f),
            new(-450f, 0f, -100f),
            new(-150f, 0f, -100f)
        };

        private static Vector3[] deliveryRoute =
            (Vector3[])DeliveryPreferred.Clone();

        private static Vector3[] sprintRoute =
            (Vector3[])SprintPreferred.Clone();

        private static Vector3[] circuitRoute =
            (Vector3[])CircuitPreferred.Clone();

        private static readonly Vector3[] UndergroundPreferred =
        {
            // Underground must stay inside the verified large-district road
            // network. These points are shared with the working delivery /
            // circuit area and are therefore guaranteed to be on-map for the
            // current authored city.
            new(-450f, 0f, 360f),
            new(-450f, 0f, 150f),
            new(-450f, 0f, -100f),
            new(-150f, 0f, -100f),
            new(150f, 0f, -100f),
            new(450f, 0f, -100f),
            new(450f, 0f, 150f),
            new(450f, 0f, 360f),
            new(150f, 0f, 450f),
            new(-150f, 0f, 450f)
        };

        private static Vector3[] undergroundRoute =
            (Vector3[])UndergroundPreferred.Clone();

        private static GameObject activeCity;
        private static GameObject activeGarageInterior;
        private static Bounds cityBounds;
        private static bool hasCityBounds;

        public static Vector3 PlayerSpawnPoint { get; private set; } =
            new(-570f, 0.25f, 505.109f);

        public static Quaternion PlayerSpawnRotation { get; private set; } =
            Quaternion.Euler(
                0f,
                90f,
                0f);

        // Dedicated player garage on the authored parking apron.
        public static Vector3 GaragePoint { get; private set; } =
            new(-585.822f, 0.2f, 505.109f);

        public static Quaternion GarageSpawnRotation { get; private set; } =
            Quaternion.Euler(
                7.40436444e-05f,
                89.9998322f,
                -4.8625111e-06f);

        public static Vector3 GarageInteriorPosition { get; } =
            new(
                -552.546936f,
                1.375f,
                -798.200012f);

        public static Quaternion GarageInteriorRotation { get; } =
            Quaternion.Euler(
                0f,
                0f,
                0f);

        public static Vector3 GarageInteriorScale { get; } =
            new(
                2f,
                2f,
                2f);

        public static Vector3 GarageVehiclePosition { get; } =
            new(
                -552.302002f,
                1.5f,
                -796.929993f);

        public static Quaternion GarageVehicleRotation { get; } =
            Quaternion.Euler(
                0f,
                0f,
                0f);

        // Western broad junction in the large district, kept separate from
        // the street sprint start on the eastern side.
        public static Vector3 DriftChallengePoint { get; private set; } =
            new(-450f, 0.2f, 150f);

        public static Vector3[] DeliveryRoute =>
            (Vector3[])deliveryRoute.Clone();

        public static Vector3[] SprintRoute =>
            (Vector3[])sprintRoute.Clone();

        public static Vector3[] CircuitRoute =>
            (Vector3[])circuitRoute.Clone();

        public static Vector3 UndergroundMeetingPoint =>
            undergroundRoute != null &&
            undergroundRoute.Length > 0
                ? undergroundRoute[0]
                : PlayerSpawnPoint;

        public static Vector3[] UndergroundRoute =>
            (Vector3[])undergroundRoute.Clone();

        public static bool TryInstall()
        {
            activeCity =
                FindExistingCity();

            if (activeCity == null)
            {
                GameObject prefab =
                    Resources.Load<GameObject>(
                        ResourcePath);

                if (prefab == null)
                    return false;

                activeCity =
                    UnityEngine.Object.Instantiate(
                        prefab);

                activeCity.name =
                    RuntimeCityName;
            }

            // Runtime treats the authored city as read-only.
            // Colliders, props, parked vehicles, traffic signals and all
            // other map objects must come exactly from CityVisual.prefab.
            cityBounds =
                CalculateCityBounds(
                    activeCity);

            hasCityBounds =
                cityBounds.size.x > 10f &&
                cityBounds.size.z > 10f;

            InstallGarageInterior();

            Physics.SyncTransforms();

            ResolveGameplayLayout();

            return true;
        }

        private static void InstallGarageInterior()
        {
            if (activeGarageInterior == null)
            {
                activeGarageInterior =
                    GameObject.Find(
                        RuntimeGarageName);
            }

            if (activeGarageInterior == null)
            {
                GameObject prefab =
                    Resources.Load<GameObject>(
                        GarageResourcePath);

                if (prefab == null)
                {
                    Debug.LogWarning(
                        "Motor City: Simple Garage runtime prefab was not found.");
                    return;
                }

                activeGarageInterior =
                    UnityEngine.Object.Instantiate(
                        prefab);

                activeGarageInterior.name =
                    RuntimeGarageName;
            }

            Transform garageTransform =
                activeGarageInterior.transform;

            garageTransform.position =
                GarageInteriorPosition;

            garageTransform.rotation =
                GarageInteriorRotation;

            garageTransform.localScale =
                GarageInteriorScale;

            Transform floor =
                FindChildByName(
                    garageTransform,
                    "Floor");

            if (floor == null)
            {
                Debug.LogWarning(
                    "Motor City: Simple Garage Floor was not found.");
                return;
            }

            BoxCollider floorCollider =
                floor.GetComponent<BoxCollider>();

            if (floorCollider == null)
            {
                floorCollider =
                    floor.gameObject.AddComponent<BoxCollider>();
            }

            MeshFilter floorMesh =
                floor.GetComponent<MeshFilter>();

            if (floorMesh != null &&
                floorMesh.sharedMesh != null)
            {
                Bounds bounds =
                    floorMesh.sharedMesh.bounds;

                Vector3 colliderSize =
                    bounds.size;

                colliderSize.y =
                    Mathf.Max(
                        0.25f,
                        colliderSize.y);

                floorCollider.center =
                    bounds.center;

                floorCollider.size =
                    colliderSize;
            }

            floorCollider.isTrigger =
                false;
        }

        private static Transform FindChildByName(
            Transform root,
            string childName)
        {
            if (root == null)
                return null;

            if (root.name ==
                childName)
            {
                return root;
            }

            for (int i = 0;
                 i < root.childCount;
                 i++)
            {
                Transform found =
                    FindChildByName(
                        root.GetChild(i),
                        childName);

                if (found != null)
                    return found;
            }

            return null;
        }

        public static Vector3 SnapToNearestRoad(
            Vector3 approximate)
        {
            if (activeCity == null)
                return approximate;

            return FindRoadPointNear(
                approximate,
                42f,
                false);
        }

        public static void ResolveNearestRoadResetPose(
            Vector3 approximate,
            Vector3 preferredForward,
            out Vector3 position,
            out Quaternion rotation)
        {
            Vector3 roadPoint =
                PlayerSpawnPoint;

            bool foundRoad =
                activeCity != null &&
                TryFindNearestRoadForReset(
                    approximate,
                    out roadPoint);

            if (!foundRoad)
            {
                roadPoint =
                    PlayerSpawnPoint;

                if (activeCity != null &&
                    TryGetRoadHit(
                        PlayerSpawnPoint.x,
                        PlayerSpawnPoint.z,
                        out Vector3 spawnRoad))
                {
                    roadPoint =
                        spawnRoad;
                }
            }

            // Use the actual road height only. Never derive rescue height
            // from the current vehicle Y; otherwise repeated resets can climb.
            position =
                new Vector3(
                    roadPoint.x,
                    roadPoint.y + 1.15f,
                    roadPoint.z);

            Vector3 roadDirection =
                activeCity != null
                    ? EstimateRoadDirection(
                        roadPoint)
                    : preferredForward;

            roadDirection.y = 0f;

            if (roadDirection.sqrMagnitude <
                0.001f)
            {
                roadDirection =
                    Vector3.forward;
            }

            roadDirection.Normalize();

            preferredForward.y = 0f;

            if (preferredForward.sqrMagnitude >
                    0.001f &&
                Vector3.Dot(
                    roadDirection,
                    preferredForward.normalized) <
                0f)
            {
                roadDirection =
                    -roadDirection;
            }

            rotation =
                Quaternion.LookRotation(
                    roadDirection,
                    Vector3.up);
        }

        private static bool TryFindNearestRoadForReset(
            Vector3 approximate,
            out Vector3 roadPoint)
        {
            roadPoint =
                default;
            if (TryGetRoadHit(
                    approximate.x,
                    approximate.z,
                    out roadPoint))
            {
                return true;
            }

            // City roads are laid out on broad FCG blocks. Searching by
            // expanding square rings gives us the nearest valid road in X/Z
            // without ever using the car's current height as a fallback.
            const float step =
                4f;

            const float maximumRadius =
                220f;

            float bestDistanceSquared =
                float.PositiveInfinity;

            bool found =
                false;

            int rings =
                Mathf.CeilToInt(
                    maximumRadius /
                    step);

            for (int ring = 1;
                 ring <= rings;
                 ring++)
            {
                float radius =
                    ring *
                    step;

                for (float offset = -radius;
                     offset <= radius;
                     offset += step)
                {
                    TestResetRoadCandidate(
                        approximate,
                        approximate.x + offset,
                        approximate.z - radius,
                        ref found,
                        ref roadPoint,
                        ref bestDistanceSquared);

                    TestResetRoadCandidate(
                        approximate,
                        approximate.x + offset,
                        approximate.z + radius,
                        ref found,
                        ref roadPoint,
                        ref bestDistanceSquared);

                    TestResetRoadCandidate(
                        approximate,
                        approximate.x - radius,
                        approximate.z + offset,
                        ref found,
                        ref roadPoint,
                        ref bestDistanceSquared);

                    TestResetRoadCandidate(
                        approximate,
                        approximate.x + radius,
                        approximate.z + offset,
                        ref found,
                        ref roadPoint,
                        ref bestDistanceSquared);
                }

                if (found &&
                    bestDistanceSquared <=
                    radius * radius)
                {
                    break;
                }
            }

            return found;
        }

        private static void TestResetRoadCandidate(
            Vector3 approximate,
            float x,
            float z,
            ref bool found,
            ref Vector3 best,
            ref float bestDistanceSquared)
        {
            if (hasCityBounds &&
                (x < cityBounds.min.x ||
                 x > cityBounds.max.x ||
                 z < cityBounds.min.z ||
                 z > cityBounds.max.z))
            {
                return;
            }

            if (!TryGetRoadHit(
                    x,
                    z,
                    out Vector3 hit))
            {
                return;
            }

            float dx =
                hit.x -
                approximate.x;

            float dz =
                hit.z -
                approximate.z;

            float distanceSquared =
                dx * dx +
                dz * dz;

            if (found &&
                distanceSquared >=
                bestDistanceSquared)
            {
                return;
            }

            found =
                true;

            best =
                hit;

            bestDistanceSquared =
                distanceSquared;
        }

        private static void ResolveGameplayLayout()
        {
            GaragePoint =
                new Vector3(
                    -585.822f,
                    0.2f,
                    505.109f);

            GarageSpawnRotation =
                Quaternion.Euler(
                    7.40436444e-05f,
                    89.9998322f,
                    -4.8625111e-06f);

            PlayerSpawnPoint =
                new Vector3(
                    -570f,
                    0.25f,
                    505.109f);

            PlayerSpawnRotation =
                Quaternion.Euler(
                    0f,
                    90f,
                    0f);

            DriftChallengePoint =
                FindRoadPointNear(
                    new Vector3(
                        -450f,
                        0f,
                        150f),
                    75f,
                    true);

            deliveryRoute =
                ResolveRoadRoute(
                    DeliveryPreferred);

            sprintRoute =
                ResolveRoadRoute(
                    SprintPreferred);

            circuitRoute =
                ResolveRoadRoute(
                    CircuitPreferred);

            undergroundRoute =
                ResolveRoadRoute(
                    UndergroundPreferred);
        }

        private static Vector3[] ResolveRoadRoute(
            Vector3[] preferred)
        {
            Vector3[] result =
                new Vector3[preferred.Length];

            for (int i = 0;
                 i < preferred.Length;
                 i++)
            {
                result[i] =
                    FindRoadPointNear(
                        preferred[i],
                        80f,
                        false);
            }

            return result;
        }

        private static Vector3 FindRoadPointNear(
            Vector3 preferred,
            float searchRadius,
            bool preferWideRoad)
        {
            if (TryGetRoadHit(
                    preferred.x,
                    preferred.z,
                    out Vector3 exact))
            {
                if (!preferWideRoad ||
                    RoadOpenness(
                        exact,
                        13f) >= 4)
                {
                    exact.y +=
                        MarkerLift;

                    return exact;
                }
            }

            const float step =
                5f;

            Vector3 best =
                preferred;

            float bestScore =
                float.PositiveInfinity;

            bool found =
                false;

            int rings =
                Mathf.CeilToInt(
                    searchRadius /
                    step);

            for (int ring = 1;
                 ring <= rings;
                 ring++)
            {
                float radius =
                    ring *
                    step;

                for (float offset = -radius;
                     offset <= radius;
                     offset += step)
                {
                    TestRoadCandidate(
                        preferred,
                        preferred.x + offset,
                        preferred.z - radius,
                        preferWideRoad,
                        ref found,
                        ref best,
                        ref bestScore);

                    TestRoadCandidate(
                        preferred,
                        preferred.x + offset,
                        preferred.z + radius,
                        preferWideRoad,
                        ref found,
                        ref best,
                        ref bestScore);

                    TestRoadCandidate(
                        preferred,
                        preferred.x - radius,
                        preferred.z + offset,
                        preferWideRoad,
                        ref found,
                        ref best,
                        ref bestScore);

                    TestRoadCandidate(
                        preferred,
                        preferred.x + radius,
                        preferred.z + offset,
                        preferWideRoad,
                        ref found,
                        ref best,
                        ref bestScore);
                }

                if (found &&
                    bestScore <=
                    radius * radius)
                    break;
            }

            if (!found)
            {
                Vector3 safeFallback =
                    PlayerSpawnPoint;

                if (TryGetRoadHit(
                        PlayerSpawnPoint.x,
                        PlayerSpawnPoint.z,
                        out Vector3 spawnRoad))
                {
                    safeFallback =
                        spawnRoad;

                    safeFallback.y +=
                        MarkerLift;
                }

                return safeFallback;
            }

            best.y +=
                MarkerLift;

            return best;
        }

        private static void TestRoadCandidate(
            Vector3 preferred,
            float x,
            float z,
            bool preferWideRoad,
            ref bool found,
            ref Vector3 best,
            ref float bestScore)
        {
            if (hasCityBounds &&
                (x < cityBounds.min.x ||
                 x > cityBounds.max.x ||
                 z < cityBounds.min.z ||
                 z > cityBounds.max.z))
                return;

            if (!TryGetRoadHit(
                    x,
                    z,
                    out Vector3 hit))
                return;

            float dx =
                hit.x -
                preferred.x;

            float dz =
                hit.z -
                preferred.z;

            float score =
                dx * dx +
                dz * dz;

            if (preferWideRoad)
            {
                int openness =
                    RoadOpenness(
                        hit,
                        13f);

                score -=
                    openness *
                    140f;
            }

            if (found &&
                score >= bestScore)
                return;

            found =
                true;

            best =
                hit;

            bestScore =
                score;
        }

        private static int RoadOpenness(
            Vector3 point,
            float radius)
        {
            int count =
                0;

            Vector2[] offsets =
            {
                new(radius, 0f),
                new(-radius, 0f),
                new(0f, radius),
                new(0f, -radius),
                new(radius * 0.7f, radius * 0.7f),
                new(-radius * 0.7f, radius * 0.7f),
                new(radius * 0.7f, -radius * 0.7f),
                new(-radius * 0.7f, -radius * 0.7f)
            };

            foreach (Vector2 offset in offsets)
            {
                if (TryGetRoadHit(
                        point.x + offset.x,
                        point.z + offset.y,
                        out _))
                {
                    count++;
                }
            }

            return count;
        }

        private static Vector3 FindParkingPointNear(
            Vector3 preferred,
            float searchRadius)
        {
            if (TryGetParkingHit(
                    preferred.x,
                    preferred.z,
                    out Vector3 exact))
            {
                exact.y +=
                    MarkerLift;

                return exact;
            }

            const float step =
                2.5f;

            float bestDistance =
                float.PositiveInfinity;

            Vector3 best =
                preferred;

            bool found =
                false;

            for (float x = -searchRadius;
                 x <= searchRadius;
                 x += step)
            {
                for (float z = -searchRadius;
                     z <= searchRadius;
                     z += step)
                {
                    if (!TryGetParkingHit(
                            preferred.x + x,
                            preferred.z + z,
                            out Vector3 hit))
                        continue;

                    float distance =
                        x * x +
                        z * z;

                    if (distance >= bestDistance)
                        continue;

                    bestDistance =
                        distance;

                    best =
                        hit;

                    found =
                        true;
                }
            }

            if (!found)
            {
                Vector3 roadFallback =
                    FindRoadPointNear(
                        preferred,
                        Mathf.Max(
                            60f,
                            searchRadius),
                        false);

                return roadFallback;
            }

            best.y +=
                MarkerLift;

            return best;
        }

        private static bool TryGetRoadHit(
            float x,
            float z,
            out Vector3 point)
        {
            point =
                default;

            RaycastHit[] hits =
                Physics.RaycastAll(
                    new Vector3(
                        x,
                        RayStartY(),
                        z),
                    Vector3.down,
                    RayDistance(),
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

            if (hits == null ||
                hits.Length == 0)
                return false;

            Array.Sort(
                hits,
                (a, b) =>
                    a.distance.CompareTo(
                        b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (!IsRoadSurface(
                        hit))
                    continue;

                point =
                    hit.point;

                return true;
            }

            return false;
        }

        private static bool TryGetParkingHit(
            float x,
            float z,
            out Vector3 point)
        {
            point =
                default;

            RaycastHit[] hits =
                Physics.RaycastAll(
                    new Vector3(
                        x,
                        RayStartY(),
                        z),
                    Vector3.down,
                    RayDistance(),
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

            if (hits == null ||
                hits.Length == 0)
                return false;

            Array.Sort(
                hits,
                (a, b) =>
                    a.distance.CompareTo(
                        b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                string path =
                    GetHierarchyPath(
                            hit.collider.transform)
                        .ToLowerInvariant();

                if (!path.Contains("park-04") &&
                    !path.Contains("park-05") &&
                    !path.Contains("park-06") &&
                    !path.Contains("park-08"))
                    continue;

                if (hit.collider is MeshCollider meshCollider)
                {
                    Renderer renderer =
                        hit.collider.GetComponent<Renderer>();

                    Material material =
                        ResolveTriangleMaterial(
                            hit,
                            renderer,
                            meshCollider);

                    if (material != null &&
                        material.name.IndexOf(
                            "grass",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                }

                point =
                    hit.point;

                return true;
            }

            return false;
        }

        private static bool IsRoadSurface(
            RaycastHit hit)
        {
            if (hit.collider == null)
                return false;

            string path =
                GetHierarchyPath(
                        hit.collider.transform)
                    .ToLowerInvariant();

            if (path.Contains("sidewalk") ||
                path.Contains("/buildings/") ||
                path.Contains("/park-") ||
                path.Contains("/garden") ||
                path.Contains("/objects/") ||
                path.Contains("guardrail") ||
                path.Contains("guard-rail") ||
                path.Contains("grass"))
            {
                return false;
            }

            bool pathLooksLikeRoad =
                path.Contains("road") ||
                path.Contains("street") ||
                path.Contains("highway") ||
                path.Contains("asphalt") ||
                path.Contains("intersection") ||
                path.Contains("crossroad");

            if (IsExactRoadTriangle(
                    hit))
            {
                return true;
            }

            Renderer renderer =
                hit.collider.GetComponent<Renderer>();

            if (renderer == null)
            {
                renderer =
                    hit.collider.GetComponentInParent<Renderer>();
            }

            if (renderer == null)
            {
                renderer =
                    hit.collider.GetComponentInChildren<Renderer>();
            }

            if (renderer != null)
            {
                Material[] materials =
                    renderer.sharedMaterials;

                if (materials != null)
                {
                    foreach (Material material in
                             materials)
                    {
                        if (material == null)
                            continue;

                        string materialName =
                            material.name
                                .ToLowerInvariant();

                        bool materialLooksLikeRoad =
                            (materialName.Contains("road") ||
                             materialName.Contains("highway") ||
                             materialName.Contains("asphalt") ||
                             materialName.Contains("street")) &&
                            !materialName.Contains("grass");

                        if (materialLooksLikeRoad)
                            return true;
                    }
                }
            }

            return pathLooksLikeRoad;
        }

        private static bool IsExactRoadTriangle(
            RaycastHit hit)
        {
            MeshCollider meshCollider =
                hit.collider as MeshCollider;

            if (meshCollider == null ||
                meshCollider.sharedMesh == null)
                return false;

            string path =
                GetHierarchyPath(
                        hit.collider.transform)
                    .ToLowerInvariant();

            if (path.Contains("sidewalk") ||
                path.Contains("/buildings/") ||
                path.Contains("/park-") ||
                path.Contains("/garden") ||
                path.Contains("/objects/") ||
                path.Contains("guardrail") ||
                path.Contains("guard-rail"))
                return false;

            Renderer renderer =
                hit.collider.GetComponent<Renderer>();

            if (renderer == null)
                return false;

            Material material =
                ResolveTriangleMaterial(
                    hit,
                    renderer,
                    meshCollider);

            if (material == null)
                return false;

            string materialName =
                material.name.ToLowerInvariant();

            bool asphalt =
                materialName.Contains("road") &&
                !materialName.Contains("grass");

            bool highway =
                materialName.Contains("highway");

            return
                asphalt ||
                highway;
        }

        private static Material ResolveTriangleMaterial(
            RaycastHit hit,
            Renderer renderer,
            MeshCollider meshCollider)
        {
            if (renderer == null ||
                meshCollider == null ||
                meshCollider.sharedMesh == null)
                return null;

            Material[] materials =
                renderer.sharedMaterials;

            if (materials == null ||
                materials.Length == 0)
                return null;

            int triangleIndex =
                hit.triangleIndex;

            if (triangleIndex < 0)
                return materials[0];

            Mesh mesh =
                meshCollider.sharedMesh;

            int triangleCursor =
                0;

            int subMeshCount =
                Mathf.Min(
                    mesh.subMeshCount,
                    materials.Length);

            for (int subMesh = 0;
                 subMesh < subMeshCount;
                 subMesh++)
            {
                if (mesh.GetTopology(subMesh) !=
                    MeshTopology.Triangles)
                    continue;

                int triangleCount =
                    (int)mesh.GetIndexCount(
                        subMesh) /
                    3;

                if (triangleIndex >= triangleCursor &&
                    triangleIndex <
                    triangleCursor + triangleCount)
                {
                    return materials[subMesh];
                }

                triangleCursor +=
                    triangleCount;
            }

            return materials[0];
        }

        private static Vector3 EstimateRoadDirection(
            Vector3 point)
        {
            int xScore =
                AxisRoadScore(
                    point,
                    Vector3.right);

            int zScore =
                AxisRoadScore(
                    point,
                    Vector3.forward);

            Vector3 direction =
                zScore >= xScore
                    ? Vector3.forward
                    : Vector3.right;

            Vector3 towardCenter =
                cityBounds.center -
                point;

            towardCenter.y =
                0f;

            if (towardCenter.sqrMagnitude > 1f &&
                Vector3.Dot(
                    direction,
                    towardCenter) < 0f)
            {
                direction =
                    -direction;
            }

            return direction;
        }

        private static int AxisRoadScore(
            Vector3 point,
            Vector3 axis)
        {
            int score =
                0;

            float[] distances =
            {
                6f,
                12f,
                18f
            };

            foreach (float distance in distances)
            {
                Vector3 forward =
                    point +
                    axis *
                    distance;

                Vector3 backward =
                    point -
                    axis *
                    distance;

                if (TryGetRoadHit(
                        forward.x,
                        forward.z,
                        out _))
                    score++;

                if (TryGetRoadHit(
                        backward.x,
                        backward.z,
                        out _))
                    score++;
            }

            return score;
        }

        private static bool HasStaticFourWheelSignature(
            Transform candidate)
        {
            if (candidate == null)
                return false;

            bool bl = false;
            bool br = false;
            bool fl = false;
            bool fr = false;

            foreach (Transform child in
                     candidate)
            {
                if (child == null)
                    continue;

                string name =
                    NormalizeStaticVehicleName(
                        child.name);

                if (IsWheelMarker(
                        name,
                        "bl"))
                {
                    bl = true;
                }
                else if (IsWheelMarker(
                             name,
                             "br"))
                {
                    br = true;
                }
                else if (IsWheelMarker(
                             name,
                             "fl"))
                {
                    fl = true;
                }
                else if (IsWheelMarker(
                             name,
                             "fr"))
                {
                    fr = true;
                }
            }

            return
                bl &&
                br &&
                fl &&
                fr;
        }

        private static bool IsWheelMarker(
            string normalized,
            string marker)
        {
            if (string.IsNullOrEmpty(
                    normalized))
                return false;

            return
                normalized == marker ||
                normalized == "wheel" + marker ||
                normalized == "tire" + marker ||
                normalized.EndsWith(
                    "wheel" + marker) ||
                normalized.EndsWith(
                    "tire" + marker) ||
                normalized.EndsWith(
                    marker);
        }

        private static bool HasRuntimeTrafficBehaviour(
            Transform candidate)
        {
            if (candidate == null)
                return false;

            foreach (Component component in
                     candidate.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                string typeName =
                    component.GetType().Name
                        .ToLowerInvariant();

                if (typeName.Contains(
                        "trafficcar") ||
                    typeName.Contains(
                        "trafficvehicle"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool UsesStaticTrafficTireMaterial(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string normalized =
                    NormalizeStaticVehicleName(
                        material.name);

                if (normalized == "tires" ||
                    normalized.StartsWith(
                        "tires"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool UsesTrafficCarAtlas(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string normalized =
                    NormalizeStaticVehicleName(
                        material.name);

                if (normalized.Contains(
                        "trafficcaratlas"))
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject FindTrafficAtlasVehicleRoot(
            Transform item,
            Transform cityRoot)
        {
            if (item == null)
                return null;

            Transform current =
                item;

            Transform candidate =
                item;

            while (current.parent != null &&
                   current.parent != cityRoot)
            {
                Transform parent =
                    current.parent;

                string parentName =
                    NormalizeStaticVehicleName(
                        parent.name);

                if (parentName == "cars" ||
                    parentName == "vehicles" ||
                    parentName.Contains(
                        "trafficcars") ||
                    parentName.Contains(
                        "parkedcars"))
                {
                    return current.gameObject;
                }

                if (parent.GetComponent<Renderer>() != null &&
                    !UsesTrafficCarAtlas(
                        parent.GetComponent<Renderer>()))
                {
                    break;
                }

                if (parent.GetComponent<Renderer>() != null ||
                    parent.GetComponent<Collider>() != null)
                {
                    candidate =
                        parent;
                }

                current =
                    parent;

                if (current.parent == null ||
                    current.parent == cityRoot)
                    break;

                string currentName =
                    NormalizeStaticVehicleName(
                        current.name);

                if (currentName == "meshes" ||
                    currentName == "buildings" ||
                    currentName == "objects" ||
                    currentName == "environment")
                {
                    break;
                }
            }

            return
                candidate != null
                    ? candidate.gameObject
                    : item.gameObject;
        }

        private static GameObject FindStaticParkedVehicleRoot(
            Transform item,
            Transform cityRoot)
        {
            Transform current =
                item;

            Transform knownVehicleRoot =
                null;

            while (current != null &&
                   current != cityRoot)
            {
                string normalized =
                    NormalizeStaticVehicleName(
                        current.name);

                if (normalized.Contains(
                        "tempra") ||
                    normalized.Contains(
                        "vesta"))
                {
                    knownVehicleRoot =
                        current;
                }

                Transform parent =
                    current.parent;

                if (parent == null)
                    break;

                string parentName =
                    NormalizeStaticVehicleName(
                        parent.name);

                if (parentName == "cars" ||
                    parentName == "vehicles")
                {
                    return current.gameObject;
                }

                current =
                    parent;
            }

            return
                knownVehicleRoot != null
                    ? knownVehicleRoot.gameObject
                    : null;
        }

        private static string NormalizeStaticVehicleName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
                return string.Empty;

            char[] source =
                value
                    .ToLowerInvariant()
                    .ToCharArray();

            var result =
                new System.Text.StringBuilder(
                    source.Length);

            foreach (char character in source)
            {
                if (char.IsLetterOrDigit(
                        character))
                {
                    result.Append(
                        character);
                }
            }

            return
                result.ToString();
        }

        private static bool IsFcgCityRenderer(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string name =
                    material.name;

                if (name.StartsWith(
                        "FCG_",
                        StringComparison.OrdinalIgnoreCase) ||
                    name.IndexOf(
                        "FCG",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject FindExistingCity()
        {
            Scene scene =
                SceneManager.GetActiveScene();

            if (!scene.IsValid() ||
                !scene.isLoaded)
                return null;

            foreach (GameObject root in
                     scene.GetRootGameObjects())
            {
                if (string.Equals(
                        root.name,
                        SceneCityName,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        root.name,
                        RuntimeCityName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return root;
                }
            }

            return null;
        }

        private static Bounds CalculateCityBounds(
            GameObject city)
        {
            Renderer[] allRenderers =
                city.GetComponentsInChildren<Renderer>(
                    true);

            bool hasFcgRenderer =
                false;

            bool boundsInitialized =
                false;

            Bounds bounds =
                default;

            foreach (Renderer renderer in
                     allRenderers)
            {
                if (!IsFcgCityRenderer(
                        renderer))
                {
                    continue;
                }

                if (!boundsInitialized)
                {
                    bounds =
                        renderer.bounds;

                    boundsInitialized =
                        true;
                }
                else
                {
                    bounds.Encapsulate(
                        renderer.bounds);
                }

                hasFcgRenderer =
                    true;
            }

            if (!hasFcgRenderer)
            {
                foreach (Renderer renderer in
                         allRenderers)
                {
                    if (renderer == null)
                        continue;

                    if (!boundsInitialized)
                    {
                        bounds =
                            renderer.bounds;

                        boundsInitialized =
                            true;
                    }
                    else
                    {
                        bounds.Encapsulate(
                            renderer.bounds);
                    }
                }
            }

            if (!boundsInitialized)
            {
                // Safe fallback for the current authored main district.
                // Do not retain the obsolete remote/highway bounds from older
                // city generations, otherwise invalid legacy coordinates can
                // be treated as playable.
                return new Bounds(
                    new Vector3(
                        0f,
                        80f,
                        175f),
                    new Vector3(
                        1200f,
                        170f,
                        900f));
            }

            return
                bounds;
        }

        private static float RayStartY()
        {
            return
                hasCityBounds
                    ? cityBounds.max.y + 40f
                    : 220f;
        }

        private static float RayDistance()
        {
            return
                hasCityBounds
                    ? cityBounds.size.y + 100f
                    : 320f;
        }

        private static string GetHierarchyPath(
            Transform transform)
        {
            string path =
                transform.name;

            Transform current =
                transform.parent;

            while (current != null)
            {
                path =
                    current.name +
                    "/" +
                    path;

                current =
                    current.parent;
            }

            return path;
        }
    }
}
