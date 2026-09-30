using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class MotorCityInterstitialRuntime :
        MonoBehaviour
    {
        private ActivityManager activities;
        private float nextAllowedRealtime;
        private bool requestRunning;

        public void Initialize(
            ActivityManager activityManager)
        {
            if (activities != null)
            {
                activities.ActivityResultDismissed -=
                    OnActivityResultDismissed;
            }

            activities =
                activityManager;

            if (activities != null)
            {
                activities.ActivityResultDismissed +=
                    OnActivityResultDismissed;
            }

            nextAllowedRealtime =
                Time.realtimeSinceStartup +
                MinimumIntervalSeconds();
        }

        private void OnDestroy()
        {
            if (activities != null)
            {
                activities.ActivityResultDismissed -=
                    OnActivityResultDismissed;
            }
        }

        private void OnActivityResultDismissed(
            string activityId,
            bool success)
        {
            if (!success ||
                requestRunning ||
                activities == null ||
                activities.IsBusy ||
                activities.HasResult ||
                !activities.SecondaryProgressionAllowed ||
                !MotorCityPlatform.SupportsAds)
            {
                return;
            }

            float now =
                Time.realtimeSinceStartup;

            if (now <
                nextAllowedRealtime)
            {
                return;
            }

            requestRunning =
                true;

            // Reserve the next interval before opening the platform modal.
            // Even if the ad fails/gets closed immediately, the player will
            // not be spammed by another result dismissal.
            nextAllowedRealtime =
                now +
                MinimumIntervalSeconds();

            MotorCityPlatform.ShowInterstitial(
                "activity_result",
                () =>
                {
                    requestRunning =
                        false;

                    MotorCity.Input.MotorCityInput.ClearVirtualState();
                });
        }

        private static float MinimumIntervalSeconds()
        {
            return Mathf.Clamp(
                MotorCityRemoteConfigRuntime.GetFloat(
                    "interstitial_min_seconds",
                    240f),
                120f,
                1800f);
        }
    }
}
