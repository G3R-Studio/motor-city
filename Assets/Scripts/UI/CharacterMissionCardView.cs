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
                    onboarding.CurrentStep >= 3;

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
            else if (season != null &&
                     season.IsSeasonOneActive &&
                     !season.IsComplete)
            {
                name =
                    season.CurrentCharacterName;

                line =
                    season.CurrentCharacterLine;

                int seasonStyle =
                    season.CurrentCharacterStyle;

                style =
                    seasonStyle switch
                    {
                        1 => 3,
                        2 => 1,
                        3 => 2,
                        _ => 0
                    };

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

            bool storySource =
                (onboarding != null &&
                 !onboarding.IsComplete) ||
                (story != null &&
                 !story.IsComplete);

            if (storySource)
            {
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
            }
            else
            {
                characterSourceText.text =
                    MotorCityLocalization.Text(
                        "hud.character.season");
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

        private string ResolveObjectiveLine()
        {
            if (adventureDirector == null)
                return string.Empty;

            return
                adventureDirector.ObjectiveLine;
        }

    }
}
