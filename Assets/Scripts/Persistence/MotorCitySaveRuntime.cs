using UnityEngine;

namespace MotorCity.Persistence
{
    public sealed class MotorCitySaveRuntime : MonoBehaviour
    {
        private const float FlushIntervalSeconds =
            5f;

        private float flushTimer;

        private void Update()
        {
            if (!MotorCitySaveService.IsDirty)
            {
                flushTimer = 0f;
                return;
            }

            flushTimer +=
                Time.unscaledDeltaTime;

            if (flushTimer <
                FlushIntervalSeconds)
            {
                return;
            }

            flushTimer = 0f;

            MotorCitySaveService.FlushNow();
        }

        private void OnApplicationPause(
            bool paused)
        {
            if (paused)
            {
                MotorCitySaveService.FlushNow();
            }
        }

        private void OnApplicationQuit()
        {
            MotorCitySaveService.FlushNow();
        }

        private void OnDestroy()
        {
            MotorCitySaveService.FlushNow();
        }
    }
}
