using System;
using System.Collections.Generic;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Play-Mode A/B experiment: hides faraway static decorative vehicles
    /// inside the authored city. Never touches traffic under CarContainer,
    /// the player's car, colliders, GameObjects, materials, or authored meshes.
    /// Disabled by default; not wired into WebGL production.
    /// </summary>
    public sealed class CityParkedVehicleCullingExperiment : MonoBehaviour
    {
        private const int DiscoveryBudget = 256;
        private const int UpdateBudget = 128;

        private struct Vehicle
        {
            public MeshRenderer[] Renderers;
            public Vector3 Center;
            public float Radius;
            public long TriangleReferences;
            public bool HiddenByUs;
        }

        private readonly List<Vehicle> vehicles = new List<Vehicle>(1024);
        private readonly HashSet<Transform> registeredRoots =
            new HashSet<Transform>();
        private MeshRenderer[] discovery = Array.Empty<MeshRenderer>();
        private GameObject cityRoot;
        private Camera observer;
        private int discoveryIndex;
        private int cursor;
        private bool refresh = true;
        private bool scanAnnounced;
        private Vector3 anchor;
        private int bodyMatches;
        private int parkedMatches;
        private int skippedScripted;

        public int VehicleCount => vehicles.Count;
        public int HiddenVehicles { get; private set; }
        public long VisibilityHides { get; private set; }
        public long VisibilityRestores { get; private set; }
        public long EligibleTriangleReferences { get; private set; }
        public long HiddenTriangleReferences { get; private set; }
        public bool ScanReady => scanAnnounced;

        public void Initialize(GameObject city, Camera camera)
        {
            RestoreAll();
            vehicles.Clear();
            registeredRoots.Clear();
            cityRoot = city;
            observer = camera;
            discovery = city != null
                ? city.GetComponentsInChildren<MeshRenderer>(true)
                : Array.Empty<MeshRenderer>();
            discoveryIndex = 0;
            cursor = 0;
            scanAnnounced = false;
            refresh = true;
            EligibleTriangleReferences = 0;
            bodyMatches = 0;
            parkedMatches = 0;
            skippedScripted = 0;
            VisibilityHides = 0;
            VisibilityRestores = 0;
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
            for (int i = 0; i < vehicles.Count; i++)
            {
                Vehicle item = vehicles[i];
                if (item.HiddenByUs)
                {
                    SetVisible(item.Renderers, true);
                    item.HiddenByUs = false;
                    vehicles[i] = item;
                }
            }

            HiddenVehicles = 0;
            HiddenTriangleReferences = 0;
            RequestRefresh();
        }

        private void Update()
        {
            if (observer == null)
                observer = Camera.main;
            if (cityRoot == null || observer == null ||
                !observer.isActiveAndEnabled)
                return;

            Vector3 cameraPosition = observer.transform.position;
            if (!scanAnnounced)
            {
                int end = Mathf.Min(discoveryIndex + DiscoveryBudget,
                    discovery.Length);
                for (int i = discoveryIndex; i < end; i++)
                    Discover(discovery[i]);

                discoveryIndex = end;
                if (discoveryIndex < discovery.Length)
                    return;

                scanAnnounced = true;
                discovery = Array.Empty<MeshRenderer>();
                Debug.Log("[MotorCity][ParkedVehicles] Scan complete: " +
                    VehicleCount + " static parked vehicle groups, " +
                    EligibleTriangleReferences + " triangle references. " +
                    "Body meshes=" + bodyMatches +
                    ", under Park-*=" + parkedMatches +
                    ", scripted/dynamic excluded=" + skippedScripted + ". " +
                    "These are whole-city references, not camera-visible tris. " +
                    "CarContainer/TrafficCar and colliders remain untouched.");
            }

            if (vehicles.Count == 0)
                return;

            if (cursor == 0)
            {
                if (!refresh &&
                    (cameraPosition - anchor).sqrMagnitude < 64f)
                    return;
                anchor = cameraPosition;
                refresh = false;
            }

            float hideDistance = MotorCityQualityRuntime.CurrentPreset switch
            {
                MotorCityQualityPreset.Low => 220f,
                MotorCityQualityPreset.Medium => 290f,
                _ => 390f
            };
            float restoreDistance = hideDistance - 55f;

            int stop = Mathf.Min(vehicles.Count, cursor + UpdateBudget);
            for (int i = cursor; i < stop; i++)
            {
                Vehicle item = vehicles[i];
                float distance = Vector3.Distance(cameraPosition, item.Center)
                    - item.Radius;

                if (item.HiddenByUs && distance < restoreDistance)
                {
                    SetVisible(item.Renderers, true);
                    item.HiddenByUs = false;
                    HiddenVehicles--;
                    VisibilityRestores++;
                    HiddenTriangleReferences -= item.TriangleReferences;
                }
                else if (!item.HiddenByUs && distance > hideDistance &&
                         AllAvailable(item.Renderers))
                {
                    SetVisible(item.Renderers, false);
                    item.HiddenByUs = true;
                    HiddenVehicles++;
                    VisibilityHides++;
                    HiddenTriangleReferences += item.TriangleReferences;
                }

                vehicles[i] = item;
            }
            cursor = stop == vehicles.Count ? 0 : stop;
        }

        private void Discover(MeshRenderer renderer)
        {
            if (renderer == null || !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy ||
                renderer.forceRenderingOff ||
                !string.Equals(renderer.name, "Car",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(renderer.name, "Car ON",
                    StringComparison.OrdinalIgnoreCase))
                return;

            bool carBody = false;
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && material.name.StartsWith(
                    "FCG_Traffic-Car-Atlas", StringComparison.OrdinalIgnoreCase))
                {
                    carBody = true;
                    break;
                }
            }
            if (!carBody)
                return;

            bodyMatches++;
            Transform vehicleRoot = renderer.transform.parent;
            if (vehicleRoot == null || vehicleRoot == cityRoot.transform ||
                vehicleRoot.name.StartsWith("Park-", StringComparison.OrdinalIgnoreCase) ||
                !HasParkedAncestor(vehicleRoot, cityRoot.transform) ||
                !registeredRoots.Add(vehicleRoot))
                return;

            parkedMatches++;

            // Explicitly exclude all controlled/moving vehicles and prefab
            // decorations containing authored MonoBehaviour state.
            if (vehicleRoot.GetComponent<Rigidbody>() != null ||
                vehicleRoot.GetComponentInChildren<MonoBehaviour>(true) != null ||
                vehicleRoot.GetComponentInParent<Rigidbody>() != null)
            {
                skippedScripted++;
                return;
            }

            MeshRenderer[] all = vehicleRoot.GetComponentsInChildren<MeshRenderer>(true);
            if (all.Length < 2 || all.Length > 24)
                return;

            Bounds combined = default;
            bool initializedBounds = false;
            long tris = 0;
            foreach (MeshRenderer part in all)
            {
                if (part == null || !part.enabled ||
                    !part.gameObject.activeInHierarchy ||
                    part.forceRenderingOff ||
                    part.GetComponentInParent<LODGroup>() != null)
                    return;

                MeshFilter filter = part.GetComponent<MeshFilter>();
                Mesh mesh = filter == null ? null : filter.sharedMesh;
                if (mesh == null)
                    return;

                if (!initializedBounds)
                {
                    combined = part.bounds;
                    initializedBounds = true;
                }
                else
                    combined.Encapsulate(part.bounds);

                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    tris += (long)mesh.GetIndexCount(sub) / 3;
            }

            if (!initializedBounds || combined.extents.magnitude > 15f)
                return;

            vehicles.Add(new Vehicle
            {
                Renderers = all,
                Center = combined.center,
                Radius = combined.extents.magnitude,
                TriangleReferences = tris
            });
            EligibleTriangleReferences += tris;
        }

        private static bool HasParkedAncestor(
            Transform vehicleRoot, Transform city)
        {
            Transform parent = vehicleRoot.parent;
            int depth = 0;
            bool parked = false;
            while (parent != null && depth++ < 16)
            {
                if (string.Equals(parent.name, "CarContainer",
                    StringComparison.OrdinalIgnoreCase))
                    return false;

                if (parent.name.StartsWith("Park-",
                    StringComparison.OrdinalIgnoreCase))
                    parked = true;

                if (parent == city)
                    return parked;

                parent = parent.parent;
            }
            return false;
        }

        private static bool AllAvailable(MeshRenderer[] renderers)
        {
            foreach (MeshRenderer renderer in renderers)
                if (renderer == null || !renderer.enabled ||
                    renderer.forceRenderingOff ||
                    !renderer.gameObject.activeInHierarchy)
                    return false;
            return true;
        }

        private static void SetVisible(MeshRenderer[] renderers, bool visible)
        {
            foreach (MeshRenderer renderer in renderers)
                if (renderer != null)
                    renderer.forceRenderingOff = !visible;
        }
    }
}
