using System;
using MotorCity.Localization;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class SeasonSystem : MonoBehaviour
    {
        private const int SeasonLengthDays = 42;
        private const string MissionKey = "MotorCity.Season1.Mission";
        private const string ProgressKey = "MotorCity.Season1.Progress";
        private const string CompleteKey = "MotorCity.Season1.Complete";
        private const float MessageSeconds = 4.5f;

        private static readonly long SeasonOneStartUnix =
            new DateTimeOffset(
                2026,
                9,
                21,
                0,
                0,
                0,
                TimeSpan.Zero)
            .ToUnixTimeSeconds();

        private ActivityManager activities;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private TurboPetSystem turbo;

        private SeasonMission[] missions;
        private int missionIndex;
        private int progress;
        private float messageTimer;

        public bool IsComplete { get; private set; }

        public bool IsSeasonOneActive
        {
            get
            {
                long now =
                    Math.Max(
                        SeasonOneStartUnix,
                        MotorCityPlatform.ServerUnixTime);

                return
                    now <
                    SeasonOneStartUnix +
                    SeasonLengthDays *
                    86400L;
            }
        }

        public int DaysRemaining
        {
            get
            {
                long end =
                    SeasonOneStartUnix +
                    SeasonLengthDays *
                    86400L;

                long seconds =
                    Math.Max(
                        0L,
                        end -
                        MotorCityPlatform.ServerUnixTime);

                return
                    Mathf.CeilToInt(
                        seconds /
                        86400f);
            }
        }

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public string ObjectiveLine
        {
            get
            {
                if (IsComplete)
                {
                    return
                        MotorCityLocalization.Text(
                            "season1.complete_hud");
                }

                if (!IsSeasonOneActive)
                {
                    return
                        MotorCityLocalization.Text(
                            "season1.ended");
                }

                SeasonMission mission =
                    CurrentMission();

                if (mission == null)
                    return string.Empty;

                return
                    MotorCityLocalization.Format(
                        "season1.hud",
                        missionIndex + 1,
                        missions.Length,
                        MotorCityLocalization.Text(
                            mission.TitleKey),
                        Mathf.Min(
                            progress,
                            mission.Target),
                        mission.Target,
                        DaysRemaining);
            }
        }

        public void Initialize(
            ActivityManager activityManager,
            PlayerWallet targetWallet,
            PlayerReputation targetReputation,
            TurboPetSystem turboSystem)
        {
            activities = activityManager;
            wallet = targetWallet;
            reputation = targetReputation;
            turbo = turboSystem;

            BuildMissions();

            IsComplete =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    CompleteKey,
                    0) != 0;

            missionIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        MissionKey,
                        0),
                    0,
                    missions.Length - 1);

            progress =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        ProgressKey,
                        0));

            if (activities != null)
                activities.ActivityResultShown += OnActivityResult;

            if (!IsComplete &&
                IsSeasonOneActive)
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "season1.welcome",
                        DaysRemaining);

                messageTimer =
                    MessageSeconds + 1f;
            }
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            if (activities != null)
                activities.ActivityResultShown -= OnActivityResult;
        }

        private void OnActivityResult(
            string activityId,
            bool success)
        {
            if (!success ||
                IsComplete ||
                !IsSeasonOneActive)
            {
                return;
            }

            SeasonMission mission =
                CurrentMission();

            if (mission == null ||
                !Matches(
                    mission.ActivityId,
                    activityId))
            {
                return;
            }

            progress =
                Mathf.Min(
                    mission.Target,
                    progress + 1);

            Save();

            if (progress <
                mission.Target)
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "season1.progress",
                        MotorCityLocalization.Text(
                            mission.TitleKey),
                        progress,
                        mission.Target);

                messageTimer =
                    MessageSeconds;
                return;
            }

            CompleteCurrent(
                mission);
        }

        private void CompleteCurrent(
            SeasonMission mission)
        {
            wallet?.AddCredits(
                mission.Credits);

            reputation?.AddReputation(
                mission.Reputation);

            turbo?.AddXp(
                mission.TurboXp);

            bool final =
                missionIndex >=
                missions.Length - 1;

            if (final)
            {
                IsComplete =
                    true;

                turbo?.UnlockSkin(
                    5);

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    CompleteKey,
                    1);

                MotorCity.Persistence.MotorCitySaveService.Save();

                StatusText =
                    MotorCityLocalization.Format(
                        "season1.final",
                        mission.Credits,
                        mission.Reputation);

                messageTimer =
                    7f;

                return;
            }

            StatusText =
                MotorCityLocalization.Format(
                    "season1.mission_complete",
                    missionIndex + 1,
                    mission.Credits,
                    mission.Reputation);

            messageTimer =
                MessageSeconds + 0.8f;

            missionIndex++;
            progress = 0;
            Save();
        }

        private SeasonMission CurrentMission()
        {
            if (missions == null ||
                missionIndex < 0 ||
                missionIndex >= missions.Length)
            {
                return null;
            }

            return
                missions[missionIndex];
        }

        private void Save()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                MissionKey,
                missionIndex);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                ProgressKey,
                progress);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private void BuildMissions()
        {
            missions =
                new[]
                {
                    new SeasonMission("season1.m1","delivery",1,420,45,18),
                    new SeasonMission("season1.m2","profession_pizza",1,480,50,20),
                    new SeasonMission("season1.m3","drift",1,520,55,22),
                    new SeasonMission("season1.m4","sprint",1,580,60,24),
                    new SeasonMission("season1.m5","profession_taxi",1,640,65,26),
                    new SeasonMission("season1.m6","circuit",1,720,75,28),
                    new SeasonMission("season1.m7","profession_mail",1,760,80,30),
                    new SeasonMission("season1.m8","profession_carwash",1,820,85,32),
                    new SeasonMission("season1.m9","*",3,1050,110,40),
                    new SeasonMission("season1.m10","circuit",2,2500,250,80)
                };
        }

        private static bool Matches(
            string expected,
            string actual)
        {
            if (expected == "*")
            {
                return
                    actual == "delivery" ||
                    actual == "drift" ||
                    actual == "sprint" ||
                    actual == "circuit" ||
                    (actual != null &&
                     actual.StartsWith(
                         "profession_",
                         StringComparison.Ordinal));
            }

            return
                expected ==
                actual;
        }

        private sealed class SeasonMission
        {
            public readonly string TitleKey;
            public readonly string ActivityId;
            public readonly int Target;
            public readonly int Credits;
            public readonly int Reputation;
            public readonly int TurboXp;

            public SeasonMission(
                string titleKey,
                string activityId,
                int target,
                int credits,
                int reputation,
                int turboXp)
            {
                TitleKey = titleKey;
                ActivityId = activityId;
                Target = Mathf.Max(1, target);
                Credits = Mathf.Max(0, credits);
                Reputation = Mathf.Max(0, reputation);
                TurboXp = Mathf.Max(0, turboXp);
            }
        }
    }
}
