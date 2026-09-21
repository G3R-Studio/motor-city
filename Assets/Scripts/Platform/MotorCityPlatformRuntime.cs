using System;
using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class MotorCityPlatformRuntime : MonoBehaviour
    {
        private bool gameplayRunning;
        private bool platformGameplayActive;
        private bool initializationRequested;
        private Action<bool> pendingInitializeCallbacks;

        private void Awake()
        {
            MotorCityLocalization.SetLanguage(
                "ru");
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
                });
        }

        private void OnApplicationFocus(
            bool hasFocus)
        {
            if (!MotorCityPlatform.IsInitialized)
                return;

            if (hasFocus)
            {
                ResumeGameplay();
            }
            else
            {
                PauseGameplay();
            }
        }

        private void OnApplicationPause(
            bool paused)
        {
            if (!MotorCityPlatform.IsInitialized)
                return;

            if (paused)
            {
                PauseGameplay();
            }
            else
            {
                ResumeGameplay();
            }
        }

        public void MarkGameplayRunning()
        {
            gameplayRunning = true;
            platformGameplayActive = true;
        }

        private void ResumeGameplay()
        {
            if (!gameplayRunning ||
                platformGameplayActive)
            {
                return;
            }

            platformGameplayActive = true;
            MotorCityPlatform.GameplayStart();
        }

        private void PauseGameplay()
        {
            if (!gameplayRunning ||
                !platformGameplayActive)
            {
                return;
            }

            platformGameplayActive = false;
            MotorCityPlatform.GameplayStop();
        }
    }
}
