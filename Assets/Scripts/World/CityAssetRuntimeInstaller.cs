using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    public static class CityAssetRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/CityVisual";

        private const float SurfaceSampleStep = 4f;

        private struct DriveableSample
        {
            public Vector3 Position;
            public bool IsHighway;
        }

        private static readonly List<DriveableSample>
            driveableSamples = new();

        private static readonly List<Bounds>
            buildingBounds = new();

        private static Bounds gameplayBounds;
        private static bool hasGameplayBounds;

        private static Vector3[] deliveryRoute =
        {
            new(-120f, 0f, -80f),
            new(-40f, 0f, -40f),
            new(30f, 0f, -10f),
            new(100f, 0f, 35f),
            new(45f, 0f, 100f),
            new(-55f, 0f, 85f)
        };

        private static Vector3[] sprintRoute =
        {
            new(-150f, 0f, -110f),
            new(-155f, 0f, 70f),
            new(-80f, 0f, 145f),
            new(55f, 0f, 150f),
            new(150f, 0f, 85f),
            new(155f, 0f, -70f),
            new(70f, 0f, -145f),
            new(-65f, 0f, -150f)
        };

        public static Vector3 PlayerSpawnPoint { get; private set; } =
            Vector3.zero;

        public static Quaternion PlayerSpawnRotation { get; private set; } =
            Quaternion.identity;

        public static Vector3 GaragePoint { get; private set; } =
            new(-60f, 0f, 30f);

        public static Vector3 DriftChallengePoint { get; private set; } =
            new(60f, 0f, 20f);

        public static Vector3[] DeliveryRoute =>
            (Vector3[])deliveryRoute.Clone();

        public static Vector3[] SprintRoute =>
            (Vector3[])sprintRoute.Clone();

        public static bool TryInstall()
        {
            driveableSamples.Clear();
            buildingBounds.Clear();

            GameObject prefab =
                Resources.Load<GameObject>(ResourcePath);

            if (prefab == null)
                return false;

            GameObject city =
                Object.Instantiate(prefab);

            city.name =
                "Motor City — Versatile Demo City";

            Transform cityPart =
                FindTransformByName(
                    city.transform,
                    "city_part_demo_main1");

            Transform buildingsRoot =
                cityPart != null
                    ? FindTransformByName(
                        cityPart,
                        "BUILDINGS")
                    : null;

            gameplayBounds =
                ResolveGameplayBounds(
                    city,
                    cityPart,
                    buildingsRoot);

            hasGameplayBounds =
                gameplayBounds.size.x > 1f &&
                gameplayBounds.size.z > 1f;

            CacheBuildingBounds(
                buildingsRoot);

            PrepareDriveableColliders(
                city);

            AddBuildingColliders(
                city);

            Physics.SyncTransforms();

            SampleDriveableSurfaces();

            CreateGroundCollider(
                city);

            ResolveGameplayLayout();

            return true;
        }

        public static Vector3 SnapToNearestRoad(
            Vector3 approximate)
        {
            if (driveableSamples.Count == 0)
                return approximate;

            return FindNearestSample(
                approximate,
                false,
                false,
                null,
                0f).Position;
        }

        private static void ResolveGameplayLayout()
        {
            Vector3 center =
                new(
                    gameplayBounds.center.x,
                    gameplayBounds.center.y,
                    gameplayBounds.center.z);

            if (driveableSamples.Count == 0)
            {
                Debug.LogWarning(
                    "Motor City: no sampled asphalt surfaces were found inside " +
                    "city_part_demo_main1/BUILDINGS. Using city-relative fallbacks.");

                PlayerSpawnPoint =
                    new Vector3(
                        120f,
                        center.y,
                        30f);

                PlayerSpawnRotation =
                    Quaternion.LookRotation(
                        FlatDirection(
                            center -
                            PlayerSpawnPoint),
                        Vector3.up);

                GaragePoint =
                    center +
                    new Vector3(-20f, 0f, 15f);

                DriftChallengePoint =
                    center +
                    new Vector3(25f, 0f, 10f);

                deliveryRoute =
                    BuildFallbackRoute(
                        center,
                        gameplayBounds.extents.x * 0.5f,
                        gameplayBounds.extents.z * 0.5f,
                        DeliveryNormalizedLayout);

                sprintRoute =
                    BuildFallbackRoute(
                        center,
                        gameplayBounds.extents.x * 0.55f,
                        gameplayBounds.extents.z * 0.55f,
                        SprintNormalizedLayout);

                return;
            }

            var reserved =
                new List<Vector3>();

            Vector3 preferredSpawn =
                new(
                    120f,
                    gameplayBounds.center.y,
                    30f);

            PlayerSpawnPoint =
                FindDriveablePointNear(
                    preferredSpawn,
                    28f,
                    "spawn");

            reserved.Add(
                PlayerSpawnPoint);

            PlayerSpawnRotation =
                Quaternion.LookRotation(
                    EstimateRoadDirection(
                        PlayerSpawnPoint,
                        center),
                    Vector3.up);

            DriveableSample garageRoad =
                SelectGarageSample(
                    center,
                    reserved);

            GaragePoint =
                MoveGarageToRoadside(
                    garageRoad.Position);

            reserved.Add(
                garageRoad.Position);

            DriveableSample drift =
                SelectDriftSample(
                    center,
                    reserved);

            DriftChallengePoint =
                drift.Position +
                Vector3.up * 0.04f;

            reserved.Add(
                drift.Position);

            deliveryRoute =
                BuildActivityRoute(
                    DeliveryNormalizedLayout,
                    false,
                    true,
                    reserved,
                    Mathf.Max(
                        24f,
                        Mathf.Min(
                            gameplayBounds.extents.x,
                            gameplayBounds.extents.z) * 0.18f));

            foreach (Vector3 point in deliveryRoute)
                reserved.Add(point);

            sprintRoute =
                BuildActivityRoute(
                    SprintNormalizedLayout,
                    true,
                    false,
                    reserved,
                    Mathf.Max(
                        28f,
                        Mathf.Min(
                            gameplayBounds.extents.x,
                            gameplayBounds.extents.z) * 0.16f));

            Debug.Log(
                "Motor City: semantic city layout resolved. " +
                $"Spawn={PlayerSpawnPoint}, " +
                $"Garage={GaragePoint}, " +
                $"Drift={DriftChallengePoint}, " +
                $"Delivery={FormatRoute(deliveryRoute)}, " +
                $"Sprint={FormatRoute(sprintRoute)}");
        }

        private static readonly Vector2[] DeliveryNormalizedLayout =
        {
            new(-0.48f, -0.30f),
            new(-0.18f, -0.14f),
            new(0.12f, -0.02f),
            new(0.46f, 0.18f),
            new(0.20f, 0.44f),
            new(-0.36f, 0.38f)
        };

        private static readonly Vector2[] SprintNormalizedLayout =
        {
            new(-0.58f, -0.48f),
            new(-0.58f, 0.08f),
            new(-0.34f, 0.54f),
            new(0.18f, 0.58f),
            new(0.56f, 0.38f),
            new(0.58f, -0.16f),
            new(0.32f, -0.56f),
            new(-0.30f, -0.56f)
        };

        private static Vector3[] BuildActivityRoute(
            Vector2[] normalizedPoints,
            bool preferHighway,
            bool avoidHighway,
            List<Vector3> reserved,
            float minimumSpacing)
        {
            Vector3[] result =
                new Vector3[normalizedPoints.Length];

            var used =
                new List<Vector3>(reserved);

            Vector3 center =
                gameplayBounds.center;

            float halfX =
                gameplayBounds.extents.x * 0.68f;

            float halfZ =
                gameplayBounds.extents.z * 0.68f;

            for (int i = 0;
                 i < normalizedPoints.Length;
                 i++)
            {
                Vector2 normalized =
                    normalizedPoints[i];

                Vector3 target =
                    center +
                    new Vector3(
                        normalized.x * halfX,
                        0f,
                        normalized.y * halfZ);

                DriveableSample sample =
                    FindNearestSample(
                        target,
                        preferHighway,
                        avoidHighway,
                        used,
                        minimumSpacing);

                result[i] =
                    sample.Position +
                    Vector3.up * 0.04f;

                used.Add(
                    sample.Position);
            }

            return result;
        }

        private static DriveableSample SelectGarageSample(
            Vector3 center,
            List<Vector3> reserved)
        {
            DriveableSample best =
                driveableSamples[0];

            float bestScore =
                float.PositiveInfinity;

            float minExtent =
                Mathf.Max(
                    1f,
                    Mathf.Min(
                        gameplayBounds.extents.x,
                        gameplayBounds.extents.z));

            foreach (DriveableSample sample in driveableSamples)
            {
                if (sample.IsHighway)
                    continue;

                float buildingDistance =
                    DistanceToNearestBuildingXZ(
                        sample.Position);

                float radial =
                    HorizontalDistance(
                        sample.Position,
                        center) /
                    minExtent;

                float score =
                    Mathf.Abs(
                        radial - 0.52f) *
                    45f;

                score +=
                    Mathf.Abs(
                        buildingDistance - 9f) *
                    2.6f;

                score +=
                    ReservationPenalty(
                        sample.Position,
                        reserved,
                        34f);

                if (!ContainsXZ(
                        gameplayBounds,
                        sample.Position,
                        12f))
                    score += 200f;

                if (score >= bestScore)
                    continue;

                bestScore =
                    score;

                best =
                    sample;
            }

            return best;
        }

        private static Vector3 MoveGarageToRoadside(
            Vector3 roadPoint)
        {
            if (!TryNearestBuildingPoint(
                    roadPoint,
                    out Vector3 buildingPoint,
                    out float buildingDistance))
            {
                return roadPoint +
                       Vector3.up * 0.04f;
            }

            Vector3 direction =
                buildingPoint -
                roadPoint;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f)
            {
                return roadPoint +
                       Vector3.up * 0.04f;
            }

            direction.Normalize();

            float offset =
                Mathf.Clamp(
                    buildingDistance - 2.8f,
                    2.5f,
                    7f);

            Vector3 roadside =
                roadPoint +
                direction *
                offset;

            roadside.x =
                Mathf.Clamp(
                    roadside.x,
                    gameplayBounds.min.x + 6f,
                    gameplayBounds.max.x - 6f);

            roadside.z =
                Mathf.Clamp(
                    roadside.z,
                    gameplayBounds.min.z + 6f,
                    gameplayBounds.max.z - 6f);

            roadside.y =
                roadPoint.y + 0.04f;

            return roadside;
        }

        private static DriveableSample SelectDriftSample(
            Vector3 center,
            List<Vector3> reserved)
        {
            DriveableSample best =
                driveableSamples[0];

            float bestScore =
                float.NegativeInfinity;

            for (int i = 0;
                 i < driveableSamples.Count;
                 i += 2)
            {
                DriveableSample sample =
                    driveableSamples[i];

                if (sample.IsHighway)
                    continue;

                if (!ContainsXZ(
                        gameplayBounds,
                        sample.Position,
                        18f))
                    continue;

                float reservedPenalty =
                    ReservationPenalty(
                        sample.Position,
                        reserved,
                        42f);

                if (reservedPenalty > 0f)
                    continue;

                int openness =
                    CountNearbyDriveableSamples(
                        sample.Position,
                        18f);

                float buildingDistance =
                    DistanceToNearestBuildingXZ(
                        sample.Position);

                float centerDistance =
                    HorizontalDistance(
                        sample.Position,
                        center);

                float score =
                    openness * 4f +
                    Mathf.Min(
                        buildingDistance,
                        28f) *
                    1.5f -
                    centerDistance * 0.025f;

                if (score <= bestScore)
                    continue;

                bestScore =
                    score;

                best =
                    sample;
            }

            return best;
        }

        private static DriveableSample FindNearestSample(
            Vector3 target,
            bool preferHighway,
            bool avoidHighway,
            List<Vector3> reserved,
            float minimumSpacing)
        {
            DriveableSample best =
                driveableSamples[0];

            float bestScore =
                float.PositiveInfinity;

            foreach (DriveableSample sample in driveableSamples)
            {
                float score =
                    HorizontalSqrDistance(
                        sample.Position,
                        target);

                if (preferHighway &&
                    !sample.IsHighway)
                    score += 1800f;

                if (avoidHighway &&
                    sample.IsHighway)
                    score += 2600f;

                if (avoidHighway)
                {
                    float buildingDistance =
                        DistanceToNearestBuildingXZ(
                            sample.Position);

                    score +=
                        Mathf.Abs(
                            buildingDistance - 9f) *
                        18f;
                }

                score +=
                    ReservationPenalty(
                        sample.Position,
                        reserved,
                        minimumSpacing);

                if (score >= bestScore)
                    continue;

                bestScore =
                    score;

                best =
                    sample;
            }

            return best;
        }

        private static float ReservationPenalty(
            Vector3 point,
            List<Vector3> reserved,
            float minimumSpacing)
        {
            if (reserved == null ||
                minimumSpacing <= 0f)
                return 0f;

            float minimumSpacingSqr =
                minimumSpacing *
                minimumSpacing;

            float penalty =
                0f;

            foreach (Vector3 used in reserved)
            {
                float distance =
                    HorizontalSqrDistance(
                        point,
                        used);

                if (distance >= minimumSpacingSqr)
                    continue;

                penalty +=
                    (minimumSpacingSqr - distance) *
                    4f;
            }

            return penalty;
        }

        private static Vector3 FindDriveablePointNear(
            Vector3 preferred,
            float searchRadius,
            string context)
        {
            const float step = 2f;

            Vector3 best =
                FindNearestSample(
                    preferred,
                    false,
                    false,
                    null,
                    0f).Position;

            float bestDistance =
                float.PositiveInfinity;

            for (float x = -searchRadius;
                 x <= searchRadius;
                 x += step)
            {
                for (float z = -searchRadius;
                     z <= searchRadius;
                     z += step)
                {
                    Vector3 probe =
                        new(
                            preferred.x + x,
                            gameplayBounds.center.y,
                            preferred.z + z);

                    if (!ContainsXZ(
                            gameplayBounds,
                            probe,
                            4f))
                        continue;

                    if (!TryFindDriveableHit(
                            probe.x,
                            probe.z,
                            out DriveableSample sample))
                        continue;

                    float distance =
                        HorizontalSqrDistance(
                            sample.Position,
                            preferred);

                    if (distance >= bestDistance)
                        continue;

                    bestDistance =
                        distance;

                    best =
                        sample.Position;
                }
            }

            if (float.IsPositiveInfinity(bestDistance))
            {
                Debug.LogWarning(
                    $"Motor City: no direct asphalt hit for {context} near {preferred}; " +
                    $"using sampled road point {best}.");
            }
            else
            {
                Debug.Log(
                    $"Motor City: {context} placed on asphalt at {best}.");
            }

            return best;
        }

        private static void PrepareDriveableColliders(
            GameObject city)
        {
            Renderer[] renderers =
                city.GetComponentsInChildren<Renderer>(true);

            int count =
                0;

            foreach (Renderer renderer in renderers)
            {
                if (!IsDriveableSurface(renderer))
                    continue;

                if (hasGameplayBounds &&
                    !IntersectsXZ(
                        gameplayBounds,
                        renderer.bounds,
                        14f))
                    continue;

                EnsureRoadCollider(
                    renderer);

                count++;
            }

            Debug.Log(
                $"Motor City: prepared {count} driveable asphalt renderers for physics sampling.");
        }

        private static void SampleDriveableSurfaces()
        {
            driveableSamples.Clear();

            float minX =
                gameplayBounds.min.x + 3f;
            float maxX =
                gameplayBounds.max.x - 3f;
            float minZ =
                gameplayBounds.min.z + 3f;
            float maxZ =
                gameplayBounds.max.z - 3f;

            for (float x = minX;
                 x <= maxX;
                 x += SurfaceSampleStep)
            {
                for (float z = minZ;
                     z <= maxZ;
                     z += SurfaceSampleStep)
                {
                    if (!TryFindDriveableHit(
                            x,
                            z,
                            out DriveableSample sample))
                        continue;

                    driveableSamples.Add(
                        sample);
                }
            }

            Debug.Log(
                $"Motor City: sampled {driveableSamples.Count} real driveable road points " +
                $"inside city_part_demo_main1/BUILDINGS.");
        }

        private static bool TryFindDriveableHit(
            float x,
            float z,
            out DriveableSample sample)
        {
            sample =
                default;

            float rayY =
                gameplayBounds.max.y +
                80f;

            float rayDistance =
                Mathf.Max(
                    180f,
                    gameplayBounds.size.y +
                    160f);

            RaycastHit[] hits =
                Physics.RaycastAll(
                    new Vector3(
                        x,
                        rayY,
                        z),
                    Vector3.down,
                    rayDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

            if (hits == null ||
                hits.Length == 0)
                return false;

            System.Array.Sort(
                hits,
                (a, b) =>
                    a.distance.CompareTo(
                        b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                Renderer renderer =
                    hit.collider.GetComponent<Renderer>();

                if (renderer == null)
                    renderer =
                        hit.collider.GetComponentInParent<Renderer>();

                if (renderer == null)
                    renderer =
                        hit.collider.GetComponentInChildren<Renderer>();

                if (!IsDriveableSurface(renderer))
                    continue;

                if (!ContainsXZ(
                        gameplayBounds,
                        hit.point,
                        2f))
                    continue;

                sample =
                    new DriveableSample
                    {
                        Position = hit.point,
                        IsHighway =
                            IsHighwaySurface(
                                renderer)
                    };

                return true;
            }

            return false;
        }

        private static bool IsDriveableSurface(
            Renderer renderer)
        {
            if (renderer == null ||
                !renderer.enabled)
                return false;

            string searchable =
                BuildHierarchyName(
                    renderer.transform) +
                " " +
                BuildMaterialNames(
                    renderer);

            if (searchable.Contains("sidewalk") ||
                searchable.Contains("sideway") ||
                searchable.Contains("footpath") ||
                searchable.Contains("pedestrian") ||
                searchable.Contains("curb") ||
                searchable.Contains("kerb") ||
                searchable.Contains("pavement") ||
                searchable.Contains("fence") ||
                searchable.Contains("plaza") ||
                searchable.Contains("building") ||
                searchable.Contains("grass") ||
                searchable.Contains("park"))
                return false;

            return
                searchable.Contains("asphalt") ||
                searchable.Contains("tarmac") ||
                searchable.Contains("lane") ||
                searchable.Contains("highway");
        }

        private static bool IsHighwaySurface(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            string searchable =
                BuildHierarchyName(
                    renderer.transform) +
                " " +
                BuildMaterialNames(
                    renderer);

            return
                searchable.Contains("highway");
        }

        private static Vector3 EstimateRoadDirection(
            Vector3 position,
            Vector3 cityCenter)
        {
            const float radius = 18f;
            float radiusSqr =
                radius *
                radius;

            float xx =
                0f;
            float xz =
                0f;
            float zz =
                0f;
            int count =
                0;

            foreach (DriveableSample sample in driveableSamples)
            {
                Vector3 delta =
                    sample.Position -
                    position;

                delta.y = 0f;

                if (delta.sqrMagnitude < 0.5f ||
                    delta.sqrMagnitude > radiusSqr)
                    continue;

                xx += delta.x * delta.x;
                xz += delta.x * delta.z;
                zz += delta.z * delta.z;
                count++;
            }

            Vector3 direction;

            if (count >= 3)
            {
                float angle =
                    0.5f *
                    Mathf.Atan2(
                        2f * xz,
                        xx - zz);

                direction =
                    new Vector3(
                        Mathf.Cos(angle),
                        0f,
                        Mathf.Sin(angle));
            }
            else
            {
                direction =
                    FlatDirection(
                        cityCenter -
                        position);
            }

            Vector3 towardCenter =
                FlatDirection(
                    cityCenter -
                    position);

            if (towardCenter.sqrMagnitude > 0.01f &&
                Vector3.Dot(
                    direction,
                    towardCenter) < 0f)
            {
                direction =
                    -direction;
            }

            return direction.sqrMagnitude > 0.01f
                ? direction.normalized
                : Vector3.forward;
        }

        private static int CountNearbyDriveableSamples(
            Vector3 position,
            float radius)
        {
            float radiusSqr =
                radius *
                radius;

            int count =
                0;

            foreach (DriveableSample sample in driveableSamples)
            {
                if (HorizontalSqrDistance(
                        sample.Position,
                        position) <=
                    radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }

        private static float DistanceToNearestBuildingXZ(
            Vector3 point)
        {
            if (!TryNearestBuildingPoint(
                    point,
                    out _,
                    out float distance))
                return 30f;

            return distance;
        }

        private static bool TryNearestBuildingPoint(
            Vector3 point,
            out Vector3 nearest,
            out float distance)
        {
            nearest =
                point;
            distance =
                float.PositiveInfinity;

            if (buildingBounds.Count == 0)
                return false;

            foreach (Bounds bounds in buildingBounds)
            {
                Vector3 probe =
                    new(
                        point.x,
                        bounds.center.y,
                        point.z);

                Vector3 closest =
                    bounds.ClosestPoint(
                        probe);

                float candidateDistance =
                    HorizontalDistance(
                        point,
                        closest);

                if (candidateDistance >= distance)
                    continue;

                distance =
                    candidateDistance;

                nearest =
                    closest;
                nearest.y =
                    point.y;
            }

            return
                !float.IsPositiveInfinity(
                    distance);
        }

        private static void CacheBuildingBounds(
            Transform buildingsRoot)
        {
            buildingBounds.Clear();

            if (buildingsRoot == null)
                return;

            Renderer[] renderers =
                buildingsRoot.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    !renderer.enabled)
                    continue;

                Bounds bounds =
                    renderer.bounds;

                if (bounds.size.x < 2f ||
                    bounds.size.z < 2f ||
                    bounds.size.y < 2f)
                    continue;

                buildingBounds.Add(
                    bounds);
            }

            Debug.Log(
                $"Motor City: cached {buildingBounds.Count} building bounds for activity placement.");
        }

        private static void EnsureRoadCollider(
            Renderer renderer)
        {
            if (renderer == null)
                return;

            if (renderer.GetComponent<Collider>() != null)
                return;

            MeshFilter meshFilter =
                renderer.GetComponent<MeshFilter>();

            if (meshFilter != null &&
                meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider =
                    renderer.gameObject
                        .AddComponent<MeshCollider>();

                meshCollider.sharedMesh =
                    meshFilter.sharedMesh;

                meshCollider.convex =
                    false;

                return;
            }

            BoxCollider box =
                renderer.gameObject
                    .AddComponent<BoxCollider>();

            Vector3 center =
                renderer.localBounds.center;

            Vector3 size =
                renderer.localBounds.size;

            float top =
                center.y +
                size.y * 0.5f;

            size.y =
                Mathf.Clamp(
                    size.y,
                    0.08f,
                    0.35f);

            center.y =
                top -
                size.y * 0.5f;

            box.center =
                center;

            box.size =
                size;
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
                    hierarchyName.Contains("shop") ||
                    hierarchyName.Contains("tower");

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

        private static Bounds ResolveGameplayBounds(
            GameObject city,
            Transform cityPart,
            Transform buildingsRoot)
        {
            Transform source =
                buildingsRoot != null
                    ? buildingsRoot
                    : cityPart != null
                        ? cityPart
                        : city.transform;

            Renderer[] renderers =
                source.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
                return GetCityBounds(city);

            Bounds bounds =
                renderers[0].bounds;

            for (int i = 1;
                 i < renderers.Length;
                 i++)
            {
                bounds.Encapsulate(
                    renderers[i].bounds);
            }

            bounds.Expand(
                new Vector3(
                    30f,
                    12f,
                    30f));

            Debug.Log(
                "Motor City: gameplay area locked to " +
                $"{source.name}, center={bounds.center}, size={bounds.size}.");

            return bounds;
        }

        private static Transform FindTransformByName(
            Transform root,
            string targetName)
        {
            if (root == null)
                return null;

            if (string.Equals(
                    root.name,
                    targetName,
                    System.StringComparison.OrdinalIgnoreCase))
                return root;

            foreach (Transform child in root)
            {
                Transform match =
                    FindTransformByName(
                        child,
                        targetName);

                if (match != null)
                    return match;
            }

            return null;
        }

        private static string BuildHierarchyName(
            Transform transform)
        {
            string value =
                string.Empty;

            Transform current =
                transform;

            for (int i = 0;
                 current != null && i < 7;
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

        private static string BuildMaterialNames(
            Renderer renderer)
        {
            string value =
                string.Empty;

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                value +=
                    " " +
                    material.name.ToLowerInvariant();
            }

            return value;
        }

        private static Bounds GetCityBounds(
            GameObject city)
        {
            Renderer[] renderers =
                city.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                return new Bounds(
                    Vector3.zero,
                    new Vector3(
                        400f,
                        1f,
                        400f));
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

        private static void CreateGroundCollider(
            GameObject city)
        {
            GameObject ground =
                new("City Safety Floor Physics");

            Bounds bounds =
                GetCityBounds(city);

            ground.transform.position =
                new Vector3(
                    bounds.center.x,
                    bounds.min.y - 20.2f,
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

        private static Vector3[] BuildFallbackRoute(
            Vector3 center,
            float halfX,
            float halfZ,
            Vector2[] normalizedPoints)
        {
            Vector3[] result =
                new Vector3[normalizedPoints.Length];

            for (int i = 0;
                 i < normalizedPoints.Length;
                 i++)
            {
                Vector2 normalized =
                    normalizedPoints[i];

                result[i] =
                    center +
                    new Vector3(
                        normalized.x * halfX,
                        0f,
                        normalized.y * halfZ);
            }

            return result;
        }

        private static bool ContainsXZ(
            Bounds bounds,
            Vector3 point,
            float inset)
        {
            float minX =
                bounds.min.x +
                inset;
            float maxX =
                bounds.max.x -
                inset;
            float minZ =
                bounds.min.z +
                inset;
            float maxZ =
                bounds.max.z -
                inset;

            if (minX > maxX)
            {
                minX =
                    bounds.min.x;
                maxX =
                    bounds.max.x;
            }

            if (minZ > maxZ)
            {
                minZ =
                    bounds.min.z;
                maxZ =
                    bounds.max.z;
            }

            return
                point.x >= minX &&
                point.x <= maxX &&
                point.z >= minZ &&
                point.z <= maxZ;
        }

        private static bool IntersectsXZ(
            Bounds area,
            Bounds candidate,
            float margin)
        {
            return
                candidate.max.x >=
                    area.min.x - margin &&
                candidate.min.x <=
                    area.max.x + margin &&
                candidate.max.z >=
                    area.min.z - margin &&
                candidate.min.z <=
                    area.max.z + margin;
        }

        private static Vector3 FlatDirection(
            Vector3 value)
        {
            value.y =
                0f;

            return value.sqrMagnitude > 0.001f
                ? value.normalized
                : Vector3.forward;
        }

        private static float HorizontalDistance(
            Vector3 a,
            Vector3 b)
        {
            return
                Mathf.Sqrt(
                    HorizontalSqrDistance(
                        a,
                        b));
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

        private static string FormatRoute(
            Vector3[] route)
        {
            if (route == null ||
                route.Length == 0)
                return "[]";

            string value =
                "[";

            for (int i = 0;
                 i < route.Length;
                 i++)
            {
                if (i > 0)
                    value +=
                        ", ";

                value +=
                    route[i].ToString("F1");
            }

            return
                value +
                "]";
        }
    }
}
