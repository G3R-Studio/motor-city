using System;
using System.Collections.Generic;
using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class MotorCityAnalyticsRuntime :
        MonoBehaviour
    {
        private readonly Queue<StatIncrement> queue =
            new();

        private ActivityManager activities;
        private PlayerWallet wallet;
        private bool sending;
        private float sessionStartedAt;
        private float lastSessionFlushAt;

        public void Initialize(
            ActivityManager activityManager,
            PlayerWallet playerWallet)
        {
            activities =
                activityManager;
            wallet =
                playerWallet;

            sessionStartedAt =
                Time.realtimeSinceStartup;

            lastSessionFlushAt =
                sessionStartedAt;

            QueueIncrement(
                "sessions",
                1);

            if (activities != null)
            {
                activities.ActivityResultShown +=
                    OnActivityResult;
            }

            if (wallet != null)
            {
                wallet.CreditsEarned +=
                    OnCreditsEarned;

                wallet.CreditsSpent +=
                    OnCreditsSpent;
            }
        }

        private void Update()
        {
            if (sending ||
                queue.Count == 0 ||
                !MotorCityPlatform.IsInitialized)
            {
                return;
            }

            StatIncrement item =
                queue.Dequeue();

            sending = true;

            MotorCityPlatform.IncrementStat(
                item.Key,
                item.Amount,
                ignored =>
                {
                    sending = false;
                });
        }

        private void OnApplicationPause(
            bool paused)
        {
            if (paused)
            {
                QueueSessionSeconds();
            }
            else
            {
                lastSessionFlushAt =
                    Time.realtimeSinceStartup;
            }
        }

        private void OnDestroy()
        {
            if (activities != null)
            {
                activities.ActivityResultShown -=
                    OnActivityResult;
            }

            if (wallet != null)
            {
                wallet.CreditsEarned -=
                    OnCreditsEarned;

                wallet.CreditsSpent -=
                    OnCreditsSpent;
            }

            QueueSessionSeconds();
        }

        private void OnCreditsEarned(
            int amount)
        {
            QueueIncrement(
                "credits_earned",
                amount);
        }

        private void OnCreditsSpent(
            int amount)
        {
            QueueIncrement(
                "credits_spent",
                amount);
        }

        private void OnActivityResult(
            string activityId,
            bool success)
        {
            QueueIncrement(
                success
                    ? "activities_success"
                    : "activities_failed",
                1);

            if (!string.IsNullOrWhiteSpace(
                    activityId))
            {
                string normalized =
                    NormalizeKey(
                        activityId);

                QueueIncrement(
                    "activity_" +
                    normalized +
                    (success
                        ? "_success"
                        : "_failed"),
                    1);
            }
        }

        private void QueueSessionSeconds()
        {
            float now =
                Time.realtimeSinceStartup;

            int seconds =
                Mathf.Max(
                    0,
                    Mathf.RoundToInt(
                        now -
                        lastSessionFlushAt));

            lastSessionFlushAt =
                now;

            if (seconds > 0)
            {
                QueueIncrement(
                    "session_seconds",
                    seconds);
            }
        }

        private void QueueIncrement(
            string key,
            long amount)
        {
            if (string.IsNullOrWhiteSpace(
                    key) ||
                amount == 0L)
            {
                return;
            }

            queue.Enqueue(
                new StatIncrement(
                    key,
                    amount));
        }

        private static string NormalizeKey(
            string value)
        {
            System.Text.StringBuilder builder =
                new();

            foreach (char c in
                     value.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(
                        c))
                {
                    builder.Append(
                        c);
                }
                else if (builder.Length > 0 &&
                         builder[builder.Length - 1] != '_')
                {
                    builder.Append(
                        '_');
                }
            }

            return
                builder.ToString()
                    .Trim('_');
        }

        private readonly struct StatIncrement
        {
            public readonly string Key;
            public readonly long Amount;

            public StatIncrement(
                string key,
                long amount)
            {
                Key =
                    key;
                Amount =
                    amount;
            }
        }
    }
}
