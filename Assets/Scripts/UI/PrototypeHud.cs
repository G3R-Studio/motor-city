using MotorCity.Gameplay;
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
        private ActivityManager activityManager;
        private GarageUpgradeSystem garage;

        private Font font;
        private Text moneyText;
        private Text upgradesText;
        private Text hintText;
        private Text speedText;
        private Text statusText;
        private Text driftText;
        private GameObject driftPanel;
        private GameObject garageOverlay;
        private Text garageMoneyText;
        private Text garageStatusText;
        private readonly Text[] garageTitleTexts = new Text[3];
        private readonly Text[] garagePriceTexts = new Text[3];
        private readonly Text[] garageDescriptionTexts = new Text[3];

        public void Bind(
            ArcadeCarController controller,
            PlayerWallet playerWallet,
            DriftTracker driftTracker,
            DeliveryActivity deliveryActivity,
            DriftChallenge challenge,
            StreetSprintActivity sprint,
            ActivityManager manager,
            GarageUpgradeSystem garageSystem)
        {
            car = controller;
            wallet = playerWallet;
            drift = driftTracker;
            delivery = deliveryActivity;
            driftChallenge = challenge;
            streetSprint = sprint;
            activityManager = manager;
            garage = garageSystem;

            BuildUi();
        }

        private void Update()
        {
            if (moneyText == null)
                return;

            int credits = wallet == null ? 0 : wallet.Credits;
            moneyText.text = $"MOTOR CITY    {credits:N0} КР";

            upgradesText.text = garage == null
                ? string.Empty
                : $"ДВ {garage.EngineLevel}    СЦ {garage.GripLevel}    СТ {garage.StabilityLevel}";

            float speed = car == null ? 0f : car.SpeedKph;
            speedText.text = $"{speed:000}\nкм/ч";

            statusText.text = ResolveStatus();

            bool showDrift =
                drift != null &&
                (drift.IsDrifting ||
                 drift.CurrentScore > 0 ||
                 drift.ShowRewardMessage);

            driftPanel.SetActive(showDrift);

            if (showDrift)
            {
                if (drift.IsDrifting || drift.CurrentScore > 0)
                {
                    string combo =
                        drift.Combo > 1.05f
                            ? $"    x{drift.Combo:0.0}"
                            : string.Empty;

                    driftText.text =
                        $"ДРИФТ  {drift.CurrentScore:N0}{combo}";
                }
                else
                {
                    driftText.text =
                        $"ДРИФТ ЗАВЕРШЁН   +{drift.LastBankedCredits:N0} КР";
                }
            }

            bool garageOpen =
                garage != null && garage.IsOpen;

            garageOverlay.SetActive(garageOpen);

            if (garageOpen)
                UpdateGarage();
        }

        private void BuildUi()
        {
            font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            GameObject canvasObject =
                new("Motor City Canvas");
            canvasObject.transform.SetParent(
                transform,
                false);

            Canvas canvas =
                canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler =
                canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            Sprite grey =
                Resources.Load<Sprite>("MotorCity/UI/grey_panel");
            Sprite blue =
                Resources.Load<Sprite>("MotorCity/UI/blue_panel");
            Sprite green =
                Resources.Load<Sprite>("MotorCity/UI/green_panel");
            Sprite yellow =
                Resources.Load<Sprite>("MotorCity/UI/yellow_panel");

            RectTransform topPanel =
                CreatePanel(
                    canvasObject.transform,
                    "Панель игрока",
                    grey,
                    new Vector2(18f, -18f),
                    new Vector2(570f, 112f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));

            moneyText =
                CreateText(
                    topPanel,
                    "Деньги",
                    27,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(22f, -12f),
                    new Vector2(520f, 42f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));

            upgradesText =
                CreateText(
                    topPanel,
                    "Улучшения",
                    19,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(22f, -57f),
                    new Vector2(520f, 32f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));

            RectTransform hintPanel =
                CreatePanel(
                    canvasObject.transform,
                    "Подсказки",
                    grey,
                    new Vector2(18f, -142f),
                    new Vector2(540f, 48f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));

            hintText =
                CreateText(
                    hintPanel,
                    "Текст подсказок",
                    17,
                    FontStyle.Normal,
                    TextAnchor.MiddleLeft,
                    new Vector2(18f, -6f),
                    new Vector2(505f, 34f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));

            hintText.text =
                "R — сброс   ПКМ — камера   Колесо — приближение";

            RectTransform speedPanel =
                CreatePanel(
                    canvasObject.transform,
                    "Скорость",
                    grey,
                    new Vector2(-22f, 22f),
                    new Vector2(290f, 108f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f));

            speedText =
                CreateText(
                    speedPanel,
                    "Скорость",
                    34,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(260f, 92f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f));

            RectTransform statusPanel =
                CreatePanel(
                    canvasObject.transform,
                    "Активность",
                    grey,
                    new Vector2(0f, 22f),
                    new Vector2(1180f, 62f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f));

            statusText =
                CreateText(
                    statusPanel,
                    "Статус",
                    19,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(1135f, 48f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f));

            RectTransform driftRect =
                CreatePanel(
                    canvasObject.transform,
                    "Дрифт",
                    yellow,
                    new Vector2(0f, -28f),
                    new Vector2(480f, 72f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f));

            driftPanel = driftRect.gameObject;
            driftText =
                CreateText(
                    driftRect,
                    "Очки дрифта",
                    25,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(440f, 56f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f));

            BuildGarage(
                canvasObject.transform,
                grey,
                blue,
                green,
                yellow);

            driftPanel.SetActive(false);
            garageOverlay.SetActive(false);
        }

        private void BuildGarage(
            Transform canvas,
            Sprite grey,
            Sprite blue,
            Sprite green,
            Sprite yellow)
        {
            garageOverlay =
                new GameObject(
                    "Гараж UI",
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
                new Color(0f, 0f, 0f, 0.48f);
            backdrop.raycastTarget = false;

            RectTransform panel =
                CreatePanel(
                    garageOverlay.transform,
                    "Панель гаража",
                    grey,
                    Vector2.zero,
                    new Vector2(860f, 520f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f));

            CreateText(
                panel,
                "Заголовок гаража",
                34,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Vector2(30f, -24f),
                new Vector2(560f, 52f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f)).text =
                    "ГАРАЖ MOTOR CITY";

            garageMoneyText =
                CreateText(
                    panel,
                    "Деньги гаража",
                    28,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(-30f, -24f),
                    new Vector2(260f, 52f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f));

            Sprite[] rowSprites =
            {
                blue,
                green,
                yellow
            };

            for (int i = 0; i < 3; i++)
            {
                float y = -108f - i * 104f;

                RectTransform row =
                    CreatePanel(
                        panel,
                        $"Улучшение {i + 1}",
                        rowSprites[i],
                        new Vector2(30f, y),
                        new Vector2(800f, 88f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f));

                garageTitleTexts[i] =
                    CreateText(
                        row,
                        "Название",
                        22,
                        FontStyle.Bold,
                        TextAnchor.MiddleLeft,
                        new Vector2(18f, -8f),
                        new Vector2(520f, 32f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f));

                garagePriceTexts[i] =
                    CreateText(
                        row,
                        "Цена",
                        22,
                        FontStyle.Bold,
                        TextAnchor.MiddleRight,
                        new Vector2(-18f, -8f),
                        new Vector2(220f, 32f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f));

                garageDescriptionTexts[i] =
                    CreateText(
                        row,
                        "Описание",
                        16,
                        FontStyle.Normal,
                        TextAnchor.MiddleLeft,
                        new Vector2(18f, -44f),
                        new Vector2(750f, 28f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f));
            }

            garageStatusText =
                CreateText(
                    panel,
                    "Статус гаража",
                    17,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(30f, 62f),
                    new Vector2(560f, 34f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f));

            Text footer =
                CreateText(
                    panel,
                    "Управление гаражом",
                    17,
                    FontStyle.Normal,
                    TextAnchor.MiddleRight,
                    new Vector2(-30f, 62f),
                    new Vector2(360f, 34f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f));

            footer.text =
                "1 / 2 / 3 — купить    E / Esc — закрыть";
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
                    "delivery" => delivery?.StatusText,
                    "drift" => driftChallenge?.StatusText,
                    "sprint" => streetSprint?.StatusText,
                    "garage" => garage?.StatusText,
                    _ => activityManager.ActiveName
                };
            }

            return
                "СИНИЙ — доставка   •   ОРАНЖЕВЫЙ — дрифт   •   ЗЕЛЁНЫЙ — спринт   •   ФИОЛЕТОВЫЙ — гараж";
        }

        private RectTransform CreatePanel(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot)
        {
            GameObject go =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image));

            go.transform.SetParent(parent, false);

            RectTransform rect =
                go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image =
                go.GetComponent<Image>();

            image.raycastTarget = false;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color =
                    new Color(0.06f, 0.075f, 0.09f, 0.92f);
            }

            return rect;
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
            Vector2 pivot)
        {
            GameObject go =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Text));

            go.transform.SetParent(parent, false);

            RectTransform rect =
                go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text =
                go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow =
                HorizontalWrapMode.Overflow;
            text.verticalOverflow =
                VerticalWrapMode.Overflow;

            return text;
        }
    }
}
