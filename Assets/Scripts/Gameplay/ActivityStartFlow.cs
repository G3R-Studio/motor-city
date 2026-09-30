using System;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class ActivityStartFlow : MonoBehaviour
    {
        private ActivityManager activityManager;
        private bool requestInProgress;
        private float countdownRemaining;
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

        public void Initialize(
            ActivityManager manager)
        {
            activityManager =
                manager;
        }

        private void Update()
        {
            if (!requestInProgress ||
                countdownRemaining <= 0f)
            {
                return;
            }

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

            Action startGameplay =
                gameplayStarted;

            ClearRequest();

            startGameplay?.Invoke();
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
                beginPreparedActivity,
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

            if (countdownSeconds <= 0f)
            {
                Action startGameplay =
                    onGameplayStarted;

                ClearRequest();

                startGameplay();
                return true;
            }

            countdownRemaining =
                Mathf.Max(
                    0.1f,
                    countdownSeconds);

            countdownStarted =
                onCountdownStarted;

            countdownTick =
                onCountdownTick;

            gameplayStarted =
                onGameplayStarted;

            lastShownCountdown =
                -1;

            countdownStarted?.Invoke();

            PublishCountdown();

            return true;
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
        }

        private void ClearRequest()
        {
            requestInProgress =
                false;

            PendingActivityId =
                null;

            countdownRemaining =
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
