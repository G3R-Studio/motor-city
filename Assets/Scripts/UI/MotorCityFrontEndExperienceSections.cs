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
        private GameObject playerExperienceClubPanel;
        private GameObject playerExperienceInspectorPanel;
        private GameObject playerExperiencePixiePanel;
        private Text playerExperienceClubText;
        private Text playerExperienceInspectorText;
        private Text playerExperiencePixieText;

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

            playerExperienceClubPanel =
                BuildPlayerExperienceDetail(
                    "Player Experience Club Panel",
                    "ux.club.title",
                    "ux.club.subtitle",
                    "ux.club.hint",
                    out playerExperienceClubText);

            playerExperienceInspectorPanel =
                BuildPlayerExperienceDetail(
                    "Player Experience Inspector Panel",
                    "ux.inspector.title",
                    "ux.inspector.subtitle",
                    "ux.inspector.hint",
                    out playerExperienceInspectorText);

            playerExperiencePixiePanel =
                BuildPlayerExperienceDetail(
                    "Player Experience Pixie Panel",
                    "ux.pixie.title",
                    "ux.pixie.subtitle",
                    "ux.pixie.hint",
                    out playerExperiencePixieText);

            playerExperienceActivitiesPanel.SetActive(false);
            playerExperienceJournalPanel.SetActive(false);
            playerExperienceClubPanel.SetActive(false);
            playerExperienceInspectorPanel.SetActive(false);
            playerExperiencePixiePanel.SetActive(false);
        }

        private GameObject BuildPlayerExperienceDetail(
            string name,
            string titleKey,
            string subtitleKey,
            string hintKey,
            out Text body)
        {
            FrontEndModalWindow page =
                CreateFrontEndModalWindow(
                    playerExperienceRoot.transform,
                    name,
                    MotorCityLocalization.Text(titleKey),
                    new Vector2(1020f, 730f),
                    112f);

            CreateFrontEndLayoutText(
                page.Content,
                MotorCityLocalization.Text(subtitleKey),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                54f,
                new Color(0.48f, 0.76f, 1f, 1f));

            body =
                CreateFrontEndLayoutText(
                    page.Content,
                    string.Empty,
                    25,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    264f,
                    new Color(0.90f, 0.95f, 1f, 1f));

            CreateFrontEndLayoutText(
                page.Content,
                MotorCityLocalization.Text(hintKey),
                20,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                90f,
                new Color(0.72f, 0.80f, 0.94f, 1f));

            CreateFrontEndFlexibleSpacer(
                page.Content,
                name + " Spacer");

            RectTransform backRow =
                CreateFrontEndActionRow(
                    page.Content,
                    name + " Back",
                    58f);

            CreateFrontEndLayoutButton(
                backRow,
                MotorCityLocalization.Text("ux.home.back"),
                ShowPlayerExperienceHome,
                320f,
                58f);

            return page.Panel.gameObject;
        }

        // A single selected panel is active, including on mobile: no
        // overlays stacked and no additional gameplay pause owner.
        private void ShowPlayerExperienceHome()
        {
            SwitchPlayerExperiencePage(null);
        }

        private void ShowPlayerExperienceActivities()
        {
            SwitchPlayerExperiencePage(playerExperienceActivitiesPanel);
        }

        private void ShowPlayerExperienceJournal()
        {
            SwitchPlayerExperiencePage(playerExperienceJournalPanel);
        }

        private void ShowPlayerExperienceClub()
        {
            SwitchPlayerExperiencePage(playerExperienceClubPanel);
        }

        private void ShowPlayerExperienceInspector()
        {
            SwitchPlayerExperiencePage(playerExperienceInspectorPanel);
        }

        private void ShowPlayerExperiencePixie()
        {
            SwitchPlayerExperiencePage(playerExperiencePixiePanel);
        }

        private void SwitchPlayerExperiencePage(GameObject selected)
        {
            playerExperienceHomePanel?.SetActive(selected == null);
            playerExperienceActivitiesPanel?.SetActive(
                selected == playerExperienceActivitiesPanel);
            playerExperienceJournalPanel?.SetActive(
                selected == playerExperienceJournalPanel);
            playerExperienceClubPanel?.SetActive(
                selected == playerExperienceClubPanel);
            playerExperienceInspectorPanel?.SetActive(
                selected == playerExperienceInspectorPanel);
            playerExperiencePixiePanel?.SetActive(
                selected == playerExperiencePixiePanel);

            if (selected == null)
                RefreshPlayerExperience();
            else
                RefreshPlayerExperienceSections();
        }

        private void HandlePlayerExperienceBack()
        {
            if (playerExperienceHomePanel != null &&
                !playerExperienceHomePanel.activeSelf)
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

            if (playerExperienceClubText != null)
            {
                playerExperienceClubText.text =
                    hud == null
                        ? MotorCityLocalization.Text("ux.sections.not_ready")
                        : hud.PlayerExperienceClubLine;
            }

            if (playerExperienceInspectorText != null)
            {
                playerExperienceInspectorText.text =
                    hud == null
                        ? MotorCityLocalization.Text("ux.sections.not_ready")
                        : hud.PlayerExperienceInspectorLine;
            }

            if (playerExperiencePixieText != null)
            {
                playerExperiencePixieText.text =
                    hud == null
                        ? MotorCityLocalization.Text("ux.sections.not_ready")
                        : hud.PlayerExperiencePixieLine;
            }
        }
    }
}
