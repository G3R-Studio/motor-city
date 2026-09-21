using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using UnityEngine;

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
        public string StatusText { get; private set; } =
            MotorCityLocalization.Text(
                "garage.marker_text");

        public string VehicleLine =>
            vehicleRoster == null
                ? MotorCityLocalization.Text(
                    "garage.vehicles_unavailable")
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

            EngineLevel = Mathf.Clamp(MotorCity.Persistence.MotorCitySaveService.GetInt(EngineKey, 0), 0, MaxLevel);
            GripLevel = Mathf.Clamp(MotorCity.Persistence.MotorCitySaveService.GetInt(GripKey, 0), 0, MaxLevel);
            StabilityLevel = Mathf.Clamp(MotorCity.Persistence.MotorCitySaveService.GetInt(StabilityKey, 0), 0, MaxLevel);

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

            if (IsNearGarage && MotorCityInput.InteractPressed)
            {
                if (IsOpen)
                    CloseGarage();
                else if (car.SpeedKph <= maxOpenSpeedKph)
                    OpenGarage();
                else
                    StatusText = MotorCityLocalization.Text("garage.stop_first");
            }

            if (!IsOpen)
            {
                StatusText = IsNearGarage
                    ? (activityManager.IsBusy
                        ? MotorCityLocalization.Format(
                            "garage.open_cancel",
                            activityManager.ActiveName)
                        : MotorCityLocalization.Text(
                            "garage.stop_and_open"))
                    : MotorCityLocalization.Text(
                        "garage.marker_text");
                return;
            }

            if (MotorCityInput.Upgrade1Pressed)
                TryBuy(UpgradeType.Engine);
            if (MotorCityInput.Upgrade2Pressed)
                TryBuy(UpgradeType.Grip);
            if (MotorCityInput.Upgrade3Pressed)
                TryBuy(UpgradeType.Stability);

            if (MotorCityInput.PreviousVehiclePressed)
                TrySelectVehicle(-1);

            if (MotorCityInput.NextVehiclePressed)
                TrySelectVehicle(1);

            if (MotorCityInput.CancelPressed)
                CloseGarage();
        }

        public string GetUpgradeTitle(int index)
        {
            UpgradeType type = (UpgradeType)Mathf.Clamp(index, 0, 2);
            int level = GetLevel(type);
            string levelText =
                level >= MaxLevel
                    ? MotorCityLocalization.Text(
                        "garage.max_short")
                    : MotorCityLocalization.Format(
                        "garage.level_line",
                        level,
                        MaxLevel);

            return
                MotorCityLocalization.Format(
                    "garage.title",
                    index + 1,
                    Name(type),
                    levelText);
        }

        public string GetUpgradePrice(int index)
        {
            UpgradeType type = (UpgradeType)Mathf.Clamp(index, 0, 2);
            int level = GetLevel(type);
            return level >= MaxLevel
                ? MotorCityLocalization.Text(
                    "garage.bought")
                : MotorCityLocalization.Format(
                    "garage.price",
                    Price(type, level));
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
                    MotorCityLocalization.Format(
                        "garage.engine_desc",
                        EngineSpeedBonus(level),
                        EngineAccelerationBonus(level),
                        EngineAssistPercent(level)),

                UpgradeType.Grip =>
                    MotorCityLocalization.Format(
                        "garage.grip_desc",
                        GripBonusPercent(level)),

                _ =>
                    MotorCityLocalization.Format(
                        "garage.stability_desc",
                        StabilityCenterDropMm(level),
                        StabilityDampingPercent(level))
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

            if (!activityManager.TryBegin(
                    ActivityId,
                    MotorCityLocalization.Text(
                        "garage.activity_name")))
                return;

            IsOpen = true;
            car.SetDrivingEnabled(false);
            StatusText = MotorCityLocalization.Text("garage.opened");
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
                ? MotorCityLocalization.Text(
                    "garage.open_prompt")
                : MotorCityLocalization.Text(
                    "garage.marker_text");
        }

        private void TrySelectVehicle(
            int offset)
        {
            if (vehicleRoster == null)
            {
                StatusText =
                    MotorCityLocalization.Text("garage.fleet_unavailable");

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
                StatusText =
                    MotorCityLocalization.Format(
                        "garage.upgrade_max",
                        Name(type));
                return;
            }

            int price = Price(type, level);
            if (!wallet.TrySpendCredits(price))
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "garage.need_credits2",
                        Name(type),
                        price);
                return;
            }

            level++;
            SetLevel(type, level);
            Save();
            ApplyUpgrades();
            StatusText =
                MotorCityLocalization.Format(
                    "garage.upgraded2",
                    Name(type),
                    level);
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
                UpgradeType.Engine =>
                    MotorCityLocalization.Text(
                        "garage.engine_name"),
                UpgradeType.Grip =>
                    MotorCityLocalization.Text(
                        "garage.grip_name"),
                _ =>
                    MotorCityLocalization.Text(
                        "garage.stability_name")
            };
        }

        private void Save()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(EngineKey, EngineLevel);
            MotorCity.Persistence.MotorCitySaveService.SetInt(GripKey, GripLevel);
            MotorCity.Persistence.MotorCitySaveService.SetInt(StabilityKey, StabilityLevel);
            MotorCity.Persistence.MotorCitySaveService.Save();
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
