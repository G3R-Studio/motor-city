using System;
using UnityEngine;

namespace MotorCity.World
{
    public static class CityCollisionUtility
    {
        private const string SafetyFloorName =
            "MotorCity_CollisionSafetyFloor";

        public readonly struct Result
        {
            public int DisabledStreetPropColliders { get; }
            public int AddedBuildingMeshColliders { get; }
            public bool SafetyFloorReady { get; }

            public Result(
                int disabledStreetPropColliders,
                int addedBuildingMeshColliders,
                bool safetyFloorReady)
            {
                DisabledStreetPropColliders =
                    disabledStreetPropColliders;

                AddedBuildingMeshColliders =
                    addedBuildingMeshColliders;

                SafetyFloorReady =
                    safetyFloorReady;
            }
        }

        public static Result Prepare(
            GameObject cityRoot)
        {
            if (cityRoot == null)
            {
                return
                    new Result(
                        0,
                        0,
                        false);
            }

            int disabled =
                DisablePassThroughStreetPropColliders(
                    cityRoot);

            int buildings =
                EnsureBuildingMeshColliders(
                    cityRoot);

            disabled +=
                RemoveRoadMarkBColliders(
                    cityRoot);

            bool safetyFloor =
                EnsureSafetyFloor(
                    cityRoot);

            return
                new Result(
                    disabled,
                    buildings,
                    safetyFloor);
        }

        public static int DisablePassThroughStreetPropColliders(
            GameObject cityRoot)
        {
            if (cityRoot == null)
                return 0;

            int disabled =
                0;

            foreach (Collider collider in
                     cityRoot.GetComponentsInChildren<Collider>(
                         true))
            {
                if (collider == null)
                    continue;

                if (!IsPassThroughStreetPropHierarchy(
                        collider.transform,
                        cityRoot.transform))
                    continue;

                if (collider.enabled)
                {
                    collider.enabled =
                        false;

                    disabled++;
                }
            }

            return disabled;
        }

        public static int RemoveRoadMarkBColliders(
            GameObject cityRoot)
        {
            if (cityRoot == null)
                return 0;

            int removed =
                0;

            foreach (Collider collider in
                     cityRoot.GetComponentsInChildren<Collider>(
                         true))
            {
                if (collider == null ||
                    !IsRoadMarkBObject(
                        collider.transform,
                        cityRoot.transform))
                    continue;

                collider.enabled =
                    false;

                UnityEngine.Object.Destroy(
                    collider);

                removed++;
            }

            return removed;
        }

        private static bool IsRoadMarkBObject(
            Transform item,
            Transform cityRoot)
        {
            if (item == null)
                return false;

            Transform current =
                item;

            while (current != null)
            {
                string normalized =
                    NormalizeName(
                        current.name);

                if (normalized.StartsWith(
                        "roadmark"))
                    return true;

                MeshFilter filter =
                    current.GetComponent<MeshFilter>();

                if (filter != null &&
                    filter.sharedMesh != null &&
                    NormalizeName(
                            filter.sharedMesh.name)
                        .StartsWith(
                            "roadmark"))
                {
                    return true;
                }

                Renderer renderer =
                    current.GetComponent<Renderer>();

                if (renderer != null)
                {
                    foreach (Material material in
                             renderer.sharedMaterials)
                    {
                        if (material == null)
                            continue;

                        if (NormalizeName(
                                material.name)
                            .StartsWith(
                                "roadmark"))
                        {
                            return true;
                        }
                    }
                }

                if (current == cityRoot)
                    break;

                current =
                    current.parent;
            }

            return false;
        }

        private static int EnsureBuildingMeshColliders(
            GameObject cityRoot)
        {
            int added =
                0;

            foreach (MeshFilter filter in
                     cityRoot.GetComponentsInChildren<MeshFilter>(
                         true))
            {
                if (filter == null ||
                    filter.sharedMesh == null)
                    continue;

                if (!ShouldHaveBuildingCollision(
                        filter.transform,
                        cityRoot.transform))
                    continue;

                Collider existing =
                    filter.GetComponent<Collider>();

                if (existing != null)
                {
                    if (!existing.isTrigger)
                    {
                        existing.enabled =
                            true;
                    }

                    continue;
                }

                MeshCollider meshCollider =
                    filter.gameObject.AddComponent<MeshCollider>();

                meshCollider.sharedMesh =
                    filter.sharedMesh;

                meshCollider.convex =
                    false;

                added++;
            }

            return added;
        }

