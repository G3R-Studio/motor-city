using System;
using System.Collections.Generic;
using System.Reflection;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class TrafficQualityAdapter :
        MonoBehaviour
    {
        private MonoBehaviour trafficSystem;
        private FieldInfo maxVehiclesField;
        private FieldInfo radiusField;
        private FieldInfo playerField;

        public void Bind(
            MonoBehaviour target)
        {
            trafficSystem =
                target;

            ResolveFields();
            ApplyQuality();
        }

        private void OnEnable()
        {
            MotorCityQualityRuntime.PresetChanged +=
                ApplyQuality;
        }

        private void OnDisable()
        {
            MotorCityQualityRuntime.PresetChanged -=
                ApplyQuality;
        }

        private void ResolveFields()
        {
            if (trafficSystem == null)
                return;

            Type type =
                trafficSystem.GetType();

            maxVehiclesField =
                type.GetField(
                    "maxVehiclesWithPlayer");

            radiusField =
                type.GetField(
                    "around");

            playerField =
                type.GetField(
                    "player");
        }

        private void ApplyQuality()
        {
            if (trafficSystem == null)
                return;

            if (maxVehiclesField == null ||
                radiusField == null)
            {
                ResolveFields();
            }

            if (maxVehiclesField != null &&
                maxVehiclesField.FieldType ==
                typeof(int))
            {
                maxVehiclesField.SetValue(
                    trafficSystem,
                    MotorCityQualityRuntime
                        .TrafficVehicleBudget);
            }

            if (radiusField != null &&
                radiusField.FieldType ==
                typeof(float))
            {
                radiusField.SetValue(
                    trafficSystem,
                    MotorCityQualityRuntime
                        .TrafficRadius);
            }

            UpdateExistingTraffic();
        }

        private void UpdateExistingTraffic()
        {
            Transform player =
                playerField != null &&
                typeof(Transform).IsAssignableFrom(
                    playerField.FieldType)
                    ? playerField.GetValue(
                        trafficSystem) as Transform
                    : null;

            GameObject container =
                GameObject.Find(
                    "CarContainer");

            if (container == null)
                return;

            List<TrafficCandidate> candidates =
                new();

            foreach (Transform child in
                     container.transform)
            {
                if (child == null)
                    continue;

                MonoBehaviour trafficCar =
                    null;

                foreach (MonoBehaviour behaviour in
                         child.GetComponents<MonoBehaviour>())
                {
                    if (behaviour != null &&
                        behaviour.GetType().Name ==
                        "TrafficCar")
                    {
                        trafficCar =
                            behaviour;

                        break;
                    }
                }

                if (trafficCar == null)
                    continue;

                FieldInfo destroyDistanceField =
                    trafficCar.GetType().GetField(
                        "distanceToSelfDestroy");

                if (destroyDistanceField != null &&
                    destroyDistanceField.FieldType ==
                    typeof(float))
                {
                    destroyDistanceField.SetValue(
                        trafficCar,
                        MotorCityQualityRuntime
                            .TrafficRadius);
                }

                float distance =
                    player == null
                        ? 0f
                        : Vector3.Distance(
                            player.position,
                            child.position);

                candidates.Add(
                    new TrafficCandidate
                    {
                        Object =
                            child.gameObject,

                        Distance =
                            distance
                    });
            }

            int excess =
                candidates.Count -
                MotorCityQualityRuntime
                    .TrafficVehicleBudget;

            if (excess <= 0 ||
                player == null)
            {
                return;
            }

            candidates.Sort(
                (a, b) =>
                    b.Distance.CompareTo(
                        a.Distance));

            for (int i = 0;
                 i < candidates.Count &&
                 excess > 0;
                 i++)
            {
                TrafficCandidate candidate =
                    candidates[i];

                if (candidate.Object == null ||
                    candidate.Distance < 85f)
                {
                    continue;
                }

                Destroy(
                    candidate.Object);

                excess--;
            }
        }

        private struct TrafficCandidate
        {
            public GameObject Object;
            public float Distance;
        }
    }
}
