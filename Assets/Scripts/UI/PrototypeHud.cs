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
        private PixieCompanionSystem pixie;
        private FirstSessionOnboardingSystem onboarding;
        private DailyAdventureSystem dailyAdventures;
        private StoryMissionSystem story;
        private SeasonSystem season;
        private PhotoHuntSystem photoHunt;
        private VehicleCustomizationSystem customization;
        private CityProfessionSystem professions;
        private CarWashJobSystem carWash;
        private TowTruckJobSystem towTruck;
        private ClubSystem club;
        private RewardedBonusSystem rewardedBonus;
        private CosmeticStoreSystem cosmeticStore;
        private bool storeOpen;
        private bool clubOpen;
        private GameObject pauseOverlay;
        private Text pauseQualityText;
        private Text pauseDrawDistanceText;
        private Text pauseAudioText;
        private Text pauseMusicText;
        private bool pauseMenuOpen;
        private bool audioMuted;
        private float audioVolume = 1f;
        private float pauseStoredTimeScale = 1f;
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

        public void FrontEndApplyAudioSettings(
            bool muted,
            float volume)
        {
            audioMuted =
                muted;

            audioVolume =
                Mathf.Clamp01(
                    volume);

            ApplyAudioVolume();

            MotorCitySaveService.SetInt(
                AudioMutedSaveKey,
                audioMuted
                    ? 1
                    : 0);

            MotorCitySaveService.SetFloat(
                AudioVolumeSaveKey,
                audioVolume);

            MotorCitySaveService.Save();

            RefreshPauseMenuText();
        }

        private AchievementSystem achievements;
        private AdventureDirector adventureDirector;

        private Font font;
        private Font boldFont;
        private MotorCityTouchArtAssets touchArtAssets;

        private Text moneyText;
        private Text reputationText;
        private Text upgradesText;
        private Text driveModeText;
        private Text speedText;

        private readonly List<Sprite> runtimeOwnedSprites =
            new();

        private Sprite TrackRuntimeSprite(
            Sprite sprite)
        {
            if (sprite != null)
            {
                runtimeOwnedSprites.Add(
                    sprite);
            }

            return sprite;
        }

        private bool lastResultOpen;
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
        private Text minimapTargetDistanceText;
        private readonly RectTransform[] minimapRouteDots =
            new RectTransform[128];
        private readonly Image[] minimapRouteImages = new Image[128];
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
        private RectTransform navigatorMenuPanelRect;
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
        private const float MinimapRouteUpdateInterval = 0.10f;

        private GameObject navigatorPanel;
        private GameObject navigatorTargetStrip;
        private GameObject navigatorShortcutButton;
        private GameObject statusPanel;
        private GameObject driftPanel;
        private GameObject garageOverlay;
        private GameObject garagePassportPanel;
        private GameObject activityResultOverlay;
        private GameObject resultTouchControlsRoot;
        private GameObject resultRetryTouchButton;
        private GameObject resultContinueButton;
        private GameObject resultRetryButton;
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
        private readonly GameObject[] garageActionButtons =
            new GameObject[7];
        private readonly Image[] garageVehicleStatFills =
            new Image[7];
        private readonly Image[,] garageUpgradeLevelSegments =
            new Image[3, 5];
        private CanvasScaler canvasScaler;
        private int lastDisplayedCredits = int.MinValue;
        private int lastDisplayedReputation = int.MinValue;
        private int lastDisplayedReputationLevel = int.MinValue;
        private int lastDisplayedSpeed = int.MinValue;
        private DriveMode? lastDisplayedDriveMode;
        private int lastDisplayedDriftScore = int.MinValue;
        private int lastDisplayedDriftComboTenth = int.MinValue;
        private int lastDisplayedDriftBankedCredits = int.MinValue;
        private bool lastDisplayedDriftWasActive;
        private bool lastDisplayedDriftVisible;
        private float slowHudUpdateTimer;
        private float contextualStatusUpdateTimer;
        private string cachedContextualStatus = string.Empty;
        private const float SlowHudUpdateInterval = 0.10f;
        private string lastHudLanguageCode = string.Empty;
        private bool lastHudTouchPrompts;
        private bool hudLocalizationStateInitialized;
        private bool garageUiDirty = true;
        private bool garageUiStateInitialized;
        private bool lastGarageRookieColorStep;
        private bool lastGarageMetaUnlocked;

        private readonly List<TouchLocalizedLabel> touchLocalizedLabels =
            new();

        private readonly Queue<string> notificationQueue =
            new();
        private string lastNotificationCandidate;
        private string activeNotification;
        private float activeNotificationTimer;

        private Image resultActivityIcon;
        private Image resultRewardIcon;
        private GameObject resultContinueTouchButton;
        private Text resultTitleText;
        private Text resultHeadlineText;
        private Text resultDetailsText;
        private Text resultRewardText;
        private Text resultMasteryText;
        private Text resultSecondaryProgressText;
        private Text resultNextGoalText;
        private Text resultControlsText;
        private Image resultRecordGlow;
        private float resultRecordAnimationTimer;
        private int resultRecordAnimatedSequence = -1;

        private Text garageMoneyText;
        private Text garageReputationText;
        private Text garageLevelText;
        private Text garageHeaderMasteryText;
        private Image garageHeaderLevelFill;
        private Image garageHeaderMasteryFill;
        private Text garageStatusText;
        private Text garageVehicleText;
        private Text garageNextVehicleText;
        private readonly Text[] garageVehicleStatLabels =
            new Text[7];
        private readonly Text[] garageVehicleStatValues =
            new Text[7];
        private Text garageVehicleCharacterText;
        private Image garageMasteryFill;
        private Text garagePassportTitleText;
        private Text garagePassportSummaryText;
        private Text garagePassportDisciplinesText;
        private Text garagePassportMasteryText;
        private Text garagePassportSpecializationText;
        private bool garagePassportOpen;
        private readonly GarageReferenceGraphic[] garagePriceIcons =
            new GarageReferenceGraphic[3];
        private readonly Text[] garageTitleTexts =
            new Text[3];
        private readonly Text[] garageLevelTexts =
            new Text[3];
        private readonly Text[] garageUpgradeActionTexts =
            new Text[3];
        private readonly Text[] garagePriceTexts =
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
            new(0.28f, 0.78f, 1f, 1f);
        private static readonly Color DriftAccent =
            new(0.95f, 0.26f, 1f, 1f);
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
            PixieCompanionSystem pixieSystem,
            FirstSessionOnboardingSystem onboardingSystem,
            DailyAdventureSystem dailyAdventureSystem,
            StoryMissionSystem storySystem,
            SeasonSystem seasonSystem,
            PhotoHuntSystem photoHuntSystem,
            VehicleCustomizationSystem customizationSystem,
            CityProfessionSystem professionSystem,
            CarWashJobSystem carWashSystem,
            TowTruckJobSystem towTruckSystem,
            ClubSystem clubSystem,
            RewardedBonusSystem rewardedBonusSystem,
            CosmeticStoreSystem cosmeticStoreSystem,
            AchievementSystem achievementSystem,
            AdventureDirector director,
            bool playOpeningPresentation = true)
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
            if (activityManager != null)
                activityManager.ActivityResultShown -= CaptureJournalBeforeActivityResult;
            activityManager = manager;
            if (activityManager != null)
                activityManager.ActivityResultShown += CaptureJournalBeforeActivityResult;
            journalBeforeResult = null;
            journalLastResultName = null;

            if (garage != null)
            {
                garage.Changed -=
                    MarkGarageUiDirty;
            }

            garage = garageSystem;

            if (garage != null)
            {
                garage.Changed +=
                    MarkGarageUiDirty;
            }

            career = careerSystem;
            vehicleHistory = historySystem;
            vehicleSpecialization = specializationSystem;
            collection = collectionSystem;
            legends = legendSystem;
            contracts = contractSystem;
            liveEvents = liveEventSystem;
            underground = undergroundSystem;
            cityRisk = riskSystem;
            pixie = pixieSystem;
            onboarding = onboardingSystem;
            dailyAdventures = dailyAdventureSystem;
            story = storySystem;
            season = seasonSystem;
            photoHunt = photoHuntSystem;
            customization = customizationSystem;
            professions = professionSystem;
            carWash = carWashSystem;
            towTruck = towTruckSystem;
            club = clubSystem;
            rewardedBonus = rewardedBonusSystem;
            cosmeticStore = cosmeticStoreSystem;
            achievements = achievementSystem;
            adventureDirector = director;

            BuildUi();

            if (playOpeningPresentation)
            {
                ArmOpeningHudReveal(
                    5f);
            }
        }

        private void MarkGarageUiDirty()
        {
            garageUiDirty =
                true;

            RefreshDrivingEnabledForUi();
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

            garageUiDirty =
                true;

            RefreshTouchLocalizedLabels();
        }

        private void Update()
        {
            if (moneyText == null)
                return;

            UpdateOpeningHudReveal();
            UpdateCountdownOverlay();

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

            if (MotorCityInput.TakePhotoPressed &&
                !HasBlockingModalUi())
            {
                customization?.CapturePhoto();
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

            if (roundedSpeed != lastDisplayedSpeed && speedText != null)
            {
                lastDisplayedSpeed = roundedSpeed;
                speedText.text = roundedSpeed.ToString();
            }

            bool resultOpen =
                activityManager != null &&
                activityManager.HasResult;

            if (resultOpen !=
                lastResultOpen)
            {
                lastResultOpen =
                    resultOpen;

                RefreshDrivingEnabledForUi();
            }

            SetActiveIfChanged(
                activityResultOverlay,
                resultOpen);

            if (resultOpen)
            {
                if (navigatorMenuOpen)
                {
                    CloseNavigatorMenuVisualOnly();
                }

                SetStoreOpen(
                    false);

                SetClubOpen(
                    false);

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

            if (clubOpen)
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
                if (statusText.text !=
                    status)
                {
                    statusText.text =
                        status;

                    RefreshStatusActivityIcon();
                }
            }

            bool showDrift =
                drift != null &&
                (drift.IsDrifting ||
                 drift.CurrentScore > 0 ||
                 drift.ShowRewardMessage);

            SetActiveIfChanged(
                driftPanel,
                showDrift);

            if (!showDrift)
            {
                lastDisplayedDriftVisible =
                    false;
            }
            else
            {
                bool driftActive =
                    drift.IsDrifting ||
                    drift.CurrentScore > 0;

                int score =
                    drift.CurrentScore;

                int comboTenth =
                    Mathf.RoundToInt(
                        drift.Combo * 10f);

                int bankedCredits =
                    drift.LastBankedCredits;

                bool driftTextChanged =
                    !lastDisplayedDriftVisible ||
                    driftActive !=
                        lastDisplayedDriftWasActive ||
                    score !=
                        lastDisplayedDriftScore ||
                    comboTenth !=
                        lastDisplayedDriftComboTenth ||
                    bankedCredits !=
                        lastDisplayedDriftBankedCredits;

                if (driftTextChanged)
                {
                    if (driftActive)
                    {
                        string combo =
                            comboTenth > 10
                                ? $"   x{comboTenth / 10f:0.0}"
                                : string.Empty;

                        driftText.text =
                            MotorCityLocalization.Format(
                                "hud.drift",
                                score,
                                combo);
                    }
                    else
                    {
                        driftText.text =
                            MotorCityLocalization.Format(
                                "hud.drift_done",
                                bankedCredits);
                    }

                    lastDisplayedDriftVisible =
                        true;

                    lastDisplayedDriftWasActive =
                        driftActive;

                    lastDisplayedDriftScore =
                        score;

                    lastDisplayedDriftComboTenth =
                        comboTenth;

                    lastDisplayedDriftBankedCredits =
                        bankedCredits;
                }
            }

            bool garageOpen =
                garage != null &&
                garage.IsOpen;

            SetActiveIfChanged(
                garageOverlay,
                garageOpen);

            SetActiveIfChanged(
                openingSpeedometerRoot,
                !garageOpen);

            if (!garageOpen)
            {
                garageUiDirty =
                    true;

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

                SetActiveIfChanged(
                    characterPanel,
                    false);

                SetActiveIfChanged(
                    seasonCompactButton,
                    false);

                SetActiveIfChanged(
                    seasonPanel,
                    false);

                UpdateGarage();
            }

            UpdateNavigator(
                garageOpen);
        }

        private void SetStoreOpen(
            bool open)
        {
            storeOpen =
                open;

            SetActiveIfChanged(
                storeOverlay,
                open);
        }

        private void SetNavigatorMenuOpen(
            bool open)
        {
            navigatorMenuOpen =
                open;

            SetActiveIfChanged(
                navigatorMenuOverlay,
                open);
        }

        private void SetClubOpen(
            bool open)
        {
            clubOpen =
                open;

            SetActiveIfChanged(
                clubOverlay,
                open);
        }

        // Read-only front-end bridge: informational UI never grants progress.
        public string PlayerExperienceObjectiveLine =>
            adventureDirector != null &&
            adventureDirector.HasMission
                ? adventureDirector.ObjectiveLine
                : onboarding != null &&
                  !onboarding.IsComplete
                    ? onboarding.ObjectiveLine
                    : string.Empty;

        // The center is informational: all records and tasks are obtained
        // from active gameplay systems, never from independent save keys.
        public string PlayerExperienceActivitiesLine
        {
            get
            {
                if (activityManager == null || streetSprint == null ||
                    circuitRace == null || delivery == null ||
                    driftChallenge == null || professions == null)
                    return MotorCityLocalization.Text("ux.sections.not_ready");

                // Read-only, built on page open; no save or payout side effects.
                return BuildPlayerExperienceActivityCatalog();
            }
        }

        private string BuildPlayerExperienceActivityCatalog()
        {
            var sb = new System.Text.StringBuilder(12000);
            sb.Append(PlayerExperienceActiveActivityLine());
            sb.Append("\n\n").Append(MotorCityLocalization.Text("ux.catalog.races"));
            AppendPlayerExperienceRaceCards(sb);
            sb.Append("\n\n").Append(MotorCityLocalization.Text("ux.catalog.drift"));
            AppendPlayerExperienceDriftCards(sb);
            sb.Append("\n\n").Append(MotorCityLocalization.Text("ux.catalog.jobs"));
            AppendPlayerExperienceJobCards(sb);
            sb.Append("\n\n").Append(MotorCityLocalization.Text("ux.catalog.city"));
            AppendPlayerExperienceCityCards(sb);
            sb.Append("\n\n").Append(MotorCityLocalization.Text("ux.catalog.special"));
            AppendPlayerExperienceSpecialCards(sb);
            return sb.ToString();
        }

        private static void AppendPlayerExperienceCard(
            System.Text.StringBuilder sb, string title,
            string about, string start, string access,
            string record, string reward, string next)
        {
            if (string.IsNullOrWhiteSpace(title))
                return;
            sb.Append("\n\n").Append(title);
            AppendPlayerExperienceCardField(sb, "ux.catalog.field.about", about);
            AppendPlayerExperienceCardField(sb, "ux.catalog.field.start", start);
            AppendPlayerExperienceCardField(sb, "ux.catalog.field.access", access);
            AppendPlayerExperienceCardField(sb, "ux.catalog.field.record", record);
            AppendPlayerExperienceCardField(sb, "ux.catalog.field.reward", reward);
            AppendPlayerExperienceCardField(sb, "ux.catalog.field.next", next);
        }

        private static void AppendPlayerExperienceCardField(
            System.Text.StringBuilder sb, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.Append("\n").Append(MotorCityLocalization.Format(key, value));
        }

        private string PlayerExperienceCatalogAccess(string id)
        {
            int restriction = activityManager.CatalogAccessState(id);
            string key = restriction == 1 ? "ux.catalog.access.busy"
                : restriction == 2 ? "ux.catalog.access.tutorial"
                : restriction == 3 ? "ux.catalog.access.story"
                : "ux.catalog.access.ready";
            return MotorCityLocalization.Text(key);
        }

        private static string PlayerExperienceCatalogMedal(int tier)
        {
            return MotorCityLocalization.Text(tier >= 3 ? "medal.gold"
                : tier == 2 ? "medal.silver"
                : tier == 1 ? "medal.bronze" : "medal.none");
        }

        private static string PlayerExperienceNextMedal(int tier)
        {
            return tier >= 3
                ? MotorCityLocalization.Text("ux.catalog.next.completed")
                : MotorCityLocalization.Format("ux.catalog.next.medal",
                    PlayerExperienceCatalogMedal(tier + 1));
        }

        private void AppendPlayerExperienceRaceCards(System.Text.StringBuilder sb)
        {
            string racingLevel = PlayerExperienceDisciplineLevelLine(
                DisciplineType.Racing, "ux.catalog.racing_level");
            string elite = MotorCityLocalization.Format(
                streetSprint.CatalogEliteUnlocked
                    ? "ux.catalog.sprint_elite_open"
                    : "ux.catalog.sprint_elite_locked",
                streetSprint.CatalogEliteRequiredLevel);
            AppendPlayerExperienceCard(sb,
                MotorCityLocalization.Text("ux.catalog.name.sprint"),
                MotorCityLocalization.Text("ux.catalog.about.sprint"),
                MotorCityLocalization.Text("ux.catalog.start.race"),
                PlayerExperienceCatalogAccess("sprint") + "\n" + racingLevel + "\n" + elite,
                FormatPlayerExperienceBestTime(streetSprint.BestTimeSeconds),
                MotorCityLocalization.Format("ux.catalog.base_reward",
                    streetSprint.CatalogBaseRewardCredits),
                streetSprint.CatalogEliteUnlocked
                    ? MotorCityLocalization.Text("ux.catalog.next.record")
                    : MotorCityLocalization.Format("ux.catalog.next.elite",
                        streetSprint.CatalogEliteRequiredLevel));

            AppendPlayerExperienceCard(sb,
                MotorCityLocalization.Text("ux.catalog.name.circuit"),
                MotorCityLocalization.Text("ux.catalog.about.circuit"),
                MotorCityLocalization.Text("ux.catalog.start.race"),
                PlayerExperienceCatalogAccess("circuit"),
                FormatPlayerExperienceBestTime(circuitRace.BestTimeSeconds) +
                    "\n" + MotorCityLocalization.Format("ux.catalog.circuit_lap",
                        FormatPlayerExperienceBestTime(circuitRace.BestLapSeconds)),
                MotorCityLocalization.Format("ux.catalog.circuit_reward",
                    circuitRace.CatalogBaseRewardCredits),
                MotorCityLocalization.Text("ux.catalog.next.record"));
        }

        private void AppendPlayerExperienceDriftCards(System.Text.StringBuilder sb)
        {
            string score = drift != null && drift.BestDrift > 0
                ? MotorCityLocalization.Format("ux.catalog.drift_best", drift.BestDrift)
                : MotorCityLocalization.Text("ux.catalog.drift_best_empty");
            string level = PlayerExperienceDisciplineLevelLine(
                DisciplineType.Drift, "ux.catalog.drift_level");
            string elite = MotorCityLocalization.Format(
                driftChallenge.CatalogEliteUnlocked
                    ? "ux.catalog.drift_elite_open"
                    : "ux.catalog.drift_elite_locked",
                driftChallenge.CatalogEliteRequiredLevel);
            AppendPlayerExperienceCard(sb,
                MotorCityLocalization.Text("ux.catalog.name.driftchallenge"),
                MotorCityLocalization.Text("ux.catalog.about.drift"),
                MotorCityLocalization.Text("ux.catalog.start.drift"),
                PlayerExperienceCatalogAccess("drift") + "\n" + level + "\n" + elite,
                score + "\n" + MotorCityLocalization.Format("ux.catalog.drift_thresholds",
                    driftChallenge.TargetScore, driftChallenge.CatalogSilverTargetScore,
                    driftChallenge.CatalogGoldTargetScore,
                    driftChallenge.CatalogLegendaryTargetScore),
                MotorCityLocalization.Format("ux.catalog.drift_rewards",
                    driftChallenge.RewardCredits,
                    driftChallenge.CatalogSilverRewardCredits,
                    driftChallenge.CatalogGoldRewardCredits,
                    driftChallenge.CatalogLegendaryRewardCredits),
                driftChallenge.CatalogEliteUnlocked
                    ? MotorCityLocalization.Text("ux.catalog.next.record")
                    : MotorCityLocalization.Format("ux.catalog.next.elite",
                        driftChallenge.CatalogEliteRequiredLevel));

            if (driftSpots == null)
                return;
            for (int i = 0; i < driftSpots.SpotCount; i++)
            {
                string name = driftSpots.GetSpotName(i);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                int medal = driftSpots.GetSpotHighestMedal(i);
                int best = driftSpots.GetSpotBestScore(i);
                string record = MotorCityLocalization.Format(
                    best > 0 ? "ux.catalog.drift_spot_record"
                        : "ux.catalog.drift_spot_no_record",
                    name, best) + "\n" +
                    MotorCityLocalization.Format("ux.catalog.drift_spot_targets",
                        driftSpots.GetSpotBronzeTarget(i),
                        driftSpots.GetSpotSilverTarget(i),
                        driftSpots.GetSpotGoldTarget(i)) + "\n" +
                    MotorCityLocalization.Format("ux.catalog.drift_spot_medal",
                        PlayerExperienceCatalogMedal(medal));
                AppendPlayerExperienceCard(sb, name,
                    MotorCityLocalization.Text("ux.catalog.about.drift"),
                    MotorCityLocalization.Text("ux.catalog.start.drift"),
                    activityManager.IsBusy
                        ? MotorCityLocalization.Text("ux.catalog.access.busy")
                        : MotorCityLocalization.Text("ux.catalog.access.passive"),
                    record, MotorCityLocalization.Format("ux.catalog.one_time_tiers",
                        driftSpots.GetCatalogMedalCreditReward(1),
                        driftSpots.GetCatalogMedalReputationReward(1),
                        driftSpots.GetCatalogMedalCreditReward(2),
                        driftSpots.GetCatalogMedalReputationReward(2),
                        driftSpots.GetCatalogMedalCreditReward(3),
                        driftSpots.GetCatalogMedalReputationReward(3)),
                    PlayerExperienceNextMedal(medal));
            }
        }

        private void AppendPlayerExperienceJobCards(System.Text.StringBuilder sb)
        {
            string deliveryLevel = PlayerExperienceDisciplineLevelLine(
                DisciplineType.Delivery, "ux.catalog.delivery_level");
            string elite = MotorCityLocalization.Format(
                delivery.EliteUnlocked
                    ? "ux.catalog.delivery_elite_unlocked"
                    : "ux.catalog.delivery_elite",
                delivery.EliteRequiredDisciplineLevel);
            AppendPlayerExperienceCard(sb,
                MotorCityLocalization.Text("ux.catalog.name.delivery"),
                MotorCityLocalization.Text("ux.catalog.about.delivery"),
                MotorCityLocalization.Text("ux.catalog.start.race"),
                PlayerExperienceCatalogAccess("delivery") + "\n" + deliveryLevel + "\n" + elite,
                FormatPlayerExperienceBestTime(delivery.BestTimeSeconds) + "\n" +
                    MotorCityLocalization.Format("ux.catalog.delivery_times",
                        delivery.CatalogGoldTimeSeconds,
                        delivery.CatalogSilverTimeSeconds,
                        delivery.CatalogBronzeTimeSeconds),
                MotorCityLocalization.Format("ux.catalog.delivery_payouts",
                    delivery.RewardCredits, delivery.CatalogSilverRewardCredits,
                    delivery.CatalogBronzeRewardCredits,
                    delivery.CatalogCompletionRewardCredits),
                delivery.EliteUnlocked
                    ? MotorCityLocalization.Text("ux.catalog.next.record")
                    : MotorCityLocalization.Format("ux.catalog.next.elite",
                        delivery.EliteRequiredDisciplineLevel));

            string nextProfession = professions.CompletionsUntilNextLevel == 0
                ? MotorCityLocalization.Text("ux.catalog.prof.max")
                : MotorCityLocalization.Format("ux.catalog.next.prof",
                    professions.CompletionsUntilNextLevel);
            for (int i = 0; i < professions.StartCount; i++)
            {
                string name = professions.GetStartName(i);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                AppendPlayerExperienceCard(sb, name,
                    MotorCityLocalization.Text("ux.catalog.about.profession"),
                    MotorCityLocalization.Text("ux.catalog.start.profession"),
                    PlayerExperienceCatalogAccess(professions.GetStartActivityId(i)),
                    MotorCityLocalization.Format("ux.catalog.profession_line",
                        professions.GetStartCompletedCount(i)),
                    MotorCityLocalization.Format("ux.catalog.tow_reward",
                        professions.GetStartNextRewardCredits(i)),
                    nextProfession);
            }
            if (carWash != null)
                AppendPlayerExperienceCard(sb,
                    MotorCityLocalization.Text("carwash.title"),
                    MotorCityLocalization.Text("ux.catalog.about.carwash"),
                    MotorCityLocalization.Text("ux.catalog.start.carwash"),
                    PlayerExperienceCatalogAccess("profession_carwash"),
                    MotorCityLocalization.Text("ux.catalog.no_records"),
                    MotorCityLocalization.Format("ux.catalog.tow_reward",
                        carWash.CatalogNextRewardCredits), nextProfession);
            if (towTruck != null)
                AppendPlayerExperienceCard(sb,
                    MotorCityLocalization.Text("tow.title"),
                    MotorCityLocalization.Text("ux.catalog.about.tow"),
                    MotorCityLocalization.Text("ux.catalog.start.tow"),
                    PlayerExperienceCatalogAccess("profession_tow"),
                    MotorCityLocalization.Text("ux.catalog.no_records"),
                    MotorCityLocalization.Format("ux.catalog.tow_reward",
                        towTruck.CatalogNextRewardCredits), nextProfession);
            if (vehicleSpecialization != null)
                sb.Append("\n\n").Append(MotorCityLocalization.Format(
                    "ux.catalog.vehicle_tip", vehicleSpecialization.GarageCompactLine));
        }

        private void AppendPlayerExperienceCityCards(System.Text.StringBuilder sb)
        {
            string passiveAccess = activityManager.IsBusy
                ? MotorCityLocalization.Text("ux.catalog.access.busy")
                : MotorCityLocalization.Text("ux.catalog.access.passive");
            if (speedTraps != null)
            {
                for (int i = 0; i < speedTraps.TrapCount; i++)
                {
                    string name = speedTraps.GetTrapName(i);
                    if (string.IsNullOrWhiteSpace(name))
                        continue;
                    float best = speedTraps.GetBestSpeed(i);
                    int medal = speedTraps.GetHighestMedal(i);
                    string progress = best > 0f
                        ? MotorCityLocalization.Format("ux.catalog.radar_progress",
                            best, PlayerExperienceCatalogMedal(medal),
                            speedTraps.GetTrapGoalText(i))
                        : MotorCityLocalization.Text("ux.catalog.radar.none") +
                            "\n" + speedTraps.GetTrapGoalText(i);
                    AppendPlayerExperienceCard(sb,
                        MotorCityLocalization.Text("ux.catalog.name.radar") + ": " + name,
                        MotorCityLocalization.Text("ux.catalog.about.radar"),
                        MotorCityLocalization.Text("ux.catalog.start.radar"),
                        passiveAccess, progress,
                        MotorCityLocalization.Format("ux.catalog.one_time_tiers",
                            speedTraps.GetCatalogMedalCreditReward(1),
                            speedTraps.GetCatalogMedalReputationReward(1),
                            speedTraps.GetCatalogMedalCreditReward(2),
                            speedTraps.GetCatalogMedalReputationReward(2),
                            speedTraps.GetCatalogMedalCreditReward(3),
                            speedTraps.GetCatalogMedalReputationReward(3)),
                        PlayerExperienceNextMedal(medal));
                }
            }
            if (discoveries != null)
            {
                string count = MotorCityLocalization.Format(
                    "ux.catalog.discovery_line", discoveries.FoundCount,
                    discoveries.DiscoveryCount);
                for (int i = 0; i < discoveries.DiscoveryCount; i++)
                {
                    string name = discoveries.GetDiscoveryName(i);
                    if (string.IsNullOrWhiteSpace(name))
                        continue;
                    bool found = discoveries.IsFound(i);
                    AppendPlayerExperienceCard(sb,
                        MotorCityLocalization.Text("ux.catalog.name.discovery") + ": " + name,
                        MotorCityLocalization.Text("ux.catalog.about.discovery"),
                        MotorCityLocalization.Text("ux.catalog.start.discovery"),
                        passiveAccess,
                        MotorCityLocalization.Text(found
                            ? "ux.catalog.discovery_done"
                            : "ux.catalog.discovery_missing") + "\n" + count,
                        MotorCityLocalization.Format("ux.catalog.reward.total",
                            DiscoverySystem.CatalogCreditsReward,
                            DiscoverySystem.CatalogReputationReward),
                        found ? MotorCityLocalization.Text("ux.catalog.completed")
                            : MotorCityLocalization.Text("ux.catalog.next.discovery"));
                }
            }
            if (photoHunt != null)
                AppendPlayerExperienceCard(sb,
                    MotorCityLocalization.Text("ux.catalog.name.photos"),
                    MotorCityLocalization.Text("ux.catalog.about.photo"),
                    MotorCityLocalization.Text("ux.catalog.start.photo"),
                    passiveAccess,
                    photoHunt.AlbumLine,
                    MotorCityLocalization.Text("ux.catalog.photo_reward"),
                    MotorCityLocalization.Text("ux.catalog.next.photo"));
        }

        private static string PlayerExperienceCatalogReward(int credits, int rep)
        {
            return credits > 0 || rep > 0
                ? MotorCityLocalization.Format("ux.catalog.reward.total", credits, rep)
                : null;
        }

        private void AppendPlayerExperienceSpecialCards(System.Text.StringBuilder sb)
        {
            if (liveEvents != null)
                AppendPlayerExperienceCard(sb,
                    MotorCityLocalization.Text("ux.catalog.name.event"),
                    MotorCityLocalization.Text("ux.catalog.about.event"),
                    MotorCityLocalization.Text("ux.catalog.start.special"),
                    MotorCityLocalization.Text("ux.catalog.access.progress"),
                    liveEvents.HudLine,
                    PlayerExperienceCatalogReward(
                        liveEvents.CatalogCurrentCreditsReward,
                        liveEvents.CatalogCurrentReputationReward),
                    MotorCityLocalization.Text("ux.catalog.next.story"));

            if (legends != null)
                AppendPlayerExperienceCard(sb,
                    MotorCityLocalization.Text("ux.catalog.name.legend"),
                    MotorCityLocalization.Text("ux.catalog.about.legend"),
                    MotorCityLocalization.Text("ux.catalog.start.special"),
                    legends.AllLegendsDefeated
                        ? MotorCityLocalization.Text("ux.catalog.completed")
                        : MotorCityLocalization.Text("ux.catalog.access.progress"),
                    legends.HudLine,
                    PlayerExperienceCatalogReward(
                        legends.CatalogCurrentCreditsReward,
                        legends.CatalogCurrentReputationReward),
                    legends.AllLegendsDefeated
                        ? MotorCityLocalization.Text("ux.catalog.completed")
                        : MotorCityLocalization.Text("ux.catalog.next.story"));

            if (underground != null)
            {
                string access = underground.CatalogSeriesComplete
                    ? MotorCityLocalization.Text("ux.catalog.completed")
                    : !underground.HasActiveInvitation
                        ? MotorCityLocalization.Text("ux.catalog.night.locked")
                        : underground.CatalogIsNight
                            ? MotorCityLocalization.Text("ux.catalog.night.now")
                            : MotorCityLocalization.Text("ux.catalog.night.wait");
                AppendPlayerExperienceCard(sb,
                    MotorCityLocalization.Text("ux.catalog.name.night"),
                    MotorCityLocalization.Text("ux.catalog.about.night"),
                    MotorCityLocalization.Text("ux.catalog.start.special"),
                    access + "\n" + MotorCityLocalization.Text(
                        "ux.catalog.night.requirement"),
                    underground.HudLine,
                    PlayerExperienceCatalogReward(
                        underground.CatalogCurrentCreditsReward,
                        underground.CatalogCurrentReputationReward),
                    underground.CatalogSeriesComplete
                        ? MotorCityLocalization.Text("ux.catalog.completed")
                        : MotorCityLocalization.Text("ux.catalog.next.story"));
            }
        }

        private string PlayerExperienceActiveActivityLine()
        {
            string activityKey = null;
            if (streetSprint.IsActive || streetSprint.IsCountingDown)
                activityKey = "ux.catalog.active_sprint";
            else if (circuitRace.IsActive || circuitRace.IsCountingDown)
                activityKey = "ux.catalog.active_circuit";
            else if (driftChallenge.IsActive || driftChallenge.IsCountingDown)
                activityKey = "ux.catalog.active_drift";
            else if (delivery.IsActive || delivery.IsCountingDown)
                activityKey = "ux.catalog.active_delivery";
            else if (professions.IsActive)
                return MotorCityLocalization.Format(
                    "ux.catalog.status_active", professions.CurrentLabel);

            return activityKey == null
                ? MotorCityLocalization.Text("ux.catalog.status_idle")
                : MotorCityLocalization.Format(
                    "ux.catalog.status_active",
                    MotorCityLocalization.Text(activityKey));
        }

        private string PlayerExperienceDisciplineLevelLine(
            MotorCity.Gameplay.DisciplineType type, string key)
        {
            if (activityManager == null)
                return MotorCityLocalization.Text("ux.catalog.level_unavailable");
            int level = activityManager.CatalogDisciplineLevel(type);
            return level > 0
                ? MotorCityLocalization.Format(key, level)
                : MotorCityLocalization.Text("ux.catalog.level_unavailable");
        }

        private string PlayerExperienceDriftZoneRecords()
        {
            if (driftSpots == null || driftSpots.SpotCount <= 0)
                return string.Empty;

            System.Text.StringBuilder records = new System.Text.StringBuilder(160);
            for (int i = 0; i < driftSpots.SpotCount; i++)
            {
                string name = driftSpots.GetSpotName(i);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                int best = driftSpots.GetSpotBestScore(i);
                records.Append(MotorCityLocalization.Format(
                    best > 0 ? "ux.catalog.drift_spot_record" : "ux.catalog.drift_spot_no_record",
                    name, best));
                records.Append('\n');
                records.Append(MotorCityLocalization.Format(
                    "ux.catalog.drift_spot_targets",
                    driftSpots.GetSpotBronzeTarget(i),
                    driftSpots.GetSpotSilverTarget(i),
                    driftSpots.GetSpotGoldTarget(i)));
                records.Append('\n');
                int medal = driftSpots.GetSpotHighestMedal(i);
                string medalName = MotorCityLocalization.Text(
                    medal >= 3 ? "medal.gold"
                    : medal == 2 ? "medal.silver"
                    : medal == 1 ? "medal.bronze" : "medal.none");
                records.Append(MotorCityLocalization.Format(
                    "ux.catalog.drift_spot_medal", medalName));
                records.Append('\n');
            }
            return records.ToString();
        }

        private string PlayerExperienceProfessionNames()
        {
            if (professions == null || professions.StartCount <= 0)
                return MotorCityLocalization.Text("ux.catalog.no_job_starts");

            System.Text.StringBuilder names = new System.Text.StringBuilder(160);
            for (int i = 0; i < professions.StartCount; i++)
            {
                string name = professions.GetStartName(i);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                if (names.Length > 0)
                    names.Append(", ");
                names.Append(name);
            }
            return names.Length > 0
                ? MotorCityLocalization.Format(
                    "ux.catalog.job_names", names.ToString())
                : MotorCityLocalization.Text("ux.catalog.no_job_starts");
        }

        private static string FormatPlayerExperienceBestTime(
            float bestSeconds)
        {
            return bestSeconds > 0f
                ? MotorCityLocalization.Format(
                    "ux.activities.best_time",
                    bestSeconds)
                : MotorCityLocalization.Text(
                    "ux.activities.no_record");
        }

        public string PlayerExperienceJournalLine =>
            PlayerExperienceJournalLineForFilter(-1);

        // UX-1 overview: only existing read-only gameplay state is displayed.
        // Club reward eligibility is NOT a "claim" button and must not be
        // confused with the goal being completed.
        public string PlayerExperienceClubLine => BuildPlayerExperienceClubDetail();

        public string PlayerExperienceInspectorLine => BuildPlayerExperienceInspectorDetail();

        public string PlayerExperiencePixieLine => BuildPlayerExperiencePixieDetail();

        // The ActivityManager owns the same priority resolver shown after
        // a completed activity. Reuse it rather than inventing a new list
        // of supposedly available jobs in the front-end.
        public string PlayerExperienceRecommendedTaskLine
        {
            get
            {
                if (activityManager == null)
                    return string.Empty;

                ResultNextGoalResolver.Goal goal =
                    activityManager.RecommendedGoal;
                if (!goal.IsValid)
                    return string.Empty;

                string progress = string.Empty;
                if (goal.Type == ResultNextGoalResolver.GoalType.Story &&
                    story != null && !story.IsComplete &&
                    story.CurrentMissionTarget > 0)
                {
                    progress = MotorCityLocalization.Format(
                        "ux.home.remaining_count",
                        story.RemainingForCurrentMission,
                        story.CurrentMissionTarget);
                }
                else if (goal.Type == ResultNextGoalResolver.GoalType.Season &&
                         season != null && !season.IsComplete &&
                         season.IsSeasonOneActive &&
                         season.CurrentMissionTarget > 0)
                {
                    progress = MotorCityLocalization.Format(
                        "ux.home.remaining_count",
                        Mathf.Max(0, season.CurrentMissionTarget -
                            season.CurrentMissionProgress),
                        season.CurrentMissionTarget);
                }

                return string.IsNullOrEmpty(progress)
                    ? goal.Text
                    : goal.Text + "\n" + progress;
            }
        }

        // UX-1: observational snapshots from the existing gameplay owners.
        // These are read only, and refreshed only when the front-end opens
        // or switches a page. They never select vehicles or grant rewards.
        // Exact reward is shown only for the currently selected story or
        // season mission. Other objective sources do not expose a reliable
        // finalized reward here, so do not invent one.
        public string PlayerExperienceObjectiveRewardLine
        {
            get
            {
                if (activityManager == null)
                    return string.Empty;

                // The recommended goal may differ from the currently
                // tracked navigator mission while an activity is active.
                // Show a reward only when its real source owns that goal.
                ResultNextGoalResolver.GoalType goalType =
                    activityManager.RecommendedGoal.Type;
                int credits;
                int rep;
                int pixieXp;

                if (goalType == ResultNextGoalResolver.GoalType.Story &&
                    story != null && !story.IsComplete)
                {
                    credits = story.CurrentCreditsReward;
                    rep = story.CurrentReputationReward;
                    pixieXp = story.CurrentPixieXpReward;
                }
                else if (goalType == ResultNextGoalResolver.GoalType.Season &&
                         season != null && !season.IsComplete &&
                         season.IsSeasonOneActive)
                {
                    credits = season.CurrentCreditsReward;
                    rep = season.CurrentReputationReward;
                    pixieXp = season.CurrentPixieXpReward;
                }
                else
                {
                    return string.Empty;
                }

                return MotorCityLocalization.Format(
                    "ux.home.mission_reward",
                    credits, rep, pixieXp);
            }
        }

        public string PlayerExperienceNextMilestoneLine
        {
            get
            {
                if (garage == null && collection == null)
                    return MotorCityLocalization.Text("ux.sections.not_ready");

                string vehicleGoal = garage == null
                    ? string.Empty
                    : garage.NextVehicleLine;
                string collectionGoal = collection == null
                    ? string.Empty
                    : collection.HudLine;
                return string.Join(
                    "\n",
                    new[] { vehicleGoal, collectionGoal });
            }
        }

        public string PlayerExperienceGarageLine => BuildPlayerExperienceGarageDetail();

        public string PlayerExperienceAchievementsLine => BuildPlayerExperienceAchievementsDetail();

        public string PlayerExperienceNightClubLine => BuildPlayerExperienceNightClubDetail();

        private bool HasBlockingModalUi()
        {
            return
                pauseMenuOpen ||
                navigatorMenuOpen ||
                storeOpen ||
                clubOpen ||
                (garage != null &&
                 garage.IsOpen) ||
                (activityManager != null &&
                 activityManager.HasResult);
        }

        private void RefreshDrivingEnabledForUi()
        {
            bool blocked =
                HasBlockingModalUi();

            car?.SetDrivingBlocked(
                "ModalUi",
                blocked);

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

        private void BuildUi()
        {
            MotorCityUiEventSystem.EnsureUiEventSystem();
            MotorCityMusicRuntime.EnsureExists();
            MotorCityIconLibrary.PrewarmCore();

            font = MotorCityTypography.Regular;

            boldFont = MotorCityTypography.Bold;

            touchArtAssets =
                Resources.Load<MotorCityTouchArtAssets>(
                    "MotorCity/UI/MotorCityTouchArtAssets");

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

            RectTransform speedCanvasRoot =
                CreateHudSubCanvas(
                    safeAreaRoot,
                    "Speed Canvas");

            RectTransform statusCanvasRoot =
                CreateHudSubCanvas(
                    safeAreaRoot,
                    "Status Canvas");

            RectTransform driftCanvasRoot =
                CreateHudSubCanvas(
                    safeAreaRoot,
                    "Drift Canvas");

            BuildPlayerCard(safeAreaRoot);
            BuildCharacterCard(safeAreaRoot);
            BuildSeasonPanel(safeAreaRoot);
            BuildSpeedometer(speedCanvasRoot);
            BuildCountdownOverlay(safeAreaRoot);
            BuildStatus(statusCanvasRoot);
            BuildNavigator(safeAreaRoot);
            BuildNavigatorMenu(safeAreaRoot);
            BuildPauseMenu(safeAreaRoot);
            BuildDriftPanel(driftCanvasRoot);
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
            ApplyDrivingHudReferenceStyle(safeAreaRoot);
            canvasObject.AddComponent<GarageCanvasRefresh>();

            driftPanel.SetActive(false);
            activityResultOverlay.SetActive(false);
            garageOverlay.SetActive(false);
            SetClubOpen(false);
            SetStoreOpen(false);
            SetNavigatorMenuOpen(false);
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

        private static RectTransform CreateHudSubCanvas(
            Transform parent,
            string name)
        {
            GameObject root =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Canvas));

            root.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                root.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                Vector2.one;

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                Vector2.zero;

            Canvas canvas =
                root.GetComponent<Canvas>();

            canvas.overrideSorting =
                false;

            canvas.pixelPerfect =
                false;

            return rect;
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
