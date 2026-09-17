using UnityEngine;

namespace MotorCity.World
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CarReset : MonoBehaviour
    {
        private Rigidbody body;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R) || transform.position.y < -10f)
            {
                ResetVehicle();
            }
        }

        public void ResetVehicle()
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = spawnPosition;
            body.rotation = spawnRotation;
        }
    }
}
