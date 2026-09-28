using MotorCity.Input;
using UnityEngine;

namespace MotorCity.Vehicle
{
    /// <summary>
    /// Runtime fallback vehicle audio. The repository currently contains no
    /// engine/tire audio clips, so this restores audible feedback without
    /// depending on missing Prometeo sample assets.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class PlayerVehicleAudio : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const int ClipSamples = SampleRate;

        private readonly struct EngineProfile
        {
            public readonly float BaseFrequency;
            public readonly float MinPitch;
            public readonly float MaxPitch;
            public readonly float IdleVolume;
            public readonly float MaxVolume;
            public readonly float[] Harmonics;
            public readonly float Roughness;
            public readonly float Flutter;
            public readonly bool ElectricLike;

            public EngineProfile(
                float baseFrequency,
                float minPitch,
                float maxPitch,
                float idleVolume,
                float maxVolume,
                float[] harmonics,
                float roughness,
                float flutter,
                bool electricLike = false)
            {
                BaseFrequency = baseFrequency;
                MinPitch = minPitch;
                MaxPitch = maxPitch;
                IdleVolume = idleVolume;
                MaxVolume = maxVolume;
                Harmonics = harmonics;
                Roughness = roughness;
                Flutter = flutter;
                ElectricLike = electricLike;
            }
        }

        private ArcadeCarController car;
        private AudioSource engineSource;
        private AudioSource tireSource;
        private AudioClip engineClip;
        private AudioClip tireClip;
        private string vehicleId = "street";
        private EngineProfile activeProfile;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();

            activeProfile =
                ResolveProfile(
                    vehicleId);

            engineClip =
                BuildEngineClip(
                    activeProfile,
                    vehicleId);

            tireClip =
                Resources.Load<AudioClip>(
                    "MotorCity/Audio/TireSkid");

            engineSource =
                CreateSource(
                    "Motor City Engine Audio",
                    engineClip,
                    0.36f);

            if (tireClip != null)
            {
                tireSource =
                    CreateSource(
                        "Motor City Tire Audio",
                        tireClip,
                        0.44f);
            }

            engineSource.volume = 0f;

            if (tireSource != null)
                tireSource.volume = 0f;

