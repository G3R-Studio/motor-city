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

        private static readonly string[] CityTrackPaths =
        {
            "MotorCity/Music/track_01",
            "MotorCity/Music/track_02",
            "MotorCity/Music/track_03",
            "MotorCity/Music/track_04",
            "MotorCity/Music/track_05"
        };

        private static MotorCityMusicRuntime instance;

        private AudioSource source;
        private AudioClip menuClip;
        private AudioClip[] cityClips;
        private int[] cityOrder;
        private int cityOrderIndex;
        private int lastCityTrackIndex = -1;

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
                    "Motor City Music");

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

            LoadSettings();
            LoadMusic();
            BuildSource();
        }

        private void Update()
        {
            if (!gameplayActive ||
                menuActive ||
                musicMuted ||
                source == null)
            {
                return;
            }

            if (!source.isPlaying)
            {
                PlayNextCityTrack();
            }
        }

        private void LoadSettings()
        {
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
        }

        private void LoadMusic()
        {
            menuClip =
                Resources.Load<AudioClip>(
                    "MotorCity/Music/menu_01");

            cityClips =
                new AudioClip[
                    CityTrackPaths.Length];

            for (int i = 0;
                 i < CityTrackPaths.Length;
                 i++)
            {
                cityClips[i] =
                    Resources.Load<AudioClip>(
                        CityTrackPaths[i]);
            }

            cityOrder =
                new int[
                    cityClips.Length];

            ShuffleCityOrder();
        }

        private void BuildSource()
        {
            source =
                gameObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.ignoreListenerVolume = true;
            source.dopplerLevel = 0f;
            source.priority = 32;

            ApplyVolume();
        }

        private void RefreshPlayback()
        {
            if (source == null)
                return;

            if (musicMuted)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                }

                return;
            }

            if (menuActive)
            {
                PlayMenuMusic();
                return;
            }

            if (gameplayActive)
            {
                if (!IsCurrentCityClip() ||
                    !source.isPlaying)
                {
                    PlayNextCityTrack();
                }

                return;
            }

            if (source.isPlaying)
            {
                source.Stop();
            }
        }

        private void PlayMenuMusic()
        {
            if (source == null ||
                menuClip == null)
            {
                return;
            }

            bool alreadyPlayingMenu =
                source.clip == menuClip &&
                source.isPlaying;

            source.loop = true;

            if (alreadyPlayingMenu)
                return;

            source.Stop();
            source.clip =
                menuClip;

            source.Play();
        }

        private void PlayNextCityTrack()
        {
            if (source == null ||
                cityClips == null ||
                cityClips.Length == 0)
            {
                return;
            }

            int attempts =
                cityClips.Length;

            while (attempts-- > 0)
            {
                if (cityOrderIndex >=
                    cityOrder.Length)
                {
                    ShuffleCityOrder();
                }

                int trackIndex =
                    cityOrder[
                        cityOrderIndex++];

                AudioClip clip =
                    cityClips[
                        trackIndex];

                if (clip == null)
                    continue;

                source.Stop();
                source.loop = false;
                source.clip =
                    clip;

                lastCityTrackIndex =
                    trackIndex;

                source.Play();
                return;
            }
        }

        private bool IsCurrentCityClip()
        {
            if (source == null ||
                source.clip == null ||
                cityClips == null)
            {
                return false;
            }

            for (int i = 0;
                 i < cityClips.Length;
                 i++)
            {
                if (source.clip ==
                    cityClips[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void ShuffleCityOrder()
        {
            if (cityOrder == null ||
                cityOrder.Length == 0)
            {
                return;
            }

            for (int i = 0;
                 i < cityOrder.Length;
                 i++)
            {
                cityOrder[i] =
                    i;
            }

            for (int i =
                     cityOrder.Length - 1;
                 i > 0;
                 i--)
            {
                int swapIndex =
                    Random.Range(
                        0,
                        i + 1);

                int temp =
                    cityOrder[i];

                cityOrder[i] =
                    cityOrder[swapIndex];

                cityOrder[swapIndex] =
                    temp;
            }

            // Avoid the same song at the seam between two completed cycles.
            if (cityOrder.Length > 1 &&
                cityOrder[0] ==
                lastCityTrackIndex)
            {
                int swapIndex =
                    Random.Range(
                        1,
                        cityOrder.Length);

                int temp =
                    cityOrder[0];

                cityOrder[0] =
                    cityOrder[swapIndex];

                cityOrder[swapIndex] =
                    temp;
            }

            cityOrderIndex =
                0;
        }

        private void ApplyVolume()
        {
            if (source == null)
                return;

            source.volume =
                musicMuted
                    ? 0f
                    : musicVolume * 0.45f;
        }
    }
}
