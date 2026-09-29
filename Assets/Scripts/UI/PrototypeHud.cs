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
        private WeekendEventSystem weekendEvents;
        private RewardedBonusSystem rewardedBonus;
        private CosmeticStoreSystem cosmeticStore;
        private bool storeOpen;
        private GameObject pauseOverlay;
        private Text pauseQualityText;
        private Text pauseAudioText;
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
        private Text storeNameText;
        private Text storeDescriptionText;
        private Text storePathText;
        private Text storeOwnershipText;
        private Text storeWalletText;
        private Image storeProductIcon;
        private Text clubEmblemText;
        private Text clubNameText;
        private Text clubDescriptionText;
        private Text clubWeeklyText;
        private RectTransform safeAreaRoot;
        private GameObject touchControlsRoot;
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

        private void Update()
        {
            if (moneyText == null)
                return;

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
                    new Vector2(150f, -196f),
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

        private static void MakeButtonTextCrisp(
            Text text)
        {
            if (text == null)
                return;

            // Button labels are short and already sized for their controls.
            // Avoid Best Fit here: dynamic shrinking can land on fractional
            // glyph sizes and makes LegacyRuntime text look soft/pixelated.
            text.resizeTextForBestFit =
                false;

            text.horizontalOverflow =
                HorizontalWrapMode.Overflow;

            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            RectTransform rect =
                text.rectTransform;

            Vector2 position =
                rect.anchoredPosition;

            rect.anchoredPosition =
                new Vector2(
                    Mathf.Round(position.x),
                    Mathf.Round(position.y));

            Vector2 size =
                rect.sizeDelta;

            rect.sizeDelta =
                new Vector2(
                    Mathf.Round(size.x),
                    Mathf.Round(size.y));
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

            Outline outline =
                go.AddComponent<Outline>();

            outline.effectColor =
                new Color(
                    BlueAccent.r,
                    BlueAccent.g,
                    BlueAccent.b,
                    0.22f);

            outline.effectDistance =
                new Vector2(1f, -1f);

            outline.useGraphicAlpha = true;
            return rect;
        }

        private void ClearPanelChrome(
            RectTransform panel)
        {
            if (panel == null)
                return;

            Image image =
                panel.GetComponent<Image>();

            if (image != null)
            {
                image.color =
                    Color.clear;
                image.raycastTarget =
                    false;
            }

            Outline outline =
                panel.GetComponent<Outline>();

            if (outline != null)
            {
                outline.enabled =
                    false;
            }
        }

        private Texture2D ResolveVillePanelTexture(
            string panelName)
        {
            if (uiThemeAssets == null)
                return null;

            return panelName switch
            {
                "Character Card" =>
                    uiThemeAssets.characterPanel,

                "Activity Status" =>
                    uiThemeAssets.statusPanel,

                "Drift HUD" =>
                    uiThemeAssets.driftPanel,

                "Navigation Target Strip" =>
                    uiThemeAssets.targetPanel,

                _ =>
                    uiThemeAssets.rectanglePanel
            };
        }


        private static Sprite GetModalButtonSprite(
            Texture2D texture)
        {
            if (texture == null)
                return null;

            if (modalButtonSprite != null &&
                modalButtonSpriteSource == texture)
            {
                return modalButtonSprite;
            }

            modalButtonSprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        texture.width,
                        texture.height),
                    new Vector2(
                        0.5f,
                        0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);

            modalButtonSprite.name =
                "Motor City Ville Modal Button";
            modalButtonSprite.hideFlags =
                HideFlags.DontSave;
            modalButtonSpriteSource =
                texture;

            return modalButtonSprite;
        }

        private void ApplyModalPanelTexture(
            RectTransform panel)
        {
            if (panel == null)
                return;

            ClearPanelChrome(
                panel);

            Texture2D texture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.modalPanel;

            if (texture == null)
                return;

            CreatePanelEdgeGlow(
                panel,
                texture,
                ResolvePanelGlowColor(
                    panel.name),
                "Ville Modal Glow");

            GameObject backgroundObject =
                new(
                    "Ville Modal Background",
                    typeof(RectTransform),
                    typeof(Image));

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

            Image image =
                backgroundObject.GetComponent<Image>();

            image.sprite =
                GetSlicedPanelSprite(
                    texture);
            image.type =
                Image.Type.Sliced;
            image.fillCenter =
                true;
            image.pixelsPerUnitMultiplier =
                1f;
            image.color =
                Color.white;
            image.raycastTarget =
                false;
        }

        private void ApplyVillePanelTexture(
            RectTransform panel,
            float alpha)
        {
            if (panel == null)
                return;

            ClearPanelChrome(
                panel);

            Texture2D texture =
                ResolveVillePanelTexture(
                    panel.name);

            if (texture == null)
                return;

            CreatePanelEdgeGlow(
                panel,
                texture,
                ResolvePanelGlowColor(
                    panel.name),
                "Ville Panel Glow");

            GameObject backgroundObject =
                new(
                    "Ville Panel Background",
                    typeof(RectTransform),
                    typeof(Image));

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

            Image image =
                backgroundObject.GetComponent<Image>();

            image.sprite =
                GetSlicedPanelSprite(
                    texture);
            image.type =
                Image.Type.Sliced;
            image.fillCenter =
                true;
            image.pixelsPerUnitMultiplier =
                1f;
            image.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    Mathf.Clamp01(alpha));
            image.raycastTarget =
                false;
        }

        private static Sprite GetSlicedPanelSprite(
            Texture2D texture)
        {
            if (texture == null)
                return null;

            if (slicedPanelSprites.TryGetValue(
                    texture,
                    out Sprite cached) &&
                cached != null)
            {
                return cached;
            }

            float minDimension =
                Mathf.Min(
                    texture.width,
                    texture.height);

            float border =
                Mathf.Clamp(
                    minDimension * 0.16f,
                    12f,
                    64f);

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        texture.width,
                        texture.height),
                    new Vector2(
                        0.5f,
                        0.5f),
                    100f,
                    0u,
                    SpriteMeshType.FullRect,
                    new Vector4(
                        border,
                        border,
                        border,
                        border));

            sprite.name =
                texture.name +
                " (Runtime 9-slice)";

            slicedPanelSprites[texture] =
                sprite;

            return sprite;
        }

        private static Color ResolvePanelGlowColor(
            string panelName)
        {
            if (string.IsNullOrWhiteSpace(
                    panelName))
            {
                return new Color(
                    0.34f,
                    0.53f,
                    1f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Garage",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    0.64f,
                    0.42f,
                    1f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Store",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    0.30f,
                    0.58f,
                    1f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Club",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    0.24f,
                    0.82f,
                    1f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Drift",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    1f,
                    0.46f,
                    0.14f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Result",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    0.50f,
                    0.62f,
                    1f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Pause",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    0.38f,
                    0.52f,
                    0.92f,
                    1f);
            }

            return new Color(
                0.34f,
                0.53f,
                1f,
                1f);
        }

        private static void CreatePanelEdgeGlow(
            RectTransform panel,
            Texture2D texture,
            Color color,
            string objectName)
        {
            if (panel == null ||
                texture == null)
            {
                return;
            }

            Vector2 size =
                panel.rect.size;

            bool compactPanel =
                size.y <= 72f ||
                size.x <= 340f;

            float outerExpansion =
                compactPanel
                    ? 8f
                    : 14f;

            float innerExpansion =
                compactPanel
                    ? 4f
                    : 7f;

            float outerAlpha =
                compactPanel
                    ? 0.028f
                    : 0.075f;

            float innerAlpha =
                compactPanel
                    ? 0.065f
                    : 0.16f;

            CreatePanelGlowLayer(
                panel,
                texture,
                color,
                objectName + " Outer",
                outerExpansion,
                outerAlpha);

            CreatePanelGlowLayer(
                panel,
                texture,
                color,
                objectName + " Inner",
                innerExpansion,
                innerAlpha);
        }

        private static void CreatePanelGlowLayer(
            RectTransform panel,
            Texture2D texture,
            Color color,
            string objectName,
            float expansion,
            float alpha)
        {
            GameObject glowObject =
                new(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image));

            glowObject.transform.SetParent(
                panel,
                false);

            glowObject.transform.SetAsFirstSibling();

            RectTransform rect =
                glowObject.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;
            rect.anchorMax =
                Vector2.one;
            rect.offsetMin =
                new Vector2(
                    -expansion,
                    -expansion);
            rect.offsetMax =
                new Vector2(
                    expansion,
                    expansion);

            Image image =
                glowObject.GetComponent<Image>();

            image.sprite =
                GetSlicedPanelSprite(
                    texture);
            image.type =
                Image.Type.Sliced;
            image.fillCenter =
                true;
            image.pixelsPerUnitMultiplier =
                1f;

            image.color =
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    alpha);

            image.raycastTarget =
                false;
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

        private static int RuntimeTextMinSize(
            int fontSize)
        {
            if (fontSize <= 11)
            {
                return
                    Mathf.Max(
                        9,
                        fontSize - 2);
            }

            return
                Mathf.Max(
                    11,
                    Mathf.RoundToInt(
                        fontSize * 0.78f));
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

            bool useTrueBold =
                fontStyle == FontStyle.Bold &&
                boldFont != null;

            text.font =
                useTrueBold
                    ? boldFont
                    : font;
            text.fontSize = fontSize;
            text.fontStyle =
                useTrueBold
                    ? FontStyle.Normal
                    : fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.alignByGeometry = true;
            text.lineSpacing = 1f;

            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            // Keep one readable typography rule across the runtime HUD.
            // Dynamic text may shrink, but never all the way down to tiny
            // 8–9 px glyphs unless that size was requested explicitly.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize =
                RuntimeTextMinSize(
                    fontSize);
            text.resizeTextMaxSize =
                fontSize;

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
