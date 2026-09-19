using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.World
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CarReset : MonoBehaviour
    {
        private ArcadeCarController car;
        private Rigidbody body;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();

            body =
                GetComponent<Rigidbody>();
        }

        private void Update()
        {
            Keyboard keyboard =
                Keyboard.current;

            bool resetPressed =
                keyboard != null &&
                keyboard.rKey.wasPressedThisFrame;

            if (resetPressed ||
                transform.position.y < -10f)
            {
                ResetVehicle();
            }
        }

        public void ResetVehicle()
        {
            Vector3 currentPosition =
                body != null
                    ? body.position
                    : transform.position;

            Vector3 currentForward =
                transform.forward;

            CityAssetRuntimeInstaller.ResolveNearestRoadResetPose(
                currentPosition,
                currentForward,
                out Vector3 resetPosition,
                out Quaternion resetRotation);

            if (car != null)
            {
                car.TeleportTo(
                    resetPosition,
                    resetRotation);

                car.SetDrivingEnabled(
                    true);

                return;
            }

            if (body != null)
            {
                body.linearVelocity =
                    Vector3.zero;

                body.angularVelocity =
                    Vector3.zero;

                body.position =
                    resetPosition;

                body.rotation =
                    resetRotation;

                body.Sleep();
                body.WakeUp();

                return;
            }

            transform.SetPositionAndRotation(
                resetPosition,
                resetRotation);
        }
    }
}
