using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using MotorCity.Input;
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
        [SerializeField] private float driftLookInfluence = 0.45f;
        [SerializeField] private float maxDriftLookAngle = 22f;
        [SerializeField] private float teleportSnapDistance = 28f;

        [Header("Obstacle Avoidance")]
        [SerializeField] private float collisionRadius = 0.32f;
        [SerializeField] private float collisionPadding = 0.14f;
        [SerializeField] private float minimumCollisionDistance = 0.65f;
        [SerializeField] private float collisionEnterSmoothTime = 0.035f;
        [SerializeField] private float collisionExitSmoothTime = 0.18f;

        [Header("Orbit")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float touchSensitivity = 0.11f;
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
        private Vector3 lastTargetPosition;
        private bool hasLastTargetPosition;
        private int cameraTouchId = -1;
        private Vector2 lastCameraTouchPosition;

        private bool openingPresentationArmed;
        private bool openingPresentationActive;
        private bool manualInputEnabled = true;
        private bool garageMode;
        private bool garagePoseSnapPending;
        private Vector3 garageInitialPosition;
        private Quaternion garageInitialRotation;
        private float openingPresentationTimer;
        private float openingPresentationDuration = 1.6f;
        private Vector3 openingPresentationStartPosition;
        private Quaternion openingPresentationStartRotation;
        private float openingPresentationStartFov;

        public bool IsOpeningPresentationActive =>
            openingPresentationActive;

        private readonly List<RaycastResult> uiRaycastResults =
            new();

        private readonly RaycastHit[] collisionHits =
            new RaycastHit[32];

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            car = target == null ? null : target.GetComponent<ArcadeCarController>();
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            currentCollisionDistance = targetDistance;
            collisionDistanceVelocity = 0f;

            if (target != null)
            {
                lastTargetPosition =
                    target.position;
            }

            hasLastTargetPosition =
                false;
        }

        public void SetManualInputEnabled(
            bool enabled)
        {
            manualInputEnabled =
                enabled;

            if (enabled)
                return;

            cameraTouchId =
                -1;

            yawOffset =
                0f;

            lastManualInputTime =
                Time.time;
        }

        public void SetGarageMode(
            bool enabled,
            Vector3 initialPosition,
            Quaternion initialRotation)
        {
            garageMode =
                enabled;

            manualInputEnabled =
                true;

            cameraTouchId =
                -1;

            if (!enabled)
            {
                lastManualInputTime =
                    Time.time;

                return;
            }

            garageInitialPosition =
                initialPosition;

            garageInitialRotation =
                initialRotation;

            garagePoseSnapPending =
                true;

            if (target == null)
                return;

            Vector3 pivot =
                target.position +
                Vector3.up *
                height;

            Vector3 offset =
                initialPosition -
                pivot;

            float orbitDistance =
                Mathf.Max(
                    minDistance,
                    offset.magnitude);

            distance =
                Mathf.Clamp(
                    orbitDistance,
                    minDistance,
                    maxDistance);

            targetDistance =
                distance;

            if (offset.sqrMagnitude >
                0.0001f)
            {
                Vector3 direction =
                    offset.normalized;

                pitch =
                    Mathf.Clamp(
                        Mathf.Asin(
                            Mathf.Clamp(
                                direction.y,
                                -1f,
                                1f)) *
                        Mathf.Rad2Deg,
                        minPitch,
                        maxPitch);

                float worldYaw =
                    Mathf.Atan2(
                        -direction.x,
                        -direction.z) *
                    Mathf.Rad2Deg;

                yawOffset =
                    Mathf.DeltaAngle(
                        target.eulerAngles.y,
                        worldYaw);
            }

            currentCollisionDistance =
                distance;

            collisionDistanceVelocity =
                0f;

            lastManualInputTime =
                Time.time;

            hasLastTargetPosition =
                false;

            transform.SetPositionAndRotation(
                initialPosition,
                initialRotation);
        }

        public void ArmOpeningPresentation(
            float duration = 5f)
        {
            openingPresentationDuration =
                Mathf.Max(
                    1.2f,
                    duration);

            openingPresentationTimer =
                0f;

            openingPresentationArmed =
                true;

            openingPresentationActive =
                false;
        }

        public void PlayOpeningPresentation(
            float duration = 5f)
        {
            if (target == null)
                return;

            openingPresentationDuration =
                Mathf.Max(
                    1.2f,
                    duration);

            openingPresentationTimer =
                0f;

            openingPresentationActive =
                true;

            float startYaw =
                target.eulerAngles.y -
                78f;

            Quaternion startOrbit =
                Quaternion.Euler(
                    12f,
                    startYaw,
                    0f);

            openingPresentationStartPosition =
                target.position +
                Vector3.up * 2.15f +
                startOrbit *
                new Vector3(
                    0f,
                    0f,
                    -6.4f);

            Vector3 lookPoint =
                target.position +
                Vector3.up * 0.95f;

            openingPresentationStartRotation =
                Quaternion.LookRotation(
                    lookPoint -
                    openingPresentationStartPosition,
                    Vector3.up);

            openingPresentationStartFov =
                58f;

            transform.position =
                openingPresentationStartPosition;

            transform.rotation =
                openingPresentationStartRotation;

            if (cameraComponent == null)
            {
                cameraComponent =
                    GetComponent<Camera>();
            }

            if (cameraComponent != null)
            {
                cameraComponent.fieldOfView =
                    openingPresentationStartFov;
            }

            hasLastTargetPosition =
                false;
        }

        private void Awake()
        {
            if (!EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Enable();
            }

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

            if (openingPresentationArmed &&
                Time.timeScale > 0f)
            {
                openingPresentationArmed =
                    false;

                PlayOpeningPresentation(
                    openingPresentationDuration);
            }

            if (openingPresentationActive)
                return;

            if (!manualInputEnabled)
            {
                cameraTouchId =
                    -1;

                return;
            }

            Mouse mouse = Mouse.current;
            bool orbiting = mouse != null && mouse.rightButton.isPressed;

            if (orbiting)
            {
                Vector2 delta = mouse.delta.ReadValue();
                ApplyOrbitDelta(
                    delta,
                    mouseSensitivity);
            }

            bool touchOrbiting =
                ProcessTouchOrbit();

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

            if (!garageMode &&
                !orbiting &&
                !touchOrbiting &&
                Time.time - lastManualInputTime > recenterDelay)
            {
                yawOffset =
                    Mathf.LerpAngle(
                        yawOffset,
                        0f,
                        1f -
                        Mathf.Exp(
                            -recenterSpeed *
                            Time.deltaTime));
            }
        }

        private void ApplyOrbitDelta(
            Vector2 delta,
            float sensitivity)
        {
            yawOffset +=
                delta.x *
                sensitivity;

            pitch -=
                delta.y *
                sensitivity;

            pitch =
                Mathf.Clamp(
                    pitch,
                    minPitch,
                    maxPitch);

            lastManualInputTime =
                Time.time;
        }

        private bool ProcessTouchOrbit()
        {
            if (!MotorCityInput.PreferTouchPrompts)
            {
                cameraTouchId =
                    -1;

                return false;
            }

            var touches =
                UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

            if (cameraTouchId >= 0)
            {
                for (int i = 0;
                     i < touches.Count;
                     i++)
                {
                    var touch =
                        touches[i];

                    if (touch.touchId !=
                        cameraTouchId)
                    {
                        continue;
                    }

                    if (touch.phase ==
                            UnityEngine.InputSystem.TouchPhase.Ended ||
                        touch.phase ==
                            UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        cameraTouchId =
                            -1;

                        return false;
                    }

                    Vector2 position =
                        touch.screenPosition;

                    Vector2 delta =
                        position -
                        lastCameraTouchPosition;

                    lastCameraTouchPosition =
                        position;

                    ApplyOrbitDelta(
                        delta,
                        touchSensitivity);

                    return true;
                }

                cameraTouchId =
                    -1;
            }

            for (int i = 0;
                 i < touches.Count;
                 i++)
            {
                var touch =
                    touches[i];

                if (touch.phase !=
                    UnityEngine.InputSystem.TouchPhase.Began)
                {
                    continue;
                }

                Vector2 position =
                    touch.screenPosition;

                if (IsScreenPointOverUi(
                        position))
                {
                    continue;
                }

                cameraTouchId =
                    touch.touchId;

                lastCameraTouchPosition =
                    position;

                return true;
            }

            return false;
        }

        private bool IsScreenPointOverUi(
            Vector2 screenPosition)
        {
            EventSystem eventSystem =
                EventSystem.current;

            if (eventSystem == null)
                return false;

            PointerEventData pointer =
                new(eventSystem)
                {
                    position =
                        screenPosition
                };

            uiRaycastResults.Clear();

            eventSystem.RaycastAll(
                pointer,
                uiRaycastResults);

            return
                uiRaycastResults.Count > 0;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (garageMode &&
                garagePoseSnapPending)
            {
                garagePoseSnapPending =
                    false;

                transform.SetPositionAndRotation(
                    garageInitialPosition,
                    garageInitialRotation);

                lastTargetPosition =
                    target.position;

                hasLastTargetPosition =
                    true;

                return;
            }

            float positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

            float speed01 = car == null
                ? 0f
                : Mathf.InverseLerp(20f, 180f, car.SpeedKph);

            float dynamicDistance =
                distance + speedDistanceBonus * speed01;

            float dynamicLookAhead =
                Mathf.Lerp(
                    lookAhead,
                    speedLookAhead,
                    speed01);

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

            bool snapAfterTeleport =
                !hasLastTargetPosition ||
                Vector3.Distance(
                    lastTargetPosition,
                    target.position) >=
                teleportSnapDistance;

            if (snapAfterTeleport)
            {
                currentCollisionDistance =
                    dynamicDistance;

                collisionDistanceVelocity =
                    0f;
            }

            Vector3 collisionSafePosition =
                ResolveStableCameraPosition(
                    cameraPivot,
                    desiredPosition);

            if (openingPresentationActive)
            {
                openingPresentationTimer +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        openingPresentationTimer /
                        openingPresentationDuration);

                float eased =
                    progress * progress *
                    (3f - 2f * progress);

                // Real orbit: sweep from the rear-left quarter around the
                // vehicle into the normal chase position while following the
                // car vertically as it settles onto the road.
                float orbitYaw =
                    Mathf.Lerp(
                        -78f,
                        0f,
                        eased);

                float orbitPitch =
                    Mathf.Lerp(
                        12f,
                        pitch,
                        eased);

                float orbitDistance =
                    Mathf.Lerp(
                        6.4f,
                        dynamicDistance,
                        eased);

                float orbitHeight =
                    Mathf.Lerp(
                        2.15f,
                        height,
                        eased);

                Quaternion openingOrbit =
                    Quaternion.Euler(
                        orbitPitch,
                        target.eulerAngles.y +
                        orbitYaw,
                        0f);

                Vector3 openingPivot =
                    target.position +
                    Vector3.up *
                    orbitHeight;

                Vector3 orbitPosition =
                    openingPivot +
                    openingOrbit *
                    new Vector3(
                        0f,
                        0f,
                        -orbitDistance);

                transform.position =
                    ResolveStableCameraPosition(
                        openingPivot,
                        orbitPosition);

                Vector3 openingLookPoint =
                    target.position +
                    target.forward *
                    Mathf.Lerp(
                        0.35f,
                        dynamicLookAhead,
                        eased) +
                    Vector3.up * 0.92f;

                transform.rotation =
                    Quaternion.LookRotation(
                        openingLookPoint -
                        transform.position,
                        Vector3.up);

                if (cameraComponent != null)
                {
                    cameraComponent.fieldOfView =
                        Mathf.Lerp(
                            openingPresentationStartFov,
                            baseFieldOfView,
                            eased);
                }

                lastTargetPosition =
                    target.position;

                hasLastTargetPosition =
                    true;

                if (progress >= 1f)
                {
                    openingPresentationActive =
                        false;
                }

                return;
            }

            transform.position =
                snapAfterTeleport
                    ? collisionSafePosition
                    : Vector3.Lerp(
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

            Vector3 lookDirection =
                target.forward;

            if (car != null &&
                car.SpeedKph >= 25f)
            {
                float driftLookAngle =
                    Mathf.Clamp(
                        car.SlipAngleDegrees,
                        -maxDriftLookAngle,
                        maxDriftLookAngle) *
                    driftLookInfluence;

                lookDirection =
                    Quaternion.AngleAxis(
                        driftLookAngle,
                        Vector3.up) *
                    target.forward;
            }

            Vector3 lookPoint =
                target.position +
                lookDirection * dynamicLookAhead +
                Vector3.up * 0.92f;

            Quaternion desiredRotation =
                Quaternion.LookRotation(
                    lookPoint - transform.position,
                    Vector3.up);

            transform.rotation =
                snapAfterTeleport
                    ? desiredRotation
                    : Quaternion.Slerp(
                        transform.rotation,
                        desiredRotation,
                        rotationT);

            lastTargetPosition =
                target.position;

            hasLastTargetPosition =
                true;

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
