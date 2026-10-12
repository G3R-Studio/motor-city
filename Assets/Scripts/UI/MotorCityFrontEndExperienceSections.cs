using MotorCity.Localization;
using MotorCity.Platform;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
        private int playerExperienceJournalFilter;
        private Button[] playerExperienceJournalFilterButtons;
        private GameObject playerExperienceClubPanel;
        private GameObject playerExperienceInspectorPanel;
        private GameObject playerExperiencePixiePanel;
        private Text playerExperienceClubText;
        private Text playerExperienceInspectorText;
        private Text playerExperiencePixieText;
        private GameObject playerExperienceGaragePanel;
        private GameObject playerExperienceAchievementsPanel;
        private GameObject playerExperienceNightClubPanel;
        private Text playerExperienceGarageText;
        private Text playerExperienceAchievementsText;
        private Text playerExperienceNightClubText;
        private GameObject playerExperienceLastOverviewSelection;
        private GameObject lastCareerFocus;
        private Text playerExperienceVisitHintText;
        private bool playerExperienceHintWasShown;
        private float playerExperienceSourcePollTimer;
        private int observedCredits;
        private int observedReputation;
        private int observedVehicleIndex;
        private int observedResultSequence;
        private long observedDay;
        private long observedWeek;

        // Only scalar stamps are checked while the career window is open.
        // Expensive catalogs and journal strings are built on navigation or
        // a changed source, not during each driving/HUD frame.
        private void CapturePlayerExperienceSourceStamp()
        {
            observedCredits = wallet == null ? 0 : wallet.Credits;
            observedReputation = reputation == null ? 0 : reputation.Reputation;
            observedVehicleIndex = hud == null ? -1 :
                hud.PlayerExperienceSelectedVehicleIndex;
            observedResultSequence = hud == null ? -1 :
                hud.PlayerExperienceResultSequence;
            long now = MotorCityPlatform.ServerUnixTime;
            observedDay = now / 86400L;
            observedWeek = now / 604800L;
            playerExperienceSourcePollTimer = 0f;
        }

        private void RefreshVisiblePlayerExperienceIfChanged()
        {
            if (playerExperienceRoot == null ||
                !playerExperienceRoot.activeInHierarchy)
                return;
            playerExperienceSourcePollTimer += Time.unscaledDeltaTime;
            if (playerExperienceSourcePollTimer < 0.75f)
                return;
            playerExperienceSourcePollTimer = 0f;
            long now = MotorCityPlatform.ServerUnixTime;
            int credits = wallet == null ? 0 : wallet.Credits;
            int rep = reputation == null ? 0 : reputation.Reputation;
            int vehicle = hud == null ? -1 :
                hud.PlayerExperienceSelectedVehicleIndex;
            int result = hud == null ? -1 :
                hud.PlayerExperienceResultSequence;
            if (credits == observedCredits && rep == observedReputation &&
                vehicle == observedVehicleIndex &&
                result == observedResultSequence &&
                now / 86400L == observedDay &&
                now / 604800L == observedWeek)
                return;

            CapturePlayerExperienceSourceStamp();
            if (playerExperienceHomePanel != null &&
                playerExperienceHomePanel.activeSelf)
                RefreshPlayerExperience();
            else
            {
                GameObject active = playerExperienceActivitiesPanel;
                if (playerExperienceJournalPanel != null &&
                    playerExperienceJournalPanel.activeSelf)
                    active = playerExperienceJournalPanel;
                else if (playerExperienceClubPanel != null &&
                    playerExperienceClubPanel.activeSelf)
                    active = playerExperienceClubPanel;
                else if (playerExperienceInspectorPanel != null &&
                    playerExperienceInspectorPanel.activeSelf)
                    active = playerExperienceInspectorPanel;
                else if (playerExperiencePixiePanel != null &&
                    playerExperiencePixiePanel.activeSelf)
                    active = playerExperiencePixiePanel;
                else if (playerExperienceGaragePanel != null &&
                    playerExperienceGaragePanel.activeSelf)
                    active = playerExperienceGaragePanel;
                else if (playerExperienceAchievementsPanel != null &&
                    playerExperienceAchievementsPanel.activeSelf)
                    active = playerExperienceAchievementsPanel;
                else if (playerExperienceNightClubPanel != null &&
                    playerExperienceNightClubPanel.activeSelf)
                    active = playerExperienceNightClubPanel;
                RefreshPlayerExperienceSections(active);
            }
            Canvas.ForceUpdateCanvases();
            RefreshFrontEndScrollCues();
        }

        private void BuildPlayerExperienceSections()
        {
            FrontEndModalWindow activities =
                CreateFrontEndModalWindow(
                    playerExperienceRoot.transform,
                    "Player Experience Activities Panel",
                    MotorCityLocalization.Text("ux.activities.title"),
                    new Vector2(1020f, 730f),
                    112f,
                    false);

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
                    20,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    0f,
                    new Color(0.90f, 0.95f, 1f, 1f));

            RectTransform activitiesBack =
                CreateFrontEndActionRow(
                    activities.Footer,
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
                    112f,
                    false);

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

            // Seven read-only filters, arranged in two rows and never
            // starting or claiming tasks.
            playerExperienceJournalFilterButtons = new Button[7];
            RectTransform journalFiltersTop = CreateFrontEndActionRow(
                journal.Content, "Journal Filters Top", 50f, 8f);
            RectTransform journalFiltersBottom = CreateFrontEndActionRow(
                journal.Content, "Journal Filters Bottom", 50f, 8f);
            for (int i = 0; i < playerExperienceJournalFilterButtons.Length; i++)
            {
                int filter = i;
                Button filterButton = CreateFrontEndLayoutButton(
                    i < 4 ? journalFiltersTop : journalFiltersBottom,
                    MotorCityLocalization.Text(JournalFilterKey(i)),
                    () => SelectPlayerExperienceJournalFilter(filter),
                    186f, 50f);
                playerExperienceJournalFilterButtons[i] = filterButton;
                // Journal filter names must remain on one line, including
                // the selected bullet and longer Russian translations.
                Text filterLabel = filterButton.GetComponentInChildren<Text>();
                if (filterLabel != null)
                {
                    filterLabel.fontSize = 18;
                    filterLabel.resizeTextForBestFit = true;
                    filterLabel.resizeTextMinSize = 14;
                    filterLabel.resizeTextMaxSize = 18;
                    filterLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                    filterLabel.verticalOverflow = VerticalWrapMode.Truncate;
                    RectTransform labelRect = filterLabel.rectTransform;
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(5f, 3f);
                    labelRect.offsetMax = new Vector2(-5f, -3f);
                }
            }

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

            RectTransform journalBack =
                CreateFrontEndActionRow(
                    journal.Footer,
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

            playerExperienceGaragePanel =
                BuildPlayerExperienceDetail(
                    "Player Experience Garage Panel",
                    "ux.garage.title",
                    "ux.garage.subtitle",
                    "ux.garage.hint",
                    out playerExperienceGarageText);

            playerExperienceAchievementsPanel =
                BuildPlayerExperienceDetail(
                    "Player Experience Achievements Panel",
                    "ux.achievements.title",
                    "ux.achievements.subtitle",
                    "ux.achievements.hint",
                    out playerExperienceAchievementsText);

            playerExperienceNightClubPanel =
                BuildPlayerExperienceDetail(
                    "Player Experience Night Club Panel",
                    "ux.nightclub.title",
                    "ux.nightclub.subtitle",
                    "ux.nightclub.hint",
                    out playerExperienceNightClubText);

            playerExperienceGaragePanel.SetActive(false);
            playerExperienceAchievementsPanel.SetActive(false);
            playerExperienceNightClubPanel.SetActive(false);
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
                    112f,
                    false);

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
                    21,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    0f,
                    new Color(0.90f, 0.95f, 1f, 1f));

            CreateFrontEndLayoutText(
                page.Content,
                MotorCityLocalization.Text(hintKey),
                20,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                90f,
                new Color(0.72f, 0.80f, 0.94f, 1f));

            RectTransform backRow =
                CreateFrontEndActionRow(
                    page.Footer,
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

#if UNITY_EDITOR || DEBUG
        // QA-only navigation: reuse the real sections, never create a
        // second modal Canvas or change progression state.
        public bool DebugShowFrontEndPage(int page)
        {
            // Main-menu pages are only safe while the menu owns the canvas.
            // Do not hijack an active game or the in-pause experience screen.
            if (canvas == null || !canvas.gameObject.activeInHierarchy ||
                playerExperienceReturnToPause != null ||
                (mainRoot != null && !mainRoot.activeSelf &&
                 (aboutRoot == null || !aboutRoot.activeSelf) &&
                 (settingsRoot == null || !settingsRoot.activeSelf)))
                return false;
            switch (page)
            {
                case 0: ShowMainMenu(); break;
                case 1: ShowAbout(); break;
                case 2: ShowSettings(); break;
                default: return false;
            }
            return true;
        }

        public bool DebugShowExperienceSection(int section)
        {
            if (playerExperienceRoot == null ||
                !playerExperienceRoot.activeInHierarchy)
                return false;
            GameObject target = section switch
            {
                1 => playerExperienceActivitiesPanel,
                2 => playerExperienceJournalPanel,
                3 => playerExperienceClubPanel,
                4 => playerExperienceInspectorPanel,
                5 => playerExperiencePixiePanel,
                6 => playerExperienceGaragePanel,
                7 => playerExperienceAchievementsPanel,
                8 => playerExperienceNightClubPanel,
                _ => null
            };
            SwitchPlayerExperiencePage(target);
            return true;
        }
#endif

        private static string JournalFilterKey(int filter)
        {
            return filter switch
            {
                0 => "ux.journal.filter.story",
                1 => "ux.journal.filter.season",
                2 => "ux.journal.filter.today",
                3 => "ux.journal.filter.contracts",
                4 => "ux.journal.filter.career",
                5 => "ux.journal.filter.legends",
                _ => "ux.journal.filter.night"
            };
        }

        private void SelectPlayerExperienceJournalFilter(int filter)
        {
            playerExperienceJournalFilter = Mathf.Clamp(filter, 0, 6);
            RefreshPlayerExperienceJournalFilters();
            RefreshPlayerExperienceSections(playerExperienceJournalPanel);
            Canvas.ForceUpdateCanvases();
            RefreshFrontEndScrollCues();
            ScrollRect scroll = playerExperienceJournalPanel == null
                ? null : playerExperienceJournalPanel.GetComponentInChildren<ScrollRect>();
            if (scroll != null)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        private void RefreshPlayerExperienceJournalFilters()
        {
            if (playerExperienceJournalFilterButtons == null) return;
            for (int i = 0; i < playerExperienceJournalFilterButtons.Length; i++)
            {
                Button button = playerExperienceJournalFilterButtons[i];
                if (button == null) continue;
                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = (i == playerExperienceJournalFilter ? "• " : "") +
                        MotorCityLocalization.Text(JournalFilterKey(i));
                    label.color = i == playerExperienceJournalFilter
                        ? new Color(0.33f, 0.79f, 1f, 1f)
                        : new Color(0.94f, 0.96f, 1f, 1f);
                }
            }
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

        private void ShowPlayerExperienceGarage()
        {
            SwitchPlayerExperiencePage(playerExperienceGaragePanel);
        }

        private void ShowPlayerExperienceAchievements()
        {
            SwitchPlayerExperiencePage(playerExperienceAchievementsPanel);
        }

        private void ShowPlayerExperienceNightClub()
        {
            SwitchPlayerExperiencePage(playerExperienceNightClubPanel);
        }

        private void SwitchPlayerExperiencePage(
            GameObject selected, bool resetOverviewScroll = false)
        {
            lastCareerFocus = null;
            bool returningToOverview =
                selected == null &&
                playerExperienceHomePanel != null &&
                !playerExperienceHomePanel.activeSelf;

            // A controller/keyboard player should return to the section
            // they opened, not jump back to the first button each time.
            if (selected != null && playerExperienceHomePanel != null &&
                playerExperienceHomePanel.activeSelf &&
                EventSystem.current != null)
            {
                GameObject focused =
                    EventSystem.current.currentSelectedGameObject;
                if (focused != null &&
                    focused.transform.IsChildOf(
                        playerExperienceHomePanel.transform))
                {
                    playerExperienceLastOverviewSelection = focused;
                }
            }

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
            playerExperienceGaragePanel?.SetActive(
                selected == playerExperienceGaragePanel);
            playerExperienceAchievementsPanel?.SetActive(
                selected == playerExperienceAchievementsPanel);
            playerExperienceNightClubPanel?.SetActive(
                selected == playerExperienceNightClubPanel);

            if (selected == null)
                RefreshPlayerExperience();
            else
                RefreshPlayerExperienceSections(selected);

            // Every page starts at the beginning rather than retaining
            // the previous section's scrollbar position.
            GameObject active = selected ?? playerExperienceHomePanel;
            ScrollRect scroll = active == null
                ? null
                : active.GetComponentInChildren<ScrollRect>();
            if (scroll != null)
            {
                // Preserve the overview scroll offset on Back, so returning
                // from a lower section does not jump to the first row.
                // Opening a fresh modal or entering any detail starts at top.
                scroll.StopMovement();
                if (selected != null ||
                    resetOverviewScroll ||
                    !returningToOverview)
                    scroll.verticalNormalizedPosition = 1f;
            }

            // Keep keyboard/controller selection visible, and restore the
            // exact overview action on Back if it is still interactable.
            if (active != null && EventSystem.current != null)
            {
                Button previous = selected == null &&
                    playerExperienceLastOverviewSelection != null
                        ? playerExperienceLastOverviewSelection
                            .GetComponent<Button>()
                        : null;
                if (previous != null && previous.IsActive() &&
                    previous.interactable)
                {
                    EventSystem.current.SetSelectedGameObject(
                        previous.gameObject);
                }
                else
                {
                    foreach (Button button in
                        active.GetComponentsInChildren<Button>())
                    {
                        if (!button.IsActive() ||
                            !button.interactable ||
                            button.gameObject.name == "X Button")
                            continue;

                        EventSystem.current.SetSelectedGameObject(
                            button.gameObject);
                        break;
                    }
                }
            }

            // Rebuild layout after the active page receives its live
            // snapshot, then re-evaluate actual overflow and scroll cues.
            // This also covers long translated goals and narrow phones.
            Canvas.ForceUpdateCanvases();
            RefreshFrontEndScrollCues();
        }

        private void KeepCareerSelectionVisible()
        {
            if (playerExperienceRoot == null ||
                !playerExperienceRoot.activeInHierarchy ||
                EventSystem.current == null)
                return;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == lastCareerFocus)
                return;
            lastCareerFocus = selected;
            if (selected == null ||
                !selected.transform.IsChildOf(playerExperienceRoot.transform))
                return;

            // Footer Back/X is outside the clipped content and needs no scroll.
            ScrollRect scroll = selected.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null ||
                scroll.viewport == null ||
                !selected.transform.IsChildOf(scroll.content))
                return;

            Canvas.ForceUpdateCanvases();
            float range = scroll.content.rect.height - scroll.viewport.rect.height;
            if (range <= 1f)
                return;

            Bounds focus = RectTransformUtility.CalculateRelativeRectTransformBounds(
                scroll.viewport, selected.transform);
            Rect view = scroll.viewport.rect;
            float change = focus.min.y < view.yMin + 12f
                ? focus.min.y - view.yMin - 12f
                : focus.max.y > view.yMax - 12f
                    ? focus.max.y - view.yMax + 12f
                    : 0f;
            if (Mathf.Abs(change) <= 0.5f)
                return;

            scroll.StopMovement();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(
                scroll.verticalNormalizedPosition + change / range);
        }

        // Optional keyboard/controller-friendly document scrolling.
        // No new gameplay shortcuts: these keys are read only while this
        // front-end page is visible (also works while gameplay is paused).
        private void HandlePlayerExperienceScrollShortcuts()
        {
            if (playerExperienceRoot == null ||
                !playerExperienceRoot.activeInHierarchy ||
                Keyboard.current == null)
                return;

            Keyboard keyboard = Keyboard.current;
            bool down = keyboard.pageDownKey.wasPressedThisFrame;
            bool up = keyboard.pageUpKey.wasPressedThisFrame;
            bool home = keyboard.homeKey.wasPressedThisFrame;
            bool end = keyboard.endKey.wasPressedThisFrame;
            if (!down && !up && !home && !end)
                return;

            // No allocations on keyboard page navigation: only one of
            // these nine screens can be active at a time.
            GameObject active = playerExperienceHomePanel;
            if (playerExperienceActivitiesPanel != null &&
                playerExperienceActivitiesPanel.activeSelf)
                active = playerExperienceActivitiesPanel;
            else if (playerExperienceJournalPanel != null &&
                     playerExperienceJournalPanel.activeSelf)
                active = playerExperienceJournalPanel;
            else if (playerExperienceClubPanel != null &&
                     playerExperienceClubPanel.activeSelf)
                active = playerExperienceClubPanel;
            else if (playerExperienceInspectorPanel != null &&
                     playerExperienceInspectorPanel.activeSelf)
                active = playerExperienceInspectorPanel;
            else if (playerExperiencePixiePanel != null &&
                     playerExperiencePixiePanel.activeSelf)
                active = playerExperiencePixiePanel;
            else if (playerExperienceGaragePanel != null &&
                     playerExperienceGaragePanel.activeSelf)
                active = playerExperienceGaragePanel;
            else if (playerExperienceAchievementsPanel != null &&
                     playerExperienceAchievementsPanel.activeSelf)
                active = playerExperienceAchievementsPanel;
            else if (playerExperienceNightClubPanel != null &&
                     playerExperienceNightClubPanel.activeSelf)
                active = playerExperienceNightClubPanel;
            ScrollRect scroll = active == null
                ? null
                : active.GetComponentInChildren<ScrollRect>();
            if (scroll == null || scroll.content == null ||
                scroll.viewport == null)
                return;

            float contentHeight = scroll.content.rect.height;
            float viewportHeight = scroll.viewport.rect.height;
            if (contentHeight <= viewportHeight + 4f)
                return;

            // A page-key jump must not fight momentum from a prior touch
            // swipe or mouse-wheel scroll on this ScrollRect.
            scroll.StopMovement();
            if (home || end)
            {
                scroll.verticalNormalizedPosition = home ? 1f : 0f;
                return;
            }

            float page = Mathf.Clamp(
                viewportHeight / contentHeight * 0.85f,
                0.08f, 1f);
            scroll.verticalNormalizedPosition = Mathf.Clamp01(
                scroll.verticalNormalizedPosition + (up ? page : -page));
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

        private void RefreshPlayerExperienceSections(GameObject selected = null)
        {
            if (playerExperienceActivitiesText != null &&
                (selected == null || selected == playerExperienceActivitiesPanel))
            {
                playerExperienceActivitiesText.text =
                    hud == null
                        ? MotorCityLocalization.Text("ux.sections.not_ready")
                        : hud.PlayerExperienceActivitiesLine;
            }

            if (playerExperienceJournalText != null &&
                (selected == null || selected == playerExperienceJournalPanel))
            {
                playerExperienceJournalText.text =
                    hud == null
                        ? MotorCityLocalization.Text("ux.sections.not_ready")
                        : hud.PlayerExperienceJournalLineForFilter(playerExperienceJournalFilter);
                RefreshPlayerExperienceJournalFilters();
            }

            if (playerExperienceClubText != null &&
                (selected == null || selected == playerExperienceClubPanel))
            {
                playerExperienceClubText.text =
                    hud == null
                        ? MotorCityLocalization.Text("ux.sections.not_ready")
                        : hud.PlayerExperienceClubLine;
            }

            if (playerExperienceInspectorText != null &&
                (selected == null || selected == playerExperienceInspectorPanel))
            {
                playerExperienceInspectorText.text =
                    hud == null
                        ? MotorCityLocalization.Text("ux.sections.not_ready")
                        : hud.PlayerExperienceInspectorLine;
            }

            if (playerExperiencePixieText != null &&
                (selected == null || selected == playerExperiencePixiePanel))
            {
                playerExperiencePixieText.text =
                    hud == null
                        ? MotorCityLocalization.Text("ux.sections.not_ready")
                        : hud.PlayerExperiencePixieLine;
            }

            if (playerExperienceGarageText != null &&
                (selected == null || selected == playerExperienceGaragePanel))
                playerExperienceGarageText.text = hud == null
                    ? MotorCityLocalization.Text("ux.sections.not_ready")
                    : hud.PlayerExperienceGarageLine;

            if (playerExperienceAchievementsText != null &&
                (selected == null || selected == playerExperienceAchievementsPanel))
                playerExperienceAchievementsText.text = hud == null
                    ? MotorCityLocalization.Text("ux.sections.not_ready")
                    : hud.PlayerExperienceAchievementsLine;

            if (playerExperienceNightClubText != null &&
                (selected == null || selected == playerExperienceNightClubPanel))
                playerExperienceNightClubText.text = hud == null
                    ? MotorCityLocalization.Text("ux.sections.not_ready")
                    : hud.PlayerExperienceNightClubLine;

            RefreshPlayerExperienceTextHeights();
        }
    }
}
