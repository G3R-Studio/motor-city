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
        [SerializeField] private float speedLookAhead = 3.2f;
        [SerializeField] private float speedDistanceBonus = 0.25f;
        [SerializeField] private float baseFieldOfView = 62f;
        [SerializeField] private float garageFieldOfView = 54f;
        [SerializeField] private float garageDistance = 6.3f;
        [SerializeField] private float highSpeedFieldOfView = 77f;
        [SerializeField] private float fieldOfViewGainSharpness = 5.5f;
        [SerializeField] private float fieldOfViewReturnSharpness = 2.8f;
        [SerializeField] private float driftLookInfluence = 0.45f;
        [SerializeField] private float maxDriftLookAngle = 22f;

        [Header("Turn Camera Lag")]
        [SerializeField] private float turnLagInfluence = 0.055f;
        [SerializeField] private float maxTurnLagAngle = 6f;
        [SerializeField] private float turnLagSharpness = 5.5f;
        [SerializeField] private float minimumTurnLagSpeedKph = 8f;
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
        private Vector3 vehicleVisualCenterLocal;
        private bool hasVehicleVisualCenter;
        private float vehicleVisualCenterRefreshTimer;
        private int cameraTouchId = -1;
        private Vector2 lastCameraTouchPosition;
        private float currentTurnLagYaw;
        private float lastVehicleYaw;
        private bool hasLastVehicleYaw;

        private bool manualInputEnabled = true;
        private bool garageMode;
        private bool garagePoseSnapPending;
        private Vector3 garageInitialPosition;

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

            currentTurnLagYaw =
                0f;

            lastVehicleYaw =
                target != null
                    ? target.eulerAngles.y
                    : 0f;

            hasLastVehicleYaw =
                target != null;

            hasVehicleVisualCenter =
                false;

            vehicleVisualCenterRefreshTimer =
                0f;

            RefreshVehicleVisualCenter();
        }

        private void RefreshVehicleVisualCenter()
        {
            if (target == null)
            {
                hasVehicleVisualCenter =
                    false;

                return;
            }

            Renderer[] renderers =
                target.GetComponentsInChildren<Renderer>(
                    true);

            bool found =
                false;

            Bounds bounds =
                default;

            for (int i = 0;
                 i < renderers.Length;
                 i++)
            {
                Renderer renderer =
                    renderers[i];

                if (renderer == null ||
                    !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy ||
                    renderer is ParticleSystemRenderer ||
                    renderer is TrailRenderer ||
                    renderer is LineRenderer ||
                    renderer is SpriteRenderer)
                {
                    continue;
                }

                if (!found)
                {
                    bounds =
                        renderer.bounds;

                    found =
                        true;
                }
                else
                {
                    bounds.Encapsulate(
                        renderer.bounds);
                }
            }

            if (!found)
            {
                vehicleVisualCenterLocal =
                    Vector3.zero;

                hasVehicleVisualCenter =
                    false;

                return;
            }

            vehicleVisualCenterLocal =
                target.InverseTransformPoint(
                    bounds.center);

            hasVehicleVisualCenter =
                true;
        }

        private Vector3 ResolveVehicleCameraBase()
        {
            if (target == null)
                return Vector3.zero;

            if (car != null)
            {
                Vector3 wheelCenter =
                    Vector3.zero;

                int wheelCount =
                    0;

                for (int i = 0;
                     i < 4;
                     i++)
                {
                    WheelCollider wheel =
                        car.GetWheelCollider(
                            i);

                    if (wheel == null)
                        continue;

                    wheelCenter +=
                        wheel.transform.position;

                    wheelCount++;
                }

                if (wheelCount >= 2)
                {
                    wheelCenter /=
                        wheelCount;

                    // WheelColliders give the true center of the wheelbase,
                    // independent of an imported model/root pivot near the hood.
                    wheelCenter.y =
                        target.position.y;

                    return wheelCenter;
                }
            }

            vehicleVisualCenterRefreshTimer -=
                Time.unscaledDeltaTime;

            if (!hasVehicleVisualCenter ||
                vehicleVisualCenterRefreshTimer <= 0f)
            {
                vehicleVisualCenterRefreshTimer =
                    0.35f;

                RefreshVehicleVisualCenter();
            }

            if (!hasVehicleVisualCenter)
                return target.position;

            Vector3 visualCenter =
                target.TransformPoint(
                    vehicleVisualCenterLocal);

            visualCenter.y =
                target.position.y;

            return visualCenter;
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

            if (cameraComponent == null)
            {
                cameraComponent =
                    GetComponent<Camera>();
            }

            if (cameraComponent != null)
            {
                cameraComponent.fieldOfView =
                    garageFieldOfView;
            }

            garageInitialPosition =
                initialPosition;

            currentTurnLagYaw =
                0f;

            hasLastVehicleYaw =
                false;

            garagePoseSnapPending =
                true;

            if (target == null)
                return;

            Vector3 pivot =
                ResolveVehicleCameraBase() +
                Vector3.up *
                height;

            Vector3 offset =
                initialPosition -
                pivot;

            Vector3 direction =
                offset.sqrMagnitude >
                0.0001f
                    ? offset.normalized
                    : -target.forward;

            distance =
                Mathf.Clamp(
                    garageDistance,
                    minDistance,
                    maxDistance);

            targetDistance =
                distance;

            garageInitialPosition =
                pivot +
                direction *
                distance;

            if (direction.sqrMagnitude >
                0.0001f)
            {

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
                garageInitialPosition,
                ResolveGarageFraming(garageInitialPosition));
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

        private Quaternion ResolveGarageFraming(Vector3 cameraPosition)
        {
            // Center of the open reference-layout area: x=102..1165,
            // y=94..694 on a 1672x941 canvas (viewport Y starts at the bottom).
            const float viewportX = 633.5f / 1672f;
            const float viewportY = 1f - 394f / 941f;
            Vector3 subject = ResolveVehicleCameraBase() + Vector3.up * .92f;
            Quaternion centered = Quaternion.LookRotation(subject - cameraPosition, Vector3.up);
            float aspect = cameraComponent != null ? cameraComponent.aspect : 16f / 9f;
            float halfHeight = Mathf.Tan(garageFieldOfView * .5f * Mathf.Deg2Rad);
            Vector3 subjectRay = new((viewportX * 2f - 1f) * halfHeight * aspect,
                (viewportY * 2f - 1f) * halfHeight, 1f);
            return centered * Quaternion.Inverse(Quaternion.LookRotation(subjectRay, Vector3.up));
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
                    ResolveGarageFraming(garageInitialPosition));

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

            float speedCurve =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    speed01);

            float dynamicDistance =
                distance +
                speedDistanceBonus *
                speedCurve;

            float dynamicLookAhead =
                Mathf.Lerp(
                    0f,
                    speedLookAhead,
                    speedCurve);

            bool targetTeleported =
                !hasLastTargetPosition ||
                Vector3.Distance(
                    lastTargetPosition,
                    target.position) >=
                teleportSnapDistance;

            float vehicleYaw =
                target.eulerAngles.y;

            bool manualOrbitActive =
                (Mouse.current != null &&
                 Mouse.current.rightButton.isPressed) ||
                cameraTouchId >= 0;

            float targetTurnLagYaw =
                0f;

            if (!garageMode &&
                !manualOrbitActive &&
                !targetTeleported &&
                hasLastVehicleYaw &&
                car != null &&
                car.SpeedKph >=
                    minimumTurnLagSpeedKph)
            {
                float yawDelta =
                    Mathf.DeltaAngle(
                        lastVehicleYaw,
                        vehicleYaw);

                float yawSpeed =
                    yawDelta /
                    Mathf.Max(
                        Time.deltaTime,
                        0.0001f);

                float speedInfluence =
                    Mathf.InverseLerp(
                        minimumTurnLagSpeedKph,
                        80f,
                        car.SpeedKph);

                targetTurnLagYaw =
                    Mathf.Clamp(
                        -yawSpeed *
                        turnLagInfluence *
                        speedInfluence,
                        -maxTurnLagAngle,
                        maxTurnLagAngle);
            }

            if (targetTeleported ||
                garageMode)
            {
                currentTurnLagYaw =
                    0f;
            }
            else
            {
                currentTurnLagYaw =
                    Mathf.Lerp(
                        currentTurnLagYaw,
                        targetTurnLagYaw,
                        1f -
                        Mathf.Exp(
                            -turnLagSharpness *
                            Time.deltaTime));
            }

            Quaternion orbitRotation =
                Quaternion.Euler(
                    pitch,
                    vehicleYaw +
                    yawOffset +
                    currentTurnLagYaw,
                    0f);

            Vector3 orbitOffset =
                orbitRotation *
                new Vector3(0f, 0f, -dynamicDistance);

            Vector3 cameraBase =
                ResolveVehicleCameraBase();

            Vector3 cameraPivot =
                cameraBase +
                Vector3.up * height;

            Vector3 desiredPosition =
                cameraPivot +
                orbitOffset;

            bool snapAfterTeleport =
                targetTeleported;

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

            if (!snapAfterTeleport &&
                hasLastTargetPosition)
            {
                // Carry the camera by the vehicle's world translation before
                // smoothing the relative offset. Without this, the position
                // lerp produces several metres of artificial trailing at high
                // speed, making the camera appear to fall behind the car.
                transform.position +=
                    target.position -
                    lastTargetPosition;
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
                cameraBase +
                lookDirection * dynamicLookAhead +
                Vector3.up * 0.92f;

            Quaternion desiredRotation =
                Quaternion.LookRotation(
                    lookPoint - transform.position,
                    Vector3.up);

            if (garageMode)
                desiredRotation = ResolveGarageFraming(transform.position);

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

            lastVehicleYaw =
                vehicleYaw;

            hasLastVehicleYaw =
                true;

            if (cameraComponent != null)
            {
                float targetFov =
                    garageMode
                        ? garageFieldOfView
                        : Mathf.Lerp(
                            baseFieldOfView,
                            highSpeedFieldOfView,
                            speedCurve);

                float fovSharpness =
                    targetFov >
                    cameraComponent.fieldOfView
                        ? fieldOfViewGainSharpness
                        : fieldOfViewReturnSharpness;

                cameraComponent.fieldOfView =
                    Mathf.Lerp(
                        cameraComponent.fieldOfView,
                        targetFov,
                        1f - Mathf.Exp(
                            -fovSharpness *
                            Time.deltaTime));
            }
        }
    }
}
