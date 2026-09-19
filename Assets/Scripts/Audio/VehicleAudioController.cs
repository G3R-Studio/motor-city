using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Audio
{
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class VehicleAudioController : MonoBehaviour
    {
        private const string ResourcePath =
            "MotorCity/Audio/VehicleAudioLibrary";

        [Header("Engine")]
        [SerializeField] private float idleVolume = 0.28f;
        [SerializeField] private float driveVolume = 0.46f;
        [SerializeField] private float drivingNoiseVolume = 0.34f;
        [SerializeField] private float maximumAudibleSpeedKph = 220f;

        private ArcadeCarController car;
        private VehicleAudioLibrary library;
        private AudioSource idleSource;
        private AudioSource driveSource;
        private AudioSource roadSource;
        private AudioSource oneShotSource;

        private bool previousHandbrake;
        private bool warnedMissingLibrary;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();

            library =
                Resources.Load<VehicleAudioLibrary>(
                    ResourcePath);

            if (library == null ||
                !library.HasAnyClip)
            {
                if (!warnedMissingLibrary)
                {
                    warnedMissingLibrary = true;
                    Debug.LogWarning(
                        "Motor City: Vehicle Essentials audio library is missing. " +
                        "Import the local sound pack and run Motor City > Audio > " +
                        "Rebuild Vehicle Essentials Audio Library.");
                }

                enabled =
                    false;

                return;
            }

            idleSource =
                CreateLoopSource(
                    "Engine Idle Audio",
                    library.EngineIdleLoop);

            driveSource =
                CreateLoopSource(
                    "Engine Drive Audio",
                    library.EngineDriveLoop);

            roadSource =
                CreateLoopSource(
                    "Driving Loop Audio",
                    library.DrivingLoop);

            oneShotSource =
                gameObject.AddComponent<AudioSource>();

            ConfigureSource(
                oneShotSource,
                false);

            if (idleSource != null)
                idleSource.Play();

            if (driveSource != null)
                driveSource.Play();

            if (roadSource != null)
                roadSource.Play();

            Debug.Log(
                "Motor City: Vehicle Essentials runtime audio enabled.");
        }

        private void Update()
        {
            if (car == null ||
                library == null)
                return;

            float speed01 =
                Mathf.Clamp01(
                    car.SpeedKph /
                    Mathf.Max(
                        1f,
                        maximumAudibleSpeedKph));

            float throttle01 =
                car.ThrottleInputHeld ||
                car.ReverseInputHeld
                    ? 1f
                    : 0f;

            UpdateEngineAudio(
                speed01,
                throttle01);

            UpdateRoadAudio(
                speed01);

            bool handbrake =
                car.HandbrakeInputHeld;

            if (handbrake &&
                !previousHandbrake &&
                library.HandbrakeClip != null &&
                oneShotSource != null)
            {
                oneShotSource.PlayOneShot(
                    library.HandbrakeClip,
                    0.55f);
            }

            previousHandbrake =
                handbrake;

            Keyboard keyboard =
                Keyboard.current;

            if (keyboard != null &&
                keyboard.hKey.wasPressedThisFrame &&
                library.HornClip != null &&
                oneShotSource != null)
            {
                oneShotSource.PlayOneShot(
                    library.HornClip,
                    0.72f);
            }
        }

        private void UpdateEngineAudio(
            float speed01,
            float throttle01)
        {
            if (idleSource != null)
            {
                float idleBlend =
                    1f -
                    Mathf.SmoothStep(
                        0.02f,
                        0.36f,
                        speed01);

                idleSource.volume =
                    idleVolume *
                    Mathf.Lerp(
                        0.72f,
                        1f,
                        throttle01) *
                    idleBlend;

                idleSource.pitch =
                    Mathf.Lerp(
                        0.92f,
                        1.16f,
                        speed01) +
                    throttle01 *
                    0.04f;
            }

            if (driveSource != null)
            {
                float driveBlend =
                    Mathf.SmoothStep(
                        0.03f,
                        0.62f,
                        speed01);

                driveSource.volume =
                    driveVolume *
                    Mathf.Lerp(
                        0.68f,
                        1f,
                        throttle01) *
                    driveBlend;

                driveSource.pitch =
                    Mathf.Lerp(
                        0.82f,
                        1.72f,
                        speed01) +
                    throttle01 *
                    0.08f;
            }
        }

        private void UpdateRoadAudio(
            float speed01)
        {
            if (roadSource == null)
                return;

            roadSource.volume =
                drivingNoiseVolume *
                Mathf.SmoothStep(
                    0.08f,
                    0.8f,
                    speed01);

            roadSource.pitch =
                Mathf.Lerp(
                    0.9f,
                    1.28f,
                    speed01);
        }

        private AudioSource CreateLoopSource(
            string sourceName,
            AudioClip clip)
        {
            if (clip == null)
                return null;

            GameObject child =
                new(sourceName);

            child.transform.SetParent(
                transform,
                false);

            AudioSource source =
                child.AddComponent<AudioSource>();

            ConfigureSource(
                source,
                true);

            source.clip =
                clip;

            source.volume =
                0f;

            return source;
        }

        private static void ConfigureSource(
            AudioSource source,
            bool loop)
        {
            source.playOnAwake =
                false;

            source.loop =
                loop;

            source.spatialBlend =
                0.55f;

            source.dopplerLevel =
                0f;

            source.rolloffMode =
                AudioRolloffMode.Linear;

            source.minDistance =
                2f;

            source.maxDistance =
                68f;

            source.priority =
                96;
        }
    }
}
