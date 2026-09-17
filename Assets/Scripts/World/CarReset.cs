using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.World
{
    public sealed class CarReset : MonoBehaviour
    {
        private ArcadeCarController car;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            bool resetPressed = keyboard != null && keyboard.rKey.wasPressedThisFrame;

            if (resetPressed || transform.position.y < -10f)
                ResetVehicle();
        }

        public void ResetVehicle()
        {
            if (car != null) car.ClearMotion();
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        }
    }
}
