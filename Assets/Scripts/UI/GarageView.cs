using MotorCity.Localization;
using MotorCity.Input;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private static void SetGarageTrackProgress(Image fill, float progress)
        {
            RectTransform rect = fill.rectTransform;
            rect.anchorMin = new Vector2(0f, .5f);
            rect.anchorMax = new Vector2(Mathf.Clamp01(progress), .5f);
            rect.pivot = new Vector2(0f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
        }

        private void UpdateGarage()
        {
            bool rookieCustomizationStep =
                onboarding != null &&
                onboarding.IsCustomizationStep;

            bool metaUnlocked =
                activityManager != null &&
                activityManager.SecondaryProgressionAllowed;

            if (!garageUiStateInitialized ||
                rookieCustomizationStep !=
                    lastGarageRookieColorStep ||
                metaUnlocked !=
                    lastGarageMetaUnlocked)
            {
                garageUiStateInitialized =
                    true;

                lastGarageRookieColorStep =
                    rookieCustomizationStep;

                lastGarageMetaUnlocked =
                    metaUnlocked;

                garageUiDirty =
                    true;
            }

            for (int i = 0;
                 i < garageActionButtons.Length;
                 i++)
            {
                if (garageActionButtons[i] == null)
                    continue;

                // During the tutorial the player may make any appearance
                // change: body color, wheel style or neon. Vehicle switching,
                // passport and unrelated meta controls stay out of the way.
                bool visible =
                    !rookieCustomizationStep ||
                    (i >= 2 && i <= 4) ||
                    i == 6;

                SetActiveIfChanged(
                    garageActionButtons[i],
                    visible);
            }

            if (!metaUnlocked)
            {
                if (garagePassportOpen)
                {
                    garagePassportOpen =
                        false;

                    garageUiDirty =
                        true;
                }
            }
            else if (MotorCityInput.WasVirtualPressed(
                         MotorCityInputAction.ToggleVehiclePassport))
            {
                garagePassportOpen =
                    !garagePassportOpen;

                garageUiDirty =
                    true;
            }

            SetActiveIfChanged(
                garagePassportPanel,
                garagePassportOpen);

            if (!garageUiDirty)
                return;

            if (garagePassportOpen)
            {
                UpdateVehiclePassport();
            }

            garageUiDirty =
                false;

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
                    (activityManager != null
                        ? activityManager.ReputationLevel
                        : 1).ToString();
            }

            if (garageHeaderMasteryText != null)
            {
                garageHeaderMasteryText.text =
                    metaUnlocked &&
                    garage != null
                        ? GarageReferenceMasteryNumber(garage.VehicleMasteryShort)
                        : string.Empty;
            }

            if (garageReferenceMasteryValue != null)
                garageReferenceMasteryValue.text = garageHeaderMasteryText != null
                    ? garageHeaderMasteryText.text : string.Empty;

            if (garageHeaderLevelFill != null)
            {
                int totalReputation =
                    activityManager != null
                        ? Mathf.Max(
                            0,
                            activityManager.TotalReputation)
                        : 0;

                float levelProgress =
                    (totalReputation % 500) /
                    500f;

                SetGarageTrackProgress(garageHeaderLevelFill, levelProgress);
            }

            if (garageHeaderMasteryFill != null)
            {
                RectTransform fillRect =
                    garageHeaderMasteryFill.rectTransform;

                RectTransform trackRect =
                    fillRect.parent as RectTransform;

                float masteryProgress =
                    metaUnlocked && garage != null
                        ? Mathf.Clamp01(
                            garage.VehicleMasteryCompletion)
                        : 0f;

                fillRect.sizeDelta =
                    new Vector2(
                        trackRect != null
                            ? trackRect.rect.width *
                              masteryProgress
                            : 0f,
                        fillRect.sizeDelta.y);
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

            if (garageReferenceVehicleState != null)
            {
                garageReferenceVehicleState.symbol = garage.SelectedVehicleUnlocked
                    ? GarageReferenceGraphic.Symbol.OpenPadlock : GarageReferenceGraphic.Symbol.Padlock;
                garageReferenceVehicleState.color = garage.SelectedVehicleUnlocked
                    ? GarageReferenceGreen : new Color(1f, .52f, .24f, 1f);
                garageReferenceVehicleState.SetVerticesDirty();
            }
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

                string[] statRows =
                    string.IsNullOrWhiteSpace(
                        statsLine)
                        ? new string[0]
                        : statsLine.Split('\n');

                for (int i = 0;
                     i < garageVehicleStatFills.Length;
                     i++)
                {
                    string row =
                        i < statRows.Length
                            ? statRows[i]
                            : string.Empty;

                    string label =
                        row;
                    string value =
                        string.Empty;

                    int splitIndex =
                        row.IndexOf(
                            "  ",
                            System.StringComparison.Ordinal);

                    if (splitIndex >= 0)
                    {
                        label =
                            row.Substring(
                                0,
                                splitIndex);

                        value =
                            row.Substring(
                                splitIndex + 2);
                    }

                    if (garageVehicleStatLabels[i] != null)
                    {
                        garageVehicleStatLabels[i].text =
                            label;
                    }

                    if (garageVehicleStatValues[i] != null)
                    {
                        garageVehicleStatValues[i].text =
                            value;
                    }

                    Image fill =
                        garageVehicleStatFills[i];

                    if (fill == null)
                        continue;

                    RectTransform fillRect =
                        fill.rectTransform;

                    RectTransform trackRect =
                        fillRect.parent as RectTransform;

                    float progress =
                        string.IsNullOrWhiteSpace(
                            row)
                            ? 0f
                            : ResolveGarageStatProgress(
                                row,
                                i);

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
                            trackRect != null
                                ? trackRect.rect.width * progress
                                : 0f,
                            fillRect.sizeDelta.y);
                }
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
                            garage.VehicleMasteryCompletion)
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

            for (int i = 0; i < 3; i++)
            {
                garageTitleTexts[i].text =
                    garage.GetUpgradeName(
                        i);

                if (garageLevelTexts[i] != null)
                {
                    garageLevelTexts[i].text =
                        garage.GetUpgradeLevelText(
                            i);
                }

                bool maxed =
                    garage.IsUpgradeMaxed(
                        i);

                if (garagePriceTexts[i] != null)
                {
                    garagePriceTexts[i].text =
                        maxed
                            ? string.Empty
                            : garage.GetUpgradeCost(i).ToString("N0");

                    garagePriceTexts[i].gameObject.SetActive(
                        !maxed);
                }

                if (garageUpgradeActionTexts[i] != null)
                {
                    garageUpgradeActionTexts[i].text =
                        maxed
                            ? MotorCityLocalization.Text(
                                "garage.bought")
                            : MotorCityLocalization.Text(
                                "garage.upgrade_action");

                    garageUpgradeActionTexts[i].color =
                        maxed
                            ? SecondaryTextColor
                            : GarageReferenceCyan;

                    ConfigureGarageUpgradeAction(garageUpgradeActionTexts[i], maxed);
                    Transform upgradeArrow = garageUpgradeActionTexts[i].transform.parent
                        .Find("Reference Upgrade Arrow");
                    if (upgradeArrow != null) upgradeArrow.gameObject.SetActive(!maxed);
                }

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
                    affordable ? Color.white : priceColor;

                if (garagePriceIcons[i] != null)
                {
                    garagePriceIcons[i].gameObject.SetActive(
                        !maxed);

                    garagePriceIcons[i].enabled = !maxed;

                    garagePriceIcons[i].color =
                        priceColor;
                }

                int upgradeLevel =
                    garage.GetUpgradeLevel(
                        i);

                for (int segment = 0;
                     segment < 5;
                     segment++)
                {
                    Image segmentImage =
                        garageUpgradeLevelSegments[i, segment];

                    if (segmentImage == null)
                        continue;

                    bool active =
                        segment <
                        upgradeLevel;

                    Color activeColor = new Color(0.72f, 0.20f, 1f, 1f);

                    segmentImage.color =
                        active
                            ? activeColor
                            : new Color(
                                0.08f,
                                0.09f,
                                0.18f,
                                0.96f);
                }
            }

            garageStatusText.text =
                rookieCustomizationStep
                    ? onboarding.ObjectiveLine
                    : string.Empty;
        }

        private static float ResolveGarageStatProgress(
            string statRow,
            int statIndex)
        {
            if (string.IsNullOrWhiteSpace(
                    statRow))
            {
                return 0f;
            }

            int sign =
                1;

            int value =
                0;

            bool foundDigit =
                false;

            foreach (char character in statRow)
            {
                if (!foundDigit &&
                    character == '-')
                {
                    sign =
                        -1;
                    continue;
                }

                if (character >= '0' &&
                    character <= '9')
                {
                    foundDigit =
                        true;

                    value =
                        value * 10 +
                        (character - '0');
                    continue;
                }

                if (foundDigit)
                    break;
            }

            if (!foundDigit)
                return 0.5f;

            float signedValue =
                value * sign;

            return statIndex switch
            {
                // Speed is now the actual base top speed from the same vehicle
                // profile that drives physics, not a hidden bonus value.
                0 =>
                    Mathf.InverseLerp(
                        90f,
                        205f,
                        signedValue),

                // Acceleration is the vehicle's base Prometeo tune. Upgrades
                // add only a small amount so the car's identity stays intact.
                1 =>
                    Mathf.InverseLerp(
                        4f,
                        10f,
                        signedValue),

                _ =>
                    Mathf.InverseLerp(
                        -30f,
                        35f,
                        signedValue)
            };
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

    }
}
