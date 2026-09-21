using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class MotorCityPlatformRuntime : MonoBehaviour
    {
        private bool gameplayRunning;
        private bool platformGameplayActive;

        private void Awake()
        {
            MotorCityLocalization.SetLanguage(
                "ru");

            MotorCityPlatform.Initialize(
                success =>
                {
                    if (!success)
                        return;

                    MotorCityLocalization.SetLanguage(
                        MotorCityPlatform.LanguageCode);
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
