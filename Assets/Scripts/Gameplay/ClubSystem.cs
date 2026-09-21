using System;
using MotorCity.Localization;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class ClubSystem : MonoBehaviour
    {
        private const string ClubKey =
            "MotorCity.Club.Selected";
        private const string WeekKey =
            "MotorCity.Club.Week";
        private const string ContributionKey =
            "MotorCity.Club.WeeklyContribution";
        private const string RewardKey =
            "MotorCity.Club.WeeklyRewardClaimed";

        private int WeeklyGoal =>
            Mathf.Clamp(
                MotorCityRemoteConfigRuntime.GetInt(
                    "club_weekly_goal",
                    12),
                3,
                30);

        private ActivityManager activities;
        private PlayerWallet wallet;
        private PlayerReputation reputation;

        private ClubDefinition[] clubs;
        private long currentWeek;
        private int weeklyContribution;
        private bool weeklyRewardClaimed;
        private float messageTimer;

        public int JoinedClubIndex { get; private set; } =
            -1;

        public int BrowseClubIndex { get; private set; }

        public bool HasClub =>
            JoinedClubIndex >= 0 &&
            clubs != null &&
            JoinedClubIndex < clubs.Length;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public string CurrentClubName =>
            HasClub
                ? MotorCityLocalization.Text(
                    clubs[JoinedClubIndex].NameKey)
                : MotorCityLocalization.Text(
                    "club.none");

        public string CurrentClubEmblem =>
            HasClub
                ? clubs[JoinedClubIndex].Emblem
                : "MC";

        public string BrowseClubName =>
            clubs == null ||
            clubs.Length == 0
                ? string.Empty
                : MotorCityLocalization.Text(
                    clubs[
                        Mathf.Clamp(
                            BrowseClubIndex,
                            0,
                            clubs.Length - 1)]
                        .NameKey);

        public string BrowseClubEmblem =>
            clubs == null ||
            clubs.Length == 0
                ? "MC"
                : clubs[
                    Mathf.Clamp(
                        BrowseClubIndex,
                        0,
                        clubs.Length - 1)]
                    .Emblem;

        public string BrowseClubDescription =>
            clubs == null ||
            clubs.Length == 0
                ? string.Empty
                : MotorCityLocalization.Text(
                    clubs[
                        Mathf.Clamp(
                            BrowseClubIndex,
                            0,
                            clubs.Length - 1)]
                        .DescriptionKey);

        public string WeeklyLine =>
            MotorCityLocalization.Format(
                weeklyRewardClaimed
                    ? "club.weekly_done"
                    : "club.weekly_progress",
                Mathf.Min(
                    weeklyContribution,
                    WeeklyGoal),
                WeeklyGoal);

        public int WeeklyContribution =>
            weeklyContribution;

        public int WeeklyTarget =>
            WeeklyGoal;

        public void Initialize(
            ActivityManager activityManager,
            PlayerWallet targetWallet,
            PlayerReputation targetReputation)
        {
            activities =
                activityManager;
            wallet =
                targetWallet;
            reputation =
                targetReputation;

            BuildClubs();

            JoinedClubIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        ClubKey,
                        -1),
                    -1,
                    clubs.Length - 1);

            BrowseClubIndex =
                JoinedClubIndex >= 0
                    ? JoinedClubIndex
                    : 0;

            ResolveWeek();

            if (activities != null)
            {
                activities.ActivityResultShown +=
                    OnActivityResult;
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

            long week =
                ResolveServerWeek();

            if (week !=
                currentWeek)
            {
                ResolveWeek();
            }
        }

        private void OnDestroy()
        {
            if (activities != null)
            {
                activities.ActivityResultShown -=
                    OnActivityResult;
            }
        }

        public void CycleBrowse(
            int direction)
        {
            if (clubs == null ||
                clubs.Length == 0)
            {
                return;
            }

            BrowseClubIndex =
                (BrowseClubIndex +
                 Math.Sign(
                     direction) +
                 clubs.Length) %
                clubs.Length;
        }

        public void JoinBrowseClub()
        {
            if (clubs == null ||
                clubs.Length == 0)
            {
                return;
            }

            JoinedClubIndex =
                Mathf.Clamp(
                    BrowseClubIndex,
                    0,
                    clubs.Length - 1);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                ClubKey,
                JoinedClubIndex);

            MotorCity.Persistence.MotorCitySaveService.Save();

            StatusText =
                MotorCityLocalization.Format(
                    "club.joined",
                    CurrentClubName);

            messageTimer =
                4.5f;
        }

        private void OnActivityResult(
            string activityId,
            bool success)
        {
            if (!success ||
                !HasClub ||
                weeklyRewardClaimed)
            {
                return;
            }

            weeklyContribution =
                Mathf.Min(
                    WeeklyGoal,
                    weeklyContribution + 1);

            SaveWeek();

            if (weeklyContribution <
                WeeklyGoal)
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "club.contribution",
                        CurrentClubName,
                        weeklyContribution,
                        WeeklyGoal);

                messageTimer =
                    3.5f;

                return;
            }

            CompleteWeeklyGoal();
        }

        private void CompleteWeeklyGoal()
        {
            if (weeklyRewardClaimed)
                return;

            weeklyRewardClaimed =
                true;

            int credits =
                Mathf.Clamp(
                    MotorCityRemoteConfigRuntime.GetInt(
                        "club_weekly_credits",
                        900),
                    100,
                    5000);

            int rep =
                Mathf.Clamp(
                    MotorCityRemoteConfigRuntime.GetInt(
                        "club_weekly_rep",
                        90),
                    10,
                    500);

            wallet?.AddCredits(
                credits);

            reputation?.AddReputation(
                rep);

            SaveWeek();

            StatusText =
                MotorCityLocalization.Format(
                    "club.weekly_reward",
                    CurrentClubName,
                    credits,
                    rep);

            messageTimer =
                5.5f;
        }

        private void ResolveWeek()
        {
            currentWeek =
                ResolveServerWeek();

            long savedWeek =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    WeekKey,
                    -1);

            if (savedWeek ==
                currentWeek)
            {
                weeklyContribution =
                    Mathf.Clamp(
                        MotorCity.Persistence.MotorCitySaveService.GetInt(
                            ContributionKey,
                            0),
                        0,
                        WeeklyGoal);

                weeklyRewardClaimed =
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        RewardKey,
                        0) != 0;

                return;
            }

            weeklyContribution = 0;
            weeklyRewardClaimed = false;
            SaveWeek();
        }

        private void SaveWeek()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                WeekKey,
                (int)Math.Min(
                    (long)int.MaxValue,
                    currentWeek));

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                ContributionKey,
                weeklyContribution);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                RewardKey,
                weeklyRewardClaimed
                    ? 1
                    : 0);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private static long ResolveServerWeek()
        {
            return
                Math.Max(
                    0L,
                    MotorCityPlatform.ServerUnixTime /
                    604800L);
        }

        private void BuildClubs()
        {
            clubs =
                new[]
                {
                    new ClubDefinition(
                        "club.name.neon",
                        "club.desc.neon",
                        "N"),

                    new ClubDefinition(
                        "club.name.turbo",
                        "club.desc.turbo",
                        "T"),

                    new ClubDefinition(
                        "club.name.sun",
                        "club.desc.sun",
                        "S"),

                    new ClubDefinition(
                        "club.name.rainbow",
                        "club.desc.rainbow",
                        "R"),

                    new ClubDefinition(
                        "club.name.city",
                        "club.desc.city",
                        "C"),

                    new ClubDefinition(
                        "club.name.spark",
                        "club.desc.spark",
                        "K")
                };
        }

        private sealed class ClubDefinition
        {
            public readonly string NameKey;
            public readonly string DescriptionKey;
            public readonly string Emblem;

            public ClubDefinition(
                string nameKey,
                string descriptionKey,
                string emblem)
            {
                NameKey =
                    nameKey;

                DescriptionKey =
                    descriptionKey;

                Emblem =
                    emblem;
            }
        }
    }
}
