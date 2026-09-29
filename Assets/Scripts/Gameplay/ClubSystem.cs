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
        private const string WeeklyMigrationKey =
            "MotorCity.Club.WeeklyPerClubMigrated";
        private const string GlobalRewardWeekKey =
            "MotorCity.Club.GlobalRewardWeek";

        private int WeeklyGoal
        {
            get
            {
                ClubDefinition definition =
                    CurrentClubDefinition();

                int fallback =
                    definition == null
                        ? 10
                        : definition.WeeklyGoal;

                return
                    Mathf.Clamp(
                        MotorCityRemoteConfigRuntime.GetInt(
                            "club_weekly_goal_" +
                            Mathf.Max(
                                0,
                                JoinedClubIndex),
                            fallback),
                        3,
                        30);
            }
        }

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

        public string BrowseClubFocusLine
        {
            get
            {
                ClubDefinition definition =
                    BrowseClubDefinition();

                return
                    definition == null
                        ? string.Empty
                        : MotorCityLocalization.Format(
                            "club.focus_line",
                            MotorCityLocalization.Text(
                                definition.FocusKey));
            }
        }

        public string BrowseClubGoalLine
        {
            get
            {
                ClubDefinition definition =
                    BrowseClubDefinition();

                if (definition == null)
                    return string.Empty;

                return
                    MotorCityLocalization.Format(
                        "club.browse_goal",
                        MotorCityLocalization.Text(
                            definition.FocusKey),
                        definition.WeeklyGoal);
            }
        }

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

        public bool WeeklyRewardAvailable =>
            HasClub &&
            !weeklyRewardClaimed &&
            !GlobalRewardClaimedThisWeek();

        public string BrowseClubRewardLine
        {
            get
            {
                int credits =
                    WeeklyCreditsReward();

                int rep =
                    WeeklyReputationReward();

                return
                    MotorCityLocalization.Format(
                        "club.browse_reward",
                        credits,
                        rep);
            }
        }

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

            MigrateLegacyWeeklyProgress();
            ResolveWeek();

            if (activities != null)
            {
                activities.ActivityCompleted +=
                    OnActivityCompleted;
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
                activities.ActivityCompleted -=
                    OnActivityCompleted;
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

            ResolveWeek();

            StatusText =
                MotorCityLocalization.Format(
                    "club.joined_focus",
                    CurrentClubName,
                    MotorCityLocalization.Text(
                        CurrentClubDefinition().FocusKey));

            messageTimer =
                4.5f;
        }

        public void SetClubForTesting(
            int index)
        {
            if (clubs == null ||
                clubs.Length == 0)
            {
                return;
            }

            BrowseClubIndex =
                Mathf.Clamp(
                    index,
                    0,
                    clubs.Length - 1);

            JoinBrowseClub();
        }

        public void CompleteWeeklyForTesting()
        {
            if (!HasClub)
                return;

            weeklyContribution =
                WeeklyGoal;

            SaveWeek();
            CompleteWeeklyGoal();
        }

        public void ResetForTesting()
        {
            JoinedClubIndex = -1;
            BrowseClubIndex = 0;
            weeklyContribution = 0;
            weeklyRewardClaimed = false;

            MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                ClubKey);

            MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                GlobalRewardWeekKey);

            for (int i = 0; i < 6; i++)
            {
                string suffix =
                    "." + i;

                MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                    WeekKey + suffix);

                MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                    ContributionKey + suffix);

                MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                    RewardKey + suffix);
            }

            MotorCity.Persistence.MotorCitySaveService.Save();

            StatusText = string.Empty;
            messageTimer = 0f;
        }

        private void OnActivityCompleted(
            string activityId)
        {
            if (activities != null &&
                !activities.SecondaryProgressionAllowed)
            {
                return;
            }

            if (!HasClub ||
                weeklyRewardClaimed ||
                !MatchesCurrentClub(
                    activityId))
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

            if (GlobalRewardClaimedThisWeek())
            {
                weeklyRewardClaimed =
                    true;

                SaveWeek();

                StatusText =
                    MotorCityLocalization.Format(
                        "club.weekly_already_claimed",
                        CurrentClubName);

                messageTimer =
                    4.5f;

                return;
            }

            weeklyRewardClaimed =
                true;

            int credits =
                WeeklyCreditsReward();

            int rep =
                WeeklyReputationReward();

            wallet?.AddCredits(
                credits);

            reputation?.AddReputation(
                rep);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                GlobalRewardWeekKey,
                (int)Math.Min(
                    (long)int.MaxValue,
                    currentWeek));

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

        private void MigrateLegacyWeeklyProgress()
        {
            if (!HasClub ||
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    WeeklyMigrationKey,
                    0) != 0)
            {
                return;
            }

            long week =
                ResolveServerWeek();

            long legacyWeek =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    WeekKey,
                    -1);

            if (legacyWeek ==
                week)
            {
                string suffix =
                    "." +
                    JoinedClubIndex;

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    WeekKey + suffix,
                    (int)Math.Min(
                        (long)int.MaxValue,
                        week));

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    ContributionKey + suffix,
                    Mathf.Max(
                        0,
                        MotorCity.Persistence.MotorCitySaveService.GetInt(
                            ContributionKey,
                            0)));

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    RewardKey + suffix,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        RewardKey,
                        0));
            }

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                WeeklyMigrationKey,
                1);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private void ResolveWeek()
        {
            currentWeek =
                ResolveServerWeek();

            if (!HasClub)
            {
                weeklyContribution = 0;
                weeklyRewardClaimed = false;
                return;
            }

            string suffix =
                "." +
                JoinedClubIndex;

            long savedWeek =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    WeekKey + suffix,
                    -1);

            if (savedWeek ==
                currentWeek)
            {
                weeklyContribution =
                    Mathf.Clamp(
                        MotorCity.Persistence.MotorCitySaveService.GetInt(
                            ContributionKey + suffix,
                            0),
                        0,
                        WeeklyGoal);

                weeklyRewardClaimed =
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        RewardKey + suffix,
                        0) != 0;

                return;
            }

            weeklyContribution = 0;
            weeklyRewardClaimed = false;
            SaveWeek();
        }

        private void SaveWeek()
        {
            if (!HasClub)
                return;

            string suffix =
                "." +
                JoinedClubIndex;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                WeekKey + suffix,
                (int)Math.Min(
                    (long)int.MaxValue,
                    currentWeek));

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                ContributionKey + suffix,
                weeklyContribution);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                RewardKey + suffix,
                weeklyRewardClaimed
                    ? 1
                    : 0);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private bool GlobalRewardClaimedThisWeek()
        {
            return
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    GlobalRewardWeekKey,
                    -1) ==
                currentWeek;
        }

        private static int WeeklyCreditsReward()
        {
            return
                Mathf.Clamp(
                    MotorCityRemoteConfigRuntime.GetInt(
                        "club_weekly_credits",
                        900),
                    100,
                    5000);
        }

        private static int WeeklyReputationReward()
        {
            return
                Mathf.Clamp(
                    MotorCityRemoteConfigRuntime.GetInt(
                        "club_weekly_rep",
                        90),
                    10,
                    500);
        }

        private static long ResolveServerWeek()
        {
            return
                Math.Max(
                    0L,
                    MotorCityPlatform.ServerUnixTime /
                    604800L);
        }

        private bool MatchesCurrentClub(
            string activityId)
        {
            ClubDefinition definition =
                CurrentClubDefinition();

            if (definition == null ||
                string.IsNullOrWhiteSpace(
                    activityId))
            {
                return false;
            }

            return
                definition.Focus switch
                {
                    ClubFocus.Neon =>
                        activityId == "drift" ||
                        activityId == "driftspot" ||
                        activityId == "underground",

                    ClubFocus.Turbo =>
                        activityId == "discovery" ||
                        activityId == "photo_hunt" ||
                        activityId == "speedtrap",

                    ClubFocus.Sun =>
                        activityId == "delivery" ||
                        activityId.StartsWith(
                            "profession_",
                            StringComparison.Ordinal),

                    ClubFocus.Rainbow =>
                        activityId == "drift" ||
                        activityId == "driftspot",

                    ClubFocus.City =>
                        activityId == "discovery" ||
                        activityId == "speedtrap" ||
                        activityId == "photo_hunt" ||
                        activityId == "profession_taxi" ||
                        activityId == "profession_mail",

                    ClubFocus.Spark =>
                        activityId == "sprint" ||
                        activityId == "circuit",

                    _ =>
                        false
                };
        }

        private ClubDefinition CurrentClubDefinition()
        {
            return
                !HasClub
                    ? null
                    : clubs[JoinedClubIndex];
        }

        private ClubDefinition BrowseClubDefinition()
        {
            if (clubs == null ||
                clubs.Length == 0)
            {
                return null;
            }

            return
                clubs[
                    Mathf.Clamp(
                        BrowseClubIndex,
                        0,
                        clubs.Length - 1)];
        }

        private void BuildClubs()
        {
            clubs =
                new[]
                {
                    new ClubDefinition(
                        "club.name.neon",
                        "club.desc.neon",
                        "club.focus.neon",
                        "N",
                        8,
                        ClubFocus.Neon),

                    new ClubDefinition(
                        "club.name.turbo",
                        "club.desc.turbo",
                        "club.focus.turbo",
                        "T",
                        8,
                        ClubFocus.Turbo),

                    new ClubDefinition(
                        "club.name.sun",
                        "club.desc.sun",
                        "club.focus.sun",
                        "S",
                        10,
                        ClubFocus.Sun),

                    new ClubDefinition(
                        "club.name.rainbow",
                        "club.desc.rainbow",
                        "club.focus.rainbow",
                        "R",
                        8,
                        ClubFocus.Rainbow),

                    new ClubDefinition(
                        "club.name.city",
                        "club.desc.city",
                        "club.focus.city",
                        "C",
                        8,
                        ClubFocus.City),

                    new ClubDefinition(
                        "club.name.spark",
                        "club.desc.spark",
                        "club.focus.spark",
                        "K",
                        10,
                        ClubFocus.Spark)
                };
        }

        private enum ClubFocus
        {
            Neon,
            Turbo,
            Sun,
            Rainbow,
            City,
            Spark
        }

        private sealed class ClubDefinition
        {
            public readonly string NameKey;
            public readonly string DescriptionKey;
            public readonly string FocusKey;
            public readonly string Emblem;
            public readonly int WeeklyGoal;
            public readonly ClubFocus Focus;

            public ClubDefinition(
                string nameKey,
                string descriptionKey,
                string focusKey,
                string emblem,
                int weeklyGoal,
                ClubFocus focus)
            {
                NameKey =
                    nameKey;

                DescriptionKey =
                    descriptionKey;

                FocusKey =
                    focusKey;

                Emblem =
                    emblem;

                WeeklyGoal =
                    Mathf.Max(
                        1,
                        weeklyGoal);

                Focus =
                    focus;
            }
        }
    }
}
