using System;
using MotorCity.CameraSystem;
using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.UI;
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
        [SerializeField] private float interactRadius = 2f;
        [SerializeField] private float maxOpenSpeedKph = 8f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private DeliveryActivity delivery;
        private DriftChallenge driftChallenge;
        private StreetSprintActivity streetSprint;
        private CircuitRaceActivity circuitRace;
        private UndergroundSceneSystem underground;
        private CityProfessionSystem professions;
        private CarWashJobSystem carWash;
        private TowTruckJobSystem towTruck;
        private VehicleRosterSystem vehicleRoster;
        private VehicleMasterySystem vehicleMastery;
        private TurboPetSystem turbo;
        private VehicleCustomizationSystem customization;
        private PlayerVehicleAudio vehicleAudio;
        private ChaseCamera chaseCamera;
        private MotorCityFrontEndFlow frontEndFlow;
        private GameObject garageLightingRig;
        private bool transitionInProgress;

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

        public event Action Changed;

        public string VehicleName =>
            vehicleRoster == null
                ? MotorCityLocalization.Text(
                    "garage.vehicles_unavailable")
                : vehicleRoster.SelectedName;

        public string VehicleLine =>
            vehicleRoster == null
                ? MotorCityLocalization.Text(
                    "garage.vehicles_unavailable")
                : vehicleRoster.GetGarageLine();

        public string NextVehicleLine =>
            vehicleRoster == null
                ? string.Empty
                : vehicleRoster.GetNextVehicleLine();

        public string VehicleStatsLine =>
            vehicleRoster == null
                ? string.Empty
                : vehicleRoster.GetStatsLine();

        public string VehicleCharacterLine =>
            vehicleRoster == null
                ? string.Empty
                : vehicleRoster.GetCharacterLine();

        public string VehicleMasteryLine =>
            vehicleRoster == null
                ? string.Empty
                : vehicleRoster.GetMasteryLine();

        public string VehicleMasteryShort =>
            vehicleRoster == null
                ? string.Empty
                : vehicleRoster.GetMasteryShort();

        public float VehicleMasteryProgress =>
            vehicleRoster == null
                ? 0f
                : vehicleRoster.MasteryProgress;

        public bool HasNextVehicle =>
            vehicleRoster != null &&
            vehicleRoster.HasNextVehicle;

        public bool NextVehicleUnlocked =>
            vehicleRoster != null &&
            vehicleRoster.NextVehicleUnlocked;

        public bool SelectedVehicleUnlocked =>
            vehicleRoster != null &&
            vehicleRoster.SelectedVehicleUnlocked;

        public bool MasteryShowMessage =>
            vehicleMastery != null &&
            vehicleMastery.ShowMessage;

        public string MasteryStatusText =>
            vehicleMastery == null
                ? string.Empty
                : vehicleMastery.StatusText;

        public string CustomizationHintLine =>
            customization == null
                ? string.Empty
                : customization.GarageHintLine;

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager manager,
            DeliveryActivity deliveryActivity,
            DriftChallenge challenge,
            StreetSprintActivity sprint,
            CircuitRaceActivity circuit,
            VehicleRosterSystem roster,
            VehicleMasterySystem mastery,
            TurboPetSystem turboSystem,
            VehicleCustomizationSystem customizationSystem)
        {
            if (wallet != null)
            {
                wallet.CreditsEarned -=
                    HandleCreditsChanged;

                wallet.CreditsSpent -=
                    HandleCreditsChanged;
            }

            if (vehicleRoster != null)
            {
                vehicleRoster.VehicleChanged -=
                    HandleGarageDataChanged;
            }

            if (customization != null)
            {
                customization.CustomizationChanged -=
                    HandleGarageDataChanged;
            }

            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            delivery = deliveryActivity;
            driftChallenge = challenge;
            streetSprint = sprint;
            circuitRace = circuit;
            vehicleRoster = roster;
            vehicleMastery = mastery;
            turbo = turboSystem;
            customization =
                customizationSystem;

            if (wallet != null)
            {
                wallet.CreditsEarned +=
                    HandleCreditsChanged;

                wallet.CreditsSpent +=
                    HandleCreditsChanged;
            }

            if (vehicleRoster != null)
            {
                vehicleRoster.VehicleChanged +=
                    HandleGarageDataChanged;
            }

            if (customization != null)
            {
                customization.CustomizationChanged +=
                    HandleGarageDataChanged;
            }

            vehicleAudio =
                car == null
                    ? null
                    : car.GetComponent<PlayerVehicleAudio>();

            garageCenter =
                MotorCity.World.CityAssetRuntimeInstaller.GaragePoint;

            LoadUpgradeLevels();

            IsOpen = false;
            car?.SetDrivingBlocked(
                "Garage",
                false);
            ApplyUpgrades();
        }

        public void ConfigureCancelableActivities(
            UndergroundSceneSystem undergroundSystem,
            CityProfessionSystem professionSystem,
            CarWashJobSystem carWashSystem,
            TowTruckJobSystem towTruckSystem)
        {
            underground =
                undergroundSystem;

            professions =
                professionSystem;

            carWash =
                carWashSystem;

            towTruck =
                towTruckSystem;
        }

        private void Update()
        {
            if (car == null ||
                wallet == null ||
                activityManager == null)
            {
                return;
            }

            if (transitionInProgress)
                return;

            if (!IsOpen)
            {
                float distance =
                    Vector3.Distance(
                        Flat(
                            car.transform.position),
                        Flat(
                            garageCenter));

                IsNearGarage =
                    distance <=
                    interactRadius;
                if (IsNearGarage &&
                    MotorCityInput.InteractPressed)
                {
                    if (car.SpeedKph <=
                        maxOpenSpeedKph)
                    {
                        OpenGarage();

                        // Do not let the same virtual Interact press fall
                        // through into the open-garage close handler below.
                        // Touch input is shared by InteractPressed and
                        // WasVirtualPressed during this update.
                        return;
                    }
                    else
                    {
                        StatusText =
                            MotorCityLocalization.Text(
                                "garage.stop_first");
                    }
                }

                if (!IsOpen)
                {
                    StatusText =
                        IsNearGarage
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
            }

            // Once the garage is open, all garage actions are UI-only.
            // Keyboard shortcuts remain available elsewhere in the game,
            // but are intentionally ignored by the garage.
            if (MotorCityInput.WasVirtualPressed(
                    MotorCityInputAction.Interact))
            {
                CloseGarage();
                return;
            }

            if (MotorCityInput.WasVirtualPressed(
                    MotorCityInputAction.Upgrade1))
            {
                TryBuy(
                    UpgradeType.Engine);
            }

            if (MotorCityInput.WasVirtualPressed(
                    MotorCityInputAction.Upgrade2))
            {
                TryBuy(
                    UpgradeType.Grip);
            }

            if (MotorCityInput.WasVirtualPressed(
                    MotorCityInputAction.Upgrade3))
            {
                TryBuy(
                    UpgradeType.Stability);
            }

            if (MotorCityInput.WasVirtualPressed(
                    MotorCityInputAction.PreviousVehicle))
            {
                TrySelectVehicle(
                    -1);
            }

            if (MotorCityInput.WasVirtualPressed(
                    MotorCityInputAction.NextVehicle))
            {
                TrySelectVehicle(
                    1);
            }

            if (MotorCityInput.WasVirtualPressed(
                    MotorCityInputAction.CycleBodyColor) &&
                customization != null)
            {
                customization.CycleBodyColor();

                StatusText =
                    string.Empty;
            }

            if (MotorCityInput.WasVirtualPressed(
                    MotorCityInputAction.CycleWheels) &&
                customization != null)
            {
                customization.CycleWheelStyle();

                StatusText =
                    string.Empty;
            }

            if (MotorCityInput.WasVirtualPressed(
                    MotorCityInputAction.CycleNeon) &&
                customization != null)
            {
                customization.CycleNeon();

                StatusText =
                    string.Empty;
            }
        }

        public string GetUpgradeTitle(
            int index)
        {
            UpgradeType type =
                (UpgradeType)Mathf.Clamp(
                    index,
                    0,
                    2);

            int level =
                GetLevel(
                    type);

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

        public string GetUpgradeName(
            int index)
        {
            UpgradeType type =
                (UpgradeType)Mathf.Clamp(
                    index,
                    0,
                    2);

            return
                Name(
                    type);
        }

        public string GetUpgradeLevelText(
            int index)
        {
            UpgradeType type =
                (UpgradeType)Mathf.Clamp(
                    index,
                    0,
                    2);

            int level =
                GetLevel(
                    type);

            return
                level >= MaxLevel
                    ? MotorCityLocalization.Text(
                        "garage.max_short")
                    : MotorCityLocalization.Format(
                        "garage.level_line",
                        level,
                        MaxLevel);
        }

        public string GetUpgradePrice(
            int index)
        {
            UpgradeType type =
                (UpgradeType)Mathf.Clamp(
                    index,
                    0,
                    2);

            int level =
                GetLevel(
                    type);

            return
                level >= MaxLevel
                    ? MotorCityLocalization.Text(
                        "garage.bought")
                    : MotorCityLocalization.Format(
                        "garage.price",
                        Price(
                            type,
                            level));
        }

        public int GetUpgradeLevel(
            int index)
        {
            UpgradeType type =
                (UpgradeType)Mathf.Clamp(
                    index,
                    0,
                    2);

            return
                GetLevel(
                    type);
        }

        public int GetUpgradeCost(
            int index)
        {
            UpgradeType type =
                (UpgradeType)Mathf.Clamp(
                    index,
                    0,
                    2);

            int level =
                GetLevel(
                    type);

            return
                level >= MaxLevel
                    ? 0
                    : Price(
                        type,
                        level);
        }

        public bool IsUpgradeMaxed(
            int index)
        {
            UpgradeType type =
                (UpgradeType)Mathf.Clamp(
                    index,
                    0,
                    2);

            return
                GetLevel(
                    type) >=
                MaxLevel;
        }

        public bool CanAffordUpgrade(
            int index)
        {
            if (wallet == null)
                return false;

            int cost =
                GetUpgradeCost(
                    index);

            return
                cost > 0 &&
                wallet.Credits >=
                    cost;
        }

        public string GetUpgradeDescription(
            int index)
        {
            UpgradeType type =
                (UpgradeType)Mathf.Clamp(
                    index,
                    0,
                    2);

            int level =
                GetLevel(
                    type);

            if (level >= MaxLevel)
            {
                return type switch
                {
                    UpgradeType.Engine =>
                        MotorCityLocalization.Format(
                            "garage.engine_desc_max",
                            EngineSpeedBonus(
                                level),
                            EngineAccelerationBonus(
                                level),
                            EngineAssistPercent(
                                level)),

                    UpgradeType.Grip =>
                        MotorCityLocalization.Format(
                            "garage.grip_desc_max",
                            GripBonusPercent(
                                level)),

                    _ =>
                        MotorCityLocalization.Format(
                            "garage.stability_desc_max",
                            StabilityCenterDropMm(
                                level),
                            StabilityDampingPercent(
                                level))
                };
            }

            int nextLevel =
                Mathf.Min(
                    MaxLevel,
                    level + 1);

            return type switch
            {
                UpgradeType.Engine =>
                    MotorCityLocalization.Format(
                        "garage.engine_desc_next",
                        EngineSpeedBonus(
                            level),
                        EngineSpeedBonus(
                            nextLevel),
                        EngineAccelerationBonus(
                            level),
                        EngineAccelerationBonus(
                            nextLevel),
                        EngineAssistPercent(
                            level),
                        EngineAssistPercent(
                            nextLevel)),

                UpgradeType.Grip =>
                    MotorCityLocalization.Format(
                        "garage.grip_desc_next",
                        GripBonusPercent(
                            level),
                        GripBonusPercent(
                            nextLevel)),

                _ =>
                    MotorCityLocalization.Format(
                        "garage.stability_desc_next",
                        StabilityCenterDropMm(
                            level),
                        StabilityCenterDropMm(
                            nextLevel),
                        StabilityDampingPercent(
                            level),
                        StabilityDampingPercent(
                            nextLevel))
            };
        }

        private void SetUpgradeLevelsForTesting(
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

            activityManager.RequestStart(
                ActivityId,
                MotorCityLocalization.Text(
                    "garage.activity_name"),
                BeginPreparedGarageOpen);
        }

        private void BeginPreparedGarageOpen()
        {
            transitionInProgress =
                true;

            car.SetGaragePresentationMode(
                true);

            MotorCityFrontEndFlow flow =
                ResolveFrontEndFlow();

            if (flow != null &&
                flow.PlayRuntimeLoadingTransition(
                    CompleteGarageOpenTeleport,
                    "ЗАГРУЖАЕМ ГАРАЖ...",
                    "LOADING GARAGE...",
                    FinishGarageTransition))
            {
                return;
            }

            CompleteGarageOpenTeleport();
            FinishGarageTransition();
        }

        private void CompleteGarageOpenTeleport()
        {
            IsOpen =
                true;

            IsNearGarage =
                true;

            car.TeleportTo(
                MotorCity.World.CityAssetRuntimeInstaller.GarageVehiclePosition,
                MotorCity.World.CityAssetRuntimeInstaller.GarageVehicleRotation);

            car.SetGaragePresentationMode(
                true);

            SetGaragePresentationSystems(
                true);

            StatusText =
                string.Empty;

            Changed?.Invoke();
        }

        private void FinishGarageTransition()
        {
            transitionInProgress =
                false;
        }

        private void CancelActiveMission()
        {
            delivery?.CancelActivity();
            driftChallenge?.CancelActivity();
            streetSprint?.CancelActivity();
            circuitRace?.CancelActivity();
            underground?.CancelActivity();
            professions?.CancelActive();
            carWash?.CancelWash();
            towTruck?.CancelJob();
        }

        public void CloseAfterRookieCustomization()
        {
            if (!IsOpen)
                return;

            CloseGarage();
        }

        private void CloseGarage()
        {
            if (transitionInProgress)
                return;

            transitionInProgress =
                true;

            MotorCityFrontEndFlow flow =
                ResolveFrontEndFlow();

            if (flow != null &&
                flow.PlayRuntimeLoadingTransition(
                    CompleteGarageCloseTeleport,
                    "ВОЗВРАЩАЕМСЯ В ГОРОД...",
                    "RETURNING TO THE CITY...",
                    FinishGarageTransition))
            {
                return;
            }

            CompleteGarageCloseTeleport();
            FinishGarageTransition();
        }

        private void CompleteGarageCloseTeleport()
        {
            IsOpen =
                false;

            SetGaragePresentationSystems(
                false);

            car.TeleportTo(
                MotorCity.World.CityAssetRuntimeInstaller.GarageExitPosition,
                MotorCity.World.CityAssetRuntimeInstaller.GarageExitRotation);

            car.SetGaragePresentationMode(
                false);

            IsNearGarage =
                true;

            activityManager.End(
                ActivityId);

            StatusText =
                MotorCityLocalization.Text(
                    "garage.open_prompt");

            Changed?.Invoke();
        }

        private MotorCityFrontEndFlow ResolveFrontEndFlow()
        {
            if (frontEndFlow != null)
                return frontEndFlow;

            frontEndFlow =
                UnityEngine.Object.FindAnyObjectByType<MotorCityFrontEndFlow>(
                    FindObjectsInactive.Include);

            return frontEndFlow;
        }

        private void SetGaragePresentationSystems(
            bool active)
        {
            if (vehicleAudio == null &&
                car != null)
            {
                vehicleAudio =
                    car.GetComponent<PlayerVehicleAudio>();
            }

            vehicleAudio?.SetMuted(
                active);

            SetGarageLighting(
                active);

            if (chaseCamera == null)
            {
                chaseCamera =
                    UnityEngine.Object.FindAnyObjectByType<ChaseCamera>();
            }

            if (chaseCamera == null)
                return;

            if (active)
            {
                chaseCamera.SetGarageMode(
                    true,
                    MotorCity.World.CityAssetRuntimeInstaller.GarageCameraPosition,
                    MotorCity.World.CityAssetRuntimeInstaller.GarageCameraRotation);
            }
            else
            {
                chaseCamera.SetGarageMode(
                    false,
                    Vector3.zero,
                    Quaternion.identity);

                chaseCamera.SetManualInputEnabled(
                    true);
            }
        }

        private void SetGarageLighting(
            bool active)
        {
            if (garageLightingRig == null)
            {
                if (!active)
                    return;

                BuildGarageLighting();
            }

            if (garageLightingRig != null)
            {
                garageLightingRig.SetActive(
                    active);
            }
        }

        private void BuildGarageLighting()
        {
            garageLightingRig =
                new GameObject(
                    "MotorCity Garage Lighting");

            garageLightingRig.transform.SetParent(
                transform,
                false);

            CreateGaragePointLight(
                "Garage Warm Light A",
                new Vector3(
                    -421.89328f,
                    4.64230585f,
                    333.540009f));

            CreateGaragePointLight(
                "Garage Warm Light B",
                new Vector3(
                    -413.215881f,
                    4.5968709f,
                    333.540009f));

            garageLightingRig.SetActive(
                false);
        }

        private void CreateGaragePointLight(
            string lightName,
            Vector3 worldPosition)
        {
            GameObject lightObject =
                new GameObject(
                    lightName);

            lightObject.transform.SetParent(
                garageLightingRig.transform,
                false);

            lightObject.transform.position =
                worldPosition;

            Light light =
                lightObject.AddComponent<Light>();

            light.type =
                LightType.Point;

            light.lightmapBakeType =
                LightmapBakeType.Realtime;

            light.color =
                new Color(
                    1f,
                    0.94f,
                    0.85f);

            light.range =
                12f;

            light.intensity =
                4f;

            light.cullingMask =
                ~0;

            light.renderMode =
                LightRenderMode.ForcePixel;

            light.shadows =
                LightShadows.Soft;

            light.shadowStrength =
                0.65f;

            light.shadowBias =
                0.03f;

            light.shadowNormalBias =
                0.2f;
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

            LoadUpgradeLevels();

            if (!string.IsNullOrWhiteSpace(
                    status))
            {
                StatusText =
                    status;
            }

            ApplyUpgrades();
            Changed?.Invoke();
        }

        private void TryBuy(UpgradeType type)
        {
            int level = GetLevel(type);
            if (level >= MaxLevel)
            {
                StatusText =
                    string.Empty;
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
                string.Empty;
            Changed?.Invoke();
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

        private string UpgradeKey(
            string baseKey)
        {
            string vehicleId =
                vehicleRoster == null
                    ? "beatall"
                    : vehicleRoster.SelectedId;

            return
                $"{baseKey}.{vehicleId}";
        }

        private void LoadUpgradeLevels()
        {
            string engineKey =
                UpgradeKey(
                    EngineKey);

            string gripKey =
                UpgradeKey(
                    GripKey);

            string stabilityKey =
                UpgradeKey(
                    StabilityKey);

            // Preserve existing players' upgrades on the car that is selected
            // the first time the per-vehicle save format is encountered.
            if (!MotorCity.Persistence.MotorCitySaveService.HasKey(
                    engineKey) &&
                MotorCity.Persistence.MotorCitySaveService.HasKey(
                    EngineKey))
            {
                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    engineKey,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        EngineKey,
                        0));

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    gripKey,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        GripKey,
                        0));

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    stabilityKey,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        StabilityKey,
                        0));

                // Legacy upgrade keys represented one global upgrade set.
                // Remove them immediately after migrating to the currently
                // selected car so they cannot be copied into every vehicle.
                MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                    EngineKey);
                MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                    GripKey);
                MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                    StabilityKey);

                MotorCity.Persistence.MotorCitySaveService.Save();
            }

            EngineLevel =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        engineKey,
                        0),
                    0,
                    MaxLevel);

            GripLevel =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        gripKey,
                        0),
                    0,
                    MaxLevel);

            StabilityLevel =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        stabilityKey,
                        0),
                    0,
                    MaxLevel);
        }

        private void HandleCreditsChanged(
            int amount)
        {
            Changed?.Invoke();
        }

        private void HandleGarageDataChanged()
        {
            Changed?.Invoke();
        }

        private void Save()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                UpgradeKey(
                    EngineKey),
                EngineLevel);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                UpgradeKey(
                    GripKey),
                GripLevel);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                UpgradeKey(
                    StabilityKey),
                StabilityLevel);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private void OnDisable()
        {
            RestoreDriving();
        }

        private void OnDestroy()
        {
            if (wallet != null)
            {
                wallet.CreditsEarned -=
                    HandleCreditsChanged;

                wallet.CreditsSpent -=
                    HandleCreditsChanged;
            }

            if (vehicleRoster != null)
            {
                vehicleRoster.VehicleChanged -=
                    HandleGarageDataChanged;
            }

            if (customization != null)
            {
                customization.CustomizationChanged -=
                    HandleGarageDataChanged;
            }

            if (garageLightingRig != null)
            {
                Destroy(
                    garageLightingRig);

                garageLightingRig =
                    null;
            }

            RestoreDriving();
        }

        private void RestoreDriving()
        {
            if (IsOpen ||
                transitionInProgress)
            {
                SetGaragePresentationSystems(
                    false);
            }

            car?.SetGaragePresentationMode(
                false);

            if (activityManager != null)
                activityManager.End(ActivityId);

            IsOpen = false;
            transitionInProgress = false;
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
