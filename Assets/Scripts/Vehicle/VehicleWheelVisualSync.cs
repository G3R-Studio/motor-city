using UnityEngine;

namespace MotorCity.Vehicle
{
    [DefaultExecutionOrder(1000)]
    public sealed class VehicleWheelVisualSync : MonoBehaviour
    {
        private readonly WheelCollider[] wheels =
            new WheelCollider[4];

        private readonly Transform[] visualRoots =
            new Transform[4];

        private bool ready;

        public void Clear()
        {
            ready = false;

            for (int i = 0;
                 i < 4;
                 i++)
            {
                wheels[i] = null;
                visualRoots[i] = null;
            }
        }

        public void Bind(
            ArcadeCarController car,
            Transform[] roots)
        {
            ready = false;

            if (car == null ||
                roots == null ||
                roots.Length < 4)
                return;

            for (int i = 0;
                 i < 4;
                 i++)
            {
                wheels[i] =
                    car.GetWheelCollider(i);

                visualRoots[i] =
                    roots[i];
            }

            ready =
                wheels[0] != null &&
                wheels[1] != null &&
                wheels[2] != null &&
                wheels[3] != null &&
                visualRoots[0] != null &&
                visualRoots[1] != null &&
                visualRoots[2] != null &&
                visualRoots[3] != null;

            if (ready)
                ApplyPose();
        }

        private void LateUpdate()
        {
            if (!ready)
                return;

            ApplyPose();
        }

        private void ApplyPose()
        {
            for (int i = 0;
                 i < 4;
                 i++)
            {
                WheelCollider wheel =
                    wheels[i];

                Transform visual =
                    visualRoots[i];

                if (wheel == null ||
                    visual == null)
                    continue;

                wheel.GetWorldPose(
                    out Vector3 position,
                    out Quaternion rotation);

                visual.SetPositionAndRotation(
                    position,
                    rotation);
            }
        }
    }
}
