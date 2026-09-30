#if UNITY_EDITOR
using MotorCity.Input;
using MotorCity.Vehicle;
using MotorCity.World;
using MotorCity.CameraSystem;
using MotorCity.UI;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class AdminDebugPanel : MonoBehaviour
    {
        private const int WindowId = 73921;

        private static readonly string[] Tabs =
        {
            "ОБЗОР",
            "ПРОГРЕСС",
            "МАШИНЫ",
            "АКТИВНОСТИ",
            "МИР",
            "СИСТЕМЫ"
        };

        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private DisciplineReputationSystem disciplines;
        private VehicleMasterySystem mastery;
        private VehicleRosterSystem roster;
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
        private ActivityManager activities;
        private ArcadeCarController car;
        private DeliveryActivity delivery;
        private DriftChallenge drift;
        private StreetSprintActivity sprint;
        private CircuitRaceActivity circuit;
        private StoryMissionSystem story;
        private FirstSessionOnboardingSystem onboarding;

        private SeasonSystem season;
        private ClubSystem club;
        private TurboPetSystem turbo;
        private DailyAdventureSystem daily;
        private AchievementSystem achievements;
        private PhotoHuntSystem photoHunt;
        private CityProfessionSystem professions;
        private CarWashJobSystem carWash;
        private TowTruckJobSystem towTruck;
        private DiscoverySystem discoveries;
        private SpeedTrapSystem speedTraps;
        private DriftSpotSystem driftSpots;
        private DayNightCycleController dayNight;

        private bool visible;
        private int selectedTab;
        private Vector2 scroll;
        private string lastAction = "Готово";

        private Rect windowRect =
            new Rect(
                18f,
                62f,
                760f,
                760f);

        public void Initialize(
            PlayerWallet playerWallet,
            PlayerReputation playerReputation,
            DisciplineReputationSystem disciplineSystem,
            VehicleMasterySystem masterySystem,
            VehicleRosterSystem vehicleRoster,
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
            ActivityManager manager,
            ArcadeCarController targetCar,
            DeliveryActivity deliveryActivity,
            DriftChallenge driftActivity,
            StreetSprintActivity sprintActivity,
            CircuitRaceActivity circuitActivity,
            StoryMissionSystem storySystem,
            FirstSessionOnboardingSystem onboardingSystem)
        {
            wallet = playerWallet;
            reputation = playerReputation;
            disciplines = disciplineSystem;
            mastery = masterySystem;
            roster = vehicleRoster;
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
            activities = manager;
            car = targetCar;
            delivery = deliveryActivity;
            drift = driftActivity;
            sprint = sprintActivity;
            circuit = circuitActivity;
            story = storySystem;
            onboarding = onboardingSystem;

            season = GetComponent<SeasonSystem>();
            club = GetComponent<ClubSystem>();
            turbo = GetComponent<TurboPetSystem>();
            daily = GetComponent<DailyAdventureSystem>();
            achievements = GetComponent<AchievementSystem>();
            photoHunt = GetComponent<PhotoHuntSystem>();
            professions = GetComponent<CityProfessionSystem>();
            carWash = GetComponent<CarWashJobSystem>();
            towTruck = GetComponent<TowTruckJobSystem>();
            discoveries = GetComponent<DiscoverySystem>();
            speedTraps = GetComponent<SpeedTrapSystem>();
            driftSpots = GetComponent<DriftSpotSystem>();

            dayNight =
                Object.FindAnyObjectByType<
                    DayNightCycleController>();
        }

        private void Update()
        {
            if (MotorCityInput.AdminTogglePressed)
            {
                visible =
                    !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            windowRect =
                GUI.Window(
                    WindowId,
                    windowRect,
                    DrawWindow,
                    "MOTOR CITY - QA ADMIN");
        }

        private void DrawWindow(
            int id)
        {
            selectedTab =
                GUILayout.Toolbar(
                    selectedTab,
                    Tabs,
                    GUILayout.Height(
                        30f));

            GUILayout.Space(
                6f);

            DrawHeader();

            scroll =
                GUILayout.BeginScrollView(
                    scroll,
                    false,
                    true);

            switch (selectedTab)
            {
                case 0:
                    DrawOverview();
                    break;
                case 1:
                    DrawProgress();
                    break;
                case 2:
                    DrawVehicles();
                    break;
                case 3:
                    DrawActivities();
                    break;
                case 4:
                    DrawWorld();
                    break;
                default:
                    DrawSystems();
                    break;
            }

            GUILayout.Space(
                12f);

            Separator();

            GUILayout.Label(
                "ПОСЛЕДНЕЕ: " +
                lastAction);

            GUILayout.Label(
                "F10 / TILDE - закрыть панель");

            GUILayout.EndScrollView();

            GUI.DragWindow(
                new Rect(
                    0f,
                    0f,
                    10000f,
                    26f));
        }

        private void DrawHeader()
        {
            GUILayout.Label(
                "КР " +
                Value(
                    wallet == null
                        ? 0
                        : wallet.Credits) +
                "   |   РЕП " +
                Value(
                    reputation == null
                        ? 0
                        : reputation.Reputation) +
                "   |   " +
                (roster == null
                    ? "-"
                    : roster.SelectedName));

            if (car != null)
            {
                GUILayout.Label(
                    "СКОРОСТЬ " +
                    car.SpeedKph.ToString(
                        "0") +
                    " км/ч   |   " +
                    car.CurrentDriveMode +
                    "   |   КОЛЁСА " +
                    car.GroundedWheels +
                    "/4");
            }

            if (activities != null)
            {
                GUILayout.Label(
                    activities.IsBusy
                        ? "АКТИВНОСТЬ: " +
                          activities.ActiveId
                        : "АКТИВНОСТЬ: свободно");
            }

            Separator();
        }

        private void DrawOverview()
        {
            Section(
                "ЭКОНОМИКА");

            GUILayout.BeginHorizontal();

            if (Button("+10 000 КР"))
            {
                wallet?.AddCredits(
                    10000);

                lastAction =
                    "+10 000 КР";
            }

            if (Button("КР 1 000 000"))
            {
                wallet?.SetCredits(
                    1000000);

                lastAction =
                    "КР = 1 000 000";
            }

            if (Button("КР 0"))
            {
                wallet?.SetCredits(
                    0);

                lastAction =
                    "КР = 0";
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            RepButton(
                "РЕП 0",
                0);

            RepButton(
                "РЕП 2 500",
                2500);

            RepButton(
                "РЕП 10 000",
                10000);

            GUILayout.EndHorizontal();

            Section(
                "КЛЮЧЕВОЙ ПРОГРЕСС");

            GUILayout.Label(
                "Первые шаги: " +
                (onboarding == null
                    ? "-"
                    : onboarding.IsComplete
                        ? "ГОТОВО"
                        : onboarding.CurrentStepNumber +
                          "/" +
                          onboarding.StepCount));

            GUILayout.Label(
                "Путь новичка: " +
                (story == null
                    ? "-"
                    : story.IsComplete
                        ? "ГОТОВО"
                        : story.CurrentMissionNumber +
                          "/" +
                          story.MissionCount));

            GUILayout.Label(
                "Сезон: " +
                (season == null
                    ? "-"
                    : season.IsComplete
                        ? "ГОТОВО"
                        : season.CurrentMissionNumber +
                          "/" +
                          season.MissionCount +
                          "  " +
                          season.CurrentMissionProgress +
                          "/" +
                          season.CurrentMissionTarget));

            GUILayout.Label(
                "Клуб: " +
                (club == null
                    ? "-"
                    : club.HasClub
                        ? club.CurrentClubName +
                          "  " +
                          club.WeeklyContribution +
                          "/" +
                          club.WeeklyTarget
                        : "НЕТ КЛУБА"));

            if (turbo != null)
            {
                GUILayout.Label(
                    "Турбо: ур. " +
                    turbo.Level +
                    "  XP " +
                    Value(
                        turbo.Xp));
            }

            if (achievements != null)
            {
                GUILayout.Label(
                    "Достижения: " +
                    achievements.UnlockedCount);
            }

            Section(
                "БЫСТРЫЕ QA ДЕЙСТВИЯ");

            GUILayout.BeginHorizontal();

            if (Button("ОТМЕНИТЬ АКТИВНОСТЬ"))
                CancelAllActivities();

            if (Button("СПАСТИ МАШИНУ"))
                RescueCar();

            if (Button("ЗАКРЫТЬ RESULT"))
            {
                if (activities != null &&
                    activities.HasResult)
                {
                    activities.DismissResult();
                    lastAction =
                        "Result закрыт";
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(
                8f);

            if (Button("СБРОСИТЬ ПРОГРЕСС ДЛЯ ЧИСТОГО QA"))
            {
                ResetProgressForTesting();
            }
        }

        private void DrawProgress()
        {
            Section(
                "ПЕРВЫЕ ШАГИ");

            GUILayout.Label(
                onboarding == null
                    ? "Система не найдена"
                    : onboarding.IsComplete
                        ? "ЗАВЕРШЕНО"
                        : "Шаг " +
                          onboarding.CurrentStepNumber +
                          "/" +
                          onboarding.StepCount +
                          " - " +
                          onboarding.ObjectiveLine);

            GUILayout.BeginHorizontal();

            if (Button("+1 ШАГ"))
            {
                onboarding?.AdvanceStepForTesting();
                lastAction =
                    "Onboarding +1";
            }

            if (Button("ЗАВЕРШИТЬ"))
            {
                onboarding?.CompleteForTesting();
                lastAction =
                    "Onboarding завершён";
            }

            if (Button("СБРОС"))
            {
                onboarding?.ResetForTesting();
                lastAction =
                    "Onboarding сброшен";
            }

            GUILayout.EndHorizontal();

            Section(
                "ПУТЬ НОВИЧКА");

            GUILayout.Label(
                story == null
                    ? "Система не найдена"
                    : story.IsComplete
                        ? "ЗАВЕРШЕНО"
                        : "Миссия " +
                          story.CurrentMissionNumber +
                          "/" +
                          story.MissionCount +
                          " - " +
                          story.CurrentMissionTitle);

            GUILayout.BeginHorizontal();

            if (Button("+1 МИССИЯ"))
            {
                story?.AdvanceMissionForTesting();
                lastAction =
                    "Story +1";
            }

            if (Button("СБРОС"))
            {
                story?.ResetForTesting();
                lastAction =
                    "Story сброшен";
            }

            GUILayout.EndHorizontal();

            Section(
                "СЕЗОН");

            if (season != null)
            {
                GUILayout.Label(
                    season.IsComplete
                        ? "СЕЗОН ЗАВЕРШЁН"
                        : "Миссия " +
                          season.CurrentMissionNumber +
                          "/" +
                          season.MissionCount +
                          " - " +
                          season.CurrentMissionTitle);

                GUILayout.Label(
                    "Прогресс " +
                    season.CurrentMissionProgress +
                    "/" +
                    season.CurrentMissionTarget +
                    "   |   дней " +
                    season.DaysRemaining);
            }

            GUILayout.BeginHorizontal();

            if (Button("+1 МИССИЯ"))
            {
                season?.AdvanceMissionForTesting();
                lastAction =
                    "Season +1";
            }

            if (Button("ЗАВЕРШИТЬ"))
            {
                season?.CompleteForTesting();
                lastAction =
                    "Season завершён";
            }

            if (Button("СБРОС"))
            {
                season?.ResetForTesting();
                lastAction =
                    "Season сброшен";
            }

            GUILayout.EndHorizontal();

            Section(
                "КЛУБ");

            GUILayout.Label(
                club == null
                    ? "Система не найдена"
                    : club.HasClub
                        ? club.CurrentClubName +
                          " - " +
                          club.WeeklyLine
                        : "Клуб не выбран");

            for (int row = 0;
                 row < 2;
                 row++)
            {
                GUILayout.BeginHorizontal();

                for (int column = 0;
                     column < 3;
                     column++)
                {
                    int index =
                        row * 3 +
                        column;

                    if (Button(
                            "КЛУБ " +
                            (index + 1)))
                    {
                        club?.SetClubForTesting(
                            index);

                        lastAction =
                            "Выбран клуб " +
                            (index + 1);
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();

            if (Button("WEEKLY ГОТОВО"))
            {
                club?.CompleteWeeklyForTesting();
                lastAction =
                    "Club weekly завершён";
            }

            if (Button("СБРОС КЛУБА"))
            {
                club?.ResetForTesting();
                lastAction =
                    "Club сброшен";
            }

            GUILayout.EndHorizontal();

            Section(
                "МЕТА ПРОГРЕСС");

            DrawDiscipline(
                "ГОНКИ",
                DisciplineType.Racing);

            DrawDiscipline(
                "ДРИФТ",
                DisciplineType.Drift);

            DrawDiscipline(
                "ДОСТАВКА",
                DisciplineType.Delivery);

            GUILayout.BeginHorizontal();

            if (Button("КАРЬЕРА 0"))
                SetCareer(
                    0);

            if (Button("КАРЬЕРА 2"))
                SetCareer(
                    2);

            if (Button("КАРЬЕРА 3"))
                SetCareer(
                    3);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("ЛЕГЕНДА ОТКРЫТЬ"))
            {
                legends?.UnlockCurrentForTesting();
                lastAction =
                    "Легенда открыта";
            }

            if (Button("ЛЕГЕНДА ГОТОВО"))
            {
                legends?.CompleteCurrentForTesting();
                lastAction =
                    "Легенда завершена";
            }

            if (Button("ЛЕГЕНДЫ СБРОС"))
            {
                legends?.ResetForTesting();
                lastAction =
                    "Легенды сброшены";
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("КОНТРАКТ ГОТОВО"))
            {
                contracts?.CompleteCurrentForTesting();
                lastAction =
                    "Контракт завершён";
            }

            if (Button("LIVE NEXT"))
            {
                liveEvents?.NextEventForTesting();
                lastAction =
                    "Live event переключён";
            }

            if (Button("LIVE ГОТОВО"))
            {
                liveEvents?.CompleteCurrentForTesting();
                lastAction =
                    "Live event завершён";
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("ПОДПОЛЬЕ +40"))
            {
                underground?.AddCredForTesting(
                    40);

                lastAction =
                    "Street cred +40";
            }

            if (Button("ПОДПОЛЬЕ ОТКРЫТЬ"))
            {
                underground?.UnlockCurrentForTesting();
                lastAction =
                    "Underground открыт";
            }

            if (Button("РИСК +50"))
            {
                cityRisk?.AddAttentionForTesting(
                    50f);

                lastAction =
                    "Риск +50";
            }

            if (Button("РИСК 0"))
            {
                cityRisk?.ClearForTesting();
                lastAction =
                    "Риск сброшен";
            }

            GUILayout.EndHorizontal();
        }

        private void DrawVehicles()
        {
            Section(
                "АВТОПАРК");

            if (roster == null)
            {
                GUILayout.Label(
                    "VehicleRosterSystem не найден");
            }
            else
            {
                GUILayout.Label(
                    "Текущая: " +
                    roster.SelectedName +
                    "   |   " +
                    roster.GetStatsLine());

                int columns = 2;

                for (int i = 0;
                     i < roster.VehicleCount;
                     i++)
                {
                    if (i % columns == 0)
                        GUILayout.BeginHorizontal();

                    int index = i;

                    string label =
                        (i + 1) +
                        ". " +
                        roster.GetVehicleId(
                                i)
                            .ToUpperInvariant();

                    if (Button(label))
                        SelectVehicle(
                            index);

                    if (i % columns ==
                        columns - 1 ||
                        i ==
                        roster.VehicleCount - 1)
                    {
                        GUILayout.EndHorizontal();
                    }
                }
            }

            Section(
                "АПГРЕЙДЫ");

            GUILayout.BeginHorizontal();

            UpgradeButton(
                "ВСЁ 0/5",
                0);

            UpgradeButton(
                "ВСЁ 3/5",
                3);

            UpgradeButton(
                "ВСЁ 5/5",
                5);

            GUILayout.EndHorizontal();

            if (garage != null)
            {
                GUILayout.Label(
                    "Двигатель " +
                    garage.EngineLevel +
                    "/5   |   Сцепление " +
                    garage.GripLevel +
                    "/5   |   Стабильность " +
                    garage.StabilityLevel +
                    "/5");
            }

            Section(
                "МАСТЕРСТВО");

            GUILayout.BeginHorizontal();

            if (Button("УР. 1"))
                SetMastery(
                    1);

            if (Button("УР. 5"))
                SetMastery(
                    5);

            if (Button("УР. 10"))
                SetMastery(
                    10);

            if (Button("ВСЕ 10"))
            {
                mastery?.SetAllVehicleLevelsForTesting(
                    10);

                lastAction =
                    "Все mastery = 10";
            }

            GUILayout.EndHorizontal();

            Section(
                "КАСТОМИЗАЦИЯ");

            VehicleCustomizationSystem customization =
                GetComponent<
                    VehicleCustomizationSystem>();

            GUILayout.BeginHorizontal();

            if (Button("ЦВЕТ"))
            {
                customization?.CycleBodyColor();
                lastAction =
                    "Следующий цвет";
            }

            if (Button("ДИСКИ"))
            {
                customization?.CycleWheelStyle();
                lastAction =
                    "Следующие диски";
            }

            if (Button("НЕОН"))
            {
                customization?.CycleNeon();
                lastAction =
                    "Следующий неон";
            }

            GUILayout.EndHorizontal();

            if (customization != null)
            {
                GUILayout.Label(
                    customization.GarageLine);
            }

            Section(
                "ТУРБО");

            GUILayout.Label(
                turbo == null
                    ? "TurboPetSystem не найден"
                    : "Ур. " +
                      turbo.Level +
                      "   XP " +
                      Value(
                          turbo.Xp) +
                      "   " +
                      turbo.MoodName);

            GUILayout.BeginHorizontal();

            if (Button("+100 XP"))
            {
                turbo?.AddXp(
                    100);

                lastAction =
                    "Turbo +100 XP";
            }

            if (Button("+1000 XP"))
            {
                turbo?.AddXp(
                    1000);

                lastAction =
                    "Turbo +1000 XP";
            }

            GUILayout.EndHorizontal();
        }

        private void DrawActivities()
        {
            Section(
                "ОСНОВНЫЕ АКТИВНОСТИ");

            GUILayout.BeginHorizontal();

            if (Button("ДОСТАВКА"))
                TeleportRoute(
                    CityAssetRuntimeInstaller.DeliveryRoute);

            if (Button("ДРИФТ"))
                Teleport(
                    drift == null
                        ? CityAssetRuntimeInstaller.DriftChallengePoint
                        : drift.ZoneCenter,
                    Quaternion.identity);

            if (Button("СПРИНТ"))
                TeleportRoute(
                    CityAssetRuntimeInstaller.SprintRoute);

            if (Button("КОЛЬЦО"))
                TeleportRoute(
                    CityAssetRuntimeInstaller.CircuitRoute);

            GUILayout.EndHorizontal();

            Section(
                "РАБОТЫ");

            GUILayout.BeginHorizontal();

            if (Button("МОЙКА") &&
                carWash != null)
            {
                Teleport(
                    carWash.StartPoint,
                    Quaternion.identity);
            }

            if (Button("ЭВАКУАТОР") &&
                towTruck != null)
            {
                Teleport(
                    towTruck.StartPoint,
                    Quaternion.identity);
            }

            if (Button("ПОДПОЛЬЕ"))
            {
                Teleport(
                    CityAssetRuntimeInstaller.UndergroundMeetingPoint,
                    Quaternion.identity);
            }

            GUILayout.EndHorizontal();

            if (professions != null &&
                professions.StartCount > 0)
            {
                GUILayout.Label(
                    "Профессии: " +
                    professions.TotalCompleted +
                    " завершено, ур. " +
                    professions.ProfessionLevel);

                for (int i = 0;
                     i < professions.StartCount;
                     i++)
                {
                    int index = i;

                    if (Button(
                            "РАБОТА " +
                            (i + 1) +
                            " - " +
                            professions.GetStartName(
                                i)))
                    {
                        Teleport(
                            professions.GetStartPoint(
                                index),
                            Quaternion.identity);
                    }
                }
            }

            Section(
                "СВОБОДНЫЕ АКТИВНОСТИ");

            GUILayout.Label(
                "Discovery: " +
                (discoveries == null
                    ? "-"
                    : discoveries.FoundCount +
                      "/" +
                      discoveries.DiscoveryCount));

            GUILayout.Label(
                "Photo Hunt: " +
                (photoHunt == null
                    ? "-"
                    : photoHunt.TotalCaptured.ToString()));

            GUILayout.Label(
                "Speed Traps: " +
                (speedTraps == null
                    ? "-"
                    : speedTraps.TrapCount.ToString()));

            GUILayout.Label(
                "Drift Spots: " +
                (driftSpots == null
                    ? "-"
                    : driftSpots.SpotCount.ToString()));

            Section(
                "УПРАВЛЕНИЕ");

            GUILayout.BeginHorizontal();

            if (Button("ОТМЕНИТЬ ВСЁ"))
                CancelAllActivities();

            if (Button("СПАСТИ"))
                RescueCar();

            if (Button("RESULT ЗАКРЫТЬ"))
            {
                if (activities != null &&
                    activities.HasResult)
                {
                    activities.DismissResult();
                    lastAction =
                        "Result закрыт";
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawWorld()
        {
            Section(
                "БЫСТРЫЕ ТЕЛЕПОРТЫ");

            GUILayout.BeginHorizontal();

            if (Button("СТАРТ"))
            {
                Teleport(
                    CityAssetRuntimeInstaller.PlayerSpawnPoint,
                    CityAssetRuntimeInstaller.PlayerSpawnRotation);
            }

            if (Button("ГАРАЖ"))
            {
                Teleport(
                    CityAssetRuntimeInstaller.GaragePoint,
                    CityAssetRuntimeInstaller.GarageSpawnRotation);
            }

            if (Button("ПОДПОЛЬЕ"))
            {
                Teleport(
                    CityAssetRuntimeInstaller.UndergroundMeetingPoint,
                    Quaternion.identity);
            }

            GUILayout.EndHorizontal();

            if (discoveries != null &&
                discoveries.DiscoveryCount > 0)
            {
                GUILayout.BeginHorizontal();

                if (Button("DISCOVERY 1"))
                {
                    Teleport(
                        discoveries.GetDiscoveryPosition(
                            0),
                        Quaternion.identity);
                }

                int last =
                    discoveries.DiscoveryCount - 1;

                if (Button("DISCOVERY LAST"))
                {
                    Teleport(
                        discoveries.GetDiscoveryPosition(
                            last),
                        Quaternion.identity);
                }

                GUILayout.EndHorizontal();
            }

            Section(
                "ВРЕМЯ СУТОК");

            GUILayout.BeginHorizontal();

            TimeButton(
                "УТРО",
                0.25f);

            TimeButton(
                "ДЕНЬ",
                0.50f);

            TimeButton(
                "ВЕЧЕР",
                0.72f);

            TimeButton(
                "НОЧЬ",
                0.88f);

            GUILayout.EndHorizontal();

            Section(
                "ФИЗИКА МАШИНЫ");

            if (car != null)
            {
                GUILayout.Label(
                    "Speed " +
                    car.SpeedKph.ToString(
                        "0.0") +
                    " km/h   |   Slip " +
                    car.SlipAngleDegrees.ToString(
                        "0.0") +
                    "°   |   Rear slip " +
                    car.RearSidewaysSlip.ToString(
                        "0.00"));

                GUILayout.Label(
                    "Grounded " +
                    car.GroundedWheels +
                    "/4   |   Sliding " +
                    (car.IsSliding
                        ? "YES"
                        : "NO") +
                    "   |   Prometeo " +
                    (car.HasPrometeoPhysics
                        ? "YES"
                        : "NO"));
            }

            GUILayout.BeginHorizontal();

            if (Button("СМЕНИТЬ РЕЖИМ"))
            {
                car?.CycleDriveMode();
                lastAction =
                    "Режим езды изменён";
            }

            if (Button("СПАСТИ МАШИНУ"))
                RescueCar();

            GUILayout.EndHorizontal();
        }

        private void DrawSystems()
        {
            Section(
                "КЛЮЧЕВЫЕ СИСТЕМЫ");

            SystemLine(
                "Onboarding",
                onboarding != null,
                onboarding == null
                    ? string.Empty
                    : onboarding.IsComplete
                        ? "complete"
                        : onboarding.CurrentStepNumber +
                          "/" +
                          onboarding.StepCount);

            SystemLine(
                "Story",
                story != null,
                story == null
                    ? string.Empty
                    : story.IsComplete
                        ? "complete"
                        : story.CurrentMissionNumber +
                          "/" +
                          story.MissionCount);

            SystemLine(
                "Season",
                season != null,
                season == null
                    ? string.Empty
                    : season.IsComplete
                        ? "complete"
                        : season.CurrentMissionNumber +
                          "/" +
                          season.MissionCount);

            SystemLine(
                "Club",
                club != null,
                club == null
                    ? string.Empty
                    : club.HasClub
                        ? club.CurrentClubName
                        : "no club");

            SystemLine(
                "Daily",
                daily != null,
                daily == null
                    ? string.Empty
                    : daily.CompletedTasks +
                      " tasks");

            SystemLine(
                "Achievements",
                achievements != null,
                achievements == null
                    ? string.Empty
                    : achievements.UnlockedCount.ToString());

            SystemLine(
                "Photo Hunt",
                photoHunt != null,
                photoHunt == null
                    ? string.Empty
                    : photoHunt.TotalCaptured.ToString());

            SystemLine(
                "Professions",
                professions != null,
                professions == null
                    ? string.Empty
                    : professions.TotalCompleted.ToString());

            SystemLine(
                "Discovery",
                discoveries != null,
                discoveries == null
                    ? string.Empty
                    : discoveries.FoundCount +
                      "/" +
                      discoveries.DiscoveryCount);

            SystemLine(
                "Speed Traps",
                speedTraps != null,
                speedTraps == null
                    ? string.Empty
                    : speedTraps.TrapCount.ToString());

            SystemLine(
                "Drift Spots",
                driftSpots != null,
                driftSpots == null
                    ? string.Empty
                    : driftSpots.SpotCount.ToString());

            SystemLine(
                "Legends",
                legends != null,
                legends == null
                    ? string.Empty
                    : legends.AdminLine);

            SystemLine(
                "Contracts",
                contracts != null,
                contracts == null
                    ? string.Empty
                    : contracts.AdminLine);

            SystemLine(
                "Live Events",
                liveEvents != null,
                liveEvents == null
                    ? string.Empty
                    : liveEvents.AdminLine);

            SystemLine(
                "Underground",
                underground != null,
                underground == null
                    ? string.Empty
                    : underground.AdminLine);

            SystemLine(
                "Risk",
                cityRisk != null,
                cityRisk == null
                    ? string.Empty
                    : cityRisk.AdminLine);

            Section(
                "ВСЕ GAMEPLAY COMPONENTS");

            MonoBehaviour[] components =
                GetComponents<MonoBehaviour>();

            int enabledCount = 0;

            foreach (MonoBehaviour component in
                     components)
            {
                if (component == null ||
                    component == this)
                {
                    continue;
                }

                if (component.enabled)
                    enabledCount++;

                GUILayout.Label(
                    (component.enabled
                        ? "[ON] "
                        : "[OFF] ") +
                    component.GetType().Name);
            }

            GUILayout.Label(
                "Активных компонентов: " +
                enabledCount +
                "/" +
                Mathf.Max(
                    0,
                    components.Length - 1));
        }

        private void DrawDiscipline(
            string label,
            DisciplineType type)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(
                label,
                GUILayout.Width(
                    100f));

            if (Button("УР. 1"))
            {
                disciplines?.SetLevelForTesting(
                    type,
                    1);

                lastAction =
                    label +
                    " = 1";
            }

            if (Button("УР. 5"))
            {
                disciplines?.SetLevelForTesting(
                    type,
                    5);

                lastAction =
                    label +
                    " = 5";
            }

            if (Button("УР. 10"))
            {
                disciplines?.SetLevelForTesting(
                    type,
                    10);

                lastAction =
                    label +
                    " = 10";
            }

            GUILayout.EndHorizontal();
        }

        private void RepButton(
            string label,
            int value)
        {
            if (!Button(label))
                return;

            reputation?.SetReputation(
                value);

            lastAction =
                "РЕП = " +
                Value(
                    value);
        }

        private void SetCareer(
            int stage)
        {
            career?.SetStageForTesting(
                stage);

            lastAction =
                "Карьера = " +
                stage;
        }

        private void SetMastery(
            int level)
        {
            mastery?.SetCurrentLevelForTesting(
                level);

            lastAction =
                "Mastery = " +
                level;
        }

        private void UpgradeButton(
            string label,
            int level)
        {
            if (!Button(label))
                return;

            garage?.SetAllUpgradeLevelsForTesting(
                level);

            lastAction =
                "Апгрейды = " +
                level +
                "/5";
        }

        private void TimeButton(
            string label,
            float time)
        {
            if (!Button(label))
                return;

            dayNight?.SetTimeOfDay(
                time);

            lastAction =
                "Время: " +
                label;
        }

        private void SelectVehicle(
            int index)
        {
            string status =
                string.Empty;

            if (roster != null &&
                roster.SelectVehicleForTesting(
                    index,
                    out status))
            {
                lastAction =
                    string.IsNullOrWhiteSpace(
                        status)
                        ? "Машина " +
                          (index + 1)
                        : status;

                return;
            }

            lastAction =
                string.IsNullOrWhiteSpace(
                    status)
                    ? "Не удалось выбрать машину"
                    : status;
        }

        private void TeleportRoute(
            Vector3[] route)
        {
            if (route == null ||
                route.Length == 0)
            {
                lastAction =
                    "Маршрут отсутствует";

                return;
            }

            Vector3 forward =
                route.Length > 1
                    ? route[1] -
                      route[0]
                    : Vector3.forward;

            forward.y = 0f;

            Quaternion rotation =
                forward.sqrMagnitude >
                0.01f
                    ? Quaternion.LookRotation(
                        forward.normalized,
                        Vector3.up)
                    : Quaternion.identity;

            Teleport(
                route[0],
                rotation);
        }

        private void Teleport(
            Vector3 position,
            Quaternion rotation)
        {
            if (car == null)
                return;

            CancelAllActivities();

            car.TeleportTo(
                position +
                Vector3.up *
                1.1f,
                rotation);

            car.SetDrivingEnabled(
                true);

            lastAction =
                "Телепорт: " +
                position.x.ToString(
                    "0") +
                ", " +
                position.z.ToString(
                    "0");
        }

        private void RescueCar()
        {
            if (car == null)
                return;

            CityAssetRuntimeInstaller.ResolveNearestRoadResetPose(
                car.transform.position,
                car.transform.forward,
                out Vector3 position,
                out Quaternion rotation);

            Teleport(
                position,
                rotation);
        }

        private void CancelAllActivities()
        {
            delivery?.CancelActivity();
            drift?.CancelActivity();
            sprint?.CancelActivity();
            circuit?.CancelActivity();
            professions?.CancelActive();
            carWash?.CancelWash();
            towTruck?.CancelJob();
            underground?.CancelRunForTesting();

            if (activities != null)
            {
                if (activities.HasResult)
                {
                    activities.DismissResult();
                }

                if (activities.IsBusy)
                {
                    activities.End(
                        activities.ActiveId);
                }
            }

            car?.SetDrivingEnabled(
                true);

            lastAction =
                "Активности отменены";
        }

        private void ResetProgressForTesting()
        {
            CancelAllActivities();

            wallet?.SetCredits(
                0);

            reputation?.SetReputation(
                0);

            disciplines?.SetAllLevelsForTesting(
                1);

            mastery?.SetAllVehicleLevelsForTesting(
                1);

            garage?.SetAllUpgradeLevelsForTesting(
                0);

            career?.SetStageForTesting(
                0);

            vehicleHistory?.ResetAllForTesting();
            collection?.ResetMilestonesForTesting();
            legends?.ResetForTesting();
            contracts?.ResetForTesting();
            liveEvents?.ResetForTesting();
            underground?.ResetForTesting();
            cityRisk?.ClearForTesting();
            club?.ResetForTesting();
            season?.ResetForTesting();
            story?.ResetForTesting();
            onboarding?.ResetForTesting();

            if (roster != null)
            {
                roster.SelectVehicleForTesting(
                    0,
                    out _);
            }

            ReplayPostLoadingStartForTesting();

            MotorCity.Persistence.MotorCitySaveService.Save();

            lastAction =
                "QA старт переигран с нуля";
        }

        private void ReplayPostLoadingStartForTesting()
        {
            if (car != null)
            {
                Vector3 spawn =
                    CityAssetRuntimeInstaller.PlayerSpawnPoint;

                spawn.y =
                    0.1861947f;

                car.TeleportTo(
                    spawn,
                    CityAssetRuntimeInstaller.PlayerSpawnRotation);

                car.ClearMotion();

                car.BeginOpeningPresentationLock(
                    5f);
            }

            ChaseCamera chase =
                Object.FindAnyObjectByType<ChaseCamera>();

            if (chase != null)
            {
                chase.PlayOpeningPresentation(
                    5f);
            }

            PrototypeHud hud =
                Object.FindAnyObjectByType<PrototypeHud>();

            if (hud != null)
            {
                hud.ReplayOpeningHudReveal(
                    5f);
            }
        }

        private static void SystemLine(
            string name,
            bool available,
            string status)
        {
            GUILayout.Label(
                (available
                    ? "[OK] "
                    : "[MISSING] ") +
                name +
                (string.IsNullOrWhiteSpace(
                    status)
                    ? string.Empty
                    : " - " +
                      status));
        }

        private static string Value(
            int value)
        {
            return
                value.ToString(
                    "N0");
        }

        private static void Section(
            string title)
        {
            GUILayout.Space(
                10f);

            GUILayout.Label(
                "== " +
                title +
                " ==");
        }

        private static void Separator()
        {
            GUILayout.Space(
                4f);

            GUILayout.Box(
                GUIContent.none,
                GUILayout.ExpandWidth(
                    true),
                GUILayout.Height(
                    1f));

            GUILayout.Space(
                4f);
        }

        private static bool Button(
            string text)
        {
            return
                GUILayout.Button(
                    text,
                    GUILayout.MinWidth(
                        110f),
                    GUILayout.Height(
                        30f));
        }
    }
}
#endif
