using MotorCity.Localization;
using MotorCity.Input;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private void BuildGarage(Transform canvas)
        {
            garageOverlay =
                new GameObject(
                    "Garage Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            garageOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                garageOverlay.GetComponent<RectTransform>();
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            Image backdrop =
                garageOverlay.GetComponent<Image>();
            backdrop.color =
                new Color(0.012f, 0.010f, 0.022f, 0.08f);
            backdrop.raycastTarget = false;

            RectTransform panel =
                CreatePanel(
                    garageOverlay.transform,
                    "Garage Panel",
                    Vector2.zero,
                    new Vector2(1880f, 1000f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Color.clear);

            Image panelImage =
                panel.GetComponent<Image>();

            if (panelImage != null)
            {
                panelImage.color =
                    Color.clear;

                panelImage.raycastTarget =
                    false;
            }

            RectTransform leftCard =
                CreatePanel(
                    panel,
                    "Garage Vehicle Card",
                    new Vector2(
                        -28f,
                        -178f),
                    new Vector2(
                        390f,
                        650f),
                    new Vector2(
                        1f,
                        1f),
                    new Vector2(
                        1f,
                        1f),
                    Color.clear);

            ApplyGarageRowTexture(
                leftCard);

            RectTransform upgradesCard =
                CreatePanel(
                    panel,
                    "Garage Balance Bar",
                    new Vector2(
                        0f,
                        -26f),
                    new Vector2(
                        980f,
                        92f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    Color.clear);

            ApplyGarageRowTexture(
                upgradesCard);

            Text vehicleCardTitle =
                CreateText(
                    panel,
                    "Garage Vehicle Card Title",
                    15,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(
                        -392f,
                        -198f),
                    new Vector2(
                        320f,
                        24f),
                    new Vector2(
                        1f,
                        1f),
                    new Vector2(
                        1f,
                        1f),
                    SecondaryTextColor);

            vehicleCardTitle.text =
                MotorCityLocalization.Text(
                    "garage.my_car");

            Text upgradesTitle =
                CreateText(
                    panel,
                    "Garage Upgrades Title",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(
                        32f,
                        190f),
                    new Vector2(
                        820f,
                        28f),
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        0f,
                        0f),
                    TextColor);

            upgradesTitle.text =
                MotorCityLocalization.Text(
                    "garage.upgrades_title");

            CreateHudIcon(
                    panel,
                    "Garage Header Icon",
                    MotorCityIconLibrary.Garage,
                    new Vector2(
                        -3000f,
                        -3000f),
                    new Vector2(
                        26f,
                        26f),
                    new Vector2(
                        0f,
                        1f),
                    GarageAccent);

            Text title =
                CreateText(
                    panel,
                    "Garage Title",
                    27,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-3000f, -3000f),
                    new Vector2(270f, 36f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);
            title.text =
                MotorCityLocalization.Text(
                    "hud.garage_title");

            GameObject creditsGroupObject =
                new(
                    "Garage Credits Group",
                    typeof(RectTransform));

            creditsGroupObject.transform.SetParent(
                panel,
                false);

            RectTransform creditsGroup =
                creditsGroupObject.GetComponent<RectTransform>();

            creditsGroup.anchorMin =
                new Vector2(0.5f, 1f);
            creditsGroup.anchorMax =
                new Vector2(0.5f, 1f);
            creditsGroup.pivot =
                new Vector2(0.5f, 1f);
            creditsGroup.anchoredPosition =
                new Vector2(
                    -330f,
                    -46f);
            creditsGroup.sizeDelta =
                new Vector2(
                    190f,
                    44f);

            CreateHudIcon(
                    creditsGroup,
                    "Garage Credits Icon",
                    MotorCityIconLibrary.Credits,
                    Vector2.zero,
                    new Vector2(
                        21f,
                        21f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Color(
                        1f,
                        0.78f,
                        0.20f,
                        1f));

            garageMoneyText =
                CreateText(
                    creditsGroup,
                    "Garage Credits",
                    24,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(
                        28f,
                        0f),
                    new Vector2(
                        94f,
                        32f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Vector2(
                        0f,
                        0.5f),
                    TextColor);

            GameObject reputationGroupObject =
                new(
                    "Garage Reputation Group",
                    typeof(RectTransform));

            reputationGroupObject.transform.SetParent(
                panel,
                false);

            RectTransform reputationGroup =
                reputationGroupObject.GetComponent<RectTransform>();

            reputationGroup.anchorMin =
                new Vector2(0.5f, 1f);
            reputationGroup.anchorMax =
                new Vector2(0.5f, 1f);
            reputationGroup.pivot =
                new Vector2(0.5f, 1f);
            reputationGroup.anchoredPosition =
                new Vector2(
                    -90f,
                    -46f);
            reputationGroup.sizeDelta =
                new Vector2(
                    180f,
                    44f);

            CreateHudIcon(
                    reputationGroup,
                    "Garage Reputation Icon",
                    MotorCityIconLibrary.Reputation,
                    Vector2.zero,
                    new Vector2(
                        19f,
                        19f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Color(
                        0.72f,
                        0.52f,
                        1f,
                        1f));

            garageReputationText =
                CreateText(
                    reputationGroup,
                    "Garage Reputation",
                    21,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(
                        26f,
                        0f),
                    new Vector2(
                        68f,
                        32f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Vector2(
                        0f,
                        0.5f),
                    TextColor);

            GameObject levelGroupObject =
                new(
                    "Garage Level Group",
                    typeof(RectTransform));

            levelGroupObject.transform.SetParent(
                panel,
                false);

            RectTransform levelGroup =
                levelGroupObject.GetComponent<RectTransform>();

            levelGroup.anchorMin =
                new Vector2(0.5f, 1f);
            levelGroup.anchorMax =
                new Vector2(0.5f, 1f);
            levelGroup.pivot =
                new Vector2(0.5f, 1f);
            levelGroup.anchoredPosition =
                new Vector2(
                    140f,
                    -46f);
            levelGroup.sizeDelta =
                new Vector2(
                    150f,
                    44f);

            garageLevelText =
                CreateText(
                    levelGroup,
                    "Garage Level",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(
                        78f,
                        32f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    SecondaryTextColor);

            garageHeaderMasteryText =
                CreateText(
                    panel,
                    "Garage Header Mastery",
                    15,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    new Vector2(
                        450f,
                        -50f),
                    new Vector2(
                        250f,
                        24f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Color(
                        0.42f,
                        0.82f,
                        1f,
                        1f));

            garageVehicleStateIcon =
                CreateHudIcon(
                    panel,
                    "Garage Vehicle State Icon",
                    MotorCityIconLibrary.Get(
                        "car"),
                    new Vector2(
                        -390f,
                        -236f),
                    new Vector2(
                        22f,
                        22f),
                    new Vector2(
                        1f,
                        1f),
                    GarageAccent);

            garageVehicleText =
                CreateText(
                    panel,
                    "Garage Vehicle",
                    23,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-350f, -232f),
                    new Vector2(300f, 36f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    TextColor);

            garageNextVehicleText =
                CreateText(
                    panel,
                    "Garage Next Vehicle",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-392f, -282f),
                    new Vector2(330f, 54f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    GarageAccent);

            garageVehicleStatsText =
                CreateText(
                    panel,
                    "Garage Vehicle Stats",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-392f, -352f),
                    new Vector2(330f, 210f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    TextColor);

            garageVehicleStatsText.resizeTextForBestFit =
                true;
            garageVehicleStatsText.resizeTextMinSize =
                9;
            garageVehicleStatsText.resizeTextMaxSize =
                13;
            garageVehicleStatsText.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            garageVehicleStatsText.verticalOverflow =
                VerticalWrapMode.Truncate;

            garageVehicleCharacterText =
                CreateText(
                    panel,
                    "Garage Vehicle Character",
                    12,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(-392f, -580f),
                    new Vector2(330f, 118f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    SecondaryTextColor);

            garageVehicleCharacterText.resizeTextForBestFit =
                true;
            garageVehicleCharacterText.resizeTextMinSize =
                10;
            garageVehicleCharacterText.resizeTextMaxSize =
                12;
            garageVehicleCharacterText.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            garageVehicleCharacterText.verticalOverflow =
                VerticalWrapMode.Truncate;

            CreateHudIcon(
                    panel,
                    "Garage Mastery Icon",
                    MotorCityIconLibrary.Achievement,
                    new Vector2(
                        -390f,
                        -738f),
                    new Vector2(
                        18f,
                        18f),
                    new Vector2(
                        1f,
                        1f),
                    new Color(
                        0.42f,
                        0.82f,
                        1f,
                        1f));

            RectTransform masteryTrackRect =
                CreatePanel(
                    panel,
                    "Garage Mastery Track",
                    new Vector2(
                        -360f,
                        -742f),
                    new Vector2(
                        300f,
                        8f),
                    new Vector2(
                        1f,
                        1f),
                    new Vector2(
                        1f,
                        1f),
                    new Color(
                        0.09f,
                        0.08f,
                        0.14f,
                        0.94f));

            RectTransform masteryFillRect =
                CreatePanel(
                    masteryTrackRect,
                    "Garage Mastery Fill",
                    Vector2.zero,
                    new Vector2(
                        0f,
                        7f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Color(
                        0.42f,
                        0.82f,
                        1f,
                        1f));

            garageMasteryFill =
                masteryFillRect.GetComponent<Image>();

            garageVehicleHistoryText =
                CreateText(
                    panel,
                    "Garage Vehicle History",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -124f),
                    new Vector2(704f, 20f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.82f,
                        0.72f,
                        1f,
                        1f));

            garageVehicleSpecializationText =
                CreateText(
                    panel,
                    "Garage Vehicle Specialization",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -146f),
                    new Vector2(704f, 20f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.52f,
                        1f,
                        0.68f,
                        1f));

            garageCollectionText =
                CreateText(
                    panel,
                    "Garage Collection",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -168f),
                    new Vector2(704f, 38f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.92f,
                        0.74f,
                        1f,
                        1f));

            garageVehicleHistoryText.gameObject.SetActive(false);
            garageVehicleSpecializationText.gameObject.SetActive(false);
            garageCollectionText.gameObject.SetActive(false);

            Color[] accents =
            {
                new(0.12f, 0.58f, 1f, 1f),
                new(0.12f, 0.9f, 0.58f, 1f),
                new(1f, 0.62f, 0.12f, 1f)
            };

            for (int i = 0; i < 3; i++)
            {
                float x =
                    30f + i * 296f;

                RectTransform row =
                    CreatePanel(
                        panel,
                        $"Upgrade {i + 1}",
                        new Vector2(x, 24f),
                        new Vector2(280f, 154f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        Color.clear);

                ApplyGarageRowTexture(
                    row);

                Image rowImage =
                    row.GetComponent<Image>();

                // CreatePanel/ApplyGarageRowTexture disable raycasts by
                // default because most panels are decorative. Upgrade cards
                // are interactive, so the full row image must participate in
                // UI raycasting for the parent Button to receive clicks.
                if (rowImage != null)
                {
                    rowImage.raycastTarget =
                        true;
                }

                Button rowButton =
                    row.gameObject.AddComponent<Button>();

                rowButton.targetGraphic =
                    rowImage;

                ColorBlock rowColors =
                    rowButton.colors;

                rowColors.normalColor =
                    Color.white;

                rowColors.highlightedColor =
                    new Color(
                        1.08f,
                        1.08f,
                        1.08f,
                        1f);

                rowColors.pressedColor =
                    new Color(
                        0.86f,
                        0.86f,
                        0.86f,
                        1f);

                rowColors.selectedColor =
                    rowColors.highlightedColor;

                rowColors.disabledColor =
                    new Color(
                        0.55f,
                        0.55f,
                        0.55f,
                        0.75f);

                rowColors.colorMultiplier =
                    1f;

                rowColors.fadeDuration =
                    0.08f;

                rowButton.colors =
                    rowColors;

                MotorCityInputAction upgradeAction =
                    i switch
                    {
                        0 => MotorCityInputAction.Upgrade1,
                        1 => MotorCityInputAction.Upgrade2,
                        _ => MotorCityInputAction.Upgrade3
                    };

                rowButton.onClick.AddListener(
                    () =>
                        MotorCityInput.PulseVirtual(
                            upgradeAction));

                Sprite upgradeSprite =
                    i switch
                    {
                        0 => MotorCityIconLibrary.Get(
                            "key"),
                        1 => MotorCityIconLibrary.Get(
                            "gear"),
                        _ => MotorCityIconLibrary.Get(
                            "target")
                    };

                CreateHudIcon(
                        row,
                        "Upgrade Icon",
                        upgradeSprite,
                        new Vector2(
                            24f,
                            -21f),
                        new Vector2(
                            22f,
                            22f),
                        new Vector2(
                            0f,
                            1f),
                        accents[i]);

                garageTitleTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Title",
                        18,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(56f, -18f),
                        new Vector2(150f, 28f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        TextColor);

                garagePriceIcons[i] =
                    CreateHudIcon(
                        row,
                        "Upgrade Price Icon",
                        MotorCityIconLibrary.Credits,
                        new Vector2(
                            -104f,
                            -10f),
                        new Vector2(
                            15f,
                            15f),
                        new Vector2(
                            1f,
                            1f),
                        accents[i]);

                garagePriceTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Price",
                        17,
                        FontStyle.Bold,
                        TextAnchor.UpperRight,
                        new Vector2(-16f, -20f),
                        new Vector2(118f, 26f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        accents[i]);

                garageDescriptionTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Description",
                        14,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(22f, 18f),
                        new Vector2(236f, 64f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        SecondaryTextColor);
            }

            garagePassportPanel =
                CreatePanel(
                    panel,
                    "Vehicle Passport",
                    new Vector2(0f, 0f),
                    new Vector2(860f, 430f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Color.clear).gameObject;

            RectTransform passportRect =
                garagePassportPanel
                    .GetComponent<RectTransform>();

            ApplyGaragePassportTexture(
                passportRect);

            CreateHudIcon(
                passportRect,
                "Passport Vehicle Icon",
                MotorCityIconLibrary.Get(
                    "car"),
                new Vector2(
                    22f,
                    -22f),
                new Vector2(
                    22f,
                    22f),
                new Vector2(
                    0f,
                    1f),
                GarageAccent);

            garagePassportTitleText =
                CreateText(
                    passportRect,
                    "Passport Title",
                    23,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(54f, -15f),
                    new Vector2(618f, 32f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);

            garagePassportSummaryText =
                CreateText(
                    passportRect,
                    "Passport Summary",
                    15,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(24f, -58f),
                    new Vector2(656f, 54f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(0.82f, 0.72f, 1f, 1f));

            garagePassportDisciplinesText =
                CreateText(
                    passportRect,
                    "Passport Disciplines",
                    14,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(24f, -120f),
                    new Vector2(656f, 44f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);

            garagePassportMasteryText =
                CreateText(
                    passportRect,
                    "Passport Mastery",
                    14,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(24f, -176f),
                    new Vector2(656f, 24f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(0.52f, 1f, 0.68f, 1f));

            garagePassportSpecializationText =
                CreateText(
                    passportRect,
                    "Passport Specialization",
                    14,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(24f, -214f),
                    new Vector2(656f, 30f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    GarageAccent);

            garagePassportPanel.SetActive(
                false);

            garageStatusText =
                CreateText(
                    panel,
                    "Garage Status",
                    10,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(0f, 196f),
                    new Vector2(720f, 42f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    SecondaryTextColor);

            garageStatusText.resizeTextForBestFit =
                false;
            garageStatusText.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            garageStatusText.verticalOverflow =
                VerticalWrapMode.Truncate;

            garageControlsText =
                CreateText(
                    panel,
                    "Garage Controls",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-28f, 20f),
                    new Vector2(468f, 22f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    SecondaryTextColor);

            garageControlsText.text =
                string.Empty;

            garageControlsText.gameObject.SetActive(
                false);

            BuildGarageTouchControls(
                panel);
        }

        private void BuildGarageTouchControls(
            RectTransform panel)
        {
            garageTouchControlsRoot =
                new GameObject(
                    "Garage Action Controls",
                    typeof(RectTransform));

            garageTouchControlsRoot.transform.SetParent(
                panel,
                false);

            RectTransform root =
                garageTouchControlsRoot.GetComponent<RectTransform>();

            root.anchorMin =
                Vector2.zero;
            root.anchorMax =
                Vector2.one;
            root.pivot =
                new Vector2(
                    0.5f,
                    0.5f);
            root.offsetMin =
                Vector2.zero;
            root.offsetMax =
                Vector2.zero;

            garageActionButtons[0] =
                CreateGarageActionButton(
                    root,
                    "Garage Previous Vehicle",
                    "‹",
                    MotorCityInputAction.PreviousVehicle,
                    new Vector2(
                        0f,
                        0.5f),
                    new Vector2(
                        66f,
                        0f),
                    new Vector2(
                        92f,
                        126f),
                    54);

            garageActionButtons[1] =
                CreateGarageActionButton(
                    root,
                    "Garage Next Vehicle",
                    "›",
                    MotorCityInputAction.NextVehicle,
                    new Vector2(
                        1f,
                        0.5f),
                    new Vector2(
                        -446f,
                        0f),
                    new Vector2(
                        92f,
                        126f),
                    54);

            garageActionButtons[2] =
                CreateGarageActionButton(
                    root,
                    "Garage Appearance Color",
                    MotorCityLocalization.Text(
                        "touch.garage.color"),
                    MotorCityInputAction.CycleBodyColor,
                    new Vector2(
                        0.5f,
                        0f),
                    new Vector2(
                        -132f,
                        68f),
                    new Vector2(
                        122f,
                        78f),
                    15);

            garageActionButtons[3] =
                CreateGarageActionButton(
                    root,
                    "Garage Appearance Wheels",
                    MotorCityLocalization.Text(
                        "touch.garage.wheels"),
                    MotorCityInputAction.CycleWheels,
                    new Vector2(
                        0.5f,
                        0f),
                    new Vector2(
                        0f,
                        68f),
                    new Vector2(
                        122f,
                        78f),
                    15);

            garageActionButtons[4] =
                CreateGarageActionButton(
                    root,
                    "Garage Appearance Neon",
                    MotorCityLocalization.Text(
                        "touch.garage.neon"),
                    MotorCityInputAction.CycleNeon,
                    new Vector2(
                        0.5f,
                        0f),
                    new Vector2(
                        132f,
                        68f),
                    new Vector2(
                        122f,
                        78f),
                    15);

            garageActionButtons[5] =
                CreateGarageActionButton(
                    root,
                    "Garage Passport",
                    MotorCityLocalization.Text(
                        "touch.garage.passport"),
                    MotorCityInputAction.ToggleVehiclePassport,
                    new Vector2(
                        1f,
                        0f),
                    new Vector2(
                        -228f,
                        42f),
                    new Vector2(
                        156f,
                        42f),
                    12);

            garageActionButtons[6] =
                CreateGarageActionButton(
                    root,
                    "Garage City Button",
                    MotorCityLocalization.Text(
                        "garage.city_button"),
                    MotorCityInputAction.Interact,
                    new Vector2(
                        1f,
                        1f),
                    new Vector2(
                        -24f,
                        -38f),
                    new Vector2(
                        286f,
                        76f),
                    24);

            RectTransform appearancePanel =
                CreatePanel(
                    root,
                    "Garage Appearance Panel",
                    new Vector2(
                        0f,
                        18f),
                    new Vector2(
                        430f,
                        142f),
                    new Vector2(
                        0.5f,
                        0f),
                    new Vector2(
                        0.5f,
                        0f),
                    Color.clear);

            ApplyGarageRowTexture(
                appearancePanel);

            appearancePanel.SetAsFirstSibling();

            Text appearanceTitle =
                CreateText(
                    root,
                    "Garage Appearance Title",
                    15,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        146f),
                    new Vector2(
                        360f,
                        26f),
                    new Vector2(
                        0.5f,
                        0f),
                    new Vector2(
                        0.5f,
                        0f),
                    SecondaryTextColor);

            appearanceTitle.text =
                MotorCityLocalization.Text(
                    "garage.appearance_title");

            RectTransform menuVisual =
                CreatePanel(
                    root,
                    "Garage Main Menu Visual",
                    new Vector2(
                        24f,
                        -38f),
                    new Vector2(
                        286f,
                        76f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    Color.clear);

            ApplyGarageRowTexture(
                menuVisual);

            Text menuText =
                CreateText(
                    menuVisual,
                    "Label",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(
                        250f,
                        54f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    TextColor);

            menuText.text =
                "‹  " +
                MotorCityLocalization.Text(
                    "garage.main_menu_button");

            garageTouchControlsRoot.SetActive(
                false);
        }

        private GameObject CreateGarageActionButton(
            Transform parent,
            string name,
            string label,
            MotorCityInputAction action,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize)
        {
            GameObject buttonObject =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            buttonObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin =
                anchor;
            rect.anchorMax =
                anchor;
            rect.pivot =
                anchor;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                buttonObject.GetComponent<Image>();

            Texture2D buttonTexture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.modalButton;

            Sprite buttonSprite =
                GetModalButtonSprite(
                    buttonTexture);

            if (buttonSprite != null)
            {
                image.sprite =
                    buttonSprite;
                image.type =
                    Image.Type.Simple;
                image.preserveAspect =
                    false;
                image.color =
                    Color.white;
            }
            else
            {
                image.color =
                    PanelColor;
            }

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            button.onClick.AddListener(
                () =>
                    MotorCityInput.PulseVirtual(
                        action));

            Text text =
                CreateText(
                    rect,
                    "Label",
                    fontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    size -
                    new Vector2(
                        16f,
                        12f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    TextColor);

            text.text =
                label;

            MakeButtonTextCrisp(
                text);

            return buttonObject;
        }

        private void UpdateGarage()
        {
            bool rookieColorStep =
                onboarding != null &&
                !onboarding.IsComplete &&
                onboarding.CurrentStep == 6;

            for (int i = 0;
                 i < garageActionButtons.Length;
                 i++)
            {
                if (garageActionButtons[i] == null)
                    continue;

                bool visible =
                    !rookieColorStep ||
                    i == 2 ||
                    i == 6;

                garageActionButtons[i].SetActive(
                    visible);
            }

            bool metaUnlocked =
                activityManager != null &&
                activityManager.SecondaryProgressionAllowed;

            if (!metaUnlocked)
            {
                garagePassportOpen =
                    false;
            }
            else if (MotorCityInput.WasVirtualPressed(
                         MotorCityInputAction.ToggleVehiclePassport))
            {
                garagePassportOpen =
                    !garagePassportOpen;
            }

            if (garagePassportPanel != null)
            {
                garagePassportPanel.SetActive(
                    garagePassportOpen);
            }

            if (garagePassportOpen)
            {
                UpdateVehiclePassport();
            }

            garageMoneyText.text =
                garage.Credits.ToString(
                    "N0");

            if (garageReputationText != null)
            {
                garageReputationText.text =
                    (activityManager != null
                        ? activityManager.TotalReputation
                        : 0).ToString(
                            "N0");
            }

            if (garageLevelText != null)
            {
                garageLevelText.text =
                    MotorCityLocalization.Text(
                        "common.level") +
                    " " +
                    (activityManager != null
                        ? activityManager.ReputationLevel
                        : 1);
            }

            if (garageHeaderMasteryText != null)
            {
                garageHeaderMasteryText.text =
                    metaUnlocked &&
                    garage != null
                        ? garage.VehicleMasteryShort
                        : string.Empty;
            }

            if (garageVehicleText != null)
            {
                garageVehicleText.text =
                    garage.VehicleLine;
            }

            if (garageNextVehicleText != null)
            {
                garageNextVehicleText.text =
                    garage.NextVehicleLine;

                garageNextVehicleText.color =
                    !garage.HasNextVehicle
                        ? SecondaryTextColor
                        : garage.NextVehicleUnlocked
                            ? new Color(
                                0.35f,
                                1f,
                                0.58f,
                                1f)
                            : new Color(
                                1f,
                                0.58f,
                                0.24f,
                                1f);
            }

            if (garageVehicleStateIcon != null)
            {
                Sprite stateSprite =
                    MotorCityIconLibrary.Unlocked;

                Color stateColor =
                    new Color(
                        0.35f,
                        1f,
                        0.58f,
                        1f);

                if (!garage.SelectedVehicleUnlocked)
                {
                    stateSprite =
                        MotorCityIconLibrary.Locked;

                    stateColor =
                        new Color(
                            1f,
                            0.52f,
                            0.24f,
                            1f);
                }
                garageVehicleStateIcon.sprite =
                    stateSprite;

                garageVehicleStateIcon.enabled =
                    stateSprite != null;

                garageVehicleStateIcon.color =
                    stateColor;
            }

            if (garageVehicleStatsText != null)
            {
                garageVehicleStatsText.text =
                    garage.VehicleStatsLine;
            }

            if (garageVehicleCharacterText != null)
            {
                string specializationLine =
                    metaUnlocked &&
                    vehicleSpecialization != null
                        ? vehicleSpecialization.GarageCompactLine
                        : string.Empty;

                garageVehicleCharacterText.text =
                    string.IsNullOrWhiteSpace(
                        specializationLine)
                        ? garage.VehicleCharacterLine
                        : garage.VehicleCharacterLine +
                          "\n" +
                          specializationLine;
            }

            if (garageMasteryFill != null)
            {
                RectTransform fillRect =
                    garageMasteryFill.rectTransform;

                fillRect.sizeDelta =
                    new Vector2(
                        metaUnlocked
                            ? 218f *
                              Mathf.Clamp01(
                                  garage.VehicleMasteryProgress)
                            : 0f,
                        7f);
            }

            if (garageVehicleHistoryText != null)
            {
                garageVehicleHistoryText.text =
                    !metaUnlocked ||
                    vehicleHistory == null
                        ? string.Empty
                        : vehicleHistory.GarageLine;
            }

            if (garageVehicleSpecializationText != null)
            {
                garageVehicleSpecializationText.text =
                    !metaUnlocked ||
                    vehicleSpecialization == null
                        ? string.Empty
                        : vehicleSpecialization.GarageLine;
            }

            if (garageCollectionText != null)
            {
                if (!metaUnlocked)
                {
                    garageCollectionText.text =
                        string.Empty;
                }
                else
                {
                    string collectionLine =
                        collection == null
                            ? string.Empty
                            : collection.GarageLine;

                    string albumLine =
                        photoHunt == null
                            ? string.Empty
                            : photoHunt.AlbumLine;

                    garageCollectionText.text =
                        string.IsNullOrWhiteSpace(
                            albumLine)
                            ? collectionLine
                            : collectionLine +
                              "\n" +
                              albumLine;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                garageTitleTexts[i].text =
                    garage.GetUpgradeTitle(i);

                garagePriceTexts[i].text =
                    garage.GetUpgradePrice(i);

                bool maxed =
                    garage.IsUpgradeMaxed(
                        i);

                bool affordable =
                    garage.CanAffordUpgrade(
                        i);

                Color priceColor =
                    maxed
                        ? SecondaryTextColor
                        : affordable
                            ? new Color(
                                0.35f,
                                1f,
                                0.58f,
                                1f)
                            : new Color(
                                1f,
                                0.45f,
                                0.28f,
                                1f);

                garagePriceTexts[i].color =
                    priceColor;

                if (garagePriceIcons[i] != null)
                {
                    garagePriceIcons[i].enabled =
                        !maxed &&
                        garagePriceIcons[i].sprite != null;

                    garagePriceIcons[i].color =
                        priceColor;
                }

                garageDescriptionTexts[i].text =
                    garage.GetUpgradeDescription(i);
            }

            garageStatusText.text =
                rookieColorStep
                    ? onboarding.ObjectiveLine
                    : string.IsNullOrWhiteSpace(
                        garage.StatusText)
                        ? garage.CustomizationHintLine
                        : garage.StatusText;
        }

        private void UpdateVehiclePassport()
        {
            if (garagePassportTitleText == null)
                return;

            garagePassportTitleText.text =
                MotorCityLocalization.Format(
                    "history.passport_title",
                    garage != null
                        ? garage.VehicleLine
                        : string.Empty);

            garagePassportSummaryText.text =
                vehicleHistory == null
                    ? string.Empty
                    : vehicleHistory.PassportSummary;

            garagePassportDisciplinesText.text =
                vehicleHistory == null
                    ? string.Empty
                    : vehicleHistory.PassportDisciplines;

            garagePassportMasteryText.text =
                garage == null
                    ? string.Empty
                    : garage.VehicleMasteryLine;

            garagePassportSpecializationText.text =
                vehicleSpecialization == null
                    ? string.Empty
                    : vehicleSpecialization.GarageLine;
        }

        private void ApplyGaragePassportTexture(
            RectTransform panel)
        {
            if (panel == null)
                return;

            ClearPanelChrome(
                panel);

            Texture2D texture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.passportPanel;

            if (texture == null)
                return;

            GameObject backgroundObject =
                new(
                    "Ville Passport Background",
                    typeof(RectTransform),
                    typeof(RawImage));

            backgroundObject.transform.SetParent(
                panel,
                false);

            backgroundObject.transform.SetAsFirstSibling();

            RectTransform rect =
                backgroundObject.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;
            rect.anchorMax =
                Vector2.one;
            rect.offsetMin =
                Vector2.zero;
            rect.offsetMax =
                Vector2.zero;

            RawImage image =
                backgroundObject.GetComponent<RawImage>();

            image.texture =
                texture;
            image.color =
                Color.white;
            image.raycastTarget =
                false;

            CreateAccent(
                panel,
                new Color(
                    0.52f,
                    1f,
                    0.68f,
                    0.85f),
                new Vector2(
                    24f,
                    -48f),
                new Vector2(
                    656f,
                    2f),
                new Vector2(
                    0f,
                    1f),
                new Vector2(
                    0f,
                    1f));
        }

        private void ApplyGarageRowTexture(
            RectTransform row)
        {
            if (row == null)
                return;

            ClearPanelChrome(
                row);

            Texture2D texture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.characterPanel;

            if (texture == null)
                return;

            GameObject backgroundObject =
                new(
                    "Ville Garage Row Background",
                    typeof(RectTransform),
                    typeof(RawImage));

            backgroundObject.transform.SetParent(
                row,
                false);

            backgroundObject.transform.SetAsFirstSibling();

            RectTransform rect =
                backgroundObject.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;
            rect.anchorMax =
                Vector2.one;
            rect.offsetMin =
                Vector2.zero;
            rect.offsetMax =
                Vector2.zero;

            RawImage image =
                backgroundObject.GetComponent<RawImage>();

            image.texture =
                texture;
            image.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.96f);
            image.raycastTarget =
                false;
        }

    }
}
