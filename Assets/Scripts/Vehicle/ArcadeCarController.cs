using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        private const int FrontLeft = 0;
        private const int FrontRight = 1;
        private const int RearLeft = 2;
        private const int RearRight = 3;

        [Header("Drift-capable Road Controller")]
        [SerializeField] private float brakePower = 10f;
        [SerializeField] private float wheelRotateSpeed = 20f;
        [SerializeField] private float wheelSteeringAngle = 34f;
        [SerializeField] private float wheelAcceleration = 28f;
        [SerializeField] private float wheelMaxSpeed = 2700f;
        [SerializeField] private float serviceBrakeTorque = 9200f;
        [SerializeField] private float reverseMotorTorque = 760f;
        [SerializeField] private float maxReverseSpeedKph = 48f;
        [SerializeField] private float highSpeedSteerAngle = 14f;
        [SerializeField] private float steerFadeSpeedKph = 145f;
        [SerializeField] private float aerodynamicDownforce = 1.8f;

        [Header("Prefab Rigidbody")]
        [SerializeField] private float vehicleMass = 1480f;
        [SerializeField] private float angularDrag = 0.28f;
        [SerializeField] private float reverseDrag = 0.08f;
        [SerializeField] private Vector3 centerOfMass = new(0f, 0.32f, 0.05f);
        [SerializeField] private float antiRollForce = 5200f;
        [SerializeField] private float physicsHalfTrack = 1.05f;
        [SerializeField] private float physicsHalfWheelbase = 1.72f;

        [Header("Prefab WheelCollider")]
        [SerializeField] private float wheelRadius = 0.5f;
        [SerializeField] private float wheelMass = 18f;
        [SerializeField] private float suspensionDistance = 0.28f;
        [SerializeField] private float suspensionSpring = 36000f;
        [SerializeField] private float suspensionDamper = 4200f;
        [SerializeField] private float suspensionTargetPosition = 0.5f;
        [SerializeField] private float forceAppPointDistance = 0.02f;
        [SerializeField] private float wheelDampingRate = 0.25f;

        [Header("Drift / Handbrake")]
        [SerializeField] private float rearGrip = 1.08f;
        [SerializeField] private float rearPowerDriftGrip = 0.82f;
        [SerializeField] private float rearHandbrakeGrip = 0.56f;
        [SerializeField] private float powerDriftMinimumSpeedKph = 32f;
        [SerializeField] private float powerDriftSteerThreshold = 0.42f;
        [SerializeField] private float powerDriftThrottleThreshold = 0.62f;
        [SerializeField] private float handbrakeRearTorque = 10500f;
        [SerializeField] private float handbrakeDeceleration = 0.65f;
        [SerializeField] private float parkingBrakeTorque = 30000f;
        [SerializeField] private float parkingBrakeSpeedKph = 8f;
        [SerializeField] private float driftDetectionSideSlip = 0.12f;
        [SerializeField] private float driftDetectionAngle = 8f;
        [SerializeField] private float driftAssistYawAcceleration = 4.6f;
        [SerializeField] private float driftAssistMinimumSpeedKph = 28f;
        [SerializeField] private float driftAssistFullSpeedKph = 105f;

        [Header("Legacy Input.GetAxis Feel")]
        [SerializeField] private float inputSensitivity = 3f;
        [SerializeField] private float inputGravity = 3f;
        [SerializeField] private bool inputSnap = true;

        private readonly WheelCollider[] wheelColliders = new WheelCollider[4];
        private readonly Transform[] wheelVisualRoots = new Transform[4];
        private readonly Transform[] brakeVisualRoots = new Transform[4];
        private readonly float[] steeringAngles = new float[4];
        private readonly float[] sourceMotorTorque = new float[4];
        private readonly Vector3[] visualWheelOffsets = new Vector3[4];

        private Vector3[] wheelCenters =
        {
            new(-1.05f, 0.42f,  1.72f),
            new( 1.05f, 0.42f,  1.72f),
            new(-1.05f, 0.42f, -1.72f),
            new( 1.05f, 0.42f, -1.72f)
        };

        private readonly string[] fallbackWheelNames =
        {
            "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR"
        };

        private Rigidbody body;
        private float horizontal;
        private float vertical;
        private int engineUpgradeLevel;
        private int gripUpgradeLevel;
        private int stabilityUpgradeLevel;
        private bool drivingEnabled = true;
        private bool handbrakeInput;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float ForwardSpeedKph =>
            body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
        public bool IsHandbrake => handbrakeInput;
        public int GroundedWheels { get; private set; }
        public float RearSidewaysSlip { get; private set; }
        public float RearForwardSlip { get; private set; }
        public bool IsSliding { get; private set; }
        public float DriftIntensity { get; private set; }

        public float SlipAngleDegrees
        {
            get
            {
                if (body == null || body.linearVelocity.sqrMagnitude < 1f) return 0f;

                Vector3 localVelocity = transform.InverseTransformDirection(body.linearVelocity);
                return Mathf.Atan2(
                    localVelocity.x,
                    Mathf.Max(0.1f, Mathf.Abs(localVelocity.z))) * Mathf.Rad2Deg;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = vehicleMass;
            body.linearDamping = 0f;
            body.angularDamping = angularDrag;
            body.useGravity = true;
            body.centerOfMass = centerOfMass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            SetupFallbackWheelVisuals();
            BuildOrReconfigureWheelColliders();
        }

        public void ConfigureExternalWheelRig(
            Transform[] visualRoots,
            Transform[] brakeRoots,
            Vector3[] centers,
            float measuredWheelRadius)
        {
            if (centers == null || centers.Length < 4) return;

            if (measuredWheelRadius > 0.01f)
                wheelRadius = Mathf.Clamp(measuredWheelRadius, 0.32f, 0.52f);

            float averageY = 0f;
            for (int i = 0; i < 4; i++) averageY += centers[i].y;
            averageY *= 0.25f;

            wheelCenters = new[]
            {
                new Vector3(-physicsHalfTrack, averageY,  physicsHalfWheelbase),
                new Vector3( physicsHalfTrack, averageY,  physicsHalfWheelbase),
                new Vector3(-physicsHalfTrack, averageY, -physicsHalfWheelbase),
                new Vector3( physicsHalfTrack, averageY, -physicsHalfWheelbase)
            };

            for (int i = 0; i < 4; i++)
            {
                wheelVisualRoots[i] =
                    visualRoots != null && visualRoots.Length > i
                        ? visualRoots[i]
                        : wheelVisualRoots[i];

                brakeVisualRoots[i] =
                    brakeRoots != null && brakeRoots.Length > i
                        ? brakeRoots[i]
                        : null;

                visualWheelOffsets[i] = centers[i] - wheelCenters[i];
            }

            BuildOrReconfigureWheelColliders();
        }

        private void SetupFallbackWheelVisuals()
        {
            for (int i = 0; i < 4; i++)
            {
                Transform mesh = transform.Find(fallbackWheelNames[i]);
                if (mesh == null) continue;

                Vector3 center = mesh.localPosition;
                GameObject pivotObject = new($"FallbackWheelVisual_{i}");
                Transform pivot = pivotObject.transform;
                pivot.SetParent(transform, false);
                pivot.localPosition = center;
                pivot.localRotation = Quaternion.identity;

                mesh.SetParent(pivot, true);
                wheelVisualRoots[i] = pivot;
                wheelCenters[i] = center;
            }
        }

        private void BuildOrReconfigureWheelColliders()
        {
            for (int i = 0; i < 4; i++)
            {
                if (wheelColliders[i] == null)
                {
                    GameObject wheelObject = new($"PhysicsWheel_{i}");
                    wheelObject.transform.SetParent(transform, false);
                    wheelColliders[i] = wheelObject.AddComponent<WheelCollider>();
                }

                WheelCollider wheel = wheelColliders[i];
                wheel.transform.localPosition =
                    wheelCenters[i] +
                    Vector3.up * (suspensionDistance * suspensionTargetPosition);
                wheel.transform.localRotation = Quaternion.identity;
                wheel.transform.localScale = Vector3.one;

                wheel.center = Vector3.zero;
                wheel.radius = wheelRadius;
                wheel.mass = wheelMass;
                wheel.suspensionDistance = suspensionDistance;
                wheel.forceAppPointDistance = forceAppPointDistance;
                wheel.wheelDampingRate = wheelDampingRate;

                JointSpring spring = wheel.suspensionSpring;
                spring.spring = suspensionSpring;
                spring.damper = suspensionDamper;
                spring.targetPosition = suspensionTargetPosition;
                wheel.suspensionSpring = spring;

                WheelFrictionCurve forward = wheel.forwardFriction;
                forward.extremumSlip = 0.32f;
                forward.extremumValue = 1f;
                forward.asymptoteSlip = 0.72f;
                forward.asymptoteValue = 0.72f;
                forward.stiffness = 1.12f;
                wheel.forwardFriction = forward;

                WheelFrictionCurve sideways = wheel.sidewaysFriction;
                sideways.extremumSlip = 0.28f;
                sideways.extremumValue = 1f;
                sideways.asymptoteSlip = 0.58f;
                sideways.asymptoteValue = 0.78f;
                float gripMultiplier = 1f + gripUpgradeLevel * 0.08f;
                sideways.stiffness =
                    (i < 2 ? 1.22f : rearGrip) *
                    gripMultiplier;
                wheel.sidewaysFriction = sideways;
            }

        }

        private void Update()
        {
            if (!drivingEnabled)
            {
                horizontal = 0f;
                vertical = 0f;
                handbrakeInput = true;
                return;
            }

            ReadInput();
        }

        private void FixedUpdate()
        {
            MeasureWheelState();
            UpdateDynamicRearGrip();
            ApplySourceController();
            ApplyHandbrake();
            ApplyDriftAssist();

            ApplyAntiRoll(FrontLeft, FrontRight);
            ApplyAntiRoll(RearLeft, RearRight);
        }

        private void MeasureWheelState()
        {
            GroundedWheels = 0;
            float rearSideways = 0f;
            float rearForward = 0f;
            int rearGrounded = 0;

            for (int i = 0; i < 4; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                if (wheel == null ||
                    !wheel.GetGroundHit(out WheelHit hit))
                    continue;

                GroundedWheels++;

                if (i >= RearLeft)
                {
                    rearSideways +=
                        Mathf.Abs(hit.sidewaysSlip);
                    rearForward +=
                        Mathf.Abs(hit.forwardSlip);
                    rearGrounded++;
                }
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
                    34f,
                    angle);

            float speedIntensity =
                Mathf.InverseLerp(
                    22f,
                    72f,
                    SpeedKph);

            DriftIntensity =
                Mathf.Clamp01(
                    Mathf.Min(
                        slipIntensity * 1.18f,
                        angleIntensity * 1.12f) *
                    Mathf.Lerp(0.7f, 1f, speedIntensity));

            IsSliding =
                GroundedWheels >= 3 &&
                SpeedKph >= 24f &&
                RearSidewaysSlip >= driftDetectionSideSlip &&
                angle >= driftDetectionAngle;
        }

        private void ApplyAntiRoll(int leftIndex, int rightIndex)
        {
            WheelCollider left = wheelColliders[leftIndex];
            WheelCollider right = wheelColliders[rightIndex];
            if (left == null || right == null) return;

            float leftTravel = 1f;
            float rightTravel = 1f;
            bool leftGrounded = left.GetGroundHit(out WheelHit leftHit);
            bool rightGrounded = right.GetGroundHit(out WheelHit rightHit);

            if (leftGrounded)
            {
                Vector3 localHit = left.transform.InverseTransformPoint(leftHit.point);
                leftTravel = (-localHit.y - left.radius) /
                             Mathf.Max(0.001f, left.suspensionDistance);
            }

            if (rightGrounded)
            {
                Vector3 localHit = right.transform.InverseTransformPoint(rightHit.point);
                rightTravel = (-localHit.y - right.radius) /
                              Mathf.Max(0.001f, right.suspensionDistance);
            }

            float stabilityMultiplier = 1f + stabilityUpgradeLevel * 0.16f;
            float antiRoll = (leftTravel - rightTravel) * antiRollForce * stabilityMultiplier;

            if (leftGrounded)
                body.AddForceAtPosition(
                    left.transform.up * -antiRoll,
                    left.transform.position,
                    ForceMode.Force);

            if (rightGrounded)
                body.AddForceAtPosition(
                    right.transform.up * antiRoll,
                    right.transform.position,
                    ForceMode.Force);
        }

        private void ReadInput()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            float keyboardVertical = 0f;
            float keyboardHorizontal = 0f;
            bool keyboardHandbrake = false;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardVertical += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardVertical -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardHorizontal -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardHorizontal += 1f;
                keyboardHandbrake = keyboard.spaceKey.isPressed;
            }

            float gamepadVertical = 0f;
            float gamepadHorizontal = 0f;
            bool gamepadHandbrake = false;

            if (gamepad != null)
            {
                gamepadVertical =
                    gamepad.rightTrigger.ReadValue() -
                    gamepad.leftTrigger.ReadValue();
                gamepadHorizontal = gamepad.leftStick.x.ReadValue();
                gamepadHandbrake = gamepad.buttonSouth.isPressed;
            }

            float verticalTarget =
                Mathf.Abs(keyboardVertical) >= Mathf.Abs(gamepadVertical)
                    ? keyboardVertical
                    : gamepadVertical;

            float horizontalTarget =
                Mathf.Abs(keyboardHorizontal) >= Mathf.Abs(gamepadHorizontal)
                    ? keyboardHorizontal
                    : gamepadHorizontal;

            vertical = SmoothLegacyAxis(vertical, verticalTarget);
            horizontal = SmoothLegacyAxis(horizontal, horizontalTarget);
            handbrakeInput = keyboardHandbrake || gamepadHandbrake;
        }

        private float SmoothLegacyAxis(float current, float target)
        {
            if (inputSnap &&
                Mathf.Abs(target) > 0.001f &&
                Mathf.Abs(current) > 0.001f &&
                Mathf.Sign(target) != Mathf.Sign(current))
            {
                current = 0f;
            }

            float rate = Mathf.Abs(target) > 0.001f ? inputSensitivity : inputGravity;
            return Mathf.MoveTowards(current, target, rate * Time.deltaTime);
        }

        private static float SourceLerpFactor(float sourceRate)
        {
            float sourceFrameT = Mathf.Clamp01(sourceRate / 60f);
            if (sourceFrameT >= 1f) return 1f;

            return 1f - Mathf.Pow(
                1f - sourceFrameT,
                Time.fixedDeltaTime * 60f);
        }

        private void UpdateDynamicRearGrip()
        {
            float gripMultiplier =
                1f + gripUpgradeLevel * 0.08f;

            bool powerDrift =
                !handbrakeInput &&
                ForwardSpeedKph >= powerDriftMinimumSpeedKph &&
                vertical >= powerDriftThrottleThreshold &&
                Mathf.Abs(horizontal) >= powerDriftSteerThreshold;

            bool sustainingDrift =
                !handbrakeInput &&
                IsSliding &&
                vertical >= 0.24f;

            float targetRearGrip =
                handbrakeInput
                    ? rearHandbrakeGrip
                    : ((powerDrift || sustainingDrift)
                        ? rearPowerDriftGrip
                        : rearGrip);

            for (int i = RearLeft; i <= RearRight; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                if (wheel == null) continue;

                WheelFrictionCurve sideways =
                    wheel.sidewaysFriction;

                sideways.stiffness =
                    targetRearGrip * gripMultiplier;

                wheel.sidewaysFriction = sideways;
            }
        }

        private void ApplyDriftAssist()
        {
            if (body == null ||
                GroundedWheels < 3 ||
                SpeedKph < driftAssistMinimumSpeedKph)
                return;

            bool driftIntent =
                handbrakeInput ||
                IsSliding ||
                (vertical > 0.52f &&
                 Mathf.Abs(horizontal) > 0.34f);

            if (!driftIntent ||
                Mathf.Abs(horizontal) < 0.05f)
                return;

            float speedFactor =
                Mathf.InverseLerp(
                    driftAssistMinimumSpeedKph,
                    driftAssistFullSpeedKph,
                    SpeedKph);

            float slideFactor =
                IsSliding
                    ? Mathf.Lerp(
                        0.62f,
                        1f,
                        DriftIntensity)
                    : 0.42f;

            float stabilityReduction =
                1f - stabilityUpgradeLevel * 0.08f;

            body.AddTorque(
                transform.up *
                horizontal *
                driftAssistYawAcceleration *
                Mathf.Lerp(0.55f, 1f, speedFactor) *
                slideFactor *
                stabilityReduction,
                ForceMode.Acceleration);

            if (IsSliding && vertical > 0.1f)
            {
                body.AddForce(
                    transform.forward *
                    (1.2f + DriftIntensity * 1.6f) *
                    vertical,
                    ForceMode.Acceleration);
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

        private void ApplySourceController()
        {
            float absSpeed = Mathf.Abs(ForwardSpeedKph);
            float steerT = Mathf.InverseLerp(20f, steerFadeSpeedKph, absSpeed);
            float steerLimit = Mathf.Lerp(wheelSteeringAngle, highSpeedSteerAngle, steerT);
            float targetSteer = horizontal * steerLimit;

            bool brakingForward = vertical < -0.05f && ForwardSpeedKph > 3f;
            bool brakingReverse = vertical > 0.05f && ForwardSpeedKph < -3f;
            bool wantsReverse = vertical < -0.05f && ForwardSpeedKph <= 3f;
            bool wantsForward = vertical > 0.05f && ForwardSpeedKph >= -3f;

            for (int i = 0; i < 4; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                if (wheel == null) continue;

                bool frontWheel = i < 2;
                bool drivenWheel = i >= RearLeft;

                steeringAngles[i] = frontWheel
                    ? Mathf.LerpAngle(
                        steeringAngles[i],
                        targetSteer,
                        SourceLerpFactor(wheelRotateSpeed))
                    : 0f;

                wheel.steerAngle = frontWheel ? steeringAngles[i] : 0f;
                wheel.motorTorque = 0f;
                wheel.brakeTorque = 0f;

                if (brakingForward || brakingReverse)
                {
                    wheel.brakeTorque =
                        serviceBrakeTorque * Mathf.Abs(vertical) *
                        (frontWheel ? 0.62f : 0.38f);
                    sourceMotorTorque[i] = 0f;
                    continue;
                }

                if (!drivenWheel)
                {
                    sourceMotorTorque[i] = 0f;
                    continue;
                }

                float requestedTorque = 0f;

                if (wantsForward)
                    requestedTorque = wheelMaxSpeed * vertical;
                else if (wantsReverse && absSpeed < maxReverseSpeedKph)
                    requestedTorque = reverseMotorTorque * vertical;

                sourceMotorTorque[i] = Mathf.Lerp(
                    sourceMotorTorque[i],
                    requestedTorque,
                    SourceLerpFactor(wheelAcceleration));

                wheel.motorTorque = sourceMotorTorque[i];
            }

            body.linearDamping =
                Mathf.Abs(vertical) < 0.05f ? 0.018f : reverseDrag;
            body.angularDamping =
                angularDrag * (1f + stabilityUpgradeLevel * 0.14f);

            if (GroundedWheels >= 2 && body.linearVelocity.sqrMagnitude > 4f)
            {
                body.AddForce(
                    -transform.up *
                    aerodynamicDownforce *
                    body.linearVelocity.sqrMagnitude,
                    ForceMode.Force);
            }
        }

        private void ApplyHandbrake()
        {
            bool parkingMode = SpeedKph <= parkingBrakeSpeedKph;

            for (int i = 0; i < wheelColliders.Length; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                if (wheel == null) continue;

                if (!handbrakeInput)
                    continue;

                wheel.motorTorque = 0f;
                sourceMotorTorque[i] = 0f;

                bool rearWheel = i == RearLeft || i == RearRight;
                wheel.brakeTorque = parkingMode
                    ? parkingBrakeTorque
                    : (rearWheel ? handbrakeRearTorque : 0f);
            }

            if (handbrakeInput)
            {
                Vector3 planarVelocity =
                    Vector3.ProjectOnPlane(body.linearVelocity, transform.up);

                if (planarVelocity.sqrMagnitude > 0.01f)
                {
                    body.AddForce(
                        -planarVelocity.normalized * handbrakeDeceleration,
                        ForceMode.Acceleration);
                }
            }

            if (handbrakeInput && parkingMode && body.linearVelocity.sqrMagnitude < 0.36f)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        public void ApplyUpgradeLevels(int engineLevel, int gripLevel, int stabilityLevel)
        {
            engineUpgradeLevel = Mathf.Clamp(engineLevel, 0, 3);
            gripUpgradeLevel = Mathf.Clamp(gripLevel, 0, 3);
            stabilityUpgradeLevel = Mathf.Clamp(stabilityLevel, 0, 3);

            wheelMaxSpeed = 2700f * (1f + engineUpgradeLevel * 0.15f);
            wheelAcceleration = 28f * (1f + engineUpgradeLevel * 0.10f);

            for (int i = 0; i < wheelColliders.Length; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                if (wheel == null) continue;

                WheelFrictionCurve sideways = wheel.sidewaysFriction;
                float gripMultiplier = 1f + gripUpgradeLevel * 0.08f;
                sideways.stiffness =
                    (i < 2 ? 1.22f : rearGrip) *
                    gripMultiplier;
                wheel.sidewaysFriction = sideways;
            }

            if (body != null)
                body.angularDamping = angularDrag * (1f + stabilityUpgradeLevel * 0.12f);
        }

        private void LateUpdate()
        {
            UpdateVisualWheels();
        }

        private void UpdateVisualWheels()
        {
            for (int i = 0; i < 4; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                Transform visual = wheelVisualRoots[i];
                if (wheel == null || visual == null) continue;

                wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
                visual.position =
                    position + transform.TransformVector(visualWheelOffsets[i]);
                visual.rotation = rotation;

                Transform brakeVisual = brakeVisualRoots[i];
                if (brakeVisual != null)
                {
                    brakeVisual.position = position;
                    brakeVisual.rotation =
                        transform.rotation *
                        Quaternion.Euler(0f, wheel.steerAngle, 0f);
                }
            }
        }

        public void UseAutomaticMassProperties()
        {
            if (body == null) return;
            body.mass = vehicleMass;
            body.ResetInertiaTensor();
            body.centerOfMass = centerOfMass;
        }

        public void SetDrivingEnabled(bool enabled)
        {
            drivingEnabled = enabled;
            if (!enabled)
                ClearMotion();
        }

        public void ClearMotion()
        {
            horizontal = 0f;
            vertical = 0f;
            handbrakeInput = false;

            for (int i = 0; i < 4; i++)
            {
                steeringAngles[i] = 0f;
                sourceMotorTorque[i] = 0f;
                if (wheelColliders[i] == null) continue;
                wheelColliders[i].motorTorque = 0f;
                wheelColliders[i].brakeTorque = 0f;
                wheelColliders[i].steerAngle = 0f;
            }

            if (body == null) return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
