using UnityEngine;

namespace MotorCity.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        [Header("Power")]
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float reverseAcceleration = 9f;
        [SerializeField] private float maxForwardSpeedKph = 165f;
        [SerializeField] private float maxReverseSpeedKph = 45f;
        [SerializeField] private float braking = 28f;

        [Header("Handling")]
        [SerializeField] private float steeringDegreesPerSecond = 95f;
        [SerializeField] private float lateralGrip = 7.5f;
        [SerializeField] private float handbrakeGrip = 1.8f;
        [SerializeField] private float downforce = 1.5f;

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
            body.linearDamping = 0.08f;
            body.angularDamping = 2.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.centerOfMass = new Vector3(0f, -0.45f, 0.15f);
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
            body.AddForce(-transform.up * body.linearVelocity.magnitude * downforce, ForceMode.Acceleration);
        }

        private void ApplyDrive()
        {
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            float forwardLimit = maxForwardSpeedKph / 3.6f;
            float reverseLimit = maxReverseSpeedKph / 3.6f;

            if (throttleInput > 0f && forwardSpeed < forwardLimit)
            {
                body.AddForce(transform.forward * (throttleInput * acceleration), ForceMode.Acceleration);
            }
            else if (throttleInput < 0f && forwardSpeed > -reverseLimit)
            {
                body.AddForce(transform.forward * (throttleInput * reverseAcceleration), ForceMode.Acceleration);
            }
        }

        private void ApplySteering()
        {
            float speed = Mathf.Abs(Vector3.Dot(body.linearVelocity, transform.forward));
            if (speed < 0.25f) return;

            float direction = Mathf.Sign(Vector3.Dot(body.linearVelocity, transform.forward));
            float speedFactor = Mathf.Lerp(1f, 0.42f, Mathf.InverseLerp(0f, 42f, speed));
            float yaw = steerInput * steeringDegreesPerSecond * speedFactor * direction * Time.fixedDeltaTime;
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
    }
}
