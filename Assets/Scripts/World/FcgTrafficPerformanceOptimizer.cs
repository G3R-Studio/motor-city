using System;
using System.Collections.Generic;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    public sealed class FcgTrafficPerformanceOptimizer : MonoBehaviour
    {
        private const float RefreshInterval = 1.0f;
        private const float NearDistance = 58f;
        private const float FarDistance = 105f;

        private readonly Dictionary<EntityId, TrafficEntry> traffic =
            new();

        private Transform player;
        private Transform carContainer;
        private float refreshTimer;

        private sealed class TrafficEntry
        {
            public MonoBehaviour Behaviour;
            public Rigidbody Body;
            public Renderer[] Renderers;
            public Light[] Lights;
            public AudioSource[] AudioSources;
            public bool IsFarMode;
        }

        public int TrackedVehicleCount =>
            traffic.Count;

        public void Initialize(
            ArcadeCarController playerCar)
        {
            player =
                playerCar != null
                    ? playerCar.transform
                    : null;

            refreshTimer =
                0.75f;

        }

        private void Update()
        {
            refreshTimer -=
                Time.deltaTime;

            if (refreshTimer > 0f)
                return;

            refreshTimer =
                RefreshInterval;

            if (player == null)
            {
                ArcadeCarController car =
                    UnityEngine.Object.FindAnyObjectByType<ArcadeCarController>();

                if (car != null)
                    player = car.transform;
            }

            RefreshTrafficCache();
            ApplyDistanceModes();
        }

        private void RefreshTrafficCache()
        {
            if (carContainer == null)
            {
                GameObject container =
                    GameObject.Find(
                        "CarContainer");

                if (container != null)
                {
                    carContainer =
                        container.transform;
                }
            }

            if (carContainer == null)
                return;

            MonoBehaviour[] behaviours =
                carContainer.GetComponentsInChildren<MonoBehaviour>(
                    false);

            var alive =
                new HashSet<EntityId>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;

                Type type =
                    behaviour.GetType();

                if (!string.Equals(
                        type.FullName,
                        "FCG.TrafficCar",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                EntityId id =
                    behaviour.GetEntityId();

                alive.Add(
                    id);

                if (traffic.ContainsKey(
                        id))
                {
                    continue;
                }

                TrafficEntry entry =
                    BuildEntry(
                        behaviour);

                traffic.Add(
                    id,
                    entry);

                ApplyCommonOptimizations(
                    entry);
            }

            var stale =
                new List<EntityId>();

            foreach (KeyValuePair<EntityId, TrafficEntry> pair in
                     traffic)
            {
                if (!alive.Contains(
                        pair.Key) ||
                    pair.Value?.Behaviour == null)
                {
                    stale.Add(
                        pair.Key);
                }
            }

            foreach (EntityId id in stale)
            {
                traffic.Remove(
                    id);
            }
        }

        private static TrafficEntry BuildEntry(
            MonoBehaviour behaviour)
        {
            return new TrafficEntry
            {
                Behaviour =
                    behaviour,
                Body =
                    behaviour.GetComponent<Rigidbody>(),
                Renderers =
                    behaviour.GetComponentsInChildren<Renderer>(true),
                Lights =
                    behaviour.GetComponentsInChildren<Light>(true),
                AudioSources =
                    behaviour.GetComponentsInChildren<AudioSource>(true)
            };
        }

        private static void ApplyCommonOptimizations(
            TrafficEntry entry)
        {
            if (entry == null)
                return;

            if (entry.Renderers != null)
            {
                foreach (Renderer renderer in
                         entry.Renderers)
                {
                    if (renderer == null)
                        continue;

                    renderer.shadowCastingMode =
                        ShadowCastingMode.Off;

                    renderer.receiveShadows =
                        false;

                    renderer.lightProbeUsage =
                        LightProbeUsage.Off;

                    renderer.reflectionProbeUsage =
                        ReflectionProbeUsage.Off;

                    renderer.motionVectorGenerationMode =
                        MotionVectorGenerationMode.ForceNoMotion;
                }
            }

            if (entry.Lights != null)
            {
                foreach (Light light in
                         entry.Lights)
                {
                    if (light != null)
                        light.enabled = false;
                }
            }

            if (entry.AudioSources != null)
            {
                foreach (AudioSource audio in
                         entry.AudioSources)
                {
                    if (audio != null)
                        audio.enabled = false;
                }
            }

            if (entry.Body != null)
            {
                entry.Body.collisionDetectionMode =
                    CollisionDetectionMode.Discrete;

                entry.Body.solverIterations =
                    4;

                entry.Body.solverVelocityIterations =
                    1;
            }
        }

        private void ApplyDistanceModes()
        {
            if (player == null)
                return;

            float nearSquared =
                NearDistance *
                NearDistance;

            float farSquared =
                FarDistance *
                FarDistance;

            Vector3 playerPosition =
                player.position;

            foreach (TrafficEntry entry in
                     traffic.Values)
            {
                if (entry?.Behaviour == null)
                    continue;

                float distanceSquared =
                    (entry.Behaviour.transform.position -
                     playerPosition).sqrMagnitude;

                if (!entry.IsFarMode &&
                    distanceSquared >
                    farSquared)
                {
                    SetFarMode(
                        entry,
                        true);
                }
                else if (entry.IsFarMode &&
                         distanceSquared <
                         nearSquared)
                {
                    SetFarMode(
                        entry,
                        false);
                }
            }
        }

        private static void SetFarMode(
            TrafficEntry entry,
            bool farMode)
        {
            if (entry == null ||
                entry.Behaviour == null ||
                entry.IsFarMode == farMode)
            {
                return;
            }

            entry.IsFarMode =
                farMode;

            MonoBehaviour trafficCar =
                entry.Behaviour;

            // FCG normally executes MoveCar every 0.02 s. Far traffic does
            // not need 50 AI/physics updates per second, so it runs at 20 Hz.
            // The full 50 Hz cadence is restored before the car is close
            // enough for the player to interact with it.
            trafficCar.CancelInvoke(
                "MoveCar");

            float interval =
                farMode
                    ? 0.05f
                    : 0.02f;

            trafficCar.InvokeRepeating(
                "MoveCar",
                interval,
                interval);

            if (entry.Body != null)
            {
                entry.Body.interpolation =
                    farMode
                        ? RigidbodyInterpolation.None
                        : RigidbodyInterpolation.Interpolate;
            }
        }
    }
}
