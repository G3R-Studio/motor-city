using System;
using System.Collections.Generic;
using System.Reflection;
using MotorCity.Input;
using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.Vehicle
{
    public enum DriveMode
    {
        Comfort = 0,
        Sport = 1,
        Drift = 2
    }

    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        private const int FrontLeft = 0;
        private const int FrontRight = 1;
        private const int RearLeft = 2;
        private const int RearRight = 3;
        private const string DriveModeKey =
            "MotorCity.Vehicle.DriveMode";

        [Header("Prometeo tuning")]
        [SerializeField] private int baseMaxSpeedKph = 180;
        [SerializeField] private int maxReverseSpeedKph = 42;
        [SerializeField] private int accelerationMultiplier = 8;
        [SerializeField] private float steeringSpeed = 0.68f;
        [SerializeField] private int brakeForce = 1500;
        [SerializeField] private int decelerationMultiplier = 2;
        [SerializeField] private int handbrakeDriftMultiplier = 7;
        [SerializeField] private Vector3 bodyMassCenter =
            new(0f, 0.32f, 0.05f);

        [Header("Vehicle body")]
        [SerializeField] private float vehicleMass = 1280f;
        [SerializeField] private float angularDamping = 0.22f;

        [Header("Power assist")]
        [SerializeField] private float basePowerAssistAcceleration = 2.8f;
        [SerializeField] private float driftPowerAssistAcceleration = 1.8f;
        [SerializeField] private float reversePowerAssistAcceleration = 1.6f;
        [SerializeField] private float powerAssistFadeStartKph = 115f;

        [Header("Prometeo WheelColliders")]
        [SerializeField] private float fallbackWheelRadius = 0.36f;
        [SerializeField] private float wheelMass = 20f;
        [SerializeField] private float suspensionDistance = 0.24f;
        [SerializeField] private float suspensionSpring = 36000f;
        [SerializeField] private float suspensionDamper = 4400f;
        [SerializeField] private float suspensionTargetPosition = 0.5f;
        [SerializeField] private float wheelDampingRate = 0.3f;
        [SerializeField] private float forceAppPointDistance = 0.05f;

        private float activeSuspensionDistance;
        private float activeSuspensionSpring;
        private float activeSuspensionDamper;
        private float activeSuspensionTargetPosition;

        [Header("Drift telemetry")]
        [SerializeField] private float driftDetectionSideSlip = 0.10f;
        [SerializeField] private float driftDetectionAngle = 7f;
        [SerializeField] private float minimumDriftSpeedKph = 22f;

        private readonly WheelCollider[] wheelColliders =
            new WheelCollider[4];

        private readonly GameObject[] wheelMeshes =
            new GameObject[4];

        private Rigidbody body;
        private Component prometeo;
        private Type prometeoType;
        private Component throttleInputProxy;
        private Component reverseInputProxy;
        private Component leftInputProxy;
        private Component rightInputProxy;
        private Component handbrakeInputProxy;
        private Type prometeoTouchInputType;
        private FieldInfo inputProxyPressedField;
        private PropertyInfo inputProxyPressedProperty;
        private FieldInfo prometeoTractionLockedField;
        private FieldInfo prometeoDriftingField;
        private bool? lastThrottleProxyPressed;
        private bool? lastReverseProxyPressed;
        private bool? lastLeftProxyPressed;
        private bool? lastRightProxyPressed;
        private bool? lastHandbrakeProxyPressed;

        private bool wheelRigReady;
        private bool drivingEnabled = true;
        private readonly HashSet<string> drivingBlockers =
            new(StringComparer.Ordinal);
        private bool presentationLock;
        private float presentationLockTimer;
        private bool garagePresentationMode;
        private bool warnedMissingPrometeo;
        private bool throttleHeld;
        private bool reverseHeld;
        private bool handbrakeHeld;
        private bool steeringInputHeld;
        private float prometeoRetryTimer;
        private float resetHoldTimer;
        private float driveModeFrictionRefreshTimer;
        private float turboAssistMultiplier = 1f;
        private float turboAssistTimer;

        private int engineUpgradeLevel;
        private int gripUpgradeLevel;
        private int stabilityUpgradeLevel;
        private int vehicleBaseMaxSpeedKph =
            155;
        private int vehicleBaseAccelerationTune =
            7;
        private float vehicleGripMultiplier = 1f;
        private float vehicleStabilityBonus;
        private float vehicleMassMultiplier = 1f;
        private float vehicleSteeringMultiplier = 1f;
        private float vehicleBrakeMultiplier = 1f;
        private float vehiclePowerMultiplier = 1f;
        private float vehicleDriftMultiplier = 1f;
        private float frontDriveTorqueMultiplier = 1f;
        private float rearDriveTorqueMultiplier = 1f;
        private int vehicleMasteryLevel = 1;
        private DriveMode currentDriveMode =
            DriveMode.Comfort;
        private float driveModeMessageTimer;
        private string controlHintMessage;
        private float controlHintMessageTimer;

        public DriveMode CurrentDriveMode =>
            currentDriveMode;

        public string DriveModeDisplayName =>
            currentDriveMode switch
            {
                DriveMode.Sport =>
                    MotorCityLocalization.Text(
                        "drive.sport.name"),
                DriveMode.Drift =>
                    MotorCityLocalization.Text(
                        "drive.drift.name"),
                _ =>
                    MotorCityLocalization.Text(
                        "drive.comfort.name")
            };

        public string DriveModeDescription =>
            currentDriveMode switch
            {
                DriveMode.Sport =>
                    MotorCityLocalization.Text(
                        "drive.sport.desc"),
                DriveMode.Drift =>
                    MotorCityLocalization.Text(
                        "drive.drift.desc"),
                _ =>
                    MotorCityLocalization.Text(
                        "drive.comfort.desc")
            };

        public bool ShowDriveModeMessage =>
            driveModeMessageTimer > 0f;

        public bool ShowControlHintMessage =>
            controlHintMessageTimer > 0f &&
            !string.IsNullOrWhiteSpace(
                controlHintMessage);

        public string ControlHintMessage =>
            ShowControlHintMessage
                ? controlHintMessage
                : string.Empty;

        public float SpeedKph =>
            body == null
                ? 0f
                : body.linearVelocity.magnitude * 3.6f;

        public float ForwardSpeedKph =>
            body == null
                ? 0f
                : Vector3.Dot(
                    body.linearVelocity,
                    transform.forward) * 3.6f;

        public float AverageWheelRpm
        {
            get
            {
                float total =
                    0f;

                int count =
                    0;

                for (int i = 0;
                     i < wheelColliders.Length;
                     i++)
                {
                    WheelCollider wheel =
                        wheelColliders[i];

                    if (wheel == null)
                        continue;

                    total +=
                        Mathf.Abs(
                            wheel.rpm);

                    count++;
                }

                return count > 0
                    ? total / count
                    : 0f;
            }
        }

        public bool IsHandbrake =>
            handbrakeHeld ||
            ReadPrometeoBool("isTractionLocked");

        public bool HandbrakeInputHeld =>
            handbrakeHeld;

        public bool HasPrometeoPhysics =>
            prometeo != null;

        public WheelCollider GetWheelCollider(
            int index)
        {
            return index >= 0 &&
                   index < wheelColliders.Length
                ? wheelColliders[index]
                : null;
        }

        public int GroundedWheels { get; private set; }
        public float RearSidewaysSlip { get; private set; }
        public float RearForwardSlip { get; private set; }
        public bool IsSliding { get; private set; }
        public float DriftIntensity { get; private set; }

        public float SlipAngleDegrees
        {
            get
            {
                if (body == null ||
                    body.linearVelocity.sqrMagnitude < 1f)
                    return 0f;

                Vector3 localVelocity =
                    transform.InverseTransformDirection(
                        body.linearVelocity);

                return Mathf.Atan2(
                    localVelocity.x,
                    Mathf.Max(
                        0.1f,
                        Mathf.Abs(localVelocity.z))) *
                    Mathf.Rad2Deg;
            }
        }

        private void Awake()
        {
            // Existing serialized scene/prefab values can preserve the older
            // arcade tuning, so enforce the city-scale baseline at runtime.
            baseMaxSpeedKph =
                Mathf.Min(
                    baseMaxSpeedKph,
                    180);

            maxReverseSpeedKph =
                Mathf.Min(
                    maxReverseSpeedKph,
                    42);

            accelerationMultiplier =
                Mathf.Min(
                    accelerationMultiplier,
                    8);

            brakeForce =
                Mathf.Max(
                    brakeForce,
                    1500);

            decelerationMultiplier =
                Mathf.Max(
                    decelerationMultiplier,
                    2);

            basePowerAssistAcceleration =
                Mathf.Min(
                    basePowerAssistAcceleration,
                    2.8f);

            driftPowerAssistAcceleration =
                Mathf.Min(
                    driftPowerAssistAcceleration,
                    1.8f);

            reversePowerAssistAcceleration =
                Mathf.Min(
                    reversePowerAssistAcceleration,
                    1.6f);

            powerAssistFadeStartKph =
                Mathf.Min(
                    powerAssistFadeStartKph,
                    115f);

            currentDriveMode =
                (DriveMode)Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        DriveModeKey,
                        (int)DriveMode.Comfort),
                    0,
                    2);

            body = GetComponent<Rigidbody>();
            body.mass = vehicleMass;
            body.linearDamping = 0.015f;
            body.angularDamping = angularDamping;
            body.useGravity = true;
            body.centerOfMass = bodyMassCenter;
            body.interpolation =
                RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

            activeSuspensionDistance =
                suspensionDistance;
            activeSuspensionSpring =
                suspensionSpring;
            activeSuspensionDamper =
                suspensionDamper;
            activeSuspensionTargetPosition =
                suspensionTargetPosition;

            if (GetComponent<PlayerVehicleAudio>() == null)
            {
                gameObject.AddComponent<PlayerVehicleAudio>();
            }

        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            if (presentationLock)
            {
                presentationLockTimer =
                    Mathf.Max(
                        0f,
                        presentationLockTimer -
                        Time.unscaledDeltaTime);

                if (presentationLockTimer <= 0f)
                {
                    SetPresentationLock(
                        false);
                }
            }

            bool conflictingControlPressed =
                MotorCityInput.DrivingControlHeld;

            if (drivingEnabled &&
                resetHoldTimer <= 0f &&
                MotorCityInput.CycleDriveModePressed &&
                !conflictingControlPressed)
            {
                if (SpeedKph <= 1f)
                {
                    CycleDriveMode();
                }
                else
                {
                    ShowControlHint(
                        MotorCityLocalization.Text(
                            "drive.stop_to_switch"));
                }
            }

            if (drivingEnabled &&
                MotorCityInput.EliteModifierPressed &&
                SpeedKph > 1f)
            {
                ShowControlHint(
                    MotorCityLocalization.Text(
                        "elite.stop_to_use"));
            }

            if (driveModeMessageTimer > 0f)
            {
                driveModeMessageTimer =
                    Mathf.Max(
                        0f,
                        driveModeMessageTimer -
                        Time.deltaTime);
            }

            if (controlHintMessageTimer > 0f)
            {
                controlHintMessageTimer =
                    Mathf.Max(
                        0f,
                        controlHintMessageTimer -
                        Time.unscaledDeltaTime);

                if (controlHintMessageTimer <= 0f)
                {
                    controlHintMessage =
                        null;
                }
            }

            if (turboAssistTimer > 0f)
            {
                turboAssistTimer =
                    Mathf.Max(
                        0f,
                        turboAssistTimer -
                        Time.deltaTime);

                if (turboAssistTimer <= 0f)
                {
                    turboAssistMultiplier =
                        1f;
                }
            }

            if (wheelRigReady &&
                prometeo == null)
            {
                prometeoRetryTimer -= Time.unscaledDeltaTime;

                if (prometeoRetryTimer <= 0f)
                {
                    prometeoRetryTimer = 1f;
                    TryBindPrometeo();
                }
            }

            UpdatePrometeoInputProxies();

            driveModeFrictionRefreshTimer -=
                Time.unscaledDeltaTime;

            if (driveModeFrictionRefreshTimer <= 0f)
            {
                driveModeFrictionRefreshTimer =
                    0.12f;

                ApplyWheelFriction();
            }

            if (resetHoldTimer > 0f)
            {
                resetHoldTimer =
                    Mathf.Max(
                        0f,
                        resetHoldTimer - Time.deltaTime);

                if (resetHoldTimer <= 0f &&
                    drivingEnabled)
                {
                    SetPrometeoEnabled(true);
                    ReleaseResetBrakes();
                }
            }
        }

        public void ActivateTurboAssist(
            float multiplier,
            float seconds)
        {
            turboAssistMultiplier =
                Mathf.Clamp(
                    multiplier,
                    1f,
                    1.6f);

            turboAssistTimer =
                Mathf.Max(
                    turboAssistTimer,
                    Mathf.Clamp(
                        seconds,
                        0f,
                        6f));
        }

        private void FixedUpdate()
        {
            if (resetHoldTimer > 0f)
            {
                HoldVehicleStill();
                return;
            }

            UpdateTelemetry();
            ApplyPowerAssist();
        }

        public void ApplySuspensionPreset(
            float distance,
            float spring,
            float damper,
            float targetPosition,
            float dampingRate)
        {
            suspensionDistance =
                Mathf.Clamp(
                    distance,
                    0.08f,
                    0.35f);

            suspensionSpring =
                Mathf.Clamp(
                    spring,
                    18000f,
                    70000f);

            suspensionDamper =
                Mathf.Clamp(
                    damper,
                    2500f,
                    12000f);

            suspensionTargetPosition =
                Mathf.Clamp01(
                    targetPosition);

            wheelDampingRate =
                Mathf.Clamp(
                    dampingRate,
                    0.1f,
                    1.5f);

            activeSuspensionDistance =
                suspensionDistance;

            activeSuspensionSpring =
                suspensionSpring;

            activeSuspensionDamper =
                suspensionDamper;

            activeSuspensionTargetPosition =
                suspensionTargetPosition;
        }

        public void ConfigurePrometeoRig(
            Transform[] visualWheelRoots,
            Vector3[] wheelCentersLocal,
            float measuredWheelRadius,
            bool externalVisualSync = false)
        {
            if (visualWheelRoots == null ||
                visualWheelRoots.Length < 4 ||
                wheelCentersLocal == null ||
                wheelCentersLocal.Length < 4)
            {
                Debug.LogError(
                    "Motor City: Prometeo rig requires four visual wheels and four wheel centers.");
                return;
            }

            float radius =
                measuredWheelRadius > 0.05f
                    ? Mathf.Clamp(
                        measuredWheelRadius,
                        0.26f,
                        0.58f)
                    : fallbackWheelRadius;

            activeSuspensionDistance =
                suspensionDistance;
            activeSuspensionSpring =
                suspensionSpring;
            activeSuspensionDamper =
                suspensionDamper;
            activeSuspensionTargetPosition =
                suspensionTargetPosition;

            for (int i = 0; i < 4; i++)
            {
                if (externalVisualSync)
                {
                    GameObject proxy =
                        new(
                            $"PrometeoWheelProxy_{i}");

                    proxy.transform.SetParent(
                        transform,
                        false);

                    proxy.transform.localPosition =
                        wheelCentersLocal[i];

                    proxy.transform.localRotation =
                        Quaternion.identity;

                    proxy.transform.localScale =
                        Vector3.one;

                    wheelMeshes[i] =
                        proxy;
                }
                else
                {
                    wheelMeshes[i] =
                        visualWheelRoots[i] != null
                            ? visualWheelRoots[i].gameObject
                            : null;
                }

                ConfigureWheelCollider(
                    i,
                    wheelCentersLocal[i],
                    radius);
            }

            wheelRigReady = true;
            ApplyWheelFriction();
            TryBindPrometeo();
        }

        private void ConfigureWheelCollider(
            int index,
            Vector3 localCenter,
            float radius)
        {
            WheelCollider wheel =
                wheelColliders[index];

            if (wheel == null)
            {
                GameObject wheelObject =
                    new($"PrometeoWheelCollider_{index}");

                wheelObject.transform.SetParent(
                    transform,
                    false);

                wheel =
                    wheelObject.AddComponent<WheelCollider>();

                wheelColliders[index] = wheel;
            }

            // Unity's WheelCollider suspension extends along local Y and
            // its wheel circle is effectively shown half a suspension travel
            // below the collider transform. Raise the collider object by half
            // the configured suspension distance so the physical wheel center
            // matches the visual wheel center detected from the mesh bounds.
            wheel.transform.localPosition =
                localCenter +
                Vector3.up *
                (activeSuspensionDistance * 0.5f);
            wheel.transform.localRotation =
                Quaternion.identity;
            wheel.transform.localScale =
                Vector3.one;

            wheel.center = Vector3.zero;
            wheel.radius = radius;
            wheel.mass = wheelMass;
            wheel.suspensionDistance =
                activeSuspensionDistance;
            wheel.forceAppPointDistance =
                forceAppPointDistance;
            wheel.wheelDampingRate =
                wheelDampingRate;

            JointSpring spring =
                wheel.suspensionSpring;

            spring.spring =
                activeSuspensionSpring;
            spring.damper =
                activeSuspensionDamper;
            spring.targetPosition =
                activeSuspensionTargetPosition;

            wheel.suspensionSpring = spring;

            WheelFrictionCurve forward =
                wheel.forwardFriction;

            forward.extremumSlip = 0.36f;
            forward.extremumValue = 1f;
            forward.asymptoteSlip = 0.78f;
            forward.asymptoteValue = 0.72f;
            forward.stiffness =
                index >= RearLeft
                    ? 1.22f
                    : 1.18f;

            wheel.forwardFriction = forward;

            WheelFrictionCurve sideways =
                wheel.sidewaysFriction;

            sideways.extremumSlip = 0.26f;
            sideways.extremumValue = 1f;
            sideways.asymptoteSlip = 0.55f;
            sideways.asymptoteValue = 0.76f;
            sideways.stiffness =
                index >= RearLeft
                    ? 0.98f
                    : 1.18f;

            wheel.sidewaysFriction = sideways;
        }

        private bool TryBindPrometeo()
        {
            if (!wheelRigReady)
                return false;

            if (prometeo == null)
            {
                prometeoType =
                    FindPrometeoType();

                if (prometeoType == null)
                {
                    if (!warnedMissingPrometeo)
                    {
                        warnedMissingPrometeo = true;
                        Debug.LogWarning(
                            "Motor City: Prometeo Car Controller is not installed yet. " +
                            "Import the Asset Store package 'Prometeo Car Controller'. " +
                            "Motor City will bind it automatically after Unity recompiles.");
                    }

                    return false;
                }

                prometeo =
                    GetComponent(prometeoType);

                if (prometeo == null)
                    prometeo =
                        gameObject.AddComponent(
                            prometeoType);

                prometeoTractionLockedField =
                    null;
                prometeoDriftingField =
                    null;
            }

            bool complete =
                wheelMeshes[FrontLeft] != null &&
                wheelMeshes[FrontRight] != null &&
                wheelMeshes[RearLeft] != null &&
                wheelMeshes[RearRight] != null &&
                wheelColliders[FrontLeft] != null &&
                wheelColliders[FrontRight] != null &&
                wheelColliders[RearLeft] != null &&
                wheelColliders[RearRight] != null;

            if (!complete)
                return false;

            bool inputProxyReady =
                EnsurePrometeoInputProxies();

            SetPrometeoField(
                "useTouchControls",
                inputProxyReady);

            SetPrometeoField(
                "frontLeftMesh",
                wheelMeshes[FrontLeft]);
            SetPrometeoField(
                "frontRightMesh",
                wheelMeshes[FrontRight]);
            SetPrometeoField(
                "rearLeftMesh",
                wheelMeshes[RearLeft]);
            SetPrometeoField(
                "rearRightMesh",
                wheelMeshes[RearRight]);

            SetPrometeoField(
                "frontLeftCollider",
                wheelColliders[FrontLeft]);
            SetPrometeoField(
                "frontRightCollider",
                wheelColliders[FrontRight]);
            SetPrometeoField(
                "rearLeftCollider",
                wheelColliders[RearLeft]);
            SetPrometeoField(
                "rearRightCollider",
                wheelColliders[RearRight]);

            SetPrometeoField(
                "useEffects",
                false);
            SetPrometeoField(
                "useUI",
                false);
            SetPrometeoField(
                "useSounds",
                false);
            if (!inputProxyReady)
            {
                Debug.LogWarning(
                    "Motor City: PrometeoTouchInput was not found, so Prometeo is falling back to its built-in input path.");
            }

            ApplyDriveModeTuning();

            driveModeFrictionRefreshTimer =
                0f;

            warnedMissingPrometeo = false;

            SetPrometeoEnabled(
                drivingEnabled &&
                resetHoldTimer <= 0f);

            return true;
        }

        private bool EnsurePrometeoInputProxies()
        {
            if (throttleInputProxy != null &&
                reverseInputProxy != null &&
                leftInputProxy != null &&
                rightInputProxy != null &&
                handbrakeInputProxy != null)
                return true;

            prometeoTouchInputType =
                FindTypeByName(
                    "PrometeoTouchInput");

            if (prometeoTouchInputType == null ||
                !typeof(Component).IsAssignableFrom(
                    prometeoTouchInputType))
                return false;

            inputProxyPressedField =
                prometeoTouchInputType.GetField(
                    "buttonPressed",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (inputProxyPressedField == null)
            {
                inputProxyPressedProperty =
                    prometeoTouchInputType.GetProperty(
                        "buttonPressed",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);
            }

            throttleInputProxy =
                CreateInputProxy(
                    "Prometeo Input - Throttle");

            reverseInputProxy =
                CreateInputProxy(
                    "Prometeo Input - Reverse");

            leftInputProxy =
                CreateInputProxy(
                    "Prometeo Input - Left");

            rightInputProxy =
                CreateInputProxy(
                    "Prometeo Input - Right");

            handbrakeInputProxy =
                CreateInputProxy(
                    "Prometeo Input - Handbrake");

            bool ready =
                throttleInputProxy != null &&
                reverseInputProxy != null &&
                leftInputProxy != null &&
                rightInputProxy != null &&
                handbrakeInputProxy != null;

            if (!ready)
                return false;

            lastThrottleProxyPressed =
                null;
            lastReverseProxyPressed =
                null;
            lastLeftProxyPressed =
                null;
            lastRightProxyPressed =
                null;
            lastHandbrakeProxyPressed =
                null;

            SetPrometeoField(
                "throttleButton",
                throttleInputProxy.gameObject);

            SetPrometeoField(
                "reverseButton",
                reverseInputProxy.gameObject);

            SetPrometeoField(
                "turnLeftButton",
                leftInputProxy.gameObject);

            SetPrometeoField(
                "turnRightButton",
                rightInputProxy.gameObject);

            SetPrometeoField(
                "handbrakeButton",
                handbrakeInputProxy.gameObject);

            return true;
        }

        private Component CreateInputProxy(
            string name)
        {
            // PrometeoTouchInput expects a RectTransform in Start(), even
            // when we drive buttonPressed from code instead of real UI.
            GameObject inputObject =
                new(
                    name,
                    typeof(RectTransform));

            RectTransform rect =
                inputObject.GetComponent<RectTransform>();

            rect.SetParent(
                transform,
                false);

            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.sizeDelta = Vector2.one;

            inputObject.hideFlags =
                HideFlags.HideInHierarchy;

            return inputObject.AddComponent(
                prometeoTouchInputType);
        }

        private void UpdatePrometeoInputProxies()
        {
            if (prometeo == null ||
                throttleInputProxy == null)
                return;

            bool throttle = false;
            bool reverse = false;
            bool left = false;
            bool right = false;
            bool handbrake = false;

            if (drivingEnabled &&
                resetHoldTimer <= 0f)
            {
                throttle =
                    MotorCityInput.ThrottleHeld;

                reverse =
                    MotorCityInput.ReverseHeld;

                left =
                    MotorCityInput.SteerLeftHeld;

                right =
                    MotorCityInput.SteerRightHeld;

                handbrake =
                    MotorCityInput.HandbrakeHeld;
            }

            throttleHeld = throttle;
            reverseHeld = reverse;
            handbrakeHeld = handbrake;
            steeringInputHeld =
                left ||
                right;

            SetInputProxyPressedIfChanged(
                throttleInputProxy,
                throttle,
                ref lastThrottleProxyPressed);

            SetInputProxyPressedIfChanged(
                reverseInputProxy,
                reverse,
                ref lastReverseProxyPressed);

            SetInputProxyPressedIfChanged(
                leftInputProxy,
                left,
                ref lastLeftProxyPressed);

            SetInputProxyPressedIfChanged(
                rightInputProxy,
                right,
                ref lastRightProxyPressed);

            SetInputProxyPressedIfChanged(
                handbrakeInputProxy,
                handbrake,
                ref lastHandbrakeProxyPressed);
        }

        private void SetInputProxyPressedIfChanged(
            Component proxy,
            bool pressed,
            ref bool? lastPressed)
        {
            if (lastPressed.HasValue &&
                lastPressed.Value == pressed)
            {
                return;
            }

            lastPressed =
                pressed;

            SetInputProxyPressed(
                proxy,
                pressed);
        }

        private void SetInputProxyPressed(
            Component proxy,
            bool pressed)
        {
            if (proxy == null)
                return;

            if (inputProxyPressedField != null &&
                inputProxyPressedField.FieldType == typeof(bool))
            {
                inputProxyPressedField.SetValue(
                    proxy,
                    pressed);
                return;
            }

            if (inputProxyPressedProperty != null &&
                inputProxyPressedProperty.CanWrite &&
                inputProxyPressedProperty.PropertyType == typeof(bool))
            {
                inputProxyPressedProperty.SetValue(
                    proxy,
                    pressed);
            }
        }

        private void ApplyPrometeoTuning()
        {
            int tunedMaxSpeed =
                GetTunedMaxSpeedKph();

            int tunedAcceleration =
                GetTunedAccelerationMultiplier();

            int tunedDriftMultiplier =
                GetTunedHandbrakeDriftMultiplier();

            int baseSteeringAngle =
                currentDriveMode switch
                {
                    DriveMode.Sport =>
                        36,
                    DriveMode.Drift =>
                        62,
                    _ =>
                        42
                };

            int tunedSteeringAngle =
                Mathf.RoundToInt(
                    baseSteeringAngle *
                    vehicleSteeringMultiplier);

            // Prometeo's physical steering model is authored for a maximum
            // of about 45 degrees. Vehicle steering multipliers can otherwise
            // push Comfort and Drift beyond that limit (AE86 Comfort reached
            // about 48 degrees and Drift much higher), which makes the front
            // WheelColliders scrub sideways and creates the "turns briefly,
            // then pushes straight" behavior.
            tunedSteeringAngle =
                Mathf.Clamp(
                    tunedSteeringAngle,
                    10,
                    45);

            float tunedSteeringSpeed =
                (currentDriveMode switch
                {
                    DriveMode.Sport =>
                        steeringSpeed * 1.12f,
                    DriveMode.Drift =>
                        steeringSpeed * 1.28f,
                    _ =>
                        steeringSpeed * 0.92f
                }) *
                vehicleSteeringMultiplier;

            int baseBrakeForce =
                currentDriveMode switch
                {
                    DriveMode.Sport =>
                        brakeForce + 500,
                    DriveMode.Drift =>
                        Mathf.Max(
                            1100,
                            brakeForce + 100),
                    _ =>
                        brakeForce + 300
                };

            int tunedBrakeForce =
                Mathf.Max(
                    1500,
                    Mathf.RoundToInt(
                        baseBrakeForce *
                        vehicleBrakeMultiplier));

            Vector3 tunedCenterOfMass =
                bodyMassCenter +
                Vector3.down *
                GetStabilityCenterDrop();

            tunedCenterOfMass +=
                currentDriveMode switch
                {
                    DriveMode.Sport =>
                        Vector3.down * 0.055f,
                    DriveMode.Comfort =>
                        Vector3.down * 0.025f,
                    _ =>
                        Vector3.up * 0.005f
                };

            if (prometeo != null)
            {
                SetPrometeoField(
                    "maxSpeed",
                    tunedMaxSpeed);
                SetPrometeoField(
                    "maxReverseSpeed",
                    maxReverseSpeedKph);
                SetPrometeoField(
                    "accelerationMultiplier",
                    tunedAcceleration);
                SetPrometeoField(
                    "frontDriveTorqueMultiplier",
                    frontDriveTorqueMultiplier);
                SetPrometeoField(
                    "rearDriveTorqueMultiplier",
                    rearDriveTorqueMultiplier);
                SetPrometeoField(
                    "maxSteeringAngle",
                    tunedSteeringAngle);
                SetPrometeoField(
                    "steeringSpeed",
                    tunedSteeringSpeed);
                SetPrometeoField(
                    "brakeForce",
                    tunedBrakeForce);
                SetPrometeoField(
                    "decelerationMultiplier",
                    currentDriveMode ==
                    DriveMode.Sport
                        ? Mathf.Max(
                            1,
                            decelerationMultiplier + 1)
                        : decelerationMultiplier);
                SetPrometeoField(
                    "handbrakeDriftMultiplier",
                    tunedDriftMultiplier);
                SetPrometeoField(
                    "bodyMassCenter",
                    tunedCenterOfMass);
            }

            if (body != null)
            {
                body.mass =
                    vehicleMass *
                    vehicleMassMultiplier;

                body.centerOfMass =
                    tunedCenterOfMass;

                float modeDamping =
                    currentDriveMode switch
                    {
                        DriveMode.Sport => 0.16f,
                        DriveMode.Comfort => 0.10f,
                        _ => -0.035f
                    };

                float upgradeStability =
                    GetStabilityDampingBonus() *
                    (currentDriveMode == DriveMode.Drift
                        ? 0.15f
                        : 1f);

                body.angularDamping =
                    Mathf.Max(
                        0.05f,
                        angularDamping +
                        vehicleStabilityBonus +
                        upgradeStability +
                        modeDamping);
            }
        }

        private int GetTunedMaxSpeedKph()
        {
            int modeBonus =
                currentDriveMode switch
                {
                    DriveMode.Sport => 8,
                    DriveMode.Drift => -8,
                    _ => 0
                };

            return Mathf.Clamp(
                vehicleBaseMaxSpeedKph +
                GetEngineSpeedBonus() +
                GetMasterySpeedBonus() +
                modeBonus,
                90,
                220);
        }

        private int GetTunedAccelerationMultiplier()
        {
            int modeBonus =
                currentDriveMode switch
                {
                    DriveMode.Sport => 1,
                    DriveMode.Drift => 0,
                    _ => 0
                };

            return Mathf.Clamp(
                vehicleBaseAccelerationTune +
                GetEngineAccelerationBonus() +
                GetMasteryAccelerationBonus() +
                modeBonus,
                4,
                13);
        }

        private int GetMasterySpeedBonus()
        {
            return
                Mathf.Max(
                    0,
                    vehicleMasteryLevel - 1) /
                2;
        }

        private int GetMasteryAccelerationBonus()
        {
            return
                vehicleMasteryLevel >= 6
                    ? 1
                    : 0;
        }

        private float GetMasteryGripBonus()
        {
            return
                Mathf.Max(
                    0,
                    vehicleMasteryLevel - 1) *
                0.006f;
        }

        private float GetMasteryPowerBonus()
        {
            return
                Mathf.Max(
                    0,
                    vehicleMasteryLevel - 1) *
                0.005f;
        }

        private int GetEngineSpeedBonus()
        {
            return
                engineUpgradeLevel switch
                {
                    1 => 3,
                    2 => 6,
                    3 => 9,
                    4 => 12,
                    5 => 15,
                    _ => 0
                };
        }

        private int GetEngineAccelerationBonus()
        {
            return
                engineUpgradeLevel switch
                {
                    1 => 0,
                    2 => 1,
                    3 => 1,
                    4 => 2,
                    5 => 2,
                    _ => 0
                };
        }

        private float GetEngineAssistBonus()
        {
            return
                engineUpgradeLevel switch
                {
                    1 => 0.05f,
                    2 => 0.10f,
                    3 => 0.16f,
                    4 => 0.22f,
                    5 => 0.30f,
                    _ => 0f
                };
        }

        private float GetGripUpgradeBonus()
        {
            return
                gripUpgradeLevel switch
                {
                    1 => 0.05f,
                    2 => 0.11f,
                    3 => 0.18f,
                    4 => 0.26f,
                    5 => 0.35f,
                    _ => 0f
                };
        }

        private float GetStabilityCenterDrop()
        {
            return
                stabilityUpgradeLevel switch
                {
                    1 => 0.018f,
                    2 => 0.038f,
                    3 => 0.062f,
                    4 => 0.090f,
                    5 => 0.122f,
                    _ => 0f
                };
        }

        private float GetStabilityDampingBonus()
        {
            return
                stabilityUpgradeLevel switch
                {
                    1 => 0.12f,
                    2 => 0.26f,
                    3 => 0.43f,
                    4 => 0.63f,
                    5 => 0.86f,
                    _ => 0f
                };
        }

        private int GetTunedHandbrakeDriftMultiplier()
        {
            int baseValue =
                handbrakeDriftMultiplier -
                gripUpgradeLevel / 2;

            int modeOffset =
                currentDriveMode switch
                {
                    DriveMode.Sport => -3,
                    DriveMode.Drift => 3,
                    _ => -1
                };

            return Mathf.Clamp(
                Mathf.RoundToInt(
                    (baseValue +
                     modeOffset) *
                    vehicleDriftMultiplier),
                2,
                12);
        }

        private void ApplyPowerAssist()
        {
            if (body == null ||
                prometeo == null ||
                !drivingEnabled ||
                GroundedWheels < 2)
                return;

            float forwardSpeed =
                ForwardSpeedKph;

            float maxSpeed =
                GetTunedMaxSpeedKph();

            if (throttleHeld &&
                forwardSpeed < maxSpeed)
            {
                float fadeStart =
                    currentDriveMode ==
                    DriveMode.Sport
                        ? Mathf.Max(
                            120f,
                            powerAssistFadeStartKph + 22f)
                        : currentDriveMode ==
                          DriveMode.Comfort
                            ? powerAssistFadeStartKph - 20f
                            : powerAssistFadeStartKph - 8f;

                float fade =
                    1f -
                    Mathf.InverseLerp(
                        fadeStart,
                        maxSpeed,
                        Mathf.Max(
                            0f,
                            forwardSpeed));

                float modeAcceleration =
                    currentDriveMode switch
                    {
                        DriveMode.Sport => 1.72f,
                        DriveMode.Drift => 1.22f,
                        _ => 0.82f
                    };

                float acceleration =
                    basePowerAssistAcceleration *
                    modeAcceleration *
                    vehiclePowerMultiplier *
                    turboAssistMultiplier *
                    (1f +
                     GetEngineAssistBonus() +
                     GetMasteryPowerBonus());

                if (currentDriveMode ==
                        DriveMode.Drift &&
                    (IsSliding ||
                     handbrakeHeld))
                {
                    acceleration +=
                        driftPowerAssistAcceleration *
                        1.45f;
                }
                else if (currentDriveMode ==
                             DriveMode.Sport &&
                         SpeedKph < 70f)
                {
                    // Direct forward acceleration helps Sport launch hard
                    // without asking the driven rear tires to create all
                    // of the launch torque and spin themselves.
                    acceleration += 2.8f;
                }

                body.AddForce(
                    transform.forward *
                    acceleration *
                    Mathf.Clamp01(fade),
                    ForceMode.Acceleration);
            }

            if (reverseHeld &&
                forwardSpeed >
                -maxReverseSpeedKph)
            {
                body.AddForce(
                    -transform.forward *
                    reversePowerAssistAcceleration,
                    ForceMode.Acceleration);
            }
        }

        private void UpdateTelemetry()
        {
            GroundedWheels = 0;

            float rearSideways = 0f;
            float rearForward = 0f;
            int rearGrounded = 0;

            for (int i = 0; i < 4; i++)
            {
                WheelCollider wheel =
                    wheelColliders[i];

                if (wheel == null ||
                    !wheel.GetGroundHit(
                        out WheelHit hit))
                    continue;

                GroundedWheels++;

                if (i < RearLeft)
                    continue;

                rearSideways +=
                    Mathf.Abs(hit.sidewaysSlip);

                rearForward +=
                    Mathf.Abs(hit.forwardSlip);

                rearGrounded++;
            }

            RearSidewaysSlip =
                rearGrounded > 0
                    ? rearSideways / rearGrounded
                    : 0f;

            RearForwardSlip =
                rearGrounded > 0
                    ? rearForward / rearGrounded
                    : 0f;

            float angle =
                Mathf.Abs(SlipAngleDegrees);

            float slipIntensity =
                Mathf.InverseLerp(
                    driftDetectionSideSlip,
                    0.58f,
                    RearSidewaysSlip);

            float angleIntensity =
                Mathf.InverseLerp(
                    driftDetectionAngle,
                    36f,
                    angle);

            float speedKph =
                SpeedKph;

            bool prometeoDrifting =
                ReadPrometeoBool(
                    "isDrifting");

            float speedIntensity =
                Mathf.InverseLerp(
                    minimumDriftSpeedKph,
                    72f,
                    speedKph);

            DriftIntensity =
                Mathf.Clamp01(
                    Mathf.Max(
                        slipIntensity *
                        angleIntensity,
                        prometeoDrifting
                            ? 0.65f
                            : 0f) *
                    Mathf.Lerp(
                        0.72f,
                        1f,
                        speedIntensity));

            bool physicalSlide =
                GroundedWheels >= 3 &&
                speedKph >= minimumDriftSpeedKph &&
                RearSidewaysSlip >=
                    driftDetectionSideSlip &&
                angle >=
                    driftDetectionAngle;

            IsSliding =
                physicalSlide ||
                (prometeoDrifting &&
                 speedKph >= minimumDriftSpeedKph);
        }

        public void ApplyStraightLineStability()
        {
            // Drift mode must leave yaw and lateral velocity to the player.
            // Running the straight-line assist here fights drift initiation:
            // before IsSliding becomes true it cancels the developing slip and
            // applies counter-yaw torque that straightens the car.
            if (currentDriveMode == DriveMode.Drift)
                return;

            // Read steering directly in the physics tick. The cached
            // steeringInputHeld value is populated in Update(), so FixedUpdate()
            // could briefly see the previous frame's value exactly when the
            // player starts turning and apply a straightening impulse.
            if (body == null ||
                !drivingEnabled ||
                resetHoldTimer > 0f ||
                Mathf.Abs(MotorCityInput.SteeringAxis) > 0.02f ||
                handbrakeHeld ||
                GroundedWheels < 3 ||
                SpeedKph < 10f)
                return;

            float slipAngle =
                Mathf.Abs(
                    SlipAngleDegrees);

            float maximumAssistSlip =
                currentDriveMode ==
                    DriveMode.Drift
                    ? 4.5f
                    : 9f;

            if (IsSliding ||
                slipAngle >
                maximumAssistSlip)
                return;

            float steerReturnRate =
                currentDriveMode switch
                {
                    DriveMode.Sport => 220f,
                    DriveMode.Drift => 115f,
                    _ => 170f
                };

            for (int i = FrontLeft;
                 i <= FrontRight;
                 i++)
            {
                WheelCollider wheel =
                    wheelColliders[i];

                if (wheel == null)
                    continue;

                wheel.steerAngle =
                    Mathf.MoveTowards(
                        wheel.steerAngle,
                        0f,
                        steerReturnRate *
                        Time.fixedDeltaTime);
            }

            Vector3 localVelocity =
                transform.InverseTransformDirection(
                    body.linearVelocity);

            float lateralGain =
                currentDriveMode switch
                {
                    DriveMode.Sport => 2.9f,
                    DriveMode.Drift => 0.65f,
                    _ => 1.8f
                };

            float speedFactor =
                Mathf.InverseLerp(
                    12f,
                    140f,
                    SpeedKph);

            float lateralAcceleration =
                -localVelocity.x *
                lateralGain *
                Mathf.Lerp(
                    0.45f,
                    1f,
                    speedFactor);

            body.AddForce(
                transform.right *
                lateralAcceleration,
                ForceMode.Acceleration);

            float yawRate =
                Vector3.Dot(
                    body.angularVelocity,
                    transform.up);

            float yawGain =
                currentDriveMode switch
                {
                    DriveMode.Sport => 2.4f,
                    DriveMode.Drift => 0.45f,
                    _ => 1.35f
                };

            body.AddTorque(
                -transform.up *
                yawRate *
                yawGain,
                ForceMode.Acceleration);
        }

        public void ApplyPhysicalHandbrake(
            float rearBrakeTorque,
            float lowSpeedRearBrakeTorque,
            float lowSpeedThresholdKph,
            float holdThresholdKph)
        {
            if (!drivingEnabled ||
                resetHoldTimer > 0f)
                return;

            if (!handbrakeHeld)
            {
                // HandbrakePhysicsAssist adds its own large rear brake torque.
                // Prometeo does not clear that torque while the player is just
                // coasting, so without this release the rear wheels can remain
                // braked until throttle/reverse is pressed again.
                //
                // Do not clear while throttle/reverse is held: reverse input can
                // intentionally use Prometeo's normal four-wheel braking while
                // changing direction.
                if (!throttleHeld &&
                    !reverseHeld)
                {
                    for (int i = RearLeft;
                         i <= RearRight;
                         i++)
                    {
                        WheelCollider wheel =
                            wheelColliders[i];

                        if (wheel != null)
                        {
                            wheel.brakeTorque =
                                0f;
                        }
                    }
                }

                return;
            }

            float torque =
                SpeedKph <= lowSpeedThresholdKph
                    ? lowSpeedRearBrakeTorque
                    : rearBrakeTorque;

            if (currentDriveMode == DriveMode.Drift)
            {
                // Drift already runs with reduced rear lateral grip. Applying
                // the full 5200/9500 Nm rear lock on top of that causes snap
                // spins instead of a controllable initiation.
                torque *= 0.62f;
            }

            for (int i = RearLeft; i <= RearRight; i++)
            {
                WheelCollider wheel =
                    wheelColliders[i];

                if (wheel == null)
                    continue;

                wheel.motorTorque = 0f;
                wheel.brakeTorque =
                    Mathf.Max(
                        wheel.brakeTorque,
                        torque);
            }

            if (body != null &&
                SpeedKph <= holdThresholdKph &&
                !throttleHeld &&
                !reverseHeld)
            {
                body.linearVelocity =
                    Vector3.zero;
                body.angularVelocity =
                    Vector3.zero;
            }
        }

        public bool TryGetRearGroundHit(
            int rearWheel,
            out WheelHit hit)
        {
            int index =
                rearWheel <= 0
                    ? RearLeft
                    : RearRight;

            WheelCollider wheel =
                wheelColliders[index];

            if (wheel != null &&
                wheel.GetGroundHit(out hit))
                return true;

            hit = default;
            return false;
        }

        public void ApplyVehicleProfile(
            int topSpeedKph,
            int accelerationTune,
            float gripMultiplier,
            float stabilityBonus,
            float massMultiplier,
            float steeringMultiplier,
            float brakeMultiplier,
            float powerMultiplier,
            float driftMultiplier)
        {
            vehicleBaseMaxSpeedKph =
                Mathf.Clamp(
                    topSpeedKph,
                    90,
                    205);

            vehicleBaseAccelerationTune =
                Mathf.Clamp(
                    accelerationTune,
                    4,
                    10);

            vehicleGripMultiplier =
                Mathf.Clamp(
                    gripMultiplier,
                    0.75f,
                    1.30f);

            vehicleStabilityBonus =
                Mathf.Clamp(
                    stabilityBonus,
                    -0.08f,
                    0.12f);

            vehicleMassMultiplier =
                Mathf.Clamp(
                    massMultiplier,
                    0.82f,
                    1.30f);

            vehicleSteeringMultiplier =
                Mathf.Clamp(
                    steeringMultiplier,
                    0.80f,
                    1.16f);

            vehicleBrakeMultiplier =
                Mathf.Clamp(
                    brakeMultiplier,
                    0.90f,
                    1.35f);

            vehiclePowerMultiplier =
                Mathf.Clamp(
                    powerMultiplier,
                    0.88f,
                    1.20f);

            vehicleDriftMultiplier =
                Mathf.Clamp(
                    driftMultiplier,
                    0.68f,
                    1.24f);

            ApplyDriveModeTuning();
        }

        public void SetDriveTorqueDistribution(
            float frontMultiplier,
            float rearMultiplier)
        {
            frontDriveTorqueMultiplier =
                Mathf.Clamp(
                    frontMultiplier,
                    0f,
                    2f);

            rearDriveTorqueMultiplier =
                Mathf.Clamp(
                    rearMultiplier,
                    0f,
                    2f);

            ApplyPrometeoTuning();
        }

        public void ApplyMasteryLevel(
            int level)
        {
            vehicleMasteryLevel =
                Mathf.Clamp(
                    level,
                    1,
                    10);

            ApplyDriveModeTuning();
        }

        public void ApplyUpgradeLevels(
            int engineLevel,
            int gripLevel,
            int stabilityLevel)
        {
            engineUpgradeLevel =
                Mathf.Clamp(
                    engineLevel,
                    0,
                    5);

            gripUpgradeLevel =
                Mathf.Clamp(
                    gripLevel,
                    0,
                    5);

            stabilityUpgradeLevel =
                Mathf.Clamp(
                    stabilityLevel,
                    0,
                    5);

            ApplyDriveModeTuning();
        }

        private void ApplyDriveModeTuning()
        {
            ApplyPrometeoTuning();
            ApplyWheelFriction();
        }

        private void ApplyWheelFriction()
        {
            float progressionGripBonus =
                GetGripUpgradeBonus() +
                GetMasteryGripBonus();

            float vehicleGrip =
                Mathf.Clamp(
                    vehicleGripMultiplier,
                    0.75f,
                    1.30f);

            // Grip progression should improve normal/sport handling, but in
            // Drift it must not erase the rear slip window. Keep only a small
            // portion of upgrade/mastery grip and soften profile grip there.
            float upgradeGrip =
                currentDriveMode == DriveMode.Drift
                    ? (1f +
                       progressionGripBonus * 0.20f) *
                      Mathf.Lerp(
                          1f,
                          vehicleGrip,
                          0.35f)
                    : (1f +
                       progressionGripBonus) *
                      vehicleGrip;

            for (int i = 0;
                 i <
                 wheelColliders.Length;
                 i++)
            {
                WheelCollider wheel =
                    wheelColliders[i];

                if (wheel == null)
                    continue;

                bool rear =
                    i >= RearLeft;

                WheelFrictionCurve forward =
                    wheel.forwardFriction;

                WheelFrictionCurve sideways =
                    wheel.sidewaysFriction;

                switch (currentDriveMode)
                {
                    case DriveMode.Sport:
                        forward.extremumSlip =
                            rear ? 0.20f : 0.24f;
                        forward.asymptoteSlip =
                            rear ? 0.48f : 0.55f;
                        forward.extremumValue = 1f;
                        forward.asymptoteValue =
                            0.82f;
                        forward.stiffness =
                            (rear ? 1.78f : 1.55f) *
                            upgradeGrip;

                        sideways.extremumSlip =
                            rear ? 0.20f : 0.23f;
                        sideways.asymptoteSlip =
                            rear ? 0.43f : 0.50f;
                        sideways.extremumValue = 1f;
                        sideways.asymptoteValue =
                            0.82f;
                        sideways.stiffness =
                            (rear ? 1.58f : 1.48f) *
                            upgradeGrip;
                        break;

                    case DriveMode.Drift:
                        forward.extremumSlip =
                            rear ? 0.42f : 0.34f;
                        forward.asymptoteSlip =
                            rear ? 0.92f : 0.74f;
                        forward.extremumValue = 1f;
                        forward.asymptoteValue =
                            rear ? 0.68f : 0.74f;
                        forward.stiffness =
                            (rear ? 1.04f : 1.18f) *
                            upgradeGrip;

                        sideways.extremumSlip =
                            rear ? 0.34f : 0.27f;
                        sideways.asymptoteSlip =
                            rear ? 0.68f : 0.58f;
                        sideways.extremumValue = 1f;
                        sideways.asymptoteValue =
                            rear ? 0.68f : 0.76f;
                        sideways.stiffness =
                            (rear ? 0.86f : 1.16f) *
                            upgradeGrip;
                        break;

                    default:
                        forward.extremumSlip = 0.31f;
                        forward.asymptoteSlip = 0.67f;
                        forward.extremumValue = 1f;
                        forward.asymptoteValue =
                            0.76f;
                        forward.stiffness =
                            (rear ? 1.38f : 1.30f) *
                            upgradeGrip;

                        sideways.extremumSlip = 0.24f;
                        sideways.asymptoteSlip = 0.52f;
                        sideways.extremumValue = 1f;
                        sideways.asymptoteValue =
                            0.78f;
                        sideways.stiffness =
                            (rear ? 1.20f : 1.30f) *
                            upgradeGrip;
                        break;
                }

                if (rear &&
                    handbrakeHeld)
                {
                    // Handbrake grip loss belongs here, alongside drive mode,
                    // upgrades, mastery and the selected vehicle profile.
                    // This prevents Prometeo from restoring stale startup
                    // friction after a mode/car change.
                    float handbrakeGripFactor =
                        currentDriveMode switch
                        {
                            DriveMode.Drift => 0.42f,
                            DriveMode.Sport => 0.58f,
                            _ => 0.50f
                        };

                    sideways.extremumSlip *=
                        currentDriveMode == DriveMode.Drift
                            ? 1.55f
                            : 1.35f;

                    sideways.asymptoteSlip *=
                        currentDriveMode == DriveMode.Drift
                            ? 1.45f
                            : 1.25f;

                    sideways.stiffness *=
                        handbrakeGripFactor;
                }

                wheel.forwardFriction =
                    forward;

                wheel.sidewaysFriction =
                    sideways;
            }
        }

        private void ShowControlHint(
            string message)
        {
            controlHintMessage =
                message;

            controlHintMessageTimer =
                3.6f;
        }

        public void CycleDriveMode()
        {
            int nextMode =
                ((int)currentDriveMode + 1) %
                3;

            currentDriveMode =
                (DriveMode)nextMode;

            ApplyDriveModeTuning();

            driveModeFrictionRefreshTimer =
                0f;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DriveModeKey,
                nextMode);

            MotorCity.Persistence.MotorCitySaveService.Save();

            driveModeMessageTimer =
                2.25f;
        }

        public void UseAutomaticMassProperties()
        {
            if (body == null)
                return;

            body.mass =
                vehicleMass *
                vehicleMassMultiplier;

            body.ResetInertiaTensor();

            ApplyDriveModeTuning();
        }

        public void BeginOpeningPresentationLock(
            float seconds)
        {
            presentationLockTimer =
                Mathf.Max(
                    0.1f,
                    seconds);

            SetPresentationLock(
                true);
        }

        public void SetPresentationLock(
            bool locked)
        {
            if (presentationLock ==
                locked)
            {
                return;
            }

            presentationLock =
                locked;

            if (!locked)
            {
                presentationLockTimer =
                    0f;

                ApplyDrivingEnabled(
                    drivingBlockers.Count == 0);

                return;
            }

            // Opening presentation must block player input without freezing
            // Rigidbody gravity. The car intentionally spawns slightly above
            // the road and must be allowed to settle naturally.
            ApplyDrivingEnabled(
                false);

            for (int i = 0;
                 i < wheelColliders.Length;
                 i++)
            {
                WheelCollider wheel =
                    wheelColliders[i];

                if (wheel == null)
                    continue;

                wheel.motorTorque =
                    0f;

                wheel.steerAngle =
                    0f;

                wheel.brakeTorque =
                    brakeForce * 8f;
            }
        }

        public bool IsGaragePresentationMode =>
            garagePresentationMode;

        public void SetGaragePresentationMode(
            bool enabled)
        {
            garagePresentationMode =
                enabled;
            if (!enabled)
            {
                resetHoldTimer =
                    0f;

                ReleaseResetBrakes();
                SetDrivingBlocked(
                    "Garage",
                    false);

                return;
            }

            // Garage mode blocks every player driving input while leaving
            // Rigidbody gravity, WheelCollider suspension and contacts alive.
            SetDrivingBlocked(
                "Garage",
                true);

            resetHoldTimer =
                0f;

            throttleHeld =
                false;

            reverseHeld =
                false;

            handbrakeHeld =
                false;

            steeringInputHeld =
                false;

            for (int i = 0;
                 i < wheelColliders.Length;
                 i++)
            {
                WheelCollider wheel =
                    wheelColliders[i];

                if (wheel == null)
                    continue;

                wheel.motorTorque =
                    0f;

                wheel.steerAngle =
                    0f;

                wheel.brakeTorque =
                    0f;
            }

            if (body != null)
            {
                body.linearVelocity =
                    Vector3.zero;

                body.angularVelocity =
                    Vector3.zero;

                body.WakeUp();
            }

            UpdatePrometeoInputProxies();
        }

        public void SetDrivingBlocked(
            string reason,
            bool blocked)
        {
            if (string.IsNullOrWhiteSpace(
                    reason))
            {
                reason = "Legacy";
            }

            bool changed =
                blocked
                    ? drivingBlockers.Add(
                        reason)
                    : drivingBlockers.Remove(
                        reason);

            if (!changed)
                return;

            ApplyDrivingEnabled(
                drivingBlockers.Count == 0 &&
                !presentationLock);
        }

        private void ApplyDrivingEnabled(
            bool enabled)
        {
            if (drivingEnabled ==
                enabled)
            {
                return;
            }

            drivingEnabled = enabled;

            if (!enabled)
            {
                ClearMotion();
                SetPrometeoEnabled(false);
                return;
            }

            if (resetHoldTimer <= 0f)
                SetPrometeoEnabled(true);
        }

        public void TeleportTo(
            Vector3 position,
            Quaternion rotation)
        {
            // Teleporting needs a short input lock while transforms and
            // Rigidbody state are synchronized, but it must not leave the
            // compatibility "Legacy" blocker behind after the teleport.
            SetDrivingBlocked(
                "Teleport",
                true);

            transform.SetPositionAndRotation(
                position,
                rotation);

            if (body != null)
            {
                body.position = position;
                body.rotation = rotation;
            }

            ClearMotion();
            Physics.SyncTransforms();

            SetDrivingBlocked(
                "Teleport",
                false);
        }

        public void ClearMotion()
        {
            resetHoldTimer = 0.20f;
            SetPrometeoEnabled(false);

            for (int i = 0;
                 i < wheelColliders.Length;
                 i++)
            {
                WheelCollider wheel =
                    wheelColliders[i];

                if (wheel == null)
                    continue;

                wheel.motorTorque = 0f;
                wheel.steerAngle = 0f;
                wheel.brakeTorque =
                    brakeForce * 8f;
            }

            if (body != null)
            {
                body.linearVelocity =
                    Vector3.zero;
                body.angularVelocity =
                    Vector3.zero;

                // Teleports intentionally place the car about a metre above
                // the road so physics can settle it safely. Do not put the
                // Rigidbody to sleep here: when driving is disabled (garage,
                // countdowns, result screens) a sleeping body can remain
                // suspended at the teleport height indefinitely.
                body.WakeUp();
            }

            DriftEffects effects =
                GetComponent<DriftEffects>();

            effects?.ClearTrails();
        }

        private void HoldVehicleStill()
        {
            if (body != null)
            {
                body.linearVelocity =
                    Vector3.zero;
                body.angularVelocity =
                    Vector3.zero;
            }

            for (int i = 0;
                 i < wheelColliders.Length;
                 i++)
            {
                WheelCollider wheel =
                    wheelColliders[i];

                if (wheel == null)
                    continue;

                wheel.motorTorque = 0f;
                wheel.brakeTorque =
                    brakeForce * 8f;
            }
        }

        private void ReleaseResetBrakes()
        {
            for (int i = 0;
                 i < wheelColliders.Length;
                 i++)
            {
                WheelCollider wheel =
                    wheelColliders[i];

                if (wheel == null)
                    continue;

                wheel.brakeTorque = 0f;
            }

            if (body != null)
                body.WakeUp();
        }

        private void SetPrometeoEnabled(
            bool enabled)
        {
            // Component uses Unity's special null semantics: an already
            // destroyed Prometeo component can still have a managed C# wrapper.
            // Pattern matching alone treats that wrapper as a live Behaviour
            // and throws MissingReferenceException when enabled is accessed.
            if (prometeo == null)
            {
                prometeo =
                    null;
                return;
            }

            if (prometeo is not Behaviour behaviour)
                return;

            behaviour.enabled =
                enabled;

            if (!enabled)
                return;

            // Prometeo can restore its own cached steering/traction values
            // after being disabled or after a traction-recovery cycle.
            // Reassert the selected Motor City drive mode immediately.
            ApplyPrometeoTuning();
            ApplyWheelFriction();

            driveModeFrictionRefreshTimer =
                0f;
        }

        private void SetPrometeoField(
            string fieldName,
            object value)
        {
            if (prometeo == null)
                return;

            FieldInfo field =
                prometeo.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (field == null)
                return;

            try
            {
                field.SetValue(
                    prometeo,
                    value);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Motor City: could not set Prometeo field '{fieldName}': {exception.Message}");
            }
        }

        private bool ReadPrometeoBool(
            string fieldName)
        {
            if (prometeo == null)
                return false;

            FieldInfo field =
                fieldName switch
                {
                    "isTractionLocked" =>
                        prometeoTractionLockedField ??=
                            ResolvePrometeoBoolField(
                                "isTractionLocked"),
                    "isDrifting" =>
                        prometeoDriftingField ??=
                            ResolvePrometeoBoolField(
                                "isDrifting"),
                    _ =>
                        ResolvePrometeoBoolField(
                            fieldName)
                };

            if (field == null)
                return false;

            try
            {
                return (bool)field.GetValue(
                    prometeo);
            }
            catch
            {
                return false;
            }
        }

        private FieldInfo ResolvePrometeoBoolField(
            string fieldName)
        {
            if (prometeo == null)
                return null;

            FieldInfo field =
                prometeo.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            return
                field != null &&
                field.FieldType == typeof(bool)
                    ? field
                    : null;
        }

        private static Type FindPrometeoType()
        {
            return FindTypeByName(
                "PrometeoCarController");
        }

        private static Type FindTypeByName(
            string typeName)
        {
            // Prometeo is imported into the project's default Assembly-CSharp,
            // the same assembly as Motor City's runtime scripts. Searching only
            // this assembly avoids AppDomain.GetAssemblies(), which Unity 6.6
            // warns can expose already-unloaded assemblies.
            System.Reflection.Assembly assembly =
                typeof(ArcadeCarController).Assembly;

            Type direct =
                assembly.GetType(
                    typeName,
                    false);

            if (direct != null)
                return direct;

            Type[] types;

            try
            {
                types =
                    assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types =
                    exception.Types;
            }

            if (types == null)
                return null;

            foreach (Type type in types)
            {
                if (type != null &&
                    type.Name == typeName)
                    return type;
            }

            return null;
        }
    }
}
