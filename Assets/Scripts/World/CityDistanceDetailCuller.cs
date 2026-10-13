using System;
using System.Collections.Generic;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Reversible culling of tiny, distant, noninteractive opaque city meshes.
    /// This is not an HLOD generator and never changes authored assets or physics.
    /// </summary>
    public sealed class CityDistanceDetailCuller : MonoBehaviour
    {
        private const int DiscoveryBudget = 256;
        private const int UpdateBudget = 512;
        private const float ReferencePixels = 1080f;
        private const float MaximumRadius = 7f;
        private static readonly string[] ProtectedNames =
        {
            "building", "road", "street", "ground", "terrain", "water",
            "bridge", "roof", "wall", "window", "door", "floor",
            "garage", "car", "vehicle", "traffic", "lamp", "light",
            "signal", "sign", "checkpoint", "marker", "billboard",
            "highway", "sidewalk", "asphalt", "spline"
        };

        private struct Candidate
        {
            public MeshRenderer Renderer;
            public Vector3 Center;
            public float Radius;
            public long Triangles;
            public bool Hidden;
        }

        private readonly List<Candidate> candidates = new List<Candidate>(4096);
        private MeshRenderer[] discovery = Array.Empty<MeshRenderer>();
        private Camera cameraToCull;
        private int discoveryIndex;
        private int cursor;
        private bool scanAnnounced;
        private bool refresh = true;
        private Vector3 sweepPosition;

        public int CandidateCount => candidates.Count;
        public int HiddenCount { get; private set; }
        public long HiddenTriangleReferences { get; private set; }
        public bool ScanReady => scanAnnounced;

        public void Initialize(GameObject city, Camera observer)
        {
            RestoreAll();
            candidates.Clear();
            cameraToCull = observer;
            discovery = city.GetComponentsInChildren<MeshRenderer>(true);
            discoveryIndex = 0;
            cursor = 0;
            refresh = true;
            scanAnnounced = false;
        }

        public void RequestRefresh()
        {
            refresh = true;
            cursor = 0;
        }

        private void OnEnable() => RequestRefresh();
        private void OnDisable() => RestoreAll();
        private void OnDestroy() => RestoreAll();

        public void RestoreAll()
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate item = candidates[i];
                if (item.Hidden && item.Renderer != null)
                    item.Renderer.forceRenderingOff = false;
                item.Hidden = false;
                candidates[i] = item;
            }
            HiddenCount = 0;
            HiddenTriangleReferences = 0;
        }

        private void Update()
        {
            if (cameraToCull == null)
                cameraToCull = Camera.main;
            if (cameraToCull == null || !cameraToCull.isActiveAndEnabled)
                return;

            Vector3 position = cameraToCull.transform.position;
            if (discoveryIndex < discovery.Length)
            {
                int end = Mathf.Min(discoveryIndex + DiscoveryBudget, discovery.Length);
                for (int i = discoveryIndex; i < end; i++)
                    Discover(discovery[i], position);
                discoveryIndex = end;
                return;
            }

            if (!scanAnnounced)
            {
                scanAnnounced = true;
                Debug.Log("[MotorCity][Optimization] Distant detail candidates: " +
                    candidates.Count + " of " + discovery.Length +
                    " city MeshRenderers. Browser opt-out: ?motorcity_detail_cull=off");
                discovery = Array.Empty<MeshRenderer>();
            }

            if (candidates.Count == 0)
                return;

            if (cursor == 0)
            {
                if (!refresh && (position - sweepPosition).sqrMagnitude < 144f)
                    return;
                sweepPosition = position;
                refresh = false;
            }

            int stop = Mathf.Min(cursor + UpdateBudget, candidates.Count);
            for (int i = cursor; i < stop; i++)
                Evaluate(i, position);
            cursor = stop == candidates.Count ? 0 : stop;
        }

        private void Discover(MeshRenderer renderer, Vector3 cameraPosition)
        {
            if (renderer == null || !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy ||
                renderer.forceRenderingOff || renderer.isPartOfStaticBatch ||
                IsProtected(renderer.name) ||
                renderer.GetComponent<Collider>() != null ||
                renderer.GetComponent<MonoBehaviour>() != null ||
                renderer.GetComponentInParent<LODGroup>() != null)
                return;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter == null ? null : filter.sharedMesh;
            Material material = renderer.sharedMaterial;
            if (mesh == null || material == null || material.renderQueue >= 3000)
                return;

            Bounds bounds = renderer.bounds;
            float radius = bounds.extents.magnitude;
            if (radius <= 0.01f || radius > MaximumRadius)
                return;

            long triangleReferences = 0;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                triangleReferences += (long)mesh.GetIndexCount(submesh) / 3;

            candidates.Add(new Candidate
            {
                Renderer = renderer,
                Center = bounds.center,
                Radius = radius,
                Triangles = triangleReferences
            });
            Evaluate(candidates.Count - 1, cameraPosition);
        }

        private static bool IsProtected(string name)
        {
            if (string.IsNullOrEmpty(name))
                return true;
            foreach (string token in ProtectedNames)
                if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private void Evaluate(int index, Vector3 position)
        {
            Candidate item = candidates[index];
            if (item.Renderer == null)
                return;

            float minDistance;
            float pixelThreshold;
            switch (MotorCityQualityRuntime.CurrentPreset)
            {
                case MotorCityQualityPreset.Low:
                    minDistance = 180f; pixelThreshold = 12f; break;
                case MotorCityQualityPreset.Medium:
                    minDistance = 245f; pixelThreshold = 11f; break;
                default:
                    minDistance = 320f; pixelThreshold = 9f; break;
            }

            float distance = Mathf.Max(0.1f,
                Vector3.Distance(position, item.Center) - item.Radius);
            float tanHalfFov = Mathf.Tan(
                Mathf.Max(1f, cameraToCull.fieldOfView) * Mathf.Deg2Rad * 0.5f);
            float pixels = item.Radius * ReferencePixels /
                (Mathf.Max(0.001f, tanHalfFov) * distance);

            if (item.Hidden)
            {
                // Restore closer than we hide, to avoid one-pixel flicker.
                if (distance < minDistance - 30f ||
                    pixels > pixelThreshold * 1.35f)
                {
                    item.Renderer.forceRenderingOff = false;
                    item.Hidden = false;
                    HiddenCount--;
                    HiddenTriangleReferences -= item.Triangles;
                }
            }
            else if (distance >= minDistance && pixels < pixelThreshold &&
                     !item.Renderer.forceRenderingOff)
            {
                item.Renderer.forceRenderingOff = true;
                item.Hidden = true;
                HiddenCount++;
                HiddenTriangleReferences += item.Triangles;
            }

            candidates[index] = item;
        }
    }
}
