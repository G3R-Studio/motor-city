using UnityEngine;

namespace MotorCity.Audio
{
    // Platform mute is temporary and must not change a saved user preference.
    // Both the HUD and main menu update the same user volume while the SDK
    // independently controls whether any game audio is allowed.
    public static class MotorCityAudioGate
    {
        private static float userVolume = 1f;
        public static bool PlatformAudioEnabled { get; private set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            userVolume = 1f;
            PlatformAudioEnabled = true;
        }

        public static void SetUserVolume(float volume)
        {
            userVolume = Mathf.Clamp01(volume);
            ApplyListenerVolume();
        }

        public static void SetPlatformAudioEnabled(bool enabled)
        {
            PlatformAudioEnabled = enabled;
            ApplyListenerVolume();
            MotorCityMusicRuntime.RefreshPlatformAudio();
        }

        private static void ApplyListenerVolume()
        {
            AudioListener.volume = PlatformAudioEnabled ? userVolume : 0f;
        }
    }
}
