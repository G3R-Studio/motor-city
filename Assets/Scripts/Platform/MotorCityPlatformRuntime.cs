using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class MotorCityPlatformRuntime : MonoBehaviour
    {
        private bool gameplayRunning;

        private void Awake()
        {
            MotorCityPlatform.Initialize();
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
        }

        private void ResumeGameplay()
        {
            if (!gameplayRunning)
                return;

            MotorCityPlatform.GameplayStart();
        }

        private void PauseGameplay()
        {
            if (!gameplayRunning)
                return;

            MotorCityPlatform.GameplayStop();
        }
    }
}
