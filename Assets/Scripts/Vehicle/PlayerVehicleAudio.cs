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

        private ArcadeCarController car;
        private AudioSource engineSource;
        private AudioSource tireSource;
        private AudioClip engineClip;
        private AudioClip tireClip;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();

            engineClip =
                BuildEngineClip();

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

            // Tire skid starts only when sliding/handbraking.
        }

        private void Update()
        {
            if (car == null)
                return;

            float speed01 =
                Mathf.InverseLerp(
                    0f,
                    180f,
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
                    0.10f,
                    0.31f,
                    speed01) +
                throttleAmount * 0.08f;

            float targetEnginePitch =
                Mathf.Lerp(
                    0.72f,
                    1.72f,
                    speed01) +
                throttleAmount * 0.10f;

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

        private static AudioClip BuildEngineClip()
        {
            float[] samples =
                new float[ClipSamples];

            const float baseFrequency =
                62f;

            for (int i = 0;
                 i < samples.Length;
                 i++)
            {
                float t =
                    i /
                    (float)SampleRate;

                float phase =
                    2f *
                    Mathf.PI *
                    baseFrequency *
                    t;

                float value =
                    Mathf.Sin(phase) *
                    0.58f +
                    Mathf.Sin(
                        phase * 2f) *
                    0.24f +
                    Mathf.Sin(
                        phase * 3f) *
                    0.10f +
                    Mathf.Sin(
                        phase * 0.5f) *
                    0.08f;

                samples[i] =
                    Mathf.Clamp(
                        value * 0.54f,
                        -1f,
                        1f);
            }

            AudioClip clip =
                AudioClip.Create(
                    "MotorCity_RuntimeEngine",
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
