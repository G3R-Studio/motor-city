using System.Collections.Generic;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class TrafficSystem : MonoBehaviour
    {
        private const int MaximumVehicles = 16;
        private const float MinimumSpawnDistance = 58f;
        private const float MaximumSpawnDistance = 150f;
        private const float DespawnDistance = 220f;
        private const float SpawnInterval = 0.55f;

        private readonly List<TrafficVehicle> vehicles =
            new();

        private readonly List<GameObject> visualPrefabs =
            new();

        private Transform player;
        private TrafficSignalController signals;
        private float spawnTimer;

        public int ActiveVehicleCount =>
            vehicles.Count;

        public void Initialize(
            ArcadeCarController playerCar,
            TrafficSignalController signalController)
        {
            player =
                playerCar != null
                    ? playerCar.transform
                    : null;

            signals =
                signalController;

            LoadVisualPrefabs();

            Debug.Log(
                $"Motor City: traffic system prepared. Visual variants={visualPrefabs.Count}, max={MaximumVehicles}.");
        }

        private void Update()
        {
            if (player == null)
                return;

            CleanupFarTraffic();

            spawnTimer -=
                Time.deltaTime;

            if (spawnTimer > 0f ||
                vehicles.Count >=
                MaximumVehicles)
            {
                return;
            }

            spawnTimer =
                SpawnInterval;

            TrySpawnVehicle();
        }

        public void NotifyDestroyed(
            TrafficVehicle vehicle)
        {
            vehicles.Remove(
                vehicle);
        }

        private void LoadVisualPrefabs()
        {
            visualPrefabs.Clear();

            AddVisual(
                Resources.Load<GameObject>(
                    "MotorCity/PlayerCarVisual"));

            for (int i = 1;
                 i <= 4;
                 i++)
            {
                AddVisual(
                    Resources.Load<GameObject>(
                        $"MotorCity/Vehicles/Vehicle_{i:00}"));
            }
        }

        private void AddVisual(
            GameObject prefab)
        {
            if (prefab != null &&
                !visualPrefabs.Contains(
                    prefab))
            {
                visualPrefabs.Add(
                    prefab);
            }
        }

        private void TrySpawnVehicle()
        {
            if (!TrafficRoadUtility.TryFindRoadAround(
                    player.position,
                    MinimumSpawnDistance,
                    MaximumSpawnDistance,
                    28,
                    out Vector3 roadPoint,
                    out _))
            {
                return;
            }

            Camera camera =
                Camera.main;

            if (camera != null)
            {
                Vector3 viewport =
                    camera.WorldToViewportPoint(
                        roadPoint +
                        Vector3.up);

                bool clearlyVisible =
                    viewport.z > 0f &&
                    viewport.x > -0.08f &&
                    viewport.x < 1.08f &&
                    viewport.y > -0.08f &&
                    viewport.y < 1.08f;

                if (clearlyVisible)
                    return;
            }

            foreach (TrafficVehicle existing in
                     vehicles)
            {
                if (existing == null)
                    continue;

                if ((existing.transform.position -
                     roadPoint).sqrMagnitude <
                    11f * 11f)
                {
                    return;
                }
            }

            Vector3 initialForward =
                Random.value < 0.5f
                    ? Vector3.forward
                    : Vector3.right;

            if (Random.value < 0.5f)
                initialForward = -initialForward;

            if (!TrafficRoadUtility.TryChooseForwardRoadTarget(
                    roadPoint,
                    initialForward,
                    10f,
                    out Vector3 firstTarget))
            {
                return;
            }

            Vector3 direction =
                firstTarget -
                roadPoint;

            direction.y =
                0f;

            if (direction.sqrMagnitude <
                1f)
                return;

            direction.Normalize();

            GameObject root =
                new("Traffic Car");

            root.transform.position =
                roadPoint +
                Vector3.up * 0.42f;

            root.transform.rotation =
                Quaternion.LookRotation(
                    direction,
                    Vector3.up);

            BoxCollider collider =
                root.AddComponent<BoxCollider>();

            collider.center =
                new Vector3(
                    0f,
                    0.55f,
                    0f);

            collider.size =
                new Vector3(
                    1.82f,
                    1.15f,
                    4.15f);

            Rigidbody rigidbody =
                root.AddComponent<Rigidbody>();

            rigidbody.isKinematic =
                true;

            rigidbody.interpolation =
                RigidbodyInterpolation.Interpolate;

            rigidbody.collisionDetectionMode =
                CollisionDetectionMode.ContinuousSpeculative;

            InstallVisual(
                root.transform);

            TrafficVehicle vehicle =
                root.AddComponent<TrafficVehicle>();

            vehicle.Initialize(
                this,
                signals,
                Random.Range(
                    8.5f,
                    14.5f));

            vehicles.Add(
                vehicle);
        }

        private void InstallVisual(
            Transform root)
        {
            if (visualPrefabs.Count == 0)
            {
                CreateFallbackVisual(
                    root);

                return;
            }

            GameObject prefab =
                visualPrefabs[
                    Random.Range(
                        0,
                        visualPrefabs.Count)];

            GameObject visual =
                Instantiate(
                    prefab,
                    root);

            visual.name =
                prefab.name +
                "_TrafficVisual";

            visual.transform.localPosition =
                Vector3.zero;

            visual.transform.localRotation =
                Quaternion.identity;

            StripVisualPhysics(
                visual);

            FitVisual(
                visual.transform,
                root);
        }

        private static void StripVisualPhysics(
            GameObject visual)
        {
            foreach (MonoBehaviour behaviour in
                     visual.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null)
                    behaviour.enabled =
                        false;
            }

            foreach (Collider collider in
                     visual.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    collider.enabled =
                        false;
            }

            foreach (Rigidbody rigidbody in
                     visual.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rigidbody != null)
                {
                    rigidbody.isKinematic =
                        true;

                    rigidbody.detectCollisions =
                        false;
                }
            }
        }

        private static void FitVisual(
            Transform visual,
            Transform root)
        {
            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
                return;

            Bounds bounds =
                renderers[0].bounds;

            for (int i = 1;
                 i < renderers.Length;
                 i++)
            {
                bounds.Encapsulate(
                    renderers[i].bounds);
            }

            float length =
                Mathf.Max(
                    bounds.size.x,
                    bounds.size.z);

            if (length > 0.1f)
            {
                visual.localScale *=
                    Mathf.Clamp(
                        4.35f / length,
                        0.72f,
                        1.35f);
            }

            // Recalculate after scale.
            bounds =
                visual.GetComponentsInChildren<Renderer>(true)[0].bounds;

            foreach (Renderer renderer in
                     visual.GetComponentsInChildren<Renderer>(true))
            {
                bounds.Encapsulate(
                    renderer.bounds);
            }

            Vector3 centerLocal =
                root.InverseTransformPoint(
                    bounds.center);

            visual.localPosition -=
                new Vector3(
                    centerLocal.x,
                    bounds.min.y -
                    root.position.y,
                    centerLocal.z);
        }

        private static void CreateFallbackVisual(
            Transform root)
        {
            GameObject body =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            body.name =
                "Traffic Fallback Body";

            body.transform.SetParent(
                root,
                false);

            body.transform.localPosition =
                new Vector3(
                    0f,
                    0.62f,
                    0f);

            body.transform.localScale =
                new Vector3(
                    1.8f,
                    0.75f,
                    4f);

            Object.Destroy(
                body.GetComponent<Collider>());

            Renderer renderer =
                body.GetComponent<Renderer>();

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            if (renderer != null &&
                shader != null)
            {
                Material material =
                    new(shader);

                material.color =
                    Color.HSVToRGB(
                        Random.value,
                        0.55f,
                        0.82f);

                renderer.sharedMaterial =
                    material;
            }
        }

        private void CleanupFarTraffic()
        {
            float maximumSquared =
                DespawnDistance *
                DespawnDistance;

            for (int i =
                     vehicles.Count - 1;
                 i >= 0;
                 i--)
            {
                TrafficVehicle vehicle =
                    vehicles[i];

                if (vehicle == null)
                {
                    vehicles.RemoveAt(
                        i);

                    continue;
                }

                if ((vehicle.transform.position -
                     player.position).sqrMagnitude <=
                    maximumSquared)
                {
                    continue;
                }

                vehicles.RemoveAt(
                    i);

                Destroy(
                    vehicle.gameObject);
            }
        }
    }
}
