using UnityEngine;

namespace MotorCity.Vehicle
{
    [DefaultExecutionOrder(500)]
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class HandbrakePhysicsAssist : MonoBehaviour
    {
        [SerializeField] private float rearBrakeTorque = 5200f;
        [SerializeField] private float lowSpeedRearBrakeTorque = 9500f;
        [SerializeField] private float lowSpeedThresholdKph = 18f;
        [SerializeField] private float holdThresholdKph = 1.4f;

        private ArcadeCarController car;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
        }

        private void FixedUpdate()
        {
            if (car == null)
                return;

            car.ApplyPhysicalHandbrake(
                rearBrakeTorque,
                lowSpeedRearBrakeTorque,
                lowSpeedThresholdKph,
                holdThresholdKph);
        }
    }
}
