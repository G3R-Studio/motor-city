using MotorCity.Gameplay;
using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed class PrototypeHud : MonoBehaviour
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
        private StuntJumpSystem stuntJumps;
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

        private Font font;
        private Sprite panelSprite;

        private Text moneyText;
        private Text reputationText;
        private Text upgradesText;
        private Text driveModeText;
        private Text hintText;
        private Text speedText;
        private Text speedUnitText;
        private Text statusText;
        private Text driftText;
        private Text navigatorArrowText;
        private Text navigatorText;
        private Text careerText;
        private Text disciplineText;
        private Text contractText;
        private Text liveEventText;
        private Text collectionText;
        private Text legendText;
        private Text objectiveText;
        private RawImage minimapImage;
        private RectTransform minimapTargetBlip;
        private Text minimapTargetText;
        private Camera minimapCamera;
        private RenderTexture minimapTexture;

        private GameObject navigatorPanel;
        private GameObject statusPanel;
        private GameObject driftPanel;
        private GameObject garageOverlay;
        private GameObject activityResultOverlay;

        private Text resultTitleText;
        private Text resultHeadlineText;
        private Text resultDetailsText;
        private Text resultRewardText;
        private Text resultControlsText;

        private Text garageMoneyText;
        private Text garageStatusText;
        private Text garageVehicleText;
        private Text garageVehicleStatsText;
        private Text garageVehicleHistoryText;
        private Text garageVehicleSpecializationText;
        private Text garageCollectionText;
        private readonly Text[] garageTitleTexts =
            new Text[3];
        private readonly Text[] garagePriceTexts =
            new Text[3];
        private readonly Text[] garageDescriptionTexts =
            new Text[3];

        private static readonly Color PanelColor =
            new(0.025f, 0.032f, 0.045f, 0.90f);
        private static readonly Color PanelSoftColor =
            new(0.035f, 0.045f, 0.06f, 0.82f);
        private static readonly Color TextColor =
            new(0.95f, 0.97f, 1f, 1f);
        private static readonly Color SecondaryTextColor =
            new(0.66f, 0.72f, 0.8f, 1f);
        private static readonly Color BlueAccent =
            new(0.12f, 0.58f, 1f, 1f);
        private static readonly Color DriftAccent =
            new(1f, 0.55f, 0.12f, 1f);
        private static readonly Color GarageAccent =
            new(0.66f, 0.3f, 1f, 1f);

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
            StuntJumpSystem stuntJumpSystem,
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
            CityRiskSystem riskSystem)
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
            stuntJumps = stuntJumpSystem;
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

            BuildUi();
        }

        private void Update()
        {
            if (moneyText == null)
                return;

            int credits =
                wallet == null
                    ? 0
                    : wallet.Credits;

            moneyText.text =
                MotorCityLocalization.Format(
                    "common.credits",
                    credits);

            if (reputationText != null &&
                activityManager != null)
            {
                reputationText.text =
                    MotorCityLocalization.Format(
                        "hud.rep",
                        activityManager.TotalReputation,
                        activityManager.ReputationLevel);
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

            if (driveModeText != null &&
                car != null)
            {
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

            float speed =
                car == null
                    ? 0f
                    : car.SpeedKph;

            speedText.text =
                Mathf.RoundToInt(speed)
                    .ToString("000");

            bool resultOpen =
                activityManager != null &&
                activityManager.HasResult;

            activityResultOverlay.SetActive(
                resultOpen);

            if (resultOpen)
            {
                statusPanel.SetActive(false);
                driftPanel.SetActive(false);
                garageOverlay.SetActive(false);
                navigatorPanel.SetActive(false);
                UpdateActivityResult();
                HandleActivityResultInput();
                return;
            }

            string status =
                ResolveStatus();

            statusPanel.SetActive(
                !string.IsNullOrWhiteSpace(status));

            if (statusPanel.activeSelf)
                statusText.text = status;

            bool showDrift =
                drift != null &&
                (drift.IsDrifting ||
                 drift.CurrentScore > 0 ||
                 drift.ShowRewardMessage);

            driftPanel.SetActive(showDrift);

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

            garageOverlay.SetActive(garageOpen);

            if (garageOpen)
                UpdateGarage();

            UpdateNavigator(
                garageOpen);
        }

        private void BuildUi()
        {
            font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            panelSprite =
                Resources.Load<Sprite>(
                    "MotorCity/UI/grey_panel");

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

            CanvasScaler scaler =
                canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1600f, 900f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            BuildPlayerCard(canvasObject.transform);
            BuildSpeedometer(canvasObject.transform);
            BuildStatus(canvasObject.transform);
            BuildNavigator(canvasObject.transform);
            BuildDriftPanel(canvasObject.transform);
            BuildActivityResult(canvasObject.transform);
            BuildGarage(canvasObject.transform);

            driftPanel.SetActive(false);
            activityResultOverlay.SetActive(false);
            garageOverlay.SetActive(false);
        }

        private void BuildPlayerCard(Transform canvas)
        {
            RectTransform card =
                CreatePanel(
                    canvas,
                    "Player Card",
                    new Vector2(18f, -18f),
                    new Vector2(360f, 132f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    PanelColor);

            CreateAccent(
                card,
                BlueAccent,
                new Vector2(5f, -8f),
                new Vector2(4f, 116f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            Text label =
                CreateText(
                    card,
                    "City Label",
                    11,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(18f, -9f),
                    new Vector2(120f, 18f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);
            label.text = "MOTOR CITY";

            moneyText =
                CreateText(
                    card,
                    "Credits",
                    22,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    new Vector2(-14f, -7f),
                    new Vector2(190f, 28f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    TextColor);

            reputationText =
                CreateText(
                    card,
                    "Reputation",
                    10,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    new Vector2(-14f, -36f),
                    new Vector2(190f, 18f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    SecondaryTextColor);

            driveModeText =
                CreateText(
                    card,
                    "Drive Mode",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(18f, -52f),
                    new Vector2(320f, 20f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f),
                    BlueAccent);

            objectiveText =
                CreateText(
                    card,
                    "Active Objective",
                    11,
                    FontStyle.Bold,
                    TextAnchor.LowerLeft,
                    new Vector2(18f, 12f),
                    new Vector2(324f, 42f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    TextColor);
        }

        private void BuildSpeedometer(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Speedometer",
                    new Vector2(-24f, 24f),
                    new Vector2(196f, 82f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    new Color(
                        0.01f,
                        0.015f,
                        0.022f,
                        0.54f));

            speedText =
                CreateText(
                    panel,
                    "Speed",
                    46,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-18f, 12f),
                    new Vector2(160f, 52f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    TextColor);

            speedUnitText =
                CreateText(
                    panel,
                    "Speed Unit",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-20f, -24f),
                    new Vector2(120f, 18f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    SecondaryTextColor);

            speedUnitText.text =
                MotorCityLocalization.Text(
                    "common.kmh");
        }

        private void BuildStatus(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Activity Status",
                    new Vector2(0f, 26f),
                    new Vector2(560f, 44f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    PanelSoftColor);

            statusPanel = panel.gameObject;

            CreateAccent(
                panel,
                BlueAccent,
                new Vector2(0f, 4f),
                new Vector2(500f, 3f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f));

            statusText =
                CreateText(
                    panel,
                    "Status Text",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -2f),
                    new Vector2(530f, 32f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);
        }

        private void BuildNavigator(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Minimap",
                    new Vector2(20f, 20f),
                    new Vector2(286f, 196f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Color(
                        0.01f,
                        0.015f,
                        0.02f,
                        0.82f));

            navigatorPanel =
                panel.gameObject;

            GameObject mapObject =
                new(
                    "Minimap View",
                    typeof(RectTransform),
                    typeof(RawImage));

            mapObject.transform.SetParent(
                panel,
                false);

            RectTransform mapRect =
                mapObject.GetComponent<RectTransform>();

            mapRect.anchorMin =
                new Vector2(0.5f, 1f);

            mapRect.anchorMax =
                new Vector2(0.5f, 1f);

            mapRect.pivot =
                new Vector2(0.5f, 1f);

            mapRect.anchoredPosition =
                new Vector2(0f, -8f);

            mapRect.sizeDelta =
                new Vector2(270f, 158f);

            minimapImage =
                mapObject.GetComponent<RawImage>();

            minimapImage.raycastTarget =
                false;

            minimapTexture =
                new RenderTexture(
                    256,
                    256,
                    16,
                    RenderTextureFormat.ARGB32);

            minimapTexture.name =
                "MotorCity_Minimap";

            minimapTexture.filterMode =
                FilterMode.Bilinear;

            minimapTexture.wrapMode =
                TextureWrapMode.Clamp;

            minimapImage.texture =
                minimapTexture;

            GameObject cameraObject =
                new("Motor City Minimap Camera");

            cameraObject.transform.SetParent(
                transform,
                false);

            minimapCamera =
                cameraObject.AddComponent<Camera>();

            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 88f;
            minimapCamera.nearClipPlane = 0.3f;
            minimapCamera.farClipPlane = 320f;
            minimapCamera.clearFlags =
                CameraClearFlags.SolidColor;

            minimapCamera.backgroundColor =
                new Color(
                    0.035f,
                    0.045f,
                    0.055f,
                    1f);

            minimapCamera.targetTexture =
                minimapTexture;

            minimapCamera.allowHDR = false;
            minimapCamera.allowMSAA = false;
            minimapCamera.useOcclusionCulling = false;
            minimapCamera.depth = -20f;

            Text playerArrow =
                CreateText(
                    panel,
                    "Minimap Player",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 11f),
                    new Vector2(34f, 34f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            playerArrow.text =
                "▲";

            Text targetBlip =
                CreateText(
                    panel,
                    "Minimap Target",
                    24,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(34f, 34f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        1f,
                        0.78f,
                        0.18f,
                        1f));

            targetBlip.text =
                "●";

            minimapTargetBlip =
                targetBlip.rectTransform;

            minimapTargetText =
                CreateText(
                    panel,
                    "Minimap Target Label",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(10f, 10f),
                    new Vector2(264f, 24f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    TextColor);
        }

        private void UpdateNavigator(
            bool garageOpen)
        {
            if (navigatorPanel == null ||
                minimapCamera == null ||
                car == null)
            {
                return;
            }

            if (garageOpen)
            {
                navigatorPanel.SetActive(false);
                return;
            }

            navigatorPanel.SetActive(true);

            Vector3 carPosition =
                car.transform.position;

            minimapCamera.transform.position =
                new Vector3(
                    carPosition.x,
                    carPosition.y + 115f,
                    carPosition.z);

            float yaw =
                car.transform.eulerAngles.y;

            minimapCamera.transform.rotation =
                Quaternion.Euler(
                    90f,
                    yaw,
                    0f);

            ResolveMinimapTarget(
                out Vector3 target,
                out string label,
                out bool hasTarget);

            if (!hasTarget)
            {
                if (minimapTargetBlip != null)
                    minimapTargetBlip.gameObject.SetActive(false);

                if (minimapTargetText != null)
                    minimapTargetText.text = string.Empty;

                return;
            }

            Vector3 delta =
                target -
                carPosition;

            delta.y = 0f;

            float distance =
                delta.magnitude;

            Vector3 local =
                Quaternion.Euler(
                    0f,
                    -yaw,
                    0f) *
                delta;

            const float mapHalfWidth = 122f;
            const float mapHalfHeight = 66f;
            const float worldRadius = 88f;

            Vector2 mapOffset =
                new Vector2(
                    local.x / worldRadius * mapHalfWidth,
                    local.z / worldRadius * mapHalfHeight);

            if (mapOffset.sqrMagnitude >
                mapHalfWidth * mapHalfWidth)
            {
                mapOffset =
                    mapOffset.normalized *
                    mapHalfWidth;
            }

            mapOffset.x =
                Mathf.Clamp(
                    mapOffset.x,
                    -mapHalfWidth,
                    mapHalfWidth);

            mapOffset.y =
                Mathf.Clamp(
                    mapOffset.y,
                    -mapHalfHeight,
                    mapHalfHeight);

            if (minimapTargetBlip != null)
            {
                minimapTargetBlip.gameObject.SetActive(true);

                minimapTargetBlip.anchoredPosition =
                    new Vector2(
                        mapOffset.x,
                        11f + mapOffset.y);
            }

            if (minimapTargetText != null)
            {
                minimapTargetText.text =
                    MotorCityLocalization.Format("hud.distance", label, Mathf.RoundToInt(distance));
            }
        }

        private void ResolveMinimapTarget(
            out Vector3 target,
            out string label,
            out bool hasTarget)
        {
            hasTarget = true;

            if (underground != null &&
                (underground.HasActiveInvitation ||
                 underground.IsActive ||
                 underground.IsCountingDown))
            {
                target =
                    underground.CurrentTarget;
                label =
                    underground.IsActive ||
                    underground.IsCountingDown
                        ? MotorCityLocalization.Text(
                            "hud.underground")
                        : MotorCityLocalization.Text(
                            "hud.secret_meeting");
                return;
            }

            if (delivery != null &&
                (delivery.IsActive ||
                 delivery.IsCountingDown))
            {
                target =
                    delivery.CurrentTarget;
                label =
                    MotorCityLocalization.Text(
                        "activity.delivery");
                return;
            }

            if (streetSprint != null &&
                (streetSprint.IsActive ||
                 streetSprint.IsCountingDown))
            {
                target =
                    streetSprint.CurrentTarget;
                label =
                    MotorCityLocalization.Text(
                        "hud.sprint");
                return;
            }

            if (circuitRace != null &&
                (circuitRace.IsActive ||
                 circuitRace.IsCountingDown))
            {
                target =
                    circuitRace.CurrentTarget;
                label =
                    MotorCityLocalization.Format("hud.circuit_lap", circuitRace.CurrentLap, circuitRace.LapCount);
                return;
            }

            if (driftChallenge != null &&
                (driftChallenge.IsActive ||
                 driftChallenge.IsCountingDown))
            {
                target =
                    driftChallenge.ZoneCenter;
                label =
                    MotorCityLocalization.Text(
                        "activity.drift");
                return;
            }

            ResolveNearestFreeRoamTarget(
                out target,
                out label);

            hasTarget =
                target !=
                car.transform.position;
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

            if (stuntJumps != null)
            {
                for (int i = 0;
                     i < stuntJumps.JumpCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        stuntJumps.GetJumpPosition(i),
                        MotorCityLocalization.Text("hud.stunt"),
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

        private static string DirectionArrow(
            float signedAngle)
        {
            float absolute =
                Mathf.Abs(
                    signedAngle);

            if (absolute <= 20f)
                return "↑";

            if (absolute >= 160f)
                return "↓";

            if (signedAngle > 0f)
                return absolute <= 70f
                    ? "↗"
                    : "→";

            return absolute <= 70f
                ? "↖"
                : "←";
        }

        private void BuildDriftPanel(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Drift HUD",
                    new Vector2(0f, -76f),
                    new Vector2(340f, 54f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Color(
                        0.10f,
                        0.055f,
                        0.025f,
                        0.92f));

            driftPanel = panel.gameObject;

            CreateAccent(
                panel,
                DriftAccent,
                new Vector2(0f, -4f),
                new Vector2(296f, 4f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f));

            driftText =
                CreateText(
                    panel,
                    "Drift Score",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -2f),
                    new Vector2(310f, 38f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);
        }

        private void BuildActivityResult(Transform canvas)
        {
            activityResultOverlay =
                new GameObject(
                    "Activity Result Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            activityResultOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                activityResultOverlay.GetComponent<RectTransform>();

            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            Image backdrop =
                activityResultOverlay.GetComponent<Image>();

            backdrop.color =
                new Color(
                    0.005f,
                    0.008f,
                    0.014f,
                    0.72f);

            backdrop.raycastTarget = false;

            RectTransform panel =
                CreatePanel(
                    activityResultOverlay.transform,
                    "Activity Result",
                    Vector2.zero,
                    new Vector2(620f, 330f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        0.02f,
                        0.028f,
                        0.042f,
                        0.98f));

            CreateAccent(
                panel,
                BlueAccent,
                new Vector2(0f, -5f),
                new Vector2(550f, 5f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f));

            resultTitleText =
                CreateText(
                    panel,
                    "Result Title",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -34f),
                    new Vector2(540f, 28f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            resultHeadlineText =
                CreateText(
                    panel,
                    "Result Headline",
                    34,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -84f),
                    new Vector2(550f, 52f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    TextColor);

            resultDetailsText =
                CreateText(
                    panel,
                    "Result Details",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -150f),
                    new Vector2(550f, 52f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            resultRewardText =
                CreateText(
                    panel,
                    "Result Reward",
                    28,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -210f),
                    new Vector2(500f, 42f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    TextColor);

            resultControlsText =
                CreateText(
                    panel,
                    "Result Controls",
                    15,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 30f),
                    new Vector2(540f, 28f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    SecondaryTextColor);

            resultControlsText.text =
                MotorCityLocalization.Text("hud.result_controls");
        }

        private void UpdateActivityResult()
        {
            if (activityManager == null ||
                !activityManager.HasResult)
                return;

            resultTitleText.text =
                activityManager.ResultTitle ?? string.Empty;

            resultHeadlineText.text =
                activityManager.ResultHeadline ?? string.Empty;

            resultDetailsText.text =
                activityManager.ResultDetails ?? string.Empty;

            bool hasCredits =
                activityManager.ResultRewardCredits > 0;

            bool hasReputation =
                activityManager.ResultReputationReward > 0;

            resultRewardText.text =
                hasCredits || hasReputation
                    ? MotorCityLocalization.Format(
                        "hud.result_reward",
                        activityManager.ResultRewardCredits,
                        activityManager.ResultReputationReward)
                    : MotorCityLocalization.Text(
                        "hud.no_rewards");

            Color accent =
                activityManager.ResultSuccess
                    ? new Color(0.20f, 1f, 0.58f, 1f)
                    : new Color(1f, 0.38f, 0.22f, 1f);

            resultHeadlineText.color = accent;
            resultRewardText.color =
                activityManager.ResultRewardCredits > 0 ||
                activityManager.ResultReputationReward > 0
                    ? accent
                    : SecondaryTextColor;
        }

        private void HandleActivityResultInput()
        {
            if (activityManager == null ||
                !activityManager.HasResult)
                return;

            if (MotorCityInput.CancelPressed)
            {
                activityManager.DismissResult();
                car?.SetDrivingEnabled(true);
                return;
            }

            bool restart =
                MotorCityInput.RetryPressed;

            if (!restart)
                return;

            switch (activityManager.ResultActivityId)
            {
                case "delivery":
                    delivery?.RestartFromResult();
                    break;

                case "drift":
                    driftChallenge?.RestartFromResult();
                    break;

                case "sprint":
                    streetSprint?.RestartFromResult();
                    break;

                case "circuit":
                    circuitRace?.RestartFromResult();
                    break;
            }
        }

        private void BuildGarage(Transform canvas)
        {
            garageOverlay =
                new GameObject(
                    "Garage Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            garageOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                garageOverlay.GetComponent<RectTransform>();
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            Image backdrop =
                garageOverlay.GetComponent<Image>();
            backdrop.color =
                new Color(0.005f, 0.008f, 0.012f, 0.68f);
            backdrop.raycastTarget = false;

            RectTransform panel =
                CreatePanel(
                    garageOverlay.transform,
                    "Garage Panel",
                    Vector2.zero,
                    new Vector2(760f, 574f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        0.02f,
                        0.026f,
                        0.038f,
                        0.98f));

            CreateAccent(
                panel,
                GarageAccent,
                new Vector2(0f, -5f),
                new Vector2(690f, 5f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f));

            Text title =
                CreateText(
                    panel,
                    "Garage Title",
                    27,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -28f),
                    new Vector2(390f, 42f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);
            title.text = "ГАРАЖ";

            garageMoneyText =
                CreateText(
                    panel,
                    "Garage Credits",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-28f, -28f),
                    new Vector2(250f, 42f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    TextColor);

            garageVehicleText =
                CreateText(
                    panel,
                    "Garage Vehicle",
                    17,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -76f),
                    new Vector2(704f, 24f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);

            garageVehicleStatsText =
                CreateText(
                    panel,
                    "Garage Vehicle Stats",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -101f),
                    new Vector2(704f, 22f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);

            garageVehicleHistoryText =
                CreateText(
                    panel,
                    "Garage Vehicle History",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -124f),
                    new Vector2(704f, 20f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.82f,
                        0.72f,
                        1f,
                        1f));

            garageVehicleSpecializationText =
                CreateText(
                    panel,
                    "Garage Vehicle Specialization",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -146f),
                    new Vector2(704f, 20f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.52f,
                        1f,
                        0.68f,
                        1f));

            garageCollectionText =
                CreateText(
                    panel,
                    "Garage Collection",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, -168f),
                    new Vector2(704f, 20f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Color(
                        0.92f,
                        0.74f,
                        1f,
                        1f));

            Color[] accents =
            {
                new(0.12f, 0.58f, 1f, 1f),
                new(0.12f, 0.9f, 0.58f, 1f),
                new(1f, 0.62f, 0.12f, 1f)
            };

            for (int i = 0; i < 3; i++)
            {
                float y =
                    -202f - i * 92f;

                RectTransform row =
                    CreatePanel(
                        panel,
                        $"Upgrade {i + 1}",
                        new Vector2(28f, y),
                        new Vector2(704f, 76f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        PanelSoftColor);

                CreateAccent(
                    row,
                    accents[i],
                    new Vector2(5f, -7f),
                    new Vector2(4f, 62f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));

                garageTitleTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Title",
                        18,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(18f, -8f),
                        new Vector2(440f, 26f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        TextColor);

                garagePriceTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Price",
                        17,
                        FontStyle.Bold,
                        TextAnchor.UpperRight,
                        new Vector2(-16f, -8f),
                        new Vector2(210f, 26f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        accents[i]);

                garageDescriptionTexts[i] =
                    CreateText(
                        row,
                        "Upgrade Description",
                        14,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(18f, 9f),
                        new Vector2(650f, 28f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        SecondaryTextColor);
            }

            garageStatusText =
                CreateText(
                    panel,
                    "Garage Status",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(28f, 26f),
                    new Vector2(420f, 26f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    SecondaryTextColor);

            Text footer =
                CreateText(
                    panel,
                    "Garage Controls",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-28f, 26f),
                    new Vector2(320f, 26f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    SecondaryTextColor);

            footer.text =
                MotorCityLocalization.Text("hud.garage_controls");
        }

        private void UpdateGarage()
        {
            garageMoneyText.text =
                MotorCityLocalization.Format("common.credits", garage.Credits);

            if (garageVehicleText != null)
            {
                garageVehicleText.text =
                    garage.VehicleLine;
            }

            if (garageVehicleStatsText != null)
            {
                garageVehicleStatsText.text =
                    garage.VehicleStatsLine;
            }

            if (garageVehicleHistoryText != null)
            {
                garageVehicleHistoryText.text =
                    vehicleHistory == null
                        ? string.Empty
                        : vehicleHistory.GarageLine;
            }

            if (garageVehicleSpecializationText != null)
            {
                garageVehicleSpecializationText.text =
                    vehicleSpecialization == null
                        ? string.Empty
                        : vehicleSpecialization.GarageLine;
            }

            if (garageCollectionText != null)
            {
                garageCollectionText.text =
                    collection == null
                        ? string.Empty
                        : collection.GarageLine;
            }

            for (int i = 0; i < 3; i++)
            {
                garageTitleTexts[i].text =
                    garage.GetUpgradeTitle(i);

                garagePriceTexts[i].text =
                    garage.GetUpgradePrice(i);

                garageDescriptionTexts[i].text =
                    garage.GetUpgradeDescription(i);
            }

            garageStatusText.text =
                garage.StatusText;
        }

        private string ResolveStatus()
        {
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
                    _ =>
                        activityManager.ActiveName
                };
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

            if (stuntJumps != null &&
                (stuntJumps.IsAttemptActive ||
                 stuntJumps.ShowMessage))
                return stuntJumps.StatusText;

            if (driftSpots != null &&
                driftSpots.ShowMessage)
                return driftSpots.StatusText;

            if (discoveries != null &&
                discoveries.ShowMessage)
                return discoveries.StatusText;

            if (speedTraps != null &&
                speedTraps.ShowMessage)
                return speedTraps.StatusText;

            return string.Empty;
        }

        private string ResolveObjectiveLine()
        {
            if (activityManager != null &&
                activityManager.IsBusy)
            {
                return
                    string.IsNullOrWhiteSpace(
                        activityManager.ActiveName)
                        ? string.Empty
                        : MotorCityLocalization.Format("hud.target", activityManager.ActiveName);
            }

            if (cityRisk != null &&
                cityRisk.PursuitActive)
            {
                return
                    cityRisk.HudLine;
            }

            if (underground != null &&
                underground.HasActiveInvitation)
            {
                return underground.HudLine;
            }

            if (legends != null &&
                !legends.AllLegendsDefeated)
            {
                return legends.HudLine;
            }

            if (liveEvents != null)
            {
                return liveEvents.HudLine;
            }

            if (contracts != null)
            {
                return contracts.HudLine;
            }

            return string.Empty;
        }

        private void OnDestroy()
        {
            if (minimapCamera != null)
            {
                minimapCamera.targetTexture = null;
                Destroy(
                    minimapCamera.gameObject);
            }

            if (minimapTexture != null)
            {
                minimapTexture.Release();
                Destroy(
                    minimapTexture);
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

            if (panelSprite != null)
            {
                image.sprite = panelSprite;
                image.type = Image.Type.Sliced;
            }

            Outline outline =
                go.AddComponent<Outline>();

            outline.effectColor =
                new Color(
                    0.35f,
                    0.5f,
                    0.72f,
                    0.14f);

            outline.effectDistance =
                new Vector2(1f, -1f);

            outline.useGraphicAlpha = true;
            return rect;
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

            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;

            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize =
                Mathf.Max(10, fontSize - 5);
            text.resizeTextMaxSize = fontSize;

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
