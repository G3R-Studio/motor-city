using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private int baseMaxSpeedKph = 250;
        [SerializeField] private int maxReverseSpeedKph = 55;
        [SerializeField] private int accelerationMultiplier = 12;
        [SerializeField] private int maxSteeringAngle = 32;
        [SerializeField] private float steeringSpeed = 0.68f;
        [SerializeField] private int brakeForce = 900;
        [SerializeField] private int decelerationMultiplier = 1;
        [SerializeField] private int handbrakeDriftMultiplier = 7;
        [SerializeField] private Vector3 bodyMassCenter =
            new(0f, 0.32f, 0.05f);

        [Header("Vehicle body")]
        [SerializeField] private float vehicleMass = 1280f;
        [SerializeField] private float angularDamping = 0.22f;

        [Header("Power assist")]
        [SerializeField] private float basePowerAssistAcceleration = 4.2f;
        [SerializeField] private float driftPowerAssistAcceleration = 2.4f;
        [SerializeField] private float reversePowerAssistAcceleration = 2.2f;
        [SerializeField] private float powerAssistFadeStartKph = 175f;

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

        private bool wheelRigReady;
        private bool drivingEnabled = true;
        private bool warnedMissingPrometeo;
        private bool throttleHeld;
        private bool reverseHeld;
        private bool handbrakeHeld;
        private bool steeringInputHeld;
        private float prometeoRetryTimer;
        private float resetHoldTimer;

        private int engineUpgradeLevel;
        private int gripUpgradeLevel;
        private int stabilityUpgradeLevel;
        private int vehicleSpeedBonus;
        private int vehicleAccelerationBonus;
        private float vehicleGripMultiplier = 1f;
        private float vehicleStabilityBonus;
        private float vehicleMassMultiplier = 1f;
        private float vehicleSteeringMultiplier = 1f;
        private float vehicleBrakeMultiplier = 1f;
        private float vehiclePowerMultiplier = 1f;
        private float vehicleDriftMultiplier = 1f;
        private DriveMode currentDriveMode =
            DriveMode.Comfort;
        private float driveModeMessageTimer;

        public DriveMode CurrentDriveMode =>
            currentDriveMode;

        public string DriveModeDisplayName =>
            currentDriveMode switch
            {
                DriveMode.Sport => "СПОРТ",
                DriveMode.Drift => "ДРИФТ",
                _ => "КОМФОРТ"
            };

        public string DriveModeDescription =>
            currentDriveMode switch
            {
                DriveMode.Sport =>
                    "максимальная тяга и сцепление",
                DriveMode.Drift =>
                    "острый руль и свободная задняя ось",
                _ =>
                    "стабильная повседневная езда"
            };

        public bool ShowDriveModeMessage =>
            driveModeMessageTimer > 0f;

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
            currentDriveMode =
                (DriveMode)Mathf.Clamp(
                    PlayerPrefs.GetInt(
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

        }

        private void Update()
        {
            Keyboard modeKeyboard =
                Keyboard.current;

            bool conflictingControlPressed =
                modeKeyboard != null &&
                (modeKeyboard.spaceKey.isPressed ||
                 modeKeyboard.wKey.isPressed ||
                 modeKeyboard.sKey.isPressed ||
                 modeKeyboard.aKey.isPressed ||
                 modeKeyboard.dKey.isPressed ||
                 modeKeyboard.upArrowKey.isPressed ||
                 modeKeyboard.downArrowKey.isPressed ||
                 modeKeyboard.leftArrowKey.isPressed ||
                 modeKeyboard.rightArrowKey.isPressed);

            if (drivingEnabled &&
                resetHoldTimer <= 0f &&
                modeKeyboard != null &&
                modeKeyboard.qKey.wasPressedThisFrame &&
                !conflictingControlPressed &&
                SpeedKph <= 1f)
            {
                CycleDriveMode();
            }

            if (driveModeMessageTimer > 0f)
            {
                driveModeMessageTimer =
                    Mathf.Max(
                        0f,
                        driveModeMessageTimer -
                        Time.deltaTime);
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

            wheel.transform.localPosition =
                localCenter;
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

            ApplyPrometeoTuning();

            warnedMissingPrometeo = false;

            SetPrometeoEnabled(
                drivingEnabled &&
                resetHoldTimer <= 0f);

            Debug.Log(
                "Motor City: Prometeo Car Controller is now the active vehicle physics controller.");

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

            throttleInputProxy =
                CreateInputProxy(
                    "Prometeo Input — Throttle");

            reverseInputProxy =
                CreateInputProxy(
                    "Prometeo Input — Reverse");

            leftInputProxy =
                CreateInputProxy(
                    "Prometeo Input — Left");

            rightInputProxy =
                CreateInputProxy(
                    "Prometeo Input — Right");

            handbrakeInputProxy =
                CreateInputProxy(
                    "Prometeo Input — Handbrake");

            bool ready =
                throttleInputProxy != null &&
                reverseInputProxy != null &&
                leftInputProxy != null &&
                rightInputProxy != null &&
                handbrakeInputProxy != null;

            if (!ready)
                return false;

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
                Keyboard keyboard =
                    Keyboard.current;

                if (keyboard != null)
                {
                    throttle =
                        keyboard.wKey.isPressed ||
                        keyboard.upArrowKey.isPressed;

                    reverse =
                        keyboard.sKey.isPressed ||
                        keyboard.downArrowKey.isPressed;

                    left =
                        keyboard.aKey.isPressed ||
                        keyboard.leftArrowKey.isPressed;

                    right =
                        keyboard.dKey.isPressed ||
                        keyboard.rightArrowKey.isPressed;

                    handbrake =
                        keyboard.spaceKey.isPressed;
                }

                Gamepad gamepad =
                    Gamepad.current;

                if (gamepad != null)
                {
                    throttle |=
                        gamepad.rightTrigger.ReadValue() >
                        0.12f;

                    reverse |=
                        gamepad.leftTrigger.ReadValue() >
                        0.12f;

                    float steer =
                        gamepad.leftStick.x.ReadValue();

                    left |= steer < -0.16f;
                    right |= steer > 0.16f;

                    handbrake |=
                        gamepad.buttonSouth.isPressed;
                }
            }

            throttleHeld = throttle;
            reverseHeld = reverse;
            handbrakeHeld = handbrake;
            steeringInputHeld =
                left ||
                right;

            SetInputProxyPressed(
                throttleInputProxy,
                throttle);

            SetInputProxyPressed(
                reverseInputProxy,
                reverse);

            SetInputProxyPressed(
                leftInputProxy,
                left);

            SetInputProxyPressed(
                rightInputProxy,
                right);

            SetInputProxyPressed(
                handbrakeInputProxy,
                handbrake);
        }

        private static void SetInputProxyPressed(
            Component proxy,
            bool pressed)
        {
            if (proxy == null)
                return;

            Type type =
                proxy.GetType();

            FieldInfo field =
                type.GetField(
                    "buttonPressed",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (field != null &&
                field.FieldType == typeof(bool))
            {
                field.SetValue(
                    proxy,
                    pressed);
                return;
            }

            PropertyInfo property =
                type.GetProperty(
                    "buttonPressed",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (property != null &&
                property.CanWrite &&
                property.PropertyType == typeof(bool))
            {
                property.SetValue(
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
                        brakeForce + 260,
                    DriveMode.Drift =>
                        Mathf.Max(
                            650,
                            brakeForce - 100),
                    _ =>
                        brakeForce + 60
                };

            int tunedBrakeForce =
                Mathf.RoundToInt(
                    baseBrakeForce *
                    vehicleBrakeMultiplier);

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

                body.angularDamping =
                    Mathf.Max(
                        0.05f,
                        angularDamping +
                        vehicleStabilityBonus +
                        GetStabilityDampingBonus() +
                        modeDamping);
            }
        }

        private int GetTunedMaxSpeedKph()
        {
            int modeBonus =
                currentDriveMode switch
                {
                    DriveMode.Sport => 30,
                    DriveMode.Drift => -12,
                    _ => -22
                };

            return Mathf.Clamp(
                baseMaxSpeedKph +
                vehicleSpeedBonus +
                GetEngineSpeedBonus() +
                modeBonus,
                20,
                360);
        }

        private int GetTunedAccelerationMultiplier()
        {
            int modeBonus =
                currentDriveMode switch
                {
                    DriveMode.Sport => 4,
                    DriveMode.Drift => 2,
                    _ => 0
                };

            return Mathf.Clamp(
                accelerationMultiplier +
                vehicleAccelerationBonus +
                GetEngineAccelerationBonus() +
                modeBonus,
                1,
                24);
        }

        private int GetEngineSpeedBonus()
        {
            return
                engineUpgradeLevel switch
                {
                    1 => 10,
                    2 => 22,
                    3 => 36,
                    4 => 52,
                    5 => 70,
                    _ => 0
                };
        }

        private int GetEngineAccelerationBonus()
        {
            return
                engineUpgradeLevel switch
                {
                    1 => 1,
                    2 => 2,
                    3 => 4,
                    4 => 6,
                    5 => 8,
                    _ => 0
                };
        }

        private float GetEngineAssistBonus()
        {
            return
                engineUpgradeLevel switch
                {
                    1 => 0.12f,
                    2 => 0.25f,
                    3 => 0.40f,
                    4 => 0.58f,
                    5 => 0.78f,
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
                    (1f +
                     GetEngineAssistBonus());

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

            float speedIntensity =
                Mathf.InverseLerp(
                    minimumDriftSpeedKph,
                    72f,
                    SpeedKph);

            DriftIntensity =
                Mathf.Clamp01(
                    Mathf.Max(
                        slipIntensity *
                        angleIntensity,
                        ReadPrometeoBool("isDrifting")
                            ? 0.65f
                            : 0f) *
                    Mathf.Lerp(
                        0.72f,
                        1f,
                        speedIntensity));

            bool physicalSlide =
                GroundedWheels >= 3 &&
                SpeedKph >= minimumDriftSpeedKph &&
                RearSidewaysSlip >=
                    driftDetectionSideSlip &&
                angle >=
                    driftDetectionAngle;

            IsSliding =
                physicalSlide ||
                (ReadPrometeoBool("isDrifting") &&
                 SpeedKph >= minimumDriftSpeedKph);
        }

        public void ApplyStraightLineStability()
        {
            if (body == null ||
                !drivingEnabled ||
                resetHoldTimer > 0f ||
                steeringInputHeld ||
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
            if (!handbrakeHeld ||
                !drivingEnabled ||
                resetHoldTimer > 0f)
                return;

            float torque =
                SpeedKph <= lowSpeedThresholdKph
                    ? lowSpeedRearBrakeTorque
                    : rearBrakeTorque;

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
            int speedBonusKph,
            int accelerationBonus,
            float gripMultiplier,
            float stabilityBonus,
            float massMultiplier,
            float steeringMultiplier,
            float brakeMultiplier,
            float powerMultiplier,
            float driftMultiplier)
        {
            vehicleSpeedBonus =
                Mathf.Clamp(
                    speedBonusKph,
                    -50,
                    80);

            vehicleAccelerationBonus =
                Mathf.Clamp(
                    accelerationBonus,
                    -4,
                    6);

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
                    1.18f);

            vehicleSteeringMultiplier =
                Mathf.Clamp(
                    steeringMultiplier,
                    0.88f,
                    1.16f);

            vehicleBrakeMultiplier =
                Mathf.Clamp(
                    brakeMultiplier,
                    0.88f,
                    1.22f);

            vehiclePowerMultiplier =
                Mathf.Clamp(
                    powerMultiplier,
                    0.88f,
                    1.20f);

            vehicleDriftMultiplier =
                Mathf.Clamp(
                    driftMultiplier,
                    0.78f,
                    1.24f);

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
            float upgradeGrip =
                (1f +
                 GetGripUpgradeBonus()) *
                Mathf.Clamp(
                    vehicleGripMultiplier,
                    0.75f,
                    1.30f);

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
                            rear ? 0.38f : 0.27f;
                        sideways.asymptoteSlip =
                            rear ? 0.78f : 0.58f;
                        sideways.extremumValue = 1f;
                        sideways.asymptoteValue =
                            rear ? 0.62f : 0.76f;
                        sideways.stiffness =
                            (rear ? 0.70f : 1.20f) *
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

                wheel.forwardFriction =
                    forward;

                wheel.sidewaysFriction =
                    sideways;
            }
        }

        public void CycleDriveMode()
        {
            int nextMode =
                ((int)currentDriveMode + 1) %
                3;

            currentDriveMode =
                (DriveMode)nextMode;

            ApplyDriveModeTuning();

            PlayerPrefs.SetInt(
                DriveModeKey,
                nextMode);

            PlayerPrefs.Save();

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

        public void SetDrivingEnabled(
            bool enabled)
        {
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
            SetDrivingEnabled(false);

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
                body.Sleep();
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
            if (prometeo is Behaviour behaviour)
                behaviour.enabled = enabled;
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
                prometeo.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (field == null ||
                field.FieldType != typeof(bool))
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
