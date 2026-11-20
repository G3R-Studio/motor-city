using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Temporary Play Mode occlusion pilot. It only forces eligible opaque
    /// building renderers off; it never changes meshes, colliders or assets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityOcclusionCullingPilot : MonoBehaviour
    {
        private const float CellSize = 128f;
        private const float ScanInterval = 0.25f;
        private const float NearKeepDistance = 38f;

        private sealed class Cell
        {
            public Bounds Bounds;
            public readonly List<MeshRenderer> Renderers = new List<MeshRenderer>();
            public int OccludedChecks;
            public bool Hidden;
        }

        private readonly Dictionary<Vector2Int, Cell> cells =
            new Dictionary<Vector2Int, Cell>();

        private readonly Dictionary<MeshRenderer, bool> originalForceOff =
            new Dictionary<MeshRenderer, bool>();

        private readonly HashSet<Collider> buildingOccluders =
            new HashSet<Collider>();

        private readonly Plane[] frustumPlanes = new Plane[6];
        private readonly RaycastHit[] raycastHits = new RaycastHit[96];

        private Camera targetCamera;
        private GameObject cityRoot;
        private float nextScan;
        private bool initialized;

        private void Start()
        {
            targetCamera = GetComponent<Camera>();
            if (targetCamera == null)
                targetCamera = Camera.main;

            cityRoot = GameObject.Find("MotorCity_FCGCity");
            if (cityRoot == null)
                cityRoot = GameObject.Find("City-Maker");

            if (targetCamera == null || cityRoot == null)
            {
                Debug.LogWarning(
                    "[Motor City Occlusion] Pilot could not find the main camera or CityVisual. " +
                    "Nothing was changed.");
                enabled = false;
                return;
            }

            int colliderCount =
                CityOcclusionVisibility.CollectBuildingOccluders(
                    cityRoot,
                    buildingOccluders);

            if (colliderCount == 0)
            {
                Debug.LogWarning(
                    "[Motor City Occlusion] No building colliders were found under Buildings. " +
                    "Pilot disabled; nothing was changed.");
                enabled = false;
                return;
            }

            BuildCells();
            Physics.SyncTransforms();
            initialized = true;

            Debug.Log(
                "[Motor City Occlusion] Play Mode pilot started. " +
                "Cells=" + cells.Count +
                ", building occluders=" + colliderCount +
                ", renderers=" + originalForceOff.Count +
                ". Stop the pilot to restore all renderer flags.");
        }

        private void BuildCells()
        {
            MeshRenderer[] renderers =
                cityRoot.GetComponentsInChildren<MeshRenderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null ||
                    renderer.isPartOfStaticBatch ||
                    !CityOcclusionVisibility.IsInsideBuildings(renderer.transform) ||
                    renderer.GetComponent<MeshFilter>() == null ||
                    renderer.GetComponent<MeshFilter>().sharedMesh == null ||
                    renderer.bounds.extents.magnitude <= 7f ||
                    renderer.GetComponentInParent<Rigidbody>() != null ||
                    renderer.GetComponentInParent<Animator>() != null ||
                    renderer.GetComponentInParent<LODGroup>() != null)
                    continue;

                if (!UsesOpaqueBuildingAtlas(renderer))
                    continue;

                if (renderer.forceRenderingOff)
                    continue;

                originalForceOff[renderer] = renderer.forceRenderingOff;

                Vector3 center = renderer.bounds.center;
                Vector2Int key = new Vector2Int(
                    Mathf.FloorToInt(center.x / CellSize),
                    Mathf.FloorToInt(center.z / CellSize));

                if (!cells.TryGetValue(key, out Cell cell))
                {
                    cell = new Cell { Bounds = renderer.bounds };
                    cells.Add(key, cell);
                }
                else
                {
                    cell.Bounds.Encapsulate(renderer.bounds);
                }

                cell.Renderers.Add(renderer);
            }
        }

        private static bool UsesOpaqueBuildingAtlas(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
                return false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null ||
                    material.name != "Atlas-1_59aa769b" ||
                    material.renderQueue != 2000)
                    return false;
            }

            return true;
        }

        private void LateUpdate()
        {
            if (!initialized || Time.unscaledTime < nextScan)
                return;

            nextScan = Time.unscaledTime + ScanInterval;

            if (targetCamera == null || !targetCamera.isActiveAndEnabled ||
                Camera.main != targetCamera || Camera.allCamerasCount != 1)
            {
                RestoreAll();
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(
                targetCamera,
                frustumPlanes);

            int hidden = 0;
            foreach (Cell cell in cells.Values)
            {
                // Outside the frustum Unity already avoids drawing the cell.
                // Leave our last state untouched until it comes back into view.
                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, cell.Bounds))
                    continue;

                bool visible = CityOcclusionVisibility.IsBoundsVisible(
                    targetCamera,
                    cell.Bounds,
                    buildingOccluders,
                    frustumPlanes,
                    raycastHits,
                    NearKeepDistance);

                if (visible)
                {
                    cell.OccludedChecks = 0;
                    SetCellHidden(cell, false);
                }
                else
                {
                    cell.OccludedChecks++;
                    // Two consecutive blocked scans prevent single-frame pops.
                    if (cell.OccludedChecks >= 2)
                        SetCellHidden(cell, true);
                }

                if (cell.Hidden)
                    hidden++;
            }

            if (Time.frameCount % 300 == 0)
            {
                Debug.Log(
                    "[Motor City Occlusion] Hidden cells=" + hidden +
                    "/" + cells.Count + " (temporary Play Mode test).");
            }
        }

        private void SetCellHidden(Cell cell, bool hidden)
        {
            if (cell.Hidden == hidden)
                return;

            cell.Hidden = hidden;
            foreach (MeshRenderer renderer in cell.Renderers)
            {
                if (renderer == null)
                    continue;

                if (hidden)
                    renderer.forceRenderingOff = true;
                else if (originalForceOff.TryGetValue(renderer, out bool original))
                    renderer.forceRenderingOff = original;
            }
        }

        private void RestoreAll()
        {
            foreach (KeyValuePair<MeshRenderer, bool> pair in originalForceOff)
            {
                if (pair.Key != null)
                    pair.Key.forceRenderingOff = pair.Value;
            }

            foreach (Cell cell in cells.Values)
            {
                cell.Hidden = false;
                cell.OccludedChecks = 0;
            }
        }

        private void OnDisable()
        {
            RestoreAll();
        }

        private void OnDestroy()
        {
            RestoreAll();
        }
    }
}
