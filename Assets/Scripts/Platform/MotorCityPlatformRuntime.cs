using System;
using MotorCity.Localization;
using MotorCity.Input;
using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class MotorCityPlatformRuntime : MonoBehaviour
    {
        [Flags]
        private enum PauseReason
        {
            None = 0,
            FocusLost = 1 << 0,
            ApplicationPaused = 1 << 1,
            PlatformModal = 1 << 2
        }

        private static MotorCityPlatformRuntime instance;

        private bool gameplayRunning;
        private bool platformGameplayActive;
        private bool initializationRequested;
        private bool localGameplayPaused;
        private float pausedTimeScale = 1f;
        private PauseReason pauseReasons;
        private Action<bool> pendingInitializeCallbacks;

        private void Awake()
        {
            instance = this;

            MotorCityInput.RefreshTouchPromptPreference();

            MotorCityLocalization.SetLanguage(
                "ru");
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void InitializePlatform(
            Action<bool> completed = null)
        {
            if (MotorCityPlatform.IsInitialized)
            {
                MotorCityLocalization.SetLanguage(
                    MotorCityPlatform.LanguageCode);

                completed?.Invoke(
                    true);

                return;
            }

            if (completed != null)
            {
                pendingInitializeCallbacks +=
                    completed;
            }

            if (initializationRequested)
                return;

            initializationRequested = true;

            MotorCityPlatform.Initialize(
                success =>
                {
                    initializationRequested = false;

                    if (success)
                    {
                        MotorCityLocalization.SetLanguage(
                            MotorCityPlatform.LanguageCode);
                    }

                    Action<bool> callbacks =
                        pendingInitializeCallbacks;

                    pendingInitializeCallbacks = null;

                    callbacks?.Invoke(
                        success);

                    ReconcilePlatformGameplay();
                });
        }

        private void OnApplicationFocus(
            bool hasFocus)
        {
            SetPauseReason(
                PauseReason.FocusLost,
                !hasFocus);
        }

        private void OnApplicationPause(
            bool paused)
        {
            SetPauseReason(
                PauseReason.ApplicationPaused,
                paused);
        }

        public static void SetPlatformModalPaused(
            bool paused)
        {
            if (instance == null)
            {
                return;
            }

            instance.SetPauseReason(
                PauseReason.PlatformModal,
                paused);
        }

        public void MarkGameplayRunning()
        {
            gameplayRunning = true;
            ReconcilePlatformGameplay();
        }

        private void SetPauseReason(
            PauseReason reason,
            bool active)
        {
            bool wasPaused =
                pauseReasons !=
                PauseReason.None;

            if (active)
            {
                pauseReasons |=
                    reason;
            }
            else
            {
                pauseReasons &=
                    ~reason;
            }

            bool isPaused =
                pauseReasons !=
                PauseReason.None;

            if (isPaused == wasPaused)
            {
                ReconcilePlatformGameplay();
                return;
            }

            if (isPaused)
            {
                ApplyLocalPause();
            }
            else
            {
                ReleaseLocalPause();
            }

            ReconcilePlatformGameplay();
        }

        private void ApplyLocalPause()
        {
            MotorCityInput.ClearVirtualState();

            if (localGameplayPaused)
                return;

            localGameplayPaused =
                true;

            pausedTimeScale =
                Time.timeScale;

            Time.timeScale =
                0f;

            AudioListener.pause =
                true;
        }

        private void ReleaseLocalPause()
        {
            if (!localGameplayPaused)
                return;

            localGameplayPaused =
                false;

            Time.timeScale =
                pausedTimeScale;

            AudioListener.pause =
                false;
        }

        private void ReconcilePlatformGameplay()
        {
            bool shouldBeActive =
                gameplayRunning &&
                pauseReasons ==
                    PauseReason.None;

            if (shouldBeActive ==
                platformGameplayActive)
            {
                return;
            }

            platformGameplayActive =
                shouldBeActive;

            if (!MotorCityPlatform.IsInitialized)
                return;

            if (shouldBeActive)
            {
                MotorCityPlatform.GameplayStart();
            }
            else
            {
                MotorCityPlatform.GameplayStop();
            }
        }
    }
}
