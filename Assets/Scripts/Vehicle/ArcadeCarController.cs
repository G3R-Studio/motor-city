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
        [SerializeField] private float vehicleMass = 2000f;
        [SerializeField] private float angularDrag = 0.05f;
        [SerializeField] private float reverseDrag = 0.3f;

        [Header("Prefab WheelCollider")]
        [SerializeField] private float wheelRadius = 0.5f;
        [SerializeField] private float wheelMass = 20f;
        [SerializeField] private float suspensionDistance = 0.5f;
        [SerializeField] private float suspensionSpring = 50000f;
        [SerializeField] private float suspensionDamper = 2000f;
        [SerializeField] private float suspensionTargetPosition = 0.5f;
        [SerializeField] private float forceAppPointDistance = 0.02f;
        [SerializeField] private float wheelDampingRate = 0.25f;

        [Header("Legacy Input.GetAxis Feel")]
        [SerializeField] private float inputSensitivity = 3f;
        [SerializeField] private float inputGravity = 3f;

        private readonly WheelCollider[] wheelColliders = new WheelCollider[4];
        private readonly Transform[] wheelVisualRoots = new Transform[4];
        private readonly Transform[] brakeVisualRoots = new Transform[4];
        private readonly float[] steeringAngles = new float[4];
        private readonly float[] sourceMotorTorque = new float[4];

        private Vector3[] wheelCenters =
        {
            new(-0.94f, 0.56f, 1.32f),
            new( 0.94f, 0.56f, 1.32f),
            new(-0.94f, 0.56f,-1.34f),
            new( 0.94f, 0.56f,-1.34f)
        };

        private readonly string[] fallbackWheelNames =
        {
            "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR"
        };

        private Rigidbody body;
        private float horizontal;
        private float vertical;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float ForwardSpeedKph =>
            body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
        public bool IsHandbrake => false;
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
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;

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

            wheelCenters = new Vector3[4];

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
                sideways.stiffness = 1f;
                wheel.sidewaysFriction = sideways;
            }

        }

        private void Update()
        {
            ReadInput();
            ApplySourceController();
            UpdateVisualWheels();
        }

        private void FixedUpdate()
        {
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
        }

        private void ReadInput()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            float keyboardVertical = 0f;
            float keyboardHorizontal = 0f;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardVertical += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardVertical -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardHorizontal -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardHorizontal += 1f;
            }

            float gamepadVertical = 0f;
            float gamepadHorizontal = 0f;

            if (gamepad != null)
            {
                gamepadVertical =
                    gamepad.rightTrigger.ReadValue() -
                    gamepad.leftTrigger.ReadValue();
                gamepadHorizontal = gamepad.leftStick.x.ReadValue();
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
        }

        private float SmoothLegacyAxis(float current, float target)
        {
            float rate = Mathf.Abs(target) > 0.001f ? inputSensitivity : inputGravity;
            return Mathf.MoveTowards(current, target, rate * Time.deltaTime);
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
                        Time.deltaTime * wheelRotateSpeed);

                // Preserve WheelController.cs torque math in its original coordinate
                // convention, then invert once for Motor City's +Z forward axis.
                sourceMotorTorque[i] =
                    -Mathf.Lerp(
                        sourceMotorTorque[i],
                        0f,
                        Time.deltaTime * wheelAcceleration);

                if (vertical > 0.1f)
                {
                    sourceMotorTorque[i] =
                        -Mathf.Lerp(
                            sourceMotorTorque[i],
                            wheelMaxSpeed,
                            Time.deltaTime * wheelAcceleration);
                }

                if (vertical < -0.1f)
                {
                    sourceMotorTorque[i] =
                        Mathf.Lerp(
                            sourceMotorTorque[i],
                            wheelMaxSpeed,
                            Time.deltaTime * wheelAcceleration * brakePower);
                }

                wheel.motorTorque = -sourceMotorTorque[i];

                if (horizontal > 0.1f)
                {
                    steeringAngles[i] =
                        Mathf.LerpAngle(
                            steeringAngles[i],
                            -wheelSteeringAngle,
                            Time.deltaTime * wheelRotateSpeed);
                }

                if (horizontal < -0.1f)
                {
                    steeringAngles[i] =
                        Mathf.LerpAngle(
                            steeringAngles[i],
                            wheelSteeringAngle,
                            Time.deltaTime * wheelRotateSpeed);
                }

                // In the source prefab all four wheels receive motor torque,
                // while only the front WheelAlignment components are steerable.
                wheel.steerAngle = i < 2 ? steeringAngles[i] : 0f;
            }

            body.linearDamping = vertical < -0.1f ? reverseDrag : 0f;
        }

        private void UpdateVisualWheels()
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
                    brakeVisual.rotation =
                        transform.rotation *
                        Quaternion.Euler(0f, wheel.steerAngle, 0f);
                }
            }
        }

        public void ClearMotion()
        {
            horizontal = 0f;
            vertical = 0f;

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
