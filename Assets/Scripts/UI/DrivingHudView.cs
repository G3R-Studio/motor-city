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
                        268f,
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
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Speedometer",
                    new Vector2(0f, 16f),
                    new Vector2(258f, 190f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Color(
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.34f));

            Vector2 gaugeCenter =
                new(0f, 132f);

            Texture2D racingFace =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.speedometerPrimary;

            if (racingFace != null)
            {
                GameObject faceObject =
                    new(
                        "Racing Speedometer Face",
                        typeof(RectTransform),
                        typeof(RawImage));

                faceObject.transform.SetParent(
                    panel,
                    false);

                RectTransform face =
                    faceObject.GetComponent<RectTransform>();

                face.anchorMin =
                    new Vector2(0.5f, 0f);
                face.anchorMax =
                    new Vector2(0.5f, 0f);
                face.pivot =
                    new Vector2(0.5f, 0.5f);
                face.anchoredPosition =
                    gaugeCenter;
                face.sizeDelta =
                    new Vector2(188f, 188f);

                RawImage faceImage =
                    faceObject.GetComponent<RawImage>();

                faceImage.texture =
                    racingFace;
                faceImage.color =
                    Color.white;
                faceImage.raycastTarget =
                    false;
            }
            else
            {
                const float tickRadius = 67f;
                const int tickCount = 13;

                for (int i = 0;
                     i < tickCount;
                     i++)
                {
                    float t =
                        i /
                        (float)(tickCount - 1);

                    float angle =
                        Mathf.Lerp(
                            -135f,
                            135f,
                            t);

                    float radians =
                        angle *
                        Mathf.Deg2Rad;

                    GameObject tickObject =
                        new(
                            "Speed Tick " + i,
                            typeof(RectTransform),
                            typeof(Image));

                    tickObject.transform.SetParent(
                        panel,
                        false);

                    RectTransform tick =
                        tickObject.GetComponent<RectTransform>();

                    tick.anchorMin =
                        new Vector2(0.5f, 0f);
                    tick.anchorMax =
                        new Vector2(0.5f, 0f);
                    tick.pivot =
                        new Vector2(0.5f, 0.5f);
                    tick.anchoredPosition =
                        gaugeCenter +
                        new Vector2(
                            Mathf.Sin(radians) *
                            tickRadius,
                            Mathf.Cos(radians) *
                            tickRadius);
                    tick.sizeDelta =
                        new Vector2(
                            i % 2 == 0
                                ? 4f
                                : 3f,
                            i % 2 == 0
                                ? 15f
                                : 9f);
                    tick.localRotation =
                        Quaternion.Euler(
                            0f,
                            0f,
                            -angle);

                    Image tickImage =
                        tickObject.GetComponent<Image>();

                    tickImage.color =
                        i >= tickCount - 3
                            ? DriftAccent
                            : SecondaryTextColor;
                    tickImage.raycastTarget =
                        false;
                }
            }

            GameObject needleGlowObject =
                new(
                    "Speed Needle Glow",
                    typeof(RectTransform),
                    typeof(RawImage));

            needleGlowObject.transform.SetParent(
                panel,
                false);

            speedNeedleGlowRect =
                needleGlowObject.GetComponent<RectTransform>();

            speedNeedleGlowRect.anchorMin =
                new Vector2(0.5f, 0f);
            speedNeedleGlowRect.anchorMax =
                new Vector2(0.5f, 0f);
            speedNeedleGlowRect.pivot =
                new Vector2(0.5f, 0.08f);
            speedNeedleGlowRect.anchoredPosition =
                gaugeCenter;
            speedNeedleGlowRect.sizeDelta =
                new Vector2(22f, 96f);
            speedNeedleGlowRect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    135f);

            speedNeedleGlow =
                needleGlowObject.GetComponent<RawImage>();

            speedNeedleGlow.texture =
                GetSpeedNeedleGlowTexture();
            speedNeedleGlow.color =
                new Color(
                    0.08f,
                    0.46f,
                    1f,
                    0f);
            speedNeedleGlow.raycastTarget =
                false;

            GameObject needleObject =
                new(
                    "Speed Needle",
                    typeof(RectTransform),
                    typeof(RawImage));

            needleObject.transform.SetParent(
                panel,
                false);

            speedNeedle =
                needleObject.GetComponent<RectTransform>();

            speedNeedle.anchorMin =
                new Vector2(0.5f, 0f);
            speedNeedle.anchorMax =
                new Vector2(0.5f, 0f);
            speedNeedle.pivot =
                new Vector2(0.5f, 0.08f);
            speedNeedle.anchoredPosition =
                gaugeCenter;

            Texture2D needleTexture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.needleLong;

            float needleHeight =
                84f;

            speedNeedle.sizeDelta =
                new Vector2(
                    7f,
                    needleHeight);
            speedNeedle.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    135f);

            RawImage needleImage =
                needleObject.GetComponent<RawImage>();

            needleImage.texture =
                needleTexture;
            needleImage.color =
                new Color32(
                    0xB9,
                    0xB8,
                    0xB7,
                    0xFF);
            needleImage.raycastTarget =
                false;

            speedText =
                CreateText(
                    panel,
                    "Speed",
                    36,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 68f),
                    new Vector2(128f, 42f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0.5f),
                    new Color32(
                        0xE4,
                        0xE5,
                        0xED,
                        0xFF));

            RectTransform driveModeChip;

            Texture2D chipTexture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.driveModePanel;

            if (chipTexture != null)
            {
                GameObject chipObject =
                    new(
                        "Drive Mode Indicator",
                        typeof(RectTransform),
                        typeof(RawImage));

                chipObject.transform.SetParent(
                    panel,
                    false);

                driveModeChip =
                    chipObject.GetComponent<RectTransform>();

                driveModeChip.anchorMin =
                    new Vector2(0.5f, 0f);
                driveModeChip.anchorMax =
                    new Vector2(0.5f, 0f);
                driveModeChip.pivot =
                    new Vector2(0.5f, 0.5f);
                driveModeChip.anchoredPosition =
                    new Vector2(0f, 18f);
                driveModeChip.sizeDelta =
                    new Vector2(158f, 32f);

                RawImage chipImage =
                    chipObject.GetComponent<RawImage>();

                chipImage.texture =
                    chipTexture;
                chipImage.color =
                    Color.white;
                chipImage.raycastTarget =
                    false;
            }
            else
            {
                driveModeChip =
                    CreatePanel(
                        panel,
                        "Drive Mode Indicator",
                        new Vector2(0f, 18f),
                        new Vector2(172f, 34f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0.5f, 0.5f),
                        PanelColor);
            }

            driveModeText =
                CreateText(
                    driveModeChip,
                    "Drive Mode",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(138f, 24f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    BlueAccent);

            lastDisplayedDriveMode =
                null;
        }

        private static Texture2D GetSpeedNeedleGlowTexture()
        {
            if (speedNeedleGlowTexture != null)
                return speedNeedleGlowTexture;

            const int width = 32;
            const int height = 128;

            Texture2D texture =
                new(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false);

            texture.name =
                "Motor City Speed Needle Soft Glow";
            texture.wrapMode =
                TextureWrapMode.Clamp;
            texture.filterMode =
                FilterMode.Bilinear;
            texture.hideFlags =
                HideFlags.DontSave;

            Color[] pixels =
                new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v =
                    y / (height - 1f);

                float endFade =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(
                            0f,
                            0.10f,
                            v)) *
                    (1f -
                     Mathf.SmoothStep(
                         0f,
                         1f,
                         Mathf.InverseLerp(
                             0.86f,
                             1f,
                             v)));

                for (int x = 0; x < width; x++)
                {
                    float u =
                        x / (width - 1f);
                    float distanceFromCenter =
                        Mathf.Abs(u - 0.5f) * 2f;

                    float sideFade =
                        Mathf.Exp(
                            -distanceFromCenter *
                            distanceFromCenter *
                            4.5f);

                    float alpha =
                        sideFade *
                        endFade;

                    pixels[y * width + x] =
                        new Color(
                            1f,
                            1f,
                            1f,
                            alpha);
                }
            }

            texture.SetPixels(
                pixels);
            texture.Apply(
                false,
                true);

            speedNeedleGlowTexture =
                texture;

            return speedNeedleGlowTexture;
        }

        private void UpdateSpeedNeedleGlow()
        {
            if (speedNeedleGlow == null)
                return;

            if (dayNightCycle == null)
            {
                dayNightResolveTimer -=
                    Time.unscaledDeltaTime;

                if (dayNightResolveTimer <= 0f)
                {
                    dayNightResolveTimer =
                        1f;

                    dayNightCycle =
                        Object.FindAnyObjectByType<
                            DayNightCycleController>();
                }
            }

            float nightAmount =
                dayNightCycle == null
                    ? 0f
                    : dayNightCycle.NightAmount;

            float glow =
                Mathf.SmoothStep(
                    0f,
                    0.38f,
                    Mathf.InverseLerp(
                        0.42f,
                        0.92f,
                        nightAmount));

            speedNeedleGlow.color =
                new Color(
                    0.08f,
                    0.46f,
                    1f,
                    glow);
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

            CenterStatusPanelChrome(
                panel);

            statusActivityIcon =
                CreateHudIcon(
                    panel,
                    "Status Activity Icon",
                    MotorCityIconLibrary.Reward,
                    new Vector2(
                        22f,
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
                    new Vector2(56f, 1f),
                    new Vector2(560f, 42f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    TextColor);
        }

        private static void CenterStatusPanelChrome(
            RectTransform panel)
        {
            if (panel == null)
                return;

            const float visualOffsetX =
                -112f;

            foreach (RectTransform child in
                     panel.GetComponentsInChildren<RectTransform>(
                         true))
            {
                if (child == null ||
                    child == panel)
                {
                    continue;
                }

                string childName =
                    child.name;

                if (childName == "Ville Panel Background" ||
                    childName.StartsWith(
                        "Ville Panel Glow"))
                {
                    Vector2 position =
                        child.anchoredPosition;

                    child.anchoredPosition =
                        new Vector2(
                            visualOffsetX,
                            position.y);
                }
            }
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

            if (weekendEvents != null &&
                weekendEvents.ShowMessage)
            {
                return
                    weekendEvents.StatusText;
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
