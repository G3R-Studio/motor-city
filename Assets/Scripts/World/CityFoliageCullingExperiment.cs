using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Optional, reversible WebGL-compatible rendering experiment.
    /// Only far-away, non-interactive FCG foliage MeshRenderers are hidden.
    /// Never changes the city prefab, mesh data, materials or colliders.
    /// Editor manual opt-in, and existing production WebGL Low/Medium
    /// fallback. High uses the separate Trees-01 foliage culler.
    /// Never active at the same time as the WebGL High culler.
    /// </summary>
    public sealed class CityFoliageCullingExperiment : MonoBehaviour
    {
        // The restore threshold must be CLOSER than the hide threshold.
        // Previously 145/165 caused renderer on/off oscillation at 145-165m.
        private const int ChecksPerFrame = 256;

        private struct Candidate
        {
            public MeshRenderer Renderer;
            public Vector3 Center;
            public float Radius;
            public bool HiddenByExperiment;
        }

        private Candidate[] candidates = Array.Empty<Candidate>();
        private int cursor;
        private Camera targetCamera;
        private int hiddenCount;

        public int CandidateCount => candidates.Length;
        public int HiddenCount => hiddenCount;

        public void Initialize(GameObject city)
        {
            RestoreAll();

            List<Candidate> found = new List<Candidate>();
            MeshRenderer[] renderers =
                city.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy ||
                    !IsDecorativeFoliage(renderer.name) ||
                    renderer.GetComponent<LODGroup>() != null ||
                    renderer.GetComponentInParent<LODGroup>() != null)
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null ||
                    renderer.sharedMaterial == null)
                    continue;

                Bounds bounds = renderer.bounds;
                // Avoid hiding unusually large authored vegetation meshes.
                if (bounds.extents.magnitude > 12f)
                    continue;

                found.Add(new Candidate
                {
                    Renderer = renderer,
                    Center = bounds.center,
                    Radius = bounds.extents.magnitude,
                    HiddenByExperiment = false
                });
            }

            candidates = found.ToArray();
            cursor = 0;
            hiddenCount = 0;
            targetCamera = Camera.main;

            Debug.Log("[MotorCity][Rendering] Foliage A/B experiment: " +
                candidates.Length + " eligible decorative mesh renderers. " +
                "Roads, traffic, garage, mission markers and colliders remain untouched.");
        }

        private static bool IsDecorativeFoliage(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.StartsWith("Plant-", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Plants-", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Fern-", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Vegetation-", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Flower-", StringComparison.OrdinalIgnoreCase);
        }

        private void Update()
        {
            if (targetCamera == null || !targetCamera.isActiveAndEnabled)
                targetCamera = Camera.main;

            if (targetCamera == null || candidates.Length == 0)
                return;

            Vector3 cameraPosition = targetCamera.transform.position;
            float hideDistance = MotorCityVisibilityRuntime.FoliageHideDistance;
            float restoreDistance = hideDistance - 30f;
            int count = Mathf.Min(ChecksPerFrame, candidates.Length);

            for (int iteration = 0; iteration < count; iteration++)
            {
                if (cursor == candidates.Length)
                    cursor = 0;

                Candidate item = candidates[cursor];
                if (item.Renderer != null)
                {
                    float distance = Vector3.Distance(
                        cameraPosition, item.Center) - item.Radius;

                    if (!item.HiddenByExperiment && distance > hideDistance &&
                        item.Renderer.enabled)
                    {
                        item.Renderer.enabled = false;
                        item.HiddenByExperiment = true;
                        hiddenCount++;
                    }
                    else if (item.HiddenByExperiment &&
                             distance < restoreDistance)
                    {
                        item.Renderer.enabled = true;
                        item.HiddenByExperiment = false;
                        hiddenCount--;
                    }
                }
                candidates[cursor] = item;
                cursor++;
            }
        }

        public void RestoreAll()
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                Candidate item = candidates[i];
                if (item.HiddenByExperiment && item.Renderer != null)
                    item.Renderer.enabled = true;
            }
            candidates = Array.Empty<Candidate>();
            hiddenCount = 0;
            cursor = 0;
        }

        private void OnDisable() => RestoreAll();
        private void OnDestroy() => RestoreAll();
    }
}
