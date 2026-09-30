using MotorCity.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private void BuildCharacterCard(
            Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Character Card",
                    new Vector2(
                        22f,
                        -22f),
                    new Vector2(
                        448f,
                        154f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    Color.clear);

            characterPanel =
                panel.gameObject;

            ApplyVillePanelTexture(
                panel,
                0.94f);

            RectTransform portraitFrame =
                CreatePanel(
                    panel,
                    "Character Portrait Frame",
                    new Vector2(
                        16f,
                        -18f),
                    new Vector2(
                        68f,
                        68f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    new Color(
                        0.05f,
                        0.075f,
                        0.11f,
                        1f));

            characterPortraitRoot =
                portraitFrame.gameObject;

            characterPortraitVitya =
                LoadCharacterPortrait(
                    "vitya",
                    "MotorCity/UI/Characters/avatar_vitya");

            characterPortraitTurbo =
                LoadCharacterPortrait(
                    "turbo",
                    "MotorCity/UI/Characters/avatar_turbo");

            characterPortraitNika =
                LoadCharacterPortrait(
                    "nika",
                    "MotorCity/UI/Characters/avatar_nika");

            characterPortraitBublik =
                LoadCharacterPortrait(
                    "bublik",
                    "MotorCity/UI/Characters/avatar_bublik");

            characterPortraitFace =
                CreatePortraitLayer(
                    portraitFrame,
                    "Portrait Face",
                    new Vector2(
                        14f,
                        -15f),
                    new Vector2(
                        30f,
                        34f),
                    new Color(
                        0.88f,
                        0.70f,
                        0.56f,
                        1f));

            characterPortraitHair =
                CreatePortraitLayer(
                    portraitFrame,
                    "Portrait Hair",
                    new Vector2(
                        12f,
                        -10f),
                    new Vector2(
                        34f,
                        13f),
                    new Color(
                        0.12f,
                        0.13f,
                        0.15f,
                        1f));

            characterPortraitLeftDetail =
                CreatePortraitLayer(
                    portraitFrame,
                    "Portrait Detail Left",
                    new Vector2(
                        17f,
                        -28f),
                    new Vector2(
                        5f,
                        5f),
                    TextColor);

            characterPortraitRightDetail =
                CreatePortraitLayer(
                    portraitFrame,
                    "Portrait Detail Right",
                    new Vector2(
                        36f,
                        -28f),
                    new Vector2(
                        5f,
                        5f),
                    TextColor);

            GameObject portraitObject =
                new(
                    "Character Portrait",
                    typeof(RectTransform),
                    typeof(Image));

            portraitObject.transform.SetParent(
                portraitFrame,
                false);

            RectTransform portraitRect =
                portraitObject.GetComponent<RectTransform>();

            portraitRect.anchorMin =
                Vector2.zero;

            portraitRect.anchorMax =
                Vector2.one;

            portraitRect.offsetMin =
                new Vector2(
                    3f,
                    3f);

            portraitRect.offsetMax =
                new Vector2(
                    -3f,
                    -3f);

            characterPortraitImage =
                portraitObject.GetComponent<Image>();

            characterPortraitImage.raycastTarget =
                false;

            characterPortraitImage.preserveAspect =
                true;

            characterPortraitImage.type =
                Image.Type.Simple;

            characterPortraitImage.color =
                Color.white;

            characterPortraitImage.enabled =
                false;

            characterSourceText =
                CreateText(
                    panel,
                    "Character Source",
                    13,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        98f,
                        -14f),
                    new Vector2(
                        320f,
                        20f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    SecondaryTextColor);

            characterNameText =
                CreateText(
                    panel,
                    "Character Name",
                    18,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        98f,
                        -36f),
                    new Vector2(
                        320f,
                        26f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    BlueAccent);

            characterMissionTitleText =
                CreateText(
                    panel,
                    "Character Mission Title",
                    12,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        98f,
                        -64f),
                    new Vector2(
                        320f,
                        20f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    SecondaryTextColor);

            characterLineText =
                CreateText(
                    panel,
                    "Character Line",
                    UiBodyFontSize,
                    FontStyle.Bold,
                    TextAnchor.LowerLeft,
                    new Vector2(
                        98f,
                        26f),
                    new Vector2(
                        320f,
                        44f),
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        0f,
                        0f),
                    TextColor);

            characterRewardText =
                CreateText(
                    panel,
                    "Character Reward",
                    UiRewardFontSize,
                    FontStyle.Bold,
                    TextAnchor.LowerRight,
                    new Vector2(
                        -20f,
                        4f),
                    new Vector2(
                        250f,
                        18f),
                    new Vector2(
                        1f,
                        0f),
                    new Vector2(
                        1f,
                        0f),
                    SecondaryTextColor);

            characterPanel.SetActive(
                false);
        }

        private Image CreatePortraitLayer(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            GameObject layer =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image));

            layer.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                layer.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0f, 1f);
            rect.anchorMax =
                new Vector2(0f, 1f);
            rect.pivot =
                new Vector2(0f, 1f);
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                layer.GetComponent<Image>();

            image.color =
                color;

            image.raycastTarget =
                false;

            return
                image;
        }

        private void UpdateCharacterCard()
        {
            if (characterPanel == null)
                return;

            string name =
                string.Empty;

            string line =
                string.Empty;

            string missionTitle =
                string.Empty;

            string rewardLine =
                string.Empty;

            int style =
                -1;

            string portraitId =
                string.Empty;

            if (onboarding != null &&
                !onboarding.IsComplete)
            {
                bool turboStep =
                    onboarding.CurrentStep >= 4;

                name =
                    MotorCityLocalization.Text(
                        turboStep
                            ? "story.character.turbo"
                            : "story.character.vitya");

                line =
                    onboarding.ObjectiveLine;

                missionTitle =
                    MotorCityLocalization.Text(
                        "onboarding.title");

                if (onboarding.CurrentRewardCredits > 0)
                {
                    rewardLine =
                        MotorCityLocalization.Format(
                            "hud.reward_credits_only",
                            onboarding.CurrentRewardCredits);
                }

                style =
                    turboStep
                        ? 3
                        : 0;

                portraitId =
                    turboStep
                        ? "turbo"
                        : "vitya";
            }
            else if (story != null &&
                     !story.IsComplete)
            {
                name =
                    story.CurrentCharacterName;

                line =
                    story.CurrentCharacterLine;

                missionTitle =
                    story.CurrentMissionTitle;

                rewardLine =
                    MotorCityLocalization.Format(
                        "hud.result_reward",
                        story.CurrentCreditsReward,
                        story.CurrentReputationReward);

                style =
                    story.CurrentCharacterStyle;

                portraitId =
                    style switch
                    {
                        1 => "nika",
                        2 => "bublik",
                        3 => "turbo",
                        _ => "vitya"
                    };
            }
            bool visible =
                !string.IsNullOrWhiteSpace(
                    name);

            characterPanel.SetActive(
                visible);

            if (!visible)
                return;

            Color accent =
                style switch
                {
                    1 =>
                        new Color(
                            1f,
                            0.42f,
                            0.66f,
                            1f),
                    2 =>
                        new Color(
                            1f,
                            0.72f,
                            0.16f,
                            1f),
                    3 =>
                        new Color(
                            0.20f,
                            0.82f,
                            1f,
                            1f),
                    _ =>
                        new Color(
                            0.56f,
                            0.86f,
                            0.34f,
                            1f)
                };

            if (story != null &&
                !story.IsComplete &&
                (onboarding == null ||
                 onboarding.IsComplete))
            {
                characterSourceText.text =
                    MotorCityLocalization.Format(
                        "hud.character.story_progress",
                        story.CurrentMissionNumber,
                        story.MissionCount);
            }
            else
            {
                characterSourceText.text =
                    onboarding != null &&
                    !onboarding.IsComplete
                        ? MotorCityLocalization.Format(
                            "hud.character.story_progress",
                            onboarding.CurrentStepNumber,
                            onboarding.StepCount)
                        : MotorCityLocalization.Text(
                            "hud.character.story");
            }

            characterSourceText.color =
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    0.78f);

            ApplyCharacterPortrait(
                style,
                accent,
                portraitId);

            characterNameText.text =
                name;

            characterNameText.color =
                accent;

            if (characterMissionTitleText != null)
            {
                characterMissionTitleText.text =
                    missionTitle;

                characterMissionTitleText.color =
                    new Color(
                        accent.r,
                        accent.g,
                        accent.b,
                        0.82f);
            }

            characterLineText.text =
                line;

            if (characterRewardText != null)
            {
                characterRewardText.text =
                    rewardLine;

                characterRewardText.color =
                    string.IsNullOrWhiteSpace(
                        rewardLine)
                        ? SecondaryTextColor
                        : new Color(
                            1f,
                            0.78f,
                            0.20f,
                            1f);
            }
        }

        private Sprite LoadCharacterPortrait(
            string characterId,
            string resourcePath)
        {
            Sprite sprite =
                Resources.Load<Sprite>(
                    resourcePath);

            if (sprite != null)
            {
                return sprite;
            }

            Texture2D fallbackTexture =
                Resources.Load<Texture2D>(
                    resourcePath);

            if (fallbackTexture != null)
            {
                Debug.LogWarning(
                    "[MotorCity][Portrait] Resource " +
                    $"'{characterId}' exists as Texture2D but not Sprite at " +
                    $"Resources/{resourcePath}. Creating runtime Sprite. " +
                    $"Texture={fallbackTexture.width}x{fallbackTexture.height}. " +
                    "Check Texture Type = Sprite (2D and UI) in Unity importer.",
                    this);

                return
                    Sprite.Create(
                        fallbackTexture,
                        new Rect(
                            0f,
                            0f,
                            fallbackTexture.width,
                            fallbackTexture.height),
                        new Vector2(
                            0.5f,
                            0.5f),
                        100f);
            }

            Object[] matches =
                Resources.LoadAll(
                    "MotorCity/UI/Characters");

            string found =
                matches == null ||
                matches.Length == 0
                    ? "none"
                    : string.Join(
                        ", ",
                        System.Array.ConvertAll(
                            matches,
                            item =>
                                item == null
                                    ? "null"
                                    : item.name +
                                      ":" +
                                      item.GetType().Name));

            Debug.LogError(
                "[MotorCity][Portrait] FAILED to load " +
                $"'{characterId}' at Resources/{resourcePath}. " +
                $"Objects visible in Resources/MotorCity/UI/Characters: {found}.",
                this);

            return null;
        }

        private void ApplyCharacterPortrait(
            int style,
            Color accent,
            string portraitId)
        {
            if (characterPortraitRoot == null ||
                characterPortraitFace == null)
            {
                return;
            }

            Sprite portrait =
                portraitId switch
                {
                    "vitya" =>
                        characterPortraitVitya,
                    "turbo" =>
                        characterPortraitTurbo,
                    "nika" =>
                        characterPortraitNika,
                    "bublik" =>
                        characterPortraitBublik,
                    _ =>
                        null
                };

            bool hasPortrait =
                characterPortraitImage != null &&
                portrait != null;

            if (lastPortraitDebugId != portraitId)
            {
                lastPortraitDebugId =
                    portraitId;

                if (!hasPortrait &&
                    !string.IsNullOrEmpty(
                        portraitId))
                {
                    Debug.LogWarning(
                        "[MotorCity][Portrait] Falling back to procedural portrait for " +
                        $"'{portraitId}' because the Sprite or Image is missing.",
                        this);
                }
            }

            if (characterPortraitImage != null)
            {
                characterPortraitImage.sprite =
                    portrait;

                characterPortraitImage.enabled =
                    hasPortrait;
            }

            characterPortraitFace.gameObject.SetActive(
                !hasPortrait);

            characterPortraitHair.gameObject.SetActive(
                !hasPortrait);

            characterPortraitLeftDetail.gameObject.SetActive(
                !hasPortrait);

            characterPortraitRightDetail.gameObject.SetActive(
                !hasPortrait);

            if (hasPortrait)
                return;

            Color face =
                new(
                    0.88f,
                    0.69f,
                    0.54f,
                    1f);

            Color hair =
                new(
                    0.13f,
                    0.14f,
                    0.17f,
                    1f);

            Vector2 facePosition =
                new(
                    14f,
                    -15f);

            Vector2 faceSize =
                new(
                    30f,
                    34f);

            Vector2 hairPosition =
                new(
                    12f,
                    -10f);

            Vector2 hairSize =
                new(
                    34f,
                    13f);

            Vector2 leftDetailPosition =
                new(
                    17f,
                    -28f);

            Vector2 rightDetailPosition =
                new(
                    36f,
                    -28f);

            Color detailColor =
                new(
                    0.08f,
                    0.09f,
                    0.11f,
                    1f);

            switch (style)
            {
                case 1:
                    face =
                        new Color(
                            0.93f,
                            0.70f,
                            0.60f,
                            1f);

                    hair =
                        new Color(
                            0.74f,
                            0.18f,
                            0.43f,
                            1f);

                    hairPosition =
                        new Vector2(
                            10f,
                            -8f);

                    hairSize =
                        new Vector2(
                            38f,
                            16f);
                    break;

                case 2:
                    face =
                        new Color(
                            0.85f,
                            0.66f,
                            0.50f,
                            1f);

                    hair =
                        new Color(
                            0.18f,
                            0.22f,
                            0.28f,
                            1f);

                    hairPosition =
                        new Vector2(
                            9f,
                            -7f);

                    hairSize =
                        new Vector2(
                            40f,
                            12f);

                    detailColor =
                        new Color(
                            0.12f,
                            0.16f,
                            0.20f,
                            1f);
                    break;

                case 3:
                    face =
                        new Color(
                            0.19f,
                            0.25f,
                            0.30f,
                            1f);

                    hair =
                        accent;

                    facePosition =
                        new Vector2(
                            12f,
                            -16f);

                    faceSize =
                        new Vector2(
                            34f,
                            31f);

                    hairPosition =
                        new Vector2(
                            8f,
                            -8f);

                    hairSize =
                        new Vector2(
                            42f,
                            9f);

                    detailColor =
                        new Color(
                            0.45f,
                            0.95f,
                            1f,
                            1f);

                    leftDetailPosition =
                        new Vector2(
                            15f,
                            -28f);

                    rightDetailPosition =
                        new Vector2(
                            38f,
                            -28f);
                    break;
            }

            characterPortraitFace.color =
                face;

            RectTransform faceRect =
                characterPortraitFace.rectTransform;

            faceRect.anchoredPosition =
                facePosition;
            faceRect.sizeDelta =
                faceSize;

            characterPortraitHair.color =
                hair;

            RectTransform hairRect =
                characterPortraitHair.rectTransform;

            hairRect.anchoredPosition =
                hairPosition;
            hairRect.sizeDelta =
                hairSize;

            characterPortraitLeftDetail.color =
                detailColor;

            characterPortraitRightDetail.color =
                detailColor;

            characterPortraitLeftDetail.rectTransform.anchoredPosition =
                leftDetailPosition;

            characterPortraitRightDetail.rectTransform.anchoredPosition =
                rightDetailPosition;
        }

        private void BuildSeasonPanel(
            Transform canvas)
        {
            RectTransform compact =
                CreatePanel(
                    canvas,
                    "Season Compact Button",
                    new Vector2(
                        22f,
                        -22f),
                    new Vector2(
                        238f,
                        38f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    new Color(
                        0.025f,
                        0.035f,
                        0.052f,
                        0.90f));

            seasonCompactButton =
                compact.gameObject;

            StyleSeasonSurface(
                compact,
                true);

            Image compactImage =
                compact.GetComponent<Image>();

            if (compactImage != null)
            {
                compactImage.raycastTarget =
                    true;
            }

            Button compactButton =
                compact.gameObject.AddComponent<Button>();

            compactButton.targetGraphic =
                compactImage;

            ColorBlock colors =
                compactButton.colors;

            colors.normalColor =
                Color.white;

            colors.highlightedColor =
                new Color(
                    1.08f,
                    1.08f,
                    1.08f,
                    1f);

            colors.pressedColor =
                new Color(
                    0.86f,
                    0.90f,
                    0.96f,
                    1f);

            colors.selectedColor =
                colors.highlightedColor;

            colors.fadeDuration =
                0.08f;

            compactButton.colors =
                colors;

            compactButton.onClick.AddListener(
                () =>
                {
                    seasonDetailsOpen =
                        !seasonDetailsOpen;

                    if (seasonPanel != null)
                    {
                        seasonPanel.SetActive(
                            seasonDetailsOpen);
                    }

                    if (seasonCompactIndicatorText != null)
                    {
                        seasonCompactIndicatorText.text =
                            seasonDetailsOpen
                                ? "▲"
                                : "▼";
                    }
                });

            seasonCompactText =
                CreateText(
                    compact,
                    "Season Compact Text",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(
                        14f,
                        0f),
                    new Vector2(
                        204f,
                        26f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Color(
                        0.88f,
                        0.94f,
                        1f,
                        1f));

            seasonCompactIndicatorText =
                CreateText(
                    compact,
                    "Season Compact Indicator",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        -15f,
                        0f),
                    new Vector2(
                        20f,
                        24f),
                    new Vector2(
                        1f,
                        0.5f),
                    new Vector2(
                        1f,
                        0.5f),
                    new Color(
                        0.42f,
                        0.78f,
                        1f,
                        1f));

            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Season Panel",
                    new Vector2(
                        22f,
                        -68f),
                    new Vector2(
                        418f,
                        172f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    new Color(
                        0.020f,
                        0.029f,
                        0.044f,
                        0.94f));

            seasonPanel =
                panel.gameObject;

            StyleSeasonSurface(
                panel,
                false);

            seasonNameText =
                CreateText(
                    panel,
                    "Season Name",
                    11,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(16f, -14f),
                    new Vector2(242f, 18f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.38f,
                        0.78f,
                        1f,
                        1f));

            seasonMissionText =
                CreateText(
                    panel,
                    "Season Mission",
                    11,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    new Vector2(-16f, -14f),
                    new Vector2(128f, 18f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Color(
                        0.60f,
                        0.67f,
                        0.76f,
                        1f));

            seasonTitleText =
                CreateText(
                    panel,
                    "Season Title",
                    18,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(16f, -39f),
                    new Vector2(386f, 28f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.95f,
                        0.97f,
                        1f,
                        1f));

            seasonProgressText =
                CreateText(
                    panel,
                    "Season Progress",
                    11,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(16f, -76f),
                    new Vector2(214f, 18f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.66f,
                        0.72f,
                        0.80f,
                        1f));

            RectTransform track =
                CreatePanel(
                    panel,
                    "Season Progress Track",
                    new Vector2(16f, -101f),
                    new Vector2(386f, 6f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.10f,
                        0.14f,
                        0.20f,
                        0.96f));

            RectTransform fill =
                CreatePanel(
                    track,
                    "Season Progress Fill",
                    Vector2.zero,
                    new Vector2(0f, 6f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Color(
                        0.22f,
                        0.66f,
                        0.96f,
                        1f));

            seasonProgressFill =
                fill.GetComponent<Image>();

            seasonRewardText =
                CreateText(
                    panel,
                    "Season Reward",
                    11,
                    FontStyle.Bold,
                    TextAnchor.LowerLeft,
                    new Vector2(16f, 18f),
                    new Vector2(286f, 20f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Color(
                        0.94f,
                        0.76f,
                        0.28f,
                        1f));

            seasonDaysText =
                CreateText(
                    panel,
                    "Season Days",
                    11,
                    FontStyle.Bold,
                    TextAnchor.LowerRight,
                    new Vector2(-16f, 18f),
                    new Vector2(112f, 20f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    new Color(
                        0.58f,
                        0.65f,
                        0.73f,
                        1f));

            seasonDetailsOpen =
                false;

            seasonCompactButton.SetActive(
                false);

            seasonPanel.SetActive(
                false);
        }

        private void StyleSeasonSurface(
            RectTransform surface,
            bool compact)
        {
            if (surface == null)
                return;

            Image image =
                surface.GetComponent<Image>();

            if (image != null)
            {
                image.sprite =
                    null;

                image.type =
                    Image.Type.Simple;
            }

            Shadow shadow =
                surface.gameObject
                    .AddComponent<Shadow>();

            shadow.effectColor =
                new Color(
                    0f,
                    0f,
                    0f,
                    compact
                        ? 0.38f
                        : 0.48f);

            shadow.effectDistance =
                new Vector2(
                    0f,
                    -3f);

            shadow.useGraphicAlpha =
                true;

            Outline outline =
                surface.gameObject
                    .AddComponent<Outline>();

            outline.effectColor =
                new Color(
                    0.52f,
                    0.66f,
                    0.82f,
                    compact
                        ? 0.34f
                        : 0.27f);

            outline.effectDistance =
                new Vector2(
                    1f,
                    -1f);

            outline.useGraphicAlpha =
                true;

            RectTransform accent =
                CreatePanel(
                    surface,
                    "Season Accent",
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        compact
                            ? 238f
                            : 418f,
                        2f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Color(
                        0.25f,
                        0.70f,
                        1f,
                        compact
                            ? 0.86f
                            : 0.74f));

            Image accentImage =
                accent.GetComponent<Image>();

            if (accentImage != null)
            {
                accentImage.raycastTarget =
                    false;
            }

            if (!compact)
            {
                RectTransform inner =
                    CreatePanel(
                        surface,
                        "Season Inner Shade",
                        new Vector2(
                            0f,
                            -2f),
                        new Vector2(
                            390f,
                            1f),
                        new Vector2(
                            0.5f,
                            1f),
                        new Vector2(
                            0.5f,
                            1f),
                        new Color(
                            1f,
                            1f,
                            1f,
                            0.045f));

                Image innerImage =
                    inner.GetComponent<Image>();

                if (innerImage != null)
                {
                    innerImage.raycastTarget =
                        false;
                }
            }
        }

        private void UpdateSeasonPanel()
        {
            if (seasonPanel == null ||
                seasonCompactButton == null)
            {
                return;
            }

            bool available =
                season != null &&
                season.IsSeasonOneActive &&
                !season.IsComplete &&
                (onboarding == null ||
                 onboarding.IsComplete) &&
                (story == null ||
                 story.IsComplete);

            seasonCompactButton.SetActive(
                available);

            if (!available)
            {
                seasonDetailsOpen =
                    false;

                seasonPanel.SetActive(
                    false);

                return;
            }

            seasonPanel.SetActive(
                seasonDetailsOpen);

            if (seasonCompactText != null)
            {
                seasonCompactText.text =
                    MotorCityLocalization.Format(
                        "season1.ui_compact",
                        season.CurrentMissionNumber,
                        season.MissionCount);
            }

            if (seasonCompactIndicatorText != null)
            {
                seasonCompactIndicatorText.text =
                    seasonDetailsOpen
                        ? "▲"
                        : "▼";
            }

            seasonNameText.text =
                MotorCityLocalization.Text(
                    "season1.ui_name");

            seasonMissionText.text =
                MotorCityLocalization.Format(
                    "season1.ui_mission",
                    season.CurrentMissionNumber,
                    season.MissionCount);

            seasonTitleText.text =
                season.CurrentMissionTitle;

            int target =
                Mathf.Max(
                    1,
                    season.CurrentMissionTarget);

            int progress =
                Mathf.Clamp(
                    season.CurrentMissionProgress,
                    0,
                    target);

            seasonProgressText.text =
                MotorCityLocalization.Format(
                    "season1.ui_progress",
                    progress,
                    target);

            if (seasonProgressFill != null)
            {
                seasonProgressFill.rectTransform.sizeDelta =
                    new Vector2(
                        386f *
                        Mathf.Clamp01(
                            progress /
                            (float)target),
                        8f);
            }

            seasonRewardText.text =
                MotorCityLocalization.Format(
                    "season1.ui_reward",
                    season.CurrentCreditsReward,
                    season.CurrentReputationReward,
                    season.CurrentTurboXpReward);

            seasonDaysText.text =
                MotorCityLocalization.Format(
                    "season1.ui_days",
                    season.DaysRemaining);
        }

        private string ResolveObjectiveLine()
        {
            if (adventureDirector == null)
                return string.Empty;

            return
                adventureDirector.ObjectiveLine;
        }

    }
}
