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

        // Port of the vehicle dynamics from the user-supplied Extreme Drift
        // VehicleControl.cs. Old Unity 5.5 input/UI/audio dependencies were
        // removed, while the drivetrain, slip, steering, COM and drift behavior
        // were retained and adapted to Unity 6 WheelCollider APIs.

        [Header("Extreme Drift Drivetrain")]
        [SerializeField] private bool frontWheelDrive = false;
        [SerializeField] private bool rearWheelDrive = true;
        [SerializeField] private float carPower = 120f;
        [SerializeField] private float shiftPower = 150f;
        [SerializeField] private float brakePower = 8000f;
        [SerializeField] private float coastBrakeTorque = 1000f;
        [SerializeField] private float idleRpm = 500f;
        [SerializeField] private float shiftDownRpm = 1500f;
        [SerializeField] private float shiftUpRpm = 2500f;
        [SerializeField] private float forwardSpeedLimit = 220f;
        [SerializeField] private float reverseSpeedLimit = 60f;
        [SerializeField] private float[] gears = { -10f, 9f, 6f, 4.5f, 3f, 2.5f };

        [Header("Extreme Drift Steering")]
        [SerializeField] private float maxSteerAngle = 25f;
        [SerializeField] private float yawAssistTorque = 10000f;

        [Header("Extreme Drift Suspension")]
        [SerializeField] private float wheelRadius = 0.4f;
        [SerializeField] private float wheelMass = 1000f;
        [SerializeField] private float suspensionDistance = 0.2f;
        [SerializeField] private float suspensionSpring = 25000f;
        [SerializeField] private float suspensionDamper = 1500f;

        [Header("Extreme Drift Tire Slip")]
        [SerializeField] private float tireStiffness = 2f;
        [SerializeField] private float minimumSlipDivisor = 0.35f;
        [SerializeField] private float airborneDownforcePerWheel = 10000f;

        [Header("Chassis")]
        [SerializeField] private float vehicleMass = 1350f;
        [SerializeField] private Vector3 baseCenterOfMass = new(0f, -0.8f, 0f);

        private readonly float[] efficiencyTable =
        {
            0.60f, 0.65f, 0.70f, 0.75f, 0.80f, 0.85f, 0.90f, 1.00f,
            1.00f, 0.95f, 0.80f, 0.70f, 0.60f, 0.50f, 0.45f, 0.40f,
            0.36f, 0.33f, 0.30f, 0.20f, 0.10f, 0.05f
        };

        private const float EfficiencyTableStep = 250f;

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

        private readonly string[] fallbackWheelNames =
        {
            "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR"
        };

        private Rigidbody body;

        private float throttleInput;
        private float steerInput;
        private bool handbrakeInput;
        private bool powerShiftInput;

        private float accel;
        private float steer;
        private bool brake;
        private bool shift;

        private float wantedRpm;
        private float motorRpm;
        private float curTorque = 100f;
        private float powerShift = 100f;
        private bool shiftMotor;
        private bool shifting;

        private int currentGear;
        private bool neutralGear;
        private bool backward;

        private float shiftTime;
        private float shiftDelay;

        // Extreme Drift's central grip state. Low values produce very high grip;
        // handbrake and collisions push it upward and reduce tire stiffness.
        private float slip;
        private float collisionSlip;
        private float lastLegacySpeed = -10f;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float ForwardSpeedKph => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
        public bool IsHandbrake => brake;
        public float RearSidewaysSlip { get; private set; }
        public float RearForwardSlip { get; private set; }
        public int GroundedWheels { get; private set; }
        public int CurrentGear => currentGear;
        public float MotorRpm => motorRpm;

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
            body.linearDamping = 0f;
            body.angularDamping = 0.05f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.centerOfMass = baseCenterOfMass;
            body.maxAngularVelocity = 12f;
            body.maxLinearVelocity = 120f;
            body.solverIterations = 12;
            body.solverVelocityIterations = 12;

            currentGear = 0;
            neutralGear = false;
            backward = false;
            slip = 0f;

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

            wheelRadius = Mathf.Clamp(measuredWheelRadius, 0.29f, 0.42f);
            wheelCenters = new Vector3[4];

            for (int i = 0; i < 4; i++)
            {
                wheelVisualRoots[i] = visualRoots[i];
                brakeVisualRoots[i] = brakeRoots != null && brakeRoots.Length > i ? brakeRoots[i] : null;
                wheelCenters[i] = centers[i];
            }

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
                wheelTransform.localPosition = wheelCenters[i];

                wheel.center = Vector3.zero;
                wheel.radius = wheelRadius;
                wheel.mass = wheelMass;
                wheel.suspensionDistance = suspensionDistance;
                wheel.wheelDampingRate = 0.25f;
                wheel.forceAppPointDistance = 0f;

                JointSpring spring = wheel.suspensionSpring;
                spring.spring = suspensionSpring;
                spring.damper = suspensionDamper;
                spring.targetPosition = 0.5f;
                wheel.suspensionSpring = spring;

                ConfigureBaseFriction(wheel);
            }

            wheelColliders[FrontLeft].ConfigureVehicleSubsteps(5f, 8, 12);
        }

        private void ConfigureBaseFriction(WheelCollider wheel)
        {
            WheelFrictionCurve forward = wheel.forwardFriction;
            forward.extremumSlip = 0.40f;
            forward.extremumValue = 1f;
            forward.asymptoteSlip = 1.4f;
            forward.asymptoteValue = 0.75f;
            forward.stiffness = tireStiffness;
            wheel.forwardFriction = forward;

            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            sideways.extremumSlip = 0.3f;
            sideways.extremumValue = 1f;
            sideways.asymptoteSlip = 2f;
            sideways.asymptoteValue = 0.72f;
            sideways.stiffness = tireStiffness;
            wheel.sidewaysFriction = sideways;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            float keyboardThrottle = 0f;
            float keyboardSteer = 0f;
            bool keyboardBrake = false;
            bool keyboardShift = false;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardThrottle += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardThrottle -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardSteer -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardSteer += 1f;
                keyboardBrake = keyboard.spaceKey.isPressed;
                keyboardShift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }

            float gamepadThrottle = 0f;
            float gamepadSteer = 0f;
            bool gamepadBrake = false;
            bool gamepadShift = false;

            if (gamepad != null)
            {
                gamepadThrottle = gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue();
                gamepadSteer = gamepad.leftStick.x.ReadValue();
                gamepadBrake = gamepad.buttonSouth.isPressed;
                gamepadShift = gamepad.rightShoulder.isPressed;
            }

            throttleInput = Mathf.Abs(keyboardThrottle) >= Mathf.Abs(gamepadThrottle) ? keyboardThrottle : gamepadThrottle;
            steerInput = Mathf.Abs(keyboardSteer) >= Mathf.Abs(gamepadSteer) ? keyboardSteer : gamepadSteer;
            handbrakeInput = keyboardBrake || gamepadBrake;
            powerShiftInput = keyboardShift || gamepadShift;
        }

        private void FixedUpdate()
        {
            if (wheelColliders[0] == null) return;

            float legacySpeed = body.linearVelocity.magnitude * 2.7f;

            if (legacySpeed < lastLegacySpeed - 10f && slip < 10f)
                slip = lastLegacySpeed / 15f;

            lastLegacySpeed = legacySpeed;
            if (collisionSlip != 0f)
                collisionSlip = Mathf.MoveTowards(collisionSlip, 0f, 0.1f);

            accel = throttleInput;
            steer = steerInput;
            brake = handbrakeInput;
            shift = powerShiftInput;

            HandleAutomaticGearbox(legacySpeed);
            UpdateCenterOfMass(legacySpeed);
            wantedRpm = 5500f * accel * 0.1f + wantedRpm * 0.9f;

            float yawDirection = currentGear > 0 ? 1f : -1f;
            if (GroundedWheels > 0)
                body.AddRelativeTorque(0f, steer * yawAssistTorque * yawDirection, 0f, ForceMode.Force);

            UpdatePowerShift(legacySpeed);

            float rpm = 0f;
            int drivenWheelCount = 0;
            GroundedWheels = 0;
            float rearSideways = 0f;
            float rearForward = 0f;
            int rearGrounded = 0;

            for (int i = 0; i < 4; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                bool driven = IsDrivenWheel(i);
                bool rear = i >= RearLeft;

                if (driven)
                {
                    if (!neutralGear && brake && currentGear < 2)
                        rpm += accel * idleRpm;
                    else if (!neutralGear)
                        rpm += wheel.rpm;
                    else
                        rpm += idleRpm * accel;

                    drivenWheelCount++;
                }

                UpdateBrakeAndSlip(wheel, rear, legacySpeed);
                UpdateDynamicFriction(wheel);

                if (wheel.GetGroundHit(out WheelHit hit))
                {
                    GroundedWheels++;

                    if (rear)
                    {
                        rearSideways += Mathf.Abs(hit.sidewaysSlip);
                        rearForward += Mathf.Abs(hit.forwardSlip);
                        rearGrounded++;
                    }
                }
                else
                {
                    body.AddForce(Vector3.down * airborneDownforcePerWheel, ForceMode.Force);
                }
            }

            RearSidewaysSlip = rearGrounded > 0 ? rearSideways / rearGrounded : 0f;
            RearForwardSlip = rearGrounded > 0 ? rearForward / rearGrounded : 0f;

            if (drivenWheelCount > 1)
                rpm /= drivenWheelCount;

            motorRpm = 0.95f * motorRpm + 0.05f * Mathf.Abs(rpm * gears[Mathf.Clamp(currentGear, 0, gears.Length - 1)]);
            if (motorRpm > 5500f) motorRpm = 5200f;

            int efficiencyIndex = Mathf.Clamp((int)(motorRpm / EfficiencyTableStep), 0, efficiencyTable.Length - 1);
            float newTorque = curTorque * gears[Mathf.Clamp(currentGear, 0, gears.Length - 1)] * efficiencyTable[efficiencyIndex];

            ApplyMotorTorque(newTorque, legacySpeed);
            ApplySteering(legacySpeed);

            shiftTime = Mathf.MoveTowards(shiftTime, 0f, 0.1f);
        }

        private void HandleAutomaticGearbox(float legacySpeed)
        {
            if (currentGear == 1 && accel < 0f)
            {
                if (legacySpeed < 5f) ShiftDown();
            }
            else if (currentGear == 0 && accel > 0f)
            {
                if (legacySpeed < 5f) ShiftUp();
            }
            else if (motorRpm > shiftUpRpm && accel > 0f && legacySpeed > 10f && !brake)
            {
                ShiftUp();
            }
            else if (motorRpm < shiftDownRpm && currentGear > 1)
            {
                ShiftDown();
            }

            if (legacySpeed < 1f)
                backward = true;

            if (currentGear == 0 && backward)
            {
                if (legacySpeed < gears[0] * -10f)
                    accel = -accel;
            }
            else
            {
                backward = false;
            }
        }

        private void ShiftUp()
        {
            float now = Time.timeSinceLevelLoad;
            if (now < shiftDelay || currentGear >= gears.Length - 1) return;

            currentGear++;
            shiftDelay = now + 1f;
            shiftTime = 1.5f;
        }

        private void ShiftDown()
        {
            float now = Time.timeSinceLevelLoad;
            if (now < shiftDelay || currentGear <= 0) return;

            currentGear--;
            shiftDelay = now + 0.1f;
            shiftTime = 2f;
        }

        private void UpdateCenterOfMass(float legacySpeed)
        {
            Vector3 center = baseCenterOfMass;

            if (currentGear == 0 && backward)
                center.z = -accel / -5f;
            else if (currentGear > 0)
                center.z = -(accel / currentGear) / -5f;

            center.x = -Mathf.Clamp(steer * (legacySpeed / 100f), -0.03f, 0.03f);
            body.centerOfMass = center;
        }

        private void UpdateBrakeAndSlip(WheelCollider wheel, bool rear, float legacySpeed)
        {
            if (brake || accel < 0f)
            {
                if (accel < 0f || (brake && rear))
                {
                    if (brake && accel > 0f)
                    {
                        slip = Mathf.Lerp(slip, 5f, Mathf.Clamp01(accel * 0.01f));
                    }
                    else if (legacySpeed > 1f)
                    {
                        slip = Mathf.Lerp(slip, 1f, 0.002f);
                    }
                    else
                    {
                        slip = Mathf.Lerp(slip, 1f, 0.02f);
                    }

                    wantedRpm = 0f;
                    wheel.brakeTorque = brakePower;
                }
                else
                {
                    wheel.brakeTorque = 0f;
                }
            }
            else
            {
                wheel.brakeTorque = accel == 0f || neutralGear ? coastBrakeTorque : 0f;

                if (legacySpeed > 0f)
                {
                    slip = legacySpeed > 100f
                        ? Mathf.Lerp(slip, 1f + Mathf.Abs(steer), 0.02f)
                        : Mathf.Lerp(slip, 1.5f, 0.02f);
                }
                else
                {
                    slip = Mathf.Lerp(slip, 0.01f, 0.02f);
                }
            }
        }

        private void UpdateDynamicFriction(WheelCollider wheel)
        {
            float divisor = Mathf.Max(minimumSlipDivisor, slip + collisionSlip);
            float dynamicStiffness = tireStiffness / divisor;

            WheelFrictionCurve forward = wheel.forwardFriction;
            forward.stiffness = dynamicStiffness;
            wheel.forwardFriction = forward;

            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            sideways.stiffness = dynamicStiffness;
            sideways.extremumSlip = 0.3f + Mathf.Abs(steer);
            wheel.sidewaysFriction = sideways;
        }

        private void UpdatePowerShift(float legacySpeed)
        {
            if (shift && currentGear > 1 && legacySpeed > 50f && shiftMotor)
            {
                if (powerShift == 0f)
                    shiftMotor = false;

                powerShift = Mathf.MoveTowards(powerShift, 0f, Time.fixedDeltaTime * 40f);
                shifting = true;
                curTorque = powerShift > 0f ? shiftPower : carPower;
            }
            else
            {
                if (powerShift > 20f)
                    shiftMotor = true;

                shifting = false;
                powerShift = Mathf.MoveTowards(powerShift, 100f, Time.fixedDeltaTime * 20f);
                curTorque = carPower;
            }
        }

        private bool IsDrivenWheel(int index)
        {
            return index < RearLeft ? frontWheelDrive : rearWheelDrive;
        }

        private void ApplyMotorTorque(float newTorque, float legacySpeed)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!IsDrivenWheel(i))
                {
                    wheelColliders[i].motorTorque = 0f;
                    continue;
                }

                WheelCollider wheel = wheelColliders[i];

                if (Mathf.Abs(wheel.rpm) > Mathf.Abs(wantedRpm))
                {
                    wheel.motorTorque = 0f;
                    continue;
                }

                if (!brake && accel != 0f && !neutralGear)
                {
                    bool withinLimit =
                        (legacySpeed < forwardSpeedLimit && currentGear > 0) ||
                        (legacySpeed < reverseSpeedLimit && currentGear == 0);

                    if (withinLimit)
                    {
                        float current = wheel.motorTorque;
                        wheel.motorTorque = current * 0.9f + newTorque;
                    }
                    else
                    {
                        wheel.motorTorque = 0f;
                        wheel.brakeTorque = 0f;
                    }
                }
                else
                {
                    wheel.motorTorque = 0f;
                }
            }
        }

        private void ApplySteering(float legacySpeed)
        {
            for (int i = 0; i < 2; i++)
            {
                WheelCollider wheel = wheelColliders[i];

                if (brake || collisionSlip > 2f)
                {
                    wheel.steerAngle = Mathf.Lerp(wheel.steerAngle, steer * maxSteerAngle, 0.05f);
                }
                else
                {
                    float steerDivisor = Mathf.Clamp(
                        legacySpeed / Mathf.Max(0.01f, maxSteerAngle),
                        1f,
                        maxSteerAngle);

                    wheel.steerAngle = steer * (maxSteerAngle / steerDivisor);
                }
            }

            wheelColliders[RearLeft].steerAngle = 0f;
            wheelColliders[RearRight].steerAngle = 0f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            ArcadeCarController other = collision.transform.root.GetComponent<ArcadeCarController>();
            if (other == null || other == this) return;

            collisionSlip = Mathf.Clamp(collision.relativeVelocity.magnitude, 0f, 10f);
            body.angularVelocity = new Vector3(
                -body.angularVelocity.x * 0.5f,
                 body.angularVelocity.y * 0.5f,
                -body.angularVelocity.z * 0.5f);

            Vector3 velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(velocity.x, velocity.y * 0.5f, velocity.z);
        }

        private void OnCollisionStay(Collision collision)
        {
            ArcadeCarController other = collision.transform.root.GetComponent<ArcadeCarController>();
            if (other != null && other != this)
                collisionSlip = 5f;
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
