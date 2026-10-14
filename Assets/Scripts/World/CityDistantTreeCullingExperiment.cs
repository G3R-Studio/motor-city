using System;
using System.Collections.Generic;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Experimental, reversible distant-tree rendering culler.
    /// This component is Editor A/B only until specifically validated for WebGL.
    /// It never changes GameObject activation, colliders, mesh assets or physics.
    /// </summary>
    public sealed class CityDistantTreeCullingExperiment : MonoBehaviour
    {
        private const int DiscoveryBudget = 256;
        private const int UpdateBudget = 256;
        private const float ReferenceScreenHeight = 1080f;
        private const float MaxTreeRadius = 12f;

        private struct Candidate
        {
            public MeshRenderer Renderer;
            public Vector3 Center;
            public float Radius;
            public long Triangles;
            public bool IsGroundCover;
            public bool HiddenByUs;
        }

        private readonly List<Candidate> candidates = new List<Candidate>(2048);
        private MeshRenderer[] discovery = Array.Empty<MeshRenderer>();
        private Camera observer;
        private int discoveryCursor;
        private int cursor;
        private bool announced;
        private bool refresh = true;
        private Vector3 sweepPosition;

        // Toggle only while the component is disabled. OnDisable
        // restores all renderers before changing categories.
        public bool IncludeWoody { get; set; } = true;
        public bool IncludeGroundCover { get; set; }
        public int WoodyCandidateCount { get; private set; }
        public int GroundCoverCandidateCount { get; private set; }
        public int HiddenWoodyCount { get; private set; }
        public int HiddenGroundCoverCount { get; private set; }
        public long HiddenWoodyTriangles { get; private set; }
        public long HiddenGroundCoverTriangles { get; private set; }

        public int CandidateCount => candidates.Count;
        public int HiddenCount { get; private set; }
        public long EligibleTriangleReferences { get; private set; }
        public long HiddenTriangleReferences { get; private set; }
        public bool ScanReady => announced;

        public void Initialize(GameObject city, Camera camera)
        {
            RestoreAll();
            candidates.Clear();
            observer = camera;
            discovery = city == null
                ? Array.Empty<MeshRenderer>()
                : city.GetComponentsInChildren<MeshRenderer>(true);
            discoveryCursor = 0;
            cursor = 0;
            announced = false;
            EligibleTriangleReferences = 0;
            WoodyCandidateCount = 0;
            GroundCoverCandidateCount = 0;
            refresh = true;
        }

        public void RequestRefresh()
        {
            cursor = 0;
            refresh = true;
        }

        private void OnEnable() => RequestRefresh();
        private void OnDisable() => RestoreAll();
        private void OnDestroy() => RestoreAll();

        public void RestoreAll()
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate item = candidates[i];
                if (item.HiddenByUs && item.Renderer != null)
                    item.Renderer.forceRenderingOff = false;
                item.HiddenByUs = false;
                candidates[i] = item;
            }

            HiddenCount = 0;
            HiddenTriangleReferences = 0;
            HiddenWoodyCount = 0;
            HiddenGroundCoverCount = 0;
            HiddenWoodyTriangles = 0;
            HiddenGroundCoverTriangles = 0;
            RequestRefresh();
        }

        /// <summary>
        /// Shared ownership rule: the tiny-detail culler must not take
        /// renderer ownership of these exact decorative tree meshes.
        /// Only pure Trees-01 renderer materials are eligible; mixed
        /// atlas/glass/architecture submeshes remain fully rendered.
        /// </summary>
        public static bool IsTreeRenderer(MeshRenderer renderer)
        {
            if (renderer == null)
                return false;
            string name = renderer.name;
            return name != null &&
                (name.StartsWith("Tree", StringComparison.OrdinalIgnoreCase) ||
                 name.StartsWith("Palm", StringComparison.OrdinalIgnoreCase)) &&
                HasOnlyTreeMaterials(renderer);
        }

        public static bool IsGroundCoverRenderer(MeshRenderer renderer)
        {
            if (renderer == null)
                return false;
            string name = renderer.name;
            return name != null &&
                (name.StartsWith("Fern", StringComparison.OrdinalIgnoreCase) ||
                 name.StartsWith("Vegetation", StringComparison.OrdinalIgnoreCase) ||
                 name.StartsWith("Plant", StringComparison.OrdinalIgnoreCase)) &&
                HasOnlyTreeMaterials(renderer);
        }

        public static bool IsExperimentalVegetationRenderer(
            MeshRenderer renderer)
        {
            return IsTreeRenderer(renderer) || IsGroundCoverRenderer(renderer);
        }

        private static bool HasOnlyTreeMaterials(MeshRenderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
                return false;
            foreach (Material material in materials)
            {
                if (material == null ||
                    !material.name.StartsWith(
                        "Trees-01", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        private void Update()
        {
            if (observer == null)
                observer = Camera.main;
            if (observer == null || !observer.isActiveAndEnabled)
                return;

            Vector3 cameraPosition = observer.transform.position;
            if (!announced)
            {
                int end = Mathf.Min(discovery.Length,
                    discoveryCursor + DiscoveryBudget);
                for (int i = discoveryCursor; i < end; i++)
                    Discover(discovery[i]);
                discoveryCursor = end;
                if (discoveryCursor < discovery.Length)
                    return;

                announced = true;
                Debug.Log("[MotorCity][Trees] Scan ready: " +
                    candidates.Count + " pure Trees-01 renderers (" +
                    WoodyCandidateCount + " trees/palms, " +
                    GroundCoverCandidateCount + " ground plants), " +
                    EligibleTriangleReferences + " triangle references. " +
                    "Tree source geometry is unchanged.");
                discovery = Array.Empty<MeshRenderer>();
                RequestRefresh();
            }

            if (candidates.Count == 0)
                return;

            if (cursor == 0)
            {
                if (!refresh &&
                    (cameraPosition - sweepPosition).sqrMagnitude < 100f)
                    return;
                sweepPosition = cameraPosition;
                refresh = false;
            }

            float minimumDistance;
            float maximumPixels;
            switch (MotorCityQualityRuntime.CurrentPreset)
            {
                case MotorCityQualityPreset.Low:
                    minimumDistance = 340f;
                    maximumPixels = 16f;
                    break;
                case MotorCityQualityPreset.Medium:
                    minimumDistance = 420f;
                    maximumPixels = 14f;
                    break;
                default:
                    minimumDistance = 500f;
                    maximumPixels = 12f;
                    break;
            }

            float fov = Mathf.Max(1f, observer.fieldOfView);
            float tanHalfFov = Mathf.Max(0.001f,
                Mathf.Tan(fov * Mathf.Deg2Rad * .5f));

            int endIndex = Mathf.Min(cursor + UpdateBudget, candidates.Count);
            for (int i = cursor; i < endIndex; i++)
            {
                Candidate item = candidates[i];
                if (item.Renderer == null ||
                    (item.IsGroundCover && !IncludeGroundCover) ||
                    (!item.IsGroundCover && !IncludeWoody))
                    continue;

                float distance = Mathf.Max(0.1f,
                    Vector3.Distance(cameraPosition, item.Center) -
                    item.Radius);
                float diameterPixels = item.Radius * ReferenceScreenHeight /
                    (tanHalfFov * distance);

                if (item.HiddenByUs)
                {
                    if (distance < minimumDistance - 50f ||
                        diameterPixels > maximumPixels * 1.35f)
                    {
                        item.Renderer.forceRenderingOff = false;
                        item.HiddenByUs = false;
                        HiddenCount--;
                        HiddenTriangleReferences -= item.Triangles;
                        if (item.IsGroundCover)
                        {
                            HiddenGroundCoverCount--;
                            HiddenGroundCoverTriangles -= item.Triangles;
                        }
                        else
                        {
                            HiddenWoodyCount--;
                            HiddenWoodyTriangles -= item.Triangles;
                        }
                    }
                }
                else if (distance > minimumDistance &&
                         diameterPixels < maximumPixels &&
                         !item.Renderer.forceRenderingOff)
                {
                    item.Renderer.forceRenderingOff = true;
                    item.HiddenByUs = true;
                    HiddenCount++;
                    HiddenTriangleReferences += item.Triangles;
                    if (item.IsGroundCover)
                    {
                        HiddenGroundCoverCount++;
                        HiddenGroundCoverTriangles += item.Triangles;
                    }
                    else
                    {
                        HiddenWoodyCount++;
                        HiddenWoodyTriangles += item.Triangles;
                    }
                }

                candidates[i] = item;
            }
            cursor = endIndex == candidates.Count ? 0 : endIndex;
        }

        private void Discover(MeshRenderer renderer)
        {
            if (renderer == null || !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy ||
                renderer.forceRenderingOff ||
                renderer.isPartOfStaticBatch ||
                renderer.GetComponentInParent<LODGroup>() != null ||
                renderer.GetComponent<MonoBehaviour>() != null ||
                renderer.GetComponentInParent<Rigidbody>() != null ||
                !IsExperimentalVegetationRenderer(renderer))
                return;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter == null ? null : filter.sharedMesh;
            if (mesh == null)
                return;

            Bounds bounds = renderer.bounds;
            float radius = bounds.extents.magnitude;
            if (radius < .1f || radius > MaxTreeRadius)
                return;

            long triangles = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                triangles += (long)mesh.GetIndexCount(i) / 3L;
            if (triangles <= 0)
                return;

            candidates.Add(new Candidate
            {
                Renderer = renderer,
                Center = bounds.center,
                Radius = radius,
                Triangles = triangles,
                IsGroundCover = IsGroundCoverRenderer(renderer)
            });
            if (IsGroundCoverRenderer(renderer))
                GroundCoverCandidateCount++;
            else
                WoodyCandidateCount++;
            EligibleTriangleReferences += triangles;
        }
    }
}
