using System;
using UnityEngine;

namespace MotorCity.World
{
    public static class TrafficRoadUtility
    {
        private const float RayHeight = 32f;
        private const float RayDistance = 72f;

        public static bool TryGetRoadPoint(
            Vector3 approximate,
            out Vector3 point,
            out Vector3 normal)
        {
            RaycastHit[] hits =
                Physics.RaycastAll(
                    approximate +
                    Vector3.up * RayHeight,
                    Vector3.down,
                    RayDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore);

            Array.Sort(
                hits,
                (a, b) =>
                    a.distance.CompareTo(
                        b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (!IsRoadHit(
                        hit))
                    continue;

                point =
                    hit.point +
                    Vector3.up * 0.03f;

                normal =
                    hit.normal;

                return true;
            }

            point =
                approximate;

            normal =
                Vector3.up;

            return false;
        }

        public static bool TryFindRoadAround(
            Vector3 center,
            float minimumRadius,
            float maximumRadius,
            int attempts,
            out Vector3 point,
            out Vector3 normal)
        {
            for (int i = 0;
                 i < attempts;
                 i++)
            {
                float angle =
                    UnityEngine.Random.Range(
                        0f,
                        Mathf.PI * 2f);

                float radius =
                    Mathf.Sqrt(
                        UnityEngine.Random.Range(
                            minimumRadius * minimumRadius,
                            maximumRadius * maximumRadius));

                Vector3 candidate =
                    center +
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius);

                if (TryGetRoadPoint(
                        candidate,
                        out point,
                        out normal))
                {
                    return true;
                }
            }

            point =
                center;

            normal =
                Vector3.up;

            return false;
        }

        public static bool TryChooseForwardRoadTarget(
            Vector3 position,
            Vector3 forward,
            float lookAhead,
            out Vector3 target)
        {
            forward.y =
                0f;

            if (forward.sqrMagnitude <
                0.01f)
            {
                forward =
                    Vector3.forward;
            }

            forward.Normalize();

            float[] angleOffsets =
            {
                0f,
                -12f,
                12f,
                -25f,
                25f,
                -42f,
                42f,
                -62f,
                62f,
                -88f,
                88f
            };

            float bestScore =
                float.NegativeInfinity;

            Vector3 best =
                position +
                forward * lookAhead;

            bool found =
                false;

            foreach (float angle in
                     angleOffsets)
            {
                Vector3 direction =
                    Quaternion.Euler(
                        0f,
                        angle,
                        0f) *
                    forward;

                Vector3 firstProbe =
                    position +
                    direction *
                    lookAhead;

                if (!TryGetRoadPoint(
                        firstProbe,
                        out Vector3 firstRoad,
                        out _))
                {
                    continue;
                }

                Vector3 secondProbe =
                    position +
                    direction *
                    (lookAhead * 1.7f);

                bool hasSecond =
                    TryGetRoadPoint(
                        secondProbe,
                        out Vector3 secondRoad,
                        out _);

                float forwardness =
                    Vector3.Dot(
                        forward,
                        direction);

                float score =
                    forwardness * 8f -
                    Mathf.Abs(angle) * 0.018f;

                if (hasSecond)
                    score += 4f;

                // At junctions occasionally allow a real turn instead of
                // making every traffic car drive straight forever.
                if (Mathf.Abs(angle) >= 55f &&
                    UnityEngine.Random.value < 0.08f)
                {
                    score += 7f;
                }

                if (score <=
                    bestScore)
                    continue;

                bestScore =
                    score;

                best =
                    hasSecond
                        ? Vector3.Lerp(
                            firstRoad,
                            secondRoad,
                            0.35f)
                        : firstRoad;

                found =
                    true;
            }

            target =
                best;

            return found;
        }

        public static bool IsRoadCollider(
            Collider collider)
        {
            if (collider == null)
                return false;

            string path =
                GetHierarchyPath(
                        collider.transform)
                    .ToLowerInvariant();

            if (path.Contains(
                    "collider road") ||
                path.Contains(
                    "highway"))
            {
                return true;
            }

            Renderer renderer =
                collider.GetComponent<Renderer>();

            if (renderer == null)
                return false;

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string name =
                    material.name.ToLowerInvariant();

                if ((name.Contains("road") ||
                     name.Contains("highway")) &&
                    !name.Contains("grass"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRoadHit(
            RaycastHit hit)
        {
            MeshCollider meshCollider =
                hit.collider as MeshCollider;

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
            {
                return false;
            }

            Renderer renderer =
                hit.collider.GetComponent<Renderer>();

            if (renderer == null)
            {
                return
                    path.Contains(
                        "collider road");
            }

            Material material =
                ResolveTriangleMaterial(
                    hit,
                    renderer,
                    meshCollider);

            if (material == null)
                return false;

            string materialName =
                material.name.ToLowerInvariant();

            return
                (materialName.Contains("road") &&
                 !materialName.Contains("grass")) ||
                materialName.Contains("highway");
        }

        private static Material ResolveTriangleMaterial(
            RaycastHit hit,
            Renderer renderer,
            MeshCollider meshCollider)
        {
            if (renderer == null)
                return null;

            Material[] materials =
                renderer.sharedMaterials;

            if (materials == null ||
                materials.Length == 0)
                return null;

            if (meshCollider == null ||
                meshCollider.sharedMesh == null ||
                hit.triangleIndex < 0)
            {
                return materials[0];
            }

            Mesh mesh =
                meshCollider.sharedMesh;

            int triangleCursor =
                0;

            int count =
                Mathf.Min(
                    mesh.subMeshCount,
                    materials.Length);

            for (int subMesh = 0;
                 subMesh < count;
                 subMesh++)
            {
                if (mesh.GetTopology(
                        subMesh) !=
                    MeshTopology.Triangles)
                {
                    continue;
                }

                int triangleCount =
                    (int)mesh.GetIndexCount(
                        subMesh) /
                    3;

                if (hit.triangleIndex >=
                        triangleCursor &&
                    hit.triangleIndex <
                        triangleCursor +
                        triangleCount)
                {
                    return
                        materials[subMesh];
                }

                triangleCursor +=
                    triangleCount;
            }

            return
                materials[0];
        }

        private static string GetHierarchyPath(
            Transform item)
        {
            if (item == null)
                return string.Empty;

            string path =
                item.name;

            Transform current =
                item.parent;

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
