#if UNITY_EDITOR
using MotorCity.CameraSystem;
using MotorCity.Input;
using MotorCity.Platform;
using MotorCity.UI;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class AdminDebugPanel : MonoBehaviour
    {
        private const int WindowId = 73921;

        private static readonly string[] Tabs =
        {
            "БЫСТРЫЙ ТЕСТ",
            "АКТИВНОСТИ",
            "ПРОГРЕСС",
            "МАШИНЫ",
            "МИР",
            "RESULT / ADS",
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
        private MotorCityInterstitialRuntime interstitialRuntime;
        private ResultNextGoalResolver nextGoalResolver;

        private bool visible;
        private int selectedTab;
        private Vector2 scroll;
        private string lastAction = "Готово";

        private Rect windowRect =
            new Rect(
                16f,
                48f,
                980f,
                860f);

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
            interstitialRuntime =
                GetComponent<MotorCityInterstitialRuntime>();

            nextGoalResolver =
                GetComponent<ResultNextGoalResolver>();

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
                    "MOTOR CITY - ПОЛНЫЙ QA ADMIN");
        }

        private void DrawWindow(
            int id)
        {
            selectedTab =
                GUILayout.Toolbar(
                    selectedTab,
                    Tabs,
                    GUILayout.Height(
                        32f));

            GUILayout.Space(
                4f);

            DrawHeader();

            scroll =
                GUILayout.BeginScrollView(
                    scroll,
                    false,
                    true);

            switch (selectedTab)
            {
                case 0:
                    DrawQuickTest();
                    break;
                case 1:
                    DrawActivities();
                    break;
                case 2:
                    DrawProgress();
                    break;
                case 3:
                    DrawVehicles();
                    break;
                case 4:
                    DrawWorld();
                    break;
                case 5:
                    DrawResultAndAds();
                    break;
                default:
                    DrawSystems();
                    break;
            }

            GUILayout.Space(
                14f);

            Separator();

            GUILayout.Label(
                "ПОСЛЕДНЕЕ ДЕЙСТВИЕ: " +
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
            string activity =
                activities == null
                    ? "-"
                    : activities.HasResult
                        ? "RESULT: " +
                          activities.ResultActivityId
                        : activities.IsBusy
                            ? "ACTIVE: " +
                              activities.ActiveId
                            : "свободно";

            GUILayout.Label(
                "КРЕДИТЫ " +
                Value(
                    wallet == null
                        ? 0
                        : wallet.Credits) +
                "   |   РЕПУТАЦИЯ " +
                Value(
                    reputation == null
                        ? 0
                        : reputation.Reputation) +
                "   |   " +
                (roster == null
                    ? "-"
                    : roster.SelectedName) +
                "   |   " +
                activity);

            if (car != null)
            {
                GUILayout.Label(
                    "МАШИНА: " +
                    car.SpeedKph.ToString(
                        "0") +
                    " км/ч   |   " +
                    car.CurrentDriveMode +
                    "   |   колёса " +
                    car.GroundedWheels +
                    "/4");
            }

            Separator();
        }

        private void DrawQuickTest()
        {
            Section(
                "ПРЕСЕТЫ СОСТОЯНИЯ ИГРЫ");

            GUILayout.Label(
                "Один клик - готовое состояние для проверки интерфейса или прогрессии.");

            GUILayout.BeginHorizontal();

            if (Button("ЧИСТЫЙ НОВЫЙ ИГРОК"))
            {
                ResetProgressForTesting();
            }

            if (Button("ОБУЧЕНИЕ ПРОЙДЕНО"))
            {
                onboarding?.CompleteForTesting();
                lastAction =
                    "Первые шаги завершены";
            }

            if (Button("ПУТЬ НОВИЧКА ПРОЙДЕН"))
            {
                CompleteStoryForTesting();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("СЕЗОН ДОСТУПЕН"))
            {
                onboarding?.CompleteForTesting();
                CompleteStoryForTesting();
                season?.ResetForTesting();

                lastAction =
                    "Подготовлено состояние для сезона";
            }

            if (Button("ПОЧТИ МАКС. ПРОГРЕСС"))
            {
                PrepareLateGameForTesting();
            }

            if (Button("ВСЁ ОТМЕНИТЬ"))
            {
                CancelAllActivities();
            }

            GUILayout.EndHorizontal();

            Section(
                "ЭКОНОМИКА");

            GUILayout.BeginHorizontal();

            CreditsButton(
                "КР 0",
                0);

            CreditsButton(
                "КР 10 000",
                10000);

            CreditsButton(
                "КР 1 000 000",
                1000000);

            RepButton(
                "РЕП 0",
                0);

            RepButton(
                "РЕП 5 000",
                5000);

            RepButton(
                "РЕП 100 000",
                100000);

            GUILayout.EndHorizontal();

            Section(
                "БЫСТРЫЙ RESULT");

            GUILayout.BeginHorizontal();

            if (Button("SUCCESS"))
            {
                SimulateResult(
                    "delivery",
                    "QA SUCCESS",
                    true,
                    false,
                    1250);
            }

            if (Button("NEW RECORD"))
            {
                SimulateResult(
                    "sprint",
                    "QA NEW RECORD",
                    true,
                    true,
                    1750);
            }

            if (Button("FAIL"))
            {
                SimulateResult(
                    "drift",
                    "QA FAIL",
                    false,
                    false,
                    0);
            }

            if (Button("ЗАКРЫТЬ RESULT"))
            {
                CloseResult();
            }

            GUILayout.EndHorizontal();

            Section(
                "SMOKE TEST");

            GUILayout.BeginHorizontal();

            if (Button("MOCK AD + COUNTDOWN"))
            {
                TestMockAdFlow();
            }

            if (Button("ПЕРЕИГРАТЬ ИНТРО"))
            {
                ReplayPostLoadingStartForTesting();
                lastAction =
                    "Интро переиграно";
            }

            if (Button("СПАСТИ МАШИНУ"))
            {
                RescueCar();
            }

            GUILayout.EndHorizontal();

            DrawCoreProgressSummary();
        }

        private void DrawActivities()
        {
            Section(
                "ОСНОВНЫЕ АКТИВНОСТИ");

            DrawActivityQaRow(
                "ДОСТАВКА",
                "delivery",
                CityAssetRuntimeInstaller.DeliveryRoute);

            DrawActivityQaRow(
                "СПРИНТ",
                "sprint",
                CityAssetRuntimeInstaller.SprintRoute);

            DrawActivityQaRow(
                "КОЛЬЦО",
                "circuit",
                CityAssetRuntimeInstaller.CircuitRoute);

            DrawActivityQaPointRow(
                "ДРИФТ",
                "drift",
                CityAssetRuntimeInstaller.DriftChallengePoint);

            Section(
                "ГОРОДСКИЕ ПРОФЕССИИ");

            if (professions != null)
            {
                GUILayout.Label(
                    "Уровень профессий: " +
                    professions.ProfessionLevel +
                    "   |   завершено: " +
                    professions.TotalCompleted);

                for (int i = 0;
                     i < professions.StartCount;
                     i++)
                {
                    int index =
                        i;

                    GUILayout.BeginHorizontal();

                    GUILayout.Label(
                        professions.GetStartName(
                            index),
                        GUILayout.Width(
                            210f));

                    if (Button("ТЕЛЕПОРТ"))
                    {
                        Teleport(
                            professions.GetStartPoint(
                                index),
                            Quaternion.identity);
                    }

                    if (Button("SUCCESS"))
                    {
                        SimulateResult(
                            ProfessionActivityId(
                                index),
                            professions.GetStartName(
                                index),
                            true,
                            false,
                            650);
                    }

                    if (Button("FAIL"))
                    {
                        SimulateResult(
                            ProfessionActivityId(
                                index),
                            professions.GetStartName(
                                index),
                            false,
                            false,
                            0);
                    }

                    GUILayout.EndHorizontal();
                }
            }

            Section(
                "АВТОМОЙКА / ЭВАКУАТОР");

            GUILayout.BeginHorizontal();

            if (Button("МОЙКА - ТЕЛЕПОРТ"))
            {
                if (carWash != null)
                {
                    Teleport(
                        carWash.StartPoint,
                        Quaternion.identity);
                }
            }

            if (Button("МОЙКА - SUCCESS"))
            {
                SimulateResult(
                    "profession_carwash",
                    "АВТОМОЙКА",
                    true,
                    false,
                    500);
            }

            if (Button("ЭВАКУАТОР - ТЕЛЕПОРТ"))
            {
                if (towTruck != null)
                {
                    Teleport(
                        towTruck.StartPoint,
                        Quaternion.identity);
                }
            }

            if (Button("ЭВАКУАТОР - SUCCESS"))
            {
                SimulateResult(
                    "profession_tow",
                    "СЛУЖБА ЭВАКУАЦИИ",
                    true,
                    false,
                    680);
            }

            GUILayout.EndHorizontal();

            Section(
                "НОЧНОЙ АВТОКЛУБ");

            GUILayout.Label(
                underground == null
                    ? "Система не найдена"
                    : underground.AdminLine);

            GUILayout.BeginHorizontal();

            if (Button("ТЕЛЕПОРТ"))
            {
                Teleport(
                    CityAssetRuntimeInstaller.UndergroundMeetingPoint,
                    Quaternion.identity);
            }

            if (Button("+100 ДОВЕРИЯ"))
            {
                underground?.AddCredForTesting(
                    100);

                lastAction =
                    "Ночной автоклуб: +100 доверия";
            }

            if (Button("ОТКРЫТЬ ТЕКУЩЕЕ"))
            {
                underground?.UnlockCurrentForTesting();
                lastAction =
                    "Ночной автоклуб: событие открыто";
            }

            if (Button("ЗАВЕРШИТЬ ТЕКУЩЕЕ"))
            {
                underground?.CompleteCurrentForTesting();
                lastAction =
                    "Ночной автоклуб: событие завершено";
            }

            if (Button("СБРОС"))
            {
                underground?.ResetForTesting();
                lastAction =
                    "Ночной автоклуб сброшен";
            }

            GUILayout.EndHorizontal();

            Section(
                "АВТО-СОБЫТИЯ");

            GUILayout.Label(
                "Speed Trap / Drift Spot / Discovery можно проверять телепортом одним кликом ниже.");

            DrawSpeedTrapButtons();
            DrawDriftSpotButtons();

            GUILayout.BeginHorizontal();

            if (Button("DISCOVERY +1"))
            {
                DiscoverFirstMissing();
            }

            if (Button("DISCOVERY ВСЕ"))
            {
                discoveries?.DiscoverAllForTesting();
                lastAction =
                    "Все открытия засчитаны";
            }

            if (Button("DISCOVERY СБРОС"))
            {
                discoveries?.ResetForTesting();
                lastAction =
                    "Открытия сброшены";
            }

            if (Button("РАДАРЫ СБРОС"))
            {
                speedTraps?.ResetForTesting();
                lastAction =
                    "Speed Traps сброшены";
            }

            if (Button("DRIFT SPOTS СБРОС"))
            {
                driftSpots?.ResetForTesting();
                lastAction =
                    "Drift Spots сброшены";
            }

            GUILayout.EndHorizontal();

            Section(
                "УПРАВЛЕНИЕ АКТИВНОСТЯМИ");

            GUILayout.BeginHorizontal();

            if (Button("ОТМЕНИТЬ ВСЁ"))
            {
                CancelAllActivities();
            }

            if (Button("RESULT ЗАКРЫТЬ"))
            {
                CloseResult();
            }

            if (Button("МОК SUCCESS ДЛЯ DAILY"))
            {
                SimulateResult(
                    "delivery",
                    "QA DAILY",
                    true,
                    false,
                    0);
            }

            GUILayout.EndHorizontal();
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
                    "Первые шаги +1";
            }

            if (Button("ЗАВЕРШИТЬ"))
            {
                onboarding?.CompleteForTesting();
                lastAction =
                    "Первые шаги завершены";
            }

            if (Button("СБРОС"))
            {
                onboarding?.ResetForTesting();
                lastAction =
                    "Первые шаги сброшены";
            }

            GUILayout.EndHorizontal();

            Section(
                "ПУТЬ НОВИЧКА / СЮЖЕТ");

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
                    "Сюжет +1 миссия";
            }

            if (Button("ЗАВЕРШИТЬ ВСЁ"))
            {
                CompleteStoryForTesting();
            }

            if (Button("СБРОС"))
            {
                story?.ResetForTesting();
                lastAction =
                    "Сюжет сброшен";
            }

            GUILayout.EndHorizontal();

            Section(
                "DAILY");

            GUILayout.Label(
                daily == null
                    ? "Система не найдена"
                    : daily.DayCompleted
                        ? "ДЕНЬ ЗАВЕРШЁН"
                        : daily.CompletedTasks +
                          "/3 - " +
                          daily.ObjectiveLine);

            GUILayout.BeginHorizontal();

            if (Button("ЗАВЕРШИТЬ DAILY"))
            {
                daily?.CompleteForTesting();
                lastAction =
                    "Daily завершён";
            }

            if (Button("СБРОС DAILY"))
            {
                daily?.ResetForTesting();
                lastAction =
                    "Daily сброшен";
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
                    "Сезон +1 миссия";
            }

            if (Button("ЗАВЕРШИТЬ"))
            {
                season?.CompleteForTesting();
                lastAction =
                    "Сезон завершён";
            }

            if (Button("СБРОС"))
            {
                season?.ResetForTesting();
                lastAction =
                    "Сезон сброшен";
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

            GUILayout.BeginHorizontal();

            for (int i = 0;
                 i < 6;
                 i++)
            {
                int index =
                    i;

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

            GUILayout.BeginHorizontal();

            if (Button("WEEKLY ГОТОВО"))
            {
                club?.CompleteWeeklyForTesting();
                lastAction =
                    "Клубная неделя завершена";
            }

            if (Button("СБРОС КЛУБА"))
            {
                club?.ResetForTesting();
                lastAction =
                    "Клуб сброшен";
            }

            GUILayout.EndHorizontal();

            Section(
                "ДИСЦИПЛИНЫ / КАРЬЕРА");

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
                SetCareer(0);

            if (Button("КАРЬЕРА 1"))
                SetCareer(1);

            if (Button("КАРЬЕРА 2"))
                SetCareer(2);

            if (Button("КАРЬЕРА 3"))
                SetCareer(3);

            GUILayout.EndHorizontal();

            Section(
                "ЛЕГЕНДЫ / КОНТРАКТЫ / LIVE EVENT");

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

            if (Button("КОНТРАКТ СБРОС"))
            {
                contracts?.ResetForTesting();
                lastAction =
                    "Контракты сброшены";
            }

            if (Button("EVENT СЛЕДУЮЩИЙ"))
            {
                liveEvents?.NextEventForTesting();
                lastAction =
                    "Live Event переключён";
            }

            if (Button("EVENT ГОТОВО"))
            {
                liveEvents?.CompleteCurrentForTesting();
                lastAction =
                    "Live Event завершён";
            }

            if (Button("EVENT СБРОС"))
            {
                liveEvents?.ResetForTesting();
                lastAction =
                    "Live Events сброшены";
            }

            GUILayout.EndHorizontal();

            Section(
                "РИСК / ДОСТИЖЕНИЯ");

            GUILayout.BeginHorizontal();

            if (Button("РИСК +25"))
            {
                cityRisk?.AddAttentionForTesting(
                    25f);

                lastAction =
                    "Риск +25";
            }

            if (Button("РИСК +100"))
            {
                cityRisk?.AddAttentionForTesting(
                    100f);

                lastAction =
                    "Риск +100";
            }

            if (Button("РИСК СБРОС"))
            {
                cityRisk?.ClearForTesting();
                lastAction =
                    "Риск сброшен";
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Достижения: " +
                (achievements == null
                    ? "-"
                    : achievements.UnlockedCount.ToString()));

            GUILayout.BeginHorizontal();

            if (Button("ACHIEVEMENT +1"))
            {
                achievements?.UnlockNextForTesting();
                lastAction =
                    "Открыто следующее достижение";
            }

            if (Button("ACHIEVEMENTS ВСЕ"))
            {
                achievements?.UnlockAllForTesting();
                lastAction =
                    "Все достижения открыты";
            }

            if (Button("ACHIEVEMENTS СБРОС"))
            {
                achievements?.ResetForTesting();
                lastAction =
                    "Достижения сброшены";
            }

            GUILayout.EndHorizontal();

            Section(
                "PHOTO HUNT");

            GUILayout.Label(
                photoHunt == null
                    ? "Система не найдена"
                    : photoHunt.AlbumLine);

            GUILayout.BeginHorizontal();

            if (Button("АЛЬБОМ ЗАПОЛНИТЬ"))
            {
                photoHunt?.CompleteAlbumForTesting();
                lastAction =
                    "Фотоальбом заполнен";
            }

            if (Button("АЛЬБОМ СБРОС"))
            {
                photoHunt?.ResetForTesting();
                lastAction =
                    "Фотоальбом сброшен";
            }

            GUILayout.EndHorizontal();
        }

        private void DrawVehicles()
        {
            Section(
                "РЕСУРСЫ");

            GUILayout.BeginHorizontal();

            CreditsButton(
                "0 КР",
                0);

            CreditsButton(
                "10K КР",
                10000);

            CreditsButton(
                "1M КР",
                1000000);

            RepButton(
                "0 РЕП",
                0);

            RepButton(
                "5K РЕП",
                5000);

            RepButton(
                "100K РЕП",
                100000);

            GUILayout.EndHorizontal();

            Section(
                "ВЫБОР ЛЮБОЙ МАШИНЫ");

            if (roster == null)
            {
                GUILayout.Label(
                    "VehicleRoster отсутствует");
            }
            else
            {
                for (int row = 0;
                     row * 4 < roster.VehicleCount;
                     row++)
                {
                    GUILayout.BeginHorizontal();

                    for (int column = 0;
                         column < 4;
                         column++)
                    {
                        int index =
                            row * 4 +
                            column;

                        if (index >=
                            roster.VehicleCount)
                        {
                            break;
                        }

                        int captured =
                            index;

                        if (Button(
                                "МАШИНА " +
                                (captured + 1)))
                        {
                            SelectVehicle(
                                captured);
                        }
                    }

                    GUILayout.EndHorizontal();
                }
            }

            Section(
                "MASTERY");

            GUILayout.BeginHorizontal();

            if (Button("ТЕКУЩАЯ УР.1"))
                SetMastery(1);

            if (Button("ТЕКУЩАЯ УР.5"))
                SetMastery(5);

            if (Button("ТЕКУЩАЯ УР.10"))
                SetMastery(10);

            if (Button("ВСЕ МАШИНЫ УР.10"))
            {
                mastery?.SetAllVehicleLevelsForTesting(
                    10);

                lastAction =
                    "Mastery всех машин = 10";
            }

            GUILayout.EndHorizontal();

            Section(
                "АПГРЕЙДЫ ГАРАЖА");

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

            Section(
                "ИСТОРИЯ / КОЛЛЕКЦИЯ");

            GUILayout.Label(
                vehicleHistory == null
                    ? "История отсутствует"
                    : "Пробег " +
                      vehicleHistory.DistanceKm.ToString(
                          "0.0") +
                      " км | побед " +
                      vehicleHistory.Victories +
                      " | заработано " +
                      Value(
                          vehicleHistory.EarnedCredits));

            GUILayout.BeginHorizontal();

            if (Button("ИСТОРИЯ: ВСЕ LEGENDARY"))
            {
                vehicleHistory?.SetAllLegendaryForTesting();
                lastAction =
                    "История всех машин = legendary";
            }

            if (Button("ИСТОРИЯ СБРОС"))
            {
                vehicleHistory?.ResetAllForTesting();
                lastAction =
                    "История машин сброшена";
            }

            if (Button("КОЛЛЕКЦИЯ ПЕРЕСЧИТАТЬ"))
            {
                collection?.RecalculateForTesting();
                lastAction =
                    "Коллекция пересчитана";
            }

            if (Button("КОЛЛЕКЦИЯ CLAIM ALL"))
            {
                collection?.ClaimAllForTesting();
                lastAction =
                    "Все награды коллекции получены";
            }

            if (Button("КОЛЛЕКЦИЯ СБРОС"))
            {
                collection?.ResetMilestonesForTesting();
                lastAction =
                    "Коллекция сброшена";
            }

            GUILayout.EndHorizontal();

            if (vehicleSpecialization != null)
            {
                GUILayout.Label(
                    "Специализация: " +
                    vehicleSpecialization.GarageLine);
            }
        }

        private void DrawWorld()
        {
            Section(
                "КЛЮЧЕВЫЕ ТЕЛЕПОРТЫ");

            GUILayout.BeginHorizontal();

            if (Button("СПАВН"))
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

            if (Button("ДОСТАВКА"))
                TeleportRoute(
                    CityAssetRuntimeInstaller.DeliveryRoute);

            if (Button("СПРИНТ"))
                TeleportRoute(
                    CityAssetRuntimeInstaller.SprintRoute);

            if (Button("КОЛЬЦО"))
                TeleportRoute(
                    CityAssetRuntimeInstaller.CircuitRoute);

            if (Button("ДРИФТ"))
            {
                Teleport(
                    CityAssetRuntimeInstaller.DriftChallengePoint,
                    Quaternion.identity);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("ПОДПОЛЬЕ"))
            {
                Teleport(
                    CityAssetRuntimeInstaller.UndergroundMeetingPoint,
                    Quaternion.identity);
            }

            if (Button("АВТОМОЙКА"))
            {
                if (carWash != null)
                {
                    Teleport(
                        carWash.StartPoint,
                        Quaternion.identity);
                }
            }

            if (Button("ЭВАКУАТОР"))
            {
                if (towTruck != null)
                {
                    Teleport(
                        towTruck.StartPoint,
                        Quaternion.identity);
                }
            }

            if (Button("СПАСТИ"))
                RescueCar();

            GUILayout.EndHorizontal();

            Section(
                "ВРЕМЯ СУТОК");

            GUILayout.BeginHorizontal();

            TimeButton(
                "УТРО",
                DayNightCycleController.MorningTime01);

            TimeButton(
                "ДЕНЬ",
                DayNightCycleController.DayTime01);

            TimeButton(
                "ВЕЧЕР",
                DayNightCycleController.EveningTime01);

            TimeButton(
                "НОЧЬ",
                DayNightCycleController.NightTime01);

            GUILayout.EndHorizontal();

            GUILayout.Label(dayNight != null ? "Скорость времени: ×" + dayNight.TimeSpeed.ToString("0") : "Время недоступно");
            GUILayout.BeginHorizontal();
            if (Button("ПАУЗА")) dayNight?.SetTimeSpeed(0f);
            foreach (float speed in new[] {1f, 5f, 10f, 30f, 60f})
                if (Button("×" + speed.ToString("0"))) dayNight?.SetTimeSpeed(speed);
            GUILayout.EndHorizontal();
            Section(
                "DISCOVERY");

            if (discoveries != null)
            {
                for (int i = 0;
                     i < discoveries.DiscoveryCount;
                     i++)
                {
                    int index =
                        i;

                    GUILayout.BeginHorizontal();

                    GUILayout.Label(
                        (discoveries.IsFound(
                            index)
                            ? "[НАЙДЕНО] "
                            : "[НЕ НАЙДЕНО] ") +
                        discoveries.GetDiscoveryName(
                            index),
                        GUILayout.Width(
                            300f));

                    if (Button("ТЕЛЕПОРТ"))
                    {
                        Teleport(
                            discoveries.GetDiscoveryPosition(
                                index),
                            Quaternion.identity);
                    }

                    if (Button("ЗАСЧИТАТЬ"))
                    {
                        discoveries.DiscoverForTesting(
                            index);

                        lastAction =
                            "Открытие засчитано: " +
                            discoveries.GetDiscoveryName(
                                index);
                    }

                    GUILayout.EndHorizontal();
                }
            }

            Section(
                "SPEED TRAPS");

            DrawSpeedTrapButtons();

            Section(
                "DRIFT SPOTS");

            DrawDriftSpotButtons();

            Section(
                "ФИЗИКА МАШИНЫ");

            if (car != null)
            {
                GUILayout.Label(
                    "Speed " +
                    car.SpeedKph.ToString(
                        "0.0") +
                    " km/h | Slip " +
                    car.SlipAngleDegrees.ToString(
                        "0.0") +
                    "° | Rear slip " +
                    car.RearSidewaysSlip.ToString(
                        "0.00") +
                    " | Grounded " +
                    car.GroundedWheels +
                    "/4 | Sliding " +
                    (car.IsSliding
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

            if (Button("ОЧИСТИТЬ ДВИЖЕНИЕ"))
            {
                car?.ClearMotion();
                lastAction =
                    "Скорость/вращение машины очищены";
            }

            GUILayout.EndHorizontal();
        }

        private void DrawResultAndAds()
        {
            Section(
                "RESULT - БЕЗ ПРОХОЖДЕНИЯ");

            GUILayout.Label(
                "Эти кнопки открывают настоящий Activity Result и запускают обычные progression listeners.");

            GUILayout.BeginHorizontal();

            if (Button("DELIVERY SUCCESS"))
                SimulateResult(
                    "delivery",
                    "ДОСТАВКА",
                    true,
                    false,
                    500);

            if (Button("DELIVERY RECORD"))
                SimulateResult(
                    "delivery",
                    "ДОСТАВКА",
                    true,
                    true,
                    500);

            if (Button("SPRINT SUCCESS"))
                SimulateResult(
                    "sprint",
                    "СПРИНТ",
                    true,
                    false,
                    1000);

            if (Button("SPRINT RECORD"))
                SimulateResult(
                    "sprint",
                    "СПРИНТ",
                    true,
                    true,
                    1000);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("CIRCUIT SUCCESS"))
                SimulateResult(
                    "circuit",
                    "КОЛЬЦО",
                    true,
                    false,
                    1200);

            if (Button("CIRCUIT RECORD"))
                SimulateResult(
                    "circuit",
                    "КОЛЬЦО",
                    true,
                    true,
                    1200);

            if (Button("DRIFT SUCCESS"))
                SimulateResult(
                    "drift",
                    "ДРИФТ-ЗАЕЗД",
                    true,
                    false,
                    850);

            if (Button("DRIFT FAIL"))
                SimulateResult(
                    "drift",
                    "ДРИФТ-ЗАЕЗД",
                    false,
                    false,
                    0);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("БЕЗ НАГРАДЫ"))
                SimulateResult(
                    "qa_result",
                    "QA RESULT",
                    false,
                    false,
                    0);

            if (Button("ЗАКРЫТЬ RESULT"))
                CloseResult();

            GUILayout.EndHorizontal();

            Section(
                "INTERSTITIAL / COUNTDOWN");

            SystemLine(
                "Interstitial",
                interstitialRuntime != null,
                interstitialRuntime == null
                    ? string.Empty
                    : interstitialRuntime.AdminLine);

            ActivityStartFlow startFlow =
                activities == null
                    ? null
                    : activities.StartFlow;

            SystemLine(
                "ActivityStartFlow",
                startFlow != null,
                startFlow == null
                    ? string.Empty
                    : startFlow.AdminLine);

            GUILayout.BeginHorizontal();

            if (Button("NEXT START - MOCK AD"))
            {
                interstitialRuntime?.ForceNextEditorMock();
                lastAction =
                    "Следующий рекламный старт получит mock ad";
            }

            if (Button("ПРОГНАТЬ MOCK AD + 3-2-1-GO"))
            {
                TestMockAdFlow();
            }

            if (Button("ОТМЕНИТЬ FLOW"))
            {
                CancelAllActivities();
            }

            GUILayout.EndHorizontal();

            Section(
                "UI / INTRO");

            GUILayout.BeginHorizontal();

            if (Button("ПЕРЕИГРАТЬ ИНТРО"))
            {
                ReplayPostLoadingStartForTesting();
                lastAction =
                    "Интро и HUD reveal переиграны";
            }

            if (Button("RESULT NEW RECORD"))
            {
                SimulateResult(
                    "sprint",
                    "QA NEW RECORD",
                    true,
                    true,
                    1500);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawSystems()
        {
            Section(
                "СОСТОЯНИЕ СИСТЕМ");

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
                "Daily",
                daily != null,
                daily == null
                    ? string.Empty
                    : daily.CompletedTasks +
                      "/3");

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
                "Next Goal",
                nextGoalResolver != null,
                nextGoalResolver == null
                    ? string.Empty
                    : nextGoalResolver.AdminLine);

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
                    : photoHunt.AlbumLine);

            SystemLine(
                "Professions",
                professions != null,
                professions == null
                    ? string.Empty
                    : professions.TotalCompleted.ToString());

            SystemLine(
                "Discoveries",
                discoveries != null,
                discoveries == null
                    ? string.Empty
                    : discoveries.FoundCount +
                      "/" +
                      discoveries.DiscoveryCount);

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
                "SAVE / RESET");

            GUILayout.BeginHorizontal();

            if (Button("SAVE СЕЙЧАС"))
            {
                MotorCity.Persistence.MotorCitySaveService.Save();
                lastAction =
                    "Save выполнен";
            }

            if (Button("ПОЛНЫЙ QA RESET"))
            {
                ResetProgressForTesting();
            }

            if (Button("ОТМЕНИТЬ ВСЁ"))
            {
                CancelAllActivities();
            }

            GUILayout.EndHorizontal();

            Section(
                "GAMEPLAY COMPONENTS");

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

        private void DrawCoreProgressSummary()
        {
            Section(
                "ТЕКУЩИЙ ПРОГРЕСС");

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
                "Daily: " +
                (daily == null
                    ? "-"
                    : daily.CompletedTasks +
                      "/3") +
                "   |   Сезон: " +
                (season == null
                    ? "-"
                    : season.IsComplete
                        ? "ГОТОВО"
                        : season.CurrentMissionNumber +
                          "/" +
                          season.MissionCount) +
                "   |   Достижения: " +
                (achievements == null
                    ? "-"
                    : achievements.UnlockedCount.ToString()));
        }

        private void DrawActivityQaRow(
            string label,
            string activityId,
            Vector3[] route)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(
                label,
                GUILayout.Width(
                    150f));

            if (Button("ТЕЛЕПОРТ"))
            {
                TeleportRoute(
                    route);
            }

            if (Button("SUCCESS"))
            {
                SimulateResult(
                    activityId,
                    label,
                    true,
                    false,
                    1000);
            }

            if (Button("NEW RECORD"))
            {
                SimulateResult(
                    activityId,
                    label,
                    true,
                    true,
                    1200);
            }

            if (Button("FAIL"))
            {
                SimulateResult(
                    activityId,
                    label,
                    false,
                    false,
                    0);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawActivityQaPointRow(
            string label,
            string activityId,
            Vector3 point)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(
                label,
                GUILayout.Width(
                    150f));

            if (Button("ТЕЛЕПОРТ"))
            {
                Teleport(
                    point,
                    Quaternion.identity);
            }

            if (Button("SUCCESS"))
            {
                SimulateResult(
                    activityId,
                    label,
                    true,
                    false,
                    900);
            }

            if (Button("FAIL"))
            {
                SimulateResult(
                    activityId,
                    label,
                    false,
                    false,
                    0);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawSpeedTrapButtons()
        {
            if (speedTraps == null)
                return;

            for (int i = 0;
                 i < speedTraps.TrapCount;
                 i++)
            {
                int index =
                    i;

                GUILayout.BeginHorizontal();

                GUILayout.Label(
                    speedTraps.GetTrapName(
                        index) +
                    " | best " +
                    speedTraps.GetBestSpeed(
                        index).ToString(
                            "0") +
                    " км/ч",
                    GUILayout.Width(
                        320f));

                if (Button("ТЕЛЕПОРТ"))
                {
                    Teleport(
                        speedTraps.GetTrapPosition(
                            index),
                        speedTraps.GetTrapRotation(
                            index));
                }

                if (Button("GOLD"))
                {
                    speedTraps.CompleteGoldForTesting(
                        index);

                    lastAction =
                        "Speed Trap GOLD: " +
                        speedTraps.GetTrapName(
                            index);
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawDriftSpotButtons()
        {
            if (driftSpots == null)
                return;

            for (int i = 0;
                 i < driftSpots.SpotCount;
                 i++)
            {
                int index =
                    i;

                GUILayout.BeginHorizontal();

                GUILayout.Label(
                    driftSpots.GetSpotName(
                        index),
                    GUILayout.Width(
                        320f));

                if (Button("ТЕЛЕПОРТ"))
                {
                    Teleport(
                        driftSpots.GetSpotPosition(
                            index),
                        Quaternion.identity);
                }

                if (Button("GOLD"))
                {
                    driftSpots.CompleteGoldForTesting(
                        index);

                    lastAction =
                        "Drift Spot GOLD: " +
                        driftSpots.GetSpotName(
                            index);
                }

                GUILayout.EndHorizontal();
            }
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

            if (Button("УР.1"))
            {
                disciplines?.SetLevelForTesting(
                    type,
                    1);

                lastAction =
                    label +
                    " = 1";
            }

            if (Button("УР.5"))
            {
                disciplines?.SetLevelForTesting(
                    type,
                    5);

                lastAction =
                    label +
                    " = 5";
            }

            if (Button("УР.10"))
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

        private void SimulateResult(
            string activityId,
            string title,
            bool success,
            bool newRecord,
            int credits)
        {
            CancelAllActivities();

            if (activities == null)
            {
                lastAction =
                    "ActivityManager отсутствует";

                return;
            }

            activities.ShowResult(
                activityId,
                title,
                success
                    ? newRecord
                        ? "98,42 С"
                        : "123,45 С"
                    : "ПРОВАЛ",
                success
                    ? newRecord
                        ? "ЗОЛОТО • НОВЫЙ РЕКОРД"
                        : "ЗОЛОТО • QA SUCCESS"
                    : "QA FAIL - прогресс не начисляется",
                credits,
                success,
                newRecord);

            lastAction =
                "Result: " +
                activityId +
                " / " +
                (success
                    ? newRecord
                        ? "NEW RECORD"
                        : "SUCCESS"
                    : "FAIL");
        }

        private void TestMockAdFlow()
        {
            CancelAllActivities();

            if (activities == null ||
                activities.StartFlow == null ||
                interstitialRuntime == null)
            {
                lastAction =
                    "ActivityStartFlow / Interstitial отсутствует";

                return;
            }

            interstitialRuntime.ForceNextEditorMock();

            bool requested =
                activities.RequestStart(
                    "sprint",
                    "QA MOCK AD",
                    3f,
                    () =>
                    {
                        car?.SetDrivingBlocked(
                            "ActivityCountdown",
                            true);

                        lastAction =
                            "Mock ad закрыт - countdown начался";
                    },
                    shown =>
                    {
                        lastAction =
                            shown <= 0
                                ? "GO"
                                : "Countdown " +
                                  shown;
                    },
                    () =>
                    {
                        activities.End(
                            "sprint");

                        car?.SetDrivingBlocked(
                            "ActivityCountdown",
                            false);

                        lastAction =
                            "Mock ad -> 3-2-1-GO -> gameplay: ГОТОВО";
                    });

            if (!requested)
            {
                lastAction =
                    "Не удалось запустить QA flow";
            }
        }

        private void CompleteStoryForTesting()
        {
            if (story == null)
                return;

            int guard =
                0;

            while (!story.IsComplete &&
                   guard < 64)
            {
                story.AdvanceMissionForTesting();
                guard++;
            }

            lastAction =
                story.IsComplete
                    ? "Путь новичка полностью завершён"
                    : "Не удалось завершить сюжет за " +
                      guard +
                      " шагов";
        }

        private void PrepareLateGameForTesting()
        {
            CancelAllActivities();

            wallet?.SetCredits(
                1000000);

            reputation?.SetReputation(
                100000);

            onboarding?.CompleteForTesting();
            CompleteStoryForTesting();
            season?.CompleteForTesting();
            daily?.CompleteForTesting();
            club?.SetClubForTesting(
                0);
            club?.CompleteWeeklyForTesting();

            disciplines?.SetAllLevelsForTesting(
                10);

            mastery?.SetAllVehicleLevelsForTesting(
                10);

            garage?.SetAllUpgradeLevelsForTesting(
                5);

            career?.SetStageForTesting(
                3);

            vehicleHistory?.SetAllLegendaryForTesting();
            collection?.ClaimAllForTesting();
            legends?.UnlockCurrentForTesting();

            lastAction =
                "Подготовлен поздний прогресс для QA";
        }

        private void DiscoverFirstMissing()
        {
            if (discoveries == null)
                return;

            for (int i = 0;
                 i < discoveries.DiscoveryCount;
                 i++)
            {
                if (discoveries.IsFound(
                        i))
                {
                    continue;
                }

                discoveries.DiscoverForTesting(
                    i);

                lastAction =
                    "Засчитано открытие: " +
                    discoveries.GetDiscoveryName(
                        i);

                return;
            }

            lastAction =
                "Все открытия уже найдены";
        }

        private static string ProfessionActivityId(
            int index)
        {
            return index switch
            {
                0 => "profession_pizza",
                1 => "profession_taxi",
                2 => "profession_mail",
                3 => "profession_icecream",
                _ => "profession_pizza"
            };
        }

        private void CreditsButton(
            string label,
            int value)
        {
            if (!Button(
                    label))
            {
                return;
            }

            wallet?.SetCredits(
                value);

            lastAction =
                "Кредиты = " +
                Value(
                    value);
        }

        private void RepButton(
            string label,
            int value)
        {
            if (!Button(
                    label))
            {
                return;
            }

            reputation?.SetReputation(
                value);

            lastAction =
                "Репутация = " +
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
            if (!Button(
                    label))
            {
                return;
            }

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
            if (!Button(
                    label))
            {
                return;
            }

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
                        ? "Выбрана машина " +
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

        private void CloseResult()
        {
            if (activities != null &&
                activities.HasResult)
            {
                activities.DismissResult();
                car?.SetDrivingBlocked(
                    "ActivityResult",
                    false);

                lastAction =
                    "Result закрыт";
            }
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

            forward.y =
                0f;

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

            car?.SetDrivingBlocked(
                "ActivityCountdown",
                false);
            car?.SetDrivingBlocked(
                "ActivityResult",
                false);

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
            daily?.ResetForTesting();
            season?.ResetForTesting();
            story?.ResetForTesting();
            onboarding?.ResetForTesting();
            discoveries?.ResetForTesting();
            speedTraps?.ResetForTesting();
            driftSpots?.ResetForTesting();
            photoHunt?.ResetForTesting();
            achievements?.ResetForTesting();

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

            OpeningCinematicCamera openingCamera =
                Object.FindAnyObjectByType<
                    OpeningCinematicCamera>();

            if (openingCamera != null)
            {
                openingCamera.Replay(
                    5f);
            }

            PrototypeHud hud =
                Object.FindAnyObjectByType<
                    PrototypeHud>();

            if (hud != null)
            {
                hud.ReplayOpeningHudReveal(
                    5f);
            }

            onboarding?.ShowWelcomeAfterDelay(
                5f);
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
