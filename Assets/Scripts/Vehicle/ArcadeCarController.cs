using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        [Header("Power")]
        [SerializeField] private float acceleration = 11.5f;
        [SerializeField] private float reverseAcceleration = 7.5f;
        [SerializeField] private float maxForwardSpeedKph = 205f;
        [SerializeField] private float maxReverseSpeedKph = 55f;
        [SerializeField] private float serviceBrakeAcceleration = 18f;

        [Header("Suspension")]
        [SerializeField] private float wheelRadius = 0.34f;
        [SerializeField] private float suspensionRestLength = 0.22f;
        [SerializeField] private float suspensionMinLength = 0.10f;
        [SerializeField] private float suspensionMaxLength = 0.32f;
        [SerializeField] private float springRate = 62000f;
        [SerializeField] private float damperRate = 7200f;
        [SerializeField] private float bumpStopRate = 90000f;
        [SerializeField] private float antiRollRate = 12000f;

        [Header("Tires")]
        [SerializeField] private float frontGrip = 1.18f;
        [SerializeField] private float rearGrip = 1.08f;
        [SerializeField] private float handbrakeRearGrip = 0.46f;
        [SerializeField] private float lateralDamping = 2400f;
        [SerializeField] private float rollingResistance = 0.12f;
        [SerializeField] private float steeringDegrees = 30f;
        [SerializeField] private float highSpeedSteeringDegrees = 10f;

        [Header("Chassis")]
        [SerializeField] private float downforce = 0.45f;
        [SerializeField] private float yawDamping = 0.35f;

        private Vector3[] suspensionHardpoints =
        {
            new(-0.82f, 0.56f, 1.34f),
            new( 0.82f, 0.56f, 1.34f),
            new(-0.82f, 0.56f,-1.34f),
            new( 0.82f, 0.56f,-1.34f)
        };

        private readonly string[] fallbackWheelNames = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };
        private readonly bool[] grounded = new bool[4];
        private readonly float[] springLength = new float[4];
        private readonly float[] wheelLoad = new float[4];
        private readonly RaycastHit[] hits = new RaycastHit[4];
        private readonly Transform[] wheelCarriers = new Transform[4];
        private readonly Transform[] wheelSpinPivots = new Transform[4];
        private readonly float[] wheelSpin = new float[4];

        private Rigidbody body;
        private float throttleInput;
        private float steerInput;
        private bool handbrakeInput;
        private float currentSteerAngle;
        private bool externalWheelRig;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float ForwardSpeedKph => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
        public bool IsHandbrake => handbrakeInput;
        public int GroundedWheels { get; private set; }

        public float SlipAngleDegrees
        {
            get
            {
                if (body == null || body.linearVelocity.sqrMagnitude < 1f) return 0f;
                Vector3 local = transform.InverseTransformDirection(body.linearVelocity);
                return Mathf.Atan2(local.x, Mathf.Abs(local.z)) * Mathf.Rad2Deg;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = 1350f;
            body.linearDamping = 0.01f;
            body.angularDamping = 0.8f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.centerOfMass = new Vector3(0f, 0.24f, 0.04f);
            body.maxLinearVelocity = maxForwardSpeedKph / 3.6f + 15f;

            CacheFallbackWheels();
            for (int i = 0; i < 4; i++) springLength[i] = suspensionMaxLength;
        }

        public void ConfigureExternalWheelRig(Transform[] carriers, Transform[] spinPivots, Vector3[] wheelCenters, float measuredWheelRadius)
        {
            if (carriers == null || spinPivots == null || wheelCenters == null ||
                carriers.Length < 4 || spinPivots.Length < 4 || wheelCenters.Length < 4)
                return;

            externalWheelRig = true;
            suspensionHardpoints = new Vector3[4];
            wheelRadius = Mathf.Clamp(measuredWheelRadius, 0.28f, 0.40f);

            for (int i = 0; i < 4; i++)
            {
                wheelCarriers[i] = carriers[i];
                wheelSpinPivots[i] = spinPivots[i];
                suspensionHardpoints[i] = new Vector3(wheelCenters[i].x, 0.56f, wheelCenters[i].z);
                springLength[i] = suspensionRestLength;
                wheelSpin[i] = 0f;
            }

            suspensionRestLength = 0.22f;
            suspensionMinLength = 0.10f;
            suspensionMaxLength = 0.31f;
            springRate = 62000f;
            damperRate = 7600f;
            bumpStopRate = 95000f;
            antiRollRate = 12500f;
            body.centerOfMass = new Vector3(0f, 0.22f, 0.04f);
        }

        private void CacheFallbackWheels()
        {
            if (externalWheelRig) return;
            for (int i = 0; i < 4; i++)
            {
                wheelCarriers[i] = transform.Find(fallbackWheelNames[i]);
                wheelSpinPivots[i] = wheelCarriers[i];
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                throttleInput = 0f;
                steerInput = 0f;
                handbrakeInput = false;
                return;
            }

            throttleInput = 0f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) throttleInput += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) throttleInput -= 1f;

            steerInput = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steerInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steerInput += 1f;

            handbrakeInput = keyboard.spaceKey.isPressed;
        }

        private void FixedUpdate()
        {
            SampleWheels();
            ApplySuspensionForces();
            ApplyAntiRoll();
            ApplyTireForces();
            ApplyAerodynamics();
        }

        private void LateUpdate() => UpdateVisualWheels();

        private void SampleWheels()
        {
            GroundedWheels = 0;
            float rayLength = suspensionMaxLength + wheelRadius;

            for (int i = 0; i < 4; i++)
            {
                Vector3 hardpoint = transform.TransformPoint(suspensionHardpoints[i]);
                grounded[i] = Physics.Raycast(hardpoint, -transform.up, out hits[i], rayLength, ~0, QueryTriggerInteraction.Ignore);

                if (!grounded[i])
                {
                    springLength[i] = suspensionMaxLength;
                    wheelLoad[i] = 0f;
                    continue;
                }

                GroundedWheels++;
                springLength[i] = Mathf.Clamp(hits[i].distance - wheelRadius, suspensionMinLength, suspensionMaxLength);
            }
        }

        private void ApplySuspensionForces()
        {
            float maxWheelForce = body.mass * Physics.gravity.magnitude * 0.75f;

            for (int i = 0; i < 4; i++)
            {
                if (!grounded[i]) continue;

                Vector3 hardpoint = transform.TransformPoint(suspensionHardpoints[i]);
                Vector3 hardpointVelocity = body.GetPointVelocity(hardpoint);
                float suspensionVelocity = Vector3.Dot(hardpointVelocity, transform.up);

                float compression = suspensionRestLength - springLength[i];
                float springForce = Mathf.Max(0f, compression * springRate);
                float damperForce = -suspensionVelocity * damperRate;
                float bumpCompression = Mathf.Max(0f, suspensionMinLength + 0.035f - springLength[i]);
                float bumpForce = bumpCompression * bumpStopRate;
                float totalForce = Mathf.Clamp(springForce + damperForce + bumpForce, 0f, maxWheelForce);

                wheelLoad[i] = totalForce;
                body.AddForceAtPosition(transform.up * totalForce, hardpoint, ForceMode.Force);
            }
        }

        private void ApplyAntiRoll()
        {
            ApplyAxleAntiRoll(0, 1);
            ApplyAxleAntiRoll(2, 3);
        }

        private void ApplyAxleAntiRoll(int left, int right)
        {
            if (!grounded[left] || !grounded[right]) return;

            float leftCompression = suspensionRestLength - springLength[left];
            float rightCompression = suspensionRestLength - springLength[right];
            float force = (leftCompression - rightCompression) * antiRollRate;

            Vector3 leftPoint = transform.TransformPoint(suspensionHardpoints[left]);
            Vector3 rightPoint = transform.TransformPoint(suspensionHardpoints[right]);
            body.AddForceAtPosition(transform.up * force, leftPoint, ForceMode.Force);
            body.AddForceAtPosition(-transform.up * force, rightPoint, ForceMode.Force);
        }

        private void ApplyTireForces()
        {
            float chassisForwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            float speedKph = Mathf.Abs(chassisForwardSpeed) * 3.6f;
            float steeringRange = Mathf.Lerp(steeringDegrees, highSpeedSteeringDegrees, Mathf.InverseLerp(35f, 160f, speedKph));
            currentSteerAngle = steerInput * steeringRange;

            for (int i = 0; i < 4; i++)
            {
                if (!grounded[i]) continue;

                bool front = i < 2;
                bool rear = i >= 2;
                Vector3 contactPoint = hits[i].point;

                Quaternion steerRotation = front ? Quaternion.AngleAxis(currentSteerAngle, transform.up) : Quaternion.identity;
                Vector3 wheelForward = steerRotation * transform.forward;
                Vector3 wheelRight = steerRotation * transform.right;
                Vector3 contactVelocity = body.GetPointVelocity(contactPoint);

                float longitudinalSpeed = Vector3.Dot(contactVelocity, wheelForward);
                float lateralSpeed = Vector3.Dot(contactVelocity, wheelRight);
                float grip = front ? frontGrip : rearGrip;
                if (rear && handbrakeInput) grip = handbrakeRearGrip;

                float load = Mathf.Max(0f, wheelLoad[i]);
                float lateralLimit = load * grip;
                float lateralForce = Mathf.Clamp(-lateralSpeed * lateralDamping, -lateralLimit, lateralLimit);
                body.AddForceAtPosition(wheelRight * lateralForce, contactPoint, ForceMode.Force);

                if (rear)
                {
                    float requestedAcceleration = 0f;
                    if (throttleInput > 0f)
                    {
                        if (chassisForwardSpeed < -0.5f) requestedAcceleration = serviceBrakeAcceleration;
                        else if (chassisForwardSpeed < maxForwardSpeedKph / 3.6f) requestedAcceleration = acceleration * throttleInput;
                    }
                    else if (throttleInput < 0f)
                    {
                        if (chassisForwardSpeed > 0.5f) requestedAcceleration = -serviceBrakeAcceleration;
                        else if (chassisForwardSpeed > -maxReverseSpeedKph / 3.6f) requestedAcceleration = reverseAcceleration * throttleInput;
                    }

                    if (Mathf.Abs(requestedAcceleration) > 0.01f)
                    {
                        float driveForce = requestedAcceleration * body.mass * 0.5f;
                        float tractionLimit = Mathf.Max(1200f, load * 1.25f);
                        driveForce = Mathf.Clamp(driveForce, -tractionLimit, tractionLimit);
                        body.AddForceAtPosition(wheelForward * driveForce, contactPoint, ForceMode.Force);
                    }

                    if (handbrakeInput && Mathf.Abs(longitudinalSpeed) > 0.2f)
                    {
                        float brakeForce = Mathf.Min(load * 0.9f, body.mass * serviceBrakeAcceleration * 0.25f);
                        body.AddForceAtPosition(-wheelForward * Mathf.Sign(longitudinalSpeed) * brakeForce, contactPoint, ForceMode.Force);
                    }
                }
            }

            if (GroundedWheels > 0 && Mathf.Abs(throttleInput) < 0.01f)
                body.AddForce(-body.linearVelocity * rollingResistance, ForceMode.Acceleration);
        }

        private void ApplyAerodynamics()
        {
            if (GroundedWheels == 0) return;

            body.AddForce(-transform.up * body.linearVelocity.sqrMagnitude * downforce, ForceMode.Force);
            Vector3 localAngular = transform.InverseTransformDirection(body.angularVelocity);
            body.AddRelativeTorque(new Vector3(0f, -localAngular.y * yawDamping, 0f), ForceMode.Acceleration);
        }

        private void UpdateVisualWheels()
        {
            if (wheelCarriers[0] == null) CacheFallbackWheels();

            float forwardSpeed = ForwardSpeedKph / 3.6f;
            float spinDelta = wheelRadius > 0.01f ? (forwardSpeed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime : 0f;

            for (int i = 0; i < 4; i++)
            {
                Transform carrier = wheelCarriers[i];
                if (carrier == null) continue;

                Vector3 hardpoint = transform.TransformPoint(suspensionHardpoints[i]);
                Vector3 centerWorld = grounded[i]
                    ? hits[i].point + transform.up * wheelRadius
                    : hardpoint - transform.up * suspensionMaxLength;

                carrier.position = centerWorld;
                float steer = i < 2 ? currentSteerAngle : 0f;
                carrier.rotation = transform.rotation * Quaternion.Euler(0f, steer, 0f);

                wheelSpin[i] = Mathf.Repeat(wheelSpin[i] + spinDelta, 360f);
                Transform spin = wheelSpinPivots[i];
                if (spin == null) continue;

                spin.localRotation = externalWheelRig
                    ? Quaternion.Euler(wheelSpin[i], 0f, 0f)
                    : Quaternion.Euler(wheelSpin[i], 0f, 90f);
            }
        }
    }
}
