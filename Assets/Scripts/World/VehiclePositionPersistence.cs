using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    [RequireComponent(typeof(ArcadeCarController))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehiclePositionPersistence : MonoBehaviour
    {
        private const string HasPositionKey =
            "MotorCity.Vehicle.Position.Has";
        private const string PositionXKey =
            "MotorCity.Vehicle.Position.X";
        private const string PositionYKey =
            "MotorCity.Vehicle.Position.Y";
        private const string PositionZKey =
            "MotorCity.Vehicle.Position.Z";
        private const string YawKey =
            "MotorCity.Vehicle.Position.Yaw";

        [SerializeField] private float saveIntervalSeconds = 2f;
        [SerializeField] private float minimumValidY = -5f;

        private ArcadeCarController car;
        private Rigidbody body;
        private float saveTimer;
        private bool restored;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();

            body =
                GetComponent<Rigidbody>();
        }

        public void RestoreSavedPosition()
        {
            if (restored)
                return;

            restored = true;

            if (PlayerPrefs.GetInt(
                    HasPositionKey,
                    0) == 0)
                return;

            Vector3 position =
                new(
                    PlayerPrefs.GetFloat(
                        PositionXKey,
                        transform.position.x),
                    PlayerPrefs.GetFloat(
                        PositionYKey,
                        transform.position.y),
                    PlayerPrefs.GetFloat(
                        PositionZKey,
                        transform.position.z));

            float yaw =
                PlayerPrefs.GetFloat(
                    YawKey,
                    transform.eulerAngles.y);

            if (!IsFinite(position) ||
                position.y < minimumValidY)
                return;

            Quaternion rotation =
                Quaternion.Euler(
                    0f,
                    yaw,
                    0f);

            if (car != null)
            {
                car.TeleportTo(
                    position,
                    rotation);

                car.SetDrivingEnabled(
                    true);
            }
            else if (body != null)
            {
                body.linearVelocity =
                    Vector3.zero;

                body.angularVelocity =
                    Vector3.zero;

                body.position =
                    position;

                body.rotation =
                    rotation;
            }
            else
            {
                transform.SetPositionAndRotation(
                    position,
                    rotation);
            }

            Physics.SyncTransforms();
        }

        private void Update()
        {
            if (!restored)
                RestoreSavedPosition();

            saveTimer +=
                Time.unscaledDeltaTime;

            if (saveTimer <
                saveIntervalSeconds)
                return;

            saveTimer = 0f;

            SaveCurrentPosition(
                false);
        }

        public void SaveNow()
        {
            SaveCurrentPosition(
                true);
        }

        private void SaveCurrentPosition(
            bool force)
        {
            Vector3 position =
                body != null
                    ? body.position
                    : transform.position;

            if (!IsFinite(position) ||
                position.y < minimumValidY)
                return;

            if (!force &&
                car != null &&
                car.GroundedWheels < 2)
                return;

            float yaw =
                body != null
                    ? body.rotation.eulerAngles.y
                    : transform.eulerAngles.y;

            PlayerPrefs.SetFloat(
                PositionXKey,
                position.x);

            PlayerPrefs.SetFloat(
                PositionYKey,
                position.y);

            PlayerPrefs.SetFloat(
                PositionZKey,
                position.z);

            PlayerPrefs.SetFloat(
                YawKey,
                yaw);

            PlayerPrefs.SetInt(
                HasPositionKey,
                1);

            PlayerPrefs.Save();
        }

        private void OnApplicationPause(
            bool paused)
        {
            if (paused)
                SaveCurrentPosition(
                    false);
        }

        private void OnApplicationQuit()
        {
            SaveCurrentPosition(
                false);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            SaveCurrentPosition(
                false);
        }

        private static bool IsFinite(
            Vector3 value)
        {
            return
                float.IsFinite(value.x) &&
                float.IsFinite(value.y) &&
                float.IsFinite(value.z);
        }
    }
}
