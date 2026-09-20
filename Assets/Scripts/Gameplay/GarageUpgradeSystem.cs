using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class GarageUpgradeSystem : MonoBehaviour
    {
        private const string EngineKey = "MotorCity.Upgrade.Engine";
        private const string GripKey = "MotorCity.Upgrade.Grip";
        private const string StabilityKey = "MotorCity.Upgrade.Stability";
        private const string ActivityId = "garage";
        private const int MaxLevel = 5;

        private Vector3 garageCenter;
        [SerializeField] private float interactRadius = 13f;
        [SerializeField] private float maxOpenSpeedKph = 8f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private DeliveryActivity delivery;
        private DriftChallenge driftChallenge;
        private StreetSprintActivity streetSprint;
        private CircuitRaceActivity circuitRace;
        private VehicleRosterSystem vehicleRoster;
        private VehicleMasterySystem vehicleMastery;

        public int EngineLevel { get; private set; }
        public int GripLevel { get; private set; }
        public int StabilityLevel { get; private set; }
        public bool IsOpen { get; private set; }
        public Vector3 GarageCenter => garageCenter;
        public bool IsNearGarage { get; private set; }
        public int Credits => wallet == null ? 0 : wallet.Credits;
        public string StatusText { get; private set; } = "Фиолетовый маркер: гараж";

        public string VehicleLine =>
            vehicleRoster == null
                ? "МАШИНЫ НЕДОСТУПНЫ"
                : vehicleRoster.GetGarageLine();

        public string VehicleStatsLine =>
            vehicleRoster == null
                ? string.Empty
                : vehicleRoster.GetStatsLine();

        public string VehicleMasteryLine =>
            vehicleRoster == null
                ? string.Empty
                : vehicleRoster.GetMasteryLine();

        public string VehicleMasteryShort =>
            vehicleRoster == null
                ? string.Empty
                : vehicleRoster.GetMasteryShort();

        public bool MasteryShowMessage =>
            vehicleMastery != null &&
            vehicleMastery.ShowMessage;

        public string MasteryStatusText =>
            vehicleMastery == null
                ? string.Empty
                : vehicleMastery.StatusText;

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager manager,
            DeliveryActivity deliveryActivity,
            DriftChallenge challenge,
            StreetSprintActivity sprint,
            CircuitRaceActivity circuit,
            VehicleRosterSystem roster,
            VehicleMasterySystem mastery)
        {
            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            delivery = deliveryActivity;
            driftChallenge = challenge;
            streetSprint = sprint;
            circuitRace = circuit;
            vehicleRoster = roster;
            vehicleMastery = mastery;
            garageCenter =
                MotorCity.World.CityAssetRuntimeInstaller.GaragePoint;

            EngineLevel = Mathf.Clamp(PlayerPrefs.GetInt(EngineKey, 0), 0, MaxLevel);
            GripLevel = Mathf.Clamp(PlayerPrefs.GetInt(GripKey, 0), 0, MaxLevel);
            StabilityLevel = Mathf.Clamp(PlayerPrefs.GetInt(StabilityKey, 0), 0, MaxLevel);

            IsOpen = false;
            car?.SetDrivingEnabled(true);
            ApplyUpgrades();
        }

        private void Update()
        {
            if (car == null || wallet == null || activityManager == null) return;

            float distance = Vector3.Distance(
                Flat(car.transform.position),
                Flat(garageCenter));

            IsNearGarage = distance <= interactRadius;

            if (IsOpen && (!IsNearGarage || car.SpeedKph > maxOpenSpeedKph))
                CloseGarage();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (IsNearGarage && keyboard.eKey.wasPressedThisFrame)
            {
                if (IsOpen)
                    CloseGarage();
                else if (car.SpeedKph <= maxOpenSpeedKph)
                    OpenGarage();
                else
                    StatusText = "Гараж: сначала полностью останови машину";
            }

            if (!IsOpen)
            {
                StatusText = IsNearGarage
                    ? (activityManager.IsBusy
                        ? $"E — открыть гараж и отменить «{activityManager.ActiveName}»"
                        : "ГАРАЖ — остановись и нажми E")
                    : "Фиолетовый маркер: гараж";
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                TryBuy(UpgradeType.Engine);
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                TryBuy(UpgradeType.Grip);
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                TryBuy(UpgradeType.Stability);

            if (keyboard.zKey.wasPressedThisFrame)
                TrySelectVehicle(-1);

            if (keyboard.xKey.wasPressedThisFrame)
                TrySelectVehicle(1);

            if (keyboard.escapeKey.wasPressedThisFrame)
                CloseGarage();
        }

        public string GetUpgradeTitle(int index)
        {
            UpgradeType type = (UpgradeType)Mathf.Clamp(index, 0, 2);
            int level = GetLevel(type);
            string levelText = level >= MaxLevel ? "МАКС" : $"УР. {level}/{MaxLevel}";
            return $"[{index + 1}] {Name(type)}   {levelText}";
        }

        public string GetUpgradePrice(int index)
        {
            UpgradeType type = (UpgradeType)Mathf.Clamp(index, 0, 2);
            int level = GetLevel(type);
            return level >= MaxLevel
                ? "КУПЛЕНО"
                : $"{Price(type, level):N0} КР";
        }

        public string GetUpgradeDescription(int index)
        {
            UpgradeType type =
                (UpgradeType)Mathf.Clamp(
                    index,
                    0,
                    2);

            int level =
                GetLevel(
                    type);

            return type switch
            {
                UpgradeType.Engine =>
                    $"ТЮНИНГ МОТОРА: +{EngineSpeedBonus(level)} км/ч, " +
                    $"+{EngineAccelerationBonus(level)} разгон, " +
                    $"+{EngineAssistPercent(level)}% тяга",

                UpgradeType.Grip =>
                    $"ШИНЫ / СЦЕП: +{GripBonusPercent(level)}% сцепления, " +
                    "меньше пробуксовка и стабильнее быстрые повороты",

                _ =>
                    $"ШАССИ: -{StabilityCenterDropMm(level)} мм центр массы, " +
                    $"+{StabilityDampingPercent(level)}% угловое демпфирование"
            };
        }

        public void SetUpgradeLevelsForTesting(
            int engine,
            int grip,
            int stability)
        {
            EngineLevel =
                Mathf.Clamp(
                    engine,
                    0,
                    MaxLevel);

            GripLevel =
                Mathf.Clamp(
                    grip,
                    0,
                    MaxLevel);

            StabilityLevel =
                Mathf.Clamp(
                    stability,
                    0,
                    MaxLevel);

            Save();
            ApplyUpgrades();
        }

        public void SetAllUpgradeLevelsForTesting(
            int level)
        {
            SetUpgradeLevelsForTesting(
                level,
                level,
                level);
        }

        private void OpenGarage()
        {
            CancelActiveMission();

            if (!activityManager.TryBegin(ActivityId, "Гараж"))
                return;

            IsOpen = true;
            car.SetDrivingEnabled(false);
            StatusText = "ГАРАЖ ОТКРЫТ";
        }

        private void CancelActiveMission()
        {
            delivery?.CancelActivity();
            driftChallenge?.CancelActivity();
            streetSprint?.CancelActivity();
            circuitRace?.CancelActivity();
        }

        private void CloseGarage()
        {
            IsOpen = false;
            car.SetDrivingEnabled(true);
            activityManager.End(ActivityId);
            StatusText = IsNearGarage
                ? "ГАРАЖ — нажми E"
                : "Фиолетовый маркер: гараж";
        }

        private void TrySelectVehicle(
            int offset)
        {
            if (vehicleRoster == null)
            {
                StatusText =
                    "Автопарк ещё не подготовлен";

                return;
            }

            vehicleRoster.TrySelectOffset(
                offset,
                out string status);

            if (!string.IsNullOrWhiteSpace(
                    status))
            {
                StatusText =
                    status;
            }

            ApplyUpgrades();
        }

        private void TryBuy(UpgradeType type)
        {
            int level = GetLevel(type);
            if (level >= MaxLevel)
            {
                StatusText = $"{Name(type)} уже улучшен до максимума";
                return;
            }

            int price = Price(type, level);
            if (!wallet.TrySpendCredits(price))
            {
                StatusText = $"Для «{Name(type)}» нужно {price:N0} КР";
                return;
            }

            level++;
            SetLevel(type, level);
            Save();
            ApplyUpgrades();
            StatusText = $"{Name(type)} улучшен до уровня {level}";
        }

        private void ApplyUpgrades()
        {
            if (car != null)
                car.ApplyUpgradeLevels(
                    EngineLevel,
                    GripLevel,
                    StabilityLevel);
        }

        private int GetLevel(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Engine => EngineLevel,
                UpgradeType.Grip => GripLevel,
                _ => StabilityLevel
            };
        }

        private void SetLevel(UpgradeType type, int level)
        {
            switch (type)
            {
                case UpgradeType.Engine:
                    EngineLevel = level;
                    break;
                case UpgradeType.Grip:
                    GripLevel = level;
                    break;
                case UpgradeType.Stability:
                    StabilityLevel = level;
                    break;
            }
        }

        private static int Price(
            UpgradeType type,
            int currentLevel)
        {
            int[] multipliers =
            {
                1,
                2,
                4,
                7,
                11
            };

            int levelIndex =
                Mathf.Clamp(
                    currentLevel,
                    0,
                    multipliers.Length - 1);

            int basePrice =
                type switch
                {
                    UpgradeType.Engine => 650,
                    UpgradeType.Grip => 600,
                    _ => 550
                };

            return
                basePrice *
                multipliers[levelIndex];
        }

        private static int EngineSpeedBonus(
            int level)
        {
            int[] values =
            {
                0,
                10,
                22,
                36,
                52,
                70
            };

            return
                values[Mathf.Clamp(
                    level,
                    0,
                    values.Length - 1)];
        }

        private static int EngineAccelerationBonus(
            int level)
        {
            int[] values =
            {
                0,
                1,
                2,
                4,
                6,
                8
            };

            return
                values[Mathf.Clamp(
                    level,
                    0,
                    values.Length - 1)];
        }

        private static int EngineAssistPercent(
            int level)
        {
            int[] values =
            {
                0,
                12,
                25,
                40,
                58,
                78
            };

            return
                values[Mathf.Clamp(
                    level,
                    0,
                    values.Length - 1)];
        }

        private static int GripBonusPercent(
            int level)
        {
            int[] values =
            {
                0,
                5,
                11,
                18,
                26,
                35
            };

            return
                values[Mathf.Clamp(
                    level,
                    0,
                    values.Length - 1)];
        }

        private static int StabilityCenterDropMm(
            int level)
        {
            int[] values =
            {
                0,
                18,
                38,
                62,
                90,
                122
            };

            return
                values[Mathf.Clamp(
                    level,
                    0,
                    values.Length - 1)];
        }

        private static int StabilityDampingPercent(
            int level)
        {
            int[] values =
            {
                0,
                12,
                26,
                43,
                63,
                86
            };

            return
                values[Mathf.Clamp(
                    level,
                    0,
                    values.Length - 1)];
        }

        private static string Name(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Engine => "Двигатель",
                UpgradeType.Grip => "Сцепление",
                _ => "Стабильность"
            };
        }

        private void Save()
        {
            PlayerPrefs.SetInt(EngineKey, EngineLevel);
            PlayerPrefs.SetInt(GripKey, GripLevel);
            PlayerPrefs.SetInt(StabilityKey, StabilityLevel);
            PlayerPrefs.Save();
        }

        private void OnDisable()
        {
            RestoreDriving();
        }

        private void OnDestroy()
        {
            RestoreDriving();
        }

        private void RestoreDriving()
        {
            if (car != null)
                car.SetDrivingEnabled(true);

            if (activityManager != null)
                activityManager.End(ActivityId);

            IsOpen = false;
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private enum UpgradeType
        {
            Engine = 0,
            Grip = 1,
            Stability = 2
        }
    }
}
