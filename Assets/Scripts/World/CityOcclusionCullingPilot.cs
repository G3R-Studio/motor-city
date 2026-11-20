using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Temporary Play Mode occlusion pilot.
    /// A complete building prefab instance is one culling unit: every Renderer
    /// below that instance is hidden/restored together. Colliders and assets are
    /// never disabled or modified.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityOcclusionCullingPilot : MonoBehaviour
    {
        private const float ScanInterval = 0.25f;
        private const float NearKeepDistance = 38f;

        // These are the building prefab name families used by the city generator.
        // The highest matching ancestor below Buildings is the prefab instance
        // (for example BC-P06(Clone)), not its inner BC-P06 mesh child.
        private static readonly string[] BuildingPrefixes =
        {
            "BB-", "BC-", "BL-", "BR-", "Base-", "DC-", "EB-", "EC-",
            "M-", "QD-", "Fabrica-", "Shopping-"
        };

        private sealed class BuildingGroup
        {
            public Transform Root;
            public Bounds Bounds;
            public readonly List<Renderer> Renderers = new List<Renderer>();
            public int OccludedChecks;
            public bool Hidden;
        }

        private readonly Dictionary<Transform, BuildingGroup> buildings =
            new Dictionary<Transform, BuildingGroup>();

        private readonly Dictionary<Renderer, bool> originalForceOff =
            new Dictionary<Renderer, bool>();

        private readonly HashSet<Transform> unsupportedBuildings =
            new HashSet<Transform>();

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

            BuildBuildingGroups();
            Physics.SyncTransforms();
            initialized = true;

            Debug.Log(
                "[Motor City Occlusion] Whole-building Play Mode pilot started. " +
                "Building instances=" + buildings.Count +
                ", skipped unsupported instances=" + unsupportedBuildings.Count +
                ", building occluders=" + colliderCount +
                ", renderers controlled=" + originalForceOff.Count +
                ". Every renderer in one building instance is toggled together. " +
                "Stop the pilot to restore all renderer flags.");
        }

        private void BuildBuildingGroups()
        {
            Renderer[] allRenderers =
                cityRoot.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer renderer = allRenderers[i];
                if (renderer == null)
                    continue;

                Transform buildingRoot =
                    FindBuildingInstanceRoot(renderer.transform);

                if (buildingRoot == null)
                    continue;

                if (!buildings.TryGetValue(buildingRoot, out BuildingGroup group))
                {
                    group = new BuildingGroup
                    {
                        Root = buildingRoot,
                        Bounds = renderer.bounds
                    };
                    buildings.Add(buildingRoot, group);
                }
                else
                {
                    group.Bounds.Encapsulate(renderer.bounds);
                }

                group.Renderers.Add(renderer);

                // Do not partially cull a prefab if Unity has merged one of its
                // renderers into a static batch, or if a runtime/LOD component
                // owns part of its visibility. Leave that complete building visible.
                if (renderer.isPartOfStaticBatch ||
                    renderer.GetComponentInParent<Rigidbody>() != null ||
                    renderer.GetComponentInParent<Animator>() != null ||
                    renderer.GetComponentInParent<LODGroup>() != null)
                {
                    unsupportedBuildings.Add(buildingRoot);
                }
            }

            // Register only complete, supported building instances. No material,
            // per-mesh size or atlas filter is used: glass and façade submeshes
            // must disappear together with the rest of the building.
            List<Transform> remove = new List<Transform>();
            foreach (KeyValuePair<Transform, BuildingGroup> pair in buildings)
            {
                Transform root = pair.Key;
                BuildingGroup group = pair.Value;

                if (root == null || unsupportedBuildings.Contains(root) ||
                    group.Renderers.Count == 0)
                {
                    remove.Add(root);
                    continue;
                }

                for (int i = 0; i < group.Renderers.Count; i++)
                {
                    Renderer renderer = group.Renderers[i];
                    if (renderer != null)
                        originalForceOff[renderer] = renderer.forceRenderingOff;
                }
            }

            for (int i = 0; i < remove.Count; i++)
                buildings.Remove(remove[i]);
        }

        private static Transform FindBuildingInstanceRoot(Transform target)
        {
            Transform highestBuildingAncestor = null;
            bool insideBuildings = false;

            // Walk from a mesh child upward. Several levels may match (for
            // example BC-P06 under BC-P06(Clone)); keep the highest match so
            // all parts of the instantiated prefab become one culling unit.
            for (Transform current = target; current != null; current = current.parent)
            {
                if (string.Equals(
                    current.name,
                    "Buildings",
                    StringComparison.OrdinalIgnoreCase))
                {
                    insideBuildings = true;
                    break;
                }

                string objectName = current.name;
                for (int i = 0; i < BuildingPrefixes.Length; i++)
                {
                    if (objectName.StartsWith(
                        BuildingPrefixes[i],
                        StringComparison.OrdinalIgnoreCase))
                    {
                        highestBuildingAncestor = current;
                        break;
                    }
                }
            }

            return insideBuildings ? highestBuildingAncestor : null;
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
            foreach (BuildingGroup building in buildings.Values)
            {
                // Unity's frustum culling already handles buildings outside view.
                // Keep our last state until the building returns into the frustum.
                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, building.Bounds))
                    continue;

                bool visible = CityOcclusionVisibility.IsBoundsVisible(
                    targetCamera,
                    building.Bounds,
                    buildingOccluders,
                    frustumPlanes,
                    raycastHits,
                    NearKeepDistance);

                if (visible)
                {
                    building.OccludedChecks = 0;
                    SetBuildingHidden(building, false);
                }
                else
                {
                    building.OccludedChecks++;
                    // Require two consecutive blocked scans to prevent flicker.
                    if (building.OccludedChecks >= 2)
                        SetBuildingHidden(building, true);
                }

                if (building.Hidden)
                    hidden++;
            }

            if (Time.frameCount % 300 == 0)
            {
                Debug.Log(
                    "[Motor City Occlusion] Hidden complete buildings=" + hidden +
                    "/" + buildings.Count + " (temporary Play Mode test).");
            }
        }

        private void SetBuildingHidden(BuildingGroup building, bool hidden)
        {
            if (building.Hidden == hidden)
                return;

            building.Hidden = hidden;

            for (int i = 0; i < building.Renderers.Count; i++)
            {
                Renderer renderer = building.Renderers[i];
                if (renderer == null)
                    continue;

                if (hidden)
                {
                    renderer.forceRenderingOff = true;
                }
                else if (originalForceOff.TryGetValue(renderer, out bool original))
                {
                    renderer.forceRenderingOff = original;
                }
            }
        }

        private void RestoreAll()
        {
            foreach (KeyValuePair<Renderer, bool> pair in originalForceOff)
            {
                if (pair.Key != null)
                    pair.Key.forceRenderingOff = pair.Value;
            }

            foreach (BuildingGroup building in buildings.Values)
            {
                building.Hidden = false;
                building.OccludedChecks = 0;
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
