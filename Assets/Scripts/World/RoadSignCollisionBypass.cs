using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    [DefaultExecutionOrder(-50)]
    public sealed class RoadSignCollisionBypass : MonoBehaviour
    {
        private const string RuntimeCityName =
            "MotorCity_FCGCity";

        private readonly List<Bounds> signZones =
            new();

        private readonly List<Collider> combinedObjectColliders =
            new();

        private Collider[] vehicleColliders =
            System.Array.Empty<Collider>();

        private bool ignoringCombinedObjects;

        private void Start()
        {
            vehicleColliders =
                GetComponentsInChildren<Collider>(
                    true);

            GameObject city =
                GameObject.Find(
                    RuntimeCityName);

            if (city == null)
                return;

            CollectRoadSignZones(
                city.transform);

            CollectCombinedObjectColliders(
                city);

            ApplyIgnoreState(
                false);
        }

        private void FixedUpdate()
        {
            if (signZones.Count == 0 ||
                combinedObjectColliders.Count == 0 ||
                vehicleColliders.Length == 0)
                return;

            bool insideSignZone =
                VehicleTouchesSignZone();

            if (insideSignZone !=
                ignoringCombinedObjects)
            {
                ApplyIgnoreState(
                    insideSignZone);
            }
        }

        private void OnDisable()
        {
            ApplyIgnoreState(
                false);
        }

        private bool VehicleTouchesSignZone()
        {
            foreach (Collider vehicleCollider in
                     vehicleColliders)
            {
                if (vehicleCollider == null ||
                    !vehicleCollider.enabled ||
                    vehicleCollider.isTrigger)
                    continue;

                Bounds vehicleBounds =
                    vehicleCollider.bounds;

                foreach (Bounds signZone in
                         signZones)
                {
                    if (vehicleBounds.Intersects(
                            signZone))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ApplyIgnoreState(
            bool ignore)
        {
            foreach (Collider vehicleCollider in
                     vehicleColliders)
            {
                if (vehicleCollider == null ||
                    vehicleCollider.isTrigger)
                    continue;

                foreach (Collider worldCollider in
                         combinedObjectColliders)
                {
                    if (worldCollider == null)
                        continue;

                    Physics.IgnoreCollision(
                        vehicleCollider,
                        worldCollider,
                        ignore);
                }
            }

            ignoringCombinedObjects =
                ignore;
        }

        private void CollectRoadSignZones(
            Transform cityRoot)
        {
            foreach (Renderer renderer in
                     cityRoot.GetComponentsInChildren<Renderer>(
                         true))
            {
                if (renderer == null ||
                    !IsRoadMarkHierarchy(
                        renderer.transform,
                        cityRoot))
                    continue;

                Bounds zone =
                    renderer.bounds;

                zone.Expand(
                    new Vector3(
                        0.55f,
                        0.35f,
                        0.55f));

                MergeOrAddZone(
                    zone);
            }
        }

        private void MergeOrAddZone(
            Bounds zone)
        {
            for (int i = 0;
                 i < signZones.Count;
                 i++)
            {
                Bounds existing =
                    signZones[i];

                if (!existing.Intersects(
                        zone))
                    continue;

                existing.Encapsulate(
                    zone);

                signZones[i] =
                    existing;

                return;
            }

            signZones.Add(
                zone);
        }

        private void CollectCombinedObjectColliders(
            GameObject city)
        {
            foreach (Collider collider in
                     city.GetComponentsInChildren<Collider>(
                         true))
            {
                if (collider == null)
                    continue;

                string objectName =
                    Normalize(
                        collider.gameObject.name);

                string meshName =
                    string.Empty;

                if (collider is MeshCollider meshCollider &&
                    meshCollider.sharedMesh != null)
                {
                    meshName =
                        Normalize(
                            meshCollider.sharedMesh.name);
                }

                if (objectName == "colliderobjects" ||
                    meshName == "colliderobjects")
                {
                    combinedObjectColliders.Add(
                        collider);
                }
            }
        }

        private static bool IsRoadMarkHierarchy(
            Transform item,
            Transform cityRoot)
        {
            Transform current =
                item;

            while (current != null)
            {
                if (Normalize(
                        current.name)
                    .StartsWith(
                        "roadmark"))
                {
                    return true;
                }

                if (current == cityRoot)
                    break;

                current =
                    current.parent;
            }

            return false;
        }

        private static string Normalize(
            string value)
        {
            return string.IsNullOrEmpty(
                    value)
                ? string.Empty
                : value
                    .Replace(
                        "-",
                        string.Empty)
                    .Replace(
                        "_",
                        string.Empty)
                    .Replace(
                        " ",
                        string.Empty)
                    .ToLowerInvariant();
        }
    }
}
