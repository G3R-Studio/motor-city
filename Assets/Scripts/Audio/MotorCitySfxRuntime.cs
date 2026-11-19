using UnityEngine;

namespace MotorCity.Audio
{
    public sealed class MotorCitySfxRuntime : MonoBehaviour
    {
        private static MotorCitySfxRuntime instance;

        private AudioSource source;
        private AudioClip uiClick;
        private float lastUiClickTime = -1f;
        private AudioClip success;
        private AudioClip failure;
        private AudioClip collision;
        private AudioClip countdownTick;
        private AudioClip countdownGo;
        private AudioClip newRecord;

        public static void PlayUiClick()
        {
            EnsureExists();
            // Avoid double sound from pointer-down and onClick handlers.
            if (Time.unscaledTime - instance.lastUiClickTime < 0.06f)
                return;
            instance.lastUiClickTime = Time.unscaledTime;
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
                gameObject.GetComponent<AudioSource>();

            if (source == null)
            {
                source =
                    gameObject.AddComponent<AudioSource>();
            }

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

            uiClick = Resources.Load<AudioClip>("MotorCity/Audio/ReleaseSfx/UiClick");
            success = Resources.Load<AudioClip>("MotorCity/Audio/ReleaseSfx/Success");
            failure = Resources.Load<AudioClip>("MotorCity/Audio/ReleaseSfx/Failure");
            collision = Resources.Load<AudioClip>("MotorCity/Audio/ReleaseSfx/Collision");
            countdownTick = Resources.Load<AudioClip>("MotorCity/Audio/ReleaseSfx/CountdownTick");
            countdownGo = Resources.Load<AudioClip>("MotorCity/Audio/ReleaseSfx/CountdownGo");
            newRecord = Resources.Load<AudioClip>("MotorCity/Audio/ReleaseSfx/NewRecord");
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Play(
            AudioClip clip,
            float volume,
            float pitch)
        {
            if (!MotorCityAudioGate.PlatformAudioEnabled || !MotorCity.Platform.MotorCityPlatformRuntime.IsGameplayResumeReady || source == null ||
                clip == null)
            {
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebAudio clip metadata is not safe to query before decoding.
            if (clip.loadState != AudioDataLoadState.Loaded)
                return;
#endif
            source.pitch =
                pitch;

            source.PlayOneShot(
                clip,
                Mathf.Clamp01(
                    volume));
        }

    }
}