            engineSource.Play();
        }

        public void SetVehicleId(
            string id)
        {
            string normalized =
                string.IsNullOrWhiteSpace(id)
                    ? "street"
                    : id.Trim().ToLowerInvariant();

            if (vehicleId == normalized &&
                engineClip != null)
            {
                return;
            }

            vehicleId =
                normalized;

            activeProfile =
                ResolveProfile(
                    vehicleId);

            AudioClip replacement =
                BuildEngineClip(
                    activeProfile,
                    vehicleId);

            if (replacement == null)
                return;

            AudioClip previous =
                engineClip;

            engineClip =
                replacement;

            if (engineSource != null)
            {
                bool wasPlaying =
                    engineSource.isPlaying;

                engineSource.Stop();
                engineSource.clip =
                    engineClip;
                engineSource.pitch =
                    activeProfile.MinPitch;

                if (wasPlaying)
                    engineSource.Play();
            }

            if (previous != null)
                Destroy(previous);
        }

        private void Update()
        {
            if (car == null)
                return;

            float speed01 =
                Mathf.InverseLerp(
                    0f,
                    vehicleId == "bus"
                        ? 110f
                        : vehicleId == "toyotaae86" ||
                          vehicleId == "porsche996"
                            ? 200f
                            : 180f,
                    car.SpeedKph);

            bool throttle =
                MotorCityInput.ThrottleHeld ||
                MotorCityInput.ReverseHeld;

            float throttleAmount =
                throttle
                    ? 1f
                    : 0f;

            float targetEngineVolume =
                Mathf.Lerp(
                    activeProfile.IdleVolume,
                    activeProfile.MaxVolume,
                    speed01) +
                throttleAmount *
                (vehicleId == "hybrid"
                    ? 0.025f
                    : 0.07f);

            float targetEnginePitch =
                Mathf.Lerp(
                    activeProfile.MinPitch,
                    activeProfile.MaxPitch,
                    speed01) +
                throttleAmount *
                (vehicleId == "bus"
                    ? 0.035f
                    : 0.08f);

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
                    2.2f);

            if (tireSource != null)
            {
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
                // Generic modern inline-four: neutral, mid-range dominant.
                "street" =>
                    new EngineProfile(
                        72f,
                        0.72f,
                        1.68f,
                        0.10f,
                        0.31f,
                        new[] { 0.56f, 0.24f, 0.12f, 0.08f },
                        0.045f,
                        0.020f),

                // Hybrid: much quieter combustion bed with a clean electric whine.
                "hybrid" =>
                    new EngineProfile(
                        126f,
                        0.78f,
                        1.42f,
                        0.045f,
                        0.18f,
                        new[] { 0.28f, 0.16f, 0.10f, 0.06f, 0.20f },
                        0.010f,
                        0.008f,
                        true),

                // Classic compact four-cylinder: softer low end, slightly coarse midrange.
                "beatall" =>
                    new EngineProfile(
                        68f,
                        0.70f,
                        1.58f,
                        0.095f,
                        0.29f,
                        new[] { 0.50f, 0.26f, 0.14f, 0.10f },
                        0.070f,
                        0.030f),

                // PRV-style V6 character: smoother than an I4, deeper fundamental.
                "delorean" =>
                    new EngineProfile(
                        58f,
                        0.72f,
                        1.55f,
                        0.10f,
                        0.30f,
                        new[] { 0.50f, 0.18f, 0.19f, 0.08f, 0.05f },
                        0.035f,
                        0.015f),

                // AMG GT: deep cross-plane V8-like burble with strong lower harmonics.
                "amggt" =>
                    new EngineProfile(
                        44f,
                        0.68f,
                        1.46f,
                        0.13f,
                        0.36f,
                        new[] { 0.62f, 0.22f, 0.08f, 0.05f, 0.03f },
                        0.095f,
                        0.045f),

                // 996: smoother, brighter flat-six-like timbre and higher rev character.
                "porsche996" =>
                    new EngineProfile(
                        84f,
                        0.76f,
                        1.86f,
                        0.09f,
                        0.32f,
                        new[] { 0.42f, 0.20f, 0.18f, 0.12f, 0.08f },
                        0.022f,
                        0.012f),

                // Peugeot 306: compact buzzy inline-four.
                "peugeot306" =>
                    new EngineProfile(
                        76f,
                        0.72f,
                        1.70f,
                        0.085f,
                        0.28f,
                        new[] { 0.46f, 0.30f, 0.14f, 0.07f, 0.03f },
                        0.060f,
                        0.026f),

                // AE86 / 4A-GE-inspired: light, bright and noticeably high-revving.
                "toyotaae86" =>
                    new EngineProfile(
                        96f,
                        0.74f,
                        1.98f,
                        0.08f,
                        0.30f,
                        new[] { 0.36f, 0.28f, 0.18f, 0.12f, 0.06f },
                        0.040f,
                        0.018f),

                // Camaro: heavier American V8-style pulse, lowest passenger-car note.
                "camaro" =>
                    new EngineProfile(
                        40f,
                        0.66f,
                        1.40f,
                        0.145f,
                        0.38f,
                        new[] { 0.66f, 0.18f, 0.07f, 0.05f, 0.04f },
                        0.115f,
                        0.055f),

                // City bus: low-rev diesel-like inline-six, strong low harmonics.
                "bus" =>
                    new EngineProfile(
                        34f,
                        0.70f,
                        1.18f,
                        0.15f,
                        0.34f,
                        new[] { 0.70f, 0.17f, 0.07f, 0.04f, 0.02f },
                        0.135f,
                        0.065f),

                _ =>
                    new EngineProfile(
                        72f,
                        0.72f,
                        1.68f,
                        0.10f,
                        0.31f,
                        new[] { 0.56f, 0.24f, 0.12f, 0.08f },
                        0.045f,
                        0.020f)
            };
        }

        private static AudioClip BuildEngineClip(
            EngineProfile profile,
            string id)
        {
            float[] samples =
                new float[ClipSamples];

            uint noiseState =
                0xA341316Cu;

            float filteredNoise =
                0f;

            for (int i = 0;
                 i < samples.Length;
                 i++)
            {
                float t =
                    i /
                    (float)SampleRate;

                float flutter =
                    1f +
                    Mathf.Sin(
                        2f *
                        Mathf.PI *
                        5.1f *
                        t) *
                    profile.Flutter;

                float phase =
                    2f *
                    Mathf.PI *
                    profile.BaseFrequency *
                    t *
                    flutter;

                float value = 0f;

                for (int h = 0;
                     h < profile.Harmonics.Length;
                     h++)
                {
                    float multiplier =
                        h + 1f;

                    value +=
                        Mathf.Sin(
                            phase *
                            multiplier +
                            h * 0.17f) *
                        profile.Harmonics[h];
                }

                if (profile.ElectricLike)
                {
                    value +=
                        Mathf.Sin(
                            phase * 5.6f) *
                        0.14f +
                        Mathf.Sin(
                            phase * 8.2f) *
                        0.07f;
                }

                noiseState =
                    noiseState *
                    1664525u +
                    1013904223u;

                float noise =
                    ((noiseState >> 8) &
                     0x00FFFFFF) /
                    8388607.5f -
                    1f;

                filteredNoise =
                    Mathf.Lerp(
                        filteredNoise,
                        noise,
                        0.09f);

                value +=
                    filteredNoise *
                    profile.Roughness;

                samples[i] =
                    Mathf.Clamp(
                        value * 0.48f,
                        -1f,
                        1f);
            }

            AudioClip clip =
                AudioClip.Create(
                    "MotorCity_Engine_" + id,
                    ClipSamples,
                    1,
                    SampleRate,
                    false);

            clip.SetData(
                samples,
                0);

            return clip;
        }

        private void OnDestroy()
        {
            if (engineClip != null)
                Destroy(engineClip);
        }
    }
}
