using UnityEngine;

namespace MotorCity.CameraSystem
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 localOffset = new(0f, 3.6f, -7.5f);
        [SerializeField] private float positionSharpness = 7f;
        [SerializeField] private float rotationSharpness = 9f;
        [SerializeField] private float lookAhead = 3f;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void LateUpdate()
        {
            if (target == null) return;

            float positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

            Vector3 desiredPosition = target.TransformPoint(localOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionT);

            Vector3 lookPoint = target.position + target.forward * lookAhead + Vector3.up * 1.1f;
            Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
        }
    }
}
