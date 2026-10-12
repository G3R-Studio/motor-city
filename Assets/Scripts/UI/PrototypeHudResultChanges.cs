using System.Text;
using MotorCity.Gameplay;
using MotorCity.Localization;
using UnityEngine;
using UnityEngine.UI;

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

        private Text resultChangesHeadingText;
        private int resultChangesLayoutSequence = -1;
        private ResultChangesSnapshot resultChangesBefore;
        private int resultChangesRenderedSequence = -1;
        private string resultChangesRenderedText = string.Empty;
        private ScrollRect resultChangesScroll;
        private RectTransform resultChangesContent;

        private void BuildResultChangesScroll(RectTransform panel)
        {
            resultChangesHeadingText = CreateText(panel,
                MotorCityLocalization.Text("ux5.changes.heading"),
                14, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0f, -248f), new Vector2(555f, 22f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                BlueAccent);

            GameObject viewportObject = new GameObject(
                "Result Changes Viewport",
                typeof(RectTransform), typeof(Image),
                typeof(RectMask2D), typeof(ScrollRect));
            viewportObject.transform.SetParent(panel, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = new Vector2(0.5f, 1f);
            viewport.anchorMax = new Vector2(0.5f, 1f);
            viewport.pivot = new Vector2(0.5f, 1f);
            viewport.anchoredPosition = new Vector2(0f, -269f);
            viewport.sizeDelta = new Vector2(550f, 114f);
            Image background = viewportObject.GetComponent<Image>();
            background.color = new Color(0.08f, 0.12f, 0.24f, 0.28f);
            background.raycastTarget = true;

            GameObject content = new GameObject(
                "Result Changes Content", typeof(RectTransform));
            content.transform.SetParent(viewport, false);
            resultChangesContent = content.GetComponent<RectTransform>();
            resultChangesContent.anchorMin = new Vector2(0f, 1f);
            resultChangesContent.anchorMax = new Vector2(1f, 1f);
            resultChangesContent.pivot = new Vector2(0.5f, 1f);
            resultChangesContent.anchoredPosition = Vector2.zero;
            resultChangesContent.sizeDelta = new Vector2(0f, 114f);

            resultSecondaryProgressText.transform.SetParent(
                resultChangesContent, false);
            RectTransform label = resultSecondaryProgressText.rectTransform;
            label.anchorMin = new Vector2(0f, 1f);
            label.anchorMax = new Vector2(1f, 1f);
            label.pivot = new Vector2(0.5f, 1f);
            label.anchoredPosition = new Vector2(0f, -6f);
            label.sizeDelta = new Vector2(-24f, 104f);
            resultSecondaryProgressText.alignment = TextAnchor.UpperLeft;
            resultSecondaryProgressText.resizeTextForBestFit = false;
            resultSecondaryProgressText.horizontalOverflow = HorizontalWrapMode.Wrap;
            resultSecondaryProgressText.verticalOverflow = VerticalWrapMode.Overflow;
            resultSecondaryProgressText.raycastTarget = false;

            resultChangesScroll = viewportObject.GetComponent<ScrollRect>();
            resultChangesScroll.content = resultChangesContent;
            resultChangesScroll.viewport = viewport;
            resultChangesScroll.horizontal = false;
            resultChangesScroll.vertical = true;
            resultChangesScroll.movementType = ScrollRect.MovementType.Clamped;
            resultChangesScroll.inertia = false;
            resultChangesScroll.scrollSensitivity = 25f;
            viewportObject.SetActive(false);
            resultChangesHeadingText.gameObject.SetActive(false);
        }

        private bool UpdateResultChangesScroll()
        {
            string changes = ResultChangedSystemsLine();
            bool visible = !string.IsNullOrWhiteSpace(changes);
            if (resultMasteryText != null)
                resultMasteryText.gameObject.SetActive(false);
            if (resultSecondaryProgressText == null ||
                resultChangesScroll == null || resultChangesContent == null)
                return false;
            if (resultChangesHeadingText != null)
                resultChangesHeadingText.gameObject.SetActive(visible);
            resultChangesScroll.gameObject.SetActive(visible);
            if (!visible)
            {
                resultChangesLayoutSequence = -1;
                return false;
            }

            bool changed = resultChangesLayoutSequence != activityManager.ResultSequence ||
                resultSecondaryProgressText.text != changes;
            if (!changed)
                return true;
            resultChangesLayoutSequence = activityManager.ResultSequence;
            resultSecondaryProgressText.text = changes;
            float contentHeight = Mathf.Max(114f,
                resultSecondaryProgressText.preferredHeight + 14f);
            resultChangesContent.sizeDelta = new Vector2(0f, contentHeight);
            resultSecondaryProgressText.rectTransform.sizeDelta =
                new Vector2(-24f, contentHeight - 10f);
            Canvas.ForceUpdateCanvases();
            resultChangesScroll.verticalNormalizedPosition = 1f;
            return true;
        }

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

            if (!activityManager.ResultSuccess)
                return resultChangesRenderedText = string.Empty;

            if (resultChangesBefore == null ||
                resultChangesBefore.Sequence != sequence)
            {
                StringBuilder fallback = new StringBuilder();
                if (activityManager.ResultMasteryXp > 0)
                    AppendResultChange(fallback, MotorCityLocalization.Format(
                        "hud.result_mastery", activityManager.ResultMasteryXp));
                AppendResultChange(fallback, activityManager.ResultSecondaryProgress);
                return resultChangesRenderedText = fallback.ToString();
            }

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
