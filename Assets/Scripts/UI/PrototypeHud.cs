using System.Collections.Generic;
using MotorCity.Audio;
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
    public sealed partial class PrototypeHud : MonoBehaviour
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
        private RewardedBonusSystem rewardedBonus;
        private CosmeticStoreSystem cosmeticStore;
        private bool storeOpen;
        private GameObject pauseOverlay;
        private Text pauseQualityText;
        private Text pauseAudioText;
        private Text pauseMusicText;
        private bool pauseMenuOpen;
        private bool audioMuted;
        private float audioVolume = 1f;
        private float pauseStoredTimeScale = 1f;
        private readonly Dictionary<Animator, float> pausedAnimatorSpeeds =
            new();
        private readonly List<ParticleSystem> pausedParticleSystems =
            new();
        private const string AudioMutedSaveKey =
            "MotorCity.Settings.AudioMuted";
        private const string AudioVolumeSaveKey =
            "MotorCity.Settings.AudioVolume";

        private GameObject openingSpeedometerRoot;
        private CanvasGroup openingCharacterGroup;
        private CanvasGroup openingStatusGroup;
        private CanvasGroup openingSpeedometerGroup;
        private CanvasGroup openingMinimapGroup;
        private CanvasGroup openingQuickActionsGroup;
        private CanvasGroup openingTouchDrivingGroup;
        private bool openingHudRevealArmed;
        private bool openingHudRevealActive;
        private float openingHudRevealTimer;
        private float openingHudRevealDuration = 5f;

        public bool FrontEndAudioMuted =>
            audioMuted;

        public float FrontEndAudioVolume =>
            audioVolume;

        public void FrontEndToggleAudio()
        {
            ToggleAudioMute();
        }

        public void FrontEndAdjustAudio(
            int direction)
        {
            AdjustAudioVolume(
                direction);
        }

        public bool FrontEndMusicMuted =>
            MotorCityMusicRuntime.Muted;

        public float FrontEndMusicVolume =>
            MotorCityMusicRuntime.Volume;

        public void FrontEndToggleMusic()
        {
            MotorCityMusicRuntime.ToggleMute();
            RefreshPauseMenuText();
        }

        public void FrontEndAdjustMusic(
            int direction)
        {
            MotorCityMusicRuntime.AdjustVolume(
                direction);

            RefreshPauseMenuText();
        }

        private AchievementSystem achievements;
        private AdventureDirector adventureDirector;

        private Font font;
        private Font boldFont;
        private MotorCityUiThemeAssets uiThemeAssets;
        private static Sprite modalButtonSprite;
        private static Texture2D modalButtonSpriteSource;
        private static readonly Dictionary<Texture2D, Sprite> slicedPanelSprites =
            new();

        private Text moneyText;
        private Text reputationText;
        private Text upgradesText;
        private Text driveModeText;
        private Text speedText;
        private RectTransform speedNeedle;
        private RectTransform speedNeedleGlowRect;
        private RawImage speedNeedleGlow;
        private static Texture2D speedNeedleGlowTexture;
        private DayNightCycleController dayNightCycle;
        private float dayNightResolveTimer;
        private Text statusText;
        private Image statusActivityIcon;
        private Text driftText;
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
        private Image characterPortraitLeftDetail;
        private Image characterPortraitRightDetail;
        private Text characterSourceText;
        private Text characterNameText;
        private Text characterMissionTitleText;
        private Text characterLineText;
        private Text characterRewardText;
        private GameObject seasonCompactButton;
        private Text seasonCompactText;
        private Text seasonCompactIndicatorText;
        private GameObject seasonPanel;
        private Text seasonNameText;
        private bool seasonDetailsOpen;
        private Text seasonMissionText;
        private Text seasonTitleText;
        private Text seasonProgressText;
        private Text seasonRewardText;
        private Text seasonDaysText;
        private Image seasonProgressFill;
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
        private Text navigatorIndexText;
        private Text navigatorCategoryText;
        private Text navigatorDescriptionText;
        private Text navigatorDistanceText;
        private Image navigatorIconImage;
        private Image navigatorIconPlateImage;
        private bool navigatorMenuOpen;
        private int navigatorSelection;
        private bool manualNavigationActive;
        private Vector3 manualNavigationTarget;
        private string manualNavigationLabel;
        private string manualNavigationMarkerId;
        private CitySchematicMap schematicMap;
        private Texture2D minimapMaskTexture;
        private Sprite minimapMaskSprite;
        private float minimapTargetResolveTimer;
        private Vector3 cachedMinimapTarget;
        private string cachedMinimapLabel = string.Empty;
        private string cachedMinimapMarkerId = string.Empty;
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
        private GameObject storeOverlay;
        private GameObject storeBuyButton;
        private Text storeNameText;
        private Text storeDescriptionText;
        private Text storePathText;
        private Text storeOwnershipText;
        private Text storeWalletText;
        private Image storeProductIcon;
        private RawImage storeCurrencyIcon;
        private Text clubEmblemText;
        private Text clubNameText;
        private Text clubDescriptionText;
        private Text clubWeeklyText;
        private Text clubFocusText;
        private Text clubRewardText;
        private RectTransform safeAreaRoot;
        private GameObject touchControlsRoot;
        private GameObject touchWheelSteeringRoot;
        private GameObject touchArrowSteeringRoot;
        private GameObject touchUtilityRoot;
        private GameObject hudQuickMenuRoot;
        private bool hudQuickMenuOpen;
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

        private Text garageMoneyText;
        private Text garageReputationText;
        private Text garageLevelText;
        private Text garageHeaderMasteryText;
        private Text garageStatusText;
        private Image garageVehicleStateIcon;
        private Text garageVehicleText;
        private Text garageNextVehicleText;
        private Text garageVehicleStatsText;
        private Text garageVehicleCharacterText;
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
        private readonly Image[] garagePriceIcons =
            new Image[3];
        private readonly Text[] garageTitleTexts =
            new Text[3];
        private readonly Text[] garagePriceTexts =
            new Text[3];
        private readonly Text[] garageDescriptionTexts =
            new Text[3];

        // Motor City racing UI palette, tuned to the imported Ville Seppanen kit.
        private static readonly Color PanelColor =
            new(0.075f, 0.07f, 0.12f, 0.94f);
        private static readonly Color PanelSoftColor =
            new(0.105f, 0.095f, 0.16f, 0.88f);
        private static readonly Color TextColor =
            new(0.98f, 0.985f, 1f, 1f);
        private static readonly Color SecondaryTextColor =
            new(0.72f, 0.75f, 0.86f, 1f);
        private static readonly Color BlueAccent =
            new(0.34f, 0.53f, 1f, 1f);
        private static readonly Color DriftAccent =
            new(1f, 0.48f, 0.13f, 1f);
        private static readonly Color GarageAccent =
            new(0.62f, 0.42f, 1f, 1f);

        // Shared runtime typography scale. Keep semantic roles stable instead
        // of picking a new font size for every individual screen.
        private const int UiWindowTitleFontSize = 28;
        private const int UiSectionLabelFontSize = 13;
        private const int UiValueFontSize = 18;
        private const int UiBodyFontSize = 15;
        private const int UiRewardFontSize = 13;
        private const int UiButtonFontSize = 13;
        private const int UiHudButtonFontSize = 12;
        private const int UiAdjustButtonFontSize = 18;

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
            rewardedBonus = rewardedBonusSystem;
            cosmeticStore = cosmeticStoreSystem;
            achievements = achievementSystem;
            adventureDirector = director;

            BuildUi();
            ArmOpeningHudReveal(
                5f);
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

        private void Update()
        {
            if (moneyText == null)
                return;

            UpdateOpeningHudReveal();

            MotorCityPlatformRuntime.SetGameplayUiPaused(
                HasBlockingModalUi());

            if (pauseMenuOpen)
            {
                HandlePauseMenuInput();
                return;
            }

            HandleNavigatorMenu();
            HandleStoreInput();
            HandleClubInput();

            UpdateTouchControlsVisibility();
            RefreshHudLocalizationState();

            if (MotorCityInput.RewardedBonusPressed &&
                activityManager != null &&
                activityManager.SecondaryProgressionAllowed &&
                !HasBlockingModalUi())
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
                        activityManager == null ||
                        !activityManager.SecondaryProgressionAllowed ||
                        career == null
                            ? string.Empty
                            : career.HudLine;
                }
    
                if (disciplineText != null)
                {
                    disciplineText.text =
                        activityManager == null ||
                        !activityManager.SecondaryProgressionAllowed
                            ? string.Empty
                            : activityManager.DisciplineHudLine;
                }
    
                if (contractText != null)
                {
                    contractText.text =
                        activityManager == null ||
                        !activityManager.SecondaryProgressionAllowed ||
                        contracts == null
                            ? string.Empty
                            : contracts.HudLine;
                }
    
                if (liveEventText != null)
                {
                    liveEventText.text =
                        activityManager == null ||
                        !activityManager.SecondaryProgressionAllowed ||
                        liveEvents == null
                            ? string.Empty
                            : liveEvents.HudLine;
                }
    
                if (collectionText != null)
                {
                    collectionText.text =
                        activityManager == null ||
                        !activityManager.SecondaryProgressionAllowed ||
                        collection == null
                            ? string.Empty
                            : collection.HudLine;
                }
    
                if (legendText != null)
                {
                    legendText.text =
                        activityManager == null ||
                        !activityManager.SecondaryProgressionAllowed ||
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
                UpdateSeasonPanel();
    
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
                    lastDisplayedSpeed ||
                speedText != null &&
                !speedText.text.EndsWith(
                    MotorCityLocalization.Text(
                        "common.kmh")))
            {
                lastDisplayedSpeed =
                    roundedSpeed;

                if (speedText != null)
                {
                    speedText.text =
                        roundedSpeed.ToString() +
                        " " +
                        MotorCityLocalization.Text(
                            "common.kmh");
                }
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

                Quaternion needleRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        needleAngle);

                speedNeedle.localRotation =
                    needleRotation;

                if (speedNeedleGlowRect != null)
                {
                    speedNeedleGlowRect.localRotation =
                        needleRotation;
                }
            }

            UpdateSpeedNeedleGlow();

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
                SetActiveIfChanged(
                    statusPanel,
                    false);
                SetActiveIfChanged(
                    storeOverlay,
                    true);

                UpdateStoreOverlay();
                return;
            }

            SetActiveIfChanged(
                storeOverlay,
                false);

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
                pauseMenuOpen ||
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
            bool blocked =
                HasBlockingModalUi();

            car?.SetDrivingEnabled(
                !blocked);

            MotorCityPlatformRuntime.SetGameplayUiPaused(
                blocked);
        }

        public void ReplayOpeningHudReveal(
            float duration = 5f)
        {
            ArmOpeningHudReveal(
                duration);
        }

        private void ArmOpeningHudReveal(
            float duration)
        {
            openingHudRevealDuration =
                Mathf.Max(
                    0.5f,
                    duration);

            openingHudRevealTimer =
                0f;

            openingHudRevealArmed =
                true;

            openingHudRevealActive =
                false;

            openingCharacterGroup =
                ResolveOpeningCanvasGroup(
                    characterPanel);

            openingStatusGroup =
                ResolveOpeningCanvasGroup(
                    statusPanel);

            openingSpeedometerGroup =
                ResolveOpeningCanvasGroup(
                    openingSpeedometerRoot);

            openingMinimapGroup =
                ResolveOpeningCanvasGroup(
                    navigatorPanel);

            openingQuickActionsGroup =
                ResolveOpeningCanvasGroup(
                    touchUtilityRoot);

            openingTouchDrivingGroup =
                ResolveOpeningCanvasGroup(
                    touchControlsRoot);

            SetOpeningHudAlpha(
                0f);
        }

        private static CanvasGroup ResolveOpeningCanvasGroup(
            GameObject target)
        {
            if (target == null)
                return null;

            CanvasGroup group =
                target.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group =
                    target.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private void UpdateOpeningHudReveal()
        {
            if (openingHudRevealArmed &&
                Time.timeScale > 0f)
            {
                openingHudRevealArmed =
                    false;

                openingHudRevealActive =
                    true;

                openingHudRevealTimer =
                    0f;
            }

            if (!openingHudRevealActive)
                return;

            openingHudRevealTimer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    openingHudRevealTimer /
                    openingHudRevealDuration);

            // Keep gameplay HUD fully hidden for the first 80% of the
            // cinematic. Reveal it only during the final second of a
            // five-second opening, immediately before the camera hand-off.
            float revealProgress =
                Mathf.InverseLerp(
                    0.80f,
                    1f,
                    progress);

            float eased =
                revealProgress *
                revealProgress *
                (3f - 2f * revealProgress);

            SetOpeningHudAlpha(
                eased);

            if (progress >= 1f)
            {
                openingHudRevealActive =
                    false;

                SetOpeningHudAlpha(
                    1f);
            }
        }

        private void SetOpeningHudAlpha(
            float alpha)
        {
            ApplyOpeningGroupAlpha(
                openingCharacterGroup,
                alpha,
                false);

            ApplyOpeningGroupAlpha(
                openingStatusGroup,
                alpha,
                false);

            ApplyOpeningGroupAlpha(
                openingSpeedometerGroup,
                alpha,
                false);

            ApplyOpeningGroupAlpha(
                openingMinimapGroup,
                alpha,
                false);

            ApplyOpeningGroupAlpha(
                openingQuickActionsGroup,
                alpha,
                alpha < 0.95f);

            ApplyOpeningGroupAlpha(
                openingTouchDrivingGroup,
                alpha,
                alpha < 0.95f);
        }

        private static void ApplyOpeningGroupAlpha(
            CanvasGroup group,
            float alpha,
            bool blockInteraction)
        {
            if (group == null)
                return;

            group.alpha =
                Mathf.Clamp01(
                    alpha);

            group.interactable =
                !blockInteraction;

            group.blocksRaycasts =
                !blockInteraction;
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
            MotorCityMusicRuntime.EnsureExists();
            MotorCityIconLibrary.PrewarmCore();

            font =
                Resources.Load<Font>(
                    "MotorCity/Fonts/Ubuntu-Regular") ??
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            boldFont =
                Resources.Load<Font>(
                    "MotorCity/Fonts/Ubuntu-Bold") ??
                font;

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
            BuildSeasonPanel(safeAreaRoot);
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
            BuildStoreOverlay(safeAreaRoot);
            BuildTouchControls(safeAreaRoot);
            BuildTouchUtilityControls(safeAreaRoot);
            BuildTouchActivityCancelControl(safeAreaRoot);
            BuildTouchPauseControl(safeAreaRoot);
            BuildModalTouchControls(safeAreaRoot);

            driftPanel.SetActive(false);
            activityResultOverlay.SetActive(false);
            garageOverlay.SetActive(false);
            clubOverlay.SetActive(false);
            storeOverlay.SetActive(false);
            navigatorMenuOverlay.SetActive(false);
            pauseOverlay.SetActive(false);

            const string audioSettingsVersionKey =
                "MotorCity.Settings.AudioVersion";

            int audioSettingsVersion =
                MotorCitySaveService.GetInt(
                    audioSettingsVersionKey,
                    0);

            if (audioSettingsVersion < 2)
            {
                // Older builds could leave audio permanently muted after the
                // pause-menu toggle. Migrate once to a known audible default.
                audioMuted = false;

                MotorCitySaveService.SetInt(
                    AudioMutedSaveKey,
                    0);

                MotorCitySaveService.SetInt(
                    audioSettingsVersionKey,
                    2);

                MotorCitySaveService.Save();
            }
            else
            {
                audioMuted =
                    MotorCitySaveService.GetInt(
                        AudioMutedSaveKey,
                        0) != 0;
            }

            audioVolume =
                Mathf.Clamp01(
                    MotorCitySaveService.GetFloat(
                        AudioVolumeSaveKey,
                        1f));

            ApplyAudioVolume();
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

    }
}
