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
            // Main district -> entrance roundabout -> complete three-piece
            // highway -> perimeter lap of the compact district.
            new(450f, 0f, 150f),
            new(150f, 0f, 100f),
            new(-150f, 0f, 0f),
            new(-300f, 0f, -240f),
            new(-314f, 0f, -420f),
            new(-314f, 0f, -620f),
            new(-300f, 0f, -800f),
            new(-300f, 0f, -1000f),
            new(-300f, 0f, -1200f),
            new(-300f, 0f, -1400f),
            new(-300f, 0f, -1600f),
            new(20f, 0f, -1832f),
            new(-300f, 0f, -2010f),
            new(-630f, 0f, -1832f),
            new(-300f, 0f, -1632f)
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
        private static Bounds cityBounds;
        private static bool hasCityBounds;

        public static Vector3 PlayerSpawnPoint { get; private set; } =
            new(0f, 0.2f, 100f);

        public static Quaternion PlayerSpawnRotation { get; private set; } =
            Quaternion.identity;

        // Park-06 beside the lower central road network in the large district.
        public static Vector3 GaragePoint { get; private set; } =
            new(368.91f, 0.4f, 183.37f);

        // Lower roundabout / broad junction visible in the large district.
        public static Vector3 DriftChallengePoint { get; private set; } =
            new(450f, 0.2f, 150f);

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
                : new Vector3(-630f, 0.2f, -1832f);

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

            Physics.SyncTransforms();

            ResolveGameplayLayout();

            return true;
        }

        public static Vector3 SnapToNearestRoad(
            Vector3 approximate)
        {
            if (activeCity == null)
                return approximate;

            return FindRoadPointNear(
                approximate,
                42f,
                "road snap",
                false);
        }

        public static void ResolveNearestRoadResetPose(
            Vector3 approximate,
            Vector3 preferredForward,
            out Vector3 position,
            out Quaternion rotation)
        {
            Vector3 roadPoint;

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
            PlayerSpawnPoint =
                FindRoadPointNear(
                    new Vector3(
                        0f,
                        0f,
                        100f),
                    90f,
                    "player spawn",
                    false);

            PlayerSpawnRotation =
                Quaternion.LookRotation(
                    EstimateRoadDirection(
                        PlayerSpawnPoint),
                    Vector3.up);

            GaragePoint =
                FindParkingPointNear(
                    new Vector3(
                        368.91f,
                        0f,
                        183.37f),
                    30f);

            DriftChallengePoint =
                FindRoadPointNear(
                    new Vector3(
                        450f,
                        0f,
                        150f),
                    75f,
                    "drift zone",
                    true);

            deliveryRoute =
                ResolveRoadRoute(
                    DeliveryPreferred,
                    "delivery");

            sprintRoute =
                ResolveRoadRoute(
                    SprintPreferred,
                    "sprint");

            circuitRoute =
                ResolveRoadRoute(
                    CircuitPreferred,
                    "circuit");

            undergroundRoute =
                ResolveRoadRoute(
                    UndergroundPreferred,
                    "underground");
        }

        private static Vector3[] ResolveRoadRoute(
            Vector3[] preferred,
            string context)
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
                        context + " " + (i + 1),
                        false);
            }

            return result;
        }

        private static Vector3 FindRoadPointNear(
            Vector3 preferred,
            float searchRadius,
            string context,
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
                best =
                    preferred;

                best.y =
                    Mathf.Max(
                        0.2f,
                        preferred.y);

                return best;
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
                preferred.y =
                    0.4f;

                return preferred;
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
                if (!IsExactRoadTriangle(
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

        private static int RemoveStaticParkedVehicles(
            GameObject city)
        {
            if (city == null)
                return 0;

            Transform cityRoot =
                city.transform;

            var rootsToRemove =
                new HashSet<GameObject>();

            foreach (Transform item in
                     city.GetComponentsInChildren<Transform>(
                         true))
            {
                if (item == null ||
                    item == cityRoot)
                    continue;

                GameObject vehicleRoot =
                    FindStaticParkedVehicleRoot(
                        item,
                        cityRoot);

                if (vehicleRoot != null)
                {
                    rootsToRemove.Add(
                        vehicleRoot);
                }
            }

            foreach (Transform candidate in
                     city.GetComponentsInChildren<Transform>(
                         true))
            {
                if (candidate == null ||
                    candidate == cityRoot)
                    continue;

                if (!HasStaticFourWheelSignature(
                        candidate))
                    continue;

                if (HasRuntimeTrafficBehaviour(
                        candidate))
                    continue;

                rootsToRemove.Add(
                    candidate.gameObject);
            }

            foreach (Renderer renderer in
                     city.GetComponentsInChildren<Renderer>(
                         true))
            {
                if (renderer == null)
                    continue;

                if (UsesTrafficCarAtlas(
                        renderer))
                {
                    GameObject vehicleRoot =
                        FindTrafficAtlasVehicleRoot(
                            renderer.transform,
                            cityRoot);

                    if (vehicleRoot != null)
                    {
                        rootsToRemove.Add(
                            vehicleRoot);
                    }
                }

                if (UsesStaticTrafficTireMaterial(
                        renderer))
                {
                    rootsToRemove.Add(
                        renderer.gameObject);
                }
            }

            int removed =
                0;

            foreach (GameObject vehicleRoot in
                     rootsToRemove)
            {
                if (vehicleRoot == null)
                    continue;

                vehicleRoot.SetActive(
                    false);

                UnityEngine.Object.Destroy(
                    vehicleRoot);

                removed++;
            }

            if (removed > 0)
            {
                Debug.Log(
                    $"Motor City: removed {removed} baked static parked vehicles from the FCG city.");
            }

            return removed;
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

        private static void EnsureDriveableMeshColliders(
            GameObject city)
        {
            int legacyHighwayCollidersDisabled =
                DisableLegacyHighwayColliders(
                    city);

            int roadCollidersAdded =
                0;

            int roadCollidersReplaced =
                0;

            int specialHighwayColliders =
                0;

            int parkingCollidersAdded =
                0;

            foreach (MeshFilter filter in
                     city.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null ||
                    filter.sharedMesh == null)
                    continue;

                Renderer renderer =
                    filter.GetComponent<Renderer>();

                if (renderer == null)
                    continue;

                string objectName =
                    filter.name.ToLowerInvariant();

                if (TryPrepareSpecialHighwayPart(
                        filter,
                        out bool handledSpecialHighway))
                {
                    if (handledSpecialHighway)
                    {
                        specialHighwayColliders++;
                        continue;
                    }
                }

                bool parking =
                    objectName.StartsWith("park-04") ||
                    objectName.StartsWith("park-05") ||
                    objectName.StartsWith("park-06") ||
                    objectName.StartsWith("park-08");

                if (parking)
                {
                    if (filter.GetComponent<Collider>() == null)
                    {
                        BoxCollider box =
                            filter.gameObject.AddComponent<BoxCollider>();

                        Bounds localBounds =
                            renderer.localBounds;

                        Vector3 size =
                            localBounds.size;

                        size.y =
                            Mathf.Max(
                                size.y,
                                0.2f);

                        Vector3 center =
                            localBounds.center;

                        center.y +=
                            size.y * 0.5f;

                        box.center =
                            center;

                        box.size =
                            size;

                        parkingCollidersAdded++;
                    }

                    continue;
                }

                if (!ShouldHaveRoadMeshCollider(
                        filter.transform,
                        renderer))
                    continue;

                if (!TryBuildDriveableCollisionMesh(
                        filter,
                        renderer,
                        out Mesh collisionMesh))
                    continue;

                foreach (Collider existing in
                         filter.GetComponents<Collider>())
                {
                    if (existing == null)
                        continue;

                    existing.enabled =
                        false;

                    UnityEngine.Object.Destroy(
                        existing);

                    roadCollidersReplaced++;
                }

                MeshCollider collider =
                    filter.gameObject.AddComponent<MeshCollider>();

                collider.sharedMesh =
                    collisionMesh;

                collider.convex =
                    false;

                roadCollidersAdded++;
            }

            if (roadCollidersAdded > 0 ||
                parkingCollidersAdded > 0)
            {
                Debug.Log(
                    "Motor City: prepared exact FCG physics surfaces. " +
                    $"Road/highway MeshColliders={roadCollidersAdded}, " +
                    $"old road colliders replaced={roadCollidersReplaced}, " +
                    $"exact HW-F-400 colliders={specialHighwayColliders}, " +
                    $"legacy highway colliders disabled={legacyHighwayCollidersDisabled}, " +
                    $"parking colliders added={parkingCollidersAdded}.");
            }
        }

        private static bool TryPrepareSpecialHighwayPart(
            MeshFilter filter,
            out bool handled)
        {
            handled =
                false;

            if (filter == null ||
                filter.sharedMesh == null)
                return false;

            Transform segment =
                FindSpecialHighwaySegmentRoot(
                    filter.transform);

            if (segment == null)
                return false;

            bool roadDeck =
                filter.transform ==
                segment;

            string normalizedName =
                NormalizeHighwayPartName(
                    filter.name);

            bool grass =
                normalizedName ==
                "grass";

            bool guardRail =
                normalizedName ==
                "guardrail";

            if (!roadDeck &&
                !grass &&
                !guardRail)
            {
                return true;
            }

            foreach (Collider existing in
                     filter.GetComponents<Collider>())
            {
                if (existing == null)
                    continue;

                existing.enabled =
                    false;

                UnityEngine.Object.Destroy(
                    existing);
            }

            MeshCollider collider =
                filter.gameObject.AddComponent<MeshCollider>();

            // These three FCG highway sections are already split into
            // independent road, grass and guard-rail visual meshes. Using
            // those exact source meshes avoids approximations that flatten
            // hills, miss valleys, or make the roadside float.
            collider.sharedMesh =
                filter.sharedMesh;

            collider.convex =
                false;

            collider.isTrigger =
                false;

            handled =
                true;

            return true;
        }

        private static Transform FindSpecialHighwaySegmentRoot(
            Transform item)
        {
            Transform current =
                item;

            while (current != null)
            {
                string name =
                    current.name;

                if (name.StartsWith(
                        "HW-F-400-01",
                        StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith(
                        "HW-F-400-02",
                        StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith(
                        "HW-F-400-04",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return current;
                }

                current =
                    current.parent;
            }

            return null;
        }

        private static string NormalizeHighwayPartName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
                return string.Empty;

            var result =
                new System.Text.StringBuilder(
                    value.Length);

            foreach (char character in
                     value.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(
                        character))
                {
                    result.Append(
                        character);
                }
            }

            return result.ToString();
        }

        private static bool TryBuildDriveableCollisionMesh(
            MeshFilter filter,
            Renderer renderer,
            out Mesh collisionMesh)
        {
            collisionMesh =
                null;

            if (filter == null ||
                filter.sharedMesh == null ||
                renderer == null)
                return false;

            Mesh source =
                filter.sharedMesh;

            Material[] materials =
                renderer.sharedMaterials;

            int subMeshCount =
                Mathf.Min(
                    source.subMeshCount,
                    materials.Length);

            var triangles =
                new List<int>();

            Vector3[] vertices =
                source.vertices;

            for (int subMesh = 0;
                 subMesh < subMeshCount;
                 subMesh++)
            {
                Material material =
                    materials[subMesh];

                if (!IsDriveableCollisionMaterial(
                        material))
                    continue;

                if (source.GetTopology(
                        subMesh) !=
                    MeshTopology.Triangles)
                    continue;

                int[] sourceTriangles =
                    source.GetTriangles(
                        subMesh);

                for (int i = 0;
                     i + 2 < sourceTriangles.Length;
                     i += 3)
                {
                    int aIndex =
                        sourceTriangles[i];

                    int bIndex =
                        sourceTriangles[i + 1];

                    int cIndex =
                        sourceTriangles[i + 2];

                    Vector3 a =
                        filter.transform.TransformPoint(
                            vertices[aIndex]);

                    Vector3 b =
                        filter.transform.TransformPoint(
                            vertices[bIndex]);

                    Vector3 c =
                        filter.transform.TransformPoint(
                            vertices[cIndex]);

                    Vector3 normal =
                        Vector3.Cross(
                            b - a,
                            c - a);

                    if (normal.sqrMagnitude <
                        0.000001f)
                        continue;

                    normal.Normalize();

                    // Keep sloped road-deck triangles even when the source
                    // mesh uses reversed winding. FCG highway sections can
                    // climb and descend sharply, so testing only positive Y
                    // normals removed valid hill faces and dropped the car
                    // through to unrelated colliders below.
                    if (Mathf.Abs(
                            normal.y) <
                        0.16f)
                        continue;

                    triangles.Add(
                        aIndex);

                    triangles.Add(
                        bIndex);

                    triangles.Add(
                        cIndex);
                }
            }

            if (triangles.Count <
                3)
                return false;

            collisionMesh =
                new Mesh
                {
                    name =
                        source.name +
                        "_MotorCityDriveableCollision",
                    indexFormat =
                        source.indexFormat
                };

            collisionMesh.vertices =
                source.vertices;

            collisionMesh.triangles =
                triangles.ToArray();

            collisionMesh.RecalculateBounds();

            return true;
        }

        private static int DisableLegacyHighwayColliders(
            GameObject city)
        {
            if (city == null)
                return 0;

            int disabled =
                0;

            foreach (Collider collider in
                     city.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null ||
                    !IsHighwayHierarchy(
                        collider.transform))
                    continue;

                if (!collider.enabled)
                    continue;

                collider.enabled =
                    false;

                disabled++;
            }

            return disabled;
        }

        private static bool IsHighwayHierarchy(
            Transform item)
        {
            Transform current =
                item;

            while (current != null)
            {
                string name =
                    current.name.ToLowerInvariant();

                if (name.Contains(
                        "highway") ||
                    name.Contains(
                        "high-way") ||
                    name.Contains(
                        "high_way") ||
                    name.StartsWith(
                        "hw-") ||
                    name.StartsWith(
                        "hwy-"))
                {
                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static bool IsDriveableCollisionMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string name =
                material.name.ToLowerInvariant();

            if (name.Contains(
                    "guardrail") ||
                name.Contains(
                    "guard-rail"))
                return false;

            return
                name.Contains(
                    "road") ||
                name.Contains(
                    "highway");
        }

        private static bool ShouldHaveRoadMeshCollider(
            Transform transform,
            Renderer renderer)
        {
            string path =
                GetHierarchyPath(
                        transform)
                    .ToLowerInvariant();

            string name =
                transform.name.ToLowerInvariant();

            if (name.Contains("guardrail") ||
                name.Contains("guard-rail") ||
                path.Contains("guardrail") ||
                path.Contains("guard-rail"))
                return false;

            bool driveableMaterial =
                UsesDriveableMaterial(
                    renderer);

            bool mainGround =
                path.Contains("/meshes/") ||
                name.StartsWith("bd-") ||
                name.StartsWith("double-block");

            bool highwayMesh =
                name.StartsWith("hw-") ||
                path.Contains("/hw-");

            return
                driveableMaterial &&
                (mainGround ||
                 highwayMesh);
        }

        private static bool UsesDriveableMaterial(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string materialName =
                    material.name;

                if (materialName.IndexOf(
                        "road",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    materialName.IndexOf(
                        "highway",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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
                city.GetComponentsInChildren<Renderer>(true);

            var cityRenderers =
                new List<Renderer>();

            foreach (Renderer renderer in allRenderers)
            {
                if (IsFcgCityRenderer(
                        renderer))
                {
                    cityRenderers.Add(
                        renderer);
                }
            }

            Renderer[] renderers =
                cityRenderers.Count > 0
                    ? cityRenderers.ToArray()
                    : allRenderers;

            if (renderers.Length == 0)
            {
                return new Bounds(
                    new Vector3(
                        0f,
                        80f,
                        -766f),
                    new Vector3(
                        1532f,
                        170f,
                        2764f));
            }

            Bounds bounds =
                renderers[0].bounds;

            for (int i = 1;
                 i < renderers.Length;
                 i++)
            {
                bounds.Encapsulate(
                    renderers[i].bounds);
            }

            return bounds;
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
