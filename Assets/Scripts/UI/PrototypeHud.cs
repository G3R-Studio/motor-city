using System.Collections.Generic;
using MotorCity.Gameplay;
using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private ArcadeCarController car;
        private PlayerWallet wallet;
        private DriftTracker drift;
        private DeliveryActivity delivery;
        private DriftChallenge driftChallenge;
        private StreetSprintActivity streetSprint;
        private CircuitRaceActivity circuitRace;
        private SpeedTrapSystem speedTraps;
        private DriftSpotSystem driftSpots;
        private DiscoverySystem discoveries;
        private StuntJumpSystem stuntJumps;
        private ActivityManager activityManager;
        private GarageUpgradeSystem garage;
        private CareerProgressionSystem career;
        private VehicleHistorySystem vehicleHistory;
        private VehicleSpecializationSystem vehicleSpecialization;
        private CollectionProgressionSystem collection;
        private CityLegendSystem legends;
        private CityContractSystem contracts;
        private CityLiveEventSystem liveEvents;
        private UndergroundSceneSystem underground;
        private CityRiskSystem cityRisk;
        private TurboPetSystem turbo;
        private FirstSessionOnboardingSystem onboarding;
        private DailyAdventureSystem dailyAdventures;
        private StoryMissionSystem story;
        private SeasonSystem season;
        private PhotoHuntSystem photoHunt;
        private CityProfessionSystem professions;
        private CarWashJobSystem carWash;
        private TowTruckJobSystem towTruck;
        private ClubSystem club;
        private WeekendEventSystem weekendEvents;
        private RewardedBonusSystem rewardedBonus;
        private CosmeticStoreSystem cosmeticStore;
        private bool storeOpen;
        private AchievementSystem achievements;
        private AdventureDirector adventureDirector;

        private Font font;
        private Sprite panelSprite;

        private Text moneyText;
        private Text reputationText;
        private Text upgradesText;
        private Text driveModeText;
        private Text hintText;
        private Text speedText;
        private Text speedUnitText;
        private Text statusText;
        private Text driftText;
        private Text navigatorArrowText;
        private Text navigatorText;
        private Text careerText;
        private Text disciplineText;
        private Text contractText;
        private Text liveEventText;
        private Text collectionText;
        private Text legendText;
        private Text objectiveText;
        private GameObject characterPanel;
        private Text characterAvatarText;
        private Text characterNameText;
        private Text characterLineText;
        private RawImage minimapImage;
        private RectTransform minimapTargetBlip;
        private RectTransform minimapPlayerArrow;
        private Text minimapTargetText;
        private readonly RectTransform[] minimapRouteDots =
            new RectTransform[36];
        private GameObject navigatorMenuOverlay;
        private Text navigatorMenuText;
        private bool navigatorMenuOpen;
        private int navigatorSelection;
        private bool manualNavigationActive;
        private Vector3 manualNavigationTarget;
        private string manualNavigationLabel;
        private CitySchematicMap schematicMap;
        private Texture2D minimapMaskTexture;
        private Sprite minimapMaskSprite;

        private GameObject navigatorPanel;
        private GameObject statusPanel;
        private GameObject driftPanel;
        private GameObject garageOverlay;
        private GameObject garagePassportPanel;
        private GameObject activityResultOverlay;
        private GameObject clubOverlay;
        private Text clubEmblemText;
        private Text clubNameText;
        private Text clubDescriptionText;
        private Text clubWeeklyText;
        private Text clubControlsText;
        private RectTransform safeAreaRoot;

        private readonly Queue<string> notificationQueue =
            new();
        private string lastNotificationCandidate;
        private string activeNotification;
        private float activeNotificationTimer;

        private Text resultTitleText;
        private Text resultHeadlineText;
        private Text resultDetailsText;
        private Text resultRewardText;
        private Text resultControlsText;

        private Text garageMoneyText;
        private Text garageStatusText;
        private Text garageVehicleText;
        private Text garageVehicleStatsText;
        private Text garageVehicleHistoryText;
        private Text garageVehicleSpecializationText;
        private Text garageCollectionText;
        private Text garagePassportTitleText;
        private Text garagePassportSummaryText;
        private Text garagePassportDisciplinesText;
        private Text garagePassportMasteryText;
        private Text garagePassportSpecializationText;
        private bool garagePassportOpen;
        private readonly Text[] garageTitleTexts =
            new Text[3];
        private readonly Text[] garagePriceTexts =
            new Text[3];
        private readonly Text[] garageDescriptionTexts =
            new Text[3];

        private static readonly Color PanelColor =
            new(0.025f, 0.032f, 0.045f, 0.90f);
        private static readonly Color PanelSoftColor =
            new(0.035f, 0.045f, 0.06f, 0.82f);
        private static readonly Color TextColor =
            new(0.95f, 0.97f, 1f, 1f);
        private static readonly Color SecondaryTextColor =
            new(0.66f, 0.72f, 0.8f, 1f);
        private static readonly Color BlueAccent =
            new(0.12f, 0.58f, 1f, 1f);
        private static readonly Color DriftAccent =
            new(1f, 0.55f, 0.12f, 1f);
        private static readonly Color GarageAccent =
            new(0.66f, 0.3f, 1f, 1f);

        public void Bind(
            ArcadeCarController controller,
            PlayerWallet playerWallet,
            DriftTracker driftTracker,
            DeliveryActivity deliveryActivity,
            DriftChallenge challenge,
            StreetSprintActivity sprint,
            CircuitRaceActivity circuit,
            SpeedTrapSystem speedTrapSystem,
            DriftSpotSystem driftSpotSystem,
            DiscoverySystem discoverySystem,
            StuntJumpSystem stuntJumpSystem,
            ActivityManager manager,
            GarageUpgradeSystem garageSystem,
            CareerProgressionSystem careerSystem,
            VehicleHistorySystem historySystem,
            VehicleSpecializationSystem specializationSystem,
            CollectionProgressionSystem collectionSystem,
            CityLegendSystem legendSystem,
            CityContractSystem contractSystem,
            CityLiveEventSystem liveEventSystem,
            UndergroundSceneSystem undergroundSystem,
            CityRiskSystem riskSystem,
            TurboPetSystem turboSystem,
            FirstSessionOnboardingSystem onboardingSystem,
            DailyAdventureSystem dailyAdventureSystem,
            StoryMissionSystem storySystem,
            SeasonSystem seasonSystem,
            PhotoHuntSystem photoHuntSystem,
            CityProfessionSystem professionSystem,
            CarWashJobSystem carWashSystem,
            TowTruckJobSystem towTruckSystem,
            ClubSystem clubSystem,
            WeekendEventSystem weekendEventSystem,
            RewardedBonusSystem rewardedBonusSystem,
            CosmeticStoreSystem cosmeticStoreSystem,
            AchievementSystem achievementSystem,
            AdventureDirector director)
        {
            car = controller;
            wallet = playerWallet;
            drift = driftTracker;
            delivery = deliveryActivity;
            driftChallenge = challenge;
            streetSprint = sprint;
            circuitRace = circuit;
            speedTraps = speedTrapSystem;
            driftSpots = driftSpotSystem;
            discoveries = discoverySystem;
            stuntJumps = stuntJumpSystem;
            activityManager = manager;
            garage = garageSystem;
            career = careerSystem;
            vehicleHistory = historySystem;
            vehicleSpecialization = specializationSystem;
            collection = collectionSystem;
            legends = legendSystem;
            contracts = contractSystem;
            liveEvents = liveEventSystem;
            underground = undergroundSystem;
            cityRisk = riskSystem;
            turbo = turboSystem;
            onboarding = onboardingSystem;
            dailyAdventures = dailyAdventureSystem;
            story = storySystem;
            season = seasonSystem;
            photoHunt = photoHuntSystem;
            professions = professionSystem;
            carWash = carWashSystem;
            towTruck = towTruckSystem;
            club = clubSystem;
            weekendEvents = weekendEventSystem;
            rewardedBonus = rewardedBonusSystem;
            cosmeticStore = cosmeticStoreSystem;
            achievements = achievementSystem;
            adventureDirector = director;

            BuildUi();
        }

        private void Update()
        {
            if (moneyText == null)
                return;

            HandleNavigatorMenu();
            HandleStoreInput();
            HandleClubInput();

            if (MotorCityInput.RewardedBonusPressed)
            {
                rewardedBonus?.TryShow();
            }

            int credits =
                wallet == null
                    ? 0
                    : wallet.Credits;

            moneyText.text =
                MotorCityLocalization.Format(
                    "common.credits",
                    credits);

            if (reputationText != null &&
                activityManager != null)
            {
                reputationText.text =
                    MotorCityLocalization.Format(
                        "hud.rep",
                        activityManager.TotalReputation,
                        activityManager.ReputationLevel);
            }

            if (upgradesText != null)
            {
                upgradesText.text =
                    garage == null
                        ? string.Empty
                        : MotorCityLocalization.Format(
                            "hud.upgrades",
                            garage.EngineLevel,
                            garage.GripLevel,
                            garage.StabilityLevel,
                            garage.VehicleMasteryShort);
            }

            if (careerText != null)
            {
                careerText.text =
                    career == null
                        ? string.Empty
                        : career.HudLine;
            }

            if (disciplineText != null)
            {
                disciplineText.text =
                    activityManager == null
                        ? string.Empty
                        : activityManager.DisciplineHudLine;
            }

            if (contractText != null)
            {
                contractText.text =
                    contracts == null
                        ? string.Empty
                        : contracts.HudLine;
            }

            if (liveEventText != null)
            {
                liveEventText.text =
                    liveEvents == null
                        ? string.Empty
                        : liveEvents.HudLine;
            }

            if (collectionText != null)
            {
                collectionText.text =
                    collection == null
                        ? string.Empty
                        : collection.HudLine;
            }

            if (legendText != null)
            {
                legendText.text =
                    legends == null
                        ? string.Empty
                        : legends.HudLine;
            }

            if (objectiveText != null)
            {
                objectiveText.text =
                    ResolveObjectiveLine();
            }

            UpdateCharacterCard();

            if (driveModeText != null &&
                car != null)
            {
                driveModeText.text =
                    MotorCityLocalization.Format(
                        "hud.drive_mode",
                        car.DriveModeDisplayName);

                driveModeText.color =
                    car.CurrentDriveMode switch
                    {
                        DriveMode.Sport =>
                            new Color(
                                0.30f,
                                1f,
                                0.54f,
                                1f),
                        DriveMode.Drift =>
                            DriftAccent,
                        _ =>
                            BlueAccent
                    };
            }

            float speed =
                car == null
                    ? 0f
                    : car.SpeedKph;

            speedText.text =
                Mathf.RoundToInt(speed)
                    .ToString("000");

            bool resultOpen =
                activityManager != null &&
                activityManager.HasResult;

            activityResultOverlay.SetActive(
                resultOpen);

            if (resultOpen)
            {
                statusPanel.SetActive(false);
                driftPanel.SetActive(false);
                garageOverlay.SetActive(false);
                navigatorPanel.SetActive(false);
                UpdateActivityResult();
                HandleActivityResultInput();
                return;
            }

            if (clubOverlay != null &&
                clubOverlay.activeSelf)
            {
                UpdateClubOverlay();
            }

            if (storeOpen &&
                cosmeticStore != null)
            {
                garageOverlay.SetActive(false);
                navigatorPanel.SetActive(false);
                driftPanel.SetActive(false);
                statusPanel.SetActive(true);
                statusText.text =
                    MotorCityLocalization.Format(
                        "store.status",
                        MotorCityLocalization.Text(
                            "store.title") +
                        " • " +
                        cosmeticStore.SelectedName,
                        cosmeticStore.SelectedDescription,
                        cosmeticStore.SeasonPathLine +
                        " • " +
                        cosmeticStore.SelectedOwnershipLine,
                        MotorCityLocalization.Text(
                            "store.controls"));
                return;
            }

            UpdateNotificationQueue();

            string status =
                !string.IsNullOrWhiteSpace(
                    activeNotification)
                    ? activeNotification
                    : ResolveContextualStatus();

            statusPanel.SetActive(
                !string.IsNullOrWhiteSpace(
                    status));

            if (statusPanel.activeSelf)
                statusText.text = status;

            bool showDrift =
                drift != null &&
                (drift.IsDrifting ||
                 drift.CurrentScore > 0 ||
                 drift.ShowRewardMessage);

            driftPanel.SetActive(showDrift);

            if (showDrift)
            {
                if (drift.IsDrifting ||
                    drift.CurrentScore > 0)
                {
                    string combo =
                        drift.Combo > 1.05f
                            ? $"   x{drift.Combo:0.0}"
                            : string.Empty;

                    driftText.text =
                        MotorCityLocalization.Format(
                            "hud.drift",
                            drift.CurrentScore,
                            combo);
                }
                else
                {
                    driftText.text =
                        MotorCityLocalization.Format(
                            "hud.drift_done",
                            drift.LastBankedCredits);
                }
            }

            bool garageOpen =
                garage != null &&
                garage.IsOpen;

            garageOverlay.SetActive(garageOpen);

            if (!garageOpen)
            {
                garagePassportOpen = false;

                if (garagePassportPanel != null)
                {
                    garagePassportPanel.SetActive(
                        false);
                }
            }

            if (garageOpen)
                UpdateGarage();

            UpdateNavigator(
                garageOpen);
        }

        private void HandleStoreInput()
        {
            if (cosmeticStore == null)
                return;

            if (MotorCityInput.ToggleStorePressed)
            {
                storeOpen =
                    !storeOpen;

                if (storeOpen &&
                    clubOverlay != null)
                {
                    clubOverlay.SetActive(
                        false);
                }
            }

            if (!storeOpen)
                return;

            if (MotorCityInput.CancelPressed)
            {
                storeOpen = false;
                return;
            }

            if (MotorCityInput.PreviousVehiclePressed)
            {
                cosmeticStore.CycleProduct(
                    -1);
            }

            if (MotorCityInput.NextVehiclePressed)
            {
                cosmeticStore.CycleProduct(
                    1);
            }

            if (MotorCityInput.InteractPressed)
            {
                cosmeticStore.PurchaseSelected();
            }
        }

        private void BuildNavigatorMenu(
            Transform canvas)
        {
            navigatorMenuOverlay =
                new GameObject(
                    "Navigator Menu Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            navigatorMenuOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                navigatorMenuOverlay.GetComponent<RectTransform>();

            overlay.anchorMin =
                Vector2.zero;
            overlay.anchorMax =
                Vector2.one;
            overlay.offsetMin =
                Vector2.zero;
            overlay.offsetMax =
                Vector2.zero;

            Image backdrop =
                navigatorMenuOverlay.GetComponent<Image>();

            backdrop.color =
                new Color(
                    0.005f,
                    0.008f,
                    0.012f,
                    0.72f);
            backdrop.raycastTarget =
                false;

            RectTransform panel =
                CreatePanel(
                    navigatorMenuOverlay.transform,
                    "Navigator Menu",
                    Vector2.zero,
                    new Vector2(
                        520f,
                        250f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Color(
                        0.02f,
                        0.03f,
                        0.045f,
                        0.98f));

            Text title =
                CreateText(
                    panel,
                    "Navigator Title",
                    27,
                    FontStyle.Bold,
                    TextAnchor.UpperCenter,
                    new Vector2(
                        0f,
                        -24f),
                    new Vector2(
                        470f,
                        42f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    TextColor);

            title.text =
                MotorCityLocalization.Text(
                    "navigator.title");

            navigatorMenuText =
                CreateText(
                    panel,
                    "Navigator Selection",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -4f),
                    new Vector2(
                        460f,
                        112f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    TextColor);

            Text controls =
                CreateText(
                    panel,
                    "Navigator Controls",
                    14,
                    FontStyle.Bold,
                    TextAnchor.LowerCenter,
                    new Vector2(
                        0f,
                        22f),
                    new Vector2(
                        470f,
                        28f),
                    new Vector2(
                        0.5f,
                        0f),
                    new Vector2(
                        0.5f,
                        0f),
                    SecondaryTextColor);

            controls.text =
                MotorCityLocalization.Text(
                    "navigator.controls");

            UpdateNavigatorMenuText();
        }

        private void HandleNavigatorMenu()
        {
            if (MotorCityInput.ToggleNavigatorPressed)
            {
                ToggleNavigatorMenu();
                return;
            }

            if (!navigatorMenuOpen)
                return;

            if (MotorCityInput.CancelPressed)
            {
                CloseNavigatorMenu();
                return;
            }

            int count =
                NavigatorDestinationCount();

            if (count <= 0)
                return;

            if (MotorCityInput.PreviousVehiclePressed)
            {
                navigatorSelection =
                    (navigatorSelection - 1 + count) %
                    count;

                UpdateNavigatorMenuText();
            }

            if (MotorCityInput.NextVehiclePressed)
            {
                navigatorSelection =
                    (navigatorSelection + 1) %
                    count;

                UpdateNavigatorMenuText();
            }

            if (MotorCityInput.RetryPressed)
            {
                if (TryResolveNavigatorDestination(
                        navigatorSelection,
                        out Vector3 target,
                        out string label))
                {
                    manualNavigationTarget =
                        target;
                    manualNavigationLabel =
                        label;
                    manualNavigationActive =
                        true;
                }

                CloseNavigatorMenu();
            }
        }

        private void ToggleNavigatorMenu()
        {
            if (navigatorMenuOpen)
            {
                CloseNavigatorMenu();
                return;
            }

            if (garage != null &&
                garage.IsOpen)
            {
                return;
            }

            navigatorMenuOpen =
                true;

            navigatorMenuOverlay?.SetActive(
                true);

            clubOverlay?.SetActive(
                false);

            storeOpen =
                false;

            car?.SetDrivingEnabled(
                false);

            UpdateNavigatorMenuText();
        }

        private void CloseNavigatorMenu()
        {
            navigatorMenuOpen =
                false;

            navigatorMenuOverlay?.SetActive(
                false);

            if (activityManager == null ||
                !activityManager.HasResult)
            {
                car?.SetDrivingEnabled(
                    true);
            }
        }

        private int NavigatorDestinationCount()
        {
            return
                7 +
                (professions != null
                    ? professions.StartCount
                    : 0);
        }

        private void UpdateNavigatorMenuText()
        {
            if (navigatorMenuText == null)
                return;

            int count =
                NavigatorDestinationCount();

            if (count <= 0)
            {
                navigatorMenuText.text =
                    MotorCityLocalization.Text(
                        "navigator.empty");
                return;
            }

            navigatorSelection =
                Mathf.Clamp(
                    navigatorSelection,
                    0,
                    count - 1);

            TryResolveNavigatorDestination(
                navigatorSelection,
                out _,
                out string label);

            navigatorMenuText.text =
                MotorCityLocalization.Format(
                    "navigator.selection",
                    navigatorSelection + 1,
                    count,
                    label);
        }

        private bool TryResolveNavigatorDestination(
            int index,
            out Vector3 target,
            out string label)
        {
            target =
                car != null
                    ? car.transform.position
                    : Vector3.zero;

            label =
                MotorCityLocalization.Text(
                    "hud.free_drive");

            switch (index)
            {
                case 0:
                    if (garage == null)
                        return false;

                    target =
                        garage.GarageCenter;
                    label =
                        MotorCityLocalization.Text(
                            "hud.garage");
                    return true;

                case 1:
                    if (delivery == null)
                        return false;

                    target =
                        delivery.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "activity.delivery");
                    return true;

                case 2:
                    if (driftChallenge == null)
                        return false;

                    target =
                        driftChallenge.ZoneCenter;
                    label =
                        MotorCityLocalization.Text(
                            "activity.drift");
                    return true;

                case 3:
                    if (streetSprint == null)
                        return false;

                    target =
                        streetSprint.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.sprint");
                    return true;

                case 4:
                    if (circuitRace == null)
                        return false;

                    target =
                        circuitRace.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.circuit");
                    return true;

                case 5:
                    if (towTruck == null)
                        return false;

                    target =
                        towTruck.StartPoint;
                    label =
                        MotorCityLocalization.Text(
                            "tow.title");
                    return true;

                case 6:
                    if (carWash == null)
                        return false;

                    target =
                        carWash.StartPoint;
                    label =
                        MotorCityLocalization.Text(
                            "carwash.title");
                    return true;
            }

            int professionIndex =
                index - 7;

            if (professions == null ||
                professionIndex < 0 ||
                professionIndex >=
                professions.StartCount)
            {
                return false;
            }

            target =
                professions.GetStartPoint(
                    professionIndex);

            label =
                professions.GetStartName(
                    professionIndex);

            return true;
        }

        private void UpdateRoadRoute(
            Vector3 carPosition,
            Vector3 target,
            float yaw,
            float worldRadius,
            bool showRoadRoute)
        {
            HideRouteDots();

            if (!showRoadRoute ||
                minimapRouteDots.Length == 0)
            {
                return;
            }

            List<Vector3> route =
                CityRoadNavigator.BuildRoute(
                    carPosition,
                    target);

            if (route == null ||
                route.Count < 2)
            {
                return;
            }

            const float markerRadius =
                74f;

            float mapScale =
                78f /
                worldRadius;

            int placed =
                0;

            bool reachedEdge =
                false;

            for (int segment = 0;
                 segment < route.Count - 1 &&
                 placed < minimapRouteDots.Length &&
                 !reachedEdge;
                 segment++)
            {
                Vector3 a =
                    route[segment];

                Vector3 b =
                    route[segment + 1];

                float length =
                    FlatDistance(
                        a,
                        b);

                int steps =
                    Mathf.Max(
                        1,
                        Mathf.CeilToInt(
                            length /
                            18f));

                for (int step = 1;
                     step <= steps &&
                     placed < minimapRouteDots.Length;
                     step++)
                {
                    Vector3 point =
                        Vector3.Lerp(
                            a,
                            b,
                            step /
                            (float)steps);

                    Vector3 delta =
                        point -
                        carPosition;

                    delta.y =
                        0f;

                    Vector3 local =
                        Quaternion.Euler(
                            0f,
                            -yaw,
                            0f) *
                        delta;

                    Vector2 offset =
                        new(
                            local.x *
                            mapScale,
                            local.z *
                            mapScale);

                    if (offset.sqrMagnitude >
                        markerRadius *
                        markerRadius)
                    {
                        offset =
                            offset.normalized *
                            markerRadius;

                        reachedEdge =
                            true;
                    }

                    RectTransform dot =
                        minimapRouteDots[
                            placed++];

                    dot.gameObject.SetActive(
                        true);

                    dot.anchoredPosition =
                        offset;

                    if (reachedEdge)
                        break;
                }
            }
        }

        private void HideRouteDots()
        {
            foreach (RectTransform dot in
                     minimapRouteDots)
            {
                if (dot != null)
                {
                    dot.gameObject.SetActive(
                        false);
                }
            }
        }

        private static float FlatDistance(
            Vector3 a,
            Vector3 b)
        {
            a.y =
                0f;
            b.y =
                0f;

            return
                Vector3.Distance(
                    a,
                    b);
        }

        private static void EnsureUiEventSystem()
        {
            EventSystem eventSystem =
                Object.FindAnyObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                GameObject eventSystemObject =
                    new(
                        "Motor City UI EventSystem",
                        typeof(EventSystem),
                        typeof(InputSystemUIInputModule));

                Object.DontDestroyOnLoad(
                    eventSystemObject);

                return;
            }

            InputSystemUIInputModule inputModule =
                eventSystem.GetComponent<InputSystemUIInputModule>();

            if (inputModule != null)
                return;

            BaseInputModule[] modules =
                eventSystem.GetComponents<BaseInputModule>();

            foreach (BaseInputModule module in
                     modules)
            {
                if (module != null)
                {
                    module.enabled =
                        false;
                }
            }

            eventSystem.gameObject.AddComponent<
                InputSystemUIInputModule>();
        }

        private void BuildUi()
        {
            EnsureUiEventSystem();

            font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            panelSprite =
                Resources.Load<Sprite>(
                    "MotorCity/UI/grey_panel");

            GameObject canvasObject =
                new("Motor City HUD");
            canvasObject.transform.SetParent(
                transform,
                false);

            Canvas canvas =
                canvasObject.AddComponent<Canvas>();
            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler =
                canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1600f, 900f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            safeAreaRoot =
                CreateSafeAreaRoot(
                    canvasObject.transform);

            BuildPlayerCard(safeAreaRoot);
            BuildCharacterCard(safeAreaRoot);
            BuildSpeedometer(safeAreaRoot);
            BuildStatus(safeAreaRoot);
            BuildNavigator(safeAreaRoot);
            BuildNavigatorMenu(safeAreaRoot);
            BuildDriftPanel(safeAreaRoot);
            BuildActivityResult(safeAreaRoot);
            BuildGarage(safeAreaRoot);
            BuildClubOverlay(safeAreaRoot);

            driftPanel.SetActive(false);
            activityResultOverlay.SetActive(false);
            garageOverlay.SetActive(false);
            clubOverlay.SetActive(false);
            navigatorMenuOverlay.SetActive(false);
        }

        private void BuildPlayerCard(Transform canvas)
        {
            RectTransform card =
                CreatePanel(
                    canvas,
                    "Player Card",
                    new Vector2(18f, -18f),
                    new Vector2(360f, 132f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    PanelColor);

            CreateAccent(
                card,
                BlueAccent,
                new Vector2(5f, -8f),
                new Vector2(4f, 116f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            Text label =
                CreateText(
                    card,
                    "City Label",
                    11,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(18f, -9f),
                    new Vector2(120f, 18f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);
            label.text = "MOTOR CITY";

            moneyText =
                CreateText(
                    card,
                    "Credits",
                    22,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    new Vector2(-14f, -7f),
                    new Vector2(190f, 28f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    TextColor);

            reputationText =
                CreateText(
                    card,
                    "Reputation",
                    10,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    new Vector2(-14f, -36f),
                    new Vector2(190f, 18f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    SecondaryTextColor);

            driveModeText =
                CreateText(
                    card,
                    "Drive Mode",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(18f, -52f),
                    new Vector2(320f, 20f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f),
                    BlueAccent);

            objectiveText =
                CreateText(
                    card,
                    "Active Objective",
                    11,
                    FontStyle.Bold,
                    TextAnchor.LowerLeft,
                    new Vector2(18f, 12f),
                    new Vector2(324f, 42f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    TextColor);
        }

        private void BuildCharacterCard(
            Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Character Card",
                    new Vector2(
                        18f,
                        -158f),
                    new Vector2(
                        360f,
                        64f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    PanelSoftColor);

            characterPanel =
                panel.gameObject;

            characterAvatarText =
                CreateText(
                    panel,
                    "Character Avatar",
                    24,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        12f,
                        -10f),
                    new Vector2(
                        46f,
                        46f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    TextColor);

            characterNameText =
                CreateText(
                    panel,
                    "Character Name",
                    13,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        68f,
                        -10f),
                    new Vector2(
                        270f,
                        20f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    BlueAccent);

            characterLineText =
                CreateText(
                    panel,
                    "Character Line",
                    12,
                    FontStyle.Bold,
                    TextAnchor.LowerLeft,
                    new Vector2(
                        68f,
                        10f),
                    new Vector2(
                        270f,
                        28f),
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        0f,
                        0f),
                    TextColor);

            characterPanel.SetActive(
                false);
        }

        private void UpdateCharacterCard()
        {
            if (characterPanel == null)
                return;

            string name =
                string.Empty;

            string line =
                string.Empty;

            int style =
                -1;

            if (story != null &&
                !story.IsComplete)
            {
                name =
                    story.CurrentCharacterName;

                line =
                    story.CurrentCharacterLine;

                style =
                    story.CurrentCharacterStyle;
            }
            else if (season != null &&
                     season.IsSeasonOneActive &&
                     !season.IsComplete)
            {
                name =
                    season.CurrentCharacterName;

                line =
                    season.CurrentCharacterLine;

                style =
                    season.CurrentCharacterStyle;
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

            characterAvatarText.text =
                name.Substring(
                    0,
                    1);

            characterAvatarText.color =
                accent;

            characterNameText.text =
                name;

            characterNameText.color =
                accent;

            characterLineText.text =
                line;
        }

        private Sprite CreateCircularMinimapSprite(
            int size)
        {
            minimapMaskTexture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false,
                    true)
                {
                    name =
                        "MotorCity_MinimapCircleMask",
                    filterMode =
                        FilterMode.Bilinear,
                    wrapMode =
                        TextureWrapMode.Clamp
                };

            Color[] pixels =
                new Color[
                    size *
                    size];

            float center =
                (size - 1) *
                0.5f;

            float radius =
                center -
                1f;

            float feather =
                1.5f;

            for (int y = 0;
                 y < size;
                 y++)
            {
                for (int x = 0;
                     x < size;
                     x++)
                {
                    float dx =
                        x -
                        center;

                    float dy =
                        y -
                        center;

                    float distance =
                        Mathf.Sqrt(
                            dx * dx +
                            dy * dy);

                    float alpha =
                        Mathf.Clamp01(
                            (radius -
                             distance) /
                            feather);

                    pixels[
                        y *
                        size +
                        x] =
                        new Color(
                            1f,
                            1f,
                            1f,
                            alpha);
                }
            }

            minimapMaskTexture.SetPixels(
                pixels);

            minimapMaskTexture.Apply(
                false,
                true);

            return
                Sprite.Create(
                    minimapMaskTexture,
                    new Rect(
                        0f,
                        0f,
                        size,
                        size),
                    new Vector2(
                        0.5f,
                        0.5f),
                    100f,
                    0u,
                    SpriteMeshType.FullRect);
        }

        private void BuildSpeedometer(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Speedometer",
                    new Vector2(-24f, 24f),
                    new Vector2(196f, 82f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    new Color(
                        0.01f,
                        0.015f,
                        0.022f,
                        0.54f));

            speedText =
                CreateText(
                    panel,
                    "Speed",
                    46,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-18f, 12f),
                    new Vector2(160f, 52f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    TextColor);

            speedUnitText =
                CreateText(
                    panel,
                    "Speed Unit",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-20f, -24f),
                    new Vector2(120f, 18f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    SecondaryTextColor);

            speedUnitText.text =
                MotorCityLocalization.Text(
                    "common.kmh");
        }

        private void BuildStatus(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Activity Status",
                    new Vector2(0f, 26f),
                    new Vector2(560f, 44f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    PanelSoftColor);

            statusPanel = panel.gameObject;

            CreateAccent(
                panel,
                BlueAccent,
                new Vector2(0f, 4f),
                new Vector2(500f, 3f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f));

            statusText =
                CreateText(
                    panel,
                    "Status Text",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -2f),
                    new Vector2(530f, 32f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);
        }

        private void BuildNavigator(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Minimap",
                    new Vector2(20f, 20f),
                    new Vector2(220f, 220f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Color(
                        0.01f,
                        0.015f,
                        0.02f,
                        0.12f));

            navigatorPanel =
                panel.gameObject;

            minimapMaskSprite =
                CreateCircularMinimapSprite(
                    128);

            GameObject rimObject =
                new(
                    "Minimap Rim",
                    typeof(RectTransform),
                    typeof(Image));

            rimObject.transform.SetParent(
                panel,
                false);

            RectTransform rimRect =
                rimObject.GetComponent<RectTransform>();

            rimRect.anchorMin =
                new Vector2(0.5f, 1f);

            rimRect.anchorMax =
                new Vector2(0.5f, 1f);

            rimRect.pivot =
                new Vector2(0.5f, 1f);

            rimRect.anchoredPosition =
                new Vector2(0f, -8f);

            rimRect.sizeDelta =
                new Vector2(188f, 188f);

            Image rimImage =
                rimObject.GetComponent<Image>();

            rimImage.sprite =
                minimapMaskSprite;

            rimImage.color =
                new Color(
                    0.025f,
                    0.035f,
                    0.05f,
                    0.92f);

            rimImage.raycastTarget =
                false;

            GameObject viewportObject =
                new(
                    "Minimap Viewport",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Mask));

            viewportObject.transform.SetParent(
                panel,
                false);

            RectTransform viewportRect =
                viewportObject.GetComponent<RectTransform>();

            viewportRect.anchorMin =
                new Vector2(0.5f, 1f);

            viewportRect.anchorMax =
                new Vector2(0.5f, 1f);

            viewportRect.pivot =
                new Vector2(0.5f, 1f);

            viewportRect.anchoredPosition =
                new Vector2(0f, -13f);

            viewportRect.sizeDelta =
                new Vector2(178f, 178f);

            Image viewportImage =
                viewportObject.GetComponent<Image>();

            viewportImage.sprite =
                minimapMaskSprite;

            viewportImage.color =
                Color.white;

            viewportImage.raycastTarget =
                false;

            Mask viewportMask =
                viewportObject.GetComponent<Mask>();

            viewportMask.showMaskGraphic =
                false;

            GameObject mapObject =
                new(
                    "Minimap View",
                    typeof(RectTransform),
                    typeof(RawImage));

            mapObject.transform.SetParent(
                viewportRect,
                false);

            RectTransform mapRect =
                mapObject.GetComponent<RectTransform>();

            mapRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            mapRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            mapRect.pivot =
                new Vector2(0.5f, 0.5f);

            mapRect.anchoredPosition =
                Vector2.zero;

            mapRect.sizeDelta =
                new Vector2(258f, 258f);

            minimapImage =
                mapObject.GetComponent<RawImage>();

            minimapImage.raycastTarget =
                false;

            schematicMap =
                new CitySchematicMap();

            if (schematicMap.Build())
            {
                minimapImage.texture =
                    schematicMap.Texture;
            }
            else
            {
                minimapImage.color =
                    new Color(
                        0.12f,
                        0.14f,
                        0.15f,
                        1f);
            }

            for (int i = 0;
                 i < minimapRouteDots.Length;
                 i++)
            {
                GameObject dot =
                    new(
                        "Route Dot " + i,
                        typeof(RectTransform),
                        typeof(Image));

                dot.transform.SetParent(
                    viewportRect,
                    false);

                RectTransform dotRect =
                    dot.GetComponent<RectTransform>();

                dotRect.anchorMin =
                    new Vector2(0.5f, 0.5f);
                dotRect.anchorMax =
                    new Vector2(0.5f, 0.5f);
                dotRect.pivot =
                    new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta =
                    new Vector2(5f, 5f);

                Image dotImage =
                    dot.GetComponent<Image>();

                dotImage.color =
                    new Color(
                        0.18f,
                        0.76f,
                        1f,
                        0.88f);
                dotImage.raycastTarget =
                    false;

                dot.SetActive(false);
                minimapRouteDots[i] =
                    dotRect;
            }

            Text playerArrow =
                CreateText(
                    viewportRect,
                    "Minimap Player",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(34f, 34f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        0.20f,
                        0.62f,
                        1f,
                        1f));

            playerArrow.text =
                "▲";

            minimapPlayerArrow =
                playerArrow.rectTransform;

            Text targetBlip =
                CreateText(
                    viewportRect,
                    "Minimap Target",
                    24,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(34f, 34f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        1f,
                        0.78f,
                        0.18f,
                        1f));

            targetBlip.text =
                "●";

            minimapTargetBlip =
                targetBlip.rectTransform;

            minimapTargetText =
                CreateText(
                    panel,
                    "Minimap Target Label",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(10f, 8f),
                    new Vector2(160f, 24f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    TextColor);

            GameObject navigatorButtonObject =
                new(
                    "Navigator Button",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            navigatorButtonObject.transform.SetParent(
                panel,
                false);

            RectTransform navigatorButtonRect =
                navigatorButtonObject.GetComponent<RectTransform>();

            navigatorButtonRect.anchorMin =
                new Vector2(1f, 0f);
            navigatorButtonRect.anchorMax =
                new Vector2(1f, 0f);
            navigatorButtonRect.pivot =
                new Vector2(1f, 0f);
            navigatorButtonRect.anchoredPosition =
                new Vector2(-8f, 6f);
            navigatorButtonRect.sizeDelta =
                new Vector2(42f, 28f);

            Image navigatorButtonImage =
                navigatorButtonObject.GetComponent<Image>();

            navigatorButtonImage.color =
                new Color(
                    0.08f,
                    0.34f,
                    0.58f,
                    0.92f);

            Button navigatorButton =
                navigatorButtonObject.GetComponent<Button>();

            navigatorButton.onClick.AddListener(
                ToggleNavigatorMenu);

            Text navigatorButtonLabel =
                CreateText(
                    navigatorButtonRect,
                    "Navigator Button Label",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(40f, 26f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            navigatorButtonLabel.text =
                "M";
        }

        private void UpdateNavigator(
            bool garageOpen)
        {
            if (navigatorPanel == null ||
                car == null)
            {
                return;
            }

            if (garageOpen)
            {
                navigatorPanel.SetActive(false);
                return;
            }

            navigatorPanel.SetActive(true);

            Vector3 carPosition =
                car.transform.position;

            const float worldRadius =
                88f;

            if (schematicMap != null &&
                schematicMap.IsValid &&
                minimapImage != null)
            {
                minimapImage.uvRect =
                    schematicMap.UvWindow(
                        carPosition,
                        worldRadius);
            }

            float yaw =
                car.transform.eulerAngles.y;

            if (minimapImage != null)
            {
                minimapImage.rectTransform.localEulerAngles =
                    new Vector3(
                        0f,
                        0f,
                        yaw);
            }

            if (minimapPlayerArrow != null)
            {
                minimapPlayerArrow.localEulerAngles =
                    Vector3.zero;
            }

            ResolveMinimapTarget(
                out Vector3 target,
                out string label,
                out bool hasTarget,
                out bool showRoadRoute);

            if (!hasTarget)
            {
                if (minimapTargetBlip != null)
                    minimapTargetBlip.gameObject.SetActive(false);

                if (minimapTargetText != null)
                    minimapTargetText.text = string.Empty;

                HideRouteDots();
                return;
            }

            Vector3 delta =
                target -
                carPosition;

            delta.y = 0f;

            float distance =
                delta.magnitude;

            Vector3 local =
                Quaternion.Euler(
                    0f,
                    -yaw,
                    0f) *
                delta;

            const float markerRadius =
                78f;

            float mapScale =
                markerRadius /
                worldRadius;

            Vector2 mapOffset =
                new Vector2(
                    local.x * mapScale,
                    local.z * mapScale);

            if (mapOffset.sqrMagnitude >
                markerRadius * markerRadius)
            {
                mapOffset =
                    mapOffset.normalized *
                    markerRadius;
            }

            UpdateRoadRoute(
                carPosition,
                target,
                yaw,
                worldRadius,
                showRoadRoute);

            if (minimapTargetBlip != null)
            {
                minimapTargetBlip.gameObject.SetActive(true);

                minimapTargetBlip.anchoredPosition =
                    mapOffset;
            }

            if (minimapTargetText != null)
            {
                minimapTargetText.text =
                    MotorCityLocalization.Format("hud.distance", label, Mathf.RoundToInt(distance));
            }
        }

        private void ResolveMinimapTarget(
            out Vector3 target,
            out string label,
            out bool hasTarget,
            out bool showRoadRoute)
        {
            hasTarget = true;
            showRoadRoute = false;

            if (activityManager != null &&
                !activityManager.IsBusy &&
                onboarding != null &&
                !onboarding.IsComplete)
            {
                if (onboarding.CurrentStep == 4 &&
                    delivery != null)
                {
                    target =
                        delivery.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.first_activity_target");
                    showRoadRoute = true;
                    return;
                }

                if (onboarding.CurrentStep == 5 &&
                    garage != null)
                {
                    target =
                        garage.GarageCenter;
                    label =
                        MotorCityLocalization.Text(
                            "hud.garage");
                    showRoadRoute = true;
                    return;
                }
            }

            if ((activityManager == null ||
                 !activityManager.IsBusy) &&
                manualNavigationActive)
            {
                target =
                    manualNavigationTarget;
                label =
                    manualNavigationLabel;
                showRoadRoute =
                    true;

                if (FlatDistance(
                        car.transform.position,
                        manualNavigationTarget) <=
                    16f)
                {
                    manualNavigationActive =
                        false;
                }
                else
                {
                    return;
                }
            }

            if (activityManager != null &&
                !activityManager.IsBusy &&
                (onboarding == null ||
                 onboarding.IsComplete) &&
                story != null &&
                !story.IsComplete)
            {
                string required =
                    story.RequiredActivityId;

                if (required == "delivery" &&
                    delivery != null)
                {
                    target =
                        delivery.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.story_delivery_target");
                    showRoadRoute = true;
                    return;
                }

                if (required == "drift" &&
                    driftChallenge != null)
                {
                    target =
                        driftChallenge.ZoneCenter;
                    label =
                        MotorCityLocalization.Text(
                            "hud.story_drift_target");
                    showRoadRoute = true;
                    return;
                }

                if (required == "sprint" &&
                    streetSprint != null)
                {
                    target =
                        streetSprint.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.story_sprint_target");
                    showRoadRoute = true;
                    return;
                }

                if (required == "circuit" &&
                    circuitRace != null)
                {
                    target =
                        circuitRace.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.story_circuit_target");
                    showRoadRoute = true;
                    return;
                }
            }

            if (underground != null &&
                (underground.HasActiveInvitation ||
                 underground.IsActive ||
                 underground.IsCountingDown))
            {
                target =
                    underground.CurrentTarget;
                label =
                    underground.IsActive ||
                    underground.IsCountingDown
                        ? MotorCityLocalization.Text(
                            "hud.underground")
                        : MotorCityLocalization.Text(
                            "hud.secret_meeting");
                showRoadRoute = true;
                return;
            }

            if (towTruck != null &&
                towTruck.IsActive)
            {
                target =
                    towTruck.CurrentTarget;

                label =
                    MotorCityLocalization.Text(
                        "tow.title");
                showRoadRoute = true;

                return;
            }

            if (carWash != null &&
                carWash.IsActive)
            {
                target =
                    carWash.CurrentTarget;

                label =
                    MotorCityLocalization.Text(
                        "carwash.title");
                showRoadRoute = true;

                return;
            }

            if (professions != null &&
                professions.IsActive)
            {
                target =
                    professions.CurrentTarget;

                label =
                    professions.CurrentLabel;
                showRoadRoute = true;

                return;
            }

            if (delivery != null &&
                (delivery.IsActive ||
                 delivery.IsCountingDown))
            {
                target =
                    delivery.CurrentTarget;
                label =
                    MotorCityLocalization.Text(
                        "activity.delivery");
                showRoadRoute = true;
                return;
            }

            if (streetSprint != null &&
                (streetSprint.IsActive ||
                 streetSprint.IsCountingDown))
            {
                target =
                    streetSprint.CurrentTarget;
                label =
                    MotorCityLocalization.Text(
                        "hud.sprint");
                showRoadRoute = true;
                return;
            }

            if (circuitRace != null &&
                (circuitRace.IsActive ||
                 circuitRace.IsCountingDown))
            {
                target =
                    circuitRace.CurrentTarget;
                label =
                    MotorCityLocalization.Format("hud.circuit_lap", circuitRace.CurrentLap, circuitRace.LapCount);
                showRoadRoute = true;
                return;
            }

            if (driftChallenge != null &&
                (driftChallenge.IsActive ||
                 driftChallenge.IsCountingDown))
            {
                target =
                    driftChallenge.ZoneCenter;
                label =
                    MotorCityLocalization.Text(
                        "activity.drift");
                showRoadRoute = true;
                return;
            }

            ResolveNearestFreeRoamTarget(
                out target,
                out label);

            hasTarget =
                target !=
                car.transform.position;
        }

        private void ResolveNearestFreeRoamTarget(
            out Vector3 target,
            out string label)
        {
            target =
                car.transform.position;
            label =
                MotorCityLocalization.Text("hud.free_drive");

            float bestDistance =
                float.PositiveInfinity;

            ConsiderNavigationTarget(
                delivery != null
                    ? delivery.CurrentTarget
                    : Vector3.zero,
                MotorCityLocalization.Text("activity.delivery"),
                delivery != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                driftChallenge != null
                    ? driftChallenge.ZoneCenter
                    : Vector3.zero,
                MotorCityLocalization.Text("activity.drift"),
                driftChallenge != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                streetSprint != null
                    ? streetSprint.CurrentTarget
                    : Vector3.zero,
                MotorCityLocalization.Text("hud.sprint"),
                streetSprint != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                circuitRace != null
                    ? circuitRace.CurrentTarget
                    : Vector3.zero,
                MotorCityLocalization.Text("hud.circuit"),
                circuitRace != null,
                ref target,
                ref label,
                ref bestDistance);

            if (speedTraps != null)
            {
                for (int i = 0;
                     i < speedTraps.TrapCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        speedTraps.GetTrapPosition(i),
                        MotorCityLocalization.Text("hud.radar"),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            if (driftSpots != null)
            {
                for (int i = 0;
                     i < driftSpots.SpotCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        driftSpots.GetSpotPosition(i),
                        MotorCityLocalization.Text("hud.drift_spot"),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            if (discoveries != null)
            {
                for (int i = 0;
                     i < discoveries.DiscoveryCount;
                     i++)
                {
                    if (discoveries.IsFound(i))
                        continue;

                    ConsiderNavigationTarget(
                        discoveries.GetDiscoveryPosition(i),
                        MotorCityLocalization.Text("hud.discovery"),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            if (stuntJumps != null)
            {
                for (int i = 0;
                     i < stuntJumps.JumpCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        stuntJumps.GetJumpPosition(i),
                        MotorCityLocalization.Text("hud.stunt"),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            if (towTruck != null)
            {
                ConsiderNavigationTarget(
                    towTruck.StartPoint,
                    MotorCityLocalization.Text(
                        "tow.title"),
                    true,
                    ref target,
                    ref label,
                    ref bestDistance);
            }

            if (carWash != null)
            {
                ConsiderNavigationTarget(
                    carWash.StartPoint,
                    MotorCityLocalization.Text(
                        "carwash.title"),
                    true,
                    ref target,
                    ref label,
                    ref bestDistance);
            }

            if (professions != null)
            {
                for (int i = 0;
                     i < professions.StartCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        professions.GetStartPoint(i),
                        professions.GetStartName(i),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            ConsiderNavigationTarget(
                garage != null
                    ? garage.GarageCenter
                    : Vector3.zero,
                MotorCityLocalization.Text("hud.garage"),
                garage != null,
                ref target,
                ref label,
                ref bestDistance);
        }

        private void ConsiderNavigationTarget(
            Vector3 candidate,
            string candidateLabel,
            bool valid,
            ref Vector3 target,
            ref string label,
            ref float bestDistance)
        {
            if (!valid)
                return;

            Vector3 delta =
                candidate -
                car.transform.position;

            delta.y = 0f;

            float distance =
                delta.sqrMagnitude;

            if (distance >= bestDistance)
                return;

            bestDistance =
                distance;
            target =
                candidate;
            label =
                candidateLabel;
        }

        private static string DirectionArrow(
            float signedAngle)
        {
            float absolute =
                Mathf.Abs(
                    signedAngle);

            if (absolute <= 20f)
                return "↑";

            if (absolute >= 160f)
                return "↓";

            if (signedAngle > 0f)
                return absolute <= 70f
                    ? "↗"
                    : "→";

            return absolute <= 70f
                ? "↖"
                : "←";
        }

        private void BuildDriftPanel(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Drift HUD",
                    new Vector2(0f, -76f),
                    new Vector2(340f, 54f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Color(
                        0.10f,
                        0.055f,
                        0.025f,
                        0.92f));

            driftPanel = panel.gameObject;

            CreateAccent(
                panel,
                DriftAccent,
                new Vector2(0f, -4f),
                new Vector2(296f, 4f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f));

            driftText =
                CreateText(
                    panel,
                    "Drift Score",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -2f),
                    new Vector2(310f, 38f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);
        }

        private void BuildActivityResult(Transform canvas)
        {
            activityResultOverlay =
                new GameObject(
                    "Activity Result Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            activityResultOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                activityResultOverlay.GetComponent<RectTransform>();

            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            Image backdrop =
                activityResultOverlay.GetComponent<Image>();

            backdrop.color =
                new Color(
                    0.005f,
                    0.008f,
                    0.014f,
                    0.72f);

            backdrop.raycastTarget = false;

            RectTransform panel =
                CreatePanel(
                    activityResultOverlay.transform,
                    "Activity Result",
                    Vector2.zero,
                    new Vector2(620f, 330f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        0.02f,
                        0.028f,
                        0.042f,
                        0.98f));

            CreateAccent(
                panel,
                BlueAccent,
                new Vector2(0f, -5f),
                new Vector2(550f, 5f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f));

            resultTitleText =
                CreateText(
                    panel,
                    "Result Title",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -34f),
                    new Vector2(540f, 28f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            resultHeadlineText =
                CreateText(
                    panel,
                    "Result Headline",
                    34,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -84f),
                    new Vector2(550f, 52f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    TextColor);

            resultDetailsText =
                CreateText(
                    panel,
                    "Result Details",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -150f),
                    new Vector2(550f, 52f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            resultRewardText =
                CreateText(
                    panel,
                    "Result Reward",
                    28,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -210f),
                    new Vector2(500f, 42f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    TextColor);

            resultControlsText =
                CreateText(
                    panel,
                    "Result Controls",
                    15,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 30f),
                    new Vector2(540f, 28f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    SecondaryTextColor);

            resultControlsText.text =
                MotorCityLocalization.Text("hud.result_controls");
        }

        private void UpdateActivityResult()
        {
            if (activityManager == null ||
                !activityManager.HasResult)
                return;

            resultTitleText.text =
                activityManager.ResultTitle ?? string.Empty;

            resultHeadlineText.text =
                activityManager.ResultHeadline ?? string.Empty;

            resultDetailsText.text =
                activityManager.ResultDetails ?? string.Empty;

            bool hasCredits =
                activityManager.ResultRewardCredits > 0;

            bool hasReputation =
                activityManager.ResultReputationReward > 0;

            resultRewardText.text =
                hasCredits || hasReputation
                    ? MotorCityLocalization.Format(
                        "hud.result_reward",
                        activityManager.ResultRewardCredits,
                        activityManager.ResultReputationReward)
                    : MotorCityLocalization.Text(
                        "hud.no_rewards");

            Color accent =
                activityManager.ResultSuccess
                    ? new Color(0.20f, 1f, 0.58f, 1f)
                    : new Color(1f, 0.38f, 0.22f, 1f);

            resultHeadlineText.color = accent;
            resultRewardText.color =
                activityManager.ResultRewardCredits > 0 ||
                activityManager.ResultReputationReward > 0
                    ? accent
                    : SecondaryTextColor;
        }

        private void HandleActivityResultInput()
        {
            if (activityManager == null ||
                !activityManager.HasResult)
                return;

            if (MotorCityInput.CancelPressed)
            {
                activityManager.DismissResult();
                car?.SetDrivingEnabled(true);
                return;
            }

            bool restart =
                MotorCityInput.RetryPressed;

            if (!restart)
                return;

            switch (activityManager.ResultActivityId)
            {
                case "delivery":
                    delivery?.RestartFromResult();
                    break;

                case "drift":
                    driftChallenge?.RestartFromResult();
                    break;

                case "sprint":
                    streetSprint?.RestartFromResult();
                    break;

                case "circuit":
                    circuitRace?.RestartFromResult();
                    break;
            }
        }

        private void BuildClubOverlay(
            Transform canvas)
        {
            clubOverlay =
                new GameObject(
                    "Club Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            clubOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                clubOverlay.GetComponent<RectTransform>();

            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            Image backdrop =
                clubOverlay.GetComponent<Image>();

            backdrop.color =
                new Color(
                    0.005f,
                    0.008f,
                    0.014f,
                    0.78f);

            backdrop.raycastTarget =
                false;

            RectTransform panel =
                CreatePanel(
                    clubOverlay.transform,
                    "Club Panel",
                    Vector2.zero,
                    new Vector2(
                        560f,
                        360f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Color(
                        0.02f,
                        0.03f,
                        0.05f,
                        0.98f));

            Text title =
                CreateText(
                    panel,
                    "Club Title",
                    25,
                    FontStyle.Bold,
                    TextAnchor.UpperCenter,
                    new Vector2(
                        0f,
                        -22f),
                    new Vector2(
                        500f,
                        36f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    TextColor);

            title.text =
                MotorCityLocalization.Text(
                    "club.title");

            clubEmblemText =
                CreateText(
                    panel,
                    "Club Emblem",
                    54,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -92f),
                    new Vector2(
                        92f,
                        92f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    BlueAccent);

            clubNameText =
                CreateText(
                    panel,
                    "Club Name",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -178f),
                    new Vector2(
                        500f,
                        34f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    TextColor);

            clubDescriptionText =
                CreateText(
                    panel,
                    "Club Description",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -220f),
                    new Vector2(
                        480f,
                        48f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    SecondaryTextColor);

            clubWeeklyText =
                CreateText(
                    panel,
                    "Club Weekly",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -270f),
                    new Vector2(
                        480f,
                        34f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Color(
                        0.24f,
                        0.88f,
                        1f,
                        1f));

            clubControlsText =
                CreateText(
                    panel,
                    "Club Controls",
                    13,
                    FontStyle.Bold,
                    TextAnchor.LowerCenter,
                    new Vector2(
                        0f,
                        18f),
                    new Vector2(
                        500f,
                        28f),
                    new Vector2(
                        0.5f,
                        0f),
                    new Vector2(
                        0.5f,
                        0f),
                    SecondaryTextColor);
        }

        private void HandleClubInput()
        {
            if (clubOverlay == null ||
                club == null)
            {
                return;
            }

            if (MotorCityInput.ToggleClubPressed)
            {
                bool open =
                    !clubOverlay.activeSelf;

                clubOverlay.SetActive(
                    open);

                if (open)
                {
                    garageOverlay?.SetActive(
                        false);

                    car?.SetDrivingEnabled(
                        false);

                    UpdateClubOverlay();
                }
                else if (activityManager == null ||
                         !activityManager.HasResult)
                {
                    car?.SetDrivingEnabled(
                        true);
                }

                return;
            }

            if (!clubOverlay.activeSelf)
                return;

            if (MotorCityInput.CancelPressed)
            {
                clubOverlay.SetActive(
                    false);

                car?.SetDrivingEnabled(
                    true);

                return;
            }

            if (MotorCityInput.PreviousVehiclePressed)
            {
                club.CycleBrowse(
                    -1);

                UpdateClubOverlay();
            }

            if (MotorCityInput.NextVehiclePressed)
            {
                club.CycleBrowse(
                    1);

                UpdateClubOverlay();
            }

            if (MotorCityInput.RetryPressed ||
                MotorCityInput.InteractPressed)
            {
                club.JoinBrowseClub();
                UpdateClubOverlay();
            }
        }

        private void UpdateClubOverlay()
        {
            if (club == null ||
                clubOverlay == null ||
                !clubOverlay.activeSelf)
            {
                return;
            }

            clubEmblemText.text =
                club.BrowseClubEmblem;

            clubNameText.text =
                club.BrowseClubName;

            clubDescriptionText.text =
                club.BrowseClubDescription;

            clubWeeklyText.text =
                club.HasClub
                    ? club.WeeklyLine
                    : MotorCityLocalization.Text(
                        "club.join_prompt");

            clubControlsText.text =
                MotorCityLocalization.Text(
                    club.HasClub
                        ? "club.controls_member"
                        : "club.controls_join");
        }

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
                new Color(0.005f, 0.008f, 0.012f, 0.68f);
            backdrop.raycastTarget = false;

            RectTransform panel =
                CreatePanel(
                    garageOverlay.transform,
                    "Garage Panel",
                    Vector2.zero,
                    new Vector2(760f, 574f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        0.02f,
                        0.026f,
                        0.038f,
                        0.98f));

            CreateAccent(
                panel,
                GarageAccent,
                new Vector2(0f, -5f),
                new Vector2(690f, 5f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f));

            Text title =
                CreateText(
                    panel,
                    "Garage Title",
                    27,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -28f),
                    new Vector2(390f, 42f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);
            title.text =
                MotorCityLocalization.Text(
                    "hud.garage_title");

            garageMoneyText =
                CreateText(
                    panel,
                    "Garage Credits",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-28f, -28f),
                    new Vector2(250f, 42f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    TextColor);

            garageVehicleText =
                CreateText(
                    panel,
                    "Garage Vehicle",
                    17,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -76f),
                    new Vector2(704f, 24f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);

            garageVehicleStatsText =
                CreateText(
                    panel,
                    "Garage Vehicle Stats",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -101f),
                    new Vector2(704f, 22f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);

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
                float y =
                    -142f - i * 96f;

                RectTransform row =
                    CreatePanel(
                        panel,
                        $"Upgrade {i + 1}",
                        new Vector2(28f, y),
                        new Vector2(704f, 82f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        PanelSoftColor);

                CreateAccent(
                    row,
                    accents[i],
                    new Vector2(5f, -7f),
                    new Vector2(4f, 62f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));

                garageTitleTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Title",
                        18,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(18f, -8f),
                        new Vector2(440f, 26f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        TextColor);

                garagePriceTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Price",
                        17,
                        FontStyle.Bold,
                        TextAnchor.UpperRight,
                        new Vector2(-16f, -8f),
                        new Vector2(210f, 26f),
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
                        new Vector2(18f, 9f),
                        new Vector2(650f, 28f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        SecondaryTextColor);
            }

            garagePassportPanel =
                CreatePanel(
                    panel,
                    "Vehicle Passport",
                    new Vector2(28f, -202f),
                    new Vector2(704f, 276f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.028f,
                        0.036f,
                        0.052f,
                        0.99f)).gameObject;

            RectTransform passportRect =
                garagePassportPanel
                    .GetComponent<RectTransform>();

            garagePassportTitleText =
                CreateText(
                    passportRect,
                    "Passport Title",
                    23,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(22f, -18f),
                    new Vector2(640f, 34f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);

            garagePassportSummaryText =
                CreateText(
                    passportRect,
                    "Passport Summary",
                    16,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(22f, -66f),
                    new Vector2(650f, 64f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(0.82f, 0.72f, 1f, 1f));

            garagePassportDisciplinesText =
                CreateText(
                    passportRect,
                    "Passport Disciplines",
                    15,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(22f, -132f),
                    new Vector2(650f, 54f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);

            garagePassportMasteryText =
                CreateText(
                    passportRect,
                    "Passport Mastery",
                    15,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(22f, -186f),
                    new Vector2(650f, 28f),
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
                    new Vector2(22f, -220f),
                    new Vector2(650f, 34f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    GarageAccent);

            garagePassportPanel.SetActive(
                false);

            garageStatusText =
                CreateText(
                    panel,
                    "Garage Status",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, 26f),
                    new Vector2(420f, 26f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    SecondaryTextColor);

            Text footer =
                CreateText(
                    panel,
                    "Garage Controls",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-28f, 26f),
                    new Vector2(320f, 26f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    SecondaryTextColor);

            footer.text =
                MotorCityLocalization.Text("hud.garage_controls") +
                "   •   " +
                MotorCityLocalization.Text("hud.passport_control");
        }

        private void UpdateGarage()
        {
            if (MotorCityInput.ToggleVehiclePassportPressed)
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
                MotorCityLocalization.Format("common.credits", garage.Credits);

            if (garageVehicleText != null)
            {
                garageVehicleText.text =
                    MotorCityLocalization.Format(
                        "hud.garage_vehicle",
                        garage.VehicleName);
            }

            if (garageVehicleStatsText != null)
            {
                garageVehicleStatsText.text =
                    garage.VehicleStatsLine;
            }

            if (garageVehicleHistoryText != null)
            {
                garageVehicleHistoryText.text =
                    vehicleHistory == null
                        ? string.Empty
                        : vehicleHistory.GarageLine;
            }

            if (garageVehicleSpecializationText != null)
            {
                garageVehicleSpecializationText.text =
                    vehicleSpecialization == null
                        ? string.Empty
                        : vehicleSpecialization.GarageLine;
            }

            if (garageCollectionText != null)
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

            for (int i = 0; i < 3; i++)
            {
                garageTitleTexts[i].text =
                    garage.GetUpgradeTitle(i);

                garagePriceTexts[i].text =
                    garage.GetUpgradePrice(i);

                garageDescriptionTexts[i].text =
                    garage.GetUpgradeDescription(i);
            }

            garageStatusText.text =
                garage.StatusText;
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

            activeNotificationTimer =
                2.8f;
        }

        private string ResolveTransientNotification()
        {
            if (onboarding != null &&
                onboarding.ShowMessage)
            {
                return
                    onboarding.StatusText;
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

            if (story != null &&
                story.ShowMessage)
            {
                return
                    story.StatusText;
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

            if (stuntJumps != null &&
                stuntJumps.ShowMessage)
            {
                return
                    stuntJumps.StatusText;
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

            if (stuntJumps != null &&
                stuntJumps.IsAttemptActive)
            {
                return
                    stuntJumps.StatusText;
            }

            return
                string.Empty;
        }

        private string ResolveObjectiveLine()
        {
            if (adventureDirector == null)
                return string.Empty;

            return
                adventureDirector.ObjectiveLine;
        }

        private void OnDestroy()
        {
            if (schematicMap != null &&
                schematicMap.Texture != null)
            {
                Destroy(
                    schematicMap.Texture);
            }

            if (minimapMaskSprite != null)
            {
                Destroy(
                    minimapMaskSprite);
            }

            if (minimapMaskTexture != null)
            {
                Destroy(
                    minimapMaskTexture);
            }
        }

        private RectTransform CreateSafeAreaRoot(
            Transform parent)
        {
            GameObject root =
                new(
                    "Safe Area",
                    typeof(RectTransform));

            root.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                root.GetComponent<RectTransform>();

            ApplySafeArea(
                rect);

            SafeAreaRuntimeUpdater updater =
                root.AddComponent<SafeAreaRuntimeUpdater>();

            updater.Bind(
                rect);

            return rect;
        }

        private static void ApplySafeArea(
            RectTransform rect)
        {
            if (rect == null ||
                Screen.width <= 0 ||
                Screen.height <= 0)
            {
                return;
            }

            Rect safe =
                Screen.safeArea;

            Vector2 min =
                safe.position;

            Vector2 max =
                safe.position +
                safe.size;

            min.x /=
                Screen.width;
            min.y /=
                Screen.height;
            max.x /=
                Screen.width;
            max.y /=
                Screen.height;

            rect.anchorMin =
                min;
            rect.anchorMax =
                max;
            rect.offsetMin =
                Vector2.zero;
            rect.offsetMax =
                Vector2.zero;
        }

        private sealed class SafeAreaRuntimeUpdater :
            MonoBehaviour
        {
            private RectTransform target;
            private Rect lastSafeArea;
            private Vector2Int lastScreen;

            public void Bind(
                RectTransform rect)
            {
                target =
                    rect;

                Refresh(
                    true);
            }

            private void Update()
            {
                Refresh(
                    false);
            }

            private void Refresh(
                bool force)
            {
                Rect currentSafeArea =
                    Screen.safeArea;

                Vector2Int currentScreen =
                    new(
                        Screen.width,
                        Screen.height);

                if (!force &&
                    currentSafeArea ==
                    lastSafeArea &&
                    currentScreen ==
                    lastScreen)
                {
                    return;
                }

                lastSafeArea =
                    currentSafeArea;

                lastScreen =
                    currentScreen;

                ApplySafeArea(
                    target);
            }
        }

        private RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot,
            Color color)
        {
            GameObject go =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image));

            go.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta = size;

            Image image =
                go.GetComponent<Image>();

            image.raycastTarget = false;
            image.color = color;

            if (panelSprite != null)
            {
                image.sprite = panelSprite;
                image.type = Image.Type.Sliced;
            }

            Outline outline =
                go.AddComponent<Outline>();

            outline.effectColor =
                new Color(
                    0.35f,
                    0.5f,
                    0.72f,
                    0.14f);

            outline.effectDistance =
                new Vector2(1f, -1f);

            outline.useGraphicAlpha = true;
            return rect;
        }

        private static void CreateAccent(
            Transform parent,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot)
        {
            GameObject go =
                new(
                    "Accent",
                    typeof(RectTransform),
                    typeof(Image));

            go.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta = size;

            Image image =
                go.GetComponent<Image>();

            image.color = color;
            image.raycastTarget = false;
        }

        private Text CreateText(
            Transform parent,
            string name,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot,
            Color color)
        {
            GameObject go =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Text));

            go.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta = size;

            Text text =
                go.GetComponent<Text>();

            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;

            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize =
                Mathf.Max(10, fontSize - 5);
            text.resizeTextMaxSize = fontSize;

            Shadow shadow =
                go.AddComponent<Shadow>();

            shadow.effectColor =
                new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance =
                new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;

            return text;
        }
    }
}
