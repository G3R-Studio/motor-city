using System.Text;
using MotorCity.Gameplay;
using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        // Captured inside ActivityResultShown (after core payment, before
        // ActivityCompleted notifies the separate progression owners).
        // One result sequence owns one immutable before/after comparison.
        private sealed class ResultChangesSnapshot
        {
            public int Sequence;
            public bool Success;
            public int Credits;
            public int Reputation;
            public int RacingRep;
            public int DriftRep;
            public int DeliveryRep;
            public int PixieLevel;
            public int PixieXp;
            public int Attention;
            public int Achievements;
            public int CollectionScore;
            public int CollectionClaimed;
            public int VehicleMasteryLevel;
            public int VehicleVictories;
        }

        private ResultChangesSnapshot resultChangesBefore;
        private int resultChangesRenderedSequence = -1;
        private string resultChangesRenderedText = string.Empty;
        private ScrollRect resultChangesScroll;
        private RectTransform resultChangesContent;

        private void CaptureResultChangesBefore(bool success)
        {
            resultChangesBefore = success ? ReadResultChangesSnapshot() : null;
            resultChangesRenderedSequence = -1;
            resultChangesRenderedText = string.Empty;
        }

        private ResultChangesSnapshot ReadResultChangesSnapshot()
        {
            return new ResultChangesSnapshot
            {
                Sequence = activityManager == null ? 0 : activityManager.ResultSequence,
                Success = activityManager != null && activityManager.ResultSuccess,
                Credits = wallet == null ? 0 : wallet.Credits,
                Reputation = activityManager == null ? 0 : activityManager.TotalReputation,
                RacingRep = activityManager == null ? 0 :
                    activityManager.CatalogDisciplineReputation(DisciplineType.Racing),
                DriftRep = activityManager == null ? 0 :
                    activityManager.CatalogDisciplineReputation(DisciplineType.Drift),
                DeliveryRep = activityManager == null ? 0 :
                    activityManager.CatalogDisciplineReputation(DisciplineType.Delivery),
                PixieLevel = pixie == null ? 0 : pixie.Level,
                PixieXp = pixie == null ? 0 : pixie.Xp,
                Attention = cityRisk == null ? 0 : Mathf.RoundToInt(cityRisk.Attention),
                Achievements = achievements == null ? 0 : achievements.UnlockedCount,
                CollectionScore = collection == null ? 0 : collection.Rating,
                CollectionClaimed = collection == null ? 0 : collection.CatalogClaimedTiers,
                VehicleMasteryLevel = garage == null ? 0 :
                    garage.CatalogVehicleMasteryLevel(garage.CatalogSelectedVehicleIndex),
                VehicleVictories = vehicleHistory == null ? 0 : vehicleHistory.Victories
            };
        }

        // Render from a completed synchronous ActivityCompleted transaction,
        // never from theoretical reward tables or a new game-state mutation.
        private string ResultChangedSystemsLine()
        {
            if (activityManager == null || !activityManager.HasResult)
                return string.Empty;
            int sequence = activityManager.ResultSequence;
            if (resultChangesRenderedSequence == sequence)
                return resultChangesRenderedText;
            resultChangesRenderedSequence = sequence;

            if (!activityManager.ResultSuccess ||
                resultChangesBefore == null ||
                resultChangesBefore.Sequence != sequence)
                return resultChangesRenderedText = string.Empty;

            ResultChangesSnapshot before = resultChangesBefore;
            ResultChangesSnapshot after = ReadResultChangesSnapshot();
            StringBuilder text = new StringBuilder(520);
            if (activityManager.ResultMasteryXp > 0)
                AppendResultChange(text, MotorCityLocalization.Format(
                    "hud.result_mastery", activityManager.ResultMasteryXp));

            AppendResultGain(text, "ux5.changes.bonus_credits",
                after.Credits - before.Credits);
            AppendResultGain(text, "ux5.changes.bonus_rep",
                after.Reputation - before.Reputation);
            AppendResultGain(text, "ux5.changes.racing_rep",
                after.RacingRep - before.RacingRep);
            AppendResultGain(text, "ux5.changes.drift_rep",
                after.DriftRep - before.DriftRep);
            AppendResultGain(text, "ux5.changes.delivery_rep",
                after.DeliveryRep - before.DeliveryRep);

            if (after.PixieLevel != before.PixieLevel)
                AppendResultChange(text, MotorCityLocalization.Format(
                    "ux5.changes.pixie_level", before.PixieLevel, after.PixieLevel));
            else if (after.PixieXp > before.PixieXp)
                AppendResultGain(text, "ux5.changes.pixie_xp",
                    after.PixieXp - before.PixieXp);

            if (after.Attention != before.Attention)
                AppendResultChange(text, MotorCityLocalization.Format(
                    "ux5.changes.attention", before.Attention, after.Attention));
            if (after.Achievements > before.Achievements)
                AppendResultGain(text, "ux5.changes.achievements",
                    after.Achievements - before.Achievements);
            if (after.CollectionScore != before.CollectionScore)
                AppendResultChange(text, MotorCityLocalization.Format(
                    "ux5.changes.collection_score",
                    before.CollectionScore, after.CollectionScore));
            if (after.CollectionClaimed > before.CollectionClaimed)
                AppendResultGain(text, "ux5.changes.collection_tiers",
                    after.CollectionClaimed - before.CollectionClaimed);
            if (after.VehicleMasteryLevel > before.VehicleMasteryLevel)
                AppendResultChange(text, MotorCityLocalization.Format(
                    "ux5.changes.mastery_level",
                    before.VehicleMasteryLevel, after.VehicleMasteryLevel));
            if (after.VehicleVictories > before.VehicleVictories)
                AppendResultGain(text, "ux5.changes.vehicle_victories",
                    after.VehicleVictories - before.VehicleVictories);

            if (journalBeforeResult != null)
            {
                for (int i = 0; i < journalBeforeResult.Length; i++)
                {
                    string previous = journalBeforeResult[i];
                    string next = PlayerExperienceJournalProgressSignature(i);
                    if (previous == next)
                        continue;
                    AppendResultChange(text, MotorCityLocalization.Format(
                        "ux5.changes.progress",
                        MotorCityLocalization.Text(JournalFilterKeys[i]), next));
                }
            }

            // Legacy result summary is a fallback only; never repeat the same
            // daily/season/club lines under the new complete snapshot.
            if (text.Length == 0)
                AppendResultChange(text, activityManager.ResultSecondaryProgress);
            return resultChangesRenderedText = text.ToString();
        }

        private static void AppendResultGain(
            StringBuilder text, string key, int gain)
        {
            if (gain > 0)
                AppendResultChange(text, MotorCityLocalization.Format(key, gain));
        }

        private static void AppendResultChange(StringBuilder text, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;
            if (text.Length > 0)
                text.Append('\n');
            text.Append(line);
        }
    }
}
