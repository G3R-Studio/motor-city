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
            ResolveGameplayLayout(city);

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
                    HorizontalSqrDistance(
                        point,
                        flatApprox);

                if (distance >= bestDistance)
                    continue;

                bestDistance =
                    distance;

                best =
                    point;
            }

            return best;
        }

        private static void ResolveGameplayLayout(
            GameObject city)
        {
            Bounds cityBounds =
                GetCityBounds(city);

            Vector3 center =
                new(
                    cityBounds.center.x,
                    0f,
                    cityBounds.center.z);

            float halfX =
                Mathf.Max(
                    80f,
                    cityBounds.extents.x);

            float halfZ =
                Mathf.Max(
                    80f,
                    cityBounds.extents.z);

            if (roadPoints.Count == 0)
            {
                Debug.LogWarning(
                    "Motor City: no road geometry was detected in the Versatile city. " +
                    "Gameplay points will use city-relative fallback coordinates.");

                PlayerSpawnPoint =
                    center +
                    new Vector3(
                        -halfX * 0.18f,
                        0f,
                        -halfZ * 0.52f);

                PlayerSpawnRotation =
                    Quaternion.LookRotation(
                        (center - PlayerSpawnPoint).normalized,
                        Vector3.up);

                GaragePoint =
                    center +
                    new Vector3(
                        -halfX * 0.56f,
                        0f,
                        halfZ * 0.12f);

                DriftChallengePoint =
                    center +
                    new Vector3(
                        halfX * 0.42f,
                        0f,
                        halfZ * 0.06f);

                deliveryRoute =
                    BuildFallbackRoute(
                        center,
                        halfX,
                        halfZ,
                        DeliveryNormalizedLayout);

                sprintRoute =
                    BuildFallbackRoute(
                        center,
                        halfX,
                        halfZ,
                        SprintNormalizedLayout);

                return;
            }

            var reserved =
                new List<Vector3>();

            Vector3 preferredSpawn =
                new(
                    120f,
                    center.y,
                    30f);

            Physics.SyncTransforms();

            PlayerSpawnPoint =
                FindDriveableSpawnNear(
                    preferredSpawn);

            reserved.Add(
                PlayerSpawnPoint);

            PlayerSpawnRotation =
                ResolveRoadRotation(
                    PlayerSpawnPoint,
                    center);

            GaragePoint =
                SelectRoadPoint(
                    center,
                    halfX,
                    halfZ,
                    new Vector2(-0.58f, 0.08f),
                    reserved,
                    Mathf.Min(halfX, halfZ) * 0.18f);

            reserved.Add(
                GaragePoint);

            DriftChallengePoint =
                SelectRoadPoint(
                    center,
                    halfX,
                    halfZ,
                    new Vector2(0.46f, 0.12f),
                    reserved,
                    Mathf.Min(halfX, halfZ) * 0.22f);

            reserved.Add(
                DriftChallengePoint);

            deliveryRoute =
                BuildRoadRoute(
                    center,
                    halfX,
                    halfZ,
                    DeliveryNormalizedLayout,
                    reserved,
                    Mathf.Min(halfX, halfZ) * 0.12f);

            foreach (Vector3 point in deliveryRoute)
                reserved.Add(point);

            sprintRoute =
                BuildRoadRoute(
                    center,
                    halfX,
                    halfZ,
                    SprintNormalizedLayout,
                    reserved,
                    Mathf.Min(halfX, halfZ) * 0.09f);

            Debug.Log(
                "Motor City: gameplay coordinates rebuilt for Versatile Demo City. " +
                $"Spawn={PlayerSpawnPoint}, " +
                $"Garage={GaragePoint}, " +
                $"Drift={DriftChallengePoint}, " +
                $"Delivery={FormatRoute(deliveryRoute)}, " +
                $"Sprint={FormatRoute(sprintRoute)}");
        }

        private static readonly Vector2[] DeliveryNormalizedLayout =
        {
            new(-0.68f, -0.34f),
            new(-0.28f, -0.18f),
            new(0.08f, -0.02f),
            new(0.58f, 0.20f),
            new(0.26f, 0.62f),
            new(-0.42f, 0.52f)
        };

        private static readonly Vector2[] SprintNormalizedLayout =
        {
            new(-0.78f, -0.68f),
            new(-0.80f, 0.18f),
            new(-0.48f, 0.74f),
            new(0.18f, 0.80f),
            new(0.76f, 0.52f),
            new(0.80f, -0.20f),
            new(0.42f, -0.76f),
            new(-0.34f, -0.80f)
        };

        private static Vector3[] BuildRoadRoute(
            Vector3 center,
            float halfX,
            float halfZ,
            Vector2[] normalizedPoints,
            List<Vector3> reserved,
            float minimumSpacing)
        {
            Vector3[] result =
                new Vector3[normalizedPoints.Length];

            var localReserved =
                new List<Vector3>(reserved);

            for (int i = 0;
                 i < normalizedPoints.Length;
                 i++)
            {
                result[i] =
                    SelectRoadPoint(
                        center,
                        halfX,
                        halfZ,
                        normalizedPoints[i],
                        localReserved,
                        minimumSpacing);

                localReserved.Add(
                    result[i]);
            }

            return result;
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

        private static Vector3 FindDriveableSpawnNear(
            Vector3 preferred)
        {
            const float searchRadius = 26f;
            const float step = 2f;
            const float rayHeight = 80f;
            const float maxRayDistance = 180f;

            Vector3 best =
                SelectNearestRoadPoint(
                    preferred,
                    null,
                    0f);

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
                    Vector3 origin =
                        new(
                            preferred.x + x,
                            preferred.y + rayHeight,
                            preferred.z + z);

                    if (!Physics.Raycast(
                            origin,
                            Vector3.down,
                            out RaycastHit hit,
                            maxRayDistance,
                            Physics.DefaultRaycastLayers,
                            QueryTriggerInteraction.Ignore))
                        continue;

                    Renderer renderer =
                        hit.collider != null
                            ? hit.collider.GetComponent<Renderer>()
                            : null;

                    if (renderer == null)
                        renderer =
                            hit.collider != null
                                ? hit.collider.GetComponentInParent<Renderer>()
                                : null;

                    if (!IsDriveableSurface(renderer))
                        continue;

                    float distance =
                        HorizontalSqrDistance(
                            hit.point,
                            preferred);

                    if (distance >= bestDistance)
                        continue;

                    bestDistance =
                        distance;

                    best =
                        hit.point;
                }
            }

            if (float.IsPositiveInfinity(bestDistance))
            {
                Debug.LogWarning(
                    "Motor City: no asphalt raycast surface was found near the preferred spawn; " +
                    $"falling back to road sample {best}.");
            }
            else
            {
                Debug.Log(
                    $"Motor City: driveable spawn surface found at {best} near preferred {preferred}.");
            }

            return best;
        }

        private static bool IsDriveableSurface(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            string hierarchy =
                BuildHierarchyName(
                    renderer.transform);

            string materials =
                BuildMaterialNames(
                    renderer);

            string searchable =
                hierarchy +
                " " +
                materials;

            if (searchable.Contains("sidewalk") ||
                searchable.Contains("sideway") ||
                searchable.Contains("footpath") ||
                searchable.Contains("pedestrian") ||
                searchable.Contains("curb") ||
                searchable.Contains("kerb") ||
                searchable.Contains("pavement") ||
                searchable.Contains("fence") ||
                searchable.Contains("plaza"))
                return false;

            return
                materials.Contains("asphalt") ||
                materials.Contains("tarmac") ||
                materials.Contains("lane") ||
                searchable.Contains("highway");
        }

        private static Vector3 SelectNearestRoadPoint(
            Vector3 target,
            List<Vector3> reserved,
            float minimumSpacing)
        {
            if (roadPoints.Count == 0)
                return target;

            float minimumSpacingSqr =
                minimumSpacing *
                minimumSpacing;

            Vector3 best =
                roadPoints[0];

            float bestScore =
                float.PositiveInfinity;

            foreach (Vector3 point in roadPoints)
            {
                float score =
                    HorizontalSqrDistance(
                        point,
                        target);

                if (reserved != null &&
                    minimumSpacing > 0f)
                {
                    foreach (Vector3 used in reserved)
                    {
                        if (HorizontalSqrDistance(
                                point,
                                used) <
                            minimumSpacingSqr)
                        {
                            score +=
                                minimumSpacingSqr * 8f;
                            break;
                        }
                    }
                }

                if (score >= bestScore)
                    continue;

                bestScore =
                    score;

                best =
                    point;
            }

            return best;
        }

        private static Vector3 SelectRoadPoint(
            Vector3 center,
            float halfX,
            float halfZ,
            Vector2 normalized,
            List<Vector3> reserved,
            float minimumSpacing)
        {
            Vector3 target =
                center +
                new Vector3(
                    normalized.x * halfX,
                    0f,
                    normalized.y * halfZ);

            float minimumSpacingSqr =
                minimumSpacing *
                minimumSpacing;

            Vector3 best =
                roadPoints[0];

            float bestScore =
                float.PositiveInfinity;

            foreach (Vector3 point in roadPoints)
            {
                float score =
                    HorizontalSqrDistance(
                        point,
                        target);

                if (reserved != null &&
                    minimumSpacing > 0f)
                {
                    bool tooClose =
                        false;

                    foreach (Vector3 used in reserved)
                    {
                        if (HorizontalSqrDistance(
                                point,
                                used) <
                            minimumSpacingSqr)
                        {
                            tooClose =
                                true;
                            break;
                        }
                    }

                    if (tooClose)
                        score +=
                            minimumSpacingSqr * 8f;
                }

                if (score >= bestScore)
                    continue;

                bestScore =
                    score;

                best =
                    point;
            }

            return best;
        }

        private static Quaternion ResolveRoadRotation(
            Vector3 position,
            Vector3 cityCenter)
        {
            if (roadAnchors.Count == 0)
            {
                Vector3 towardCenter =
                    cityCenter -
                    position;

                towardCenter.y = 0f;

                return towardCenter.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(
                        towardCenter.normalized,
                        Vector3.up)
                    : Quaternion.identity;
            }

            float bestDistance =
                float.PositiveInfinity;

            Vector3 bestDirection =
                Vector3.forward;

            foreach (RoadAnchor anchor in roadAnchors)
            {
                float distance =
                    HorizontalSqrDistance(
                        anchor.Position,
                        position);

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

            Vector3 towardCenterDirection =
                cityCenter -
                position;

            towardCenterDirection.y = 0f;

            if (towardCenterDirection.sqrMagnitude > 0.01f &&
                Vector3.Dot(
                    bestDirection,
                    towardCenterDirection) < 0f)
            {
                bestDirection =
                    -bestDirection;
            }

            return Quaternion.LookRotation(
                bestDirection.normalized,
                Vector3.up);
        }

        private static void CacheRoadPoints(
            GameObject city)
        {
            Renderer[] renderers =
                city.GetComponentsInChildren<Renderer>(true);

            Bounds cityBounds =
                GetCityBounds(city);

            float cityGroundY =
                cityBounds.min.y;

            // Pass 1: actual asphalt/lane surfaces only. Versatile Studio has
            // meshes/materials named "road_sideway_fences" whose bounds include
            // the sidewalk; those must never drive player spawn placement.
            foreach (Renderer renderer in renderers)
            {
                if (!IsUsableRoadRenderer(
                        renderer,
                        cityGroundY,
                        RoadSearchMode.AsphaltOnly))
                    continue;

                EnsureRoadCollider(
                    renderer);

                AddRoadRendererSamples(
                    renderer);
            }

            if (roadPoints.Count > 0)
            {
                Debug.Log(
                    $"Motor City: detected {roadPoints.Count} asphalt road samples.");
                return;
            }

            // Pass 2: generic explicit road names, still excluding sideway/fence
            // meshes and pedestrian surfaces.
            foreach (Renderer renderer in renderers)
            {
                if (!IsUsableRoadRenderer(
                        renderer,
                        cityGroundY,
                        RoadSearchMode.NamedRoad))
                    continue;

                EnsureRoadCollider(
                    renderer);

                AddRoadRendererSamples(
                    renderer);
            }

            if (roadPoints.Count > 0)
            {
                Debug.LogWarning(
                    $"Motor City: asphalt surfaces were not found; using {roadPoints.Count} named road samples.");
                return;
            }

            // Pass 3: geometry-only fallback for an unexpectedly named asset.
            foreach (Renderer renderer in renderers)
            {
                if (!IsUsableRoadRenderer(
                        renderer,
                        cityGroundY,
                        RoadSearchMode.GeometryFallback))
                    continue;

                EnsureRoadCollider(
                    renderer);

                AddRoadRendererSamples(
                    renderer);
            }

            if (roadPoints.Count == 0)
            {
                Debug.LogWarning(
                    "Motor City: road detector found no suitable renderer surfaces.");
            }
            else
            {
                Debug.LogWarning(
                    $"Motor City: using {roadPoints.Count} geometric road samples.");
            }
        }

        private enum RoadSearchMode
        {
            AsphaltOnly,
            NamedRoad,
            GeometryFallback
        }

        private static bool IsUsableRoadRenderer(
            Renderer renderer,
            float cityGroundY,
            RoadSearchMode mode)
        {
            if (renderer == null ||
                !renderer.enabled)
                return false;

            string hierarchy =
                BuildHierarchyName(
                    renderer.transform);

            string materials =
                BuildMaterialNames(
                    renderer);

            string searchable =
                hierarchy +
                " " +
                materials;

            bool rejected =
                searchable.Contains("sidewalk") ||
                searchable.Contains("sideway") ||
                searchable.Contains("footpath") ||
                searchable.Contains("pedestrian") ||
                searchable.Contains("curb") ||
                searchable.Contains("kerb") ||
                searchable.Contains("plaza") ||
                searchable.Contains("pavement") ||
                searchable.Contains("walkway") ||
                searchable.Contains("stairs") ||
                searchable.Contains("step") ||
                searchable.Contains("fence") ||
                searchable.Contains("building") ||
                searchable.Contains("grass") ||
                searchable.Contains("park");

            if (rejected)
                return false;

            bool asphaltSurface =
                materials.Contains("asphalt") ||
                materials.Contains("tarmac") ||
                materials.Contains("lane") ||
                hierarchy.Contains("asphalt");

            if (mode == RoadSearchMode.AsphaltOnly)
                return asphaltSurface;

            bool explicitRoad =
                asphaltSurface ||
                searchable.Contains("highway") ||
                searchable.Contains("street") ||
                searchable.Contains("intersection") ||
                searchable.Contains("road");

            if (mode == RoadSearchMode.NamedRoad)
                return explicitRoad;

            Bounds bounds =
                renderer.bounds;

            return
                bounds.size.y <= 0.30f &&
                Mathf.Max(
                    bounds.size.x,
                    bounds.size.z) >= 18f &&
                Mathf.Min(
                    bounds.size.x,
                    bounds.size.z) >= 5f &&
                bounds.min.y <= cityGroundY + 2.0f;
        }

        private static void AddRoadRendererSamples(
            Renderer renderer)
        {
            Bounds bounds =
                renderer.bounds;

            Vector3 center =
                new(
                    bounds.center.x,
                    bounds.max.y,
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

            AddRoadAnchor(
                center,
                direction);

            float longExtent =
                Mathf.Max(
                    bounds.extents.x,
                    bounds.extents.z);

            int sampleCount =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        longExtent / 24f),
                    1,
                    8);

            for (int i = 1;
                 i <= sampleCount;
                 i++)
            {
                float distance =
                    longExtent *
                    (i /
                     (float)(sampleCount + 1));

                AddRoadAnchor(
                    center +
                    direction *
                    distance,
                    direction);

                AddRoadAnchor(
                    center -
                    direction *
                    distance,
                    direction);
            }
        }

        private static void AddRoadAnchor(
            Vector3 position,
            Vector3 direction)
        {
            roadPoints.Add(
                position);

            roadAnchors.Add(
                new RoadAnchor
                {
                    Position = position,
                    Direction = direction
                });
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

            // Fallback for road renderers without a MeshFilter.
            // Keep the collider thin so it follows the visible surface
            // instead of creating a large invisible wall.
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

        private static string BuildHierarchyName(
            Transform transform)
        {
            string value =
                string.Empty;

            Transform current =
                transform;

            for (int i = 0;
                 current != null && i < 6;
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
                new("City Ground Physics");

            Bounds bounds =
                GetCityBounds(city);

            ground.name =
                "City Safety Floor Physics";

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

        private static float HorizontalSqrDistance(
            Vector3 a,
            Vector3 b)
        {
            float x =
                a.x - b.x;

            float z =
                a.z - b.z;

            return x * x +
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
                    value += ", ";

                value +=
                    route[i].ToString("F1");
            }

            return value +
                   "]";
        }
    }
}
