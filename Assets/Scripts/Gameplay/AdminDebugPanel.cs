using MotorCity.Input;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class AdminDebugPanel : MonoBehaviour
    {
        private const int WindowId = 73921;

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
        private DayNightCycleController dayNight;

        private bool visible;
        private Vector2 scroll;
        private string lastAction =
            "Готово к тестированию";

        private Rect windowRect =
            new(20f, 80f, 470f, 720f);

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
            CircuitRaceActivity circuitActivity)
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
        }

        private void Update()
        {
            if (MotorCityInput.AdminTogglePressed)
            {
                visible = !visible;
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
                    "MOTOR CITY — АДМИН / ТЕСТ");
        }

        private void DrawWindow(
            int id)
        {
            scroll =
                GUILayout.BeginScrollView(
                    scroll,
                    false,
                    true);

            DrawSummary();
            DrawPresets();
            DrawMoneyAndRep();
            DrawDisciplines();
            DrawVehicles();
            DrawUpgradesAndMastery();
            DrawCollection();
            DrawLegends();
            DrawCareer();
            DrawContracts();
            DrawLiveEvents();
            DrawUnderground();
            DrawPoliceRisk();
            DrawTime();
            DrawTeleports();

            GUILayout.Space(12f);
            GUILayout.Label(
                $"ПОСЛЕДНЕЕ: {lastAction}");

            GUILayout.Label(
                "F10 / ` — закрыть панель");

            GUILayout.EndScrollView();

            GUI.DragWindow(
                new Rect(
                    0f,
                    0f,
                    10000f,
                    24f));
        }

        private void DrawSummary()
        {
            GUILayout.Label(
                $"КР {(wallet == null ? 0 : wallet.Credits):N0}   •   " +
                $"РЕП {(reputation == null ? 0 : reputation.Reputation):N0}");

            if (disciplines != null)
            {
                GUILayout.Label(
                    $"ГОНКИ {disciplines.RacingLevel}/10   •   " +
                    $"ДРИФТ {disciplines.DriftLevel}/10   •   " +
                    $"ДОСТАВКА {disciplines.DeliveryLevel}/10");
            }

            if (roster != null &&
                mastery != null)
            {
                GUILayout.Label(
                    $"{roster.SelectedName}   •   " +
                    $"МАСТЕРСТВО {mastery.CurrentLevel}/10");
            }

            if (vehicleSpecialization != null)
            {
                GUILayout.Label(
                    vehicleSpecialization.GarageLine);
            }

            if (collection != null)
            {
                GUILayout.Label(
                    collection.GarageLine);
            }

            if (legends != null)
            {
                GUILayout.Label(
                    legends.AdminLine);
            }

            if (contracts != null)
            {
                GUILayout.Label(
                    contracts.AdminLine);
            }

            if (liveEvents != null)
            {
                GUILayout.Label(
                    liveEvents.AdminLine);
            }

            GUILayout.Space(8f);
        }

        private void DrawPresets()
        {
            GUILayout.Label("БЫСТРЫЕ ПРЕСЕТЫ");

            GUILayout.BeginHorizontal();

            if (Button("МАКСИМУМ ВСЕГО"))
                MaxEverything();

            if (Button("ГОТОВО К ЭЛИТЕ"))
                EliteReady();

            GUILayout.EndHorizontal();

            if (Button("СБРОСИТЬ ТЕСТОВЫЙ ПРОГРЕСС"))
                ResetTestProgression();

            GUILayout.Space(10f);
        }

        private void DrawMoneyAndRep()
        {
            GUILayout.Label("ДЕНЬГИ / ОБЩАЯ РЕПУТАЦИЯ");

            GUILayout.BeginHorizontal();

            if (Button("+10 000 КР"))
                wallet?.AddCredits(10000);

            if (Button("КР = 1 000 000"))
                wallet?.SetCredits(1000000);

            if (Button("КР = 0"))
                wallet?.SetCredits(0);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("РЕП = 0"))
                reputation?.SetReputation(0);

            if (Button("РЕП = 3 500"))
                reputation?.SetReputation(3500);

            if (Button("РЕП = 10 000"))
                reputation?.SetReputation(10000);

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
        }

        private void DrawDisciplines()
        {
            GUILayout.Label("ДИСЦИПЛИНЫ");

            DrawDisciplineRow(
                "ГОНКИ",
                DisciplineType.Racing);

            DrawDisciplineRow(
                "ДРИФТ",
                DisciplineType.Drift);

            DrawDisciplineRow(
                "ДОСТАВКА",
                DisciplineType.Delivery);

            GUILayout.Space(10f);
        }

        private void DrawDisciplineRow(
            string label,
            DisciplineType type)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                label,
                GUILayout.Width(82f));

            if (Button("УР. 1", 78f))
                disciplines?.SetLevelForTesting(
                    type,
                    1);

            if (Button("УР. 3", 78f))
                disciplines?.SetLevelForTesting(
                    type,
                    3);

            if (Button("УР. 10", 78f))
                disciplines?.SetLevelForTesting(
                    type,
                    10);

            GUILayout.EndHorizontal();
        }

        private void DrawVehicles()
        {
            GUILayout.Label("МАШИНЫ");

            string[] names =
            {
                "УЛИЧНАЯ",
                "КЛУБНАЯ",
                "МАСЛКАР",
                "ГРАН-ТУРИЗМО",
                "АПЕКС"
            };

            GUILayout.BeginHorizontal();

            for (int i = 0;
                 i < names.Length;
                 i++)
            {
                if (!Button(
                        names[i],
                        82f))
                {
                    continue;
                }

                string status =
                    string.Empty;

                if (roster != null &&
                    roster.SelectVehicleForTesting(
                        i,
                        out status))
                {
                    lastAction =
                        status;
                }
                else if (!string.IsNullOrWhiteSpace(
                             status))
                {
                    lastAction =
                        status;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
        }

        private void DrawUpgradesAndMastery()
        {
            GUILayout.Label("ТЮНИНГ");

            GUILayout.BeginHorizontal();

            if (Button("ВСЁ 0/5"))
                garage?.SetAllUpgradeLevelsForTesting(0);

            if (Button("ВСЁ 3/5"))
                garage?.SetAllUpgradeLevelsForTesting(3);

            if (Button("ВСЁ 5/5"))
                garage?.SetAllUpgradeLevelsForTesting(5);

            GUILayout.EndHorizontal();

            GUILayout.Label("МАСТЕРСТВО ТЕКУЩЕЙ МАШИНЫ");

            GUILayout.BeginHorizontal();

            if (Button("УР. 1"))
                mastery?.SetCurrentLevelForTesting(1);

            if (Button("УР. 5"))
                mastery?.SetCurrentLevelForTesting(5);

            if (Button("УР. 10"))
                mastery?.SetCurrentLevelForTesting(10);

            GUILayout.EndHorizontal();

            if (Button("ВСЕ МАШИНЫ — МАСТЕРСТВО 10"))
                mastery?.SetAllVehicleLevelsForTesting(10);

            GUILayout.Space(10f);
        }

        private void DrawCollection()
        {
            GUILayout.Label("КОЛЛЕКЦИЯ");

            if (collection != null)
            {
                GUILayout.Label(
                    collection.GarageLine);
            }

            GUILayout.BeginHorizontal();

            if (Button("ПЕРЕСЧИТАТЬ"))
            {
                collection?.RecalculateForTesting();
                lastAction =
                    "Коллекционный рейтинг пересчитан";
            }

            if (Button("ЗАБРАТЬ ВСЁ (ТЕСТ)"))
            {
                collection?.ClaimAllForTesting();
                lastAction =
                    "Коллекционные награды выданы";
            }

            GUILayout.EndHorizontal();

            if (Button("СБРОСИТЬ НАГРАДЫ КОЛЛЕКЦИИ"))
            {
                collection?.ResetMilestonesForTesting();
                lastAction =
                    "Награды коллекции сброшены";
            }

            GUILayout.Space(10f);
        }

        private void DrawLegends()
        {
            GUILayout.Label("ЛЕГЕНДЫ ГОРОДА");

            if (legends != null)
            {
                GUILayout.Label(
                    legends.AdminLine);
            }

            GUILayout.BeginHorizontal();

            if (Button("РАЗБЛОКИРОВАТЬ"))
            {
                legends?.UnlockCurrentForTesting();
                lastAction =
                    "Текущая легенда разблокирована";
            }

            if (Button("ПОБЕДИТЬ"))
            {
                legends?.CompleteCurrentForTesting();
                lastAction =
                    "Текущая легенда завершена";
            }

            GUILayout.EndHorizontal();

            if (Button("СБРОСИТЬ ЛЕГЕНД"))
            {
                legends?.ResetForTesting();
                lastAction =
                    "Легенды города сброшены";
            }

            GUILayout.Space(10f);
        }

        private void DrawCareer()
        {
            GUILayout.Label("КАРЬЕРА");

            GUILayout.BeginHorizontal();

            if (Button("ЭТАП 0"))
                career?.SetStageForTesting(0);

            if (Button("ЭТАП 1"))
                career?.SetStageForTesting(1);

            if (Button("ЭТАП 2"))
                career?.SetStageForTesting(2);

            if (Button("ГОТОВО"))
                career?.SetStageForTesting(3);

            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
        }

        private void DrawContracts()
        {
            GUILayout.Label("КОНТРАКТЫ");

            if (contracts != null)
            {
                GUILayout.Label(
                    contracts.AdminLine);
            }

            GUILayout.BeginHorizontal();

            if (Button("ЗАВЕРШИТЬ ТЕКУЩИЙ"))
            {
                contracts?.CompleteCurrentForTesting();
                lastAction =
                    "Текущий контракт завершён";
            }

            if (Button("ЦИКЛ 5"))
            {
                contracts?.SetCycleForTesting(5);
                lastAction =
                    "Контракты: цикл 5";
            }

            GUILayout.EndHorizontal();

            if (Button("СБРОСИТЬ КОНТРАКТЫ"))
            {
                contracts?.ResetForTesting();
                lastAction =
                    "Контракты сброшены";
            }

            GUILayout.Space(10f);
        }

        private void DrawLiveEvents()
        {
            GUILayout.Label("ГОРОДСКИЕ СОБЫТИЯ");

            if (liveEvents != null)
            {
                GUILayout.Label(
                    liveEvents.AdminLine);
            }

            GUILayout.BeginHorizontal();

            if (Button("ЗАВЕРШИТЬ СОБЫТИЕ"))
            {
                liveEvents?.CompleteCurrentForTesting();
                lastAction =
                    "Городское событие завершено";
            }

            if (Button("СЛЕДУЮЩЕЕ СОБЫТИЕ"))
            {
                liveEvents?.NextEventForTesting();
                lastAction =
                    "Городское событие переключено";
            }

            GUILayout.EndHorizontal();

            if (Button("СБРОСИТЬ ГОРОДСКИЕ СОБЫТИЯ"))
            {
                liveEvents?.ResetForTesting();
                lastAction =
                    "Городские события сброшены";
            }

            GUILayout.Space(10f);
        }

        private void DrawUnderground()
        {
            GUILayout.Label("ПОДПОЛЬЕ");

            if (underground != null)
            {
                GUILayout.Label(
                    underground.AdminLine);
            }

            GUILayout.BeginHorizontal();

            if (Button("+40 АВТОРИТЕТА"))
            {
                underground?.AddCredForTesting(
                    40);

                lastAction =
                    "Подполье: +40 уличного авторитета";
            }

            if (Button("РАЗБЛОКИРОВАТЬ"))
            {
                underground?.UnlockCurrentForTesting();

                lastAction =
                    "Подполье событие разблокирован";
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("ЗАВЕРШИТЬ СОБЫТИЕ"))
            {
                underground?.CompleteCurrentForTesting();

                lastAction =
                    "Подполье событие завершён";
            }

            if (Button("СБРОСИТЬ"))
            {
                underground?.ResetForTesting();

                lastAction =
                    "Подполье прогресс сброшен";
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
        }

        private void DrawPoliceRisk()
        {
            GUILayout.Label("ПОЛИЦИЯ / РИСК");

            if (cityRisk != null)
            {
                GUILayout.Label(
                    cityRisk.AdminLine);
            }

            GUILayout.BeginHorizontal();

            if (Button("+20 ВНИМАНИЯ"))
            {
                cityRisk?.AddAttentionForTesting(
                    20f);

                lastAction =
                    "Полиция: +20 внимания";
            }

            if (Button("+50 ВНИМАНИЯ"))
            {
                cityRisk?.AddAttentionForTesting(
                    50f);

                lastAction =
                    "Полиция: +50 внимания";
            }

            if (Button("СБРОСИТЬ"))
            {
                cityRisk?.ClearForTesting();

                lastAction =
                    "Внимание полиции сброшено";
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
        }

        private void DrawTime()
        {
            GUILayout.Label("ВРЕМЯ СУТОК");

            if (dayNight == null)
            {
                dayNight =
                    Object.FindAnyObjectByType<DayNightCycleController>();
            }

            GUILayout.BeginHorizontal();

            if (Button("НОЧЬ"))
                dayNight?.SetTimeOfDay(0f);

            if (Button("РАССВЕТ"))
                dayNight?.SetTimeOfDay(0.25f);

            if (Button("ДЕНЬ"))
                dayNight?.SetTimeOfDay(0.50f);

            if (Button("ЗАКАТ"))
                dayNight?.SetTimeOfDay(0.75f);

            GUILayout.EndHorizontal();

            if (dayNight != null)
            {
                GUILayout.Label(
                    $"ВРЕМЯ {dayNight.TimeOfDay01:0.00}   •   " +
                    $"НОЧЬ {(dayNight.IsNight ? "ДА" : "НЕТ")}");
            }

            GUILayout.Space(10f);
        }

        private void DrawTeleports()
        {
            GUILayout.Label("ТЕЛЕПОРТ / АКТИВНОСТИ");

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
                    Quaternion.identity);
            }

            if (Button("ДРИФТ"))
            {
                Teleport(
                    CityAssetRuntimeInstaller.DriftChallengePoint,
                    Quaternion.identity);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("ДОСТАВКА"))
                TeleportToRoute(
                    CityAssetRuntimeInstaller.DeliveryRoute);

            if (Button("СПРИНТ"))
                TeleportToRoute(
                    CityAssetRuntimeInstaller.SprintRoute);

            if (Button("КОЛЬЦО"))
                TeleportToRoute(
                    CityAssetRuntimeInstaller.CircuitRoute);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (Button("ОТМЕНИТЬ АКТИВНОСТЬ"))
                CancelActivities();

            if (Button("R — СПАСТИ МАШИНУ"))
            {
                if (car != null)
                {
                    CityAssetRuntimeInstaller.ResolveNearestRoadResetPose(
                        car.transform.position,
                        car.transform.forward,
                        out Vector3 position,
                        out Quaternion rotation);

                    Teleport(
                        position,
                        rotation);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void MaxEverything()
        {
            wallet?.SetCredits(
                1000000);

            reputation?.SetReputation(
                10000);

            disciplines?.SetAllLevelsForTesting(
                10);

            mastery?.SetAllVehicleLevelsForTesting(
                10);

            garage?.SetAllUpgradeLevelsForTesting(
                5);

            career?.SetStageForTesting(
                3);

            vehicleHistory?.SetAllLegendaryForTesting();
            collection?.RecalculateForTesting();
            legends?.UnlockCurrentForTesting();
            contracts?.SetCycleForTesting(5);
            liveEvents?.CompleteCurrentForTesting();
            underground?.AddCredForTesting(400);

            lastAction =
                "МАКСИМУМ ВСЕГО применён";
        }

        private void EliteReady()
        {
            wallet?.SetCredits(
                100000);

            reputation?.SetReputation(
                3500);

            disciplines?.SetAllLevelsForTesting(
                3);

            mastery?.SetCurrentLevelForTesting(
                5);

            garage?.SetAllUpgradeLevelsForTesting(
                3);

            lastAction =
                "ЭЛИТА/ПРЕМИУМ события разблокированы";
        }

        private void ResetTestProgression()
        {
            CancelActivities();

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

            if (roster != null)
            {
                roster.SelectVehicleForTesting(
                    0,
                    out _);
            }

            lastAction =
                "Тестовая прогрессия сброшена";
        }

        private void TeleportToRoute(
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
                    ? route[1] - route[0]
                    : Vector3.forward;

            forward.y = 0f;

            Quaternion rotation =
                forward.sqrMagnitude > 0.01f
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

            CancelActivities();

            car.TeleportTo(
                position +
                Vector3.up * 1.1f,
                rotation);

            car.SetDrivingEnabled(
                true);

            lastAction =
                $"Телепорт: {position.x:0}, {position.z:0}";
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
                    activityManager.End(
                        activityManager.ActiveId);
            }

            car?.SetDrivingEnabled(
                true);

            lastAction =
                "Активности отменены";
        }

        private static bool Button(
            string text,
            float width = 0f)
        {
            if (width > 0f)
            {
                return
                    GUILayout.Button(
                        text,
                        GUILayout.Width(
                            width),
                        GUILayout.Height(
                            30f));
            }

            return
                GUILayout.Button(
                    text,
                    GUILayout.Height(
                        30f));
        }
    }
}
