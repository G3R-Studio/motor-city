using MotorCity.Persistence;
using UnityEngine;

namespace MotorCity.Audio
{
    public sealed class MotorCityMusicRuntime : MonoBehaviour
    {
        private const string VolumeKey =
            "MotorCity.Settings.MusicVolume";
        private const string MutedKey =
            "MotorCity.Settings.MusicMuted";

        private const int SampleRate = 22050;
        private const float LoopSeconds = 48f;

        private static MotorCityMusicRuntime instance;

        private AudioSource source;
        private float musicVolume = 0.55f;
        private bool musicMuted;
        private bool menuActive;

        public static float Volume
        {
            get
            {
                EnsureExists();
                return instance.musicVolume;
            }
        }

        public static bool Muted
        {
            get
            {
                EnsureExists();
                return instance.musicMuted;
            }
        }

        public static void EnsureExists()
        {
            if (instance != null)
                return;

            instance =
                Object.FindAnyObjectByType<MotorCityMusicRuntime>();

            if (instance != null)
                return;

            GameObject host =
                new GameObject(
                    "Motor City Menu Music");

            DontDestroyOnLoad(
                host);

            instance =
                host.AddComponent<MotorCityMusicRuntime>();
        }

        public static void SetMenuActive(
            bool active)
        {
            EnsureExists();

            instance.menuActive =
                active;

            instance.RefreshPlayback();
        }

        public static void AdjustVolume(
            int direction)
        {
            EnsureExists();

            float stepped =
                Mathf.Round(
                    (instance.musicVolume +
                     direction * 0.1f) *
                    10f) /
                10f;

            instance.musicVolume =
                Mathf.Clamp01(
                    stepped);

            MotorCitySaveService.SetFloat(
                VolumeKey,
                instance.musicVolume);

            MotorCitySaveService.Save();

            instance.ApplyVolume();
        }

        public static void ToggleMute()
        {
            EnsureExists();

            instance.musicMuted =
                !instance.musicMuted;

            MotorCitySaveService.SetInt(
                MutedKey,
                instance.musicMuted
                    ? 1
                    : 0);

            MotorCitySaveService.Save();

            instance.ApplyVolume();
            instance.RefreshPlayback();
        }

        private void Awake()
        {
            if (instance != null &&
                instance != this)
            {
                Destroy(
                    gameObject);
                return;
            }

            instance = this;

            DontDestroyOnLoad(
                gameObject);

            musicVolume =
                Mathf.Clamp01(
                    MotorCitySaveService.GetFloat(
                        VolumeKey,
                        0.55f));

            musicMuted =
                MotorCitySaveService.GetInt(
                    MutedKey,
                    0) != 0;

            BuildSource();
        }

        private void BuildSource()
        {
            source =
                gameObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.dopplerLevel = 0f;
            source.priority = 32;

            source.clip =
                BuildMenuLoop();

            ApplyVolume();
        }

        private void RefreshPlayback()
        {
            if (source == null ||
                source.clip == null)
            {
                return;
            }

            if (menuActive &&
                !musicMuted)
            {
                if (!source.isPlaying)
                {
                    source.Play();
                }

                return;
            }

            if (source.isPlaying)
            {
                source.Stop();
            }
        }

        private void ApplyVolume()
        {
            if (source == null)
                return;

            source.volume =
                musicMuted
                    ? 0f
                    : musicVolume * 0.34f;
        }

        private static AudioClip BuildMenuLoop()
        {
            int sampleCount =
                Mathf.RoundToInt(
                    SampleRate *
                    LoopSeconds);

            float[] samples =
                new float[
                    sampleCount];

            // Slow, understated city-night harmony.
            int[] chordRoots =
            {
                45,
                48,
                41,
                43,
                45,
                48
            };

            int[] melodyNotes =
            {
                64,
                67,
                69,
                67,
                64,
                62,
                60,
                62
            };

            const float chordSeconds =
                8f;

            const float melodyStepSeconds =
                3f;

            for (int i = 0;
                 i < sampleCount;
                 i++)
            {
                float t =
                    i /
                    (float)SampleRate;

                int chordIndex =
                    Mathf.FloorToInt(
                        t /
                        chordSeconds) %
                    chordRoots.Length;

                int root =
                    chordRoots[
                        chordIndex];

                float chordLocal =
                    Mathf.Repeat(
                        t,
                        chordSeconds);

                float chordFade =
                    SmoothEnvelope(
                        chordLocal,
                        chordSeconds,
                        1.8f,
                        2.2f);

                float value = 0f;

                // Wide, soft pad built only from sine-based harmonics.
                value +=
                    SoftTone(
                        root + 12,
                        t,
                        0.050f);

                value +=
                    SoftTone(
                        root + 19,
                        t,
                        0.036f);

                value +=
                    SoftTone(
                        root + 24,
                        t,
                        0.024f);

                value *=
                    chordFade;

                // Quiet low root that gives the menu weight without a beat.
                value +=
                    SoftTone(
                        root,
                        t,
                        0.052f) *
                    (0.72f +
                     0.28f *
                     Mathf.Sin(
                         t *
                         Mathf.PI *
                         0.25f));

                // Sparse glass-like lead, deliberately much quieter.
                int melodyIndex =
                    Mathf.FloorToInt(
                        t /
                        melodyStepSeconds) %
                    melodyNotes.Length;

                float melodyLocal =
                    Mathf.Repeat(
                        t,
                        melodyStepSeconds);

                float melodyEnvelope =
                    Mathf.Exp(
                        -melodyLocal *
                        1.65f) *
                    Mathf.Clamp01(
                        melodyLocal /
                        0.08f);

                int melody =
                    melodyNotes[
                        melodyIndex];

                value +=
                    SoftTone(
                        melody,
                        t,
                        0.028f) *
                    melodyEnvelope;

                value +=
                    SoftTone(
                        melody + 12,
                        t,
                        0.010f) *
                    melodyEnvelope;

                // Very slow movement so the loop does not feel static.
                float breathe =
                    0.88f +
                    0.12f *
                    Mathf.Sin(
                        2f *
                        Mathf.PI *
                        t /
                        12f);

                value *=
                    breathe;

                float loopEdge =
                    Mathf.Min(
                        t,
                        LoopSeconds - t);

                float loopFade =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(
                            loopEdge /
                            0.30f));

                samples[i] =
                    Mathf.Clamp(
                        value *
                        loopFade,
                        -0.72f,
                        0.72f);
            }

            AudioClip clip =
                AudioClip.Create(
                    "Motor City Menu Atmosphere",
                    sampleCount,
                    1,
                    SampleRate,
                    false);

            clip.SetData(
                samples,
                0);

            return clip;
        }

        private static float SoftTone(
            int midi,
            float time,
            float amplitude)
        {
            float frequency =
                440f *
                Mathf.Pow(
                    2f,
                    (midi - 69) /
                    12f);

            float fundamental =
                Mathf.Sin(
                    2f *
                    Mathf.PI *
                    frequency *
                    time);

            float second =
                Mathf.Sin(
                    2f *
                    Mathf.PI *
                    frequency *
                    2f *
                    time +
                    0.35f);

            return
                (fundamental * 0.86f +
                 second * 0.14f) *
                amplitude;
        }

        private static float SmoothEnvelope(
            float localTime,
            float duration,
            float attack,
            float release)
        {
            float fadeIn =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        localTime /
                        attack));

            float fadeOut =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        (duration -
                         localTime) /
                        release));

            return
                Mathf.Min(
                    fadeIn,
                    fadeOut);
        }
    }
}
