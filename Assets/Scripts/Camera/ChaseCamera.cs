using UnityEngine;
using UnityEngine.InputSystem;
using MotorCity.Vehicle;

namespace MotorCity.CameraSystem
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 6.8f;
        [SerializeField] private float height = 2.25f;
        [SerializeField] private float positionSharpness = 7.5f;
        [SerializeField] private float rotationSharpness = 10f;
        [SerializeField] private float lookAhead = 2.2f;
        [SerializeField] private float speedLookAhead = 3.8f;
        [SerializeField] private float speedDistanceBonus = 1.25f;
        [SerializeField] private float baseFieldOfView = 62f;
        [SerializeField] private float highSpeedFieldOfView = 72f;
        [SerializeField] private float fieldOfViewSharpness = 4.5f;

        [Header("Obstacle Avoidance")]
        [SerializeField] private float collisionRadius = 0.32f;
        [SerializeField] private float collisionPadding = 0.14f;
        [SerializeField] private float minimumCollisionDistance = 0.65f;
        [SerializeField] private float collisionEnterSmoothTime = 0.035f;
        [SerializeField] private float collisionExitSmoothTime = 0.18f;

        [Header("Orbit")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -8f;
        [SerializeField] private float maxPitch = 55f;
        [SerializeField] private float minDistance = 4.5f;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private float zoomStep = 0.9f;
        [SerializeField] private float zoomSharpness = 12f;
        [SerializeField] private float recenterDelay = 1.35f;
        [SerializeField] private float recenterSpeed = 3f;

        private float yawOffset;
        private float pitch = 13f;
        private float lastManualInputTime = -10f;
        private float targetDistance;
        private ArcadeCarController car;
        private Camera cameraComponent;
        private float currentCollisionDistance;
        private float collisionDistanceVelocity;

        private readonly RaycastHit[] collisionHits =
            new RaycastHit[32];

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            car = target == null ? null : target.GetComponent<ArcadeCarController>();
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            currentCollisionDistance = targetDistance;
            collisionDistanceVelocity = 0f;
        }

        private void Awake()
        {
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            currentCollisionDistance = targetDistance;
            collisionDistanceVelocity = 0f;
            cameraComponent = GetComponent<Camera>();
            RemoveCameraPhysics();
        }

        private void RemoveCameraPhysics()
        {
            foreach (Collider collider in
                     GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                {
                    collider.enabled =
                        false;

                    Destroy(
                        collider);
                }
            }

            foreach (Rigidbody body in
                     GetComponentsInChildren<Rigidbody>(true))
            {
                if (body != null)
                {
                    body.detectCollisions =
                        false;

                    body.isKinematic =
                        true;

                    Destroy(
                        body);
                }
            }
        }

        private float ResolveObstacleDistance(
            Vector3 pivot,
            Vector3 desiredPosition)
        {
            Vector3 offset =
                desiredPosition -
                pivot;

            float distanceToCandidate =
                offset.magnitude;

            if (distanceToCandidate <=
                0.001f)
            {
                return
                    minimumCollisionDistance;
            }

            Vector3 direction =
                offset /
                distanceToCandidate;

            int hitCount =
                Physics.SphereCastNonAlloc(
                    pivot,
                    collisionRadius,
                    direction,
                    collisionHits,
                    distanceToCandidate,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

            float nearestDistance =
                distanceToCandidate;

            for (int i = 0;
                 i < hitCount;
                 i++)
            {
                Collider collider =
                    collisionHits[i].collider;

                if (collider == null)
                    continue;

                if (target != null &&
                    (collider.transform == target ||
                     collider.transform.IsChildOf(
                         target)))
                {
                    continue;
                }

                nearestDistance =
                    Mathf.Min(
                        nearestDistance,
                        collisionHits[i].distance);
            }

            if (nearestDistance >=
                distanceToCandidate -
                0.001f)
            {
                return
                    distanceToCandidate;
            }

            return
                Mathf.Clamp(
                    nearestDistance -
                    collisionPadding,
                    minimumCollisionDistance,
                    distanceToCandidate);
        }

        private Vector3 ResolveStableCameraPosition(
            Vector3 pivot,
            Vector3 desiredPosition)
        {
            Vector3 offset =
                desiredPosition -
                pivot;

            float desiredDistance =
                offset.magnitude;

            if (desiredDistance <=
                0.001f)
            {
                return desiredPosition;
            }

            Vector3 direction =
                offset /
                desiredDistance;

            float allowedDistance =
                ResolveObstacleDistance(
                    pivot,
                    desiredPosition);

            if (currentCollisionDistance <=
                0.001f)
            {
                currentCollisionDistance =
                    allowedDistance;
            }

            float smoothTime =
                allowedDistance <
                currentCollisionDistance
                    ? collisionEnterSmoothTime
                    : collisionExitSmoothTime;

            currentCollisionDistance =
                Mathf.SmoothDamp(
                    currentCollisionDistance,
                    allowedDistance,
                    ref collisionDistanceVelocity,
                    Mathf.Max(
                        0.001f,
                        smoothTime),
                    Mathf.Infinity,
                    Time.deltaTime);

            // Never let smoothing overshoot through the obstacle.
            if (allowedDistance <
                currentCollisionDistance)
            {
                currentCollisionDistance =
                    allowedDistance;

                collisionDistanceVelocity =
                    Mathf.Min(
                        0f,
                        collisionDistanceVelocity);
            }

            currentCollisionDistance =
                Mathf.Clamp(
                    currentCollisionDistance,
                    minimumCollisionDistance,
                    desiredDistance);

            return
                pivot +
                direction *
                currentCollisionDistance;
        }

        private void Update()
        {
            if (target == null) return;

            Mouse mouse = Mouse.current;
            bool orbiting = mouse != null && mouse.rightButton.isPressed;

            if (orbiting)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yawOffset += delta.x * mouseSensitivity;
                pitch -= delta.y * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                lastManualInputTime = Time.time;
            }

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    float direction = Mathf.Sign(scroll);
                    targetDistance = Mathf.Clamp(
                        targetDistance - direction * zoomStep,
                        minDistance,
                        maxDistance);
                    lastManualInputTime = Time.time;
                }
            }

            distance = Mathf.Lerp(
                distance,
                targetDistance,
                1f - Mathf.Exp(-zoomSharpness * Time.deltaTime));

            if (!orbiting && Time.time - lastManualInputTime > recenterDelay)
                yawOffset = Mathf.LerpAngle(yawOffset, 0f, 1f - Mathf.Exp(-recenterSpeed * Time.deltaTime));
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

            float speed01 = car == null
                ? 0f
                : Mathf.InverseLerp(20f, 180f, car.SpeedKph);

            float dynamicDistance =
                distance + speedDistanceBonus * speed01;

            Quaternion orbitRotation =
                Quaternion.Euler(
                    pitch,
                    target.eulerAngles.y + yawOffset,
                    0f);

            Vector3 orbitOffset =
                orbitRotation *
                new Vector3(0f, 0f, -dynamicDistance);

            Vector3 cameraPivot =
                target.position +
                Vector3.up * height;

            Vector3 desiredPosition =
                cameraPivot +
                orbitOffset;

            Vector3 collisionSafePosition =
                ResolveStableCameraPosition(
                    cameraPivot,
                    desiredPosition);

            transform.position =
                Vector3.Lerp(
                    transform.position,
                    collisionSafePosition,
                    positionT);

            // A final clamp keeps the interpolated camera outside geometry
            // without repeatedly snapping it in and out every frame.
            float finalAllowedDistance =
                ResolveObstacleDistance(
                    cameraPivot,
                    transform.position);

            Vector3 finalOffset =
                transform.position -
                cameraPivot;

            float finalDistance =
                finalOffset.magnitude;

            if (finalDistance >
                    finalAllowedDistance +
                    0.001f &&
                finalDistance >
                    0.001f)
            {
                transform.position =
                    cameraPivot +
                    finalOffset /
                    finalDistance *
                    finalAllowedDistance;
            }

            float dynamicLookAhead =
                Mathf.Lerp(
                    lookAhead,
                    speedLookAhead,
                    speed01);

            Vector3 lookPoint =
                target.position +
                target.forward * dynamicLookAhead +
                Vector3.up * 0.92f;

            Quaternion desiredRotation =
                Quaternion.LookRotation(
                    lookPoint - transform.position,
                    Vector3.up);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    rotationT);

            if (cameraComponent != null)
            {
                float targetFov =
                    Mathf.Lerp(
                        baseFieldOfView,
                        highSpeedFieldOfView,
                        speed01);

                cameraComponent.fieldOfView =
                    Mathf.Lerp(
                        cameraComponent.fieldOfView,
                        targetFov,
                        1f - Mathf.Exp(
                            -fieldOfViewSharpness *
                            Time.deltaTime));
            }
        }
    }
}
