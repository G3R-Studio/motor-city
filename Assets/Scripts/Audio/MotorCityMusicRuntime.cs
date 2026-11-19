using System.Collections;
using UnityEngine.Networking;
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

        private static readonly string[] MenuTrackPaths =
        {
            "MotorCity/Music/menu_01",
            "MotorCity/Music/menu_02"
        };

        private static readonly string[] CityTrackPaths =
        {
            "MotorCity/Music/track_01",
            "MotorCity/Music/track_02",
            "MotorCity/Music/track_03",
            "MotorCity/Music/track_04",
            "MotorCity/Music/track_05",
            "MotorCity/Music/track_06",
            "MotorCity/Music/track_07",
            "MotorCity/Music/track_08",
            "MotorCity/Music/track_09",
            "MotorCity/Music/track_10",
            "MotorCity/Music/track_11",
            "MotorCity/Music/track_12",
            "MotorCity/Music/track_13",
            "MotorCity/Music/track_14"
        };

        private static MotorCityMusicRuntime instance;

        private AudioSource source;
        private AudioClip[] menuClips;
        private bool menuDownloadPending;
        private float nextMenuRetryTime;
        private int[] menuOrder;
        private int menuOrderIndex;
        private int lastMenuTrackIndex = -1;
        private AudioClip[] cityClips;
        private bool cityDownloadPending;
        private float nextCityRetryTime;
        private int[] cityOrder;
        private int cityOrderIndex;
        private int lastCityTrackIndex = -1;

        private float musicVolume = 0.10f;
        private bool musicMuted;
        private bool menuActive;
        private bool gameplayActive;
        private bool systemPaused;
        private bool pauseMenuPaused;

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

        public static void SetSystemPaused(
            bool paused)
        {
            if (instance == null)
            {
                if (!paused)
                    return;

                EnsureExists();
            }

            if (instance == null ||
                instance.systemPaused ==
                    paused)
            {
                return;
            }

            instance.systemPaused =
                paused;

            instance.RefreshRuntimePause();
        }

        public static void SetPauseMenuPaused(
            bool paused)
        {
            if (instance == null)
            {
                if (!paused)
                    return;

                EnsureExists();
            }

            if (instance == null ||
                instance.pauseMenuPaused ==
                    paused)
            {
                return;
            }

            instance.pauseMenuPaused =
                paused;

            instance.RefreshRuntimePause();
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

        private void OnDestroy()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (instance == this) MotorCityMusicStop();
#endif
            if (menuClips != null)
                foreach (AudioClip clip in menuClips)
                    if (clip != null) Destroy(clip);
            if (cityClips != null)
                foreach (AudioClip clip in cityClips)
                    if (clip != null) Destroy(clip);
            if (instance == this)
            {
                instance =
                    null;
            }
        }

        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            UpdateWebMusic();
            return;
