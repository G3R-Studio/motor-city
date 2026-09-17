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

        [Header("Engine")]
        [SerializeField] private float maxMotorTorque = 1150f;
        [SerializeField] private float reverseTorqueFactor = 0.62f;
        [SerializeField] private float maxForwardSpeedKph = 205f;
        [SerializeField] private float maxReverseSpeedKph = 55f;
        [SerializeField] private float serviceBrakeTorque = 3600f;
        [SerializeField] private float handbrakeTorque = 4800f;
        [SerializeField] private float coastBrakeTorque = 45f;

        [Header("Steering")]
        [SerializeField] private float lowSpeedSteerAngle = 32f;
        [SerializeField] private float highSpeedSteerAngle = 10f;
        [SerializeField] private float steeringResponse = 145f;
        [SerializeField] private float steeringReturnResponse = 205f;

        [Header("Wheel Collider Suspension")]
        [SerializeField] private float wheelRadius = 0.34f;
        [SerializeField] private float wheelMass = 27f;
        [SerializeField] private float suspensionDistance = 0.18f;
        [SerializeField] private float suspensionSpring = 36000f;
        [SerializeField] private float suspensionDamper = 5200f;
        [SerializeField, Range(0f, 1f)] private float suspensionTargetPosition = 0.5f;
        [SerializeField] private float wheelDampingRate = 0.45f;
        [SerializeField] private float forceAppPointDistance = 0.12f;
        [SerializeField] private float antiRollForce = 6500f;

        [Header("Tire Grip")]
        [SerializeField] private float frontSidewaysStiffness = 1.72f;
        [SerializeField] private float rearSidewaysStiffness = 1.58f;
        [SerializeField] private float rearHandbrakeStiffness = 0.58f;
        [SerializeField] private float forwardStiffness = 1.32f;
        [SerializeField] private float tractionSlipThreshold = 0.38f;

        [Header("Chassis")]
        [SerializeField] private float vehicleMass = 1350f;
        [SerializeField] private Vector3 centerOfMass = new(0f, 0.13f, 0.05f);
        [SerializeField] private float aerodynamicDownforce = 2.15f;

        private readonly WheelCollider[] wheelColliders = new WheelCollider[4];
        private readonly Transform[] wheelVisualRoots = new Transform[4];
        private readonly Transform[] brakeVisualRoots = new Transform[4];
        private Vector3[] wheelCenters =
        {
            new(-0.94f, 0.18f, 1.32f),
            new( 0.94f, 0.18f, 1.32f),
            new(-0.94f, 0.18f,-1.34f),
            new( 0.94f, 0.18f,-1.34f)
        };

        private readonly string[] fallbackWheelNames = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };
        private Rigidbody body;
        private float throttleInput;
        private float steerInput;
        private bool handbrakeInput;
        private float currentSteerAngle;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float ForwardSpeedKph => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
        public bool IsHandbrake => handbrakeInput;
        public int GroundedWheels { get; private set; }

        public float SlipAngleDegrees
        {
            get
            {
                if (body == null || body.linearVelocity.sqrMagnitude < 1f) return 0f;
                Vector3 localVelocity = transform.InverseTransformDirection(body.linearVelocity);
                return Mathf.Atan2(localVelocity.x, Mathf.Max(0.1f, Mathf.Abs(localVelocity.z))) * Mathf.Rad2Deg;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = vehicleMass;
            body.linearDamping = 0.018f;
            body.angularDamping = 0.72f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.centerOfMass = centerOfMass;
            body.maxLinearVelocity = maxForwardSpeedKph / 3.6f + 15f;
            body.solverIterations = 12;
            body.solverVelocityIterations = 12;

            SetupFallbackWheelVisuals();
            BuildOrReconfigureWheelColliders();
        }

        public void ConfigureExternalWheelRig(
            Transform[] visualRoots,
            Transform[] brakeRoots,
            Vector3[] centers,
            float measuredWheelRadius)
        {
            if (visualRoots == null || centers == null || visualRoots.Length < 4 || centers.Length < 4) return;

            wheelRadius = Mathf.Clamp(measuredWheelRadius, 0.29f, 0.40f);
            wheelCenters = new Vector3[4];

            for (int i = 0; i < 4; i++)
            {
                wheelVisualRoots[i] = visualRoots[i];
                brakeVisualRoots[i] = brakeRoots != null && brakeRoots.Length > i ? brakeRoots[i] : null;
                wheelCenters[i] = centers[i];
            }

            body.centerOfMass = centerOfMass;
            BuildOrReconfigureWheelColliders();
        }

        private void SetupFallbackWheelVisuals()
        {
            for (int i = 0; i < 4; i++)
            {
                Transform mesh = transform.Find(fallbackWheelNames[i]);
                if (mesh == null) continue;

                Vector3 localCenter = mesh.localPosition;
                GameObject pivotObject = new($"FallbackWheelVisual_{i}");
                Transform pivot = pivotObject.transform;
                pivot.SetParent(transform, false);
                pivot.localPosition = localCenter;
                pivot.localRotation = Quaternion.identity;

                mesh.SetParent(pivot, true);
                wheelVisualRoots[i] = pivot;
                wheelCenters[i] = localCenter;
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
                Transform wheelTransform = wheel.transform;
                wheelTransform.localRotation = Quaternion.identity;
                wheelTransform.localScale = Vector3.one;

                // Unity's WheelCollider suspension extends downward from its Transform.
                // With a 0.5 target position the transform therefore sits half a travel
                // above the visual wheel centre at rest.
                Vector3 center = wheelCenters[i];
                wheelTransform.localPosition = new Vector3(
                    center.x,
                    center.y + suspensionDistance * 0.5f,
                    center.z);

                wheel.center = Vector3.zero;
                wheel.mass = wheelMass;
                wheel.radius = wheelRadius;
                wheel.wheelDampingRate = wheelDampingRate;
                wheel.suspensionDistance = suspensionDistance;
                wheel.forceAppPointDistance = forceAppPointDistance;

                JointSpring spring = wheel.suspensionSpring;
                spring.spring = suspensionSpring;
                spring.damper = suspensionDamper;
                spring.targetPosition = suspensionTargetPosition;
                wheel.suspensionSpring = spring;

                ConfigureForwardFriction(wheel);
                ConfigureSidewaysFriction(wheel, i < 2 ? frontSidewaysStiffness : rearSidewaysStiffness);
            }

            wheelColliders[FrontLeft].ConfigureVehicleSubsteps(5f, 8, 12);
        }

        private void ConfigureForwardFriction(WheelCollider wheel)
        {
            WheelFrictionCurve friction = wheel.forwardFriction;
            friction.extremumSlip = 0.34f;
            friction.extremumValue = 1f;
            friction.asymptoteSlip = 0.82f;
            friction.asymptoteValue = 0.78f;
            friction.stiffness = forwardStiffness;
            wheel.forwardFriction = friction;
        }

        private static void ConfigureSidewaysFriction(WheelCollider wheel, float stiffness)
        {
            WheelFrictionCurve friction = wheel.sidewaysFriction;
            friction.extremumSlip = 0.22f;
            friction.extremumValue = 1f;
            friction.asymptoteSlip = 0.55f;
            friction.asymptoteValue = 0.78f;
            friction.stiffness = stiffness;
            wheel.sidewaysFriction = friction;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            float keyboardThrottle = 0f;
            float keyboardSteer = 0f;
            bool keyboardHandbrake = false;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardThrottle += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardThrottle -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardSteer -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardSteer += 1f;
                keyboardHandbrake = keyboard.spaceKey.isPressed;
            }

            float gamepadThrottle = 0f;
            float gamepadSteer = 0f;
            bool gamepadHandbrake = false;
            if (gamepad != null)
            {
                gamepadThrottle = gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue();
                gamepadSteer = gamepad.leftStick.x.ReadValue();
                gamepadHandbrake = gamepad.buttonSouth.isPressed;
            }

            throttleInput = Mathf.Abs(keyboardThrottle) >= Mathf.Abs(gamepadThrottle) ? keyboardThrottle : gamepadThrottle;
            steerInput = Mathf.Abs(keyboardSteer) >= Mathf.Abs(gamepadSteer) ? keyboardSteer : gamepadSteer;
            handbrakeInput = keyboardHandbrake || gamepadHandbrake;
        }

        private void FixedUpdate()
        {
            if (wheelColliders[0] == null) return;

            UpdateGroundedState();

            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            float speedKph = Mathf.Abs(forwardSpeed) * 3.6f;

            ApplySteering(speedKph);
            ApplyDriveAndBrakes(forwardSpeed);
            ApplyRearGrip();
            ApplyAntiRoll(FrontLeft, FrontRight);
            ApplyAntiRoll(RearLeft, RearRight);
            ApplyDownforce();
        }

        private void UpdateGroundedState()
        {
            GroundedWheels = 0;
            for (int i = 0; i < 4; i++)
                if (wheelColliders[i] != null && wheelColliders[i].isGrounded) GroundedWheels++;
        }

        private void ApplySteering(float speedKph)
        {
            float speedFactor = Mathf.InverseLerp(35f, 190f, speedKph);
            float steeringLimit = Mathf.Lerp(lowSpeedSteerAngle, highSpeedSteerAngle, speedFactor);
            float desiredAngle = steerInput * steeringLimit;
            float response = Mathf.Abs(steerInput) > 0.01f ? steeringResponse : steeringReturnResponse;
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, desiredAngle, response * Time.fixedDeltaTime);

            float wheelbase = Mathf.Abs((wheelCenters[FrontLeft].z + wheelCenters[FrontRight].z) * 0.5f -
                                        (wheelCenters[RearLeft].z + wheelCenters[RearRight].z) * 0.5f);
            float track = Mathf.Abs(wheelCenters[FrontRight].x - wheelCenters[FrontLeft].x);

            if (Mathf.Abs(currentSteerAngle) < 0.01f || wheelbase < 0.1f || track < 0.1f)
            {
                wheelColliders[FrontLeft].steerAngle = currentSteerAngle;
                wheelColliders[FrontRight].steerAngle = currentSteerAngle;
                return;
            }

            float absAngle = Mathf.Abs(currentSteerAngle) * Mathf.Deg2Rad;
            float turnRadius = wheelbase / Mathf.Max(0.01f, Mathf.Tan(absAngle));
            float innerRadius = Mathf.Max(0.2f, turnRadius - track * 0.5f);
            float outerRadius = turnRadius + track * 0.5f;
            float innerAngle = Mathf.Atan(wheelbase / innerRadius) * Mathf.Rad2Deg;
            float outerAngle = Mathf.Atan(wheelbase / outerRadius) * Mathf.Rad2Deg;

            if (currentSteerAngle > 0f)
            {
                wheelColliders[FrontLeft].steerAngle = outerAngle;
                wheelColliders[FrontRight].steerAngle = innerAngle;
            }
            else
            {
                wheelColliders[FrontLeft].steerAngle = -innerAngle;
                wheelColliders[FrontRight].steerAngle = -outerAngle;
            }
        }

        private void ApplyDriveAndBrakes(float forwardSpeed)
        {
            for (int i = 0; i < 4; i++)
            {
                wheelColliders[i].motorTorque = 0f;
                wheelColliders[i].brakeTorque = Mathf.Abs(throttleInput) < 0.01f ? coastBrakeTorque : 0f;
            }

            bool hasThrottle = Mathf.Abs(throttleInput) > 0.01f;
            bool moving = Mathf.Abs(forwardSpeed) > 0.65f;
            bool changingDirection = hasThrottle && moving && Mathf.Sign(throttleInput) != Mathf.Sign(forwardSpeed);

            if (changingDirection)
            {
                float brake = serviceBrakeTorque * Mathf.Abs(throttleInput);
                wheelColliders[FrontLeft].brakeTorque = brake;
                wheelColliders[FrontRight].brakeTorque = brake;
                wheelColliders[RearLeft].brakeTorque = brake * 0.82f;
                wheelColliders[RearRight].brakeTorque = brake * 0.82f;
            }
            else if (hasThrottle && !handbrakeInput)
            {
                float limit = throttleInput >= 0f ? maxForwardSpeedKph : maxReverseSpeedKph;
                float speedRatio = Mathf.Clamp01(Mathf.Abs(forwardSpeed) * 3.6f / Mathf.Max(1f, limit));
                float torqueFalloff = 1f - Mathf.Pow(speedRatio, 1.45f);
                float torque = maxMotorTorque * throttleInput * torqueFalloff;
                if (throttleInput < 0f) torque *= reverseTorqueFactor;

                wheelColliders[RearLeft].motorTorque = torque * TractionFactor(wheelColliders[RearLeft]);
                wheelColliders[RearRight].motorTorque = torque * TractionFactor(wheelColliders[RearRight]);
            }

            if (handbrakeInput)
            {
                wheelColliders[RearLeft].motorTorque = 0f;
                wheelColliders[RearRight].motorTorque = 0f;
                wheelColliders[RearLeft].brakeTorque = Mathf.Max(wheelColliders[RearLeft].brakeTorque, handbrakeTorque);
                wheelColliders[RearRight].brakeTorque = Mathf.Max(wheelColliders[RearRight].brakeTorque, handbrakeTorque);
            }
        }

        private float TractionFactor(WheelCollider wheel)
        {
            if (handbrakeInput || !wheel.GetGroundHit(out WheelHit hit)) return 1f;

            float slip = Mathf.Abs(hit.forwardSlip);
            if (slip <= tractionSlipThreshold) return 1f;

            return Mathf.Clamp(1f - (slip - tractionSlipThreshold) * 1.35f, 0.38f, 1f);
        }

        private void ApplyRearGrip()
        {
            float stiffness = handbrakeInput ? rearHandbrakeStiffness : rearSidewaysStiffness;
            SetSidewaysStiffness(wheelColliders[RearLeft], stiffness);
            SetSidewaysStiffness(wheelColliders[RearRight], stiffness);
        }

        private static void SetSidewaysStiffness(WheelCollider wheel, float stiffness)
        {
            WheelFrictionCurve friction = wheel.sidewaysFriction;
            if (Mathf.Abs(friction.stiffness - stiffness) < 0.001f) return;
            friction.stiffness = stiffness;
            wheel.sidewaysFriction = friction;
        }

        private void ApplyAntiRoll(int leftIndex, int rightIndex)
        {
            WheelCollider left = wheelColliders[leftIndex];
            WheelCollider right = wheelColliders[rightIndex];

            float leftCompression = SuspensionCompression(left, out bool leftGrounded);
            float rightCompression = SuspensionCompression(right, out bool rightGrounded);
            float force = (leftCompression - rightCompression) * antiRollForce;

            if (leftGrounded)
                body.AddForceAtPosition(left.transform.up * force, left.transform.position, ForceMode.Force);
            if (rightGrounded)
                body.AddForceAtPosition(-right.transform.up * force, right.transform.position, ForceMode.Force);
        }

        private static float SuspensionCompression(WheelCollider wheel, out bool grounded)
        {
            grounded = wheel.GetGroundHit(out WheelHit hit);
            if (!grounded || wheel.suspensionDistance <= 0.001f) return 0f;

            Vector3 localHit = wheel.transform.InverseTransformPoint(hit.point);
            float suspensionLength = Mathf.Max(0f, -localHit.y - wheel.radius);
            return 1f - Mathf.Clamp01(suspensionLength / wheel.suspensionDistance);
        }

        private void ApplyDownforce()
        {
            if (GroundedWheels == 0) return;
            body.AddForce(-transform.up * body.linearVelocity.sqrMagnitude * aerodynamicDownforce, ForceMode.Force);
        }

        private void LateUpdate()
        {
            for (int i = 0; i < 4; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                Transform visual = wheelVisualRoots[i];
                if (wheel == null || visual == null) continue;

                wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
                visual.position = position;
                visual.rotation = rotation;

                Transform brakeVisual = brakeVisualRoots[i];
                if (brakeVisual != null)
                {
                    brakeVisual.position = position;
                    brakeVisual.rotation = transform.rotation * Quaternion.Euler(0f, wheel.steerAngle, 0f);
                }
            }
        }
    }
}
