using UnityEngine;

namespace MotorCity.World
{
    [RequireComponent(typeof(MotorCity.Vehicle.ArcadeCarController))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BreakablePropImpactSensor : MonoBehaviour
    {
        private const int HitBufferSize =
            64;

        private readonly RaycastHit[] castHits =
            new RaycastHit[HitBufferSize];

        private readonly Collider[] overlapHits =
            new Collider[HitBufferSize];

        private MotorCity.Vehicle.ArcadeCarController car;
        private BoxCollider chassis;
        private Vector3 previousCenter;
        private Quaternion previousRotation;
        private bool initialized;

        private void Awake()
        {
            car =
                GetComponent<MotorCity.Vehicle.ArcadeCarController>();

            ResolveChassis();
            RememberPose();
        }

        private void FixedUpdate()
        {
            if (car == null)
                return;

            if (chassis == null ||
                !chassis.enabled ||
                chassis.isTrigger)
            {
                ResolveChassis();

                if (chassis == null)
                    return;
            }

            Vector3 currentCenter =
                chassis.transform.TransformPoint(
                    chassis.center);

            Quaternion currentRotation =
                chassis.transform.rotation;

            Vector3 halfExtents =
                ScaledHalfExtents(
                    chassis);

            if (!initialized)
            {
                previousCenter =
                    currentCenter;

                previousRotation =
                    currentRotation;

                initialized =
                    true;
            }

            int overlapCount =
                Physics.OverlapBoxNonAlloc(
                    currentCenter,
                    halfExtents *
                    0.96f,
                    overlapHits,
                    currentRotation,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide);

            for (int i = 0;
                 i < overlapCount;
                 i++)
            {
                TryBreak(
                    overlapHits[i]);
            }

            Vector3 movement =
                currentCenter -
                previousCenter;

            float distance =
                movement.magnitude;

            if (distance > 0.015f)
            {
                int hitCount =
                    Physics.BoxCastNonAlloc(
                        previousCenter,
                        halfExtents *
                        0.92f,
                        movement /
                        distance,
                        castHits,
                        previousRotation,
                        distance +
                        0.08f,
                        Physics.AllLayers,
                        QueryTriggerInteraction.Collide);

                for (int i = 0;
                     i < hitCount;
                     i++)
                {
                    TryBreak(
                        castHits[i].collider);
                }
            }

            previousCenter =
                currentCenter;

            previousRotation =
                currentRotation;
        }

        private void ResolveChassis()
        {
            chassis =
                null;

            foreach (BoxCollider box in
                     GetComponents<BoxCollider>())
            {
                if (box == null ||
                    !box.enabled ||
                    box.isTrigger)
                    continue;

                chassis =
                    box;

                break;
            }
        }

        private void RememberPose()
        {
            if (chassis == null)
                return;

            previousCenter =
                chassis.transform.TransformPoint(
                    chassis.center);

            previousRotation =
                chassis.transform.rotation;

            initialized =
                true;
        }

        private void TryBreak(
            Collider collider)
        {
            if (collider == null)
                return;

            BreakableStreetProp prop =
                collider.GetComponentInParent<BreakableStreetProp>();

            if (prop == null)
                return;

            prop.TryBreakFromCar(
                car);
        }

        private static Vector3 ScaledHalfExtents(
            BoxCollider box)
        {
            Vector3 scale =
                box.transform.lossyScale;

            scale =
                new Vector3(
                    Mathf.Abs(
                        scale.x),
                    Mathf.Abs(
                        scale.y),
                    Mathf.Abs(
                        scale.z));

            return
                Vector3.Scale(
                    box.size *
                    0.5f,
                    scale);
        }
    }
}
