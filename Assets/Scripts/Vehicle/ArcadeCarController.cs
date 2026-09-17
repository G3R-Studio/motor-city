using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Vehicle
{
    public sealed class ArcadeCarController : MonoBehaviour
    {
        [Header("CarController.cs")]
        [SerializeField] private float moveSpeed = 50f;
        [SerializeField] private float maxSpeed = 15f;
        [SerializeField] private float drag = 0.98f;
        [SerializeField] private float steerAngle = 20f;
        [SerializeField] private float traction = 1f;

        [Header("Legacy Input.GetAxis Feel")]
        [SerializeField] private float inputSensitivity = 3f;
        [SerializeField] private float inputGravity = 3f;

        [Header("Visual Wheels")]
        [SerializeField] private float visualWheelRadius = 0.34f;

        private readonly Transform[] wheelVisualRoots = new Transform[4];
        private readonly Transform[] brakeVisualRoots = new Transform[4];

        // This is the entire movement state, matching the uploaded CarController.cs.
        private Vector3 moveForce;
        private float throttleInput;
        private float steerInput;
        private float wheelSpinDegrees;

        public float SpeedKph => moveForce.magnitude * 3.6f;
        public float ForwardSpeedKph => Vector3.Dot(moveForce, transform.forward) * 3.6f;
        public bool IsHandbrake => false;
        public int GroundedWheels => 4;
        public float RearForwardSlip => 0f;

        public float RearSidewaysSlip
        {
            get
            {
                float speed = moveForce.magnitude;
                if (speed < 0.01f) return 0f;

                Vector3 localVelocity = transform.InverseTransformDirection(moveForce);
                return Mathf.Clamp01(Mathf.Abs(localVelocity.x) / speed);
            }
        }

        public float SlipAngleDegrees
        {
            get
            {
                if (moveForce.sqrMagnitude < 0.01f) return 0f;

                Vector3 localVelocity = transform.InverseTransformDirection(moveForce);
                return Mathf.Atan2(
                    localVelocity.x,
                    Mathf.Max(0.01f, Mathf.Abs(localVelocity.z))) * Mathf.Rad2Deg;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            float keyboardThrottle = 0f;
            float keyboardSteer = 0f;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardThrottle += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardThrottle -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardSteer -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardSteer += 1f;
            }

            float gamepadThrottle = 0f;
            float gamepadSteer = 0f;

            if (gamepad != null)
            {
                gamepadThrottle = gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue();
                gamepadSteer = gamepad.leftStick.x.ReadValue();
            }

            float throttleTarget =
                Mathf.Abs(keyboardThrottle) >= Mathf.Abs(gamepadThrottle)
                    ? keyboardThrottle
                    : gamepadThrottle;

            float steerTarget =
                Mathf.Abs(keyboardSteer) >= Mathf.Abs(gamepadSteer)
                    ? keyboardSteer
                    : gamepadSteer;

            throttleInput = SmoothLegacyAxis(throttleInput, throttleTarget);
            steerInput = SmoothLegacyAxis(steerInput, steerTarget);

            // Direct port of the uploaded CarController.cs movement:
            // acceleration -> transform movement -> steering -> drag -> speed cap -> traction.
            moveForce += transform.forward * moveSpeed * throttleInput * Time.deltaTime;
            transform.position += moveForce * Time.deltaTime;

            transform.Rotate(
                Vector3.up *
                steerInput *
                moveForce.magnitude *
                steerAngle *
                Time.deltaTime);

            // The source multiplies by Drag once per Update. That makes its feel
            // depend on frame rate. Preserve exactly the same damping it has at
            // 60 FPS while making it stable at any editor/browser frame rate.
            float frameRateIndependentDrag =
                Mathf.Pow(Mathf.Clamp01(drag), Time.deltaTime * 60f);
            moveForce *= frameRateIndependentDrag;
            moveForce = Vector3.ClampMagnitude(moveForce, maxSpeed);

            if (moveForce.sqrMagnitude > 0.000001f)
            {
                moveForce =
                    Vector3.Lerp(
                        moveForce.normalized,
                        transform.forward,
                        traction * Time.deltaTime) *
                    moveForce.magnitude;
            }

            UpdateVisualWheels();
        }

        private float SmoothLegacyAxis(float current, float target)
        {
            float rate = Mathf.Abs(target) > 0.001f ? inputSensitivity : inputGravity;
            return Mathf.MoveTowards(current, target, rate * Time.deltaTime);
        }

        public void ConfigureExternalWheelRig(
            Transform[] visualRoots,
            Transform[] brakeRoots,
            Vector3[] centers,
            float measuredWheelRadius)
        {
            visualWheelRadius = Mathf.Max(0.01f, measuredWheelRadius);

            for (int i = 0; i < 4; i++)
            {
                wheelVisualRoots[i] =
                    visualRoots != null && visualRoots.Length > i
                        ? visualRoots[i]
                        : null;

                brakeVisualRoots[i] =
                    brakeRoots != null && brakeRoots.Length > i
                        ? brakeRoots[i]
                        : null;
            }
        }

        public void ClearMotion()
        {
            moveForce = Vector3.zero;
            throttleInput = 0f;
            steerInput = 0f;
            wheelSpinDegrees = 0f;
        }

        private void UpdateVisualWheels()
        {
            float signedSpeed = Vector3.Dot(moveForce, transform.forward);
            float circumference = 2f * Mathf.PI * visualWheelRadius;

            if (circumference > 0.001f)
            {
                wheelSpinDegrees +=
                    signedSpeed / circumference *
                    360f *
                    Time.deltaTime;
            }

            float visualSteer = steerInput * steerAngle;

            for (int i = 0; i < 4; i++)
            {
                Transform wheel = wheelVisualRoots[i];
                if (wheel != null)
                {
                    float yaw = i < 2 ? visualSteer : 0f;
                    wheel.localRotation =
                        Quaternion.Euler(wheelSpinDegrees, yaw, 0f);
                }

                Transform brakeVisual = brakeVisualRoots[i];
                if (brakeVisual != null)
                {
                    float yaw = i < 2 ? visualSteer : 0f;
                    brakeVisual.localRotation = Quaternion.Euler(0f, yaw, 0f);
                }
            }
        }
    }
}