#endif
            if (musicMuted ||
                IsRuntimePaused ||
                source == null)
            {
                return;
            }

            if (menuActive)
            {
                if (!source.isPlaying)
                {
                    PlayNextMenuTrack();
                }

                return;
            }

            if (gameplayActive)
            {
                if (!source.isPlaying || !IsCurrentCityClip())
                    PlayNextCityTrack();
                else if (!cityDownloadPending)
                    PrefetchCityTrack();
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
            menuClips =
                new AudioClip[
                    MenuTrackPaths.Length];

            menuOrder =
                new int[
                    menuClips.Length];

            ShuffleMenuOrder();

            cityClips =
                new AudioClip[
                    CityTrackPaths.Length];

            // City music lives in StreamingAssets, never in the initial Resources payload.

            cityOrder =
                new int[
                    cityClips.Length];

            ShuffleCityOrder();
        }

        private void BuildSource()
        {
            source =
                gameObject.GetComponent<AudioSource>();

            if (source == null)
            {
                source =
                    gameObject.AddComponent<AudioSource>();
            }

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
#if UNITY_WEBGL && !UNITY_EDITOR
            UpdateWebMusic();
            return;
#endif
            if (source == null)
                return;

            if (IsRuntimePaused)
            {
                if (source.isPlaying)
                {
                    source.Pause();
                }

                return;
            }

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
                if (!IsCurrentMenuClip() ||
                    !source.isPlaying)
                {
                    PlayNextMenuTrack();
                }

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

        private void PlayNextMenuTrack()
        {
            if (source == null ||
                menuClips == null ||
                menuClips.Length == 0)
            {
                return;
            }

            int attempts =
                menuClips.Length;

            while (attempts-- > 0)
            {
                if (menuOrderIndex >=
                    menuOrder.Length)
                {
                    ShuffleMenuOrder();
                }

                int trackIndex =
                    menuOrder[
                        menuOrderIndex++];

                AudioClip clip =
                    menuClips[
                        trackIndex];

                if (clip == null)
                {
                    menuOrderIndex--;
                    if (!menuDownloadPending && Time.realtimeSinceStartup >= nextMenuRetryTime)
                    {
                        menuDownloadPending = true;
                        StartCoroutine(DownloadMenuTrack(trackIndex));
                    }
                    return;
                }

#if UNITY_WEBGL && !UNITY_EDITOR
                if (!EnsureClipReadyForWeb(
                        clip))
                {
                    continue;
                }
#endif

                source.Stop();
                source.loop = false;
                source.clip =
                    clip;

                lastMenuTrackIndex =
                    trackIndex;

                source.Play();
                return;
            }
        }

        private IEnumerator DownloadMenuTrack(int index)
        {
            string url = Application.streamingAssetsPath.TrimEnd('/') + "/" + MenuTrackPaths[index] + ".ogg";
            if (!url.Contains("://") && !url.StartsWith("jar:"))
                url = new System.Uri(System.IO.Path.GetFullPath(url)).AbsoluteUri;
            using (var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
            {
                request.timeout = 30;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    menuClips[index] = DownloadHandlerAudioClip.GetContent(request);
            }
            menuDownloadPending = false;
            if (menuClips[index] == null)
            {
                nextMenuRetryTime = Time.realtimeSinceStartup + 5f;
                menuOrderIndex++;
            }
        }

        private bool IsCurrentMenuClip()
        {
            if (source == null ||
                source.clip == null ||
                menuClips == null)
            {
                return false;
            }

            for (int i = 0;
                 i < menuClips.Length;
                 i++)
            {
                if (source.clip ==
                    menuClips[i])
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void MotorCityMusicTick(string url, string nextUrl, int active, int paused, float volume);
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int MotorCityMusicState();
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void MotorCityMusicStop();

        private string webTrack = "";
        private string webNext = "";
        private float webRetry;

        private bool webMenu;
        private string WebUrl(int index) =>
            Application.streamingAssetsPath.TrimEnd('/') + "/" + (menuActive ? MenuTrackPaths[index] : CityTrackPaths[index]) + ".ogg";

        private int NextWebIndex()
        {
            if (!menuActive) return NextCityIndex();
            if (menuOrderIndex >= menuOrder.Length) ShuffleMenuOrder();
            return menuOrder[menuOrderIndex];
        }

        private void UpdateWebMusic()
        {
            if ((!gameplayActive && !menuActive) || webMenu != menuActive)
            {
                if (webTrack != "")
                {
                    MotorCityMusicStop();
                    webTrack = webNext = "";
                }
                webMenu = menuActive;
                webRetry = 0f;
                if (!gameplayActive && !menuActive) return;
            }

            if (source != null && source.clip != null)
            {
                source.Stop();
                source.clip = null;
            }

            bool paused = IsRuntimePaused || musicMuted || !MotorCityAudioGate.PlatformAudioEnabled;
            if (!paused && Time.realtimeSinceStartup >= webRetry)
            {
                int state = MotorCityMusicState();
                if (webTrack == "" || state == 2 || state == 3)
                {
                    int i = NextWebIndex();
                    webTrack = WebUrl(i);
                    if (menuActive) { menuOrderIndex++; lastMenuTrackIndex = i; }
                    else { cityOrderIndex++; lastCityTrackIndex = i; }
                    webNext = WebUrl(NextWebIndex());
                    if (state == 3) webRetry = Time.realtimeSinceStartup + 3f;
                }
            }
            MotorCityMusicTick(webTrack, webNext, 1, paused || Time.realtimeSinceStartup < webRetry ? 1 : 0,
                musicVolume * 0.03f);
        }
#endif

        private int NextCityIndex()
        {
            if (cityOrderIndex >= cityOrder.Length)
                ShuffleCityOrder();
            return cityOrder[cityOrderIndex];
        }

        private void PrefetchCityTrack()
        {
            if (!gameplayActive || musicMuted || IsRuntimePaused ||
                cityDownloadPending || Time.realtimeSinceStartup < nextCityRetryTime)
                return;

            int index = NextCityIndex();
            if (cityClips[index] == null)
            {
                cityDownloadPending = true;
                StartCoroutine(DownloadCityTrack(index));
            }
        }

        private IEnumerator DownloadCityTrack(int index)
        {
            string url = Application.streamingAssetsPath.TrimEnd('/') +
                "/" + CityTrackPaths[index] + ".ogg";
            if (!url.Contains("://") && !url.StartsWith("jar:"))
                url = new System.Uri(System.IO.Path.GetFullPath(url)).AbsoluteUri;

            using (UnityWebRequest request =
                UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
            {
                request.timeout = 30;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip != null)
                    {
                        cityClips[index] = clip;
                        cityDownloadPending = false;
                        yield break;
                    }
                }

                Debug.LogWarning("Motor City: music download failed: " +
                    CityTrackPaths[index] + " (" + request.error + ")");
            }

            // Music is optional: failed downloads must never block driving.
            cityDownloadPending = false;
            nextCityRetryTime = Time.realtimeSinceStartup + 5f;
            // Try another song instead of looping forever on one bad file.
            if (cityOrderIndex < cityOrder.Length && cityOrder[cityOrderIndex] == index)
                cityOrderIndex++;
        }

        private void PlayNextCityTrack()
        {
            if (source == null || cityClips == null || cityOrder == null ||
                !gameplayActive || musicMuted || IsRuntimePaused)
                return;

            if (!IsCurrentCityClip() && source.isPlaying)
                source.Stop();

            int index = NextCityIndex();
            AudioClip clip = cityClips[index];
            if (clip == null)
            {
                PrefetchCityTrack();
                return;
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
                return;

            source.Stop();
            source.loop = false;
            source.clip = clip;
            cityOrderIndex++;
            lastCityTrackIndex = index;
            source.Play();

            // Retain only the active track and the next prefetched track.
            for (int i = 0; i < cityClips.Length; i++)
            {
                if (i == index || cityClips[i] == null)
                    continue;
                Destroy(cityClips[i]);
                cityClips[i] = null;
            }
        }

        private static bool EnsureClipReadyForWeb(
            AudioClip clip)
        {
            if (clip == null)
                return false;

            if (clip.loadState ==
                AudioDataLoadState.Loaded)
            {
                return true;
            }

            // WebGL clips are configured with preloadAudioData.
            // Do not call LoadAudioData() manually here: before the browser
            // AudioContext is unlocked Unity WebAudio queries clip metadata
            // and produces repeated "sound is not loaded yet" warnings.
            return false;
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

        private void ShuffleMenuOrder()
        {
            if (menuOrder == null ||
                menuOrder.Length == 0)
            {
                return;
            }

            for (int i = 0;
                 i < menuOrder.Length;
                 i++)
            {
                menuOrder[i] =
                    i;
            }

            for (int i =
                     menuOrder.Length - 1;
                 i > 0;
                 i--)
            {
                int swapIndex =
                    Random.Range(
                        0,
                        i + 1);

                int temp =
                    menuOrder[i];

                menuOrder[i] =
                    menuOrder[swapIndex];

                menuOrder[swapIndex] =
                    temp;
            }

            if (menuOrder.Length > 1 &&
                menuOrder[0] ==
                lastMenuTrackIndex)
            {
                int swapIndex =
                    Random.Range(
                        1,
                        menuOrder.Length);

                int temp =
                    menuOrder[0];

                menuOrder[0] =
                    menuOrder[swapIndex];

                menuOrder[swapIndex] =
                    temp;
            }

            menuOrderIndex =
                0;
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

        private bool IsRuntimePaused =>
            systemPaused ||
            pauseMenuPaused;

        private void RefreshRuntimePause()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            UpdateWebMusic();
            return;
#endif
            if (source == null)
                return;

            if (IsRuntimePaused)
            {
                if (source.isPlaying)
                {
                    source.Pause();
                }

                return;
            }

            if (musicMuted)
                return;

            // Resume the exact same clip/time when possible. If no clip was
            // active yet, RefreshPlayback will start the appropriate menu or
            // city music normally.
            if (source.clip != null)
            {
                source.UnPause();
            }

            if (!source.isPlaying)
            {
                RefreshPlayback();
            }
        }

        // Music intentionally bypasses the user's effects-volume listener.
        // Respect platform-wide audio disable independently of that preference.
        internal static void RefreshPlatformAudio()
        {
            if (instance != null)
                instance.ApplyVolume();
        }

        private void ApplyVolume()
        {
            if (source == null)
                return;

#if UNITY_WEBGL && !UNITY_EDITOR
            UpdateWebMusic();
#endif
            source.volume =
                musicMuted || !MotorCityAudioGate.PlatformAudioEnabled
                    ? 0f
                    : musicVolume * 0.03f;
        }
    }
}
