using System.Text;
using MotorCity.Gameplay;
using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private string[] journalBeforeResult;
        private string journalLastResultName;

        // ActivityResultShown is raised before ActivityCompleted updates all
        // independent systems. The journal compares that snapshot with the
        // current owner state only when opened; it does not save or reward.
        private void CaptureJournalBeforeActivityResult(string id, bool success)
        {
            journalBeforeResult = null;
            journalLastResultName = null;
            if (!success)
                return;
            journalBeforeResult = new string[7];
            for (int i = 0; i < journalBeforeResult.Length; i++)
                journalBeforeResult[i] = PlayerExperienceJournalProgressSignature(i);
            journalLastResultName = id;
        }

        public string PlayerExperienceJournalLineForFilter(int filter)
        {
            if (story == null || season == null || dailyAdventures == null)
                return MotorCityLocalization.Text("ux.sections.not_ready");
            StringBuilder sb = new StringBuilder(2800);
            AppendPlayerExperienceJournalUpdates(sb);
            if (filter < 0)
            {
                for (int i = 0; i < 7; i++)
                    AppendPlayerExperienceJournalSection(sb, i);
            }
            else
                AppendPlayerExperienceJournalSection(sb, Mathf.Clamp(filter, 0, 6));
            return sb.ToString();
        }

        private static readonly string[] JournalFilterKeys =
        {
            "ux.journal.filter.story", "ux.journal.filter.season",
            "ux.journal.filter.today", "ux.journal.filter.contracts",
            "ux.journal.filter.career", "ux.journal.filter.legends",
            "ux.journal.filter.night"
        };

        private void AppendPlayerExperienceJournalSection(StringBuilder sb, int filter)
        {
            sb.Append("\n\n").Append(MotorCityLocalization.Text(JournalFilterKeys[filter]));
            switch (filter)
            {
                case 0: AppendJournalStory(sb); break;
                case 1: AppendJournalSeason(sb); break;
                case 2: AppendJournalToday(sb); break;
                case 3: AppendJournalContracts(sb); break;
                case 4: AppendJournalCareer(sb); break;
                case 5: AppendJournalLegends(sb); break;
                case 6: AppendJournalNightClub(sb); break;
            }
        }

        private static string JournalStatus(bool complete, bool locked = false)
        {
            return MotorCityLocalization.Text(complete
                ? "ux.journal.status.complete"
                : locked ? "ux.journal.status.locked"
                : "ux.journal.status.active");
        }

        private static string JournalReward(int credits, int rep, int pixieXp = 0)
        {
            return credits == 0 && rep == 0 && pixieXp == 0
                ? MotorCityLocalization.Text("ux.journal.no_reward")
                : MotorCityLocalization.Format("ux.journal.reward",
                    credits, rep, pixieXp);
        }

        private static void JournalItem(StringBuilder sb, string title,
            string condition, string progress, string reward,
            string next, string status)
        {
            sb.Append("\n\n").Append(title);
            JournalField(sb, "ux.journal.field.status", status);
            JournalField(sb, "ux.journal.field.condition", condition);
            JournalField(sb, "ux.journal.field.progress", progress);
            JournalField(sb, "ux.journal.field.reward", reward);
            JournalField(sb, "ux.journal.field.next", next);
        }

        private static void JournalField(StringBuilder sb, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.Append("\n").Append(MotorCityLocalization.Format(key, value));
        }

        private static string JournalFraction(int current, int target)
        {
            return MotorCityLocalization.Format("ux.journal.progress",
                Mathf.Min(current, target), target);
        }

        private static string JournalDuration(long seconds)
        {
            var duration = System.TimeSpan.FromSeconds(
                System.Math.Max(0L, seconds));
            return duration.Days > 0
                ? duration.Days + "d " + duration.Hours.ToString("00") + "h"
                : duration.Hours.ToString("00") + ":" +
                    duration.Minutes.ToString("00");
        }

        private string PlayerExperienceJournalProgressSignature(int filter)
        {
            switch (filter)
            {
                case 0:
                    return story == null ? "" :
                        story.IsComplete ? "DONE" :
                        story.CurrentMissionNumber + "/" + story.MissionCount +
                        " " + story.CurrentMissionProgress + "/" + story.CurrentMissionTarget;
                case 1:
                    return season == null ? "" :
                        season.CompletedMissionCount + "/" + season.MissionCount +
                        " " + season.CurrentMissionProgress + "/" + season.CurrentMissionTarget +
                        (season.IsComplete ? " DONE" : "");
                case 2:
                    if (dailyAdventures == null) return "";
                    StringBuilder daily = new StringBuilder();
                    daily.Append(dailyAdventures.CompletedTasks).Append("/3");
                    for (int i = 0; i < dailyAdventures.CatalogTaskCount; i++)
                        daily.Append(" ").Append(dailyAdventures.CatalogTaskProgress(i))
                            .Append("/").Append(dailyAdventures.CatalogTaskTarget(i));
                    if (club != null && club.HasClub)
                        daily.Append(" W").Append(club.WeeklyContribution)
                            .Append("/").Append(club.WeeklyTarget);
                    return daily.ToString();
                case 3:
                    return contracts == null ? "" :
                        contracts.CatalogCycle + ":" + contracts.CatalogIndex +
                        " " + contracts.CatalogRacingProgress + "/" + contracts.CatalogRacingTarget +
                        " " + contracts.CatalogDriftProgress + "/" + contracts.CatalogDriftTarget +
                        " " + contracts.CatalogDeliveryProgress + "/" + contracts.CatalogDeliveryTarget +
                        " " + contracts.CatalogNightProgress + "/" + contracts.CatalogNightTarget;
                case 4:
                    return career == null ? "" :
                        career.Stage + "/" + career.CatalogStageCount +
                        " " + career.CatalogDeliveryWins + "," + career.CatalogDriftWins +
                        "," + career.CatalogSprintWins + "," + career.CatalogCircuitWins;
                case 5:
                    return legends == null ? "" :
                        legends.CatalogCurrentNumber + "/" + legends.CatalogTotalCount +
                        " " + legends.CatalogCurrentProgress +
                        (legends.AllLegendsDefeated ? " DONE" : "");
                case 6:
                    return underground == null ? "" :
                        underground.CatalogCurrentNumber + "/" + underground.CatalogTotalCount +
                        " " + underground.StreetCred + "/" + underground.CatalogRequiredCred +
                        " " + underground.CatalogCurrentProgress +
                        (underground.CatalogSeriesComplete ? " DONE" : "");
                default:
                    return "";
            }
        }

        private void AppendJournalStory(StringBuilder sb)
        {
            if (story == null) return;
            bool done = story.IsComplete;
            bool waiting = !done && onboarding != null && !onboarding.IsComplete;
            JournalItem(sb,
                done ? MotorCityLocalization.Text("ux.journal.complete") :
                    story.CurrentCharacterName + " - " + story.CurrentMissionTitle,
                done ? MotorCityLocalization.Text("ux.journal.complete") :
                    story.CurrentCharacterLine,
                done ? JournalFraction(story.MissionCount, story.MissionCount) :
                    MotorCityLocalization.Format("ux.journal.progress_stage",
                        story.CurrentMissionNumber, story.MissionCount,
                        story.CurrentMissionProgress, story.CurrentMissionTarget),
                done ? MotorCityLocalization.Text("ux.journal.no_reward") :
                    JournalReward(story.CurrentCreditsReward,
                        story.CurrentReputationReward, story.CurrentPixieXpReward),
                done ? MotorCityLocalization.Text("ux.journal.complete") :
                    MotorCityLocalization.Text(waiting
                        ? "ux.journal.story_tutorial" : "ux.journal.story_follow"),
                JournalStatus(done, waiting));
        }

        private void AppendJournalSeason(StringBuilder sb)
        {
            if (season == null) return;
            bool done = season.IsComplete;
            bool locked = !done && (!season.IsSeasonOneActive ||
                onboarding != null && !onboarding.IsComplete ||
                story != null && !story.IsComplete);
            string time = MotorCityLocalization.Format(
                "ux.journal.season_time", season.DaysRemaining);
            JournalItem(sb,
                done ? MotorCityLocalization.Text("ux.journal.complete") :
                    season.CurrentCharacterName + " - " + season.CurrentMissionTitle,
                done ? MotorCityLocalization.Text("ux.journal.complete") : season.ObjectiveLine,
                MotorCityLocalization.Format("ux.journal.progress_stage",
                    season.CurrentMissionNumber, season.MissionCount,
                    season.CurrentMissionProgress, season.CurrentMissionTarget) + "\n" + time,
                done ? MotorCityLocalization.Text("ux.journal.no_reward") :
                    JournalReward(season.CurrentCreditsReward,
                        season.CurrentReputationReward, season.CurrentPixieXpReward),
                MotorCityLocalization.Text(done ? "ux.journal.complete" :
                    locked ? "ux.journal.season_story" : "ux.journal.season_next"),
                JournalStatus(done, locked));
        }

        private void AppendJournalToday(StringBuilder sb)
        {
            if (dailyAdventures == null) return;
            string reset = MotorCityLocalization.Format("ux.journal.today_time",
                JournalDuration(dailyAdventures.CatalogSecondsUntilReset));
            for (int i = 0; i < dailyAdventures.CatalogTaskCount; i++)
            {
                bool done = dailyAdventures.CatalogTaskComplete(i);
                JournalItem(sb,
                    MotorCityLocalization.Format("ux.journal.daily_item", i + 1),
                    dailyAdventures.CatalogTaskDescription(i),
                    JournalFraction(dailyAdventures.CatalogTaskProgress(i),
                        dailyAdventures.CatalogTaskTarget(i)) + "\n" + reset,
                    done ? MotorCityLocalization.Text("ux.journal.no_reward") :
                        MotorCityLocalization.Format("ux.journal.daily_reward",
                            dailyAdventures.CatalogTaskReward(i)),
                    done ? reset : MotorCityLocalization.Text("ux.journal.daily_next"),
                    JournalStatus(done));
            }

            JournalItem(sb, MotorCityLocalization.Text("ux.journal.today_bonus"),
                MotorCityLocalization.Text("ux.journal.today_bonus"),
                JournalFraction(dailyAdventures.CompletedTasks,
                    dailyAdventures.CatalogTaskCount) + "\n" + reset,
                dailyAdventures.DayCompleted
                    ? MotorCityLocalization.Text("ux.journal.no_reward")
                    : MotorCityLocalization.Format("ux.journal.reward_credits",
                        dailyAdventures.CatalogDayReward +
                        dailyAdventures.CatalogNextMilestoneReward),
                reset, JournalStatus(dailyAdventures.DayCompleted));

            if (club == null) return;
            bool joined = club.HasClub;
            bool doneWeek = joined && club.CatalogWeeklyRewardClaimed;
            string weeklyReset = MotorCityLocalization.Format(
                "ux.journal.week_time",
                JournalDuration(club.CatalogSecondsUntilWeeklyReset));
            JournalItem(sb,
                joined ? MotorCityLocalization.Format("ux.journal.weekly",
                    club.CurrentClubName) : MotorCityLocalization.Text("ux.journal.weekly_no_club"),
                joined ? club.WeeklyLine :
                    MotorCityLocalization.Text("ux.journal.weekly_no_club"),
                joined ? JournalFraction(club.WeeklyContribution, club.WeeklyTarget) +
                    "\n" + weeklyReset : weeklyReset,
                !joined || doneWeek
                    ? MotorCityLocalization.Text("ux.journal.no_reward")
                    : MotorCityLocalization.Format("ux.journal.reward_credits_rep",
                        club.CatalogWeeklyCreditsReward,
                        club.CatalogWeeklyReputationReward),
                joined ? weeklyReset : MotorCityLocalization.Text("ux.journal.weekly_no_club"),
                JournalStatus(doneWeek, !joined));
        }

        private void AppendJournalContracts(StringBuilder sb)
        {
            if (contracts == null) return;
            JournalItem(sb,
                MotorCityLocalization.Format("ux.journal.contract_title",
                    contracts.CatalogCycle, contracts.CatalogIndex + 1,
                    6, contracts.CatalogTitle) + " - " + contracts.CatalogClient,
                contracts.CatalogBrief + "\n" +
                    MotorCityLocalization.Text("ux.journal.contract_passive"),
                contracts.CatalogProgressLine,
                JournalReward(contracts.CatalogRewardCredits,
                    contracts.CatalogRewardReputation),
                MotorCityLocalization.Text("ux.journal.contract_next"),
                JournalStatus(false, false));
        }

        private void AppendJournalCareer(StringBuilder sb)
        {
            if (career == null) return;
            bool complete = career.CatalogComplete;
            string required = MotorCityLocalization.Text(
                "ux.journal.career_condition");
            string progress = career.HudLine;
            JournalItem(sb, complete ? MotorCityLocalization.Text("ux.journal.complete") :
                    MotorCityLocalization.Format("ux.journal.career_title",
                        career.Stage + 1, career.CatalogStageCount),
                required, progress,
                complete ? MotorCityLocalization.Text("ux.journal.no_reward") :
                    JournalReward(career.CatalogNextCredits,
                        career.CatalogNextReputation),
                MotorCityLocalization.Text(complete
                    ? "ux.journal.complete" : "ux.journal.career_next"),
                JournalStatus(complete));
        }

        private void AppendPlayerExperienceJournalUpdates(StringBuilder sb)
        {
            if (journalBeforeResult == null || journalLastResultName == null)
                return;
            StringBuilder changed = new StringBuilder(420);
            for (int i = 0; i < 7; i++)
            {
                string before = journalBeforeResult[i];
                string after = PlayerExperienceJournalProgressSignature(i);
                if (before == after)
                    continue;
                changed.Append("\n").Append(MotorCityLocalization.Format(
                    "ux.journal.change_line",
                    MotorCityLocalization.Text(JournalFilterKeys[i]),
                    before, after));
            }
            if (changed.Length == 0)
                return;
            sb.Append(MotorCityLocalization.Text("ux.journal.changes"));
            sb.Append(changed);
        }
    }
}
