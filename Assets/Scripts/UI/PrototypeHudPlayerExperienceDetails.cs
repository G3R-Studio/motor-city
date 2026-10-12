using System.Text;
using MotorCity.Gameplay;
using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        // UX-4 details remain informational. Their owners decide unlocks,
        // payouts and saved progress; merely opening a page changes nothing.
        private string BuildPlayerExperienceClubDetail()
        {
            if (club == null)
                return MotorCityLocalization.Text("ux.sections.not_ready");
            if (!club.HasClub)
                return MotorCityLocalization.Text("ux.club.not_joined") + "\n\n" +
                    MotorCityLocalization.Text("ux4.club.joinhint") + "\n" +
                    MotorCityLocalization.Text("ux4.club.no_online");

            StringBuilder sb = new StringBuilder(450);
            sb.Append(MotorCityLocalization.Format("ux.club.current", club.CurrentClubName));
            sb.Append("\n\n").Append(club.CatalogCurrentFocusLine);
            sb.Append("\n").Append(club.CatalogCountedActivities);
            sb.Append("\n\n").Append(MotorCityLocalization.Format(
                "ux4.club.progress", club.WeeklyContribution, club.WeeklyTarget));
            sb.Append("\n").Append(MotorCityLocalization.Format(
                "ux4.club.reward",
                club.CatalogWeeklyCreditsReward,
                club.CatalogWeeklyReputationReward));
            sb.Append("\n").Append(MotorCityLocalization.Text(
                club.CatalogWeeklyRewardClaimed ? "ux4.club.claimed" :
                club.WeeklyRewardAvailable ? "ux4.club.available" :
                "ux4.club.notclaimed"));
            sb.Append("\n").Append(MotorCityLocalization.Format(
                "ux4.club.weekreset",
                PlayerExperienceDetailsDuration(club.CatalogSecondsUntilWeeklyReset)));
            sb.Append("\n\n").Append(MotorCityLocalization.Text("ux4.club.no_online"));
            return sb.ToString();
        }

        private string BuildPlayerExperienceInspectorDetail()
        {
            if (cityRisk == null)
                return MotorCityLocalization.Text("ux.sections.not_ready");
            return string.Join("\n\n",
                MotorCityLocalization.Format("ux4.risk.scale",
                    cityRisk.Attention, cityRisk.CatalogAttentionMax, cityRisk.RiskLevel),
                MotorCityLocalization.Format("ux4.risk.state",
                    MotorCityLocalization.Text(cityRisk.PursuitActive
                        ? "ux.inspector.watching" : "ux.inspector.calm")),
                MotorCityLocalization.Format("ux4.risk.threshold",
                    cityRisk.CatalogObservationStart, cityRisk.CatalogObservationEnd),
                MotorCityLocalization.Text("ux4.risk.growth"),
                MotorCityLocalization.Text("ux4.risk.reduce"),
                MotorCityLocalization.Text("ux4.risk.note"));
        }

        private string BuildPlayerExperiencePixieDetail()
        {
            if (pixie == null)
                return MotorCityLocalization.Text("ux.sections.not_ready");
            StringBuilder sb = new StringBuilder(650);
            sb.Append(pixie.Level >= 10
                ? MotorCityLocalization.Format("ux.pixie.progress_max", pixie.Level, pixie.Xp)
                : MotorCityLocalization.Format("ux.pixie.progress_next",
                    pixie.Level, pixie.Xp, pixie.XpForNextLevelTarget,
                    pixie.XpRemainingToNextLevel));
            sb.Append("\n\n").Append(MotorCityLocalization.Text(
                pixie.ExclusiveVisualActive ? "ux.pixie.skin_ex" :
                pixie.ExclusiveVisualFallback ? "ux.pixie.skin_missing_ex" :
                "ux.pixie.skin_standard"));

            string[] abilities = {
                "ux4.pixie.ability.hints",
                "ux4.pixie.ability.discovery",
                "ux4.pixie.ability.boost"
            };
            for (int i = 0; i < abilities.Length; i++)
            {
                int requirement = i + 2;
                sb.Append("\n\n").Append(MotorCityLocalization.Text(abilities[i]));
                if (pixie.Level < requirement)
                    sb.Append("\n").Append(MotorCityLocalization.Format(
                        "ux4.pixie.ability.locked", requirement));
            }
            sb.Append("\n\n").Append(MotorCityLocalization.Format(
                "ux4.pixie.daily", pixie.CatalogDailyProgress,
                pixie.CatalogDailyTarget, pixie.DailyObjectiveLine));
            sb.Append("\n").Append(pixie.CatalogDailyClaimed
                ? MotorCityLocalization.Text("ux4.pixie.rewardclaimed")
                : MotorCityLocalization.Format("ux4.pixie.dailyreward",
                    pixie.CatalogDailyCreditsReward, pixie.CatalogDailyXpReward));
            sb.Append("\n\n").Append(MotorCityLocalization.Text("ux4.pixie.note"));
            return sb.ToString();
        }

        private string BuildPlayerExperienceGarageDetail()
        {
            if (garage == null)
                return MotorCityLocalization.Text("ux.sections.not_ready");
            StringBuilder sb = new StringBuilder(1100);
            int visibleCount = 0;
            int unlockedCount = 0;
            for (int i = 0; i < garage.CatalogVehicleCount; i++)
            {
                if (!garage.CatalogVehicleAvailable(i))
                    continue;
                bool unlocked = garage.CatalogVehicleUnlocked(i);
                // Do not spoil the supporter-exclusive model until entitlement exists.
                if (garage.CatalogVehicleSupporterExclusive(i) && !unlocked)
                    continue;
                visibleCount++;
                if (unlocked) unlockedCount++;
            }
            sb.Append(MotorCityLocalization.Format("ux4.garage.summary",
                unlockedCount, visibleCount, garage.VehicleName));
            for (int i = 0; i < garage.CatalogVehicleCount; i++)
            {
                if (!garage.CatalogVehicleAvailable(i))
                    continue;
                bool unlocked = garage.CatalogVehicleUnlocked(i);
                if (garage.CatalogVehicleSupporterExclusive(i) && !unlocked)
                    continue;
                string access = unlocked
                    ? MotorCityLocalization.Text("ux4.garage.vehicle.open")
                    : MotorCityLocalization.Format("ux4.garage.vehicle.locked",
                        garage.CatalogVehicleRequiredRep(i));
                sb.Append("\n").Append(MotorCityLocalization.Format(
                    "ux4.garage.vehicle", garage.CatalogVehicleName(i),
                    access, garage.CatalogVehicleMasteryLevel(i)));
            }
            sb.Append("\n\n").Append(garage.NextVehicleLine);
            sb.Append("\n\n").Append(garage.VehicleMasteryLine);
            if (vehicleSpecialization != null)
                sb.Append("\n\n").Append(vehicleSpecialization.GarageLine);
            sb.Append("\n\n").Append(MotorCityLocalization.Text("ux4.garage.upgrades"));
            for (int i = 0; i < 3; i++)
                sb.Append("\n").Append(MotorCityLocalization.Format(
                    "ux4.garage.upgrade", garage.GetUpgradeName(i),
                    garage.GetUpgradeLevelText(i), garage.GetUpgradePrice(i)));
            if (vehicleHistory != null)
                sb.Append("\n\n").Append(MotorCityLocalization.Text("ux4.garage.history"))
                    .Append("\n").Append(vehicleHistory.GarageLine);
            sb.Append("\n\n").Append(MotorCityLocalization.Text("ux4.garage.note"));
            return sb.ToString();
        }

        private string BuildPlayerExperienceAchievementsDetail()
        {
            if (achievements == null && collection == null)
                return MotorCityLocalization.Text("ux.sections.not_ready");
            StringBuilder sb = new StringBuilder(1000);
            if (achievements != null)
            {
                sb.Append(MotorCityLocalization.Format("ux.achievements.count",
                    achievements.UnlockedCount, achievements.TotalCount));
                for (int i = 0; i < achievements.TotalCount; i++)
                {
                    bool unlocked = achievements.IsAchievementUnlocked(i);
                    achievements.GetAchievementProgress(i, out int current, out int target);
                    achievements.GetAchievementReward(i, out int credits, out int rep);
                    sb.Append("\n\n").Append(MotorCityLocalization.Text(
                        unlocked ? "ux.achievements.done" : "ux.achievements.locked"));
                    sb.Append(" ").Append(achievements.GetAchievementName(i));
                    sb.Append("\n").Append(achievements.GetAchievementCondition(i));
                    sb.Append("\n").Append(MotorCityLocalization.Format(
                        "ux.achievements.progress_reward", current, target, credits, rep));
                    sb.Append("\n").Append(unlocked
                        ? MotorCityLocalization.Text("ux4.achievement.reward.claimed")
                        : MotorCityLocalization.Format("ux4.achievement.reward.pending",
                            credits, rep));
                }
            }
            if (collection != null)
            {
                sb.Append("\n\n").Append(MotorCityLocalization.Text(
                    "ux.achievements.collection"));
                sb.Append("\n").Append(MotorCityLocalization.Format(
                    "ux4.collection.progress", collection.Rating,
                    collection.CatalogClaimedTiers, collection.CatalogTierCount));
                sb.Append("\n").Append(collection.CatalogAllClaimed
                    ? MotorCityLocalization.Text("ux4.collection.done")
                    : MotorCityLocalization.Format("ux4.collection.next",
                        collection.CatalogNextScoreTarget,
                        collection.CatalogNextCreditsReward,
                        collection.CatalogNextReputationReward));
                sb.Append("\n").Append(MotorCityLocalization.Text("ux4.collection.note"));
            }
            return sb.ToString();
        }

        private string BuildPlayerExperienceNightClubDetail()
        {
            if (underground == null && legends == null)
                return MotorCityLocalization.Text("ux.sections.not_ready");
            StringBuilder sb = new StringBuilder(700);
            if (underground != null)
            {
                sb.Append(MotorCityLocalization.Text("ux.journal.night_title"));
                if (underground.CatalogSeriesComplete)
                    sb.Append("\n").Append(MotorCityLocalization.Text("ux4.special.completed"));
                else
                {
                    sb.Append("\n").Append(MotorCityLocalization.Format(
                        "ux4.special.night", underground.CatalogCurrentNumber,
                        underground.CatalogTotalCount, underground.CatalogCurrentName));
                    sb.Append("\n").Append(MotorCityLocalization.Format(
                        "ux4.special.trust",
                        underground.StreetCred, underground.CatalogRequiredCred));
                    sb.Append("\n").Append(MotorCityLocalization.Text(
                        !underground.HasActiveInvitation
                            ? "ux4.special.invite_locked"
                            : underground.CatalogIsNight
                                ? "ux4.special.night_ready" : "ux4.special.day_locked"));
                    sb.Append("\n").Append(MotorCityLocalization.Format(
                        "ux4.special.progress", underground.CatalogCurrentProgress));
                    sb.Append("\n").Append(MotorCityLocalization.Format(
                        "ux4.special.reward", underground.CatalogCurrentCreditsReward,
                        underground.CatalogCurrentReputationReward));
                }
            }
            if (legends != null)
            {
                sb.Append("\n\n").Append(MotorCityLocalization.Text(
                    "ux.journal.legend_title"));
                if (legends.AllLegendsDefeated)
                    sb.Append("\n").Append(MotorCityLocalization.Text("ux4.special.completed"));
                else
                {
                    sb.Append("\n").Append(MotorCityLocalization.Format(
                        "ux4.special.legend", legends.CatalogCurrentNumber,
                        legends.CatalogTotalCount, legends.CatalogCurrentName));
                    sb.Append("\n").Append(MotorCityLocalization.Format(
                        "ux4.special.unlock", legends.CatalogCurrentRequirements));
                    sb.Append("\n").Append(MotorCityLocalization.Format(
                        "ux4.special.progress", legends.CatalogCurrentProgress));
                    sb.Append("\n").Append(MotorCityLocalization.Format(
                        "ux4.special.reward", legends.CatalogCurrentCreditsReward,
                        legends.CatalogCurrentReputationReward));
                }
                sb.Append("\n").Append(MotorCityLocalization.Text(
                    "ux4.special.legends_note"));
            }
            return sb.ToString();
        }

        private static string PlayerExperienceDetailsDuration(long seconds)
        {
            System.TimeSpan time = System.TimeSpan.FromSeconds(
                System.Math.Max(0L, seconds));
            return time.Days > 0
                ? time.Days + "d " + time.Hours.ToString("00") + "h"
                : time.Hours.ToString("00") + ":" + time.Minutes.ToString("00");
        }
    }
}
