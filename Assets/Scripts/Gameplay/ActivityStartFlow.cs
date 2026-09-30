using System;
using MotorCity.Audio;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class ActivityStartFlow : MonoBehaviour
    {
        private ActivityManager activityManager;
        private MotorCityInterstitialRuntime interstitialRuntime;
        private bool requestInProgress;
        private const float GoPresentationSeconds =
            0.42f;

        private float countdownRemaining;
        private float goRemaining;
        private float pendingCountdownSeconds;
        private bool showingGo;
        private Action countdownStarted;
        private Action<int> countdownTick;
        private Action gameplayStarted;
        private int lastShownCountdown = -1;

        public bool RequestInProgress =>
            requestInProgress;

        public string PendingActivityId { get; private set; }

        public float CountdownRemaining =>
            Mathf.Max(
                0f,
                countdownRemaining);

        public bool IsCountdownPresentationActive =>
            requestInProgress &&
            (countdownRemaining > 0f ||
             showingGo);

        public int CountdownDisplayValue =>
            IsCountdownPresentationActive
                ? lastShownCountdown
                : -1;

        public void Initialize(
            ActivityManager manager)
        {
            activityManager =
                manager;
        }

        public void SetInterstitialRuntime(
            MotorCityInterstitialRuntime runtime)
        {
            interstitialRuntime =
                runtime;
        }

        private void Update()
        {
            if (!requestInProgress)
                return;

            if (showingGo)
            {
                goRemaining =
                    Mathf.Max(
                        0f,
                        goRemaining -
                        Time.deltaTime);

                if (goRemaining > 0f)
                    return;

                Action startGameplay =
                    gameplayStarted;

                ClearRequest();

                startGameplay?.Invoke();
                return;
            }

            if (countdownRemaining <= 0f)
                return;

            countdownRemaining =
                Mathf.Max(
                    0f,
                    countdownRemaining -
                    Time.deltaTime);

            if (countdownRemaining > 0f)
            {
                PublishCountdown();
                return;
            }

            showingGo =
                true;

            goRemaining =
                GoPresentationSeconds;

            lastShownCountdown =
                0;

            countdownTick?.Invoke(
                0);

            MotorCitySfxRuntime.PlayCountdownGo();
        }

        public bool RequestStart(
            string activityId,
            string displayName,
            Action beginPreparedActivity)
        {
            return RequestStart(
                activityId,
                displayName,
                0f,
                null,
                null,
                beginPreparedActivity);
        }

        public bool RequestStart(
            string activityId,
            string displayName,
            float countdownSeconds,
            Action onCountdownStarted,
            Action<int> onCountdownTick,
            Action onGameplayStarted)
        {
            if (requestInProgress ||
                activityManager == null ||
                string.IsNullOrWhiteSpace(
                    activityId) ||
                onGameplayStarted == null ||
                activityManager.IsActive(
                    activityId))
            {
                return false;
            }

            requestInProgress =
                true;

            PendingActivityId =
                activityId;

            if (!activityManager.TryBegin(
                    activityId,
                    displayName))
            {
                ClearRequest();
                return false;
            }

            pendingCountdownSeconds =
                Mathf.Max(
                    0f,
                    countdownSeconds);

            countdownStarted =
                onCountdownStarted;

            countdownTick =
                onCountdownTick;

            gameplayStarted =
                onGameplayStarted;

            ContinueAfterInterstitial(
                activityId);

            return true;
        }

        private void ContinueAfterInterstitial(
            string activityId)
        {
            if (interstitialRuntime == null)
            {
                ContinuePreparedStart(
                    activityId);

                return;
            }

            interstitialRuntime.ContinueBeforeActivity(
                activityId,
                () =>
                    ContinuePreparedStart(
                        activityId));
        }

        private void ContinuePreparedStart(
            string activityId)
        {
            if (!IsPending(
                    activityId))
            {
                return;
            }

            if (pendingCountdownSeconds <= 0f)
            {
                Action startGameplay =
                    gameplayStarted;

                ClearRequest();

                startGameplay?.Invoke();
                return;
            }

            countdownRemaining =
                Mathf.Max(
                    0.1f,
                    pendingCountdownSeconds);

            lastShownCountdown =
                -1;

            countdownStarted?.Invoke();

            PublishCountdown();
        }

        public bool IsPending(
            string activityId)
        {
            return
                requestInProgress &&
                PendingActivityId ==
                activityId;
        }

        public bool CancelPending(
            string activityId)
        {
            if (!IsPending(
                    activityId))
            {
                return false;
            }

            string cancelledId =
                PendingActivityId;

            ClearRequest();

            activityManager?.End(
                cancelledId);

            return true;
        }

        private void PublishCountdown()
        {
            int shown =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        countdownRemaining));

            if (shown ==
                lastShownCountdown)
            {
                return;
            }

            lastShownCountdown =
                shown;

            countdownTick?.Invoke(
                shown);

            MotorCitySfxRuntime.PlayCountdownTick(
                shown);
        }

        private void ClearRequest()
        {
            requestInProgress =
                false;

            PendingActivityId =
                null;

            countdownRemaining =
                0f;

            goRemaining =
                0f;

            showingGo =
                false;

            pendingCountdownSeconds =
                0f;

            countdownStarted =
                null;

            countdownTick =
                null;

            gameplayStarted =
                null;

            lastShownCountdown =
                -1;
        }
    }
}
