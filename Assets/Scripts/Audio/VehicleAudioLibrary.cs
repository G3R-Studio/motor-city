using UnityEngine;

namespace MotorCity.Audio
{
    public sealed class VehicleAudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip engineIdleLoop;
        [SerializeField] private AudioClip engineDriveLoop;
        [SerializeField] private AudioClip drivingLoop;
        [SerializeField] private AudioClip handbrakeClip;
        [SerializeField] private AudioClip hornClip;

        public AudioClip EngineIdleLoop => engineIdleLoop;
        public AudioClip EngineDriveLoop => engineDriveLoop;
        public AudioClip DrivingLoop => drivingLoop;
        public AudioClip HandbrakeClip => handbrakeClip;
        public AudioClip HornClip => hornClip;

        public bool HasAnyClip =>
            engineIdleLoop != null ||
            engineDriveLoop != null ||
            drivingLoop != null ||
            handbrakeClip != null ||
            hornClip != null;
    }
}
