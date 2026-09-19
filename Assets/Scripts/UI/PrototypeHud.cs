using MotorCity.Gameplay;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private ActivityManager activityManager;
        private GarageUpgradeSystem garage;

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
            ActivityManager manager,
            GarageUpgradeSystem garageSystem)
        {
            car = controller;
            wallet = playerWallet;
            drift = driftTracker;
            delivery = deliveryActivity;
            driftChallenge = challenge;
            streetSprint = sprint;
            circuitRace = circuit;
            activityManager = manager;
            garage = garageSystem;

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
                $"{credits:N0} КР";

            if (reputationText != null &&
                activityManager != null)
            {
                reputationText.text =
                    $"REP {activityManager.TotalReputation:N0}   •   УР. {activityManager.ReputationLevel}";
            }

            upgradesText.text =
                garage == null
                    ? string.Empty
                    : $"ДВИГ {garage.EngineLevel}   •   СЦЕП {garage.GripLevel}   •   СТАБ {garage.StabilityLevel}";

            if (driveModeText != null &&
                car != null)
            {
                driveModeText.text =
                    $"РЕЖИМ  {car.DriveModeDisplayName}";

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
                        $"DRIFT   {drift.CurrentScore:N0}{combo}";
                }
                else
                {
                    driftText.text =
                        $"DRIFT ЗАВЕРШЁН   +{drift.LastBankedCredits:N0} КР";
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
            BuildControlsHint(canvasObject.transform);
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
                    new Vector2(360f, 96f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    PanelColor);

            CreateAccent(
                card,
                BlueAccent,
                new Vector2(5f, -8f),
                new Vector2(4f, 80f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            Text label =
                CreateText(
                    card,
                    "City Label",
                    12,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(20f, -9f),
                    new Vector2(150f, 18f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);
            label.text = "MOTOR CITY";

            moneyText =
                CreateText(
                    card,
                    "Credits",
                    24,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    new Vector2(-14f, -8f),
                    new Vector2(170f, 30f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    TextColor);

            reputationText =
                CreateText(
                    card,
                    "Reputation",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-14f, -40f),
                    new Vector2(170f, 18f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0.5f),
                    SecondaryTextColor);

            driveModeText =
                CreateText(
                    card,
                    "Drive Mode",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(20f, -51f),
                    new Vector2(320f, 20f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f),
                    BlueAccent);

            upgradesText =
                CreateText(
                    card,
                    "Upgrades",
                    13,
                    FontStyle.Bold,
                    TextAnchor.LowerLeft,
                    new Vector2(20f, 9f),
                    new Vector2(320f, 22f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    SecondaryTextColor);
        }

        private void BuildSpeedometer(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Speedometer",
                    new Vector2(-20f, 20f),
                    new Vector2(174f, 112f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    PanelColor);

            CreateAccent(
                panel,
                BlueAccent,
                new Vector2(-9f, 8f),
                new Vector2(156f, 4f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f));

            speedText =
                CreateText(
                    panel,
                    "Speed",
                    50,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 12f),
                    new Vector2(158f, 68f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            speedUnitText =
                CreateText(
                    panel,
                    "Speed Unit",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -32f),
                    new Vector2(130f, 20f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    SecondaryTextColor);
            speedUnitText.text = "КМ/Ч";
        }

        private void BuildStatus(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Activity Status",
                    new Vector2(0f, -18f),
                    new Vector2(720f, 46f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    PanelSoftColor);

            statusPanel = panel.gameObject;

            CreateAccent(
                panel,
                BlueAccent,
                new Vector2(0f, -4f),
                new Vector2(650f, 3f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f));

            statusText =
                CreateText(
                    panel,
                    "Status Text",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -2f),
                    new Vector2(684f, 34f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);
        }

        private void BuildNavigator(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Navigator",
                    new Vector2(-20f, -18f),
                    new Vector2(300f, 74f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    PanelSoftColor);

            navigatorPanel =
                panel.gameObject;

            CreateAccent(
                panel,
                BlueAccent,
                new Vector2(-8f, -8f),
                new Vector2(4f, 58f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f));

            navigatorArrowText =
                CreateText(
                    panel,
                    "Navigator Arrow",
                    30,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(22f, -37f),
                    new Vector2(54f, 54f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f),
                    BlueAccent);

            navigatorText =
                CreateText(
                    panel,
                    "Navigator Text",
                    15,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(58f, -37f),
                    new Vector2(220f, 52f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f),
                    TextColor);
        }

        private void UpdateNavigator(
            bool garageOpen)
        {
            if (navigatorPanel == null ||
                navigatorArrowText == null ||
                navigatorText == null)
                return;

            if (car == null ||
                garageOpen)
            {
                navigatorPanel.SetActive(false);
                return;
            }

            Vector3 target;
            string label;

            if (delivery != null &&
                (delivery.IsActive ||
                 delivery.IsCountingDown))
            {
                target =
                    delivery.CurrentTarget;
                label =
                    "ДОСТАВКА";
            }
            else if (streetSprint != null &&
                     (streetSprint.IsActive ||
                      streetSprint.IsCountingDown))
            {
                target =
                    streetSprint.CurrentTarget;
                label =
                    "СПРИНТ";
            }
            else if (circuitRace != null &&
                     (circuitRace.IsActive ||
                      circuitRace.IsCountingDown))
            {
                target =
                    circuitRace.CurrentTarget;
                label =
                    $"КОЛЬЦО {circuitRace.CurrentLap}/{circuitRace.LapCount}";
            }
            else if (driftChallenge != null &&
                     (driftChallenge.IsActive ||
                      driftChallenge.IsCountingDown))
            {
                target =
                    driftChallenge.ZoneCenter;
                label =
                    "ДРИФТ-ЗОНА";
            }
            else
            {
                ResolveNearestFreeRoamTarget(
                    out target,
                    out label);
            }

            Vector3 toTarget =
                target -
                car.transform.position;

            toTarget.y = 0f;

            float distance =
                toTarget.magnitude;

            if (distance < 0.1f)
            {
                navigatorArrowText.text =
                    "•";
            }
            else
            {
                float signedAngle =
                    Vector3.SignedAngle(
                        car.transform.forward,
                        toTarget.normalized,
                        Vector3.up);

                navigatorArrowText.text =
                    DirectionArrow(
                        signedAngle);
            }

            navigatorText.text =
                $"{label}\n{Mathf.RoundToInt(distance)} М";

            navigatorPanel.SetActive(true);
        }

        private void ResolveNearestFreeRoamTarget(
            out Vector3 target,
            out string label)
        {
            target =
                car.transform.position;
            label =
                "СВОБОДНАЯ ЕЗДА";

            float bestDistance =
                float.PositiveInfinity;

            ConsiderNavigationTarget(
                delivery != null
                    ? delivery.CurrentTarget
                    : Vector3.zero,
                "ДОСТАВКА",
                delivery != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                driftChallenge != null
                    ? driftChallenge.ZoneCenter
                    : Vector3.zero,
                "ДРИФТ",
                driftChallenge != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                streetSprint != null
                    ? streetSprint.CurrentTarget
                    : Vector3.zero,
                "СПРИНТ",
                streetSprint != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                circuitRace != null
                    ? circuitRace.CurrentTarget
                    : Vector3.zero,
                "КОЛЬЦО",
                circuitRace != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                garage != null
                    ? garage.GarageCenter
                    : Vector3.zero,
                "ГАРАЖ",
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

        private void BuildControlsHint(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Controls Hint",
                    new Vector2(18f, 20f),
                    new Vector2(810f, 34f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Color(
                        PanelSoftColor.r,
                        PanelSoftColor.g,
                        PanelSoftColor.b,
                        0.72f));

            hintText =
                CreateText(
                    panel,
                    "Controls Text",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(14f, 0f),
                    new Vector2(782f, 24f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    SecondaryTextColor);

            hintText.text =
                "WASD  ДВИЖЕНИЕ   Q  РЕЖИМ   SPACE  РУЧНИК   E  СТАРТ / ГАРАЖ   ESC  ОТМЕНА   R  СБРОС";
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
                "ENTER  ПОВТОРИТЬ     ESC  ПРОДОЛЖИТЬ";
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
                    ? $"+{activityManager.ResultRewardCredits:N0} КР   +{activityManager.ResultReputationReward:N0} REP"
                    : "БЕЗ НАГРАДЫ";

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
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null ||
                activityManager == null ||
                !activityManager.HasResult)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                activityManager.DismissResult();
                car?.SetDrivingEnabled(true);
                return;
            }

            bool restart =
                keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame;

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
                    new Vector2(760f, 446f),
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

            Color[] accents =
            {
                new(0.12f, 0.58f, 1f, 1f),
                new(0.12f, 0.9f, 0.58f, 1f),
                new(1f, 0.62f, 0.12f, 1f)
            };

            for (int i = 0; i < 3; i++)
            {
                float y =
                    -104f - i * 92f;

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
                "1 / 2 / 3  КУПИТЬ    E / ESC  ЗАКРЫТЬ";
        }

        private void UpdateGarage()
        {
            garageMoneyText.text =
                $"{garage.Credits:N0} КР";

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

            return
                "СВОБОДНАЯ ЕЗДА   •   ДОСТАВКА   •   ДРИФТ   •   СПРИНТ   •   КОЛЬЦО   •   ГАРАЖ";
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
