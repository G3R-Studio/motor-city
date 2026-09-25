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

        private readonly Vector3[] visualPositionOffsets =
            new Vector3[4];

        private readonly Quaternion[] visualRotationOffsets =
            new Quaternion[4];

        private readonly Transform[] additionalVisualRoots =
            new Transform[2];

        private readonly int[] additionalSourceWheelIndices =
            new int[2];

        private readonly Vector3[] additionalOffsetsLocal =
            new Vector3[2];

        private Transform carTransform;
        private bool ready;

        public void Clear()
        {
            ready = false;
            carTransform = null;

            for (int i = 0;
                 i < 4;
                 i++)
            {
                wheels[i] = null;
                visualRoots[i] = null;
                visualPositionOffsets[i] = Vector3.zero;
                visualRotationOffsets[i] = Quaternion.identity;
            }

            for (int i = 0;
                 i < additionalVisualRoots.Length;
                 i++)
            {
                additionalVisualRoots[i] = null;
                additionalSourceWheelIndices[i] = -1;
                additionalOffsetsLocal[i] = Vector3.zero;
            }
        }

        public void Bind(
            ArcadeCarController car,
            Transform[] roots)
        {
            Clear();

            if (car == null ||
                roots == null ||
                roots.Length < 4)
                return;

            carTransform =
                car.transform;

            for (int i = 0;
                 i < 4;
                 i++)
            {
                wheels[i] =
                    car.GetWheelCollider(i);

                visualRoots[i] =
                    roots[i];

                if (wheels[i] != null &&
                    visualRoots[i] != null)
                {
                    wheels[i].GetWorldPose(
                        out Vector3 posePosition,
                        out Quaternion poseRotation);

                    visualPositionOffsets[i] =
                        Quaternion.Inverse(
                            poseRotation) *
                        (visualRoots[i].position -
                         posePosition);

                    visualRotationOffsets[i] =
                        Quaternion.Inverse(
                            poseRotation) *
                        visualRoots[i].rotation;
                }
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

        public void BindAdditionalVisual(
            int slot,
            Transform visualRoot,
            int sourceWheelIndex,
            Vector3 offsetLocal)
        {
            if (slot < 0 ||
                slot >= additionalVisualRoots.Length ||
                visualRoot == null ||
                sourceWheelIndex < 0 ||
                sourceWheelIndex >= wheels.Length)
            {
                return;
            }

            additionalVisualRoots[slot] =
                visualRoot;

            additionalSourceWheelIndices[slot] =
                sourceWheelIndex;

            additionalOffsetsLocal[slot] =
                offsetLocal;

            if (ready)
                ApplyAdditionalPose(slot);
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
                    position +
                    rotation *
                    visualPositionOffsets[i],
                    rotation *
                    visualRotationOffsets[i]);
            }

            for (int i = 0;
                 i < additionalVisualRoots.Length;
                 i++)
            {
                ApplyAdditionalPose(i);
            }
        }

        private void ApplyAdditionalPose(
            int slot)
        {
            if (carTransform == null ||
                slot < 0 ||
                slot >= additionalVisualRoots.Length)
            {
                return;
            }

            Transform visual =
                additionalVisualRoots[slot];

            int sourceIndex =
                additionalSourceWheelIndices[slot];

            if (visual == null ||
                sourceIndex < 0 ||
                sourceIndex >= wheels.Length)
            {
                return;
            }

            WheelCollider wheel =
                wheels[sourceIndex];

            if (wheel == null)
                return;

            wheel.GetWorldPose(
                out Vector3 position,
                out Quaternion rotation);

            position +=
                carTransform.TransformVector(
                    additionalOffsetsLocal[slot]);

            visual.SetPositionAndRotation(
                position,
                rotation);
        }
    }
}
