using MotorCity.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class MotorCityFrontEndFlow
    {
        // One shared front-end Canvas: the overview and its detail pages
        // never overlap. No independent pause state, save keys or rewards.
        private GameObject playerExperienceHomePanel;
        private GameObject playerExperienceActivitiesPanel;
        private GameObject playerExperienceJournalPanel;
        private Text playerExperienceActivitiesText;
        private Text playerExperienceJournalText;

        private void BuildPlayerExperienceSections()
        {
            FrontEndModalWindow activities =
                CreateFrontEndModalWindow(
                    playerExperienceRoot.transform,
                    "Player Experience Activities Panel",
                    MotorCityLocalization.Text("ux.activities.title"),
                    new Vector2(1020f, 730f),
                    112f);

            playerExperienceActivitiesPanel =
                activities.Panel.gameObject;

            CreateFrontEndLayoutText(
                activities.Content,
                MotorCityLocalization.Text("ux.activities.subtitle"),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                46f,
                new Color(0.48f, 0.76f, 1f, 1f));

            playerExperienceActivitiesText =
                CreateFrontEndLayoutText(
                    activities.Content,
                    string.Empty,
                    23,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    340f,
                    new Color(0.90f, 0.95f, 1f, 1f));

            CreateFrontEndLayoutText(
                activities.Content,
                MotorCityLocalization.Text("ux.activities.hint"),
                19,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                64f,
                new Color(0.72f, 0.80f, 0.94f, 1f));

            CreateFrontEndFlexibleSpacer(
                activities.Content,
                "Player Experience Activities Spacer");

            RectTransform activitiesBack =
                CreateFrontEndActionRow(
                    activities.Content,
                    "Player Experience Activities Back",
                    58f);

            CreateFrontEndLayoutButton(
                activitiesBack,
                MotorCityLocalization.Text("ux.home.back"),
                ShowPlayerExperienceHome,
                320f,
                58f);

            FrontEndModalWindow journal =
                CreateFrontEndModalWindow(
                    playerExperienceRoot.transform,
                    "Player Experience Journal Panel",
                    MotorCityLocalization.Text("ux.journal.title"),
                    new Vector2(1020f, 730f),
                    112f);

            playerExperienceJournalPanel =
                journal.Panel.gameObject;

            CreateFrontEndLayoutText(
                journal.Content,
                MotorCityLocalization.Text("ux.journal.subtitle"),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                46f,
                new Color(0.48f, 0.76f, 1f, 1f));

            playerExperienceJournalText =
                CreateFrontEndLayoutText(
                    journal.Content,
                    string.Empty,
                    21,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    332f,
                    new Color(0.90f, 0.95f, 1f, 1f));

            CreateFrontEndLayoutText(
                journal.Content,
                MotorCityLocalization.Text("ux.journal.hint"),
                19,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                70f,
                new Color(0.72f, 0.80f, 0.94f, 1f));

            CreateFrontEndFlexibleSpacer(
                journal.Content,
                "Player Experience Journal Spacer");

            RectTransform journalBack =
                CreateFrontEndActionRow(
                    journal.Content,
                    "Player Experience Journal Back",
                    58f);

            CreateFrontEndLayoutButton(
                journalBack,
                MotorCityLocalization.Text("ux.home.back"),
                ShowPlayerExperienceHome,
                320f,
                58f);

            playerExperienceActivitiesPanel.SetActive(false);
            playerExperienceJournalPanel.SetActive(false);
        }

        private void ShowPlayerExperienceHome()
        {
            playerExperienceHomePanel?.SetActive(true);
            playerExperienceActivitiesPanel?.SetActive(false);
            playerExperienceJournalPanel?.SetActive(false);
            RefreshPlayerExperience();
        }

        private void ShowPlayerExperienceActivities()
        {
            playerExperienceHomePanel?.SetActive(false);
            playerExperienceJournalPanel?.SetActive(false);
            playerExperienceActivitiesPanel?.SetActive(true);
            RefreshPlayerExperienceSections();
        }

        private void ShowPlayerExperienceJournal()
        {
            playerExperienceHomePanel?.SetActive(false);
            playerExperienceActivitiesPanel?.SetActive(false);
            playerExperienceJournalPanel?.SetActive(true);
            RefreshPlayerExperienceSections();
        }

        private void HandlePlayerExperienceBack()
        {
            if (playerExperienceActivitiesPanel != null &&
                playerExperienceActivitiesPanel.activeSelf ||
                playerExperienceJournalPanel != null &&
                playerExperienceJournalPanel.activeSelf)
            {
                ShowPlayerExperienceHome();
                return;
            }

            ReturnFromPlayerExperience();
        }

        private void RefreshPlayerExperienceSections()
        {
            if (playerExperienceActivitiesText != null)
            {
                playerExperienceActivitiesText.text =
                    hud == null
                        ? MotorCityLocalization.Text("ux.sections.not_ready")
                        : hud.PlayerExperienceActivitiesLine;
            }

            if (playerExperienceJournalText != null)
            {
                playerExperienceJournalText.text =
                    hud == null
                        ? MotorCityLocalization.Text("ux.sections.not_ready")
                        : hud.PlayerExperienceJournalLine;
            }
        }
    }
}
