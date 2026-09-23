#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MotorCity.Input;
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
            "ОБЗОР",
            "ПРОГРЕСС",
            "МАШИНЫ",
            "АКТИВНОСТИ",
            "МИР",
            "СИСТЕМЫ"
        };

        private static readonly string[] VehicleNames =
        {
            "STREET",
            "SPRINT",
            "RANGER",
            "VORTEX",
            "APEX",
            "BUS"
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
        private ActivityManager activityManager;
        private ArcadeCarController car;
        private DeliveryActivity delivery;
        private DriftChallenge driftChallenge;
        private StreetSprintActivity sprint;
        private CircuitRaceActivity circuit;
        private StoryMissionSystem story;
        private DayNightCycleController dayNight;

        private readonly List<MonoBehaviour> systems = new();

        private bool visible;
        private int selectedTab;
        private Vector2 scroll;
        private float refreshTimer;
        private string lastAction = "Готово";

        private Rect windowRect =
            new Rect(18f, 62f, 720f, 780f);

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
            StoryMissionSystem storySystem)
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
            activityManager = manager;
            car = targetCar;
            delivery = deliveryActivity;
            driftChallenge = driftActivity;
            sprint = sprintActivity;
            circuit = circuitActivity;
            story = storySystem;

            RefreshSystems();
        }

        private void Update()
        {
            if (MotorCityInput.AdminTogglePressed)
            {
                visible = !visible;

                if (visible)
                    RefreshSystems();
            }

            if (!visible)
                return;

            refreshTimer -= Time.unscaledDeltaTime;

            if (refreshTimer <= 0f)
            {
                refreshTimer = 2f;
                RefreshSystems();
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
                    "MOTOR CITY — ADMIN / TEST");
        }

        private void DrawWindow(int id)
        {
            selectedTab =
                GUILayout.Toolbar(
                    selectedTab,
                    Tabs,
                    GUILayout.Height(30f));

            GUILayout.Space(6f);

            scroll =
                GUILayout.BeginScrollView(
                    scroll,
                    false,
                    true);

            DrawHeader();

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

            GUILayout.Space(14f);
            GUILayout.Label("ПОСЛЕДНЕЕ: " + lastAction);
            GUILayout.Label("F10 / TILDE — закрыть панель");

            GUILayout.EndScrollView();

            GUI.DragWindow(
                new Rect(0f, 0f, 10000f, 25f));
        }

        private void DrawHeader()
        {
            string vehicle =
                roster != null
                    ? roster.SelectedName
                    : "—";

            GUILayout.Label(
                "КР " +
                (wallet == null ? 0 : wallet.Credits).ToString("N0") +
                "   |   РЕП " +
                (reputation == null ? 0 : reputation.Reputation).ToString("N0") +
                "   |   " +
                vehicle);

            if (car != null)
            {
                GUILayout.Label(
                    "СКОРОСТЬ " +
                    car.SpeedKph.ToString("0") +
                    " км/ч   |   РЕЖИМ " +
                    car.CurrentDriveMode +
                    "   |   ДРИФТ " +
                    (car.IsSliding ? "ДА" : "НЕТ"));
            }

            if (activityManager != null)
            {
                GUILayout.Label(
                    activityManager.IsBusy
                        ? "АКТИВНОСТЬ: " + activityManager.ActiveId
                        : "АКТИВНОСТЬ: свободный режим");
            }

            Separator();
        }

        private void DrawOverview()
        {
            Section("БЫСТРЫЕ ПРЕСЕТЫ");

            GUILayout.BeginHorizontal();

            if (Button("МАКСИМУМ"))
                MaxEverything();

            if (Button("СЕРЕДИНА ИГРЫ"))
                MidGame();

            if (Button("ЧИСТЫЙ СТАРТ"))
                ResetEverything();

            GUILayout.EndHorizontal();

            Section("ЭКОНОМИКА");

            GUILayout.BeginHorizontal();

            if (Button("+10 000 КР"))
            {
                wallet?.AddCredits(10000);
                lastAction = "+10 000 КР";
            }

            if (Button("1 000 000 КР"))
            {
                wallet?.SetCredits(1000000);
                lastAction = "КР = 1 000 000";
            }

            if (Button("0 КР"))
            {
                wallet?.SetCredits(0);
                lastAction = "КР = 0";
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("РЕП 0"))
                SetRep(0);

            if (Button("РЕП 3 500"))
                SetRep(3500);

            if (Button("РЕП 10 000"))
                SetRep(10000);

            GUILayout.EndHorizontal();

            Section("ТЕКУЩЕЕ СОСТОЯНИЕ");

            if (disciplines != null)
            {
                GUILayout.Label(
                    "ГОНКИ " +
                    disciplines.RacingLevel +
                    "/10   •   ДРИФТ " +
                    disciplines.DriftLevel +
                    "/10   •   ДОСТАВКА " +
                    disciplines.DeliveryLevel +
                    "/10");
            }

            if (mastery != null)
                GUILayout.Label("МАСТЕРСТВО " + mastery.CurrentLevel + "/10");

            if (vehicleSpecialization != null)
                GUILayout.Label(vehicleSpecialization.GarageLine);

            if (collection != null)
                GUILayout.Label(collection.GarageLine);

            if (legends != null)
                GUILayout.Label(legends.AdminLine);

            if (contracts != null)
                GUILayout.Label(contracts.AdminLine);

            if (liveEvents != null)
                GUILayout.Label(liveEvents.AdminLine);

            if (underground != null)
                GUILayout.Label(underground.AdminLine);

            if (cityRisk != null)
                GUILayout.Label(cityRisk.AdminLine);
        }

        private void DrawProgress()
        {
            Section("ДИСЦИПЛИНЫ");
            DrawDiscipline("ГОНКИ", DisciplineType.Racing);
            DrawDiscipline("ДРИФТ", DisciplineType.Drift);
            DrawDiscipline("ДОСТАВКА", DisciplineType.Delivery);

            Section("КАРЬЕРА");

            GUILayout.BeginHorizontal();

            for (int i = 0; i <= 3; i++)
            {
                int stage = i;

                if (Button(i == 3 ? "ГОТОВО" : "ЭТАП " + i))
                {
                    career?.SetStageForTesting(stage);
                    lastAction = "Карьера: этап " + stage;
                }
            }

            GUILayout.EndHorizontal();

            Section("ПУТЬ НОВИЧКА");

            if (story != null)
            {
                GUILayout.Label(
                    story.IsComplete
                        ? "ЗАВЕРШЁН"
                        : "ЭТАП " +
                          story.CurrentMissionNumber +
                          "/" +
                          story.MissionCount);

                GUILayout.BeginHorizontal();

                if (Button("+1 ЭТАП"))
                {
                    story.AdvanceMissionForTesting();
                    lastAction = "Путь новичка продвинут";
                }

                if (Button("СБРОС"))
                {
                    story.ResetForTesting();
                    lastAction = "Путь новичка сброшен";
                }

                GUILayout.EndHorizontal();
            }

            Section("КОЛЛЕКЦИЯ / ЛЕГЕНДЫ");

            GUILayout.BeginHorizontal();

            if (Button("КОЛЛЕКЦИЯ: ПЕРЕСЧЁТ"))
                Run("Коллекция пересчитана", () => collection?.RecalculateForTesting());

            if (Button("КОЛЛЕКЦИЯ: НАГРАДЫ"))
                Run("Награды коллекции выданы", () => collection?.ClaimAllForTesting());

            if (Button("КОЛЛЕКЦИЯ: СБРОС"))
                Run("Награды коллекции сброшены", () => collection?.ResetMilestonesForTesting());

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("ЛЕГЕНДА: ОТКРЫТЬ"))
                Run("Легенда открыта", () => legends?.UnlockCurrentForTesting());

            if (Button("ЛЕГЕНДА: ПРОЙТИ"))
                Run("Легенда завершена", () => legends?.CompleteCurrentForTesting());

            if (Button("ЛЕГЕНДЫ: СБРОС"))
                Run("Легенды сброшены", () => legends?.ResetForTesting());

            GUILayout.EndHorizontal();

            Section("КОНТРАКТЫ / СОБЫТИЯ / ПОДПОЛЬЕ");

            GUILayout.BeginHorizontal();

            if (Button("КОНТРАКТ: ГОТОВО"))
                Run("Контракт завершён", () => contracts?.CompleteCurrentForTesting());

            if (Button("КОНТРАКТЫ: ЦИКЛ 5"))
                Run("Контракты: цикл 5", () => contracts?.SetCycleForTesting(5));

            if (Button("КОНТРАКТЫ: СБРОС"))
                Run("Контракты сброшены", () => contracts?.ResetForTesting());

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("СОБЫТИЕ: ГОТОВО"))
                Run("Событие завершено", () => liveEvents?.CompleteCurrentForTesting());

            if (Button("СОБЫТИЕ: СЛЕДУЮЩЕЕ"))
                Run("Событие переключено", () => liveEvents?.NextEventForTesting());

            if (Button("СОБЫТИЯ: СБРОС"))
                Run("События сброшены", () => liveEvents?.ResetForTesting());

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("ПОДПОЛЬЕ +40"))
                Run("Подполье +40", () => underground?.AddCredForTesting(40));

            if (Button("ПОДПОЛЬЕ: ОТКРЫТЬ"))
                Run("Подполье открыто", () => underground?.UnlockCurrentForTesting());

            if (Button("ПОДПОЛЬЕ: ПРОЙТИ"))
                Run("Подполье завершено", () => underground?.CompleteCurrentForTesting());

            if (Button("ПОДПОЛЬЕ: СБРОС"))
                Run("Подполье сброшено", () => underground?.ResetForTesting());

            GUILayout.EndHorizontal();

            Section("ПОЛИЦИЯ");

            GUILayout.BeginHorizontal();

            if (Button("+20 ВНИМАНИЯ"))
                Run("Полиция +20", () => cityRisk?.AddAttentionForTesting(20f));

            if (Button("+50 ВНИМАНИЯ"))
                Run("Полиция +50", () => cityRisk?.AddAttentionForTesting(50f));

            if (Button("СБРОСИТЬ"))
                Run("Внимание сброшено", () => cityRisk?.ClearForTesting());

            GUILayout.EndHorizontal();
        }

        private void DrawVehicles()
        {
            Section("МАШИНЫ");

            for (int row = 0; row < 2; row++)
            {
                GUILayout.BeginHorizontal();

                for (int column = 0; column < 3; column++)
                {
                    int index = row * 3 + column;

                    if (index >= VehicleNames.Length)
                        continue;

                    if (Button(VehicleNames[index]))
                        SelectVehicle(index);
                }

                GUILayout.EndHorizontal();
            }

            Section("ТЮНИНГ");

            GUILayout.BeginHorizontal();

            if (Button("ВСЁ 0/5"))
                Run("Тюнинг 0/5", () => garage?.SetAllUpgradeLevelsForTesting(0));

            if (Button("ВСЁ 3/5"))
                Run("Тюнинг 3/5", () => garage?.SetAllUpgradeLevelsForTesting(3));

            if (Button("ВСЁ 5/5"))
                Run("Тюнинг 5/5", () => garage?.SetAllUpgradeLevelsForTesting(5));

            GUILayout.EndHorizontal();

            Section("МАСТЕРСТВО");

            GUILayout.BeginHorizontal();

            if (Button("УР. 1"))
                Run("Мастерство 1", () => mastery?.SetCurrentLevelForTesting(1));

            if (Button("УР. 5"))
                Run("Мастерство 5", () => mastery?.SetCurrentLevelForTesting(5));

            if (Button("УР. 10"))
                Run("Мастерство 10", () => mastery?.SetCurrentLevelForTesting(10));

            if (Button("ВСЕ = 10"))
                Run("Все машины: мастерство 10", () => mastery?.SetAllVehicleLevelsForTesting(10));

            GUILayout.EndHorizontal();

            Section("КАСТОМИЗАЦИЯ");

            MonoBehaviour customization =
                FindSystem("VehicleCustomizationSystem");

            GUILayout.BeginHorizontal();

            if (Button("ПОКРАСКА"))
                InvokeNoArg(customization, "CycleBodyColor");

            if (Button("ДИСКИ"))
                InvokeNoArg(customization, "CycleWheelStyle");

            if (Button("НЕОН"))
                InvokeNoArg(customization, "CycleNeon");

            GUILayout.EndHorizontal();

            Section("ПИКСИ");

            MonoBehaviour pixie =
                FindSystem("TurboPetSystem");

            GUILayout.BeginHorizontal();

            if (Button("СЛЕД. СКИН"))
                InvokeNoArg(pixie, "CycleSkin");

            if (Button("+100 XP"))
                InvokeNumber(pixie, "AddXp", 100d);

            GUILayout.EndHorizontal();

            DrawStatus(pixie, 8);
        }

        private void DrawActivities()
        {
            Section("ТЕЛЕПОРТ");

            GUILayout.BeginHorizontal();

            if (Button("СТАРТ"))
                Teleport(CityAssetRuntimeInstaller.PlayerSpawnPoint, CityAssetRuntimeInstaller.PlayerSpawnRotation);

            if (Button("ГАРАЖ"))
                Teleport(CityAssetRuntimeInstaller.GaragePoint, Quaternion.identity);

            if (Button("ДРИФТ"))
                Teleport(CityAssetRuntimeInstaller.DriftChallengePoint, Quaternion.identity);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("ДОСТАВКА"))
                TeleportRoute(CityAssetRuntimeInstaller.DeliveryRoute);

            if (Button("СПРИНТ"))
                TeleportRoute(CityAssetRuntimeInstaller.SprintRoute);

            if (Button("КОЛЬЦО"))
                TeleportRoute(CityAssetRuntimeInstaller.CircuitRoute);

            GUILayout.EndHorizontal();

            Section("УПРАВЛЕНИЕ");

            GUILayout.BeginHorizontal();

            if (Button("ОТМЕНИТЬ ВСЁ"))
                CancelActivities();

            if (Button("СПАСТИ МАШИНУ"))
                RescueCar();

            if (Button("СКРЫТЬ РЕЗУЛЬТАТ"))
            {
                if (activityManager != null && activityManager.HasResult)
                {
                    activityManager.DismissResult();
                    lastAction = "Результат закрыт";
                }
            }

            GUILayout.EndHorizontal();

            DrawNamedSystem("DailyAdventureSystem");
            DrawNamedSystem("SeasonSystem");
            DrawNamedSystem("PhotoHuntSystem");
            DrawNamedSystem("WeekendEventSystem");
        }

        private void DrawWorld()
        {
            Section("ВРЕМЯ СУТОК");

            dayNight ??=
                UnityEngine.Object.FindAnyObjectByType<DayNightCycleController>();

            GUILayout.BeginHorizontal();

            if (Button("НОЧЬ"))
                SetTime(0f);

            if (Button("РАССВЕТ"))
                SetTime(0.25f);

            if (Button("ДЕНЬ"))
                SetTime(0.5f);

            if (Button("ЗАКАТ"))
                SetTime(0.75f);

            GUILayout.EndHorizontal();

            if (dayNight != null)
            {
                GUILayout.Label(
                    "TIME " +
                    dayNight.TimeOfDay01.ToString("0.00") +
                    "   |   NIGHT " +
                    (dayNight.IsNight ? "YES" : "NO") +
                    "   |   AMOUNT " +
                    dayNight.NightAmount.ToString("0.00"));
            }

            Section("РАБОТЫ / СОЦИАЛЬНЫЕ СИСТЕМЫ");

            DrawNamedSystem("CityProfessionSystem");
            DrawNamedSystem("CarWashJobSystem");
            DrawNamedSystem("TowTruckJobSystem");
            DrawNamedSystem("ClubSystem");

            Section("МАГАЗИН / НАГРАДЫ");

            DrawNamedSystem("CosmeticStoreSystem");
            DrawNamedSystem("RewardedBonusSystem");
            DrawNamedSystem("AchievementSystem");
        }

        private void DrawSystems()
        {
            GUILayout.Label(
                "Все активные MotorCity.Gameplay системы. " +
                "Панель автоматически показывает их public-состояние " +
                "и test/debug методы без параметров.");

            GUILayout.Space(6f);

            foreach (MonoBehaviour system in systems)
            {
                if (system == null)
                    continue;

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(system.GetType().Name);

                DrawStatus(system, 10);

                MethodInfo[] methods =
                    system.GetType()
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .Where(
                            method =>
                                method.ReturnType == typeof(void) &&
                                method.GetParameters().Length == 0 &&
                                method.Name.EndsWith(
                                    "ForTesting",
                                    StringComparison.Ordinal))
                        .OrderBy(method => method.Name)
                        .ToArray();

                int index = 0;

                foreach (MethodInfo method in methods)
                {
                    if (index % 3 == 0)
                        GUILayout.BeginHorizontal();

                    if (Button(
                            method.Name.Replace(
                                "ForTesting",
                                string.Empty)
                            .ToUpperInvariant()))
                    {
                        try
                        {
                            method.Invoke(system, null);
                            lastAction =
                                system.GetType().Name +
                                "." +
                                method.Name;
                        }
                        catch (Exception exception)
                        {
                            lastAction =
                                "ОШИБКА: " +
                                exception.GetBaseException().Message;
                        }
                    }

                    index++;

                    if (index % 3 == 0)
                        GUILayout.EndHorizontal();
                }

                if (index % 3 != 0)
                    GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
        }

        private void DrawNamedSystem(string name)
        {
            MonoBehaviour system =
                FindSystem(name);

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(name);
            DrawStatus(system, 7);
            GUILayout.EndVertical();
        }

        private void DrawStatus(
            MonoBehaviour system,
            int max)
        {
            if (system == null)
            {
                GUILayout.Label("— не найдено");
                return;
            }

            PropertyInfo[] properties =
                system.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(
                        property =>
                            property.CanRead &&
                            property.GetIndexParameters().Length == 0 &&
                            Displayable(property.PropertyType))
                    .Take(max)
                    .ToArray();

            foreach (PropertyInfo property in properties)
            {
                try
                {
                    object value =
                        property.GetValue(system);

                    GUILayout.Label(
                        property.Name +
                        ": " +
                        (value ?? "null"));
                }
                catch
                {
                }
            }
        }

        private void RefreshSystems()
        {
            systems.Clear();

            systems.AddRange(
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                        FindObjectsSortMode.None)
                    .Where(
                        item =>
                            item != null &&
                            item != this &&
                            item.GetType().Namespace ==
                            "MotorCity.Gameplay")
                    .OrderBy(
                        item =>
                            item.GetType().Name));
        }

        private MonoBehaviour FindSystem(string name)
        {
            return
                systems.FirstOrDefault(
                    item =>
                        item != null &&
                        item.GetType().Name == name);
        }

        private void DrawDiscipline(
            string label,
            DisciplineType type)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100f));

            if (Button("УР. 1"))
                disciplines?.SetLevelForTesting(type, 1);

            if (Button("УР. 3"))
                disciplines?.SetLevelForTesting(type, 3);

            if (Button("УР. 10"))
                disciplines?.SetLevelForTesting(type, 10);

            GUILayout.EndHorizontal();
        }

        private void SelectVehicle(int index)
        {
            string status = string.Empty;

            if (roster != null &&
                roster.SelectVehicleForTesting(index, out status))
            {
                lastAction = status;
                return;
            }

            lastAction =
                string.IsNullOrWhiteSpace(status)
                    ? "Не удалось выбрать машину"
                    : status;
        }

        private void SetRep(int value)
        {
            reputation?.SetReputation(value);
            lastAction = "РЕП = " + value.ToString("N0");
        }

        private void SetTime(float value)
        {
            dayNight?.SetTimeOfDay(value);
            lastAction = "Время = " + value.ToString("0.00");
        }

        private void MidGame()
        {
            wallet?.SetCredits(120000);
            reputation?.SetReputation(3500);
            disciplines?.SetAllLevelsForTesting(4);
            mastery?.SetCurrentLevelForTesting(5);
            garage?.SetAllUpgradeLevelsForTesting(3);
            career?.SetStageForTesting(2);
            collection?.RecalculateForTesting();
            lastAction = "Пресет середины игры";
        }

        private void MaxEverything()
        {
            wallet?.SetCredits(1000000);
            reputation?.SetReputation(10000);
            disciplines?.SetAllLevelsForTesting(10);
            mastery?.SetAllVehicleLevelsForTesting(10);
            garage?.SetAllUpgradeLevelsForTesting(5);
            career?.SetStageForTesting(3);
            vehicleHistory?.SetAllLegendaryForTesting();
            collection?.RecalculateForTesting();
            legends?.UnlockCurrentForTesting();
            contracts?.SetCycleForTesting(5);
            liveEvents?.CompleteCurrentForTesting();
            underground?.AddCredForTesting(400);
            lastAction = "Максимальный тестовый прогресс";
        }

        private void ResetEverything()
        {
            CancelActivities();
            wallet?.SetCredits(0);
            reputation?.SetReputation(0);
            disciplines?.SetAllLevelsForTesting(1);
            mastery?.SetAllVehicleLevelsForTesting(1);
            garage?.SetAllUpgradeLevelsForTesting(0);
            career?.SetStageForTesting(0);
            vehicleHistory?.ResetAllForTesting();
            collection?.ResetMilestonesForTesting();
            legends?.ResetForTesting();
            contracts?.ResetForTesting();
            liveEvents?.ResetForTesting();
            underground?.ResetForTesting();
            cityRisk?.ClearForTesting();
            story?.ResetForTesting();

            if (roster != null)
                roster.SelectVehicleForTesting(0, out _);

            lastAction = "Тестовый прогресс сброшен";
        }

        private void TeleportRoute(Vector3[] route)
        {
            if (route == null || route.Length == 0)
            {
                lastAction = "Маршрут отсутствует";
                return;
            }

            Vector3 forward =
                route.Length > 1
                    ? route[1] - route[0]
                    : Vector3.forward;

            forward.y = 0f;

            Quaternion rotation =
                forward.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(
                        forward.normalized,
                        Vector3.up)
                    : Quaternion.identity;

            Teleport(route[0], rotation);
        }

        private void Teleport(
            Vector3 position,
            Quaternion rotation)
        {
            if (car == null)
                return;

            CancelActivities();

            car.TeleportTo(
                position + Vector3.up * 1.1f,
                rotation);

            car.SetDrivingEnabled(true);

            lastAction =
                "Телепорт: " +
                position.x.ToString("0") +
                ", " +
                position.z.ToString("0");
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

            Teleport(position, rotation);
        }

        private void CancelActivities()
        {
            delivery?.CancelActivity();
            driftChallenge?.CancelActivity();
            sprint?.CancelActivity();
            circuit?.CancelActivity();

            if (activityManager != null)
            {
                if (activityManager.HasResult)
                    activityManager.DismissResult();

                if (activityManager.IsBusy)
                    activityManager.End(activityManager.ActiveId);
            }

            car?.SetDrivingEnabled(true);
            lastAction = "Активности отменены";
        }

        private void InvokeNoArg(
            MonoBehaviour target,
            string methodName)
        {
            if (target == null)
            {
                lastAction = "Система не найдена";
                return;
            }

            MethodInfo method =
                target.GetType()
                    .GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public);

            if (method == null ||
                method.GetParameters().Length != 0)
            {
                lastAction = "Метод не найден: " + methodName;
                return;
            }

            method.Invoke(target, null);
            lastAction =
                target.GetType().Name +
                "." +
                methodName;
        }

        private void InvokeNumber(
            MonoBehaviour target,
            string methodName,
            double value)
        {
            if (target == null)
            {
                lastAction = "Система не найдена";
                return;
            }

            MethodInfo method =
                target.GetType()
                    .GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public)
                    .FirstOrDefault(
                        candidate =>
                            candidate.Name == methodName &&
                            candidate.GetParameters().Length == 1);

            if (method == null)
            {
                lastAction = "Метод не найден: " + methodName;
                return;
            }

            Type type =
                method.GetParameters()[0].ParameterType;

            object converted =
                Convert.ChangeType(value, type);

            method.Invoke(
                target,
                new[]
                {
                    converted
                });

            lastAction =
                target.GetType().Name +
                "." +
                methodName;
        }

        private void Run(
            string status,
            Action action)
        {
            action?.Invoke();
            lastAction = status;
        }

        private static bool Displayable(Type type)
        {
            return
                type == typeof(string) ||
                type == typeof(bool) ||
                type == typeof(int) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(long) ||
                type.IsEnum;
        }

        private static void Section(string title)
        {
            GUILayout.Space(10f);
            GUILayout.Label("── " + title + " ──");
        }

        private static void Separator()
        {
            GUILayout.Space(4f);
            GUILayout.Box(
                GUIContent.none,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(1f));
            GUILayout.Space(3f);
        }

        private static bool Button(string text)
        {
            return
                GUILayout.Button(
                    text,
                    GUILayout.MinWidth(105f),
                    GUILayout.Height(30f));
        }
    }
}
#endif
