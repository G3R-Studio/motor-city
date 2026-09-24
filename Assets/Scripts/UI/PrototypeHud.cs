using System.Collections.Generic;
using MotorCity.Gameplay;
using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Persistence;
using MotorCity.Platform;
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
        private GameObject pauseOverlay;
        private Text pauseQualityText;
        private Text pauseAudioText;
        private bool pauseMenuOpen;
        private bool audioMuted;
        private float pauseStoredTimeScale = 1f;
        private const string AudioMutedSaveKey =
            "MotorCity.Settings.AudioMuted";

        private AchievementSystem achievements;
        private AdventureDirector adventureDirector;

        private Font font;
        private Sprite panelSprite;
        private MotorCityUiThemeAssets uiThemeAssets;

        private Text moneyText;
        private Text reputationText;
        private Text upgradesText;
        private Text driveModeText;
        private Text hintText;
        private Text speedText;
        private Text speedUnitText;
        private RectTransform speedNeedle;
        private Text statusText;
        private Image statusActivityIcon;
        private Text driftText;
        private Text navigatorText;
        private Text careerText;
        private Text disciplineText;
        private Text contractText;
        private Text liveEventText;
        private Text collectionText;
        private Text legendText;
        private Text objectiveText;
        private GameObject characterPanel;
        private GameObject characterPortraitRoot;
        private Image characterPortraitImage;
        private Sprite characterPortraitVitya;
        private Sprite characterPortraitTurbo;
        private Sprite characterPortraitNika;
        private Sprite characterPortraitBublik;
        private string lastPortraitDebugId =
            string.Empty;
        private Image characterPortraitFace;
        private Image characterPortraitHair;
        private Image characterPortraitAccent;
        private Image characterPortraitLeftDetail;
        private Image characterPortraitRightDetail;
        private Text characterSourceText;
        private Text characterNameText;
        private Text characterMissionTitleText;
        private Text characterLineText;
        private Text characterRewardText;
        private RawImage minimapImage;
        private RectTransform minimapTargetBlip;
        private Image minimapTargetIcon;
        private RectTransform minimapPlayerArrow;
        private Text minimapTargetText;
        private readonly RectTransform[] minimapRouteDots =
            new RectTransform[36];
        private int visibleRouteDotCount;
        private readonly List<Vector3> fixedRoadRoute =
            new();
        private Vector3 fixedRoadRouteTarget;
        private bool fixedRoadRouteValid;
        private int fixedRoadRouteProgress;
        private const float RouteTargetChangeDistance = 8f;
        private const float RouteRebuildOffPathDistance = 32f;
        private const float RouteAdvanceDistance = 18f;
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
        private float minimapTargetResolveTimer;
        private Vector3 cachedMinimapTarget;
        private string cachedMinimapLabel = string.Empty;
        private bool cachedMinimapHasTarget;
        private bool cachedMinimapShowRoadRoute;
        private int lastMinimapDistance = int.MinValue;
        private string lastMinimapDistanceLabel = string.Empty;
        private float minimapRouteUpdateTimer;
        private float minimapDistanceUpdateTimer;
        private const float MinimapTargetResolveInterval = 0.10f;
        private const float MinimapRouteUpdateInterval = 0.05f;

        private GameObject navigatorPanel;
        private GameObject statusPanel;
        private GameObject driftPanel;
        private GameObject garageOverlay;
        private GameObject garagePassportPanel;
        private GameObject activityResultOverlay;
        private GameObject resultTouchControlsRoot;
        private GameObject resultRetryTouchButton;
        private GameObject clubOverlay;
        private Text clubEmblemText;
        private Text clubNameText;
        private Text clubDescriptionText;
        private Text clubWeeklyText;
        private Text clubControlsText;
        private RectTransform safeAreaRoot;
        private GameObject touchControlsRoot;
        private GameObject touchUtilityRoot;
        private GameObject touchActivityCancelRoot;
        private GameObject touchPauseRoot;
        private GameObject navigatorTouchControlsRoot;
        private GameObject storeTouchControlsRoot;
        private GameObject clubTouchControlsRoot;
        private GameObject garageTouchControlsRoot;
        private Text garageControlsText;
        private CanvasScaler canvasScaler;
        private bool lastPortraitLayout;
        private int lastDisplayedCredits = int.MinValue;
        private int lastDisplayedReputation = int.MinValue;
        private int lastDisplayedReputationLevel = int.MinValue;
        private int lastDisplayedSpeed = int.MinValue;
        private DriveMode? lastDisplayedDriveMode;
        private float slowHudUpdateTimer;
        private float contextualStatusUpdateTimer;
        private string cachedContextualStatus = string.Empty;
        private const float SlowHudUpdateInterval = 0.10f;
        private string lastHudLanguageCode = string.Empty;
        private bool lastHudTouchPrompts;
        private bool hudLocalizationStateInitialized;

        private readonly List<TouchLocalizedLabel> touchLocalizedLabels =
            new();

        private readonly Queue<string> notificationQueue =
            new();
        private string lastNotificationCandidate;
        private string activeNotification;
        private float activeNotificationTimer;

        private Image resultActivityIcon;
        private Image resultRewardIcon;
        private Text resultTitleText;
        private Text resultHeadlineText;
        private Text resultDetailsText;
        private Text resultRewardText;
        private Text resultControlsText;

        private Image garageHeaderIcon;
        private Image garageCreditsIcon;
        private Image garageReputationIcon;
        private Text garageMoneyText;
        private Text garageReputationText;
        private Text garageStatusText;
        private Image garageVehicleStateIcon;
        private Text garageVehicleText;
        private Text garageNextVehicleText;
        private Text garageVehicleStatsText;
        private Image garageMasteryIcon;
        private Image garageMasteryTrack;
        private Image garageMasteryFill;
        private Text garageVehicleHistoryText;
        private Text garageVehicleSpecializationText;
        private Text garageCollectionText;
        private Text garagePassportTitleText;
        private Text garagePassportSummaryText;
        private Text garagePassportDisciplinesText;
        private Text garagePassportMasteryText;
        private Text garagePassportSpecializationText;
        private bool garagePassportOpen;
        private readonly Image[] garageUpgradeIcons =
            new Image[3];
        private readonly Image[] garagePriceIcons =
            new Image[3];
        private readonly Text[] garageTitleTexts =
            new Text[3];
        private readonly Text[] garagePriceTexts =
            new Text[3];
        private readonly Text[] garageDescriptionTexts =
            new Text[3];

        // Shared Motor City racing UI palette. The values deliberately stay
        // close to the imported Ville Seppanen HUD: graphite/navy surfaces,
        // cool cyan for navigation/focus and warm orange for live driving events.
        private static readonly Color PanelColor =
            new(0.030f, 0.042f, 0.066f, 0.96f);
        private static readonly Color PanelSoftColor =
            new(0.050f, 0.064f, 0.094f, 0.92f);
        private static readonly Color TextColor =
            new(0.98f, 0.985f, 1f, 1f);
        private static readonly Color SecondaryTextColor =
            new(0.70f, 0.76f, 0.86f, 1f);
        private static readonly Color BlueAccent =
            new(0.18f, 0.72f, 1f, 1f);
        private static readonly Color DriftAccent =
            new(1f, 0.48f, 0.13f, 1f);
        private static readonly Color GarageAccent =
            new(0.56f, 0.48f, 1f, 1f);

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

        private static void SetActiveIfChanged(
            GameObject target,
            bool active)
        {
            if (target != null &&
                target.activeSelf != active)
            {
                target.SetActive(
                    active);
            }
        }

        private void RefreshHudLocalizationState()
        {
            string languageCode =
                MotorCityLocalization.LanguageCode;

            bool touchPrompts =
                MotorCityInput.PreferTouchPrompts;

            if (hudLocalizationStateInitialized &&
                string.Equals(
                    lastHudLanguageCode,
                    languageCode,
                    System.StringComparison.Ordinal) &&
                lastHudTouchPrompts ==
                    touchPrompts)
            {
                return;
            }

            hudLocalizationStateInitialized =
                true;

            lastHudLanguageCode =
                languageCode;

            lastHudTouchPrompts =
                touchPrompts;

            lastDisplayedCredits =
                int.MinValue;

            lastDisplayedReputation =
                int.MinValue;

            lastDisplayedReputationLevel =
                int.MinValue;

            lastDisplayedDriveMode =
                null;

            lastMinimapDistance =
                int.MinValue;

            lastMinimapDistanceLabel =
                string.Empty;

            cachedMinimapLabel =
                string.Empty;

            cachedContextualStatus =
                string.Empty;

            slowHudUpdateTimer =
                0f;

            contextualStatusUpdateTimer =
                0f;

            minimapTargetResolveTimer =
                0f;

            RefreshTouchLocalizedLabels();
        }

        private void RefreshTouchLocalizedLabels()
        {
            foreach (TouchLocalizedLabel binding in
                     touchLocalizedLabels)
            {
                if (binding.Text == null)
                    continue;

                binding.Text.text =
                    MotorCityLocalization.Text(
                        binding.LocalizationKey);
            }
        }

        private void Update()
        {
            if (moneyText == null)
                return;

            bool hadBlockingModal =
                HasBlockingModalUi();

            if (pauseMenuOpen)
            {
                HandlePauseMenuInput();
                return;
            }

            HandleNavigatorMenu();
            HandleStoreInput();
            HandleClubInput();

            if (MotorCityInput.CancelPressed &&
                !hadBlockingModal &&
                !HasBlockingModalUi())
            {
                OpenPauseMenu();
                return;
            }
            UpdateTouchControlsVisibility();
            RefreshHudLocalizationState();

            if (MotorCityInput.RewardedBonusPressed)
            {
                rewardedBonus?.TryShow();
            }

            slowHudUpdateTimer -=
                Time.unscaledDeltaTime;

            if (slowHudUpdateTimer <= 0f)
            {
                slowHudUpdateTimer =
                    SlowHudUpdateInterval;

                int credits =
                    wallet == null
                        ? 0
                        : wallet.Credits;
    
                if (credits !=
                    lastDisplayedCredits)
                {
                    lastDisplayedCredits =
                        credits;
    
                    moneyText.text =
                        MotorCityLocalization.Format(
                            "common.credits",
                            credits);
                }
    
                if (reputationText != null &&
                    activityManager != null)
                {
                    int totalReputation =
                        activityManager.TotalReputation;
    
                    int reputationLevel =
                        activityManager.ReputationLevel;
    
                    if (totalReputation !=
                            lastDisplayedReputation ||
                        reputationLevel !=
                            lastDisplayedReputationLevel)
                    {
                        lastDisplayedReputation =
                            totalReputation;
    
                        lastDisplayedReputationLevel =
                            reputationLevel;
    
                        reputationText.text =
                            MotorCityLocalization.Format(
                                "hud.rep",
                                totalReputation,
                                reputationLevel);
                    }
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
                    car != null &&
                    (!lastDisplayedDriveMode.HasValue ||
                     lastDisplayedDriveMode.Value !=
                        car.CurrentDriveMode))
                {
                    lastDisplayedDriveMode =
                        car.CurrentDriveMode;
    
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
    
    
            }

            float speed =
                car == null
                    ? 0f
                    : car.SpeedKph;

            int roundedSpeed =
                Mathf.RoundToInt(
                    speed);

            if (roundedSpeed !=
                lastDisplayedSpeed)
            {
                lastDisplayedSpeed =
                    roundedSpeed;

                speedText.text =
                    roundedSpeed.ToString(
                        "000");
            }

            if (speedNeedle != null)
            {
                float normalizedSpeed =
                    Mathf.Clamp01(
                        speed /
                        240f);

                float needleAngle =
                    Mathf.Lerp(
                        135f,
                        -135f,
                        normalizedSpeed);

                speedNeedle.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        needleAngle);
            }

            bool resultOpen =
                activityManager != null &&
                activityManager.HasResult;

            SetActiveIfChanged(
                activityResultOverlay,
                resultOpen);

            if (resultOpen)
            {
                if (navigatorMenuOpen)
                {
                    CloseNavigatorMenuVisualOnly();
                }

                storeOpen =
                    false;

                if (clubOverlay != null &&
                    clubOverlay.activeSelf)
                {
                    clubOverlay.SetActive(
                        false);
                }

                SetActiveIfChanged(
                    statusPanel,
                    false);
                SetActiveIfChanged(
                    driftPanel,
                    false);
                SetActiveIfChanged(
                    garageOverlay,
                    false);
                SetActiveIfChanged(
                    navigatorPanel,
                    false);
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
                SetActiveIfChanged(
                    garageOverlay,
                    false);
                SetActiveIfChanged(
                    navigatorPanel,
                    false);
                SetActiveIfChanged(
                    driftPanel,
                    false);
                statusPanel.SetActive(true);
                statusText.text =
                    MotorCityLocalization.Format(
                        "store.status",
                        MotorCityLocalization.Text(
                            "store.title") +
                        " • " +
                        cosmeticStore.SelectedName +
                        "   •   " +
                        MotorCityLocalization.Format(
                            "common.credits",
                            wallet != null
                                ? wallet.Credits
                                : 0) +
                        "   •   " +
                        MotorCityLocalization.Format(
                            "hud.rep",
                            activityManager != null
                                ? activityManager.TotalReputation
                                : 0,
                            activityManager != null
                                ? activityManager.ReputationLevel
                                : 1),
                        cosmeticStore.SelectedDescription,
                        cosmeticStore.SeasonPathLine +
                        " • " +
                        cosmeticStore.SelectedOwnershipLine,
                        MotorCityLocalization.Text(
                            "store.controls"));
                return;
            }

            UpdateNotificationQueue();

            string status;

            if (!string.IsNullOrWhiteSpace(
                    activeNotification))
            {
                status =
                    activeNotification;
            }
            else
            {
                contextualStatusUpdateTimer -=
                    Time.unscaledDeltaTime;

                if (contextualStatusUpdateTimer <= 0f)
                {
                    contextualStatusUpdateTimer =
                        SlowHudUpdateInterval;

                    cachedContextualStatus =
                        ResolveContextualStatus();
                }

                status =
                    cachedContextualStatus;
            }

            SetActiveIfChanged(
                statusPanel,
                !string.IsNullOrWhiteSpace(
                    status));

            if (statusPanel.activeSelf)
            {
                statusText.text = status;
                RefreshStatusActivityIcon();
            }

            bool showDrift =
                drift != null &&
                (drift.IsDrifting ||
                 drift.CurrentScore > 0 ||
                 drift.ShowRewardMessage);

            SetActiveIfChanged(
                driftPanel,
                showDrift);

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

            SetActiveIfChanged(
                garageOverlay,
                garageOpen);

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
            {
                SetActiveIfChanged(
                    statusPanel,
                    false);

                SetActiveIfChanged(
                    driftPanel,
                    false);

                UpdateGarage();
            }

            UpdateNavigator(
                garageOpen);
        }

        private bool HasBlockingModalUi()
        {
            return
                navigatorMenuOpen ||
                storeOpen ||
                (clubOverlay != null &&
                 clubOverlay.activeSelf) ||
                (garage != null &&
                 garage.IsOpen) ||
                (activityManager != null &&
                 activityManager.HasResult);
        }

        private void RefreshDrivingEnabledForUi()
        {
            car?.SetDrivingEnabled(
                !HasBlockingModalUi());
        }

        private void CloseNavigatorMenuVisualOnly()
        {
            navigatorMenuOpen =
                false;

            navigatorMenuOverlay?.SetActive(
                false);
        }

        private void HandleStoreInput()
        {
            if (cosmeticStore == null)
                return;

            if (MotorCityInput.ToggleStorePressed)
            {
                bool opening =
                    !storeOpen;

                if (opening &&
                    ((activityManager != null &&
                      (activityManager.IsBusy ||
                       activityManager.HasResult)) ||
                     (garage != null &&
                      garage.IsOpen)))
                {
                    return;
                }

                storeOpen =
                    opening;

                if (storeOpen)
                {
                    CloseNavigatorMenuVisualOnly();

                    if (clubOverlay != null)
                    {
                        clubOverlay.SetActive(
                            false);
                    }
                }

                RefreshDrivingEnabledForUi();
            }

            if (!storeOpen)
                return;

            if (MotorCityInput.CancelPressed)
            {
                storeOpen =
                    false;

                RefreshDrivingEnabledForUi();
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
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.985f));

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

            if (activityManager != null &&
                activityManager.IsBusy)
            {
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

            RefreshDrivingEnabledForUi();

            UpdateNavigatorMenuText();
        }

        private void CloseNavigatorMenu()
        {
            CloseNavigatorMenuVisualOnly();
            RefreshDrivingEnabledForUi();
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

        private float MinimapWorldScale(
            float worldRadius)
        {
            if (minimapImage == null ||
                worldRadius <= 0.01f)
            {
                return 1f;
            }

            float diameter =
                worldRadius * 2f;

            return
                minimapImage.rectTransform.rect.width /
                diameter;
        }

        private void UpdateRoadRoute(
            Vector3 carPosition,
            Vector3 target,
            float yaw,
            float worldRadius,
            bool showRoadRoute)
        {
            if (!showRoadRoute ||
                minimapRouteDots.Length == 0)
            {
                minimapRouteUpdateTimer =
                    0f;
                minimapDistanceUpdateTimer =
                    0f;

                HideRouteDots();
                ClearFixedRoadRoute();
                return;
            }

            EnsureFixedRoadRoute(
                carPosition,
                target);

            if (!fixedRoadRouteValid ||
                fixedRoadRoute.Count < 2)
            {
                return;
            }

            AdvanceFixedRoadRouteProgress(
                carPosition);

            const float markerRadius =
                74f;

            float mapScale =
                MinimapWorldScale(
                    worldRadius);

            int placed =
                0;

            bool reachedEdge =
                false;

            int firstSegment =
                Mathf.Clamp(
                    fixedRoadRouteProgress,
                    0,
                    fixedRoadRoute.Count - 2);

            for (int segment = firstSegment;
                 segment < fixedRoadRoute.Count - 1 &&
                 placed < minimapRouteDots.Length &&
                 !reachedEdge;
                 segment++)
            {
                Vector3 a =
                    fixedRoadRoute[segment];

                Vector3 b =
                    fixedRoadRoute[segment + 1];

                if (segment == firstSegment)
                {
                    a =
                        ClosestPointOnFlatSegment(
                            carPosition,
                            a,
                            b);
                }

                float length =
                    FlatDistance(
                        a,
                        b);

                int steps =
                    Mathf.Max(
                        1,
                        Mathf.CeilToInt(
                            length /
                            12f));

                for (int step = 0;
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

                    if (!dot.gameObject.activeSelf)
                    {
                        dot.gameObject.SetActive(
                            true);
                    }

                    dot.anchoredPosition =
                        offset;

                    if (reachedEdge)
                        break;
                }
            }

            for (int i = placed;
                 i < visibleRouteDotCount &&
                 i < minimapRouteDots.Length;
                 i++)
            {
                RectTransform dot =
                    minimapRouteDots[i];

                if (dot != null &&
                    dot.gameObject.activeSelf)
                {
                    dot.gameObject.SetActive(
                        false);
                }
            }

            visibleRouteDotCount =
                placed;
        }

        private void EnsureFixedRoadRoute(
            Vector3 carPosition,
            Vector3 target)
        {
            bool targetChanged =
                !fixedRoadRouteValid ||
                FlatDistance(
                    fixedRoadRouteTarget,
                    target) >
                RouteTargetChangeDistance;

            bool offRoute =
                fixedRoadRouteValid &&
                DistanceToRemainingRoute(
                    carPosition) >
                RouteRebuildOffPathDistance;

            if (!targetChanged &&
                !offRoute)
            {
                return;
            }

            List<Vector3> route =
                CityRoadNavigator.BuildRoute(
                    carPosition,
                    target);

            fixedRoadRoute.Clear();

            if (route == null ||
                route.Count < 2)
            {
                fixedRoadRouteValid =
                    false;

                return;
            }

            foreach (Vector3 point in route)
            {
                if (fixedRoadRoute.Count == 0 ||
                    FlatDistance(
                        fixedRoadRoute[
                            fixedRoadRoute.Count - 1],
                        point) >
                    1.5f)
                {
                    fixedRoadRoute.Add(
                        point);
                }
            }

            fixedRoadRouteTarget =
                target;

            fixedRoadRouteProgress =
                0;

            fixedRoadRouteValid =
                fixedRoadRoute.Count >= 2;
        }

        private void AdvanceFixedRoadRouteProgress(
            Vector3 carPosition)
        {
            if (!fixedRoadRouteValid ||
                fixedRoadRoute.Count < 2)
            {
                return;
            }

            int maxSegment =
                fixedRoadRoute.Count - 2;

            int searchEnd =
                Mathf.Min(
                    maxSegment,
                    fixedRoadRouteProgress + 6);

            int bestSegment =
                fixedRoadRouteProgress;

            float bestDistance =
                float.PositiveInfinity;

            for (int segment =
                     fixedRoadRouteProgress;
                 segment <= searchEnd;
                 segment++)
            {
                Vector3 closest =
                    ClosestPointOnFlatSegment(
                        carPosition,
                        fixedRoadRoute[segment],
                        fixedRoadRoute[segment + 1]);

                float distance =
                    FlatDistance(
                        carPosition,
                        closest);

                if (distance <
                    bestDistance)
                {
                    bestDistance =
                        distance;

                    bestSegment =
                        segment;
                }
            }

            fixedRoadRouteProgress =
                bestSegment;

            while (fixedRoadRouteProgress <
                   maxSegment &&
                   FlatDistance(
                       carPosition,
                       fixedRoadRoute[
                           fixedRoadRouteProgress + 1]) <=
                   RouteAdvanceDistance)
            {
                fixedRoadRouteProgress++;
            }
        }

        private float DistanceToRemainingRoute(
            Vector3 carPosition)
        {
            if (!fixedRoadRouteValid ||
                fixedRoadRoute.Count < 2)
            {
                return
                    float.PositiveInfinity;
            }

            float best =
                float.PositiveInfinity;

            int startSegment =
                Mathf.Clamp(
                    fixedRoadRouteProgress,
                    0,
                    fixedRoadRoute.Count - 2);

            int endSegment =
                Mathf.Min(
                    fixedRoadRoute.Count - 2,
                    startSegment + 10);

            for (int segment = startSegment;
                 segment <= endSegment;
                 segment++)
            {
                Vector3 closest =
                    ClosestPointOnFlatSegment(
                        carPosition,
                        fixedRoadRoute[segment],
                        fixedRoadRoute[segment + 1]);

                best =
                    Mathf.Min(
                        best,
                        FlatDistance(
                            carPosition,
                            closest));
            }

            return best;
        }

        private static Vector3 ClosestPointOnFlatSegment(
            Vector3 point,
            Vector3 a,
            Vector3 b)
        {
            Vector2 p =
                new(
                    point.x,
                    point.z);

            Vector2 av =
                new(
                    a.x,
                    a.z);

            Vector2 bv =
                new(
                    b.x,
                    b.z);

            Vector2 ab =
                bv -
                av;

            float lengthSquared =
                ab.sqrMagnitude;

            float t =
                lengthSquared <=
                    0.0001f
                    ? 0f
                    : Mathf.Clamp01(
                        Vector2.Dot(
                            p - av,
                            ab) /
                        lengthSquared);

            return new Vector3(
                Mathf.Lerp(
                    a.x,
                    b.x,
                    t),
                Mathf.Lerp(
                    a.y,
                    b.y,
                    t),
                Mathf.Lerp(
                    a.z,
                    b.z,
                    t));
        }

        private void ClearFixedRoadRoute()
        {
            fixedRoadRoute.Clear();

            fixedRoadRouteValid =
                false;

            fixedRoadRouteProgress =
                0;

            fixedRoadRouteTarget =
                Vector3.zero;
        }

        private void HideRouteDots()
        {
            int count =
                Mathf.Min(
                    visibleRouteDotCount,
                    minimapRouteDots.Length);

            for (int i = 0;
                 i < count;
                 i++)
            {
                RectTransform dot =
                    minimapRouteDots[i];

                if (dot != null &&
                    dot.gameObject.activeSelf)
                {
                    dot.gameObject.SetActive(
                        false);
                }
            }

            visibleRouteDotCount =
                0;
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
            EventSystem existing =
                Object.FindAnyObjectByType<EventSystem>();

            if (existing != null)
            {
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                {
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                }

                return;
            }

            GameObject eventSystemObject =
                new(
                    "Motor City UI EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));

            Object.DontDestroyOnLoad(
                eventSystemObject);
        }

        private void BuildUi()
        {
            EnsureUiEventSystem();
            MotorCityIconLibrary.PrewarmCore();

            font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            panelSprite =
                Resources.Load<Sprite>(
                    "MotorCity/UI/grey_panel");

            uiThemeAssets =
                Resources.Load<MotorCityUiThemeAssets>(
                    "MotorCity/UI/MotorCityUiThemeAssets");

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
            canvas.pixelPerfect = true;

            canvasScaler =
                canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            ApplyResponsiveCanvasScale(
                true);

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
            BuildPauseMenu(safeAreaRoot);
            BuildDriftPanel(safeAreaRoot);
            BuildActivityResult(safeAreaRoot);
            BuildResultTouchControls(safeAreaRoot);
            BuildGarage(safeAreaRoot);
            BuildClubOverlay(safeAreaRoot);
            BuildTouchControls(safeAreaRoot);
            BuildTouchUtilityControls(safeAreaRoot);
            BuildTouchActivityCancelControl(safeAreaRoot);
            BuildTouchPauseControl(safeAreaRoot);
            BuildModalTouchControls(safeAreaRoot);

            driftPanel.SetActive(false);
            activityResultOverlay.SetActive(false);
            garageOverlay.SetActive(false);
            clubOverlay.SetActive(false);
            navigatorMenuOverlay.SetActive(false);
            pauseOverlay.SetActive(false);

            audioMuted =
                MotorCitySaveService.GetInt(
                    AudioMutedSaveKey,
                    0) != 0;

            AudioListener.volume =
                audioMuted
                    ? 0f
                    : 1f;
        }

        private void BuildPauseMenu(
            Transform canvas)
        {
            pauseOverlay =
                new GameObject(
                    "Pause Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            pauseOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                pauseOverlay.GetComponent<RectTransform>();

            overlay.anchorMin =
                Vector2.zero;
            overlay.anchorMax =
                Vector2.one;
            overlay.offsetMin =
                Vector2.zero;
            overlay.offsetMax =
                Vector2.zero;

            Image backdrop =
                pauseOverlay.GetComponent<Image>();

            backdrop.color =
                new Color(
                    0.005f,
                    0.008f,
                    0.012f,
                    0.82f);

            backdrop.raycastTarget =
                true;

            RectTransform panel =
                CreatePanel(
                    pauseOverlay.transform,
                    "Pause Panel",
                    Vector2.zero,
                    new Vector2(
                        520f,
                        330f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Color(
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.985f));

            Text title =
                CreateText(
                    panel,
                    "Pause Title",
                    29,
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
                    "pause.title");

            pauseQualityText =
                CreateText(
                    panel,
                    "Pause Quality",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        45f),
                    new Vector2(
                        460f,
                        42f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    TextColor);

            pauseAudioText =
                CreateText(
                    panel,
                    "Pause Audio",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        460f,
                        42f),
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
                    "Pause Controls",
                    14,
                    FontStyle.Bold,
                    TextAnchor.LowerCenter,
                    new Vector2(
                        0f,
                        22f),
                    new Vector2(
                        470f,
                        34f),
                    new Vector2(
                        0.5f,
                        0f),
                    new Vector2(
                        0.5f,
                        0f),
                    SecondaryTextColor);

            controls.text =
                MotorCityLocalization.Text(
                    ShouldUseTouchUi()
                        ? "pause.controls_touch"
                        : "pause.controls");

            BuildPauseTouchActions(
                panel);

            RefreshPauseMenuText();
        }

        private void BuildPauseTouchActions(
            Transform panel)
        {
            if (!ShouldUseTouchUi())
                return;

            CreatePauseButton(
                panel,
                "Pause Quality Previous",
                "touch.modal.prev",
                new Vector2(-165f, -70f),
                new Vector2(96f, 44f),
                () =>
                    CycleQuality(-1));

            CreatePauseButton(
                panel,
                "Pause Audio Toggle",
                "pause.audio_touch",
                new Vector2(-55f, -70f),
                new Vector2(112f, 44f),
                () =>
                {
                    ToggleAudioMute();
                });

            CreatePauseButton(
                panel,
                "Pause Quality Next",
                "touch.modal.next",
                new Vector2(65f, -70f),
                new Vector2(96f, 44f),
                () =>
                    CycleQuality(1));

            CreatePauseButton(
                panel,
                "Pause Resume",
                "pause.resume",
                new Vector2(175f, -70f),
                new Vector2(112f, 44f),
                ClosePauseMenu);
        }

        private void CreatePauseButton(
            Transform parent,
            string objectName,
            string localizationKey,
            Vector2 anchoredPosition,
            Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject =
                new(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            buttonObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0.5f, 0.5f);

            rect.anchorMax =
                new Vector2(0.5f, 0.5f);

            rect.pivot =
                new Vector2(0.5f, 0.5f);

            rect.anchoredPosition =
                anchoredPosition;

            rect.sizeDelta =
                size;

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                PanelSoftColor;

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            button.onClick.AddListener(
                action);

            Text text =
                CreateText(
                    rect,
                    "Label",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    size -
                    new Vector2(8f, 6f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            text.text =
                MotorCityLocalization.Text(
                    localizationKey);

            touchLocalizedLabels.Add(
                new TouchLocalizedLabel(
                    text,
                    localizationKey));
        }

        private void OpenPauseMenu()
        {
            pauseMenuOpen =
                true;

            pauseStoredTimeScale =
                Time.timeScale;

            Time.timeScale =
                0f;

            pauseOverlay?.SetActive(
                true);

            touchControlsRoot?.SetActive(
                false);

            touchUtilityRoot?.SetActive(
                false);

            touchActivityCancelRoot?.SetActive(
                false);

            touchPauseRoot?.SetActive(
                false);

            car?.SetDrivingEnabled(
                false);

            RefreshPauseMenuText();
        }

        private void ClosePauseMenu()
        {
            pauseMenuOpen =
                false;

            pauseOverlay?.SetActive(
                false);

            Time.timeScale =
                pauseStoredTimeScale;

            car?.SetDrivingEnabled(
                true);

            UpdateTouchControlsVisibility();
        }

        private void HandlePauseMenuInput()
        {
            if (MotorCityInput.CancelPressed)
            {
                ClosePauseMenu();
                return;
            }

            if (MotorCityInput.PreviousVehiclePressed)
            {
                CycleQuality(
                    -1);
            }

            if (MotorCityInput.NextVehiclePressed)
            {
                CycleQuality(
                    1);
            }

            if (MotorCityInput.CycleBodyColorPressed)
            {
                ToggleAudioMute();
            }
        }

        private void ToggleAudioMute()
        {
            audioMuted =
                !audioMuted;

            AudioListener.volume =
                audioMuted
                    ? 0f
                    : 1f;

            MotorCitySaveService.SetInt(
                AudioMutedSaveKey,
                audioMuted
                    ? 1
                    : 0);

            MotorCitySaveService.Save();

            RefreshPauseMenuText();
        }

        private void CycleQuality(
            int direction)
        {
            int current =
                (int)MotorCityQualityRuntime.CurrentPreset;

            int next =
                (current + direction + 3) %
                3;

            MotorCityQualityRuntime.Apply(
                (MotorCityQualityPreset)next,
                true);

            RefreshPauseMenuText();
        }

        private void RefreshPauseMenuText()
        {
            if (pauseQualityText != null)
            {
                string quality =
                    MotorCityQualityRuntime.CurrentPreset switch
                    {
                        MotorCityQualityPreset.Low =>
                            MotorCityLocalization.Text(
                                "pause.quality_low"),

                        MotorCityQualityPreset.High =>
                            MotorCityLocalization.Text(
                                "pause.quality_high"),

                        _ =>
                            MotorCityLocalization.Text(
                                "pause.quality_medium")
                    };

                pauseQualityText.text =
                    MotorCityLocalization.Format(
                        "pause.quality",
                        quality);
            }

            if (pauseAudioText != null)
            {
                pauseAudioText.text =
                    MotorCityLocalization.Format(
                        "pause.audio",
                        MotorCityLocalization.Text(
                            audioMuted
                                ? "pause.off"
                                : "pause.on"));
            }
        }

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

        private void BuildCharacterCard(
            Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Character Card",
                    new Vector2(
                        18f,
                        -18f),
                    new Vector2(
                        342f,
                        96f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    new Color(
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.96f));

            characterPanel =
                panel.gameObject;

            CreateAccent(
                panel,
                BlueAccent,
                new Vector2(
                    4f,
                    -10f),
                new Vector2(
                    3f,
                    76f),
                new Vector2(
                    0f,
                    1f),
                new Vector2(
                    0f,
                    1f));

            RectTransform portraitFrame =
                CreatePanel(
                    panel,
                    "Character Portrait Frame",
                    new Vector2(
                        13f,
                        -12f),
                    new Vector2(
                        52f,
                        52f),
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

            characterPortraitAccent =
                CreatePortraitLayer(
                    portraitFrame,
                    "Portrait Accent",
                    new Vector2(
                        5f,
                        -5f),
                    new Vector2(
                        48f,
                        48f),
                    new Color(
                        0.15f,
                        0.55f,
                        1f,
                        0.30f));

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
                    10,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        76f,
                        -8f),
                    new Vector2(
                        150f,
                        16f),
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
                    14,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        76f,
                        -23f),
                    new Vector2(
                        238f,
                        20f),
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
                    10,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        76f,
                        -42f),
                    new Vector2(
                        238f,
                        16f),
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
                    12,
                    FontStyle.Bold,
                    TextAnchor.LowerLeft,
                    new Vector2(
                        76f,
                        18f),
                    new Vector2(
                        238f,
                        31f),
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
                    10,
                    FontStyle.Bold,
                    TextAnchor.LowerRight,
                    new Vector2(
                        -14f,
                        6f),
                    new Vector2(
                        178f,
                        15f),
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

        private Image CreateHudIcon(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Color color)
        {
            GameObject iconObject =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image));

            iconObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                iconObject.GetComponent<RectTransform>();

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
                iconObject.GetComponent<Image>();

            image.sprite =
                sprite;

            image.preserveAspect =
                true;

            image.raycastTarget =
                false;

            image.color =
                color;

            image.enabled =
                sprite != null;

            return image;
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

            if (panelSprite != null)
            {
                image.sprite =
                    panelSprite;

                image.type =
                    Image.Type.Sliced;
            }

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

            characterPortraitAccent.color =
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    0.30f);

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
                    new Vector2(0f, 12f),
                    new Vector2(264f, 224f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Color(
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.74f));

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

            if (racingFace != null)
            {
                RectTransform unitPlate =
                    CreatePanel(
                        panel,
                        "Speed Unit Plate",
                        gaugeCenter +
                            new Vector2(
                                0f,
                                31f),
                        new Vector2(
                            42f,
                            16f),
                        new Vector2(
                            0.5f,
                            0f),
                        new Vector2(
                            0.5f,
                            0.5f),
                        new Color(
                            PanelColor.r,
                            PanelColor.g,
                            PanelColor.b,
                            0.72f));

                Text dialUnit =
                    CreateText(
                        unitPlate,
                        "Dial Unit",
                        8,
                        FontStyle.Bold,
                        TextAnchor.MiddleCenter,
                        Vector2.zero,
                        new Vector2(
                            38f,
                            13f),
                        new Vector2(
                            0.5f,
                            0.5f),
                        new Vector2(
                            0.5f,
                            0.5f),
                        SecondaryTextColor);

                dialUnit.text =
                    MotorCityLocalization.Text(
                        "common.kmh");
            }

            GameObject needleObject =
                new(
                    "Speed Needle",
                    typeof(RectTransform),
                    typeof(Image));

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
                new Vector2(0.5f, 0f);
            speedNeedle.anchoredPosition =
                gaugeCenter;
            speedNeedle.sizeDelta =
                new Vector2(4f, 59f);
            speedNeedle.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    135f);

            Image needleImage =
                needleObject.GetComponent<Image>();

            needleImage.color =
                DriftAccent;
            needleImage.raycastTarget =
                false;

            GameObject hubObject =
                new(
                    "Speed Needle Hub",
                    typeof(RectTransform),
                    typeof(Image));

            hubObject.transform.SetParent(
                panel,
                false);

            RectTransform hub =
                hubObject.GetComponent<RectTransform>();

            hub.anchorMin =
                new Vector2(0.5f, 0f);
            hub.anchorMax =
                new Vector2(0.5f, 0f);
            hub.pivot =
                new Vector2(0.5f, 0.5f);
            hub.anchoredPosition =
                gaugeCenter;
            hub.sizeDelta =
                new Vector2(13f, 13f);

            Image hubImage =
                hubObject.GetComponent<Image>();

            hubImage.color =
                TextColor;
            hubImage.raycastTarget =
                false;

            speedText =
                CreateText(
                    panel,
                    "Speed",
                    36,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 62f),
                    new Vector2(128f, 42f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            speedUnitText = null;

            RectTransform driveModeChip;

            Texture2D chipTexture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.rectanglePanel;

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
                    new Vector2(172f, 34f);

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
                    new Vector2(158f, 26f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    BlueAccent);

            lastDisplayedDriveMode =
                null;
        }

        private void BuildStatus(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Activity Status",
                    new Vector2(0f, -18f),
                    new Vector2(430f, 40f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Color(
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.94f));

            statusPanel = panel.gameObject;

            statusActivityIcon =
                CreateHudIcon(
                    panel,
                    "Status Activity Icon",
                    MotorCityIconLibrary.Reward,
                    new Vector2(
                        16f,
                        0f),
                    new Vector2(
                        22f,
                        22f),
                    new Vector2(
                        0f,
                        0.5f),
                    BlueAccent);

            statusText =
                CreateText(
                    panel,
                    "Status Text",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(44f, 0f),
                    new Vector2(368f, 28f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    TextColor);
        }

        private void CreateMinimapPlayerChevron(
            Transform parent)
        {
            Color color =
                new(
                    0.16f,
                    0.72f,
                    1f,
                    1f);

            CreateChevronStroke(
                parent,
                "Player Arrow Left",
                new Vector2(
                    -4.5f,
                    1f),
                -42f,
                color);

            CreateChevronStroke(
                parent,
                "Player Arrow Right",
                new Vector2(
                    4.5f,
                    1f),
                42f,
                color);

            GameObject tail =
                new(
                    "Player Arrow Tail",
                    typeof(RectTransform),
                    typeof(Image));

            tail.transform.SetParent(
                parent,
                false);

            RectTransform tailRect =
                tail.GetComponent<RectTransform>();

            tailRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            tailRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f);

            tailRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            tailRect.anchoredPosition =
                new Vector2(
                    0f,
                    -5f);

            tailRect.sizeDelta =
                new Vector2(
                    4f,
                    13f);

            Image image =
                tail.GetComponent<Image>();

            image.color =
                color;

            image.raycastTarget =
                false;
        }

        private static void CreateChevronStroke(
            Transform parent,
            string objectName,
            Vector2 position,
            float rotation,
            Color color)
        {
            GameObject stroke =
                new(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image));

            stroke.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                stroke.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchoredPosition =
                position;

            rect.sizeDelta =
                new Vector2(
                    4f,
                    15f);

            rect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    rotation);

            Image image =
                stroke.GetComponent<Image>();

            image.color =
                color;

            image.raycastTarget =
                false;
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

        private void BuildNavigator(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Minimap",
                    new Vector2(-18f, -18f),
                    new Vector2(218f, 230f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Color(
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.94f));

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
                new Vector2(0f, -10f);

            rimRect.sizeDelta =
                new Vector2(184f, 184f);

            Image rimImage =
                rimObject.GetComponent<Image>();

            rimImage.sprite =
                minimapMaskSprite;

            rimImage.color =
                new Color(
                    0.09f,
                    0.08f,
                    0.14f,
                    0.98f);

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
                new Vector2(0f, -15f);

            viewportRect.sizeDelta =
                new Vector2(174f, 174f);

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
                new Vector2(266f, 266f);

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
                    new Vector2(6f, 6f);

                Image dotImage =
                    dot.GetComponent<Image>();

                dotImage.color =
                    new Color(
                        0.16f,
                        0.82f,
                        1f,
                        0.96f);
                dotImage.raycastTarget =
                    false;

                dot.SetActive(false);
                minimapRouteDots[i] =
                    dotRect;
            }

            GameObject playerArrowObject =
                new(
                    "Minimap Player",
                    typeof(RectTransform));

            playerArrowObject.transform.SetParent(
                viewportRect,
                false);

            minimapPlayerArrow =
                playerArrowObject.GetComponent<RectTransform>();

            minimapPlayerArrow.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            minimapPlayerArrow.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f);

            minimapPlayerArrow.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            minimapPlayerArrow.anchoredPosition =
                Vector2.zero;

            minimapPlayerArrow.sizeDelta =
                new Vector2(
                    28f,
                    28f);

            CreateMinimapPlayerChevron(
                minimapPlayerArrow);

            minimapTargetIcon =
                CreateHudIcon(
                    viewportRect,
                    "Minimap Target",
                    MotorCityIconLibrary.ForActivity(
                        activityManager != null
                            ? activityManager.ActiveId
                            : string.Empty),
                    Vector2.zero,
                    new Vector2(
                        26f,
                        26f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Color(
                        1f,
                        0.70f,
                        0.10f,
                        1f));

            minimapTargetBlip =
                minimapTargetIcon.rectTransform;

            RectTransform targetStrip =
                CreatePanel(
                    panel,
                    "Navigation Target Strip",
                    new Vector2(
                        8f,
                        7f),
                    new Vector2(
                        148f,
                        30f),
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        0f,
                        0f),
                    new Color(
                        0.055f,
                        0.05f,
                        0.095f,
                        0.96f));

            minimapTargetText =
                CreateText(
                    targetStrip,
                    "Minimap Target Label",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(
                        8f,
                        0f),
                    new Vector2(
                        132f,
                        26f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Vector2(
                        0f,
                        0.5f),
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
                new Vector2(-8f, 7f);
            bool touchUi =
                ShouldUseTouchUi();

            navigatorButtonRect.sizeDelta =
                new Vector2(48f, 32f);

            Image navigatorButtonImage =
                navigatorButtonObject.GetComponent<Image>();

            navigatorButtonImage.color =
                new Color(
                    BlueAccent.r,
                    BlueAccent.g,
                    BlueAccent.b,
                    0.94f);

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
                    new Vector2(44f, 28f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            navigatorButtonLabel.text =
                touchUi
                    ? MotorCityLocalization.Text(
                        "navigator.touch_button")
                    : "M";
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
                SetActiveIfChanged(
                    navigatorPanel,
                    false);
                return;
            }

            SetActiveIfChanged(
                navigatorPanel,
                true);

            Vector3 carPosition =
                car.transform.position;

            const float worldRadius =
                260f;

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

            minimapTargetResolveTimer -=
                Time.unscaledDeltaTime;

            if (minimapTargetResolveTimer <= 0f)
            {
                minimapTargetResolveTimer =
                    MinimapTargetResolveInterval;

                ResolveMinimapTarget(
                    out cachedMinimapTarget,
                    out cachedMinimapLabel,
                    out cachedMinimapHasTarget,
                    out cachedMinimapShowRoadRoute);
            }

            Vector3 target =
                cachedMinimapTarget;

            string label =
                cachedMinimapLabel;

            bool hasTarget =
                cachedMinimapHasTarget;

            bool showRoadRoute =
                cachedMinimapShowRoadRoute;

            if (!hasTarget)
            {
                if (minimapTargetBlip != null)
                {
                    SetActiveIfChanged(
                        minimapTargetBlip.gameObject,
                        false);
                }

                if (minimapTargetText != null &&
                    !string.IsNullOrEmpty(
                        minimapTargetText.text))
                {
                    minimapTargetText.text =
                        string.Empty;
                }

                lastMinimapDistance =
                    int.MinValue;
                lastMinimapDistanceLabel =
                    string.Empty;

                HideRouteDots();
                ClearFixedRoadRoute();
                return;
            }

            Vector3 delta =
                target -
                carPosition;

            delta.y = 0f;

            Vector3 local =
                Quaternion.Euler(
                    0f,
                    -yaw,
                    0f) *
                delta;

            const float markerRadius =
                78f;

            float mapScale =
                MinimapWorldScale(
                    worldRadius);

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

            minimapRouteUpdateTimer -=
                Time.unscaledDeltaTime;

            if (minimapRouteUpdateTimer <= 0f)
            {
                minimapRouteUpdateTimer =
                    MinimapRouteUpdateInterval;

                UpdateRoadRoute(
                    carPosition,
                    target,
                    yaw,
                    worldRadius,
                    showRoadRoute);
            }

            if (minimapTargetBlip != null)
            {
                SetActiveIfChanged(
                    minimapTargetBlip.gameObject,
                    true);

                minimapTargetBlip.anchoredPosition =
                    mapOffset;

                if (minimapTargetIcon != null)
                {
                    Sprite targetSprite =
                        MotorCityIconLibrary.ForActivity(
                            activityManager != null
                                ? activityManager.ActiveId
                                : string.Empty);

                    if (targetSprite != null &&
                        minimapTargetIcon.sprite !=
                            targetSprite)
                    {
                        minimapTargetIcon.sprite =
                            targetSprite;
                    }
                }
            }

            if (minimapTargetText != null)
            {
                minimapDistanceUpdateTimer -=
                    Time.unscaledDeltaTime;

                bool labelChanged =
                    !string.Equals(
                        label,
                        lastMinimapDistanceLabel,
                        System.StringComparison.Ordinal);

                if (labelChanged ||
                    minimapDistanceUpdateTimer <= 0f)
                {
                    minimapDistanceUpdateTimer =
                        MinimapTargetResolveInterval;

                    int roundedDistance =
                        Mathf.RoundToInt(
                            Mathf.Sqrt(
                                delta.sqrMagnitude));

                    if (roundedDistance !=
                            lastMinimapDistance ||
                        labelChanged)
                    {
                        lastMinimapDistance =
                            roundedDistance;
                        lastMinimapDistanceLabel =
                            label ?? string.Empty;

                        minimapTargetText.text =
                            MotorCityLocalization.Format(
                                "hud.distance",
                                label,
                                roundedDistance);
                    }
                }
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

            if (activityManager != null &&
                !activityManager.IsBusy &&
                (onboarding == null ||
                 onboarding.IsComplete) &&
                (story == null ||
                 story.IsComplete) &&
                season != null &&
                !season.IsComplete &&
                season.IsSeasonOneActive)
            {
                string required =
                    season.RequiredActivityId;

                if (required == "delivery" &&
                    delivery != null)
                {
                    target =
                        delivery.CurrentTarget;

                    label =
                        MotorCityLocalization.Text(
                            "activity.delivery");

                    showRoadRoute =
                        true;

                    return;
                }

                if (required == "drift" &&
                    driftChallenge != null)
                {
                    target =
                        driftChallenge.ZoneCenter;

                    label =
                        MotorCityLocalization.Text(
                            "activity.drift");

                    showRoadRoute =
                        true;

                    return;
                }

                if (required == "sprint" &&
                    streetSprint != null)
                {
                    target =
                        streetSprint.CurrentTarget;

                    label =
                        MotorCityLocalization.Text(
                            "hud.sprint");

                    showRoadRoute =
                        true;

                    return;
                }

                if (required == "circuit" &&
                    circuitRace != null)
                {
                    target =
                        circuitRace.CurrentTarget;

                    label =
                        MotorCityLocalization.Text(
                            "hud.circuit");

                    showRoadRoute =
                        true;

                    return;
                }

                if (required == "profession_carwash" &&
                    carWash != null)
                {
                    target =
                        carWash.StartPoint;

                    label =
                        MotorCityLocalization.Text(
                            "carwash.title");

                    showRoadRoute =
                        true;

                    return;
                }

                if (professions != null &&
                    professions.TryGetStartForActivity(
                        required,
                        out Vector3 professionTarget,
                        out string professionLabel))
                {
                    target =
                        professionTarget;

                    label =
                        professionLabel;

                    showRoadRoute =
                        true;

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

        private void BuildDriftPanel(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Drift HUD",
                    new Vector2(0f, -66f),
                    new Vector2(258f, 40f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Color(
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.94f));

            driftPanel = panel.gameObject;

            driftText =
                CreateText(
                    panel,
                    "Drift Score",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(232f, 30f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    DriftAccent);
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
                    0.012f,
                    0.010f,
                    0.022f,
                    0.78f);

            backdrop.raycastTarget = false;

            RectTransform panel =
                CreatePanel(
                    activityResultOverlay.transform,
                    "Activity Result",
                    Vector2.zero,
                    new Vector2(600f, 316f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.985f));

            resultActivityIcon =
                CreateHudIcon(
                    panel,
                    "Result Activity Icon",
                    MotorCityIconLibrary.Achievement,
                    new Vector2(
                        -246f,
                        -32f),
                    new Vector2(
                        30f,
                        30f),
                    new Vector2(
                        0.5f,
                        1f),
                    SecondaryTextColor);

            resultTitleText =
                CreateText(
                    panel,
                    "Result Title",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -32f),
                    new Vector2(520f, 26f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            resultHeadlineText =
                CreateText(
                    panel,
                    "Result Headline",
                    32,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -80f),
                    new Vector2(520f, 48f),
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
                    new Vector2(0f, -142f),
                    new Vector2(520f, 48f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            resultRewardIcon =
                CreateHudIcon(
                    panel,
                    "Result Reward Icon",
                    MotorCityIconLibrary.Reward,
                    new Vector2(
                        -166f,
                        -198f),
                    new Vector2(
                        30f,
                        30f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Color(
                        1f,
                        0.78f,
                        0.20f,
                        1f));

            resultRewardText =
                CreateText(
                    panel,
                    "Result Reward",
                    28,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-142f, -198f),
                    new Vector2(330f, 40f),
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

        private void BuildResultTouchControls(
            Transform canvas)
        {
            resultTouchControlsRoot =
                new GameObject(
                    "Result Touch Controls",
                    typeof(RectTransform));

            resultTouchControlsRoot.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                resultTouchControlsRoot.GetComponent<RectTransform>();

            root.anchorMin =
                new Vector2(0.5f, 0f);
            root.anchorMax =
                new Vector2(0.5f, 0f);
            root.pivot =
                new Vector2(0.5f, 0f);
            root.anchoredPosition =
                new Vector2(0f, 42f);
            root.sizeDelta =
                new Vector2(380f, 56f);

            resultRetryTouchButton =
                CreateLocalizedTouchPulseButton(
                    root,
                    "Result Retry",
                    "touch.result.retry",
                    MotorCityInputAction.Retry,
                    new Vector2(-95f, 4f),
                    new Vector2(170f, 48f));

            CreateLocalizedTouchPulseButton(
                root,
                "Result Continue",
                "touch.result.continue",
                MotorCityInputAction.Cancel,
                new Vector2(95f, 4f),
                new Vector2(170f, 48f));

            resultTouchControlsRoot.SetActive(
                false);
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

            if (resultActivityIcon != null)
            {
                resultActivityIcon.sprite =
                    MotorCityIconLibrary.ForActivity(
                        activityManager.ResultActivityId,
                        activityManager.ResultSuccess);

                resultActivityIcon.enabled =
                    resultActivityIcon.sprite != null;

                resultActivityIcon.color =
                    accent;
            }

            resultHeadlineText.color = accent;

            bool hasReward =
                activityManager.ResultRewardCredits > 0 ||
                activityManager.ResultReputationReward > 0;

            resultRewardText.color =
                hasReward
                    ? new Color(
                        1f,
                        0.78f,
                        0.20f,
                        1f)
                    : SecondaryTextColor;

            if (resultRewardIcon != null)
            {
                resultRewardIcon.enabled =
                    hasReward &&
                    resultRewardIcon.sprite != null;

                resultRewardIcon.color =
                    resultRewardText.color;
            }
        }

        private static bool IsReplayableResult(
            string activityId)
        {
            return
                activityId == "delivery" ||
                activityId == "drift" ||
                activityId == "sprint" ||
                activityId == "circuit" ||
                activityId == "profession_carwash" ||
                activityId == "profession_tow" ||
                activityId == "profession_pizza" ||
                activityId == "profession_taxi" ||
                activityId == "profession_mail" ||
                activityId == "profession_icecream";
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

                case "profession_carwash":
                    carWash?.RestartFromResult();
                    break;

                case "profession_tow":
                    towTruck?.RestartFromResult();
                    break;

                case "profession_pizza":
                case "profession_taxi":
                case "profession_mail":
                case "profession_icecream":
                    professions?.RestartFromResult();
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
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.985f));

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

                if (open &&
                    ((activityManager != null &&
                      (activityManager.IsBusy ||
                       activityManager.HasResult)) ||
                     (garage != null &&
                      garage.IsOpen)))
                {
                    return;
                }

                clubOverlay.SetActive(
                    open);

                if (open)
                {
                    CloseNavigatorMenuVisualOnly();

                    storeOpen =
                        false;

                    garageOverlay?.SetActive(
                        false);

                    UpdateClubOverlay();
                }

                RefreshDrivingEnabledForUi();
                return;
            }

            if (!clubOverlay.activeSelf)
                return;

            if (MotorCityInput.CancelPressed)
            {
                clubOverlay.SetActive(
                    false);

                RefreshDrivingEnabledForUi();
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
                new Color(0.012f, 0.010f, 0.022f, 0.76f);
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
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.985f));

            garageHeaderIcon =
                CreateHudIcon(
                    panel,
                    "Garage Header Icon",
                    MotorCityIconLibrary.Garage,
                    new Vector2(
                        28f,
                        -29f),
                    new Vector2(
                        30f,
                        30f),
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
                    new Vector2(70f, -28f),
                    new Vector2(258f, 42f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);
            title.text =
                MotorCityLocalization.Text(
                    "hud.garage_title");

            garageCreditsIcon =
                CreateHudIcon(
                    panel,
                    "Garage Credits Icon",
                    MotorCityIconLibrary.Credits,
                    new Vector2(
                        -368f,
                        -27f),
                    new Vector2(
                        22f,
                        22f),
                    new Vector2(
                        1f,
                        1f),
                    new Color(
                        1f,
                        0.78f,
                        0.20f,
                        1f));

            garageMoneyText =
                CreateText(
                    panel,
                    "Garage Credits",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-188f, -28f),
                    new Vector2(170f, 42f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    TextColor);

            garageReputationIcon =
                CreateHudIcon(
                    panel,
                    "Garage Reputation Icon",
                    MotorCityIconLibrary.Reputation,
                    new Vector2(
                        -158f,
                        -27f),
                    new Vector2(
                        20f,
                        20f),
                    new Vector2(
                        1f,
                        1f),
                    new Color(
                        0.72f,
                        0.52f,
                        1f,
                        1f));

            garageReputationText =
                CreateText(
                    panel,
                    "Garage Reputation",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-28f, -28f),
                    new Vector2(120f, 42f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    TextColor);

            garageVehicleStateIcon =
                CreateHudIcon(
                    panel,
                    "Garage Vehicle State Icon",
                    MotorCityIconLibrary.Unlocked,
                    new Vector2(
                        28f,
                        -98f),
                    new Vector2(
                        22f,
                        22f),
                    new Vector2(
                        0f,
                        1f),
                    GarageAccent);

            garageVehicleText =
                CreateText(
                    panel,
                    "Garage Vehicle",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(58f, -70f),
                    new Vector2(674f, 26f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);

            garageNextVehicleText =
                CreateText(
                    panel,
                    "Garage Next Vehicle",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(58f, -98f),
                    new Vector2(674f, 22f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    GarageAccent);

            garageVehicleStatsText =
                CreateText(
                    panel,
                    "Garage Vehicle Stats",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -123f),
                    new Vector2(704f, 22f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);

            garageMasteryIcon =
                CreateHudIcon(
                    panel,
                    "Garage Mastery Icon",
                    MotorCityIconLibrary.Reputation,
                    new Vector2(
                        28f,
                        -149f),
                    new Vector2(
                        18f,
                        18f),
                    new Vector2(
                        0f,
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
                        54f,
                        -149f),
                    new Vector2(
                        678f,
                        7f),
                    new Vector2(
                        0f,
                        1f),
                    new Vector2(
                        0f,
                        1f),
                    new Color(
                        0.09f,
                        0.08f,
                        0.14f,
                        0.94f));

            garageMasteryTrack =
                masteryTrackRect.GetComponent<Image>();

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
                float y =
                    -172f - i * 96f;

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

                Sprite upgradeSprite =
                    i switch
                    {
                        0 => MotorCityIconLibrary.Upgrades,
                        1 => MotorCityIconLibrary.ForActivity(
                            ActivityIcon.Drift),
                        _ => MotorCityIconLibrary.ForActivity(
                            ActivityIcon.SpeedTrap)
                    };

                garageUpgradeIcons[i] =
                    CreateHudIcon(
                        row,
                        "Upgrade Icon",
                        upgradeSprite,
                        new Vector2(
                            20f,
                            -26f),
                        new Vector2(
                            28f,
                            28f),
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
                        new Vector2(60f, -8f),
                        new Vector2(398f, 26f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        TextColor);

                garagePriceIcons[i] =
                    CreateHudIcon(
                        row,
                        "Upgrade Price Icon",
                        MotorCityIconLibrary.Credits,
                        new Vector2(
                            -208f,
                            -12f),
                        new Vector2(
                            18f,
                            18f),
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
                        new Vector2(-16f, -8f),
                        new Vector2(180f, 26f),
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
                        new Vector2(60f, 9f),
                        new Vector2(608f, 28f),
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
                        0.065f,
                        0.058f,
                        0.11f,
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

            garageControlsText =
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

            garageControlsText.text =
                MotorCityLocalization.Text("hud.garage_controls") +
                "   •   " +
                MotorCityLocalization.Text("hud.passport_control");

            BuildGarageTouchControls(
                panel);
        }

        private void BuildGarageTouchControls(
            RectTransform panel)
        {
            garageTouchControlsRoot =
                new GameObject(
                    "Garage Touch Controls",
                    typeof(RectTransform));

            garageTouchControlsRoot.transform.SetParent(
                panel,
                false);

            GameObject rootObject =
                garageTouchControlsRoot;

            RectTransform root =
                rootObject.GetComponent<RectTransform>();

            root.anchorMin =
                new Vector2(0.5f, 0f);
            root.anchorMax =
                new Vector2(0.5f, 0f);
            root.pivot =
                new Vector2(0.5f, 0f);
            root.anchoredPosition =
                new Vector2(0f, 12f);
            root.sizeDelta =
                new Vector2(700f, 108f);

            MotorCityInputAction[] actions =
            {
                MotorCityInputAction.PreviousVehicle,
                MotorCityInputAction.NextVehicle,
                MotorCityInputAction.BuyVehicle,
                MotorCityInputAction.Upgrade1,
                MotorCityInputAction.Upgrade2,
                MotorCityInputAction.Upgrade3,
                MotorCityInputAction.CycleBodyColor,
                MotorCityInputAction.CycleWheels,
                MotorCityInputAction.CycleNeon,
                MotorCityInputAction.ToggleVehiclePassport,
                MotorCityInputAction.Interact
            };

            string[] localizationKeys =
            {
                "touch.garage.prev",
                "touch.garage.next",
                "touch.garage.buy",
                "touch.garage.engine",
                "touch.garage.grip",
                "touch.garage.stability",
                "touch.garage.color",
                "touch.garage.wheels",
                "touch.garage.neon",
                "touch.garage.passport",
                "touch.garage.close"
            };

            const float buttonWidth = 110f;
            const float buttonHeight = 46f;
            const float gap = 6f;
            const int columns = 6;

            for (int i = 0;
                 i < actions.Length;
                 i++)
            {
                int row =
                    i /
                    columns;

                int column =
                    i %
                    columns;

                float totalWidth =
                    columns *
                    buttonWidth +
                    (columns - 1) *
                    gap;

                float x =
                    -totalWidth * 0.5f +
                    buttonWidth * 0.5f +
                    column *
                    (buttonWidth + gap);

                float y =
                    56f -
                    row *
                    (buttonHeight + gap);

                CreateLocalizedTouchPulseButton(
                    root,
                    "Garage Touch " + i,
                    localizationKeys[i],
                    actions[i],
                    new Vector2(x, y),
                    new Vector2(
                        buttonWidth,
                        buttonHeight));
            }

            garageTouchControlsRoot.SetActive(
                false);
        }

        private void CreateTouchPulseButton(
            Transform parent,
            string name,
            string label,
            MotorCityInputAction action,
            Vector2 anchoredPosition,
            Vector2 size)
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
                new Vector2(0.5f, 0f);
            rect.anchorMax =
                new Vector2(0.5f, 0f);
            rect.pivot =
                new Vector2(0.5f, 0.5f);
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.075f,
                    0.07f,
                    0.12f,
                    0.94f);

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
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    size -
                    new Vector2(8f, 6f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            text.text =
                label;
        }

        private GameObject CreateLocalizedTouchPulseButton(
            Transform parent,
            string name,
            string localizationKey,
            MotorCityInputAction action,
            Vector2 anchoredPosition,
            Vector2 size)
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
                new Vector2(0.5f, 0f);
            rect.anchorMax =
                new Vector2(0.5f, 0f);
            rect.pivot =
                new Vector2(0.5f, 0.5f);
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.075f,
                    0.07f,
                    0.12f,
                    0.94f);

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            button.onClick.AddListener(
                () =>
                    MotorCityInput.PulseVirtual(
                        action));

            Sprite actionIcon =
                TouchActionIcon(
                    action);

            if (actionIcon != null)
            {
                CreateHudIcon(
                    rect,
                    "Action Icon",
                    actionIcon,
                    new Vector2(
                        -size.x * 0.5f + 18f,
                        0f),
                    new Vector2(
                        19f,
                        19f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    TextColor);
            }

            Text text =
                CreateText(
                    rect,
                    "Label",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    actionIcon != null
                        ? new Vector2(
                            9f,
                            0f)
                        : Vector2.zero,
                    size -
                    new Vector2(
                        actionIcon != null
                            ? 30f
                            : 8f,
                        6f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            text.text =
                MotorCityLocalization.Text(
                    localizationKey);

            touchLocalizedLabels.Add(
                new TouchLocalizedLabel(
                    text,
                    localizationKey));

            return
                buttonObject;
        }

        private static Sprite TouchActionIcon(
            MotorCityInputAction action)
        {
            return
                action switch
                {
                    MotorCityInputAction.Interact =>
                        MotorCityIconLibrary.Confirm,

                    MotorCityInputAction.BuyVehicle =>
                        MotorCityIconLibrary.Store,

                    MotorCityInputAction.Upgrade1 or
                    MotorCityInputAction.Upgrade2 or
                    MotorCityInputAction.Upgrade3 =>
                        MotorCityIconLibrary.Upgrades,

                    MotorCityInputAction.CycleDriveMode or
                    MotorCityInputAction.CycleWheels or
                    MotorCityInputAction.CycleNeon =>
                        MotorCityIconLibrary.Garage,

                    MotorCityInputAction.CyclePetSkin or
                    MotorCityInputAction.CycleBodyColor or
                    MotorCityInputAction.CycleSticker or
                    MotorCityInputAction.CycleVinyl or
                    MotorCityInputAction.CyclePlate =>
                        MotorCityIconLibrary.Reputation,

                    MotorCityInputAction.SaveCustomizationPreset =>
                        MotorCityIconLibrary.Confirm,

                    MotorCityInputAction.LoadCustomizationPreset =>
                        MotorCityIconLibrary.Unlocked,

                    MotorCityInputAction.TakePhoto =>
                        MotorCityIconLibrary.ForActivity(
                            ActivityIcon.PhotoHunt),

                    MotorCityInputAction.ToggleVehiclePassport =>
                        MotorCityIconLibrary.ForSystem(
                            SystemIcon.VehicleHistory),

                    MotorCityInputAction.ToggleClub =>
                        MotorCityIconLibrary.ForSystem(
                            SystemIcon.Club),

                    MotorCityInputAction.RewardedBonus =>
                        MotorCityIconLibrary.Reward,

                    MotorCityInputAction.ToggleStore =>
                        MotorCityIconLibrary.Store,

                    MotorCityInputAction.ToggleNavigator =>
                        MotorCityIconLibrary.ForActivity(
                            ActivityIcon.Discovery),

                    _ =>
                        null
                };
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
                garage.Credits.ToString(
                    "N0");

            if (garageReputationText != null)
            {
                garageReputationText.text =
                    MotorCityLocalization.Format(
                        "hud.rep_short",
                        activityManager != null
                            ? activityManager.TotalReputation
                            : 0,
                        activityManager != null
                            ? activityManager.ReputationLevel
                            : 1);
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

                if (garage.HasNextVehicle &&
                    !garage.NextVehicleUnlocked)
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
                else if (garage.HasNextVehicle &&
                         !garage.NextVehicleOwned)
                {
                    stateSprite =
                        MotorCityIconLibrary.Credits;

                    stateColor =
                        garage.CanAffordNextVehicle
                            ? new Color(
                                1f,
                                0.78f,
                                0.20f,
                                1f)
                            : new Color(
                                1f,
                                0.42f,
                                0.28f,
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
                    garage.VehicleStatsLine +
                    "   •   " +
                    garage.VehicleMasteryShort;
            }

            if (garageMasteryFill != null)
            {
                RectTransform fillRect =
                    garageMasteryFill.rectTransform;

                fillRect.sizeDelta =
                    new Vector2(
                        678f *
                        Mathf.Clamp01(
                            garage.VehicleMasteryProgress),
                        7f);
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

        private sealed class TouchLocalizedLabel
        {
            public readonly Text Text;
            public readonly string LocalizationKey;

            public TouchLocalizedLabel(
                Text text,
                string localizationKey)
            {
                Text =
                    text;

                LocalizationKey =
                    localizationKey;
            }
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

        private static bool ShouldUseTouchUi()
        {
            if (MotorCityInput.PreferTouchPrompts)
            {
                return true;
            }

#if UNITY_EDITOR
            // Device Simulator can still run as Editor with platform/touch
            // flags unavailable. Keep the portrait fallback for quick phone
            // HUD testing while the shared runtime preference handles actual
            // mobile devices and landscape simulator sessions.
            return
                Screen.height > Screen.width;
#else
            return false;
#endif
        }

        private void ApplyResponsiveCanvasScale(
            bool force)
        {
            if (canvasScaler == null)
                return;

            bool portrait =
                Screen.height > Screen.width;

            if (!force &&
                portrait ==
                lastPortraitLayout)
            {
                return;
            }

            lastPortraitLayout =
                portrait;

            canvasScaler.referenceResolution =
                portrait
                    ? new Vector2(900f, 1600f)
                    : new Vector2(1600f, 900f);

            canvasScaler.matchWidthOrHeight =
                portrait
                    ? 0.35f
                    : 0.5f;
        }

        private void BuildTouchUtilityControls(
            Transform canvas)
        {
            touchUtilityRoot =
                new GameObject(
                    "Touch Utility Controls",
                    typeof(RectTransform));

            touchUtilityRoot.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                touchUtilityRoot.GetComponent<RectTransform>();

            root.anchorMin =
                new Vector2(0.5f, 1f);
            root.anchorMax =
                new Vector2(0.5f, 1f);
            root.pivot =
                new Vector2(0.5f, 1f);
            root.anchoredPosition =
                new Vector2(0f, -18f);
            root.sizeDelta =
                new Vector2(440f, 48f);

            CreateLocalizedTouchPulseButton(
                root,
                "Touch Photo",
                "touch.utility.photo",
                MotorCityInputAction.TakePhoto,
                new Vector2(-165f, 0f),
                new Vector2(100f, 40f));

            CreateLocalizedTouchPulseButton(
                root,
                "Touch Club",
                "touch.utility.club",
                MotorCityInputAction.ToggleClub,
                new Vector2(-55f, 0f),
                new Vector2(100f, 40f));

            CreateLocalizedTouchPulseButton(
                root,
                "Touch Store",
                "touch.utility.store",
                MotorCityInputAction.ToggleStore,
                new Vector2(55f, 0f),
                new Vector2(100f, 40f));

            CreateLocalizedTouchPulseButton(
                root,
                "Touch Bonus",
                "touch.utility.bonus",
                MotorCityInputAction.RewardedBonus,
                new Vector2(165f, 0f),
                new Vector2(100f, 40f));
        }

        private void BuildTouchPauseControl(
            Transform canvas)
        {
            touchPauseRoot =
                new GameObject(
                    "Touch Pause Control",
                    typeof(RectTransform));

            touchPauseRoot.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                touchPauseRoot.GetComponent<RectTransform>();

            root.anchorMin =
                new Vector2(0f, 1f);

            root.anchorMax =
                new Vector2(0f, 1f);

            root.pivot =
                new Vector2(0f, 1f);

            root.anchoredPosition =
                new Vector2(
                    18f,
                    -154f);

            root.sizeDelta =
                new Vector2(
                    54f,
                    44f);

            GameObject buttonObject =
                new(
                    "Touch Pause",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            buttonObject.transform.SetParent(
                root,
                false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0.5f, 0.5f);

            rect.anchorMax =
                new Vector2(0.5f, 0.5f);

            rect.pivot =
                new Vector2(0.5f, 0.5f);

            rect.anchoredPosition =
                Vector2.zero;

            rect.sizeDelta =
                new Vector2(50f, 40f);

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.075f,
                    0.07f,
                    0.12f,
                    0.94f);

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            button.onClick.AddListener(
                OpenPauseMenu);

            CreatePauseGlyph(
                rect);
        }

        private static void CreatePauseGlyph(
            Transform parent)
        {
            for (int i = 0;
                 i < 2;
                 i++)
            {
                GameObject bar =
                    new(
                        "Pause Bar",
                        typeof(RectTransform),
                        typeof(Image));

                bar.transform.SetParent(
                    parent,
                    false);

                RectTransform rect =
                    bar.GetComponent<RectTransform>();

                rect.anchorMin =
                    new Vector2(0.5f, 0.5f);

                rect.anchorMax =
                    new Vector2(0.5f, 0.5f);

                rect.pivot =
                    new Vector2(0.5f, 0.5f);

                rect.anchoredPosition =
                    new Vector2(
                        i == 0
                            ? -5f
                            : 5f,
                        0f);

                rect.sizeDelta =
                    new Vector2(
                        4f,
                        16f);

                Image image =
                    bar.GetComponent<Image>();

                image.color =
                    TextColor;

                image.raycastTarget =
                    false;
            }
        }

        private void BuildTouchActivityCancelControl(
            Transform canvas)
        {
            touchActivityCancelRoot =
                new GameObject(
                    "Touch Activity Cancel",
                    typeof(RectTransform));

            touchActivityCancelRoot.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                touchActivityCancelRoot.GetComponent<RectTransform>();

            root.anchorMin =
                new Vector2(1f, 0f);
            root.anchorMax =
                new Vector2(1f, 0f);
            root.pivot =
                new Vector2(1f, 0f);
            root.anchoredPosition =
                new Vector2(-28f, 354f);
            root.sizeDelta =
                new Vector2(190f, 46f);

            GameObject button =
                CreateLocalizedTouchPulseButton(
                    root,
                    "Touch Cancel Activity",
                    "touch.drive.cancel",
                    MotorCityInputAction.Cancel,
                    new Vector2(0f, 2f),
                    new Vector2(180f, 42f));

            RectTransform buttonRect =
                button.GetComponent<RectTransform>();

            buttonRect.anchorMin =
                new Vector2(0.5f, 0f);
            buttonRect.anchorMax =
                new Vector2(0.5f, 0f);
            buttonRect.pivot =
                new Vector2(0.5f, 0f);

            touchActivityCancelRoot.SetActive(
                false);
        }

        private void BuildModalTouchControls(
            Transform canvas)
        {
            navigatorTouchControlsRoot =
                BuildTouchModalRow(
                    canvas,
                    "Navigator Touch Controls",
                    "touch.modal.prev",
                    MotorCityInputAction.PreviousVehicle,
                    "touch.modal.select",
                    MotorCityInputAction.Retry,
                    "touch.modal.next",
                    MotorCityInputAction.NextVehicle,
                    "touch.modal.close",
                    MotorCityInputAction.ToggleNavigator);

            storeTouchControlsRoot =
                BuildTouchModalRow(
                    canvas,
                    "Store Touch Controls",
                    "touch.modal.prev",
                    MotorCityInputAction.PreviousVehicle,
                    "touch.store.buy",
                    MotorCityInputAction.Interact,
                    "touch.modal.next",
                    MotorCityInputAction.NextVehicle,
                    "touch.modal.close",
                    MotorCityInputAction.ToggleStore);

            clubTouchControlsRoot =
                BuildTouchModalRow(
                    canvas,
                    "Club Touch Controls",
                    "touch.modal.prev",
                    MotorCityInputAction.PreviousVehicle,
                    "touch.club.join",
                    MotorCityInputAction.Interact,
                    "touch.modal.next",
                    MotorCityInputAction.NextVehicle,
                    "touch.modal.close",
                    MotorCityInputAction.ToggleClub);

            navigatorTouchControlsRoot.SetActive(
                false);
            storeTouchControlsRoot.SetActive(
                false);
            clubTouchControlsRoot.SetActive(
                false);
        }

        private GameObject BuildTouchModalRow(
            Transform canvas,
            string name,
            string leftLocalizationKey,
            MotorCityInputAction leftAction,
            string centerLocalizationKey,
            MotorCityInputAction centerAction,
            string rightLocalizationKey,
            MotorCityInputAction rightAction,
            string closeLocalizationKey,
            MotorCityInputAction closeAction)
        {
            GameObject rootObject =
                new(
                    name,
                    typeof(RectTransform));

            rootObject.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                rootObject.GetComponent<RectTransform>();

            root.anchorMin =
                new Vector2(0.5f, 0f);
            root.anchorMax =
                new Vector2(0.5f, 0f);
            root.pivot =
                new Vector2(0.5f, 0f);
            root.anchoredPosition =
                new Vector2(0f, 28f);
            root.sizeDelta =
                new Vector2(620f, 58f);

            CreateLocalizedTouchPulseButton(
                root,
                name + " Prev",
                leftLocalizationKey,
                leftAction,
                new Vector2(-225f, 26f),
                new Vector2(130f, 50f));

            CreateLocalizedTouchPulseButton(
                root,
                name + " Action",
                centerLocalizationKey,
                centerAction,
                new Vector2(-75f, 26f),
                new Vector2(150f, 50f));

            CreateLocalizedTouchPulseButton(
                root,
                name + " Next",
                rightLocalizationKey,
                rightAction,
                new Vector2(85f, 26f),
                new Vector2(130f, 50f));

            CreateLocalizedTouchPulseButton(
                root,
                name + " Close",
                closeLocalizationKey,
                closeAction,
                new Vector2(230f, 26f),
                new Vector2(130f, 50f));

            return
                rootObject;
        }

        private void BuildTouchControls(
            Transform canvas)
        {
            touchControlsRoot =
                new GameObject(
                    "Touch Driving Controls",
                    typeof(RectTransform));

            touchControlsRoot.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                touchControlsRoot.GetComponent<RectTransform>();

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            BuildLandscapeDrivingControls(
                root);
        }

        private void BuildLandscapeDrivingControls(
            RectTransform root)
        {
            // Steering lives on the left. Throttle/brake live on the right.
            // This mirrors common mobile driving layouts and avoids a
            // keyboard-like WASD cross on landscape screens.
            CreateTouchControlBackdrop(
                root,
                "Steering Backdrop",
                new Vector2(34f, 26f),
                new Vector2(286f, 112f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f));

            RectTransform steerLeft =
                CreateTouchHoldButton(
                    root,
                    "Steer Left",
                    string.Empty,
                    MotorCityInputAction.SteerLeft,
                    new Vector2(42f, 34f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(116f, 88f),
                    34);

            CreateTouchDirectionGlyph(
                steerLeft,
                90f);

            RectTransform steerRight =
                CreateTouchHoldButton(
                    root,
                    "Steer Right",
                    string.Empty,
                    MotorCityInputAction.SteerRight,
                    new Vector2(166f, 34f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(116f, 88f),
                    34);

            CreateTouchDirectionGlyph(
                steerRight,
                -90f);

            CreateTouchControlBackdrop(
                root,
                "Pedals Backdrop",
                new Vector2(-28f, 26f),
                new Vector2(256f, 196f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f));

            CreateTouchHoldButton(
                root,
                "Throttle",
                MotorCityLocalization.Text(
                    "touch.drive.throttle"),
                MotorCityInputAction.Throttle,
                new Vector2(-40f, 122f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(104f, 88f),
                16);

            CreateTouchHoldButton(
                root,
                "Reverse",
                MotorCityLocalization.Text(
                    "touch.drive.brake"),
                MotorCityInputAction.Reverse,
                new Vector2(-40f, 28f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(104f, 88f),
                15);

            CreateTouchHoldButton(
                root,
                "Handbrake",
                MotorCityLocalization.Text(
                    "touch.drive.handbrake_short"),
                MotorCityInputAction.Handbrake,
                new Vector2(-154f, 28f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(88f, 70f),
                13);

            CreateTouchControlBackdrop(
                root,
                "Action Backdrop",
                new Vector2(-28f, 232f),
                new Vector2(190f, 112f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f));

            CreateTouchHoldButton(
                root,
                "Interact",
                MotorCityLocalization.Text(
                    "touch.drive.action_short"),
                MotorCityInputAction.Interact,
                new Vector2(-40f, 242f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(86f, 80f),
                13);

            CreateTouchHoldButton(
                root,
                "Rescue",
                MotorCityLocalization.Text(
                    "touch.drive.rescue_short"),
                MotorCityInputAction.Rescue,
                new Vector2(-132f, 242f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(86f, 80f),
                13);
        }

        private void CreateTouchControlBackdrop(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot)
        {
            GameObject backdropObject =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image));

            backdropObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                backdropObject.GetComponent<RectTransform>();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                backdropObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.015f,
                    0.035f,
                    0.06f,
                    0.24f);

            image.raycastTarget =
                false;
        }

        private RectTransform CreateTouchHoldButton(
            Transform parent,
            string name,
            string label,
            MotorCityInputAction action,
            Vector2 anchoredPosition,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 size,
            int fontSize)
        {
            GameObject buttonObject =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(TouchHoldInputButton));

            buttonObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.035f,
                    0.085f,
                    0.13f,
                    0.76f);

            TouchHoldInputButton input =
                buttonObject.GetComponent<TouchHoldInputButton>();

            input.Bind(action);

            Text text =
                CreateText(
                    rect,
                    "Label",
                    fontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    size -
                    new Vector2(10f, 10f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            text.text =
                label;

            text.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(
                    label));

            return rect;
        }

        private void CreateTouchDirectionGlyph(
            Transform parent,
            float rotation)
        {
            if (parent == null)
                return;

            GameObject glyph =
                new(
                    "Direction Glyph",
                    typeof(RectTransform));

            glyph.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                glyph.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchoredPosition =
                Vector2.zero;

            rect.sizeDelta =
                new Vector2(
                    34f,
                    34f);

            rect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    rotation);

            CreateMinimapPlayerChevron(
                rect);
        }

        private void UpdateTouchControlsVisibility()
        {
            ApplyResponsiveCanvasScale(
                false);

            if (touchControlsRoot == null)
                return;

            bool touchUi =
                ShouldUseTouchUi();

            if (!touchUi)
            {
                SetActiveIfChanged(
                    touchControlsRoot,
                    false);

                SetActiveIfChanged(
                    touchUtilityRoot,
                    false);

                SetActiveIfChanged(
                    touchActivityCancelRoot,
                    false);

                SetActiveIfChanged(
                    touchPauseRoot,
                    false);

                SetActiveIfChanged(
                    resultTouchControlsRoot,
                    false);

                SetActiveIfChanged(
                    navigatorTouchControlsRoot,
                    false);

                SetActiveIfChanged(
                    storeTouchControlsRoot,
                    false);

                SetActiveIfChanged(
                    clubTouchControlsRoot,
                    false);

                SetActiveIfChanged(
                    garageTouchControlsRoot,
                    false);

                if (garageControlsText != null)
                {
                    garageControlsText.gameObject.SetActive(
                        true);
                }

                return;
            }

            SetActiveIfChanged(
                touchControlsRoot,
                !HasBlockingModalUi());

            SetActiveIfChanged(
                touchPauseRoot,
                !pauseMenuOpen &&
                !HasBlockingModalUi());

            bool onboardingComplete =
                onboarding == null ||
                onboarding.IsComplete;

            SetActiveIfChanged(
                touchUtilityRoot,
                onboardingComplete &&
                !HasBlockingModalUi() &&
                (activityManager == null ||
                 !activityManager.IsBusy));

            SetActiveIfChanged(
                touchActivityCancelRoot,
                !HasBlockingModalUi() &&
                activityManager != null &&
                activityManager.IsBusy);

            bool resultOpen =
                activityManager != null &&
                activityManager.HasResult;

            SetActiveIfChanged(
                resultTouchControlsRoot,
                resultOpen);

            if (resultRetryTouchButton != null)
            {
                SetActiveIfChanged(
                    resultRetryTouchButton,
                    resultOpen &&
                    IsReplayableResult(
                        activityManager.ResultActivityId));
            }

            SetActiveIfChanged(
                navigatorTouchControlsRoot,
                navigatorMenuOpen);

            SetActiveIfChanged(
                storeTouchControlsRoot,
                storeOpen);

            SetActiveIfChanged(
                clubTouchControlsRoot,
                clubOverlay != null &&
                clubOverlay.activeSelf);

            bool garageOpen =
                garage != null &&
                garage.IsOpen;

            SetActiveIfChanged(
                garageTouchControlsRoot,
                garageOpen);

            if (garageControlsText != null)
            {
                garageControlsText.gameObject.SetActive(
                    !garageOpen);
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
                    BlueAccent.r,
                    BlueAccent.g,
                    BlueAccent.b,
                    0.12f);

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
