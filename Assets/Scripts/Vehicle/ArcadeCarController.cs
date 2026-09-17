using UnityEngine;

namespace MotorCity.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        [Header("Power")]
        [SerializeField] private float acceleration = 32f;
        [SerializeField] private float reverseAcceleration = 18f;
        [SerializeField] private float maxForwardSpeedKph = 205f;
        [SerializeField] private float maxReverseSpeedKph = 55f;
        [SerializeField] private float braking = 42f;
        [SerializeField] private float directionChangeBrake = 55f;

        [Header("Handling")]
        [SerializeField] private float steeringDegreesPerSecond = 110f;
        [SerializeField] private float lateralGrip = 8.5f;
        [SerializeField] private float handbrakeGrip = 1.8f;
        [SerializeField] private float downforce = 1.35f;
        [SerializeField] private float lowSpeedSteerAssist = 0.85f;

        private Rigidbody body;
        private float throttleInput;
        private float steerInput;
        private bool brakeInput;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float ForwardSpeedKph => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = 1350f;
            body.linearDamping = 0.035f;
            body.angularDamping = 2.2f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.centerOfMass = new Vector3(0f, -0.45f, 0.15f);

            // Unity 6 clamps Rigidbody velocity to maxLinearVelocity before each
            // simulation step. Set it explicitly above the car's own speed limit so
            // our controller, not the physics safety cap, determines top speed.
            float forwardLimitMs = maxForwardSpeedKph / 3.6f;
            body.maxLinearVelocity = forwardLimitMs + 10f;
        }

        private void Update()
        {
            throttleInput = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) throttleInput += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) throttleInput -= 1f;

            steerInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) steerInput -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) steerInput += 1f;

            brakeInput = Input.GetKey(KeyCode.Space);
        }

        private void FixedUpdate()
        {
            ApplyDrive();
            ApplySteering();
            ApplyGrip();
            ApplyBrakes();
            StabilizeVeryLowSpeed();
            body.AddForce(-transform.up * body.linearVelocity.magnitude * downforce, ForceMode.Acceleration);
        }

        private void ApplyDrive()
        {
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            float forwardLimit = maxForwardSpeedKph / 3.6f;
            float reverseLimit = maxReverseSpeedKph / 3.6f;

            if (throttleInput > 0f)
            {
                if (forwardSpeed < -0.8f)
                {
                    ApplyDirectionalBrake();
                    return;
                }

                if (forwardSpeed < forwardLimit)
                {
                    float speedFactor = Mathf.Lerp(1f, 0.28f, Mathf.InverseLerp(0f, forwardLimit, Mathf.Max(0f, forwardSpeed)));
                    body.AddForce(transform.forward * (acceleration * speedFactor), ForceMode.Acceleration);
                }
            }
            else if (throttleInput < 0f)
            {
                if (forwardSpeed > 0.8f)
                {
                    ApplyDirectionalBrake();
                    return;
                }

                if (forwardSpeed > -reverseLimit)
                {
                    float reverseSpeed = Mathf.Abs(Mathf.Min(0f, forwardSpeed));
                    float speedFactor = Mathf.Lerp(1f, 0.35f, Mathf.InverseLerp(0f, reverseLimit, reverseSpeed));
                    body.AddForce(-transform.forward * (reverseAcceleration * speedFactor), ForceMode.Acceleration);
                }
            }
        }

        private void ApplyDirectionalBrake()
        {
            Vector3 forwardVelocity = transform.forward * Vector3.Dot(body.linearVelocity, transform.forward);
            if (forwardVelocity.sqrMagnitude < 0.01f) return;
            body.AddForce(-forwardVelocity.normalized * directionChangeBrake, ForceMode.Acceleration);
        }

        private void ApplySteering()
        {
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            float speed = Mathf.Abs(forwardSpeed);

            float steerStrength;
            if (speed < 1.5f)
            {
                if (Mathf.Abs(throttleInput) < 0.01f) return;
                steerStrength = lowSpeedSteerAssist;
            }
            else
            {
                steerStrength = Mathf.Lerp(1f, 0.42f, Mathf.InverseLerp(0f, 42f, speed));
            }

            float direction = forwardSpeed < -0.1f ? -1f : 1f;
            float yaw = steerInput * steeringDegreesPerSecond * steerStrength * direction * Time.fixedDeltaTime;
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, yaw, 0f));
        }

        private void ApplyGrip()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            float grip = brakeInput ? handbrakeGrip : lateralGrip;
            float lateralCorrection = -localVelocity.x * grip;
            body.AddForce(transform.right * lateralCorrection, ForceMode.Acceleration);
        }

        private void ApplyBrakes()
        {
            if (!brakeInput || body.linearVelocity.sqrMagnitude < 0.05f) return;
            body.AddForce(-body.linearVelocity.normalized * braking, ForceMode.Acceleration);
        }

        private void StabilizeVeryLowSpeed()
        {
            if (Mathf.Abs(throttleInput) > 0.01f || brakeInput) return;

            Vector3 localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            if (Mathf.Abs(localVelocity.z) < 0.12f) localVelocity.z = 0f;
            if (Mathf.Abs(localVelocity.x) < 0.08f) localVelocity.x = 0f;
            body.linearVelocity = transform.TransformDirection(localVelocity);
        }
    }
}
