using MotorCity.Audio;
using MotorCity.Input;
using UnityEngine;

namespace MotorCity.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class PlayerVehicleAudio : MonoBehaviour
    {
        private readonly struct EngineProfile
        {
            public readonly float MinPitch;
            public readonly float MaxPitch;
            public readonly float IdleVolume;
            public readonly float MaxVolume;
            public readonly float LowPassCutoff;
            public readonly float DistortionLevel;
            public readonly float SpeedReference;

            public EngineProfile(
                float minPitch,
                float maxPitch,
                float idleVolume,
                float maxVolume,
                float lowPassCutoff,
                float distortionLevel,
                float speedReference)
            {
                MinPitch = minPitch;
                MaxPitch = maxPitch;
                IdleVolume = idleVolume;
                MaxVolume = maxVolume;
                LowPassCutoff = lowPassCutoff;
                DistortionLevel = distortionLevel;
                SpeedReference = speedReference;
            }
        }

        private ArcadeCarController car;
        private AudioSource engineSource;
        private AudioSource tireSource;
        private AudioLowPassFilter engineLowPass;
        private AudioDistortionFilter engineDistortion;
        private AudioClip engineClip;
        private AudioClip tireClip;
        private string vehicleId = "street";
        private EngineProfile activeProfile;
        private float nextCollisionSoundTime;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();

            engineClip =
                Resources.Load<AudioClip>(
                    "MotorCity/Audio/CarEngine");

            tireClip =
                Resources.Load<AudioClip>(
                    "MotorCity/Audio/TireSkid");

            activeProfile =
                ResolveProfile(
                    vehicleId);

            if (engineClip != null)
            {
                engineSource =
                    CreateSource(
                        "Motor City Engine Audio",
                        engineClip,
                        0.36f);

                engineLowPass =
                    engineSource.gameObject
                        .AddComponent<AudioLowPassFilter>();

                engineDistortion =
                    engineSource.gameObject
                        .AddComponent<AudioDistortionFilter>();

                ApplyEngineProfile();

                engineSource.volume = 0f;
                engineSource.loop = true;
                engineSource.Play();
            }
            else
            {
                Debug.LogWarning(
                    "Motor City: engine clip is missing at Resources/MotorCity/Audio/CarEngine.",
                    this);
            }

            if (tireClip != null)
            {
                tireSource =
                    CreateSource(
                        "Motor City Tire Audio",
                        tireClip,
                        0.44f);

                tireSource.volume = 0f;
            }
        }

        public void SetVehicleId(
            string id)
        {
            vehicleId =
                string.IsNullOrWhiteSpace(id)
                    ? "street"
                    : id.Trim().ToLowerInvariant();

            activeProfile =
                ResolveProfile(
                    vehicleId);

            ApplyEngineProfile();
        }

        private void ApplyEngineProfile()
        {
            if (engineSource != null)
            {
                engineSource.pitch =
                    activeProfile.MinPitch;
            }

            if (engineLowPass != null)
            {
                engineLowPass.cutoffFrequency =
                    activeProfile.LowPassCutoff;
            }

            if (engineDistortion != null)
            {
                engineDistortion.distortionLevel =
                    activeProfile.DistortionLevel;
            }
        }

        private void Update()
        {
            if (car == null)
                return;

            float speed01 =
                Mathf.InverseLerp(
                    0f,
                    activeProfile.SpeedReference,
                    car.SpeedKph);

            float wheelRpm =
                car.AverageWheelRpm;

            float rpmReference =
                Mathf.Max(
                    450f,
                    activeProfile.SpeedReference /
                    (2f * Mathf.PI * 0.36f) *
                    60f /
                    3.6f);

            float rpm01 =
                Mathf.InverseLerp(
                    0f,
                    rpmReference,
                    wheelRpm);

            // RPM gives the engine sound its immediate response to wheel speed
            // and slip, while the speed component prevents extreme pitch spikes
            // during wheelspin or brief airborne moments.
            float engineLoad01 =
                Mathf.Clamp01(
                    rpm01 * 0.68f +
                    speed01 * 0.32f);

            bool throttle =
                MotorCityInput.ThrottleHeld ||
                MotorCityInput.ReverseHeld;

            float throttleAmount =
                throttle
                    ? 1f
                    : 0f;

            if (engineSource != null)
            {
                float targetEngineVolume =
                    Mathf.Lerp(
                        activeProfile.IdleVolume,
                        activeProfile.MaxVolume,
                        speed01) +
                    throttleAmount * 0.05f;

                float targetEnginePitch =
                    Mathf.Lerp(
                        activeProfile.MinPitch,
                        activeProfile.MaxPitch,
                        engineLoad01) +
                    throttleAmount * 0.045f;

                engineSource.volume =
                    Mathf.MoveTowards(
                        engineSource.volume,
                        targetEngineVolume,
                        Time.unscaledDeltaTime *
                        1.8f);

                engineSource.pitch =
                    Mathf.MoveTowards(
                        engineSource.pitch,
                        targetEnginePitch,
                        Time.unscaledDeltaTime *
                        1.9f);

                if (!engineSource.isPlaying)
                    engineSource.Play();
            }

            UpdateTireAudio();
        }

        private void UpdateTireAudio()
        {
            if (tireSource == null)
                return;

            float slideIntensity =
                Mathf.Clamp01(
                    Mathf.Max(
                        car.DriftIntensity,
                        Mathf.Abs(
                            car.RearSidewaysSlip) *
                        1.8f));

            bool shouldScreech =
                car.SpeedKph > 16f &&
                (car.IsSliding ||
                 (car.HandbrakeInputHeld &&
                  car.SpeedKph > 24f));

            float targetTireVolume =
                shouldScreech
                    ? Mathf.Lerp(
                        0.04f,
                        0.24f,
                        slideIntensity)
                    : 0f;

            tireSource.volume =
                Mathf.MoveTowards(
                    tireSource.volume,
                    targetTireVolume,
                    Time.unscaledDeltaTime *
                    (shouldScreech
                        ? 0.70f
                        : 1.20f));

            tireSource.pitch =
                Mathf.Lerp(
                    0.82f,
                    0.98f,
                    slideIntensity);

            if (tireSource.volume > 0.005f)
            {
                if (!tireSource.isPlaying)
                    tireSource.Play();
            }
            else if (tireSource.isPlaying)
            {
                tireSource.Stop();
            }
        }

        private void OnCollisionEnter(
            Collision collision)
        {
            if (collision == null ||
                Time.unscaledTime <
                    nextCollisionSoundTime)
            {
                return;
            }

            float impactSpeed =
                collision.relativeVelocity.magnitude;

            if (impactSpeed < 3.5f)
                return;

            nextCollisionSoundTime =
                Time.unscaledTime +
                0.12f;

            MotorCitySfxRuntime.PlayCollision(
                Mathf.InverseLerp(
                    3.5f,
                    22f,
                    impactSpeed));
        }

        private AudioSource CreateSource(
            string objectName,
            AudioClip clip,
            float spatialBlend)
        {
            GameObject audioObject =
                new(objectName);

            audioObject.transform.SetParent(
                transform,
                false);

            AudioSource source =
                audioObject.AddComponent<AudioSource>();

            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            source.rolloffMode =
                AudioRolloffMode.Linear;
            source.minDistance = 3f;
            source.maxDistance = 42f;
            source.priority = 64;

            return source;
        }

        private static EngineProfile ResolveProfile(
            string id)
        {
            return id switch
            {
                // Baseline modern road car.
                "street" =>
                    new EngineProfile(
                        0.72f,
                        1.68f,
                        0.10f,
                        0.31f,
                        9000f,
                        0.03f,
                        180f),

                // Same source clip, but cleaner, quieter and less aggressive.
                "hybrid" =>
                    new EngineProfile(
                        0.88f,
                        1.42f,
                        0.045f,
                        0.18f,
                        12000f,
                        0f,
                        165f),

                // Older compact engine: softer top end and slightly rougher.
                "beatall" =>
                    new EngineProfile(
                        0.68f,
                        1.52f,
                        0.095f,
                        0.29f,
                        6200f,
                        0.07f,
                        165f),

                // Deeper V6-like character.
                "delorean" =>
                    new EngineProfile(
                        0.66f,
                        1.46f,
                        0.10f,
                        0.30f,
                        7200f,
                        0.05f,
                        175f),

                // AMG GT: low, darker and heavier.
                "amggt" =>
                    new EngineProfile(
                        0.56f,
                        1.28f,
                        0.13f,
                        0.36f,
                        4700f,
                        0.13f,
                        185f),

                // Porsche 996: brighter and higher-revving.
                "porsche996" =>
                    new EngineProfile(
                        0.84f,
                        1.86f,
                        0.09f,
                        0.32f,
                        12500f,
                        0.025f,
                        205f),

                // Small naturally aspirated inline-four character.
                "peugeot306" =>
                    new EngineProfile(
                        0.77f,
                        1.72f,
                        0.085f,
                        0.28f,
                        8300f,
                        0.045f,
                        180f),

                // AE86: light and clearly higher-pitched.
                "toyotaae86" =>
                    new EngineProfile(
                        0.92f,
                        2.00f,
                        0.08f,
                        0.30f,
                        13500f,
                        0.035f,
                        205f),

                // Camaro: deepest passenger-car profile.
                "camaro" =>
                    new EngineProfile(
                        0.52f,
                        1.20f,
                        0.145f,
                        0.38f,
                        4100f,
                        0.16f,
                        175f),

                // Bus: low-revving and heavily filtered like a large diesel.
                "bus" =>
                    new EngineProfile(
                        0.46f,
                        0.92f,
                        0.15f,
                        0.34f,
                        3000f,
                        0.10f,
                        105f),

                _ =>
                    new EngineProfile(
                        0.72f,
                        1.68f,
                        0.10f,
                        0.31f,
                        9000f,
                        0.03f,
                        180f)
            };
        }
    }
}
