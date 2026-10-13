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

            // The east arterial has a break between the north and south
            // sections. Returning to the old mid-point after a detour made
            // racers turn around before continuing south. Aim the detour at
            // the next real southern checkpoint instead.
            undergroundRoute =
                RoutePastDisconnectedCheckpoint(
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

        // Give the detour planner the same material-validated road surface
        // as the rest of the city, not a raw Physics hit on buildings.
        public static bool TryGetRoadPoint(float x, float z, out Vector3 point)
        {
            point = default;
            if (activeCity == null || !TryGetRoadHit(x, z, out point))
                return false;
            point.y += MarkerLift;
            return true;
        }

        // A preferred grid position may snap onto exactly the same road
        // triangle as another type of marker. Resolve separation AFTER road
        // projection; do not rely on authored X/Z distance.
        public static Vector3 ResolveSeparatedMarkerPoint(Vector3 current,
            IReadOnlyList<Vector3> occupied, float minimumDistance = 95f)
        {
            if (occupied == null || occupied.Count == 0)
                return current;

            bool HasClearance(Vector3 point)
            {
                for (int i = 0; i < occupied.Count; i++)
                {
                    if (FlatDistanceXZ(point, occupied[i]) < minimumDistance)
                        return false;
                }
                return true;
            }

            if (HasClearance(current))
                return current;

            // Search closest offsets first. Each accepted point is a real
            // FCG road hit, not a raw nearby nav-waypoint or sidewalk.
            float[] radii = { 100f, 145f, 195f, 260f, 340f, 430f };
            const int directions = 16;
            foreach (float radius in radii)
            {
                for (int index = 0; index < directions; index++)
                {
                    float angle = 2f * Mathf.PI * index / directions;
                    float x = current.x + Mathf.Cos(angle) * radius;
                    float z = current.z + Mathf.Sin(angle) * radius;
                    if (!TryGetRoadHit(x, z, out Vector3 road))
                        continue;

                    road.y += MarkerLift;
                    if (HasClearance(road))
                        return road;
                }
            }

            Debug.LogWarning("[MotorCity][Layout] Cannot safely relocate a " +
                "marker away from its neighbours: " + current.ToString("F1"));
            return current;
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

        // Unlike the FCG graph, this answers whether the straight segment
        // itself is physically drivable, including short diagonal stretches.
        // This is used by the layout audit to avoid false detour warnings
        // where asphalt is continuous but FCG Way links are incomplete.
        public static bool HasDrivableAsphaltBetween(Vector3 from, Vector3 to)
        {
            if (activeCity == null)
                return false;

            float length = FlatDistanceXZ(from, to);
            if (length > 160f)
                return false;

            int samples = Mathf.Max(1, Mathf.CeilToInt(length / 3.5f));
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                float x = Mathf.Lerp(from.x, to.x, t);
                float z = Mathf.Lerp(from.z, to.z, t);
                if (!TryGetRoadHit(x, z, out Vector3 road) ||
                    Mathf.Abs(road.y - Mathf.Lerp(from.y, to.y, t)) > 3f)
                    return false;
            }
            return true;
        }

        // An authored road can bend around a non-drivable city block even
        // when two targets are only 125m apart in a straight line. Replace
        // the flagged direct leg by intermediate checkpoints sampled along
        // the *actual connected FCG route*, not by crossing the block.
        // This is computed once per city installation, not every frame.
        // FCG can connect waypoint nodes through empty lots. Never convert
        // those graph edges directly into mission checkpoint coordinates.
        // The route planner checks *every* sampled metre against the asphalt
        // collider, and its output is validated again before insertion.
        // Skip a blocked mid-point only when an independently validated
        // asphalt route reaches the following checkpoint without a hairpin.
        // Used for the east arterial: stopping at its northern endpoint after
        // approaching from the south previously forced an immediate U-turn.
        // The original route remains the safe fallback if we cannot prove
        // the alternative in the current FCG city geometry.
        public static Vector3[] RoutePastDisconnectedCheckpoint(
            Vector3[] anchors, int blockedLeg)
        {
            if (anchors == null || blockedLeg < 0 ||
                blockedLeg + 2 >= anchors.Length)
                return anchors;

            Vector3 skipped = anchors[blockedLeg + 1];
            Vector3 after = anchors[blockedLeg + 2];
            List<Vector3> shortened =
                new List<Vector3>(anchors.Length - 1);
            for (int i = 0; i < anchors.Length; i++)
            {
                if (i != blockedLeg + 1)
                    shortened.Add(anchors[i]);
            }

            Vector3[] directRoute = shortened.ToArray();
            Vector3[] alternative =
                InsertRoadGraphWaypoints(directRoute, blockedLeg);

            bool connected = alternative.Length > directRoute.Length ||
                HasDrivableAsphaltBetween(
                    directRoute[blockedLeg], after);
            if (connected && !HasSharpCheckpointReversal(alternative))
            {
                Debug.Log("[MotorCity][Layout] Bypassed unreachable " +
                    "return checkpoint " + skipped.ToString("F1") +
                    " with " + (alternative.Length - directRoute.Length) +
                    " verified asphalt detour waypoints.");
                return alternative;
            }

            Debug.LogWarning("[MotorCity][Layout] Cannot bypass blocked " +
                "return checkpoint " + skipped.ToString("F1") +
                " without a sharp U-turn; retaining original route for QA.");
            return InsertRoadGraphWaypoints(anchors, blockedLeg);
        }

        // Match the editor's real turning-angle rule, never its warning text.
        // A candidate detour is accepted only if its whole route maintains
        // forward progress through each checkpoint.
        private static bool HasSharpCheckpointReversal(Vector3[] points)
        {
            for (int i = 1; i + 1 < points.Length; i++)
            {
                Vector3 a = points[i] - points[i - 1];
                Vector3 b = points[i + 1] - points[i];
                Vector2 incoming = new Vector2(a.x, a.z);
                Vector2 outgoing = new Vector2(b.x, b.z);
                if (incoming.sqrMagnitude <= 16f ||
                    outgoing.sqrMagnitude <= 16f)
                    continue;

                if (Vector2.Dot(incoming.normalized, outgoing.normalized)
                    < -0.35f)
                    return true;
            }
            return false;
        }

        public static Vector3[] InsertRoadGraphWaypoints(
            Vector3[] anchors, int legIndex)
        {
            if (anchors == null || legIndex < 0 ||
                legIndex + 1 >= anchors.Length)
                return anchors;

            Vector3 start = anchors[legIndex];
            Vector3 finish = anchors[legIndex + 1];

            if (HasDrivableAsphaltBetween(start, finish))
                return anchors;

            if (!DrivableRoadDetourPlanner.TryFindWaypoints(
                    start, finish, out List<Vector3> stops) ||
                stops == null || stops.Count == 0)
            {
                Debug.LogWarning("[MotorCity][Layout] No validated asphalt " +
                    "detour for checkpoint leg " + legIndex + " (" +
                    start.ToString("F1") + " -> " + finish.ToString("F1") +
                    "). Keeping original targets visible to the layout audit.");
                return anchors;
            }

            // Reject overshort, duplicated and off-asphalt intermediate
            // checkpoint positions. A failure should remain observable by QA.
            Vector3 previous = start;
            for (int i = 0; i <= stops.Count; i++)
            {
                Vector3 next = i == stops.Count ? finish : stops[i];
                float leg = FlatDistanceXZ(previous, next);
                bool hasAsphalt = HasDrivableAsphaltBetween(previous, next);
                if (leg < 24f || leg > 160f || !hasAsphalt)
                {
                    string reason = leg < 24f ? "checkpoint spacing below 24m" :
                        leg > 160f ? "checkpoint spacing above 160m" :
                        "non-drivable asphalt between endpoints";
                    Debug.LogWarning("[MotorCity][Layout] Asphalt detour " +
                        "failed segment validation at point " + i +
                        " for leg " + legIndex + ": " + reason +
                        ", distance=" + leg.ToString("F1") + "m, from " +
                        previous.ToString("F1") + " to " +
                        next.ToString("F1") + ". Original leg preserved.");
                    return anchors;
                }
                previous = next;
            }

            List<Vector3> result = new List<Vector3>(
                anchors.Length + stops.Count);
            for (int i = 0; i < anchors.Length; i++)
            {
                result.Add(anchors[i]);
                if (i == legIndex)
                    result.AddRange(stops);
            }

            Debug.Log("[MotorCity][Layout] Checkpoint leg " + legIndex +
                " now follows " + (stops.Count + 1) +
                " physically validated asphalt segments.");
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
