using System;
using MotorCity.Localization;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class WeekendEventSystem : MonoBehaviour
    {
        private ActivityManager activities;
        private PlayerWallet wallet;
        private float messageTimer;
        private float stateCheckTimer;
        private long currentDay;
        private long currentWeek;

        private const float StateCheckInterval =
            1f;

        public bool IsActive { get; private set; }
        public bool ShowMessage => messageTimer > 0f;
        public string StatusText { get; private set; }

        public string EventName =>
            MotorCityLocalization.Text(EventNameKey());

        public string HudLine =>
            !IsActive
                ? string.Empty
                : MotorCityLocalization.Format(
                    "weekend.hud",
                    EventName,
                    BonusCredits());

        public void Initialize(
            ActivityManager activityManager,
            PlayerWallet targetWallet)
        {
            activities = activityManager;
            wallet = targetWallet;

            if (activities != null)
                activities.ActivityResultShown += OnActivityResult;

            RefreshState(true);
        }

        private void Update()
        {
            if (messageTimer > 0f)
                messageTimer = Mathf.Max(
                    0f,
                    messageTimer - Time.unscaledDeltaTime);

            stateCheckTimer -=
                Time.unscaledDeltaTime;

            if (stateCheckTimer > 0f)
                return;

            stateCheckTimer =
                StateCheckInterval;

            long day =
                Math.Max(
                    1L,
                    MotorCityPlatform.ServerUnixTime /
                    86400L);

            if (day != currentDay)
                RefreshState(true);
        }

        private void OnDestroy()
        {
            if (activities != null)
                activities.ActivityResultShown -= OnActivityResult;
        }

        private void RefreshState(bool announce)
        {
            DateTimeOffset date =
                DateTimeOffset.FromUnixTimeSeconds(
                    Math.Max(
                        1L,
                        MotorCityPlatform.ServerUnixTime));

            IsActive =
                date.DayOfWeek == DayOfWeek.Saturday ||
                date.DayOfWeek == DayOfWeek.Sunday;

            long serverTime =
                Math.Max(
                    1L,
                    MotorCityPlatform.ServerUnixTime);

            currentDay =
                serverTime /
                86400L;

            currentWeek =
                serverTime /
                604800L;

            if (IsActive && announce)
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "weekend.started",
                        EventName,
                        BonusCredits());

                messageTimer = 5f;
            }
        }

        private void OnActivityResult(string activityId, bool success)
        {
            if (!success ||
                !IsActive ||
                !MatchesEvent(activityId))
            {
                return;
            }

            int bonus = BonusCredits();

            wallet?.AddCredits(bonus);

            StatusText =
                MotorCityLocalization.Format(
                    "weekend.reward",
                    EventName,
                    bonus);

            messageTimer = 4f;
        }

        private bool MatchesEvent(string activityId)
        {
            return EventType() switch
            {
                0 => activityId == "drift",
                1 => activityId == "delivery" ||
                     (activityId != null &&
                      activityId.StartsWith(
                          "profession_",
                          StringComparison.Ordinal)),
                _ => activityId == "sprint" ||
                     activityId == "circuit"
            };
        }

        private int EventType()
        {
            return
                (int)(Math.Abs(currentWeek) % 3L);
        }

        private string EventNameKey()
        {
            return EventType() switch
            {
                0 => "weekend.drift",
                1 => "weekend.helpers",
                _ => "weekend.speed"
            };
        }

        private static int BonusCredits()
        {
            return Mathf.Clamp(
                MotorCityRemoteConfigRuntime.GetInt(
                    "weekend_bonus_credits",
                    180),
                50,
                1000);
        }
    }
}