        private static bool EnsureSafetyFloor(
            GameObject cityRoot)
        {
            Transform existing =
                cityRoot.transform.Find(
                    SafetyFloorName);

            GameObject floor;

            if (existing != null)
            {
                floor =
                    existing.gameObject;
            }
            else
            {
                floor =
                    new GameObject(
                        SafetyFloorName);

                floor.transform.SetParent(
                    cityRoot.transform,
                    false);
            }

            if (!TryCalculateCityHorizontalBounds(
                    cityRoot,
                    out Bounds bounds))
                return false;

            BoxCollider box =
                floor.GetComponent<BoxCollider>();

            if (box == null)
            {
                box =
                    floor.AddComponent<BoxCollider>();
            }

            float floorTop =
                bounds.min.y -
                6f;

            floor.transform.position =
                new Vector3(
                    bounds.center.x,
                    floorTop -
                    1f,
                    bounds.center.z);

            floor.transform.rotation =
                Quaternion.identity;

            floor.transform.localScale =
                Vector3.one;

            box.center =
                Vector3.zero;

            box.size =
                new Vector3(
                    Mathf.Max(
                        20f,
                        bounds.size.x +
                        12f),
                    2f,
                    Mathf.Max(
                        20f,
                        bounds.size.z +
                        12f));

            box.isTrigger =
                false;

            box.enabled =
                true;

            return true;
        }

        private static bool TryCalculateCityHorizontalBounds(
            GameObject cityRoot,
            out Bounds bounds)
        {
            Renderer[] renderers =
                cityRoot.GetComponentsInChildren<Renderer>(
                    true);

            bool initialized =
                false;

            bounds =
                default;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (IsPassThroughStreetPropHierarchy(
                        renderer.transform,
                        cityRoot.transform))
                    continue;

                if (!initialized)
                {
                    bounds =
                        renderer.bounds;

                    initialized =
                        true;
                }
                else
                {
                    bounds.Encapsulate(
                        renderer.bounds);
                }
            }

            return initialized;
        }

        private static bool ShouldHaveBuildingCollision(
            Transform item,
            Transform cityRoot)
        {
            if (item == null)
                return false;

            if (IsPassThroughStreetPropHierarchy(
                    item,
                    cityRoot))
                return false;

            string path =
                GetRelativeHierarchyPath(
                        item,
                        cityRoot)
                    .ToLowerInvariant();

            if (!path.Contains(
                    "/buildings/"))
                return false;

            if (path.Contains(
                    "/highway/") ||
                path.Contains(
                    "/high-way/") ||
                path.Contains(
                    "/high_way/") ||
                path.Contains(
                    "/hw-") ||
                path.Contains(
                    "/hwy-"))
                return false;

            string[] excluded =
            {
                "/trees/",
                "/tress/",
                "tree-",
                "treeb-",
                "vegetation",
                "foliage",
                "fern-",
                "palm-",
                "flower",
                "garden-",
                "park-",
                "/cars/",
                "/vehicles/",
                "tempra",
                "vesta",
                "busstop",
                "bus-stop"
            };

            foreach (string token in excluded)
            {
                if (path.Contains(
                        token))
                    return false;
            }

            Renderer renderer =
                item.GetComponent<Renderer>();

            if (renderer == null)
                return false;

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string materialName =
                    material.name.ToLowerInvariant();

                if (materialName.Contains(
                        "road") ||
                    materialName.Contains(
                        "highway"))
                {
                    return false;
                }
            }

            Bounds bounds =
                renderer.bounds;

            if (bounds.size.sqrMagnitude <
                0.04f)
                return false;

            return true;
        }

        public static bool IsPassThroughStreetPropHierarchy(
            Transform item,
            Transform cityRoot = null)
        {
            Transform current =
                item;

            while (current != null)
            {
                if (current == cityRoot)
                    break;

                string normalized =
                    NormalizeName(
                        current.name);

                if (IsPassThroughStreetPropName(
                        normalized))
                    return true;

                if (normalized.StartsWith(
                        "roadmarkb"))
                    return true;

                current =
                    current.parent;
            }

            return false;
        }

        private static bool IsPassThroughStreetPropName(
            string normalized)
        {
            if (string.IsNullOrWhiteSpace(
                    normalized))
                return false;

            return
                normalized.StartsWith(
                    "streetlight") ||
                normalized.StartsWith(
                    "parklamp") ||
                normalized.StartsWith(
                    "trafficlight") ||
                normalized.Contains(
                    "hydrant") ||
                normalized.Contains(
                    "trash") ||
                normalized.StartsWith(
                    "parkbench") ||
                normalized.StartsWith(
                    "bench") ||
                normalized.Contains(
                    "trafficsign") ||
                normalized.Contains(
                    "roadsign") ||
                normalized.Contains(
                    "streetsign") ||
                normalized.Contains(
                    "waysign") ||
                normalized.Contains(
                    "signpost") ||
                normalized.StartsWith(
                    "sign") ||
                normalized.Contains(
                    "bollard") ||
                normalized.StartsWith(
                    "pole");
        }

        private static string NormalizeName(
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

            return result.ToString();
        }

        private static string GetRelativeHierarchyPath(
            Transform item,
            Transform root)
        {
            string path =
                item != null
                    ? item.name
                    : string.Empty;

            Transform current =
                item != null
                    ? item.parent
                    : null;

            while (current != null &&
                   current != root)
            {
                path =
                    current.name +
                    "/" +
                    path;

                current =
                    current.parent;
            }

            return
                "/" +
                path;
        }
    }
}
