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
