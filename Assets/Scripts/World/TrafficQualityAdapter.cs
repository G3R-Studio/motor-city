using System;
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
        }
    }
}
