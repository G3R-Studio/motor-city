using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.CameraSystem
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 7.5f;
        [SerializeField] private float height = 2.7f;
        [SerializeField] private float positionSharpness = 9f;
        [SerializeField] private float rotationSharpness = 12f;
        [SerializeField] private float lookAhead = 2.5f;

        [Header("Orbit")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -8f;
        [SerializeField] private float maxPitch = 55f;
        [SerializeField] private float minDistance = 4.5f;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private float zoomSpeed = 0.0125f;
        [SerializeField] private float recenterDelay = 1.35f;
        [SerializeField] private float recenterSpeed = 3f;

        private float yawOffset;
        private float pitch = 13f;
        private float lastManualInputTime = -10f;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void Update()
        {
            if (target == null) return;

            Mouse mouse = Mouse.current;
            bool orbiting = mouse != null && mouse.rightButton.isPressed;

            if (orbiting)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yawOffset += delta.x * mouseSensitivity;
                pitch -= delta.y * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                lastManualInputTime = Time.time;
            }

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
                    lastManualInputTime = Time.time;
                }
            }

            if (!orbiting && Time.time - lastManualInputTime > recenterDelay)
                yawOffset = Mathf.LerpAngle(yawOffset, 0f, 1f - Mathf.Exp(-recenterSpeed * Time.deltaTime));
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

            Quaternion orbitRotation = Quaternion.Euler(pitch, target.eulerAngles.y + yawOffset, 0f);
            Vector3 orbitOffset = orbitRotation * new Vector3(0f, 0f, -distance);
            Vector3 desiredPosition = target.position + Vector3.up * height + orbitOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionT);

            Vector3 lookPoint = target.position + target.forward * lookAhead + Vector3.up * 1.05f;
            Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
        }
    }
}
