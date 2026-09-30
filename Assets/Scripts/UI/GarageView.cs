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
                new Color(0.01f, 0.01f, 0.025f, 0.04f);
            backdrop.raycastTarget = false;

            RectTransform panel =
                CreatePanel(
                    garageOverlay.transform,
                    "Garage Panel",
                    Vector2.zero,
                    new Vector2(1920f, 1080f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Color.clear);

            Image panelImage =
                panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = Color.clear;
                panelImage.raycastTarget = false;
            }

            // Fresh garage HUD: the car and the authored garage stay visually
            // dominant. UI only frames the scene instead of covering it.
            Color glass =
                new Color(0.025f, 0.035f, 0.10f, 0.88f);
            Color glassSoft =
                new Color(0.035f, 0.045f, 0.13f, 0.82f);
            Color cyan =
                new Color(0.22f, 0.82f, 1f, 1f);
            Color violet =
                new Color(0.72f, 0.34f, 1f, 1f);
            Color green =
                new Color(0.20f, 1f, 0.62f, 1f);

            RectTransform topBalance =
                CreatePanel(
                    panel,
                    "Garage Top Balance",
                    new Vector2(0f, -16f),
                    new Vector2(880f, 82f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    glass);

            CreateAccent(
                topBalance,
                violet,
                new Vector2(0f, 0f),
                new Vector2(836f, 3f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f));

            GameObject creditsGroupObject =
                new(
                    "Garage Credits Group",
                    typeof(RectTransform));
            creditsGroupObject.transform.SetParent(
                topBalance,
                false);
            RectTransform creditsGroup =
                creditsGroupObject.GetComponent<RectTransform>();
            creditsGroup.anchorMin = new Vector2(0f, 0.5f);
            creditsGroup.anchorMax = new Vector2(0f, 0.5f);
            creditsGroup.pivot = new Vector2(0f, 0.5f);
            creditsGroup.anchoredPosition = new Vector2(30f, 0f);
            creditsGroup.sizeDelta = new Vector2(195f, 62f);

            CreateHudIcon(
                creditsGroup,
                "Garage Credits Icon",
                MotorCityIconLibrary.Credits,
                new Vector2(0f, 0f),
                new Vector2(28f, 28f),
                new Vector2(0f, 0.5f),
                green);

            Text creditsLabel =
                CreateText(
                    creditsGroup,
                    "Garage Credits Label",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(40f, 13f),
                    new Vector2(145f, 20f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    SecondaryTextColor);
            creditsLabel.text =
                MotorCityLocalization.Text("garage.credits_label");

            garageMoneyText =
                CreateText(
                    creditsGroup,
                    "Garage Credits",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(40f, -11f),
                    new Vector2(145f, 28f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    TextColor);

            GameObject reputationGroupObject =
                new(
                    "Garage Reputation Group",
                    typeof(RectTransform));
            reputationGroupObject.transform.SetParent(
                topBalance,
                false);
            RectTransform reputationGroup =
                reputationGroupObject.GetComponent<RectTransform>();
            reputationGroup.anchorMin = new Vector2(0f, 0.5f);
            reputationGroup.anchorMax = new Vector2(0f, 0.5f);
            reputationGroup.pivot = new Vector2(0f, 0.5f);
            reputationGroup.anchoredPosition = new Vector2(250f, 0f);
            reputationGroup.sizeDelta = new Vector2(185f, 62f);

            CreateHudIcon(
                reputationGroup,
                "Garage Reputation Icon",
                MotorCityIconLibrary.Reputation,
                Vector2.zero,
                new Vector2(28f, 28f),
                new Vector2(0f, 0.5f),
                violet);

            Text reputationLabel =
                CreateText(
                    reputationGroup,
                    "Garage Reputation Label",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(40f, 13f),
                    new Vector2(140f, 20f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    SecondaryTextColor);
            reputationLabel.text =
                MotorCityLocalization.Text("garage.reputation_label");

            garageReputationText =
                CreateText(
                    reputationGroup,
                    "Garage Reputation",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(40f, -11f),
                    new Vector2(135f, 28f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    TextColor);

            GameObject levelGroupObject =
                new(
                    "Garage Level Group",
                    typeof(RectTransform));
            levelGroupObject.transform.SetParent(
                topBalance,
                false);
            RectTransform levelGroup =
                levelGroupObject.GetComponent<RectTransform>();
            levelGroup.anchorMin = new Vector2(0f, 0.5f);
            levelGroup.anchorMax = new Vector2(0f, 0.5f);
            levelGroup.pivot = new Vector2(0f, 0.5f);
            levelGroup.anchoredPosition = new Vector2(462f, 0f);
            levelGroup.sizeDelta = new Vector2(170f, 62f);

            CreateHudIcon(
                levelGroup,
                "Garage Level Icon",
                MotorCityIconLibrary.Achievement,
                Vector2.zero,
                new Vector2(28f, 28f),
                new Vector2(0f, 0.5f),
                cyan);

            garageLevelText =
                CreateText(
                    levelGroup,
                    "Garage Level",
                    19,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(40f, 0f),
                    new Vector2(126f, 30f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    TextColor);

            garageHeaderMasteryText =
                CreateText(
                    topBalance,
                    "Garage Header Mastery",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(675f, 0f),
                    new Vector2(190f, 42f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    cyan);

            RectTransform vehicleCard =
                CreatePanel(
                    panel,
                    "Garage Vehicle Card",
                    new Vector2(-26f, -118f),
                    new Vector2(344f, 610f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    glass);

            CreateAccent(
                vehicleCard,
                violet,
                new Vector2(0f, 0f),
                new Vector2(304f, 3f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f));

            Text vehicleCardTitle =
                CreateText(
                    vehicleCard,
                    "Garage Vehicle Card Title",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -28f),
                    new Vector2(238f, 24f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);
            vehicleCardTitle.text =
                MotorCityLocalization.Text("garage.my_car");

            garageVehicleStateIcon =
                CreateHudIcon(
                    vehicleCard,
                    "Garage Vehicle State Icon",
                    MotorCityIconLibrary.Get("car"),
                    new Vector2(28f, -72f),
                    new Vector2(34f, 34f),
                    new Vector2(0f, 1f),
                    green);

            garageVehicleText =
                CreateText(
                    vehicleCard,
                    "Garage Vehicle",
                    25,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(76f, -72f),
                    new Vector2(254f, 40f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);

            garageNextVehicleText =
                CreateText(
                    vehicleCard,
                    "Garage Next Vehicle",
                    13,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(28f, -118f),
                    new Vector2(290f, 58f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    GarageAccent);

            garageVehicleStatsText =
                CreateText(
                    vehicleCard,
                    "Garage Vehicle Stats",
                    14,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(28f, -194f),
                    new Vector2(290f, 236f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);
            garageVehicleStatsText.resizeTextForBestFit = true;
            garageVehicleStatsText.resizeTextMinSize = 11;
            garageVehicleStatsText.resizeTextMaxSize = 14;
            garageVehicleStatsText.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            garageVehicleStatsText.verticalOverflow =
                VerticalWrapMode.Truncate;
            garageVehicleStatsText.lineSpacing = 1.42f;

            garageVehicleCharacterText =
                CreateText(
                    vehicleCard,
                    "Garage Vehicle Character",
                    13,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    new Vector2(28f, -346f),
                    new Vector2(290f, 96f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);
            garageVehicleCharacterText.resizeTextForBestFit = true;
            garageVehicleCharacterText.resizeTextMinSize = 11;
            garageVehicleCharacterText.resizeTextMaxSize = 13;
            garageVehicleCharacterText.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            garageVehicleCharacterText.verticalOverflow =
                VerticalWrapMode.Truncate;

            Text masteryLabel =
                CreateText(
                    vehicleCard,
                    "Garage Mastery Label",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -456f),
                    new Vector2(180f, 24f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);
            masteryLabel.text =
                MotorCityLocalization.Text("garage.mastery_label");

            CreateHudIcon(
                vehicleCard,
                "Garage Mastery Icon",
                MotorCityIconLibrary.Achievement,
                new Vector2(28f, -504f),
                new Vector2(24f, 24f),
                new Vector2(0f, 1f),
                cyan);

            RectTransform masteryTrackRect =
                CreatePanel(
                    vehicleCard,
                    "Garage Mastery Track",
                    new Vector2(64f, -512f),
                    new Vector2(246f, 12f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(0.08f, 0.09f, 0.19f, 1f));

            RectTransform masteryFillRect =
                CreatePanel(
                    masteryTrackRect,
                    "Garage Mastery Fill",
                    Vector2.zero,
                    new Vector2(0f, 10f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    cyan);
            garageMasteryFill =
                masteryFillRect.GetComponent<Image>();

            Outline masteryFillOutline =
                masteryFillRect.GetComponent<Outline>();

            if (masteryFillOutline != null)
            {
                masteryFillOutline.enabled =
                    false;
            }

            garageVehicleHistoryText =
                CreateText(
                    panel,
                    "Garage Vehicle History",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-3000f, -3000f),
                    new Vector2(10f, 10f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.clear);

            garageVehicleSpecializationText =
                CreateText(
                    panel,
                    "Garage Vehicle Specialization",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-3000f, -3000f),
                    new Vector2(10f, 10f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.clear);

            garageCollectionText =
                CreateText(
                    panel,
                    "Garage Collection",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-3000f, -3000f),
                    new Vector2(10f, 10f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.clear);

            garageVehicleHistoryText.gameObject.SetActive(false);
            garageVehicleSpecializationText.gameObject.SetActive(false);
            garageCollectionText.gameObject.SetActive(false);

            Color[] accents =
            {
                violet,
                cyan,
                new Color(0.55f, 0.42f, 1f, 1f)
            };

            for (int i = 0; i < 3; i++)
            {
                float x =
                    40f + i * 252f;

                RectTransform row =
                    CreatePanel(
                        panel,
                        $"Upgrade {i + 1}",
                        new Vector2(x, 74f),
                        new Vector2(228f, 184f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        glassSoft);

                Image rowImage =
                    row.GetComponent<Image>();
                if (rowImage != null)
                    rowImage.raycastTarget = true;

                Button rowButton =
                    row.gameObject.AddComponent<Button>();
                rowButton.targetGraphic = rowImage;

                ColorBlock rowColors =
                    rowButton.colors;
                rowColors.normalColor = Color.white;
                rowColors.highlightedColor =
                    new Color(1.08f, 1.08f, 1.10f, 1f);
                rowColors.pressedColor =
                    new Color(0.82f, 0.84f, 0.95f, 1f);
                rowColors.colorMultiplier = 1f;
                rowColors.fadeDuration = 0.08f;
                rowButton.colors = rowColors;

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
                        0 => MotorCityIconLibrary.Get("key"),
                        1 => MotorCityIconLibrary.Get("gear"),
                        _ => MotorCityIconLibrary.Get("target")
                    };

                CreateHudIcon(
                    row,
                    "Upgrade Icon",
                    upgradeSprite,
                    new Vector2(20f, -22f),
                    new Vector2(30f, 30f),
                    new Vector2(0f, 1f),
                    accents[i]);

                garageTitleTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Title",
                        16,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(60f, -18f),
                        new Vector2(152f, 44f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        TextColor);

                garageDescriptionTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Description",
                        12,
                        FontStyle.Normal,
                        TextAnchor.UpperLeft,
                        new Vector2(20f, -78f),
                        new Vector2(188f, 56f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        SecondaryTextColor);
                garageDescriptionTexts[i].resizeTextForBestFit = true;
                garageDescriptionTexts[i].resizeTextMinSize = 10;
                garageDescriptionTexts[i].resizeTextMaxSize = 12;

                garagePriceIcons[i] =
                    CreateHudIcon(
                        row,
                        "Upgrade Price Icon",
                        MotorCityIconLibrary.Credits,
                        new Vector2(20f, 22f),
                        new Vector2(20f, 20f),
                        new Vector2(0f, 0f),
                        green);

                garagePriceTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Price",
                        17,
                        FontStyle.Bold,
                        TextAnchor.MiddleLeft,
                        new Vector2(50f, 26f),
                        new Vector2(158f, 28f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        green);
            }

            garagePassportPanel =
                CreatePanel(
                    panel,
                    "Vehicle Passport",
                    Vector2.zero,
                    new Vector2(860f, 430f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    glass).gameObject;

            RectTransform passportRect =
                garagePassportPanel.GetComponent<RectTransform>();

            garagePassportTitleText =
                CreateText(
                    passportRect,
                    "Passport Title",
                    23,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(28f, -22f),
                    new Vector2(804f, 34f),
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
                    new Vector2(28f, -72f),
                    new Vector2(804f, 58f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    cyan);

            garagePassportDisciplinesText =
                CreateText(
                    passportRect,
                    "Passport Disciplines",
                    14,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(28f, -146f),
                    new Vector2(804f, 56f),
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
                    new Vector2(28f, -222f),
                    new Vector2(804f, 34f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    green);

            garagePassportSpecializationText =
                CreateText(
                    passportRect,
                    "Passport Specialization",
                    14,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(28f, -274f),
                    new Vector2(828f, 74f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    GarageAccent);

            garagePassportPanel.SetActive(false);

            garageStatusText =
                CreateText(
                    panel,
                    "Garage Status",
                    10,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 238f),
                    new Vector2(620f, 30f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    SecondaryTextColor);
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
                    TextAnchor.MiddleCenter,
                    new Vector2(-3000f, -3000f),
                    new Vector2(10f, 10f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.clear);
            garageControlsText.gameObject.SetActive(false);

            BuildGarageTouchControls(panel);
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
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Color navColor =
                new Color(0.06f, 0.055f, 0.18f, 0.94f);

            garageActionButtons[0] =
                CreateGarageActionButton(
                    root,
                    "Garage Previous Vehicle",
                    "‹",
                    MotorCityInputAction.PreviousVehicle,
                    new Vector2(0f, 0.5f),
                    new Vector2(92f, 0f),
                    new Vector2(78f, 112f),
                    58,
                    navColor);

            garageActionButtons[1] =
                CreateGarageActionButton(
                    root,
                    "Garage Next Vehicle",
                    "›",
                    MotorCityInputAction.NextVehicle,
                    new Vector2(1f, 0.5f),
                    new Vector2(-430f, 0f),
                    new Vector2(86f, 124f),
                    58,
                    navColor);

            RectTransform appearancePanel =
                CreatePanel(
                    root,
                    "Garage Appearance Panel",
                    new Vector2(804f, 64f),
                    new Vector2(488f, 184f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Color(0.025f, 0.035f, 0.10f, 0.88f));
            appearancePanel.SetAsFirstSibling();

            Text appearanceTitle =
                CreateText(
                    appearancePanel,
                    "Garage Appearance Title",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -18f),
                    new Vector2(448f, 28f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);
            appearanceTitle.text =
                MotorCityLocalization.Text("garage.appearance_title");

            Vector2 appearanceButtonSize =
                new Vector2(144f, 110f);

            const int appearanceButtonFontSize =
                17;

            garageActionButtons[2] =
                CreateGarageActionButton(
                    appearancePanel,
                    "Garage Appearance Color",
                    MotorCityLocalization.Text("touch.garage.color"),
                    MotorCityInputAction.CycleBodyColor,
                    new Vector2(0f, 0f),
                    new Vector2(16f, 18f),
                    appearanceButtonSize,
                    appearanceButtonFontSize,
                    new Color(0.06f, 0.08f, 0.19f, 0.96f));

            garageActionButtons[3] =
                CreateGarageActionButton(
                    appearancePanel,
                    "Garage Appearance Wheels",
                    MotorCityLocalization.Text("touch.garage.wheels"),
                    MotorCityInputAction.CycleWheels,
                    new Vector2(0f, 0f),
                    new Vector2(170f, 18f),
                    appearanceButtonSize,
                    appearanceButtonFontSize,
                    new Color(0.06f, 0.08f, 0.19f, 0.96f));

            garageActionButtons[4] =
                CreateGarageActionButton(
                    appearancePanel,
                    "Garage Appearance Neon",
                    MotorCityLocalization.Text("touch.garage.neon"),
                    MotorCityInputAction.CycleNeon,
                    new Vector2(0f, 0f),
                    new Vector2(326f, 18f),
                    appearanceButtonSize,
                    appearanceButtonFontSize,
                    new Color(0.06f, 0.08f, 0.19f, 0.96f));

            garageActionButtons[5] =
                CreateGarageActionButton(
                    root,
                    "Garage Passport",
                    MotorCityLocalization.Text("touch.garage.passport"),
                    MotorCityInputAction.ToggleVehiclePassport,
                    new Vector2(1f, 0f),
                    new Vector2(-26f, 26f),
                    new Vector2(184f, 46f),
                    13,
                    new Color(0.06f, 0.08f, 0.19f, 0.96f));

            garageActionButtons[6] =
                CreateGarageActionButton(
                    root,
                    "Garage City Button",
                    MotorCityLocalization.Text("garage.city_button") + "  ›",
                    MotorCityInputAction.Interact,
                    new Vector2(1f, 1f),
                    new Vector2(-28f, -18f),
                    new Vector2(258f, 66f),
                    24,
                    new Color(0.16f, 0.06f, 0.32f, 0.96f));

            RectTransform menuVisual =
                CreatePanel(
                    root,
                    "Garage Main Menu Visual",
                    new Vector2(28f, -18f),
                    new Vector2(292f, 72f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(0.06f, 0.055f, 0.18f, 0.94f));

            Text menuText =
                CreateText(
                    menuVisual,
                    "Label",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(234f, 44f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);
            menuText.text =
                "‹  " +
                MotorCityLocalization.Text("garage.main_menu_button");

            garageTouchControlsRoot.SetActive(true);
        }

        private GameObject CreateGarageActionButton(
            Transform parent,
            string name,
            string label,
            MotorCityInputAction action,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            Color backgroundColor)
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
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image =
                buttonObject.GetComponent<Image>();
            image.color = backgroundColor;

            Button button =
                buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors =
                button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color(1.15f, 1.12f, 1.25f, 1f);
            colors.pressedColor =
                new Color(0.78f, 0.80f, 0.94f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

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
                    size - new Vector2(16f, 12f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);
            text.text = label;
            MakeButtonTextCrisp(text);

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
                string statsLine =
                    garage.VehicleStatsLine;

                if (!string.IsNullOrWhiteSpace(
                        statsLine))
                {
                    statsLine =
                        statsLine
                            .Replace(" • ", "\n")
                            .Replace("СКОР ", "СКОРОСТЬ  ")
                            .Replace("РАЗГ ", "РАЗГОН  ")
                            .Replace("СЦЕП ", "СЦЕПЛЕНИЕ  ")
                            .Replace("СТАБ ", "СТАБИЛЬНОСТЬ  ")
                            .Replace("РУЛЬ ", "РУЛЬ  ")
                            .Replace("ДРИФТ ", "ДРИФТ  ")
                            .Replace("МАССА ", "МАССА  ")
                            .Replace("SPEED ", "SPEED  ")
                            .Replace("ACCEL ", "ACCELERATION  ")
                            .Replace("GRIP ", "GRIP  ")
                            .Replace("STAB ", "STABILITY  ")
                            .Replace("STEER ", "STEERING  ")
                            .Replace("DRIFT ", "DRIFT  ")
                            .Replace("MASS ", "MASS  ");
                }

                garageVehicleStatsText.text =
                    statsLine ?? string.Empty;
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

                RectTransform trackRect =
                    fillRect.parent as RectTransform;

                float progress =
                    metaUnlocked && garage != null
                        ? Mathf.Clamp01(
                            garage.VehicleMasteryProgress)
                        : 0f;

                float trackWidth =
                    trackRect != null
                        ? trackRect.rect.width
                        : 0f;

                fillRect.anchorMin =
                    new Vector2(0f, 0.5f);
                fillRect.anchorMax =
                    new Vector2(0f, 0.5f);
                fillRect.pivot =
                    new Vector2(0f, 0.5f);
                fillRect.anchoredPosition =
                    Vector2.zero;

                fillRect.sizeDelta =
                    new Vector2(
                        trackWidth * progress,
                        fillRect.sizeDelta.y);
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
                        ? string.Empty
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
