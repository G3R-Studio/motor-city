using System;
using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class MotorCityInterstitialRuntime :
        MonoBehaviour
    {
        public enum InterstitialState
        {
            Idle,
            Eligible,
            Requesting,
            Showing,
            Closing,
            Cooldown
        }

        private ActivityManager activities;
        private float sessionStartedRealtime;
        private float nextAllowedRealtime;
        private int completedActivities;
        private int activityStartRequests;
        private bool requestRunning;

        public InterstitialState State { get; private set; } =
            InterstitialState.Idle;

        public void Initialize(
            ActivityManager activityManager)
        {
            if (activities != null)
            {
                activities.ActivityCompleted -=
                    OnActivityCompleted;
            }

            activities =
                activityManager;

            if (activities != null)
            {
                activities.ActivityCompleted +=
                    OnActivityCompleted;
            }

            sessionStartedRealtime =
                Time.realtimeSinceStartup;

            nextAllowedRealtime =
                sessionStartedRealtime;

            completedActivities =
                0;

            activityStartRequests =
                0;

            requestRunning =
                false;

            State =
                InterstitialState.Idle;
        }

        private void OnDestroy()
        {
            if (activities != null)
            {
                activities.ActivityCompleted -=
                    OnActivityCompleted;
            }
        }

        private void OnActivityCompleted(
            string activityId)
        {
            if (string.IsNullOrWhiteSpace(
                    activityId))
            {
                return;
            }

            completedActivities++;
        }

        public void ContinueBeforeActivity(
            string activityId,
            Action completed)
        {
            activityStartRequests++;

            if (!ShouldRequestInterstitial(
                    activityId))
            {
                completed?.Invoke();
                return;
            }

            requestRunning =
                true;

            State =
                InterstitialState.Eligible;

            float now =
                Time.realtimeSinceStartup;

            // Reserve cooldown on request, not on successful rendering.
            // This prevents repeated SDK calls when Yandex has no ad available.
            nextAllowedRealtime =
                now +
                CooldownSeconds();

            State =
                InterstitialState.Requesting;

            MotorCityPlatform.ShowInterstitial(
                "before_activity",
                () =>
                {
                    State =
                        InterstitialState.Closing;

                    requestRunning =
                        false;

                    MotorCity.Input.MotorCityInput.ClearVirtualState();

                    State =
                        InterstitialState.Cooldown;

                    completed?.Invoke();
                });

            State =
                InterstitialState.Showing;
        }

        private bool ShouldRequestInterstitial(
            string activityId)
        {
            if (requestRunning ||
                activities == null ||
                string.IsNullOrWhiteSpace(
                    activityId) ||
                !IsExplicitAdActivity(
                    activityId) ||
                !MotorCityRemoteConfigRuntime.GetBool(
                    "interstitial_enabled",
                    true) ||
                !MotorCityRemoteConfigRuntime.GetBool(
                    "interstitial_before_activity",
                    true) ||
                !MotorCityPlatform.SupportsAds)
            {
                return false;
            }

            if (MotorCityRemoteConfigRuntime.GetBool(
                    "interstitial_skip_rookie_path",
                    true) &&
                activities.IsOnboardingActive)
            {
                return false;
            }

            if (MotorCityRemoteConfigRuntime.GetBool(
                    "interstitial_skip_first_activity",
                    true) &&
                activityStartRequests <= 1)
            {
                return false;
            }

            float now =
                Time.realtimeSinceStartup;

            if (now -
                    sessionStartedRealtime <
                MinimumSessionSeconds())
            {
                return false;
            }

            if (completedActivities <
                MinimumCompletedActivities())
            {
                return false;
            }

            if (now <
                nextAllowedRealtime)
            {
                State =
                    InterstitialState.Cooldown;

                return false;
            }

            float chance =
                Mathf.Clamp01(
                    MotorCityRemoteConfigRuntime.GetFloat(
                        "interstitial_chance",
                        0.40f));

            if (UnityEngine.Random.value >
                chance)
            {
                State =
                    InterstitialState.Idle;

                return false;
            }

            return true;
        }

        private static bool IsExplicitAdActivity(
            string activityId)
        {
            return activityId switch
            {
                "delivery" => true,
                "sprint" => true,
                "circuit" => true,
                "drift" => true,
                "profession_pizza" => true,
                "profession_taxi" => true,
                "profession_mail" => true,
                "profession_icecream" => true,
                "profession_carwash" => true,
                "profession_tow" => true,
                "underground" => true,
                _ => false
            };
        }

        private static float CooldownSeconds()
        {
            return Mathf.Clamp(
                MotorCityRemoteConfigRuntime.GetFloat(
                    "interstitial_cooldown_seconds",
                    180f),
                60f,
                1800f);
        }

        private static float MinimumSessionSeconds()
        {
            return Mathf.Max(
                0f,
                MotorCityRemoteConfigRuntime.GetFloat(
                    "interstitial_min_session_seconds",
                    120f));
        }

        private static int MinimumCompletedActivities()
        {
            return Mathf.Max(
                0,
                MotorCityRemoteConfigRuntime.GetInt(
                    "interstitial_min_completed_activities",
                    1));
        }
    }
}
