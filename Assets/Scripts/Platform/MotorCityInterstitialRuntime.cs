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
        private const float RequestWatchdogSeconds = 12f;

        private bool requestRunning;
        private float requestDeadlineRealtime;
        private int requestSerial;
        private Action requestCompleted;
        private bool waitingForGameplayResume;
        private Action resumeCompleted;
#if UNITY_EDITOR || DEBUG
        private bool forceNextEditorMock;
        private float editorMockCloseRealtime;
        private Action editorMockCompleted;
#endif

        public InterstitialState State { get; private set; } =
            InterstitialState.Idle;

        public string AdminLine =>
            State +
            " | starts " +
            activityStartRequests +
            " | completed " +
            completedActivities +
            " | cooldown " +
            Mathf.Max(
                0f,
                nextAllowedRealtime -
                Time.realtimeSinceStartup).ToString(
                    "0") +
            "s";

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

            requestDeadlineRealtime =
                0f;

            requestSerial =
                0;

            requestCompleted =
                null;

            waitingForGameplayResume =
                false;

            resumeCompleted =
                null;

            State =
                InterstitialState.Idle;
        }

        private void Update()
        {
            if (requestRunning &&
                requestDeadlineRealtime > 0f &&
                Time.realtimeSinceStartup >=
                    requestDeadlineRealtime)
            {
                FinishPendingRequest(
                    requestSerial);
            }

#if UNITY_EDITOR || DEBUG
            if (editorMockCompleted != null &&
                Time.realtimeSinceStartup >=
                    editorMockCloseRealtime)
            {
                Action callback =
                    editorMockCompleted;

                editorMockCompleted =
                    null;

                MotorCityPlatformRuntime.SetPlatformModalPaused(
                    false);

                FinishInterstitialClose(
                    callback);
            }
#endif

            if (waitingForGameplayResume &&
                MotorCityPlatformRuntime.IsGameplayResumeReady)
            {
                waitingForGameplayResume =
                    false;

                Action callback =
                    resumeCompleted;

                resumeCompleted =
                    null;

                State =
                    InterstitialState.Cooldown;

                callback?.Invoke();
            }

            if (!requestRunning &&
                !waitingForGameplayResume &&
                State ==
                    InterstitialState.Cooldown &&
                Time.realtimeSinceStartup >=
                    nextAllowedRealtime)
            {
                State =
                    InterstitialState.Idle;
            }
        }

        private void OnDestroy()
        {
            if (activities != null)
            {
                activities.ActivityCompleted -=
                    OnActivityCompleted;
            }

            requestCompleted =
                null;

            resumeCompleted =
                null;

            waitingForGameplayResume =
                false;
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

#if UNITY_EDITOR || DEBUG
        public void ForceNextEditorMock()
        {
            forceNextEditorMock =
                true;
        }
#endif

        public void ContinueBeforeActivity(
            string activityId,
            Action completed)
        {
            ActivityStartFlow flow =
                activities == null
                    ? null
                    : activities.StartFlow;

            if (flow != null &&
                flow.Phase !=
                    ActivityStartFlow.StartPhase.AwaitingAdClose)
            {
                completed?.Invoke();
                return;
            }

#if UNITY_EDITOR || DEBUG
            if (forceNextEditorMock &&
                IsExplicitAdActivity(
                    activityId))
            {
                forceNextEditorMock =
                    false;

                BeginEditorMock(
                    completed);

                return;
            }
#endif

            if (IsExplicitAdActivity(
                    activityId))
            {
                activityStartRequests++;
            }

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

            nextAllowedRealtime = Time.realtimeSinceStartup;

            State =
                InterstitialState.Requesting;

            State =
                InterstitialState.Showing;

            requestSerial++;
            int serial =
                requestSerial;

            requestCompleted =
                completed;

            // Bridge state callbacks provide completion; do not time out an open ad.
            requestDeadlineRealtime = 0f;

            MotorCityPlatform.ShowInterstitial(
                "before_activity",
                () =>
                    FinishPendingRequest(
                        serial));
        }

        private void FinishPendingRequest(
            int serial)
        {
            if (!requestRunning ||
                serial != requestSerial)
            {
                return;
            }

            Action completed =
                requestCompleted;

            requestCompleted =
                null;

            requestDeadlineRealtime =
                0f;

            FinishInterstitialClose(
                completed);
        }

        private void FinishInterstitialClose(
            Action completed)
        {
            State =
                InterstitialState.Closing;

            requestRunning =
                false;

            MotorCity.Input.MotorCityInput.ClearVirtualState();

            if (MotorCityPlatformRuntime.IsGameplayResumeReady)
            {
                State =
                    InterstitialState.Cooldown;

                completed?.Invoke();
                return;
            }

            waitingForGameplayResume =
                true;

            resumeCompleted =
                completed;
        }

#if UNITY_EDITOR || DEBUG
        private void BeginEditorMock(
            Action completed)
        {
            requestRunning =
                true;

            State =
                InterstitialState.Eligible;

            nextAllowedRealtime =
                Time.realtimeSinceStartup +
                CooldownSeconds();

            State =
                InterstitialState.Requesting;

            MotorCityPlatformRuntime.SetPlatformModalPaused(
                true);

            State =
                InterstitialState.Showing;

            editorMockCompleted =
                completed;

            editorMockCloseRealtime =
                Time.realtimeSinceStartup +
                1.5f;
        }
#endif

        private bool ShouldRequestInterstitial(
            string activityId)
        {
            // Bridge owns frequency, initial delay, and availability.
            return !requestRunning && activities != null &&
                !string.IsNullOrWhiteSpace(activityId) &&
                IsExplicitAdActivity(activityId) &&
                MotorCityPlatform.SupportsInterstitial &&
                !CosmeticStoreSystem.NoAdsOwned;
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
