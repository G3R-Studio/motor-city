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

        private const string SettingsVersionKey =
            "MotorCity.Settings.MusicVersion";

        private const int CurrentSettingsVersion = 2;
        private const int SampleRate = 22050;
        private const float LoopSeconds = 48f;

        private static MotorCityMusicRuntime instance;

        private AudioSource source;
        private AudioClip menuClip;
        private AudioClip cityClip;
        private float musicVolume = 0.10f;
        private bool musicMuted;
        private bool menuActive;
        private bool gameplayActive;

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

            if (active)
            {
                instance.gameplayActive =
                    false;
            }

            instance.RefreshPlayback();
        }

        public static void SetGameplayActive(
            bool active)
        {
            EnsureExists();

            instance.gameplayActive =
                active;

            if (active)
            {
                instance.menuActive =
                    false;
            }

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

            int settingsVersion =
                MotorCitySaveService.GetInt(
                    SettingsVersionKey,
                    0);

            if (settingsVersion <
                CurrentSettingsVersion)
            {
                musicVolume =
                    0.10f;

                MotorCitySaveService.SetFloat(
                    VolumeKey,
                    musicVolume);

                MotorCitySaveService.SetInt(
                    SettingsVersionKey,
                    CurrentSettingsVersion);

                MotorCitySaveService.Save();
            }
            else
            {
                musicVolume =
                    Mathf.Clamp01(
                        MotorCitySaveService.GetFloat(
                            VolumeKey,
                            0.10f));
            }

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
            source.ignoreListenerVolume = true;
            source.dopplerLevel = 0f;
            source.priority = 32;

            menuClip =
                BuildMenuLoop();

            cityClip =
                BuildCityLoop();

            source.clip =
                menuClip;

            ApplyVolume();
        }

        private void RefreshPlayback()
        {
            if (source == null)
                return;

            AudioClip desiredClip =
                menuActive
                    ? menuClip
                    : gameplayActive
                        ? cityClip
                        : null;

            if (desiredClip == null ||
                musicMuted)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                }

                return;
            }

            if (source.clip !=
                desiredClip)
            {
                source.Stop();
                source.clip =
                    desiredClip;
            }

            if (!source.isPlaying)
            {
                source.Play();
            }
        }

        private void ApplyVolume()
        {
            if (source == null)
                return;

            source.volume =
                musicMuted
                    ? 0f
                    : musicVolume;
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
                        loopFade *
                        2.35f,
                        -0.90f,
                        0.90f);
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

        private static AudioClip BuildCityLoop()
        {
            int sampleCount =
                Mathf.RoundToInt(
                    SampleRate *
                    LoopSeconds);

            float[] samples =
                new float[
                    sampleCount];

            const float bpm =
                92f;

            float beatSeconds =
                60f /
                bpm;

            int[] roots =
            {
                45,
                41,
                48,
                43
            };

            int[] leadNotes =
            {
                64,
                67,
                69,
                71,
                69,
                67,
                64,
                62
            };

            for (int i = 0;
                 i < sampleCount;
                 i++)
            {
                float t =
                    i /
                    (float)SampleRate;

                int beat =
                    Mathf.FloorToInt(
                        t /
                        beatSeconds);

                int bar =
                    beat /
                    4;

                int root =
                    roots[
                        bar %
                        roots.Length];

                float beatLocal =
                    Mathf.Repeat(
                        t,
                        beatSeconds);

                float value = 0f;

                // Continuous city-night pad.
                value +=
                    SoftTone(
                        root + 12,
                        t,
                        0.040f);

                value +=
                    SoftTone(
                        root + 19,
                        t,
                        0.030f);

                value +=
                    SoftTone(
                        root + 24,
                        t,
                        0.018f);

                // Soft pulse on each beat instead of a heavy drum.
                float pulse =
                    Mathf.Exp(
                        -beatLocal *
                        3.8f);

                value +=
                    SoftTone(
                        root,
                        t,
                        0.060f) *
                    pulse;

                // Small low-frequency thump on beats 1 and 3.
                if (beat % 4 == 0 ||
                    beat % 4 == 2)
                {
                    float kickEnvelope =
                        Mathf.Exp(
                            -beatLocal *
                            14f);

                    value +=
                        Mathf.Sin(
                            2f *
                            Mathf.PI *
                            52f *
                            t) *
                        kickEnvelope *
                        0.055f;
                }

                // Sparse driving melody every two beats.
                int melodyStep =
                    Mathf.FloorToInt(
                        t /
                        (beatSeconds * 2f));

                float melodyLocal =
                    Mathf.Repeat(
                        t,
                        beatSeconds * 2f);

                float melodyEnvelope =
                    Mathf.Exp(
                        -melodyLocal *
                        1.8f) *
                    Mathf.Clamp01(
                        melodyLocal /
                        0.06f);

                int melody =
                    leadNotes[
                        melodyStep %
                        leadNotes.Length];

                value +=
                    SoftTone(
                        melody,
                        t,
                        0.022f) *
                    melodyEnvelope;

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
                        loopFade *
                        2.15f,
                        -0.88f,
                        0.88f);
            }

            AudioClip clip =
                AudioClip.Create(
                    "Motor City Night Drive",
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
