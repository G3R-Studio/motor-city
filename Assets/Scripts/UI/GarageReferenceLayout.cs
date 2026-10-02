using MotorCity.Localization;
using MotorCity.Input;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private Text garageReferenceMasteryValue;
        private GarageReferenceGraphic garageReferenceVehicleState;
        private static readonly Vector2 GarageReferenceScale = new(1920f / 1672f, 1080f / 941f);
        private static readonly Color GarageReferenceLilac = new(.72f, .72f, 1f, 1f);
        private static readonly Color GarageReferenceCyan = new(.28f, .78f, 1f, 1f);
        private static readonly Color GarageReferenceGreen = new(.12f, 1f, .55f, 1f);

        private void BuildGarage(Transform canvas)
        {
            RectTransform overlay = GarageObject(canvas, "Garage Overlay");
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            garageOverlay = overlay.gameObject;
            RectTransform panel = GarageObject(overlay, "Garage Panel");
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(1920, 1080);

            RectTransform balance = GarageSurface(panel, "Garage Top Balance", 405, 16, 871, 78);
            RectTransform credits = GarageRect(balance, "Garage Credits Group", 36, 14, 163, 52);
            RectTransform reputation = GarageRect(balance, "Garage Reputation Group", 227, 14, 169, 52);
            RectTransform level = GarageRect(balance, "Garage Level Group", 429, 14, 172, 52);
            ReferenceLabel(credits, "Garage Credits Label", MotorCityLocalization.Text("garage.credits_label"), 54, 0, 112, 20, 16);
            garageMoneyText = ReferenceLabel(credits, "Garage Credits", "", 54, 22, 112, 28, 24, Color.white);
            ReferenceLabel(reputation, "Garage Reputation Label", MotorCityLocalization.Text("garage.reputation_label"), 57, 0, 116, 20, 16);
            garageReputationText = ReferenceLabel(reputation, "Garage Reputation", "", 57, 22, 116, 28, 24, Color.white);
            GarageIcon(credits, "Garage Credits Icon", GarageReferenceGraphic.Symbol.Credits, GarageReferenceGreen, 0, 6, 38, 38);
            GarageIcon(reputation, "Garage Reputation Icon", GarageReferenceGraphic.Symbol.Crown, new Color(1,.25f,.9f), 0, 8, 41, 36);
            GarageIcon(level, "Garage Level Icon", GarageReferenceGraphic.Symbol.Star, GarageReferenceCyan, 0, 6, 39, 39);
            ReferenceLabel(level, "Garage Level Label", MotorCityLocalization.Text("garage.level_label"), 54, 0, 120, 20, 16);
            garageLevelText = ReferenceLabel(level, "Garage Level", "", 54, 22, 54, 28, 24, Color.white);
            garageHeaderLevelFill = GarageTrack(level, "Garage Header Level Track", 112, 31, 59, 14);
            ReferenceLabel(balance, "Garage Header Mastery Label", MotorCityLocalization.Text("garage.auto_mastery_label"), 698, 15, 161, 20, 16);
            garageHeaderMasteryText = ReferenceLabel(balance, "Garage Header Mastery", "", 698, 36, 62, 28, 24, Color.white);
            GarageIcon(balance, "Garage Wrench", GarageReferenceGraphic.Symbol.Wrench, GarageReferenceLilac, 645, 21, 42, 42);
            garageHeaderMasteryFill = GarageTrack(balance, "Garage Header Mastery Track", 752, 46, 89, 14);
            foreach (float x in new[] {203f, 401f, 623f}) GarageBar(balance, "Divider", x, 18, 1, 43);

            RectTransform card = GarageSurface(panel, "Garage Vehicle Card", 1257, 213, 396, 665);
            ReferenceLabel(card, "Garage Vehicle Card Title", MotorCityLocalization.Text("garage.my_car"), 87, 14, 277, 24, 18);
            garageVehicleText = ReferenceLabel(card, "Garage Vehicle", "", 87, 40, 291, 36, 29, Color.white);
            garageReferenceVehicleState = GarageIcon(card, "Garage Vehicle State Icon", GarageReferenceGraphic.Symbol.OpenPadlock, GarageReferenceGreen, 26, 22, 41, 49);
            garageNextVehicleText = ReferenceLabel(card, "Garage Next Vehicle", "", 26, 89, 350, 59, 20, GarageReferenceGreen);
            GarageReferenceGraphic.Symbol[] stats = {GarageReferenceGraphic.Symbol.Speed, GarageReferenceGraphic.Symbol.Acceleration,
                GarageReferenceGraphic.Symbol.Gear, GarageReferenceGraphic.Symbol.Stability, GarageReferenceGraphic.Symbol.Steering,
                GarageReferenceGraphic.Symbol.Drift, GarageReferenceGraphic.Symbol.Mass};
            for (int i = 0; i < stats.Length; i++)
            {
                float y = 171 + i * 36;
                garageVehicleStatLabels[i] = ReferenceLabel(card, "Garage Stat Label " + i, "", 71, y, 119, 26, 18);
                garageVehicleStatValues[i] = ReferenceLabel(card, "Garage Stat Value " + i, "", 314, y, 60, 26, 18, Color.white);
                garageVehicleStatFills[i] = GarageTrack(card, "Garage Stat Track " + i, 198, y + 7, 111, 14);
                GarageIcon(card, "Garage Stat Icon " + i, stats[i], GarageReferenceLilac, 27, y + 1, 27, 27);
            }
            garageVehicleCharacterText = ReferenceLabel(card, "Garage Vehicle Character", "", 25, 451, 350, 95, 18);
            ReferenceLabel(card, "Garage Mastery Label", MotorCityLocalization.Text("garage.mastery_label"), 25, 570, 205, 30, 20);
            garageReferenceMasteryValue = ReferenceLabel(card, "Garage Mastery Value", "", 310, 570, 64, 30, 23, Color.white);
            garageReferenceMasteryValue.alignment = TextAnchor.MiddleRight;
            GarageIcon(card, "Garage Mastery Icon", GarageReferenceGraphic.Symbol.Crown, GarageReferenceCyan, 25, 608, 36, 36);
            garageMasteryFill = GarageTrack(card, "Garage Mastery Track", 76, 615, 286, 19);
            foreach (float y in new[] {154f, 436f, 560f}) GarageBar(card, "Divider", 20, y, 356, 1);

            for (int i = 0; i < 3; i++)
            {
                RectTransform upgrade = GarageSurface(panel, "Upgrade " + (i + 1), 26 + i * 229, 694, i == 2 ? 231 : 218, 207);
                GarageBind(upgrade, i == 0 ? MotorCityInputAction.Upgrade1 : i == 1 ? MotorCityInputAction.Upgrade2 : MotorCityInputAction.Upgrade3);
                GarageIcon(upgrade, "Upgrade Icon", i == 0 ? GarageReferenceGraphic.Symbol.Engine : i == 1 ? GarageReferenceGraphic.Symbol.Brake : GarageReferenceGraphic.Symbol.Shock, GarageReferenceLilac, 18, 22, 52, 47);
                garageTitleTexts[i] = ReferenceLabel(upgrade, "Upgrade Title", "", 87, 18, i == 2 ? 140 : 123, 29, 22, Color.white);
                garageLevelTexts[i] = ReferenceLabel(upgrade, "Upgrade Level", "", 87, 50, 126, 25, 19);
                RectTransform track = GarageRect(upgrade, "Upgrade Level Track", 18, 89, (i == 2 ? 231 : 218) - 36, 16);
                for (int segment = 0; segment < 5; segment++)
                    garageUpgradeLevelSegments[i, segment] = GarageBar(track, "Upgrade Segment " + segment, 0, 0, 0, 16);
                track.gameObject.AddComponent<GarageUpgradeSegmentLayout>();
                RectTransform strip = GarageSurface(upgrade, "Upgrade Action Strip", 13, 124, i == 2 ? 205 : 192, 71);
                garageUpgradeActionTexts[i] = ReferenceLabel(strip, "Upgrade Action", "", 68, 7, 116, 25, 18, GarageReferenceCyan);
                GarageIcon(strip, "Reference Upgrade Arrow", GarageReferenceGraphic.Symbol.Up, GarageReferenceGreen, 37, 12, 22, 20);
                garagePriceIcons[i] = GarageIcon(strip, "Upgrade Price Icon", GarageReferenceGraphic.Symbol.Credits, GarageReferenceGreen, 38, 34, 26, 26);
                garagePriceTexts[i] = ReferenceLabel(strip, "Upgrade Price", "", 78, 32, 109, 33, 25, Color.white);
            }
            RectTransform actions = GarageObject(panel, "Garage Action Controls");
            actions.anchorMin = Vector2.zero; actions.anchorMax = Vector2.one;
            actions.offsetMin = actions.offsetMax = Vector2.zero;
            garageTouchControlsRoot = actions.gameObject;
            RectTransform appearance = GarageSurface(actions, "Garage Appearance Panel", 739, 733, 484, 161);
            ReferenceLabel(appearance, "Garage Appearance Title", MotorCityLocalization.Text("garage.appearance_title"), 13, 5, 458, 27, 20).alignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < 3; i++)
            {
                RectTransform button = GarageSurface(appearance, "Garage Appearance " + i, 16 + i * 155, 37, 143, 114);
                garageActionButtons[i + 2] = button.gameObject;
                GarageBind(button, i == 0 ? MotorCityInputAction.CycleBodyColor : i == 1 ? MotorCityInputAction.CycleWheels : MotorCityInputAction.CycleNeon);
                string key = i == 0 ? "touch.garage.color" : i == 1 ? "touch.garage.wheels" : "touch.garage.neon";
                ReferenceLabel(button, "Label", MotorCityLocalization.Text(key), 8, 76, 127, 31, 23, GarageReferenceCyan).alignment = TextAnchor.MiddleCenter;
                GarageIcon(button, "Garage Action Icon", i == 0 ? GarageReferenceGraphic.Symbol.Paint : i == 1 ? GarageReferenceGraphic.Symbol.Rim : GarageReferenceGraphic.Symbol.Neon,
                    i == 2 ? new Color(.95f,.26f,1) : GarageReferenceCyan, 42, 13, 58, 57);
            }
            for (int i = 0; i < 2; i++)
            {
                RectTransform arrow = GarageSurface(actions, i == 0 ? "Garage Previous Vehicle" : "Garage Next Vehicle", i == 0 ? 16 : 1165, 406, 86, 124,
                    i == 0 ? GarageReferenceGraphic.Symbol.NavigationLeft : GarageReferenceGraphic.Symbol.NavigationRight);
                garageActionButtons[i] = arrow.gameObject;
                GarageBind(arrow, i == 0 ? MotorCityInputAction.PreviousVehicle : MotorCityInputAction.NextVehicle);
                GarageIcon(arrow, "Chevron", i == 0 ? GarageReferenceGraphic.Symbol.Left : GarageReferenceGraphic.Symbol.Right, Color.white, 22, 30, 42, 65);
            }
            RectTransform city = GarageSurface(actions, "Garage City Button", 1348, 18, 305, 76, GarageReferenceGraphic.Symbol.CitySurface);
            garageActionButtons[6] = city.gameObject;
            GarageBind(city, MotorCityInputAction.Interact);
            ReferenceLabel(city, "Label", MotorCityLocalization.Text("garage.city_button"), 34, 15, 185, 45, 34, Color.white);
            GarageIcon(city, "Chevron", GarageReferenceGraphic.Symbol.Right, Color.white, 237, 15, 37, 48);
            RectTransform menu = GarageSurface(actions, "Garage Main Menu", 19, 21, 295, 69);
            GarageButton(menu).onClick.AddListener(OpenGarageMainMenu);
            ReferenceLabel(menu, "Label", MotorCityLocalization.Text("garage.main_menu_button"), 81, 11, 205, 47, 26, Color.white);
            GarageIcon(menu, "Chevron", GarageReferenceGraphic.Symbol.Left, Color.white, 32, 8, 35, 51);
            RectTransform passportAction = GarageRect(actions, "Garage Passport", 1282, 783, 350, 74);
            Image passportHit = passportAction.gameObject.AddComponent<Image>();
            passportHit.color = Color.clear; passportHit.raycastTarget = true;
            Button passportButton = passportAction.gameObject.AddComponent<Button>();
            passportButton.targetGraphic = passportHit;
            passportButton.onClick.AddListener(() => MotorCityInput.PulseVirtual(MotorCityInputAction.ToggleVehiclePassport));
            garageActionButtons[5] = passportAction.gameObject;
            garageStatusText = ReferenceLabel(panel, "Garage Status", "", 26, 910, 1197, 24, 16);
            BuildGaragePassport(panel);
            panel.gameObject.AddComponent<GarageCanvasRefresh>();
        }

        private void BuildGaragePassport(Transform panel)
        {
            RectTransform passport = GarageSurface(panel, "Vehicle Passport", 400, 260, 872, 390);
            garagePassportPanel = passport.gameObject;
            garagePassportTitleText = ReferenceLabel(passport, "Passport Title", "", 28, 20, 804, 38, 26, Color.white);
            garagePassportSummaryText = ReferenceLabel(passport, "Passport Summary", "", 28, 72, 804, 58, 18, GarageReferenceCyan);
            garagePassportDisciplinesText = ReferenceLabel(passport, "Passport Disciplines", "", 28, 146, 804, 56, 18);
            garagePassportMasteryText = ReferenceLabel(passport, "Passport Mastery", "", 28, 222, 804, 34, 18, GarageReferenceGreen);
            garagePassportSpecializationText = ReferenceLabel(passport, "Passport Specialization", "", 28, 274, 804, 74, 18);
            garagePassportPanel.SetActive(false);
        }

        private static RectTransform GarageObject(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }
        private static RectTransform GarageRect(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rect = GarageObject(parent, name);
            ReferencePlace(rect, x, y, w, h);
            return rect;
        }
        private static RectTransform GarageSurface(Transform parent, string name, float x, float y, float w, float h, GarageReferenceGraphic.Symbol symbol = GarageReferenceGraphic.Symbol.Surface)
        {
            RectTransform rect = GarageRect(parent, name, x, y, w, h);
            var graphic = rect.gameObject.AddComponent<GarageReferenceGraphic>();
            graphic.symbol = symbol; graphic.color = new Color(.16f,.12f,.32f,.94f);
            graphic.raycastTarget = false; graphic.SetAllDirty();
            return rect;
        }
        private static GarageReferenceGraphic GarageIcon(Transform parent, string name, GarageReferenceGraphic.Symbol symbol, Color color, float x, float y, float w, float h)
        {
            var graphic = GarageRect(parent, name, x, y, w, h).gameObject.AddComponent<GarageReferenceGraphic>();
            graphic.symbol = symbol; graphic.color = color; graphic.raycastTarget = false; graphic.SetAllDirty();
            return graphic;
        }
        private static Button GarageButton(RectTransform rect)
        {
            Graphic graphic = rect.GetComponent<Graphic>(); graphic.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic;
            return button;
        }
        private static void GarageBind(RectTransform rect, MotorCityInputAction action)
        {
            GarageButton(rect).onClick.AddListener(() => MotorCityInput.PulseVirtual(action));
        }
        private static Image GarageBar(Transform parent, string name, float x, float y, float w, float h)
        {
            Image image = GarageRect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = new Color(.42f,.39f,.70f,.8f); image.raycastTarget = false;
            return image;
        }
        private static Image GarageTrack(Transform parent, string name, float x, float y, float w, float h)
        {
            Image track = GarageBar(parent, name, x, y, w, h); track.color = new Color(.13f,.13f,.25f,.94f);
            RectTransform rect = GarageObject(track.transform, "Fill");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,.5f);
            rect.sizeDelta = new Vector2(0, h * GarageReferenceScale.y - 2);
            Image fill = rect.gameObject.AddComponent<Image>(); fill.color = GarageReferenceCyan; fill.raycastTarget = false;
            return fill;
        }
        private Text ReferenceLabel(Transform parent, string name, string value, float x, float y, float w, float h, int size, Color? tint = null)
        {
            Text text = GarageRect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = MotorCityTypography.Bold;
            text.fontStyle = FontStyle.Normal; text.alignment = TextAnchor.MiddleLeft;
            // Dynamic glyph line height is rounded at the current canvas scale.
            // Truncate can discard the entire first line in a tightly sized box.
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.horizontalOverflow = h < size * 2f
                ? HorizontalWrapMode.Overflow : HorizontalWrapMode.Wrap;
            text.fontSize = Mathf.RoundToInt(size * GarageReferenceScale.y);
            text.resizeTextForBestFit = false; text.resizeTextMinSize = Mathf.RoundToInt(text.fontSize * .82f); text.resizeTextMaxSize = text.fontSize;
            text.color = tint ?? GarageReferenceLilac; text.raycastTarget = false; text.text = value;
            return text;
        }
        private static void ReferencePlace(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.Scale(new Vector2(x, -y), GarageReferenceScale);
            rect.sizeDelta = Vector2.Scale(new Vector2(width, height), GarageReferenceScale);
        }
        private static void ConfigureGarageUpgradeAction(Text actionText, bool maxed)
        {

            actionText.alignment = maxed ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            actionText.fontSize = Mathf.RoundToInt((maxed ? 26f : 18f) * GarageReferenceScale.y);
            actionText.resizeTextForBestFit = false;
            if (maxed)
            {
                RectTransform rect = actionText.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, .5f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            else ReferencePlace(actionText.rectTransform, 68, 7, 116, 25);
        }

        private static string GarageReferenceMasteryNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            int start = value.LastIndexOf(' ');
            return start >= 0 ? value.Substring(start + 1) : value;
        }
        private void OpenGarageMainMenu()
        {
            MotorCityFrontEndFlow frontEnd = Object.FindAnyObjectByType<MotorCityFrontEndFlow>();
            if (frontEnd != null) frontEnd.ShowMainMenuFromGarage();
            else OpenPauseMenu();
        }
    }
}