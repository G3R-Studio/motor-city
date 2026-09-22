using System;
using MotorCity.Localization;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class RewardedBonusSystem : MonoBehaviour
    {
        private const string DayKey = "MotorCity.Rewarded.Day";
        private const string CountKey = "MotorCity.Rewarded.Count";

        private PlayerWallet wallet;
        private ActivityManager activities;
        private long currentDay;
        private int watchedToday;
        private float cooldown;
        private float messageTimer;
        private float dayCheckTimer;

        private const float DayCheckInterval =
            1f;
        private bool requestRunning;

        public bool ShowMessage => messageTimer > 0f;
        public string StatusText { get; private set; }

        public bool CanRequest =>
            MotorCityPlatform.SupportsAds &&
            !requestRunning &&
            cooldown <= 0f &&
            watchedToday < DailyLimit() &&
            (activities == null ||
             (!activities.IsBusy &&
              !activities.HasResult));

        public string PromptLine
        {
            get
            {
                ResolveDay();

                if (!MotorCityPlatform.SupportsAds)
                    return MotorCityLocalization.Text(
                        "rewarded.yandex_only");

                if (watchedToday >= DailyLimit())
                    return MotorCityLocalization.Text(
                        "rewarded.limit");

                if (cooldown > 0f)
                    return MotorCityLocalization.Format(
                        "rewarded.cooldown",
                        Mathf.CeilToInt(cooldown));

                return MotorCityLocalization.Format(
                    "rewarded.prompt",
                    RewardCredits(),
                    watchedToday,
                    DailyLimit());
            }
        }

        public void Initialize(
            PlayerWallet targetWallet,
            ActivityManager activityManager)
        {
            wallet = targetWallet;
            activities = activityManager;
            ResolveDay();
        }

        private void Update()
        {
            dayCheckTimer -=
                Time.unscaledDeltaTime;

            if (dayCheckTimer <= 0f)
            {
                dayCheckTimer =
                    DayCheckInterval;

                ResolveDay();
            }

            if (cooldown > 0f)
                cooldown = Mathf.Max(
                    0f,
                    cooldown - Time.unscaledDeltaTime);

            if (messageTimer > 0f)
                messageTimer = Mathf.Max(
                    0f,
                    messageTimer - Time.unscaledDeltaTime);
        }

        public void TryShow()
        {
            ResolveDay();

            if (!CanRequest)
            {
                StatusText = PromptLine;
                messageTimer = 3.5f;
                return;
            }

            requestRunning = true;
            StatusText = MotorCityLocalization.Text(
                "rewarded.opening");
            messageTimer = 3f;

            MotorCityPlatform.ShowRewarded(
                "garage_bonus",
                rewarded =>
                {
                    requestRunning = false;

                    if (!rewarded)
                    {
                        StatusText =
                            MotorCityLocalization.Text(
                                "rewarded.no_reward");

                        messageTimer = 3.5f;
                        return;
                    }

                    int reward = RewardCredits();
                    wallet?.AddCredits(reward);
                    watchedToday++;

                    MotorCity.Persistence.MotorCitySaveService.SetInt(
                        CountKey,
                        watchedToday);

                    MotorCity.Persistence.MotorCitySaveService.Save();

                    cooldown = CooldownSeconds();

                    StatusText =
                        MotorCityLocalization.Format(
                            "rewarded.received",
                            reward);

                    messageTimer = 4.5f;
                });
        }

        private void ResolveDay()
        {
            long day =
                Math.Max(
                    1L,
                    MotorCityPlatform.ServerUnixTime /
                    86400L);

            if (day == currentDay)
                return;

            currentDay = day;

            long savedDay =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    DayKey,
                    0);

            if (savedDay == currentDay)
            {
                watchedToday =
                    Mathf.Max(
                        0,
                        MotorCity.Persistence.MotorCitySaveService.GetInt(
                            CountKey,
                            0));
                return;
            }

            watchedToday = 0;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DayKey,
                (int)Math.Min(
                    (long)int.MaxValue,
                    currentDay));

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                CountKey,
                0);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private static int RewardCredits()
        {
            return Mathf.Clamp(
                MotorCityRemoteConfigRuntime.GetInt(
                    "rewarded_credits",
                    250),
                50,
                2000);
        }

        private static int DailyLimit()
        {
            return Mathf.Clamp(
                MotorCityRemoteConfigRuntime.GetInt(
                    "rewarded_daily_limit",
                    5),
                1,
                10);
        }

        private static float CooldownSeconds()
        {
            return Mathf.Clamp(
                MotorCityRemoteConfigRuntime.GetFloat(
                    "rewarded_cooldown_seconds",
                    180f),
                60f,
                600f);
        }
    }
}
