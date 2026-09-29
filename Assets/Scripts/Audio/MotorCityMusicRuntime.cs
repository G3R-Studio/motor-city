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
        private const float LoopSeconds = 32f;

        private static MotorCityMusicRuntime instance;

        private AudioSource source;
        private float musicVolume = 0.55f;
        private bool musicMuted;

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
                    "Motor City Music");

            DontDestroyOnLoad(
                host);

            instance =
                host.AddComponent<MotorCityMusicRuntime>();
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
                BuildCruiseLoop();

            ApplyVolume();

            if (source.clip != null)
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
                    : musicVolume * 0.42f;
        }

        private static AudioClip BuildCruiseLoop()
        {
            int sampleCount =
                Mathf.RoundToInt(
                    SampleRate *
                    LoopSeconds);

            float[] samples =
                new float[
                    sampleCount];

            const float bpm = 105f;
            float beatSeconds =
                60f / bpm;

            int[] chordRoots =
            {
                45,
                41,
                48,
                43
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
                    beat / 4;

                int rootMidi =
                    chordRoots[
                        bar %
                        chordRoots.Length];

                float phaseInBeat =
                    Mathf.Repeat(
                        t,
                        beatSeconds);

                float value = 0f;

                // Warm sustained pad.
                value +=
                    SineMidi(
                        rootMidi + 12,
                        t) *
                    0.055f;

                value +=
                    SineMidi(
                        rootMidi + 19,
                        t) *
                    0.040f;

                value +=
                    SineMidi(
                        rootMidi + 24,
                        t) *
                    0.028f;

                // Pulsing bass on quarter notes.
                float bassEnvelope =
                    Mathf.Exp(
                        -phaseInBeat *
                        4.2f);

                value +=
                    SineMidi(
                        rootMidi,
                        t) *
                    bassEnvelope *
                    0.12f;

                // Gentle arpeggio on eighth notes.
                float eighth =
                    beatSeconds * 0.5f;

                int arpStep =
                    Mathf.FloorToInt(
                        t /
                        eighth);

                int[] arp =
                {
                    12,
                    19,
                    24,
                    19
                };

                float arpPhase =
                    Mathf.Repeat(
                        t,
                        eighth);

                float arpEnvelope =
                    Mathf.Exp(
                        -arpPhase *
                        7.5f);

                value +=
                    TriangleMidi(
                        rootMidi +
                        arp[
                            arpStep %
                            arp.Length],
                        t) *
                    arpEnvelope *
                    0.045f;

                // Soft kick on beats 1 and 3.
                int beatInBar =
                    beat % 4;

                if (beatInBar == 0 ||
                    beatInBar == 2)
                {
                    float kickEnvelope =
                        Mathf.Exp(
                            -phaseInBeat *
                            18f);

                    float kickFrequency =
                        56f +
                        40f *
                        Mathf.Exp(
                            -phaseInBeat *
                            16f);

                    value +=
                        Mathf.Sin(
                            2f *
                            Mathf.PI *
                            kickFrequency *
                            t) *
                        kickEnvelope *
                        0.16f;
                }

                // Tiny deterministic hi-hat texture.
                float halfBeat =
                    beatSeconds * 0.5f;

                float hatPhase =
                    Mathf.Repeat(
                        t,
                        halfBeat);

                float hatEnvelope =
                    Mathf.Exp(
                        -hatPhase *
                        42f);

                float noise =
                    HashNoise(
                        i);

                value +=
                    noise *
                    hatEnvelope *
                    0.018f;

                // Fade the loop edges to avoid clicks.
                float edge =
                    Mathf.Min(
                        t,
                        LoopSeconds - t);

                float edgeFade =
                    Mathf.Clamp01(
                        edge /
                        0.08f);

                samples[i] =
                    Mathf.Clamp(
                        value *
                        edgeFade,
                        -0.82f,
                        0.82f);
            }

            AudioClip clip =
                AudioClip.Create(
                    "Motor City Night Cruise",
                    sampleCount,
                    1,
                    SampleRate,
                    false);

            clip.SetData(
                samples,
                0);

            return clip;
        }

        private static float SineMidi(
            int midi,
            float time)
        {
            float frequency =
                440f *
                Mathf.Pow(
                    2f,
                    (midi - 69) /
                    12f);

            return
                Mathf.Sin(
                    2f *
                    Mathf.PI *
                    frequency *
                    time);
        }

        private static float TriangleMidi(
            int midi,
            float time)
        {
            float frequency =
                440f *
                Mathf.Pow(
                    2f,
                    (midi - 69) /
                    12f);

            float phase =
                Mathf.Repeat(
                    time *
                    frequency,
                    1f);

            return
                1f -
                4f *
                Mathf.Abs(
                    phase -
                    0.5f);
        }

        private static float HashNoise(
            int sample)
        {
            unchecked
            {
                uint x =
                    (uint)sample;

                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;

                return
                    (x /
                     (float)uint.MaxValue) *
                    2f -
                    1f;
            }
        }
    }
}
