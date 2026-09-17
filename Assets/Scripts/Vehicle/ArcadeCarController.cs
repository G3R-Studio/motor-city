using UnityEngine;

namespace MotorCity.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        [Header("Power")]
        [SerializeField] private float acceleration = 14.5f;
        [SerializeField] private float reverseAcceleration = 8.5f;
        [SerializeField] private float maxForwardSpeedKph = 205f;
        [SerializeField] private float maxReverseSpeedKph = 55f;
        [SerializeField] private float braking = 20f;
        [SerializeField] private float directionChangeBrake = 24f;

        [Header("Suspension")]
        [SerializeField] private float suspensionRestLength = 0.72f;
        [SerializeField] private float wheelRadius = 0.34f;
        [SerializeField] private float springStrength = 36000f;
        [SerializeField] private float damperStrength = 5600f;

        [Header("Tires")]
        [SerializeField] private float tireGrip = 1.25f;
        [SerializeField] private float rearGrip = 1.12f;
        [SerializeField] private float handbrakeGrip = 0.42f;
        [SerializeField] private float lateralStiffness = 5.4f;
        [SerializeField] private float rollingResistance = 0.018f;
        [SerializeField] private float steeringDegrees = 30f;
        [SerializeField] private float highSpeedSteeringDegrees = 11f;

        [Header("Stability")]
        [SerializeField] private float downforce = 0.8f;
        [SerializeField] private float antiRollStrength = 2600f;
        [SerializeField] private float rollDamping = 3.2f;
        [SerializeField] private float pitchDamping = 1.5f;

        private readonly Vector3[] suspensionPoints =
        {
            new(-0.82f, -0.12f, 1.34f),
            new( 0.82f, -0.12f, 1.34f),
            new(-0.82f, -0.12f,-1.34f),
            new( 0.82f, -0.12f,-1.34f)
        };

        private readonly bool[] grounded = new bool[4];
        private readonly float[] compression = new float[4];
        private readonly float[] suspensionLoad = new float[4];
        private readonly RaycastHit[] hits = new RaycastHit[4];

        private Rigidbody body;
        private float throttleInput;
        private float steerInput;
        private bool handbrakeInput;

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
            body.linearDamping = 0.015f;
            body.angularDamping = 1.9f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.centerOfMass = new Vector3(0f, -0.62f, 0.06f);
            body.maxLinearVelocity = maxForwardSpeedKph / 3.6f + 15f;
        }

        private void Update()
        {
            throttleInput = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) throttleInput += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) throttleInput -= 1f;

            steerInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) steerInput -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) steerInput += 1f;

            handbrakeInput = Input.GetKey(KeyCode.Space);
        }

        private void FixedUpdate()
        {
            SampleSuspension();
            ApplySuspension();
            ApplyTireForces();
            ApplyAntiRoll();
            ApplyBodyDamping();
            ApplyDownforce();
        }

        private void SampleSuspension()
        {
            GroundedWheels = 0;
            float rayLength = suspensionRestLength + wheelRadius;

            for (int i = 0; i < suspensionPoints.Length; i++)
            {
                Vector3 origin = transform.TransformPoint(suspensionPoints[i]);
                grounded[i] = Physics.Raycast(origin, -transform.up, out hits[i], rayLength, ~0, QueryTriggerInteraction.Ignore);

                if (!grounded[i])
                {
                    compression[i] = 0f;
                    suspensionLoad[i] = 0f;
                    continue;
                }

                GroundedWheels++;
                float springLength = Mathf.Max(0f, hits[i].distance - wheelRadius);
                compression[i] = Mathf.Clamp01((suspensionRestLength - springLength) / suspensionRestLength);
            }
        }

        private void ApplySuspension()
        {
            for (int i = 0; i < suspensionPoints.Length; i++)
            {
                if (!grounded[i]) continue;

                Vector3 point = transform.TransformPoint(suspensionPoints[i]);
                Vector3 pointVelocity = body.GetPointVelocity(point);
                float verticalVelocity = Vector3.Dot(pointVelocity, transform.up);
                float springCompressionMeters = compression[i] * suspensionRestLength;
                float force = springCompressionMeters * springStrength - verticalVelocity * damperStrength;
                force = Mathf.Clamp(force, 0f, body.mass * Physics.gravity.magnitude * 0.85f);
                suspensionLoad[i] = force;

                body.AddForceAtPosition(transform.up * force, point, ForceMode.Force);
            }
        }

        private void ApplyTireForces()
        {
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            float speedKph = Mathf.Abs(forwardSpeed) * 3.6f;
            float steeringRange = Mathf.Lerp(steeringDegrees, highSpeedSteeringDegrees, Mathf.InverseLerp(35f, 160f, speedKph));
            float steerAngle = steerInput * steeringRange;

            for (int i = 0; i < suspensionPoints.Length; i++)
            {
                if (!grounded[i]) continue;

                bool frontWheel = i < 2;
                bool rearWheel = i >= 2;
                Vector3 point = transform.TransformPoint(suspensionPoints[i]);
                Quaternion wheelRotation = frontWheel ? Quaternion.AngleAxis(steerAngle, transform.up) * transform.rotation : transform.rotation;
                Vector3 wheelForward = wheelRotation * Vector3.forward;
                Vector3 wheelRight = wheelRotation * Vector3.right;
                Vector3 velocity = body.GetPointVelocity(point);

                float longitudinalSpeed = Vector3.Dot(velocity, wheelForward);
                float lateralSpeed = Vector3.Dot(velocity, wheelRight);
                float gripCoefficient = frontWheel ? tireGrip : rearGrip;
                if (rearWheel && handbrakeInput) gripCoefficient = handbrakeGrip;

                float load = Mathf.Max(500f, suspensionLoad[i]);
                float maxLateralForce = load * gripCoefficient;
                float requestedLateralForce = -lateralSpeed * body.mass * lateralStiffness * 0.25f;
                float lateralForce = Mathf.Clamp(requestedLateralForce, -maxLateralForce, maxLateralForce);
                body.AddForceAtPosition(wheelRight * lateralForce, point, ForceMode.Force);

                float driveAcceleration = 0f;
                if (throttleInput > 0f)
                {
                    if (forwardSpeed < -0.7f) driveAcceleration = directionChangeBrake;
                    else if (forwardSpeed < maxForwardSpeedKph / 3.6f) driveAcceleration = acceleration * throttleInput;
                }
                else if (throttleInput < 0f)
                {
                    if (forwardSpeed > 0.7f) driveAcceleration = -directionChangeBrake;
                    else if (forwardSpeed > -maxReverseSpeedKph / 3.6f) driveAcceleration = reverseAcceleration * throttleInput;
                }

                if (rearWheel && Mathf.Abs(driveAcceleration) > 0.01f)
                    body.AddForceAtPosition(wheelForward * (driveAcceleration * body.mass * 0.5f), point, ForceMode.Force);

                if (handbrakeInput && rearWheel && Mathf.Abs(longitudinalSpeed) > 0.1f)
                    body.AddForceAtPosition(-wheelForward * Mathf.Sign(longitudinalSpeed) * braking * body.mass * 0.25f, point, ForceMode.Force);
            }

            if (Mathf.Abs(throttleInput) < 0.01f && body.linearVelocity.sqrMagnitude > 0.04f)
                body.AddForce(-body.linearVelocity * rollingResistance, ForceMode.Acceleration);
        }

        private void ApplyAntiRoll()
        {
            ApplyAxleAntiRoll(0, 1);
            ApplyAxleAntiRoll(2, 3);
        }

        private void ApplyAxleAntiRoll(int left, int right)
        {
            // Do not use the anti-roll bar as a lever when one wheel has already left
            // the ground; that amplified the tipping seen in the first suspension build.
            if (!grounded[left] || !grounded[right]) return;

            float travelDifference = compression[left] - compression[right];
            float force = travelDifference * antiRollStrength;
            body.AddForceAtPosition(-transform.up * force, transform.TransformPoint(suspensionPoints[left]), ForceMode.Force);
            body.AddForceAtPosition(transform.up * force, transform.TransformPoint(suspensionPoints[right]), ForceMode.Force);
        }

        private void ApplyBodyDamping()
        {
            if (GroundedWheels < 2) return;

            Vector3 localAngular = transform.InverseTransformDirection(body.angularVelocity);
            Vector3 dampingTorqueLocal = new(
                -localAngular.x * pitchDamping,
                0f,
                -localAngular.z * rollDamping
            );
            body.AddRelativeTorque(dampingTorqueLocal, ForceMode.Acceleration);
        }

        private void ApplyDownforce()
        {
            if (GroundedWheels == 0) return;
            body.AddForce(-transform.up * body.linearVelocity.sqrMagnitude * downforce, ForceMode.Force);
        }
    }
}
