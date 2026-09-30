using UnityEngine;

namespace MotorCity.Audio
{
    public sealed class MotorCitySfxRuntime : MonoBehaviour
    {
        private static MotorCitySfxRuntime instance;

        private AudioSource source;
        private AudioClip uiClick;
        private AudioClip success;
        private AudioClip failure;
        private AudioClip collision;
        private AudioClip countdownTick;
        private AudioClip countdownGo;
        private AudioClip newRecord;

        public static void PlayUiClick()
        {
            EnsureExists();
            instance.Play(instance.uiClick, 0.12f, 1f);
        }

        public static void PlayActivityResult(
            bool successful)
        {
            EnsureExists();

            instance.Play(
                successful
                    ? instance.success
                    : instance.failure,
                successful
                    ? 0.20f
                    : 0.16f,
                1f);
        }

        public static void PlayCountdownTick(
            int shown)
        {
            EnsureExists();

            float pitch =
                shown switch
                {
                    3 => 0.96f,
                    2 => 1.00f,
                    _ => 1.04f
                };

            instance.Play(
                instance.countdownTick,
                0.18f,
                pitch);
        }

        public static void PlayCountdownGo()
        {
            EnsureExists();

            instance.Play(
                instance.countdownGo,
                0.24f,
                1f);
        }

        public static void PlayNewRecord()
        {
            EnsureExists();

            instance.Play(
                instance.newRecord,
                0.26f,
                1f);
        }

        public static void PlayCollision(
            float normalizedStrength)
        {
            EnsureExists();

            float strength =
                Mathf.Clamp01(
                    normalizedStrength);

            if (strength < 0.08f)
                return;

            instance.Play(
                instance.collision,
                Mathf.Lerp(
                    0.04f,
                    0.24f,
                    strength),
                Mathf.Lerp(
                    1.12f,
                    0.78f,
                    strength));
        }

        private static void EnsureExists()
        {
            if (instance != null)
                return;

            instance =
                Object.FindAnyObjectByType<MotorCitySfxRuntime>();

            if (instance != null)
                return;

            GameObject host =
                new(
                    "Motor City SFX");

            DontDestroyOnLoad(
                host);

            instance =
                host.AddComponent<MotorCitySfxRuntime>();
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

            instance =
                this;

            DontDestroyOnLoad(
                gameObject);

            source =
                gameObject.AddComponent<AudioSource>();

            source.playOnAwake =
                false;
            source.loop =
                false;
            source.spatialBlend =
                0f;
            source.ignoreListenerPause =
                true;
            source.dopplerLevel =
                0f;
            source.priority =
                48;

            uiClick =
                BuildTone(
                    "Motor City UI Click",
                    920f,
                    0.038f,
                    0.42f,
                    0.10f);

            success =
                BuildTwoTone(
                    "Motor City Success",
                    620f,
                    880f,
                    0.18f,
                    0.34f);

            failure =
                BuildTwoTone(
                    "Motor City Failure",
                    410f,
                    270f,
                    0.20f,
                    0.30f);

            collision =
                BuildNoise(
                    "Motor City Collision",
                    0.16f);

            countdownTick =
                BuildTone(
                    "Motor City Countdown Tick",
                    760f,
                    0.075f,
                    0.34f,
                    0.02f);

            countdownGo =
                BuildTwoTone(
                    "Motor City Countdown GO",
                    720f,
                    1080f,
                    0.20f,
                    0.30f);

            newRecord =
                BuildTwoTone(
                    "Motor City New Record",
                    920f,
                    1320f,
                    0.26f,
                    0.34f);
        }

        private void Play(
            AudioClip clip,
            float volume,
            float pitch)
        {
            if (source == null ||
                clip == null)
            {
                return;
            }

            source.pitch =
                pitch;

            source.PlayOneShot(
                clip,
                Mathf.Clamp01(
                    volume));
        }

        private static AudioClip BuildTone(
            string name,
            float frequency,
            float seconds,
            float startAmplitude,
            float endAmplitude)
        {
            const int SampleRate =
                22050;

            int samples =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        SampleRate *
                        seconds));

            float[] data =
                new float[
                    samples];

            for (int i = 0;
                 i < samples;
                 i++)
            {
                float t =
                    i /
                    (float)Mathf.Max(
                        1,
                        samples - 1);

                float envelope =
                    Mathf.Lerp(
                        startAmplitude,
                        endAmplitude,
                        t);

                data[i] =
                    Mathf.Sin(
                        2f *
                        Mathf.PI *
                        frequency *
                        i /
                        SampleRate) *
                    envelope;
            }

            AudioClip clip =
                AudioClip.Create(
                    name,
                    samples,
                    1,
                    SampleRate,
                    false);

            clip.SetData(
                data,
                0);

            return clip;
        }

        private static AudioClip BuildTwoTone(
            string name,
            float firstFrequency,
            float secondFrequency,
            float seconds,
            float amplitude)
        {
            const int SampleRate =
                22050;

            int samples =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        SampleRate *
                        seconds));

            float[] data =
                new float[
                    samples];

            for (int i = 0;
                 i < samples;
                 i++)
            {
                float t =
                    i /
                    (float)Mathf.Max(
                        1,
                        samples - 1);

                float frequency =
                    t < 0.48f
                        ? firstFrequency
                        : secondFrequency;

                float envelope =
                    Mathf.Sin(
                        Mathf.PI *
                        t) *
                    amplitude;

                data[i] =
                    Mathf.Sin(
                        2f *
                        Mathf.PI *
                        frequency *
                        i /
                        SampleRate) *
                    envelope;
            }

            AudioClip clip =
                AudioClip.Create(
                    name,
                    samples,
                    1,
                    SampleRate,
                    false);

            clip.SetData(
                data,
                0);

            return clip;
        }

        private static AudioClip BuildNoise(
            string name,
            float seconds)
        {
            const int SampleRate =
                22050;

            int samples =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        SampleRate *
                        seconds));

            float[] data =
                new float[
                    samples];

            uint state =
                0x12345678u;

            float filtered =
                0f;

            for (int i = 0;
                 i < samples;
                 i++)
            {
                state =
                    state *
                    1664525u +
                    1013904223u;

                float raw =
                    ((state >> 8) /
                     16777215f) *
                    2f -
                    1f;

                filtered =
                    Mathf.Lerp(
                        filtered,
                        raw,
                        0.18f);

                float t =
                    i /
                    (float)Mathf.Max(
                        1,
                        samples - 1);

                float envelope =
                    Mathf.Pow(
                        1f - t,
                        1.8f);

                data[i] =
                    filtered *
                    envelope *
                    0.55f;
            }

            AudioClip clip =
                AudioClip.Create(
                    name,
                    samples,
                    1,
                    SampleRate,
                    false);

            clip.SetData(
                data,
                0);

            return clip;
        }
    }
}
