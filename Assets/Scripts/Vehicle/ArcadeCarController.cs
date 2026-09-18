using System;
using System.Reflection;
using UnityEngine;

namespace MotorCity.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        private const int FrontLeft = 0;
        private const int FrontRight = 1;
        private const int RearLeft = 2;
        private const int RearRight = 3;

        [Header("Prometeo tuning")]
        [SerializeField] private int baseMaxSpeedKph = 170;
        [SerializeField] private int maxReverseSpeedKph = 45;
        [SerializeField] private int accelerationMultiplier = 6;
        [SerializeField] private int maxSteeringAngle = 32;
        [SerializeField] private float steeringSpeed = 0.68f;
        [SerializeField] private int brakeForce = 900;
        [SerializeField] private int decelerationMultiplier = 4;
        [SerializeField] private int handbrakeDriftMultiplier = 5;
        [SerializeField] private Vector3 bodyMassCenter =
            new(0f, 0.32f, 0.05f);

        [Header("Vehicle body")]
        [SerializeField] private float vehicleMass = 1400f;
        [SerializeField] private float angularDamping = 0.24f;

        [Header("Prometeo WheelColliders")]
        [SerializeField] private float fallbackWheelRadius = 0.36f;
        [SerializeField] private float wheelMass = 20f;
        [SerializeField] private float suspensionDistance = 0.24f;
        [SerializeField] private float suspensionSpring = 36000f;
        [SerializeField] private float suspensionDamper = 4400f;
        [SerializeField] private float suspensionTargetPosition = 0.5f;
        [SerializeField] private float wheelDampingRate = 0.3f;
        [SerializeField] private float forceAppPointDistance = 0.05f;

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

        private bool wheelRigReady;
        private bool drivingEnabled = true;
        private bool warnedMissingPrometeo;
        private float prometeoRetryTimer;
        private float resetHoldTimer;

        private int engineUpgradeLevel;
        private int gripUpgradeLevel;
        private int stabilityUpgradeLevel;

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
            ReadPrometeoBool("isTractionLocked");

        public bool HasPrometeoPhysics =>
            prometeo != null;

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
        }

        private void Update()
        {
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
                HoldVehicleStill();

            UpdateTelemetry();
        }

        public void ConfigurePrometeoRig(
            Transform[] visualWheelRoots,
            Vector3[] wheelCentersLocal,
            float measuredWheelRadius)
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

            for (int i = 0; i < 4; i++)
            {
                wheelMeshes[i] =
                    visualWheelRoots[i] != null
                        ? visualWheelRoots[i].gameObject
                        : null;

                ConfigureWheelCollider(
                    i,
                    wheelCentersLocal[i],
                    radius);
            }

            wheelRigReady = true;
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
                suspensionDistance;
            wheel.forceAppPointDistance =
                forceAppPointDistance;
            wheel.wheelDampingRate =
                wheelDampingRate;

            JointSpring spring =
                wheel.suspensionSpring;

            spring.spring = suspensionSpring;
            spring.damper = suspensionDamper;
            spring.targetPosition =
                suspensionTargetPosition;

            wheel.suspensionSpring = spring;

            WheelFrictionCurve forward =
                wheel.forwardFriction;

            forward.extremumSlip = 0.36f;
            forward.extremumValue = 1f;
            forward.asymptoteSlip = 0.78f;
            forward.asymptoteValue = 0.72f;
            forward.stiffness = 1.12f;

            wheel.forwardFriction = forward;

            WheelFrictionCurve sideways =
                wheel.sidewaysFriction;

            sideways.extremumSlip = 0.26f;
            sideways.extremumValue = 1f;
            sideways.asymptoteSlip = 0.55f;
            sideways.asymptoteValue = 0.76f;
            sideways.stiffness = 1.08f;

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
            SetPrometeoField(
                "useTouchControls",
                false);

            ApplyPrometeoTuning();

            warnedMissingPrometeo = false;

            SetPrometeoEnabled(
                drivingEnabled &&
                resetHoldTimer <= 0f);

            Debug.Log(
                "Motor City: Prometeo Car Controller is now the active vehicle physics controller.");

            return true;
        }

        private void ApplyPrometeoTuning()
        {
            if (prometeo == null)
                return;

            int tunedMaxSpeed =
                Mathf.Clamp(
                    baseMaxSpeedKph +
                    engineUpgradeLevel * 7,
                    20,
                    190);

            int tunedAcceleration =
                Mathf.Clamp(
                    accelerationMultiplier +
                    engineUpgradeLevel,
                    1,
                    10);

            int tunedDriftMultiplier =
                Mathf.Clamp(
                    handbrakeDriftMultiplier -
                    gripUpgradeLevel / 2,
                    3,
                    10);

            Vector3 tunedCenterOfMass =
                bodyMassCenter +
                Vector3.down *
                (stabilityUpgradeLevel * 0.025f);

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
                maxSteeringAngle);
            SetPrometeoField(
                "steeringSpeed",
                steeringSpeed);
            SetPrometeoField(
                "brakeForce",
                brakeForce);
            SetPrometeoField(
                "decelerationMultiplier",
                decelerationMultiplier);
            SetPrometeoField(
                "handbrakeDriftMultiplier",
                tunedDriftMultiplier);
            SetPrometeoField(
                "bodyMassCenter",
                tunedCenterOfMass);

            if (body != null)
            {
                body.mass = vehicleMass;
                body.centerOfMass =
                    tunedCenterOfMass;
                body.angularDamping =
                    angularDamping +
                    stabilityUpgradeLevel * 0.035f;
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

        public void ApplyUpgradeLevels(
            int engineLevel,
            int gripLevel,
            int stabilityLevel)
        {
            engineUpgradeLevel =
                Mathf.Clamp(
                    engineLevel,
                    0,
                    3);

            gripUpgradeLevel =
                Mathf.Clamp(
                    gripLevel,
                    0,
                    3);

            stabilityUpgradeLevel =
                Mathf.Clamp(
                    stabilityLevel,
                    0,
                    3);

            ApplyPrometeoTuning();

            float gripMultiplier =
                1f +
                gripUpgradeLevel * 0.055f;

            for (int i = 0;
                 i < wheelColliders.Length;
                 i++)
            {
                WheelCollider wheel =
                    wheelColliders[i];

                if (wheel == null)
                    continue;

                WheelFrictionCurve forward =
                    wheel.forwardFriction;
                forward.stiffness =
                    1.12f * gripMultiplier;
                wheel.forwardFriction = forward;

                WheelFrictionCurve sideways =
                    wheel.sidewaysFriction;
                sideways.stiffness =
                    1.08f * gripMultiplier;
                wheel.sidewaysFriction = sideways;
            }
        }

        public void UseAutomaticMassProperties()
        {
            if (body == null)
                return;

            body.mass = vehicleMass;
            body.ResetInertiaTensor();
            body.centerOfMass = bodyMassCenter;
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
            foreach (System.Reflection.Assembly assembly
                     in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type direct =
                    assembly.GetType(
                        "PrometeoCarController",
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
                    continue;

                foreach (Type type in types)
                {
                    if (type != null &&
                        type.Name ==
                        "PrometeoCarController")
                        return type;
                }
            }

            return null;
        }
    }
}
