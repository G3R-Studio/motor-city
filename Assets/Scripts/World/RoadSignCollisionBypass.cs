using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    [DefaultExecutionOrder(-50)]
    public sealed class RoadSignCollisionBypass : MonoBehaviour
    {
        private const string RuntimeCityName =
            "MotorCity_FCGCity";

        private readonly List<Collider> combinedObjectColliders =
            new();

        private readonly HashSet<Collider> activeRoadSignTriggers =
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

            CollectCombinedObjectColliders(
                city);

            ApplyIgnoreState(
                false);
        }

        private void OnTriggerEnter(
            Collider other)
        {
            if (!IsRoadMarkTrigger(
                    other))
                return;

            activeRoadSignTriggers.Add(
                other);

            ApplyIgnoreState(
                true);
        }

        private void OnTriggerStay(
            Collider other)
        {
            if (!IsRoadMarkTrigger(
                    other))
                return;

            activeRoadSignTriggers.Add(
                other);

            if (!ignoringCombinedObjects)
            {
                ApplyIgnoreState(
                    true);
            }
        }

        private void OnTriggerExit(
            Collider other)
        {
            if (!IsRoadMarkTrigger(
                    other))
                return;

            activeRoadSignTriggers.Remove(
                other);

            activeRoadSignTriggers.RemoveWhere(
                collider =>
                    collider == null);

            if (activeRoadSignTriggers.Count == 0)
            {
                ApplyIgnoreState(
                    false);
            }
        }

        private void OnDisable()
        {
            activeRoadSignTriggers.Clear();

            ApplyIgnoreState(
                false);
        }

        private bool IsRoadMarkTrigger(
            Collider collider)
        {
            if (collider == null ||
                !collider.isTrigger)
                return false;

            Transform current =
                collider.transform;

            while (current != null)
            {
                if (Normalize(
                        current.name)
                    .StartsWith(
                        "roadmark"))
                {
                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private void ApplyIgnoreState(
            bool ignore)
        {
            if (ignoringCombinedObjects ==
                    ignore &&
                ignore)
                return;

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
