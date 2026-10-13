using MotorCity.Diagnostics;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
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

        private static readonly Vector3 AuthoredPlayerSpawnPoint =
            new(
                -428.434631f,
                0.145054966f,
                315.49292f);

        private static readonly Quaternion AuthoredPlayerSpawnRotation =
            Quaternion.Euler(
                359.60907f,
                89.3797455f,
                3.25508745e-06f);

        private static readonly Vector3 AuthoredGaragePoint =
            new(
                -421.68866f,
                0.182654113f,
                330.916077f);

        private static readonly Quaternion AuthoredGarageSpawnRotation =
            Quaternion.Euler(
                7.40436444e-05f,
                89.9998322f,
                -4.8625111e-06f);

        // These targets follow the actual generated FCG grid from the
        // 2026-09-18 city report. Every road target is validated against
        // the exact MeshCollider triangle/submesh using the FCG_Roads material.
        // West-to-east delivery run, then north to the upper district; no backtrack.
        // All anchors snap to actual FCG road triangles on city installation.
        private static readonly Vector3[] DeliveryPreferred =
        {
            new(-330f, 0f, -100f),
            new(-180f, 0f, -100f),
            new(0f, 0f, -100f),
            new(180f, 0f, -100f),
            new(360f, 0f, -100f),
            new(450f, 0f, -95f),
            new(450f, 0f, 80f),
            new(450f, 0f, 255f),
            new(440f, 0f, 415f),
            new(235f, 0f, 450f),
            new(20f, 0f, 450f),
            new(-190f, 0f, 450f)
        };

        // Southbound from the western district, east along the avenue, then north.
        // All anchors snap to actual FCG road triangles on city installation.
        private static readonly Vector3[] SprintPreferred =
        {
            new(-450f, 0f, 200f),
            new(-450f, 0f, 40f),
            new(-450f, 0f, -100f),
            new(-260f, 0f, -100f),
            new(-70f, 0f, -100f),
            new(120f, 0f, -100f),
            new(310f, 0f, -100f),
            new(450f, 0f, -95f),
            new(450f, 0f, 80f),
            new(450f, 0f, 255f),
            new(445f, 0f, 420f)
        };

        // Clockwise perimeter loop. End connects to start on the southern avenue.
        // All anchors snap to actual FCG road triangles on city installation.
        private static readonly Vector3[] CircuitPreferred =
        {
            new(80f, 0f, -100f),
            new(260f, 0f, -100f),
            new(440f, 0f, -100f),
            new(450f, 0f, 150f),
            new(445f, 0f, 390f),
            new(260f, 0f, 450f),
            new(80f, 0f, 450f),
            new(-150f, 0f, 450f),
            new(-450f, 0f, 420f),
            new(-450f, 0f, 145f),
            new(-450f, 0f, -95f),
            new(-230f, 0f, -100f)
        };

        private static Vector3[] deliveryRoute =
            (Vector3[])DeliveryPreferred.Clone();

        private static Vector3[] sprintRoute =
            (Vector3[])SprintPreferred.Clone();

        private static Vector3[] circuitRoute =
            (Vector3[])CircuitPreferred.Clone();

        // Eastbound across the north, south along the eastern road, then west.
        // All anchors snap to actual FCG road triangles on city installation.
        private static readonly Vector3[] UndergroundPreferred =
        {
            new(-300f, 0f, 450f),
            new(-120f, 0f, 450f),
            new(80f, 0f, 450f),
            new(260f, 0f, 450f),
            new(435f, 0f, 405f),
            new(450f, 0f, 230f),
            new(450f, 0f, 55f),
            new(445f, 0f, -95f),
            new(230f, 0f, -100f),
            new(25f, 0f, -100f)
        };

        private static Vector3[] undergroundRoute =
            (Vector3[])UndergroundPreferred.Clone();


        // Visible in Unity Profiler Timeline; no logging or automatic gates.
        private static readonly ProfilerMarker BeginCityPreloadMarker =
            new("MotorCity.City.BeginPrefabPreload");
        private static readonly ProfilerMarker LoadCityMarker =
            new("MotorCity.City.LoadPrefab");
        private static readonly ProfilerMarker InstantiateCityMarker =
            new("MotorCity.City.Instantiate");
        private static readonly ProfilerMarker SnapshotRenderersMarker =
            new("MotorCity.City.SnapshotRenderers");
        private static readonly ProfilerMarker BindNightWindowsMarker =
            new("MotorCity.City.BindNightWindows");
        private static readonly ProfilerMarker CalculateBoundsMarker =
            new("MotorCity.City.CalculateBounds");
        private static readonly ProfilerMarker ResolveGameplayMarker =
            new("MotorCity.City.ResolveGameplayLayout");

        private static GameObject activeCity;
        private static ResourceRequest cityPrefabPreload;

        // Begin the Resources load while the front-end loading screen is
        // visible. The authored prefab is instantiated later, in the same
        // deterministic bootstrap order as before.
        public static void BeginPrefabPreload()
        {
            if (activeCity != null || cityPrefabPreload != null)
                return;

            using (BeginCityPreloadMarker.Auto())
            {
                cityPrefabPreload =
                    Resources.LoadAsync<GameObject>(ResourcePath);
            }
        }

        public static bool IsPrefabPreloadReady =>
            cityPrefabPreload == null || cityPrefabPreload.isDone;

        private static Bounds cityBounds;
        private static bool hasCityBounds;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            activeCity =
                null;

            // Static state must not leak across Editor Play sessions with
            // Domain Reload disabled.
            cityPrefabPreload =
                null;

            cityBounds =
                default;

            hasCityBounds =
                false;

            deliveryRoute =
                (Vector3[])DeliveryPreferred.Clone();

            sprintRoute =
                (Vector3[])SprintPreferred.Clone();

            circuitRoute =
                (Vector3[])CircuitPreferred.Clone();

            undergroundRoute =
                (Vector3[])UndergroundPreferred.Clone();

            PlayerSpawnPoint =
                AuthoredPlayerSpawnPoint;

            PlayerSpawnRotation =
                AuthoredPlayerSpawnRotation;

            GaragePoint =
                AuthoredGaragePoint;

            GarageSpawnRotation =
                AuthoredGarageSpawnRotation;

            DriftChallengePoint =
                new Vector3(
                    150f,
                    0.2f,
                    150f);
        }

        public static Vector3 PlayerSpawnPoint { get; private set; } =
            AuthoredPlayerSpawnPoint;

        public static Quaternion PlayerSpawnRotation { get; private set; } =
            AuthoredPlayerSpawnRotation;

        // Dedicated player garage on the authored parking apron.
        public static Vector3 GaragePoint { get; private set; } =
            AuthoredGaragePoint;

        public static Quaternion GarageSpawnRotation { get; private set; } =
            AuthoredGarageSpawnRotation;

        public static Vector3 GarageVehiclePosition { get; } =
            AuthoredGaragePoint;

        public static Quaternion GarageVehicleRotation { get; } =
            Quaternion.Euler(
                0f,
                142.676514f,
                0f);

        public static Vector3 GarageExitPosition { get; } =
            new(
                -413.192017f,
                0.174802512f,
                331.059753f);

        public static Quaternion GarageExitRotation { get; } =
            Quaternion.Euler(
                359.607452f,
                181.803848f,
                0.0546497814f);

        public static Vector3 GarageCameraPosition { get; } =
            new(
                -549.252f,
                3.25f,
                -792.25f);

        public static Quaternion GarageCameraRotation { get; } =
            Quaternion.Euler(
                11.5f,
                224.75f,
                0f);

        // North-central drift arena, clear of the garage and activity starts.
        public static Vector3 DriftChallengePoint { get; private set; } =
            new(150f, 0.2f, 150f);

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
                GameObject prefab;
                using (LoadCityMarker.Auto())
                {
                    // Normally completed by the loading-screen prefetch.
                    // Preserve synchronous loading for direct callers and
                    // older initialization flows that skip the front end.
                    prefab = cityPrefabPreload != null &&
                             cityPrefabPreload.isDone
                        ? cityPrefabPreload.asset as GameObject
                        : Resources.Load<GameObject>(ResourcePath);
                }

                cityPrefabPreload = null;

                if (prefab == null)
                    return false;

                using (InstantiateCityMarker.Auto())
                {
                    activeCity = UnityEngine.Object.Instantiate(prefab);
                    activeCity.name = RuntimeCityName;
                }
            }

            // Both night emission and city bounds use the same authored
            // renderer snapshot. City geometry and positions remain untouched.
            Renderer[] cityRenderers;
            using (SnapshotRenderersMarker.Auto())
            {
                cityRenderers = activeCity.GetComponentsInChildren<Renderer>(true);
            }

            using (BindNightWindowsMarker.Auto())
            {
                FcgRuntimeGlassMaterialFactory.BindAuthoredWindowEmission(
                    cityRenderers);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // Development build detection is runtime-only in Unity 6.
            // Keep the explicit audit define available for release diagnostics.
            bool runMaterialDiagnostics = Debug.isDebugBuild;
#if MOTORCITY_CITY_MATERIAL_AUDIT
            runMaterialDiagnostics = true;
#endif
            if (runMaterialDiagnostics)
            {
                // Diagnostic only: never rewrite authored city materials.
                MotorCityWebMaterialDiagnostics.Run(activeCity);
            }
#endif

            // Runtime treats the authored city as read-only.
            // Colliders, props, parked vehicles, traffic signals and all
            // other map objects must come exactly from CityVisual.prefab.
            using (CalculateBoundsMarker.Auto())
            {
                cityBounds = CalculateCityBounds(cityRenderers);
            }

            hasCityBounds =
                cityBounds.size.x > 10f &&
                cityBounds.size.z > 10f;

            Physics.SyncTransforms();

            using (ResolveGameplayMarker.Auto())
            {
                ResolveGameplayLayout();
            }

            return true;
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
                AuthoredGaragePoint;

            GarageSpawnRotation =
                AuthoredGarageSpawnRotation;

            PlayerSpawnPoint =
                AuthoredPlayerSpawnPoint;

            PlayerSpawnRotation =
                AuthoredPlayerSpawnRotation;

            DriftChallengePoint =
                FindRoadPointNear(
                    new Vector3(
                        150f,
                        0f,
                        150f),
                    75f,
                    true);

            deliveryRoute =
                InsertRoadGraphWaypoints(
                    ResolveRoadRoute(DeliveryPreferred), 6);

            sprintRoute =
                InsertRoadGraphWaypoints(
                    ResolveRoadRoute(SprintPreferred), 8);

            circuitRoute =
                ResolveRoadRoute(
                    CircuitPreferred);

            undergroundRoute =
                InsertRoadGraphWaypoints(
                    ResolveRoadRoute(UndergroundPreferred), 5);
        }

        // Activity start points and secondary markers use exactly the same
        // road-triangle validation as race checkpoints. Garage remains authored.
        public static Vector3 ResolveActivityRoadPoint(Vector3 preferred)
        {
            return FindRoadPointNear(preferred, 80f, false);
        }

        // Editor layout audit verifies the actual FCG road collider surface,
        // not merely a rough city-grid coordinate.
        public static bool IsRoadSurface(Vector3 position)
        {
            return activeCity != null &&
                TryGetRoadHit(position.x, position.z, out _);
        }

        // An FCG way graph can omit a link between adjacent pieces of road.
        // Only permit the navigator to bridge that missing link when the whole
        // straight segment is verified as drivable asphalt in the city mesh.
        // Do not infer this from proximity or from the authored grid alone.
        public static bool IsContinuousStraightRoad(Vector3 from, Vector3 to)
        {
            if (activeCity == null)
                return false;

            Vector2 delta = new Vector2(to.x - from.x, to.z - from.z);
            float distance = delta.magnitude;
            if (distance < 24f || distance > 260f)
                return false;

            // Check narrow road corridors, not diagonal shortcuts through
            // the middle of blocks.
            if (Mathf.Min(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) >
                Mathf.Max(8f, distance * 0.08f))
                return false;

            int segments = Mathf.CeilToInt(distance / 4f);
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float x = Mathf.Lerp(from.x, to.x, t);
                float z = Mathf.Lerp(from.z, to.z, t);
                if (!TryGetRoadHit(x, z, out Vector3 asphalt))
                    return false;

                // Reject stacked streets, bridges at different elevations,
                // or segments that dip below the intended driving surface.
                float expectedY = Mathf.Lerp(from.y, to.y, t);
                if (Mathf.Abs(asphalt.y - expectedY) > 3f)
                    return false;
            }

            return true;
        }

        // An authored road can bend around a non-drivable city block even
        // when two targets are only 125m apart in a straight line. Replace
        // the flagged direct leg by intermediate checkpoints sampled along
        // the *actual connected FCG route*, not by crossing the block.
        // This is computed once per city installation, not every frame.
        public static Vector3[] InsertRoadGraphWaypoints(
            Vector3[] anchors, int legIndex)
        {
            if (anchors == null || legIndex < 0 ||
                legIndex + 1 >= anchors.Length)
                return anchors;

            Vector3 start = anchors[legIndex];
            Vector3 finish = anchors[legIndex + 1];
            List<Vector3> path = CityRoadNavigator.BuildRoute(start, finish);
            if (path.Count < 2)
            {
                Debug.LogWarning("[MotorCity][Layout] Cannot connect road detour " +
                    legIndex + "; keeping original route for audit.");
                return anchors;
            }

            float distance = 0f;
            for (int i = 1; i < path.Count; i++)
                distance += FlatDistanceXZ(path[i - 1], path[i]);

            float direct = FlatDistanceXZ(start, finish);
            // Keep a road segment unchanged when the current FCG graph has
            // no significant detour; this handles future city revisions.
            if (distance <= direct * 3.2f + 60f)
                return anchors;

            // About one checkpoint every 100m of *road travel* ensures that
            // successive targets are approachable without driving 550m
            // toward an apparently adjacent marker on another street.
            int intervals = Mathf.Max(2, Mathf.CeilToInt(distance / 105f));
            List<Vector3> inserted = new List<Vector3>(intervals - 1);
            int segment = 1;
            float covered = 0f;
            for (int sample = 1; sample < intervals; sample++)
            {
                float targetDistance = distance * sample / intervals;
                while (segment < path.Count - 1 &&
                       covered + FlatDistanceXZ(path[segment - 1],
                           path[segment]) < targetDistance)
                {
                    covered += FlatDistanceXZ(path[segment - 1], path[segment]);
                    segment++;
                }

                Vector3 a = path[segment - 1];
                Vector3 b = path[segment];
                float legDistance = FlatDistanceXZ(a, b);
                float t = legDistance > 0.001f
                    ? Mathf.Clamp01((targetDistance - covered) / legDistance)
                    : 0f;
                Vector3 samplePosition = Vector3.Lerp(a, b, t);

                if (!TryGetRoadHit(samplePosition.x, samplePosition.z,
                        out Vector3 roadPosition))
                {
                    // Refuse to put mission markers on a non-road graph link.
                    // The audit keeps the original warning in this case.
                    Debug.LogWarning("[MotorCity][Layout] Road detour sample " +
                        sample + " for leg " + legIndex +
                        " has no drivable asphalt; inspect FCG links.");
                    return anchors;
                }

                roadPosition.y += MarkerLift;
                inserted.Add(roadPosition);
            }

            List<Vector3> result = new List<Vector3>(
                anchors.Length + inserted.Count);
            for (int i = 0; i < anchors.Length; i++)
            {
                result.Add(anchors[i]);
                if (i == legIndex)
                    result.AddRange(inserted);
            }

            Debug.Log("[MotorCity][Layout] Expanded disconnected straight leg " +
                legIndex + " into " + intervals + " road-aligned legs.");
            return result.ToArray();
        }

        private static float FlatDistanceXZ(Vector3 a, Vector3 b)
        {
            float dx = b.x - a.x;
            float dz = b.z - a.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
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

        private static bool IsFcgCityRenderer(
            Renderer renderer,
            List<Material> sharedMaterialBuffer)
        {
            if (renderer == null)
                return false;

            // Renderer.sharedMaterials allocates an array on every call.
            // Reuse one buffer across the authored city's renderers instead.
            sharedMaterialBuffer.Clear();
            renderer.GetSharedMaterials(sharedMaterialBuffer);

            foreach (Material material in sharedMaterialBuffer)
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
            Renderer[] allRenderers)
        {
            bool hasFcgRenderer =
                false;

            bool boundsInitialized =
                false;

            Bounds bounds =
                default;

            var sharedMaterialBuffer = new List<Material>(8);
            foreach (Renderer renderer in
                     allRenderers)
            {
                if (!IsFcgCityRenderer(
                        renderer,
                        sharedMaterialBuffer))
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
