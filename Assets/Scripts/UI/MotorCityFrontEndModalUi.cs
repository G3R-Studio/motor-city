using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class MotorCityFrontEndFlow
    {
        private const float FrontEndModalHorizontalPadding = 54f;
        private const float FrontEndModalRowSpacing = 16f;
        private const float FrontEndModalButtonSpacing = 14f;

        // All pages belong to one front-end Canvas. Reflow only on a
        // screen/safe-area/scale change, never by polling gameplay systems.
        private readonly System.Collections.Generic.List<RectTransform>
            playerExperienceModalPanels = new();
        private Vector2Int lastPlayerExperienceLayoutSize;
        private Rect lastPlayerExperienceSafeArea;
        private float lastPlayerExperienceCanvasScale;
        private RectTransform controlChoiceModalPanel;

        private sealed class FrontEndScrollCue
        {
            public ScrollRect Scroll;
            public RectTransform Handle;
            public GameObject Track;
            public Text Hint;
        }

        private readonly System.Collections.Generic.List<FrontEndScrollCue>
            frontEndScrollCues = new();

        private readonly struct FrontEndModalWindow
        {
            public readonly RectTransform Panel;
            public readonly RectTransform Content;
            public readonly RectTransform Footer;
            public readonly Text Title;

            public FrontEndModalWindow(
                RectTransform panel,
                RectTransform content,
                RectTransform footer,
                Text title)
            {
                Panel = panel;
                Content = content;
                Footer = footer;
                Title = title;
            }
        }

        private FrontEndModalWindow CreateFrontEndModalWindow(
            Transform parent,
            string objectName,
            string title,
            Vector2 size,
            float headerHeight = 108f)
        {
            RectTransform panel =
                CreateFrontEndPanel(
                    parent,
                    objectName,
                    Vector2.zero,
                    size);

            if (parent == playerExperienceRoot.transform)
                playerExperienceModalPanels.Add(panel);
            else if (parent == controlChoiceRoot.transform)
                controlChoiceModalPanel = panel;

            Text titleText =
                CreateText(
                    panel,
                    title,
                    44,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.zero,
                    new Vector2(0.5f, 1f));

            titleText.name = objectName + " Title";
            titleText.color = new Color(0.90f, 0.95f, 1f, 1f);
            // Long RU/EN titles remain readable instead of running under X
            // when a narrow phone safe area reduces the available width.
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 18;
            titleText.resizeTextMaxSize = 44;
            titleText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -24f);
            // Reserve the right corner for a permanent touch close target.
            titleRect.sizeDelta = new Vector2(
                -(FrontEndModalHorizontalPadding * 2f + 72f),
                headerHeight - 34f);

            CreateButton(
                panel,
                "X",
                new Vector2(-24f, -22f),
                new Vector2(64f, 60f),
                ReturnFromPlayerExperience,
                new Vector2(1f, 1f));

            // Header and footer stay visible when text grows or the viewport
            // becomes short; only the read-only body can scroll.
            RectTransform viewport =
                new GameObject(
                    objectName + " Viewport",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(RectMask2D),
                    typeof(ScrollRect))
                .GetComponent<RectTransform>();
            viewport.SetParent(panel, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(
                FrontEndModalHorizontalPadding, 116f);
            viewport.offsetMax = new Vector2(
                -FrontEndModalHorizontalPadding, -headerHeight - 6f);
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = true;

            RectTransform content =
                new GameObject(
                    objectName + " Content",
                    typeof(RectTransform))
                .GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout =
                content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 4, 4);
            layout.spacing = FrontEndModalRowSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
            scroll.inertia = true;

            // The track and small arrow stay in the left gutter, outside
            // the clipped text viewport. They appear only on overflow.
            FrontEndScrollCue cue = CreateFrontEndScrollCue(
                panel, scroll, headerHeight);
            frontEndScrollCues.Add(cue);
            scroll.onValueChanged.AddListener(
                _ => UpdateFrontEndScrollCue(cue));

            RectTransform footer =
                new GameObject(
                    objectName + " Footer",
                    typeof(RectTransform))
                .GetComponent<RectTransform>();
            footer.SetParent(panel, false);
            footer.anchorMin = new Vector2(0f, 0f);
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(0.5f, 0f);
            footer.offsetMin = new Vector2(
                FrontEndModalHorizontalPadding, 22f);
            footer.offsetMax = new Vector2(
                -FrontEndModalHorizontalPadding, 98f);

            VerticalLayoutGroup footerLayout =
                footer.gameObject.AddComponent<VerticalLayoutGroup>();
            footerLayout.childAlignment = TextAnchor.MiddleCenter;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = false;
            footerLayout.spacing = 0f;

            return new FrontEndModalWindow(
                panel, content, footer, titleText);
        }

        private void ApplyPlayerExperienceModalLayout(bool force)
        {
            if (canvas == null ||
                playerExperienceModalPanels.Count == 0 ||
                Screen.width <= 0 ||
                Screen.height <= 0)
            {
                return;
            }

            Vector2Int size = new(Screen.width, Screen.height);
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f)
                safe = new Rect(0f, 0f, Screen.width, Screen.height);

            float scale = Mathf.Max(0.01f, canvas.scaleFactor);
            if (!force &&
                size == lastPlayerExperienceLayoutSize &&
                safe == lastPlayerExperienceSafeArea &&
                Mathf.Approximately(scale, lastPlayerExperienceCanvasScale))
            {
                return;
            }

            lastPlayerExperienceLayoutSize = size;
            lastPlayerExperienceSafeArea = safe;
            lastPlayerExperienceCanvasScale = scale;

            Vector2 position =
                (safe.center - new Vector2(
                    Screen.width * 0.5f, Screen.height * 0.5f)) / scale;
            Vector2 panelSize = new(
                Mathf.Min(1020f, Mathf.Max(1f, safe.width / scale - 48f)),
                Mathf.Min(730f, Mathf.Max(1f, safe.height / scale - 48f)));

            // The control selection window must not inherit the much
            // taller career overview dimensions.
            if (controlChoiceModalPanel != null)
            {
                controlChoiceModalPanel.anchoredPosition = position;
                controlChoiceModalPanel.sizeDelta = new Vector2(
                    Mathf.Min(980f,
                        Mathf.Max(1f, safe.width / scale - 32f)),
                    Mathf.Min(576f,
                        Mathf.Max(1f, safe.height / scale - 32f)));
            }

            foreach (RectTransform panel in playerExperienceModalPanels)
            {
                if (panel == null)
                    continue;
                panel.anchoredPosition = position;
                panel.sizeDelta = panelSize;
            }

            // A 3-button horizontal row must never spill out of the clipped
            // viewport. Switch the two overview rows to vertical on narrow
            // panels, leaving all touch actions reachable by scrolling.
            bool stacked = panelSize.x < 960f;
            ConfigurePlayerExperienceActionRow(
                playerExperienceSectionActions, 2, 330f,
                panelSize.x - 2f * FrontEndModalHorizontalPadding, stacked);
            ConfigurePlayerExperienceActionRow(
                playerExperienceProgressActions, 3, 270f,
                panelSize.x - 2f * FrontEndModalHorizontalPadding, stacked);
            ConfigurePlayerExperienceActionRow(
                playerExperienceMoreActions, 3, 270f,
                panelSize.x - 2f * FrontEndModalHorizontalPadding, stacked);

            Vector2 choiceSize = controlChoiceModalPanel == null
                ? Vector2.zero : controlChoiceModalPanel.sizeDelta;
            float choiceContentWidth = Mathf.Max(1f,
                choiceSize.x - 2f * FrontEndModalHorizontalPadding);
            bool stackChoice = choiceSize.x < 700f;
            ConfigureControlChoiceActionRow(
                controlChoiceKeyboardActions, 1, choiceContentWidth, stackChoice);
            ConfigureControlChoiceActionRow(
                controlChoiceWheelActions, 2, choiceContentWidth, stackChoice);
            ConfigureControlChoiceActionRow(
                controlChoiceArrowsActions, 2, choiceContentWidth, stackChoice);

            foreach (RectTransform panel in playerExperienceModalPanels)
                FitFrontEndModalFooter(panel);
            FitFrontEndModalFooter(controlChoiceModalPanel);

            Canvas.ForceUpdateCanvases();
            RefreshPlayerExperienceTextHeights();
            RefreshFrontEndScrollCues();
        }

        private static void ConfigurePlayerExperienceActionRow(
            RectTransform row, int count, float normalButtonWidth,
            float availableWidth, bool stacked)
        {
            if (row == null)
                return;

            // A GridLayoutGroup switches columns without swapping or
            // stacking mutually exclusive LayoutGroup components at runtime.
            GridLayoutGroup grid = row.GetComponent<GridLayoutGroup>();
            if (grid == null)
                return;

            grid.constraintCount = stacked ? 1 : count;
            grid.cellSize = new Vector2(
                stacked
                    ? Mathf.Max(1f, availableWidth - 4f)
                    : normalButtonWidth,
                64f);

            LayoutElement rowSize = row.GetComponent<LayoutElement>();
            rowSize.minHeight = stacked
                ? count * 64f + (count - 1) * FrontEndModalButtonSpacing
                : 64f;
            rowSize.preferredHeight = rowSize.minHeight;
            StretchFrontEndActionLabels(row);
        }

        private FrontEndScrollCue CreateFrontEndScrollCue(
            RectTransform panel, ScrollRect scroll, float headerHeight)
        {
            RectTransform track = new GameObject(
                panel.name + " Scroll Track",
                typeof(RectTransform), typeof(Image))
                .GetComponent<RectTransform>();
            track.SetParent(panel, false);
            track.anchorMin = new Vector2(0f, 0f);
            track.anchorMax = new Vector2(0f, 1f);
            track.offsetMin = new Vector2(20f, 138f);
            track.offsetMax = new Vector2(26f, -headerHeight - 24f);
            Image background = track.GetComponent<Image>();
            background.color = new Color(0.36f, 0.48f, 0.71f, 0.5f);
            background.raycastTarget = false;

            RectTransform handle = new GameObject(
                "Scroll Position", typeof(RectTransform), typeof(Image))
                .GetComponent<RectTransform>();
            handle.SetParent(track, false);
            handle.anchorMin = new Vector2(0f, 0.76f);
            handle.anchorMax = new Vector2(1f, 1f);
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;
            Image marker = handle.GetComponent<Image>();
            marker.color = new Color(0.40f, 0.86f, 1f, 1f);
            marker.raycastTarget = false;

            // The full-width "scroll down" caption used to overlay
            // bottom rows. Keep only a down arrow beneath the left track,
            // entirely outside the viewport (which begins at x = 54).
            Text hint = CreateText(
                panel, "↓",
                21, FontStyle.Bold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.zero, new Vector2(0f, 0f));
            hint.name = "Scroll Down Arrow";
            RectTransform hintRect = hint.rectTransform;
            hintRect.anchorMin = Vector2.zero;
            hintRect.anchorMax = Vector2.zero;
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(23f, 106f);
            hintRect.sizeDelta = new Vector2(30f, 26f);
            hint.color = new Color(0.52f, 0.84f, 1f, 0.95f);
            hint.raycastTarget = false;

            FrontEndScrollCue cue = new FrontEndScrollCue
            {
                Scroll = scroll,
                Handle = handle,
                Track = track.gameObject,
                Hint = hint
            };
            cue.Track.SetActive(false);
            cue.Hint.gameObject.SetActive(false);
            return cue;
        }

        private void RefreshFrontEndScrollCues()
        {
            // Called on opening pages, refresh and resize, not in Update().
            Canvas.ForceUpdateCanvases();
            foreach (FrontEndScrollCue cue in frontEndScrollCues)
                UpdateFrontEndScrollCue(cue);
        }

        private static void UpdateFrontEndScrollCue(FrontEndScrollCue cue)
        {
            if (cue == null || cue.Scroll == null ||
                cue.Scroll.viewport == null || cue.Scroll.content == null)
                return;

            float viewHeight = cue.Scroll.viewport.rect.height;
            float contentHeight = cue.Scroll.content.rect.height;
            bool overflow = viewHeight > 0f &&
                            contentHeight > viewHeight + 4f;
            cue.Track.SetActive(overflow);

            float position = Mathf.Clamp01(
                cue.Scroll.verticalNormalizedPosition);
            cue.Hint.gameObject.SetActive(overflow && position > 0.04f);
            if (!overflow)
                return;

            float ratio = Mathf.Clamp(viewHeight / contentHeight, 0.16f, 1f);
            float bottom = (1f - ratio) * position;
            cue.Handle.anchorMin = new Vector2(0f, bottom);
            cue.Handle.anchorMax = new Vector2(1f, bottom + ratio);
        }

        private static void ConfigureControlChoiceActionRow(
            RectTransform row, int buttonCount, float width, bool stacked)
        {
            if (row == null)
                return;

            GridLayoutGroup grid = row.GetComponent<GridLayoutGroup>();
            if (grid == null)
                return;

            int columns = stacked ? 1 : buttonCount;
            grid.constraintCount = columns;
            float cellWidth = stacked
                ? Mathf.Max(1f, width - 4f)
                : buttonCount == 1
                    ? Mathf.Min(620f, width)
                    : Mathf.Min(340f, (width - FrontEndModalButtonSpacing) / 2f);
            grid.cellSize = new Vector2(Mathf.Max(1f, cellWidth), 72f);

            LayoutElement rowSize = row.GetComponent<LayoutElement>();
            if (rowSize != null)
            {
                float height = stacked && buttonCount > 1
                    ? buttonCount * 72f +
                        (buttonCount - 1) * FrontEndModalButtonSpacing
                    : 72f;
                rowSize.minHeight = height;
                rowSize.preferredHeight = height;
            }

            StretchFrontEndActionLabels(row);
        }

        // The original button labels are built at their designer width.
        // Responsive GridLayoutGroup may use much smaller cells, so labels
        // must follow the real button rect instead of overflowing sideways.
        private static void StretchFrontEndActionLabels(RectTransform row)
        {
            if (row == null)
                return;

            foreach (Button button in row.GetComponentsInChildren<Button>(true))
            {
                Text label = button.GetComponentInChildren<Text>(true);
                if (label == null)
                    continue;

                RectTransform rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(10f, 5f);
                rect.offsetMax = new Vector2(-10f, -5f);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 16;
                label.resizeTextMaxSize = 28;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        private static void FitFrontEndModalFooter(RectTransform panel)
        {
            if (panel == null)
                return;

            Transform footer = panel.Find(panel.name + " Footer");
            if (footer == null)
                return;

            float availableWidth = Mathf.Max(
                1f, panel.rect.width - 2f * FrontEndModalHorizontalPadding);
            foreach (Button button in footer.GetComponentsInChildren<Button>(true))
            {
                LayoutElement element = button.GetComponent<LayoutElement>();
                if (element == null)
                    continue;

                float preferred = panel.name == "Control Choice Panel"
                    ? 220f : 320f;
                float width = Mathf.Min(preferred, availableWidth);
                element.minWidth = width;
                element.preferredWidth = width;
            }
        }

        private void RefreshPlayerExperienceTextHeights()
        {
            SetPlayerExperienceTextHeight(playerExperienceObjectiveText, 110f);
            SetPlayerExperienceTextHeight(playerExperienceMilestoneText, 110f);
            SetPlayerExperienceTextHeight(playerExperienceMissionRewardText, 62f);
            SetPlayerExperienceTextHeight(playerExperienceActivitiesText, 340f);
            SetPlayerExperienceTextHeight(playerExperienceJournalText, 332f);
            SetPlayerExperienceTextHeight(playerExperienceClubText, 264f);
            SetPlayerExperienceTextHeight(playerExperienceInspectorText, 264f);
            SetPlayerExperienceTextHeight(playerExperiencePixieText, 264f);
            SetPlayerExperienceTextHeight(playerExperienceGarageText, 264f);
            SetPlayerExperienceTextHeight(playerExperienceAchievementsText, 264f);
            SetPlayerExperienceTextHeight(playerExperienceNightClubText, 264f);
        }

        private static void SetPlayerExperienceTextHeight(
            Text text, float minimumHeight)
        {
            if (text == null)
                return;
            LayoutElement element = text.GetComponent<LayoutElement>();
            if (element != null)
            {
                element.preferredHeight =
                    Mathf.Max(minimumHeight, text.preferredHeight + 12f);
            }
        }

        private void CreateFrontEndFlexibleSpacer(
            Transform parent,
            string objectName = "Flexible Spacer")
        {
            GameObject spacer =
                new(
                    objectName,
                    typeof(RectTransform),
                    typeof(LayoutElement));

            spacer.transform.SetParent(
                parent,
                false);

            LayoutElement element =
                spacer.GetComponent<LayoutElement>();

            element.minHeight =
                0f;

            element.preferredHeight =
                0f;

            element.flexibleHeight =
                1f;
        }

        private RectTransform CreateFrontEndActionRow(
            Transform parent,
            string objectName,
            float height,
            float spacing =
                FrontEndModalButtonSpacing)
        {
            GameObject rowObject =
                new(
                    objectName,
                    typeof(RectTransform),
                    typeof(LayoutElement));

            rowObject.transform.SetParent(
                parent,
                false);

            RectTransform row =
                rowObject.GetComponent<RectTransform>();

            LayoutElement element =
                rowObject.GetComponent<LayoutElement>();

            element.minHeight =
                height;

            element.preferredHeight =
                height;

            if (objectName == "Keyboard Control Actions" ||
                objectName == "Wheel Control Actions" ||
                objectName == "Arrows Control Actions")
            {
                GridLayoutGroup grid =
                    rowObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(300f, height);
                grid.spacing = new Vector2(spacing, spacing);
                grid.childAlignment = TextAnchor.MiddleCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = objectName == "Keyboard Control Actions" ? 1 : 2;
                return row;
            }

            if (objectName == "Player Experience Section Actions" ||
                objectName == "Player Experience Progress Actions" ||
                objectName == "Player Experience More Actions")
            {
                GridLayoutGroup grid =
                    rowObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(270f, height);
                grid.spacing = new Vector2(spacing, spacing);
                grid.childAlignment = TextAnchor.MiddleCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = objectName ==
                    "Player Experience Section Actions" ? 2 : 3;
                return row;
            }

            HorizontalLayoutGroup layout =
                rowObject.AddComponent<
                    HorizontalLayoutGroup>();

            layout.padding =
                new RectOffset();

            layout.spacing =
                spacing;

            layout.childAlignment =
                TextAnchor.MiddleCenter;

            layout.childControlWidth =
                true;

            layout.childControlHeight =
                true;

            layout.childForceExpandWidth =
                false;

            layout.childForceExpandHeight =
                true;

            return row;
        }

        private Button CreateFrontEndLayoutButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            float preferredWidth = -1f,
            float height = 58f)
        {
            Button button =
                CreateButton(
                    parent,
                    label,
                    Vector2.zero,
                    new Vector2(
                        preferredWidth > 0f
                            ? preferredWidth
                            : 320f,
                        height),
                    action,
                    new Vector2(0.5f, 0.5f));

            LayoutElement element =
                button.gameObject.AddComponent<
                    LayoutElement>();

            element.minHeight =
                height;

            element.preferredHeight =
                height;

            if (preferredWidth > 0f)
            {
                element.minWidth =
                    preferredWidth;

                element.preferredWidth =
                    preferredWidth;

                element.flexibleWidth =
                    0f;
            }
            else
            {
                element.flexibleWidth =
                    1f;
            }

            return button;
        }

        private Text CreateFrontEndLayoutText(
            Transform parent,
            string value,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            float height,
            Color color)
        {
            Text text =
                CreateText(
                    parent,
                    value,
                    fontSize,
                    style,
                    alignment,
                    Vector2.zero,
                    Vector2.zero,
                    new Vector2(0.5f, 0.5f));

            RectTransform rect =
                text.rectTransform;

            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                Vector2.one;

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                Vector2.zero;

            LayoutElement element =
                text.gameObject.AddComponent<
                    LayoutElement>();

            element.minHeight =
                height;

            element.preferredHeight =
                height;

            text.color =
                color;

            return text;
        }

        private void CreateFrontEndSettingsLayoutRow(
            Transform parent,
            string label,
            out Text valueText,
            UnityEngine.Events.UnityAction minusAction,
            UnityEngine.Events.UnityAction plusAction,
            UnityEngine.Events.UnityAction wideAction)
        {
            GameObject rowObject =
                new(
                    label + " Row",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));

            rowObject.transform.SetParent(
                parent,
                false);

            RectTransform row =
                rowObject.GetComponent<RectTransform>();

            LayoutElement rowLayout =
                rowObject.GetComponent<
                    LayoutElement>();

            rowLayout.minHeight =
                88f;

            rowLayout.preferredHeight =
                88f;

            Image background =
                rowObject.GetComponent<Image>();

            background.color =
                Color.clear;

            background.raycastTarget =
                false;

            FrontEndSurface(
                row);

            Text labelText =
                CreateText(
                    row,
                    label,
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero);

            labelText.name =
                "Label";

            labelText.color =
                new Color(
                    0.46f,
                    0.82f,
                    1f,
                    1f);

            RectTransform labelRect =
                labelText.rectTransform;

            labelRect.anchorMin =
                new Vector2(0f, 0f);

            labelRect.anchorMax =
                new Vector2(0.28f, 1f);

            labelRect.offsetMin =
                new Vector2(20f, 10f);

            labelRect.offsetMax =
                new Vector2(-8f, -10f);

            ConfigureFrontEndTextFit(
                labelText,
                14);

            valueText =
                CreateText(
                    row,
                    string.Empty,
                    24,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero);

            valueText.name =
                "Value";

            valueText.color =
                new Color(
                    0.95f,
                    0.96f,
                    1f,
                    1f);

            RectTransform valueRect =
                valueText.rectTransform;

            valueRect.anchorMin =
                new Vector2(0.28f, 0f);

            valueRect.anchorMax =
                new Vector2(0.50f, 1f);

            valueRect.offsetMin =
                new Vector2(4f, 10f);

            valueRect.offsetMax =
                new Vector2(-4f, -10f);

            ConfigureFrontEndTextFit(
                valueText,
                16);

            RectTransform controls =
                new GameObject(
                    "Controls",
                    typeof(RectTransform))
                    .GetComponent<RectTransform>();

            controls.transform.SetParent(
                row,
                false);

            controls.anchorMin =
                new Vector2(0.50f, 0f);

            controls.anchorMax =
                new Vector2(1f, 1f);

            controls.offsetMin =
                new Vector2(8f, 14f);

            controls.offsetMax =
                new Vector2(-16f, -14f);

            HorizontalLayoutGroup layout =
                controls.gameObject.AddComponent<
                    HorizontalLayoutGroup>();

            layout.padding =
                new RectOffset();

            layout.spacing =
                8f;

            layout.childAlignment =
                TextAnchor.MiddleRight;

            layout.childControlWidth =
                true;

            layout.childControlHeight =
                true;

            layout.childForceExpandWidth =
                false;

            layout.childForceExpandHeight =
                true;

            if (minusAction != null)
            {
                CreateFrontEndLayoutButton(
                    controls,
                    "-",
                    minusAction,
                    72f,
                    50f);
            }

            if (plusAction != null)
            {
                CreateFrontEndLayoutButton(
                    controls,
                    "+",
                    plusAction,
                    72f,
                    50f);
            }

            if (wideAction != null)
            {
                string actionLabel =
                    label ==
                    (IsRussian()
                        ? "ЯЗЫК"
                        : "LANGUAGE")
                        ? (IsRussian()
                            ? "СМЕНИТЬ"
                            : "CHANGE")
                        : (IsRussian()
                            ? "ВКЛ / ВЫКЛ"
                            : "ON / OFF");

                CreateFrontEndLayoutButton(
                    controls,
                    actionLabel,
                    wideAction,
                    210f,
                    50f);
            }
        }
    }
}
