using MotorCity.World;
using MotorCity.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private void BuildPlayerCard(
            Transform canvas)
        {
            RectTransform card =
                CreatePanel(
                    canvas,
                    "Player Card",
                    new Vector2(
                        18f,
                        -18f),
                    new Vector2(
                        360f,
                        124f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    new Color(
                        0.018f,
                        0.032f,
                        0.052f,
                        0.92f));

            CreateAccent(
                card,
                BlueAccent,
                new Vector2(
                    5f,
                    -7f),
                new Vector2(
                    4f,
                    110f),
                new Vector2(
                    0f,
                    1f),
                new Vector2(
                    0f,
                    1f));

            CreateAccent(
                card,
                new Color(
                    BlueAccent.r,
                    BlueAccent.g,
                    BlueAccent.b,
                    0.20f),
                new Vector2(
                    18f,
                    -76f),
                new Vector2(
                    324f,
                    1f),
                new Vector2(
                    0f,
                    1f),
                new Vector2(
                    0f,
                    1f));

            Text label =
                CreateText(
                    card,
                    "City Label",
                    10,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        18f,
                        -8f),
                    new Vector2(
                        110f,
                        16f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    SecondaryTextColor);

            label.text =
                "MOTOR CITY";

            CreateHudIcon(
                card,
                "Credits Icon",
                MotorCityIconLibrary.Credits,
                new Vector2(
                    -218f,
                    -10f),
                new Vector2(
                    18f,
                    18f),
                new Vector2(
                    1f,
                    1f),
                new Color(
                    1f,
                    0.78f,
                    0.20f,
                    1f));

            moneyText =
                CreateText(
                    card,
                    "Credits",
                    24,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    new Vector2(
                        -14f,
                        -5f),
                    new Vector2(
                        190f,
                        30f),
                    new Vector2(
                        1f,
                        1f),
                    new Vector2(
                        1f,
                        1f),
                    TextColor);

            CreateHudIcon(
                card,
                "Reputation Icon",
                MotorCityIconLibrary.Reputation,
                new Vector2(
                    -214f,
                    -36f),
                new Vector2(
                    14f,
                    14f),
                new Vector2(
                    1f,
                    1f),
                BlueAccent);

            reputationText =
                CreateText(
                    card,
                    "Reputation",
                    10,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    new Vector2(
                        -14f,
                        -35f),
                    new Vector2(
                        190f,
                        17f),
                    new Vector2(
                        1f,
                        1f),
                    new Vector2(
                        1f,
                        1f),
                    SecondaryTextColor);

            RectTransform driveChip =
                CreatePanel(
                    card,
                    "Drive Mode Chip",
                    new Vector2(
                        16f,
                        -45f),
                    new Vector2(
                        126f,
                        24f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    new Color(
                        0.035f,
                        0.07f,
                        0.11f,
                        0.88f));

            driveModeText =
                CreateText(
                    driveChip,
                    "Drive Mode",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(
                        116f,
                        20f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    BlueAccent);

            Text objectiveLabel =
                CreateText(
                    card,
                    "Objective Label",
                    9,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        18f,
                        -80f),
                    new Vector2(
                        60f,
                        13f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    BlueAccent);

            objectiveLabel.text =
                MotorCityLocalization.Text(
                    "hud.objective_label");

            objectiveText =
                CreateText(
                    card,
                    "Active Objective",
                    11,
                    FontStyle.Bold,
                    TextAnchor.LowerLeft,
                    new Vector2(
                        74f,
                        8f),
                    new Vector2(
                        294f,
                        38f),
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        0f,
                        0f),
                    TextColor);

            // Keep these references alive for the existing HUD update loop,
            // but remove the persistent economy/mode/objective block from
            // gameplay. Credits and progression remain available in purchase
            // surfaces such as the garage/store.
            card.gameObject.SetActive(
                false);
        }

        private void BuildSpeedometer(Transform canvas)
        {
            RectTransform panel = CreatePanel(canvas, "Speedometer",
                new Vector2(0f, 16f), new Vector2(258f, 190f),
                new Vector2(.5f, 0f), new Vector2(.5f, 0f), Color.clear);
            openingSpeedometerRoot = panel.gameObject;

            RectTransform dial = CreatePanel(panel, "Speed Readout",
                new Vector2(0f, 88f), new Vector2(238f, 104f),
                new Vector2(.5f, 0f), new Vector2(.5f, 0f), Color.clear);
            ClearPanelChrome(dial);
            speedText = CreateText(dial, "Speed", 42, FontStyle.Bold,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(216f, 74f),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Color.white);
            speedText.resizeTextForBestFit = false;
            MotorCityTextLayout.Configure(speedText);
            speedText.alignment = TextAnchor.MiddleRight;
            speedText.alignByGeometry = false;
            speedText.rectTransform.anchoredPosition = new Vector2(-54f, 0f);
            speedText.rectTransform.sizeDelta = new Vector2(130f, 74f);
            speedText.text = "0";
            Text speedUnit = CreateText(dial, "Speed Unit", 23, FontStyle.Bold,
                TextAnchor.MiddleLeft, new Vector2(64f, 0f), new Vector2(95f, 42f),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), GarageReferenceLilac);
            speedUnit.text = MotorCityLocalization.Text("common.kmh");
            speedUnit.alignByGeometry = false;
            speedUnit.resizeTextForBestFit = false;

            RectTransform mode = CreatePanel(panel, "Drive Mode Indicator",
                new Vector2(-92f, 4f), new Vector2(172f, 38f),
                new Vector2(.5f, 0f), new Vector2(.5f, 0f), Color.clear);
            ApplyReferenceHudSurface(mode);
            driveModeText = CreateText(mode, "Drive Mode", 15, FontStyle.Bold,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(148f, 28f),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), GarageReferenceCyan);
            lastDisplayedDriveMode = null;
            lastDisplayedSpeed = int.MinValue;
        }
        private void BuildStatus(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Activity Status",
                    new Vector2(0f, -196f),
                    new Vector2(640f, 58f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    Color.clear);

            statusPanel = panel.gameObject;

            ApplyVillePanelTexture(
                panel,
                0.90f);


            statusActivityIcon =
                CreateHudIcon(
                    panel,
                    "Status Activity Icon",
                    MotorCityIconLibrary.Reward,
                    new Vector2(
                        18f,
                        0f),
                    new Vector2(
                        24f,
                        24f),
                    new Vector2(
                        0f,
                        0.5f),
                    BlueAccent);

            statusText =
                CreateText(
                    panel,
                    "Status Text",
                    17,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(58f, 0f),
                    new Vector2(560f, 42f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    TextColor);
        }

        private void RefreshStatusActivityIcon()
        {
            if (statusActivityIcon == null)
                return;

            Sprite sprite;

            if (storeOpen)
            {
                sprite =
                    MotorCityIconLibrary.Store;
            }
            else if (activityManager != null &&
                     !string.IsNullOrWhiteSpace(
                         activityManager.ActiveId))
            {
                sprite =
                    MotorCityIconLibrary.ForActivity(
                        activityManager.ActiveId);
            }
            else
            {
                sprite =
                    MotorCityIconLibrary.Reward;
            }

            statusActivityIcon.sprite =
                sprite;

            statusActivityIcon.enabled =
                sprite != null;
        }

        private void BuildDriftPanel(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Drift HUD",
                    new Vector2(0f, -58f),
                    new Vector2(304f, 48f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    Color.clear);

            driftPanel = panel.gameObject;

            ApplyVillePanelTexture(
                panel,
                0.96f);

            driftText =
                CreateText(
                    panel,
                    "Drift Score",
                    20,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 1f),
                    new Vector2(268f, 32f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);
        }

        private string ResolveContextualStatus()
        {
            if (garage != null &&
                garage.IsNearGarage &&
                !garage.IsOpen)
            {
                return garage.StatusText;
            }

            if (activityManager != null &&
                activityManager.IsBusy)
            {
                return activityManager.ActiveId switch
                {
                    "delivery" =>
                        delivery?.StatusText,
                    "drift" =>
                        driftChallenge?.StatusText,
                    "sprint" =>
                        streetSprint?.StatusText,
                    "circuit" =>
                        circuitRace?.StatusText,
                    "garage" =>
                        garage?.StatusText,
                    "underground" =>
                        underground?.StatusText,
                    "profession_carwash" =>
                        carWash?.StatusText,
                    "profession_tow" =>
                        towTruck?.StatusText,
                    _ =>
                        activityManager.ActiveId != null &&
                        activityManager.ActiveId.StartsWith(
                            "profession_")
                            ? professions?.StatusText
                            : activityManager.ActiveName
                };
            }

            if (towTruck != null &&
                towTruck.IsNearStart)
            {
                return
                    towTruck.StatusText;
            }

            if (carWash != null &&
                carWash.IsNearStart)
            {
                return
                    carWash.StatusText;
            }

            if (professions != null &&
                professions.IsNearStart)
            {
                return
                    professions.StatusText;
            }

            if (delivery != null &&
                delivery.IsNearStart)
                return delivery.StatusText;

            if (driftChallenge != null &&
                driftChallenge.IsNearStart)
                return driftChallenge.StatusText;

            if (streetSprint != null &&
                streetSprint.IsNearStart)
                return streetSprint.StatusText;

            if (circuitRace != null &&
                circuitRace.IsNearStart)
                return circuitRace.StatusText;

            return
                string.Empty;
        }

        private void UpdateNotificationQueue()
        {
            string candidate =
                ResolveTransientNotification();

            if (!string.IsNullOrWhiteSpace(
                    candidate) &&
                candidate !=
                lastNotificationCandidate)
            {
                notificationQueue.Enqueue(
                    candidate);

                lastNotificationCandidate =
                    candidate;
            }

            if (string.IsNullOrWhiteSpace(
                    candidate))
            {
                lastNotificationCandidate =
                    null;
            }

            if (!string.IsNullOrWhiteSpace(
                    activeNotification))
            {
                activeNotificationTimer -=
                    Time.unscaledDeltaTime;

                if (activeNotificationTimer > 0f)
                    return;

                activeNotification =
                    null;
            }

            if (notificationQueue.Count <= 0)
                return;

            activeNotification =
                notificationQueue.Dequeue();

            // Give longer messages enough screen time without making
            // short rewards/prompts feel sluggish.
            activeNotificationTimer =
                Mathf.Clamp(
                    2.8f +
                    activeNotification.Length * 0.018f,
                    2.8f,
                    4.4f);
        }

        private string ResolveTransientNotification()
        {
            if (car != null &&
                car.ShowControlHintMessage)
            {
                return
                    car.ControlHintMessage;
            }

            if (onboarding != null &&
                !onboarding.IsComplete)
            {
                return
                    onboarding.ShowMessage
                        ? onboarding.StatusText
                        : string.Empty;
            }

            if (story != null &&
                !story.IsComplete)
            {
                return
                    story.ShowMessage
                        ? story.StatusText
                        : string.Empty;
            }

            if (cosmeticStore != null &&
                cosmeticStore.ShowMessage)
            {
                return
                    cosmeticStore.StatusText;
            }

            if (rewardedBonus != null &&
                rewardedBonus.ShowMessage)
            {
                return
                    rewardedBonus.StatusText;
            }

            if (club != null &&
                club.ShowMessage)
            {
                return
                    club.StatusText;
            }

            if (achievements != null &&
                achievements.ShowMessage)
            {
                return
                    achievements.StatusText;
            }

            if (season != null &&
                season.ShowMessage)
            {
                return
                    season.StatusText;
            }

            if (photoHunt != null &&
                photoHunt.ShowMessage)
            {
                return
                    photoHunt.StatusText;
            }

            if (dailyAdventures != null &&
                dailyAdventures.ShowMessage)
            {
                return
                    dailyAdventures.StatusText;
            }

            if (turbo != null &&
                turbo.ShowMessage)
            {
                return
                    turbo.StatusText;
            }

            if (cityRisk != null &&
                cityRisk.ShowMessage)
            {
                return
                    cityRisk.StatusText;
            }

            if (underground != null &&
                underground.ShowMessage)
            {
                return
                    underground.StatusText;
            }

            if (legends != null &&
                legends.ShowMessage)
            {
                return
                    legends.StatusText;
            }

            if (collection != null &&
                collection.ShowMessage)
            {
                return
                    collection.StatusText;
            }

            if (vehicleSpecialization != null &&
                vehicleSpecialization.ShowMessage)
            {
                return
                    vehicleSpecialization.StatusText;
            }

            if (liveEvents != null &&
                liveEvents.ShowMessage)
            {
                return
                    liveEvents.StatusText;
            }

            if (contracts != null &&
                contracts.ShowMessage)
            {
                return
                    contracts.StatusText;
            }

            if (activityManager != null &&
                activityManager.DisciplineShowMessage)
            {
                return
                    activityManager.DisciplineStatusText;
            }

            if (garage != null &&
                garage.MasteryShowMessage)
            {
                return
                    garage.MasteryStatusText;
            }

            if (career != null &&
                career.ShowMessage)
            {
                return
                    career.StatusText;
            }

            if (driftSpots != null &&
                driftSpots.ShowMessage)
            {
                return
                    driftSpots.StatusText;
            }

            if (discoveries != null &&
                discoveries.ShowMessage)
            {
                return
                    discoveries.StatusText;
            }

            if (speedTraps != null &&
                speedTraps.ShowMessage)
            {
                return
                    speedTraps.StatusText;
            }

            return
                string.Empty;
        }

    }
}
