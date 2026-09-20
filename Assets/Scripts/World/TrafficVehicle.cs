using UnityEngine;

namespace MotorCity.World
{
    public sealed class TrafficVehicle : MonoBehaviour
    {
        private const float TargetRefreshDistance = 4.5f;
        private const float RoadLookAhead = 11f;
        private const float ObstacleDistance = 12f;

        private Rigidbody body;
        private TrafficSystem owner;
        private TrafficSignalController signals;

        private Vector3 roadTarget;
        private float cruiseSpeed;
        private float currentSpeed;
        private float stuckTimer;
        private bool hasTarget;

        public float CurrentSpeed => currentSpeed;

        public void Initialize(
            TrafficSystem trafficOwner,
            TrafficSignalController signalController,
            float speed)
        {
            owner =
                trafficOwner;

            signals =
                signalController;

            cruiseSpeed =
                speed;

            body =
                GetComponent<Rigidbody>();

            ChooseNextTarget();
        }

        private void FixedUpdate()
        {
            if (body == null)
                return;

            Vector3 position =
                body.position;

            Vector3 flatToTarget =
                roadTarget -
                position;

            flatToTarget.y =
                0f;

            if (!hasTarget ||
                flatToTarget.sqrMagnitude <=
                TargetRefreshDistance *
                TargetRefreshDistance)
            {
                ChooseNextTarget();

                flatToTarget =
                    roadTarget -
                    position;

                flatToTarget.y =
                    0f;
            }

            Vector3 desiredForward =
                flatToTarget.sqrMagnitude >
                0.05f
                    ? flatToTarget.normalized
                    : transform.forward;

            float maximumTurn =
                70f *
                Time.fixedDeltaTime;

            Vector3 steeredForward =
                Vector3.RotateTowards(
                    transform.forward,
                    desiredForward,
                    maximumTurn *
                    Mathf.Deg2Rad,
                    0f);

            steeredForward.y =
                0f;

            if (steeredForward.sqrMagnitude <
                0.01f)
            {
                steeredForward =
                    transform.forward;
            }

            steeredForward.Normalize();

            bool blocked =
                IsObstacleAhead(
                    position,
                    steeredForward,
                    out float obstacleDistance);

            bool redLight =
                signals != null &&
                signals.ShouldStop(
                    position,
                    steeredForward);

            float wantedSpeed =
                cruiseSpeed;

            if (blocked)
            {
                wantedSpeed *=
                    Mathf.InverseLerp(
                        2.2f,
                        ObstacleDistance,
                        obstacleDistance);
            }

            if (redLight)
            {
                wantedSpeed =
                    0f;
            }

            float acceleration =
                wantedSpeed >
                currentSpeed
                    ? 4.5f
                    : 10f;

            currentSpeed =
                Mathf.MoveTowards(
                    currentSpeed,
                    wantedSpeed,
                    acceleration *
                    Time.fixedDeltaTime);

            if (currentSpeed <
                0.35f)
            {
                stuckTimer +=
                    Time.fixedDeltaTime;
            }
            else
            {
                stuckTimer =
                    0f;
            }

            if (stuckTimer > 8f &&
                !redLight)
            {
                ChooseNextTarget(
                    true);

                stuckTimer =
                    0f;
            }

            Quaternion rotation =
                Quaternion.LookRotation(
                    steeredForward,
                    Vector3.up);

            Vector3 next =
                position +
                steeredForward *
                currentSpeed *
                Time.fixedDeltaTime;

            if (TrafficRoadUtility.TryGetRoadPoint(
                    next,
                    out Vector3 roadPoint,
                    out _))
            {
                next.y =
                    Mathf.Lerp(
                        next.y,
                        roadPoint.y +
                        0.42f,
                        0.7f);
            }

            body.MoveRotation(
                rotation);

            body.MovePosition(
                next);
        }

        private void ChooseNextTarget(
            bool allowReverse = false)
        {
            Vector3 forward =
                transform.forward;

            if (allowReverse)
            {
                float sign =
                    Random.value < 0.5f
                        ? -1f
                        : 1f;

                forward =
                    Quaternion.Euler(
                        0f,
                        75f * sign,
                        0f) *
                    forward;
            }

            hasTarget =
                TrafficRoadUtility.TryChooseForwardRoadTarget(
                    transform.position,
                    forward,
                    RoadLookAhead,
                    out roadTarget);

            if (!hasTarget)
            {
                roadTarget =
                    transform.position +
                    forward *
                    RoadLookAhead;
            }
        }

        private bool IsObstacleAhead(
            Vector3 position,
            Vector3 direction,
            out float distance)
        {
            Vector3 origin =
                position +
                Vector3.up * 0.7f +
                direction * 1.3f;

            RaycastHit[] hits =
                Physics.BoxCastAll(
                    origin,
                    new Vector3(
                        0.72f,
                        0.48f,
                        0.42f),
                    direction,
                    Quaternion.LookRotation(
                        direction,
                        Vector3.up),
                    ObstacleDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore);

            distance =
                ObstacleDistance;

            bool blocked =
                false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (hit.collider.transform.IsChildOf(
                        transform))
                {
                    continue;
                }

                if (TrafficRoadUtility.IsRoadCollider(
                        hit.collider))
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<TrafficVehicle>() ==
                    this)
                {
                    continue;
                }

                distance =
                    Mathf.Min(
                        distance,
                        hit.distance);

                blocked =
                    true;
            }

            return blocked;
        }

        private void OnDestroy()
        {
            if (owner != null)
            {
                owner.NotifyDestroyed(
                    this);
            }
        }
    }
}
