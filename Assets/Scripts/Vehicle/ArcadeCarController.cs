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

        [Header("Pro Drift Controller v1")]
        [SerializeField] private float brakePower = 10f;
        [SerializeField] private float wheelRotateSpeed = 20f;
        [SerializeField] private float wheelSteeringAngle = 40f;
        [SerializeField] private float wheelAcceleration = 30f;
        [SerializeField] private float wheelMaxSpeed = 2000f;

        [Header("Prefab Rigidbody")]
        [SerializeField] private float vehicleMass = 2400f;
        [SerializeField] private float angularDrag = 0.32f;
        [SerializeField] private float reverseDrag = 0.3f;
        [SerializeField] private Vector3 centerOfMass = new(0f, 0.42f, 0.08f);
        [SerializeField] private float antiRollForce = 4600f;
        [SerializeField] private float physicsHalfTrack = 1.05f;
        [SerializeField] private float physicsHalfWheelbase = 1.72f;

        [Header("Prefab WheelCollider")]
        [SerializeField] private float wheelRadius = 0.5f;
        [SerializeField] private float wheelMass = 20f;
        [SerializeField] private float suspensionDistance = 0.34f;
        [SerializeField] private float suspensionSpring = 56000f;
        [SerializeField] private float suspensionDamper = 3200f;
        [SerializeField] private float suspensionTargetPosition = 0.5f;
        [SerializeField] private float forceAppPointDistance = 0.02f;
        [SerializeField] private float wheelDampingRate = 0.25f;

        [Header("Handbrake")]
        [SerializeField] private float handbrakeRearTorque = 18000f;
        [SerializeField] private float handbrakeDeceleration = 8.5f;
        [SerializeField] private float parkingBrakeTorque = 30000f;
        [SerializeField] private float parkingBrakeSpeedKph = 8f;

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
                forward.extremumSlip = 0.4f;
                forward.extremumValue = 1f;
                forward.asymptoteSlip = 0.8f;
                forward.asymptoteValue = 0.5f;
                forward.stiffness = 1f;
                wheel.forwardFriction = forward;

                WheelFrictionCurve sideways = wheel.sidewaysFriction;
                sideways.extremumSlip = 0.8f;
                sideways.extremumValue = 1f;
                sideways.asymptoteSlip = 0.5f;
                sideways.asymptoteValue = 0.75f;
                float gripMultiplier = 1f + gripUpgradeLevel * 0.10f;
                sideways.stiffness = (i < 2 ? 1.16f : 1.10f) * gripMultiplier;
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
            ApplySourceController();
            ApplyHandbrake();

            GroundedWheels = 0;
            float rearSideways = 0f;
            float rearForward = 0f;
            int rearGrounded = 0;

            for (int i = 0; i < 4; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                if (wheel == null || !wheel.GetGroundHit(out WheelHit hit)) continue;

                GroundedWheels++;

                if (i >= RearLeft)
                {
                    rearSideways += Mathf.Abs(hit.sidewaysSlip);
                    rearForward += Mathf.Abs(hit.forwardSlip);
                    rearGrounded++;
                }
            }

            RearSidewaysSlip = rearGrounded > 0 ? rearSideways / rearGrounded : 0f;
            RearForwardSlip = rearGrounded > 0 ? rearForward / rearGrounded : 0f;

            ApplyAntiRoll(FrontLeft, FrontRight);
            ApplyAntiRoll(RearLeft, RearRight);
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

        private void ApplySourceController()
        {
            for (int i = 0; i < 4; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                if (wheel == null) continue;

                // WheelController.cs: return steering to zero every frame.
                steeringAngles[i] =
                    Mathf.LerpAngle(
                        steeringAngles[i],
                        0f,
                        SourceLerpFactor(wheelRotateSpeed));

                // Preserve WheelController.cs torque math in its original coordinate
                // convention, then invert once for Motor City's +Z forward axis.
                sourceMotorTorque[i] =
                    -Mathf.Lerp(
                        sourceMotorTorque[i],
                        0f,
                        SourceLerpFactor(wheelAcceleration));

                if (vertical > 0.1f)
                {
                    sourceMotorTorque[i] =
                        -Mathf.Lerp(
                            sourceMotorTorque[i],
                            wheelMaxSpeed,
                            SourceLerpFactor(wheelAcceleration));
                }

                if (vertical < -0.1f)
                {
                    sourceMotorTorque[i] =
                        Mathf.Lerp(
                            sourceMotorTorque[i],
                            wheelMaxSpeed,
                            SourceLerpFactor(wheelAcceleration * brakePower));
                }

                wheel.motorTorque = -sourceMotorTorque[i];

                if (horizontal > 0.1f)
                {
                    steeringAngles[i] =
                        Mathf.LerpAngle(
                            steeringAngles[i],
                            wheelSteeringAngle,
                            SourceLerpFactor(wheelRotateSpeed));
                }

                if (horizontal < -0.1f)
                {
                    steeringAngles[i] =
                        Mathf.LerpAngle(
                            steeringAngles[i],
                            -wheelSteeringAngle,
                            SourceLerpFactor(wheelRotateSpeed));
                }

                // In the source prefab all four wheels receive motor torque,
                // while only the front WheelAlignment components are steerable.
                wheel.steerAngle = i < 2 ? steeringAngles[i] : 0f;
            }

            body.linearDamping = vertical < -0.1f ? reverseDrag : 0f;
            body.angularDamping = angularDrag * (1f + stabilityUpgradeLevel * 0.12f);
        }


        private void ApplyHandbrake()
        {
            bool parkingMode = SpeedKph <= parkingBrakeSpeedKph;

            for (int i = 0; i < wheelColliders.Length; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                if (wheel == null) continue;

                if (!handbrakeInput)
                {
                    wheel.brakeTorque = 0f;
                    continue;
                }

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

            wheelMaxSpeed = 2000f * (1f + engineUpgradeLevel * 0.12f);
            wheelAcceleration = 30f * (1f + engineUpgradeLevel * 0.10f);

            for (int i = 0; i < wheelColliders.Length; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                if (wheel == null) continue;

                WheelFrictionCurve sideways = wheel.sidewaysFriction;
                float gripMultiplier = 1f + gripUpgradeLevel * 0.10f;
                sideways.stiffness = (i < 2 ? 1.16f : 1.10f) * gripMultiplier;
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
            body.ResetCenterOfMass();
            body.ResetInertiaTensor();
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
