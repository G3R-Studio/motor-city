using MotorCity.Localization;
using SpriteLessUI;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private Text garageReferenceMasteryValue;
        private GarageReferenceGraphic garageReferenceVehicleState;
        // Reference is 1672 x 941. Keep the HUD's existing 1920 x 1080 canvas.
        private static readonly Vector2 GarageReferenceScale = new(1920f / 1672f, 1080f / 941f);
        private static readonly Color GarageReferenceLilac = new(0.72f, 0.72f, 1f, 1f);
        private static readonly Color GarageReferenceCyan = new(0.28f, 0.78f, 1f, 1f);
        private static readonly Color GarageReferenceGreen = new(0.12f, 1f, 0.55f, 1f);

        private void ApplyGarageReferenceLayout(RectTransform panel)
        {
            Font condensed = Resources.Load<Font>("MotorCity/Fonts/RobotoCondensed-Regular");
            foreach (Text text in panel.GetComponentsInChildren<Text>(true))
            {
                if (condensed != null) text.font = condensed;
                text.raycastTarget = false;
                text.color = GarageReferenceLilac;
            }

            RectTransform balance = ReferenceRect(panel, "Garage Top Balance", 405, 16, 871, 78);
            ReferenceSurface(balance, false);
            ReferenceRect(balance, "Garage Credits Group", 36, 14, 163, 52);
            ReferenceRect(balance, "Garage Reputation Group", 227, 14, 169, 52);
            ReferenceRect(balance, "Garage Level Group", 429, 14, 172, 52);
            ReferenceRect(balance, "Garage Credits Group/Garage Credits Label", 54, 0, 112, 20);
            ReferenceRect(balance, "Garage Credits Group/Garage Credits", 54, 22, 112, 28);
            ReferenceRect(balance, "Garage Reputation Group/Garage Reputation Label", 57, 0, 116, 20);
            ReferenceRect(balance, "Garage Reputation Group/Garage Reputation", 57, 22, 116, 28);
            ReferenceIcon(balance.Find("Garage Credits Group/Garage Credits Icon"), GarageReferenceGraphic.Symbol.Credits, GarageReferenceGreen, 0, 6, 38, 38);
            ReferenceIcon(balance.Find("Garage Reputation Group/Garage Reputation Icon"), GarageReferenceGraphic.Symbol.Crown, new Color(1f, .25f, .9f), 0, 8, 41, 36);
            ReferenceIcon(balance.Find("Garage Level Group/Garage Level Icon"), GarageReferenceGraphic.Symbol.Star, new Color(.28f, .66f, 1f), 0, 6, 39, 39);
            ReferenceRect(balance, "Garage Level Group/Garage Level", 54, 22, 30, 28);
            ReferenceText(balance.Find("Garage Level Group/Garage Level"), 24, Color.white);
            ReferenceText(balance.Find("Garage Credits Group/Garage Credits"), 24, Color.white);
            ReferenceText(balance.Find("Garage Reputation Group/Garage Reputation"), 24, Color.white);
            ReferenceText(balance.Find("Garage Credits Group/Garage Credits Label"), 16, GarageReferenceLilac);
            ReferenceText(balance.Find("Garage Reputation Group/Garage Reputation Label"), 16, GarageReferenceLilac);
            ReferenceLabel(balance.Find("Garage Level Group"), "Reference Level Label", MotorCityLocalization.Text("garage.level_label"), 54, 0, 120, 20, 16);
            ReferenceTrack(balance.Find("Garage Level Group/Garage Header Level Track"), 86, 31, 85, 14);
            ReferenceRect(balance, "Garage Header Mastery", 698, 36, 62, 28);
            ReferenceText(balance.Find("Garage Header Mastery"), 24, Color.white);
            ReferenceLabel(balance, "Reference Mastery Label", MotorCityLocalization.Text("garage.auto_mastery_label"), 698, 15, 161, 20, 16);
            ReferenceIcon(CreateReferenceObject(balance, "Reference Wrench"), GarageReferenceGraphic.Symbol.Wrench, new Color(.60f,.57f,1f), 645, 21, 42, 42);
            ReferenceTrack(balance.Find("Garage Header Mastery Track"), 752, 46, 89, 14);
            // Move existing separators to the four measured metric columns.
            int separator = 0;
            foreach (Transform child in balance)
            {
                if (child.name != "Accent") continue;
                ReferencePlace(child as RectTransform, new[] {203f, 401f, 623f}[separator++], 18, 1, 43);
                if (separator == 3) break;
            }

            RectTransform card = ReferenceRect(panel, "Garage Vehicle Card", 1257, 213, 396, 665);
            ReferenceSurface(card, false);
            ReferenceRect(card, "Garage Vehicle Card Title", 87, 14, 277, 24);
            ReferenceText(card.Find("Garage Vehicle Card Title"), 18, GarageReferenceLilac);
            ReferenceRect(card, "Garage Vehicle", 87, 40, 291, 36);
            ReferenceText(card.Find("Garage Vehicle"), 29, Color.white);
            garageReferenceVehicleState = ReferenceIcon(card.Find("Garage Vehicle State Icon"), GarageReferenceGraphic.Symbol.OpenPadlock, GarageReferenceGreen, 26, 22, 41, 49);
            garageReferenceVehicleState.syncImageColor = true;
            ReferenceRect(card, "Garage Next Vehicle", 26, 89, 350, 59);
            ReferenceText(card.Find("Garage Next Vehicle"), 20, GarageReferenceGreen);

            string[] statNames = { "Speed", "Acceleration", "Grip", "Stability", "Steering", "Drift", "Mass" };
            GarageReferenceGraphic.Symbol[] statSymbols = {
                GarageReferenceGraphic.Symbol.Speed, GarageReferenceGraphic.Symbol.Acceleration,
                GarageReferenceGraphic.Symbol.Gear, GarageReferenceGraphic.Symbol.Stability,
                GarageReferenceGraphic.Symbol.Steering, GarageReferenceGraphic.Symbol.Drift,
                GarageReferenceGraphic.Symbol.Mass };
            for (int i = 0; i < garageVehicleStatFills.Length; i++)
            {
                float y = 171 + i * 36;
                ReferencePlace(garageVehicleStatLabels[i].rectTransform, 71, y, 119, 26);
                ReferenceText(garageVehicleStatLabels[i].transform, 18, GarageReferenceLilac);
                ReferencePlace(garageVehicleStatValues[i].rectTransform, 314, y, 60, 26);
                ReferenceText(garageVehicleStatValues[i].transform, 18, Color.white);
                ReferenceTrack(garageVehicleStatFills[i].transform.parent, 198, y + 7, 111, 14);
                ReferenceIcon(CreateReferenceObject(card, "Reference Stat " + statNames[i]), statSymbols[i], GarageReferenceLilac, 27, y + 1, 27, 27);
            }
            ReferenceRect(card, "Garage Vehicle Character", 25, 451, 350, 95);
            ReferenceText(card.Find("Garage Vehicle Character"), 18, GarageReferenceLilac);
            garageVehicleCharacterText.resizeTextForBestFit = true;
            garageVehicleCharacterText.resizeTextMinSize = 17;
            garageVehicleCharacterText.resizeTextMaxSize = 21;
            ReferenceRect(card, "Garage Mastery Label", 25, 570, 205, 30);
            ReferenceText(card.Find("Garage Mastery Label"), 20, GarageReferenceLilac);
            garageReferenceMasteryValue = ReferenceLabel(card, "Reference Mastery Value", string.Empty, 310, 570, 64, 30, 23);
            garageReferenceMasteryValue.alignment = TextAnchor.MiddleRight;
            ReferenceIcon(card.Find("Garage Mastery Icon"), GarageReferenceGraphic.Symbol.Crown, GarageReferenceCyan, 25, 608, 36, 36);
            ReferenceTrack(card.Find("Garage Mastery Track"), 76, 615, 286, 19);
            AddReferenceSegments(card.Find("Garage Mastery Track") as RectTransform, 6);
            int divider = 0;
            foreach (Transform child in card)
            {
                if (child.name != "Accent") continue;
                ReferencePlace(child as RectTransform, 20, new[] {154f, 436f, 560f}[divider++], 356, 1);
                if (divider == 3) break;
            }

            for (int i = 0; i < 3; i++)
            {
                RectTransform upgrade = ReferenceRect(panel, "Upgrade " + (i + 1), 26 + i * 229, 694, 218 + (i == 2 ? 13 : 0), 207);
                ReferenceSurface(upgrade, false);
                ReferenceIcon(upgrade.Find("Upgrade Icon"), i == 0 ? GarageReferenceGraphic.Symbol.Engine : i == 1 ? GarageReferenceGraphic.Symbol.Brake : GarageReferenceGraphic.Symbol.Shock, new Color(.65f,.54f,1f), 18, 22, 52, 47);
                ReferenceRect(upgrade, "Upgrade Title", 87, 18, i == 2 ? 140 : 123, 29);
                ReferenceText(upgrade.Find("Upgrade Title"), 22, Color.white);
                ReferenceRect(upgrade, "Upgrade Level", 87, 50, 126, 25);
                ReferenceText(upgrade.Find("Upgrade Level"), 19, GarageReferenceLilac);
                garageDescriptionTexts[i].gameObject.SetActive(false);
                for (int segment = 0; segment < 5; segment++)
                {
                    ReferencePlace(garageUpgradeLevelSegments[i, segment].rectTransform, 18 + segment * 37, 89, 36, 16);
                }
                RectTransform strip = ReferenceRect(upgrade, "Upgrade Action Strip", 13, 124, 192 + (i == 2 ? 13 : 0), 71);
                ReferenceSurface(strip, true);
                ReferenceRect(strip, "Upgrade Action", 68, 7, 116, 25);
                ReferenceText(strip.Find("Upgrade Action"), 18, GarageReferenceCyan);
                ReferenceIcon(CreateReferenceObject(strip, "Reference Upgrade Arrow"), GarageReferenceGraphic.Symbol.Up, GarageReferenceGreen, 37, 12, 22, 20);
                ReferenceIcon(strip.Find("Upgrade Price Icon"), GarageReferenceGraphic.Symbol.Credits, GarageReferenceGreen, 38, 34, 26, 26);
                ReferenceRect(strip, "Upgrade Price", 78, 32, 109, 33);
                ReferenceText(strip.Find("Upgrade Price"), 25, Color.white);
            }

            RectTransform actions = panel.Find("Garage Action Controls") as RectTransform;
            RectTransform appearance = ReferenceRect(actions, "Garage Appearance Panel", 739, 733, 484, 161);
            ReferenceSurface(appearance, false);
            ReferenceRect(appearance, "Garage Appearance Title", 13, 5, 458, 27);
            ReferenceText(appearance.Find("Garage Appearance Title"), 20, GarageReferenceLilac);
            for (int i = 0; i < 3; i++)
            {
                RectTransform button = garageActionButtons[i + 2].GetComponent<RectTransform>();
                ReferencePlace(button, 16 + i * 155, 37, 143, 114);
                ReferenceSurface(button, true);
                ReferenceRect(button, "Label", 8, 76, 127, 31);
                ReferenceText(button.Find("Label"), 23, GarageReferenceCyan);
                ReferenceIcon(button.Find("Garage Action Icon"), i == 0 ? GarageReferenceGraphic.Symbol.Paint : i == 1 ? GarageReferenceGraphic.Symbol.Rim : GarageReferenceGraphic.Symbol.Neon, i == 2 ? new Color(.95f,.26f,1f) : GarageReferenceCyan, 42, 13, 58, 57);
            }

            ReferencePlace(garageActionButtons[0].GetComponent<RectTransform>(), 16, 406, 86, 124);
            ReferencePlace(garageActionButtons[1].GetComponent<RectTransform>(), 1165, 406, 86, 124);
            for (int i = 0; i < 2; i++)
            {
                RectTransform arrow = garageActionButtons[i].GetComponent<RectTransform>();
                ReferenceNavigationSurface(arrow, i == 0 ? GarageReferenceGraphic.Symbol.NavigationLeft : GarageReferenceGraphic.Symbol.NavigationRight);
                arrow.Find("Label").gameObject.SetActive(false);
                ReferenceIcon(CreateReferenceObject(arrow, "Reference Chevron"), i == 0 ? GarageReferenceGraphic.Symbol.Left : GarageReferenceGraphic.Symbol.Right, Color.white, 22, 30, 42, 65);
            }
            RectTransform city = garageActionButtons[6].GetComponent<RectTransform>();
            ReferencePlace(city, 1348, 18, 305, 76);
            ReferenceNavigationSurface(city, GarageReferenceGraphic.Symbol.CitySurface);
            city.Find("Label").GetComponent<Text>().text = MotorCityLocalization.Text("garage.city_button");
            ReferenceRect(city, "Label", 34, 15, 185, 45);
            ReferenceText(city.Find("Label"), 34, Color.white);
            ReferenceIcon(CreateReferenceObject(city, "Reference City Chevron"), GarageReferenceGraphic.Symbol.Right, Color.white, 237, 15, 37, 48);

            RectTransform menu = ReferenceRect(actions, "Garage Main Menu Visual", 19, 21, 295, 69);
            ReferenceSurface(menu, true);
            menu.Find("Label").GetComponent<Text>().text = MotorCityLocalization.Text("garage.main_menu_button");
            ReferenceRect(menu, "Label", 81, 11, 205, 47);
            ReferenceText(menu.Find("Label"), 26, Color.white);
            ReferenceIcon(CreateReferenceObject(menu, "Reference Menu Chevron"), GarageReferenceGraphic.Symbol.Left, Color.white, 32, 8, 35, 51);
            SpriteLessImage menuShape = menu.GetComponent<SpriteLessImage>();
            menuShape.raycastTarget = true;
            Button menuButton = menu.gameObject.AddComponent<Button>();
            menuButton.targetGraphic = menuShape;
            menuButton.onClick.AddListener(OpenGarageMainMenu);

            // Keep the passport available without adding a separate card absent
            // from the reference: its existing action becomes the mastery hit area.
            RectTransform passport = garageActionButtons[5].GetComponent<RectTransform>();
            ReferencePlace(passport, 1282, 783, 350, 74);
            passport.GetComponent<SpriteLessImage>().color = Color.clear;
            passport.GetComponent<SpriteLessImage>().BorderEnabled = false;
            foreach (Graphic graphic in passport.GetComponentsInChildren<Graphic>(true))
                if (graphic.gameObject != passport.gameObject) graphic.enabled = false;
            ReferencePlace(garageStatusText.rectTransform, 26, 910, 1197, 24);
            ReferenceText(garageStatusText.transform, 16, GarageReferenceLilac);
        }

        private static RectTransform ReferenceRect(Transform parent, string path, float x, float y, float width, float height)
        {
            RectTransform rect = parent.Find(path) as RectTransform;
            if (rect != null) ReferencePlace(rect, x, y, width, height);
            return rect;
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

        private static void ReferencePlace(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.Scale(new Vector2(x, -y), GarageReferenceScale);
            rect.sizeDelta = Vector2.Scale(new Vector2(width, height), GarageReferenceScale);
        }

        private static void ReferenceText(Transform target, int size, Color color)
        {
            if (target == null) return;
            Text text = target.GetComponent<Text>();
            text.fontSize = Mathf.RoundToInt(size * GarageReferenceScale.y);
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.RoundToInt(size * GarageReferenceScale.y * .82f);
            text.resizeTextMaxSize = text.fontSize;
        }

        private Text ReferenceLabel(Transform parent, string name, string value, float x, float y, float w, float h, int size)
        {
            Text text = CreateText(parent, name, size, FontStyle.Bold, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, GarageReferenceLilac);
            ReferencePlace(text.rectTransform, x, y, w, h);
            text.font = Resources.Load<Font>("MotorCity/Fonts/RobotoCondensed-Regular") ?? text.font;
            ReferenceText(text.transform, size, GarageReferenceLilac);
            text.text = value;
            return text;
        }

        private static Transform CreateReferenceObject(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static GarageReferenceGraphic ReferenceIcon(Transform target, GarageReferenceGraphic.Symbol symbol, Color tint, float x, float y, float width, float height)
        {
            ReferencePlace(target as RectTransform, x, y, width, height);
            // Use a child Graphic so an existing Image component can continue
            // receiving state updates (locked/unlocked) from the HUD.
            Image original = target.GetComponent<Image>();
            if (original != null) original.enabled = false;
            var icon = CreateReferenceObject(target, "Reference Icon");
            RectTransform rect = icon as RectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var graphic = icon.gameObject.AddComponent<GarageReferenceGraphic>();
            graphic.symbol = symbol; graphic.color = tint; graphic.raycastTarget = false;
            graphic.replacedImage = original;
            graphic.syncImageColor = target.name == "Upgrade Price Icon";
            return graphic;
        }

        private static void ReferenceSurface(RectTransform rect, bool bright)
        {
            SpriteLessImage shape = rect.GetComponent<SpriteLessImage>();
            if (shape != null)
            {
                shape.color = new Color(.025f,.025f,.075f,.92f);
                shape.CornerRadius = 14;
                shape.BorderColor = bright ? new Color(.68f,.59f,1f,.95f) : new Color(.48f,.43f,.88f,.85f);
                shape.BorderWidth = bright ? 2f : 1.5f;
            }
            Transform surface = CreateReferenceObject(rect, "Reference Glass Gradient");
            RectTransform surfaceRect = surface as RectTransform;
            surfaceRect.anchorMin = Vector2.zero; surfaceRect.anchorMax = Vector2.one;
            surfaceRect.offsetMin = new Vector2(3,3); surfaceRect.offsetMax = new Vector2(-3,-3);
            var graphic = surface.gameObject.AddComponent<GarageReferenceGraphic>();
            graphic.symbol = GarageReferenceGraphic.Symbol.Surface;
            graphic.color = bright ? new Color(.25f,.22f,.48f,.72f) : new Color(.16f,.12f,.32f,.73f);
            graphic.raycastTarget = false;
            // Preserve the existing glow behind the fill and labels above it.
            surface.SetAsFirstSibling();
        }

        private static void ReferenceNavigationSurface(RectTransform rect, GarageReferenceGraphic.Symbol symbol)
        {
            SpriteLessImage original = rect.GetComponent<SpriteLessImage>();
            original.color = Color.clear; original.BorderEnabled = false;
            Transform surface = CreateReferenceObject(rect, "Reference Navigation Surface");
            RectTransform r = surface as RectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            var graphic = surface.gameObject.AddComponent<GarageReferenceGraphic>();
            graphic.symbol = symbol; graphic.raycastTarget = false;
            surface.SetAsFirstSibling();
        }

        private static void ReferenceTrack(Transform target, float x, float y, float width, float height)
        {
            RectTransform rect = target as RectTransform;
            ReferencePlace(rect, x, y, width, height);
            Image background = target.GetComponent<Image>();
            if (background != null) background.color = new Color(.13f,.13f,.25f,.94f);
            Outline outline = target.GetComponent<Outline>();
            if (outline != null) { outline.enabled = true; outline.effectColor = new Color(.42f,.39f,.70f,.8f); outline.effectDistance = new Vector2(1, -1); }
            RectTransform fill = target.GetChild(0) as RectTransform;
            fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0,.5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(0, height * GarageReferenceScale.y - 2);
            fill.GetComponent<Image>().color = GarageReferenceCyan;
        }

        private static void AddReferenceSegments(RectTransform track, int count)
        {
            for (int i = 1; i < count; i++)
            {
                Transform tick = CreateReferenceObject(track, "Reference Segment " + i);
                RectTransform rect = tick as RectTransform;
                rect.anchorMin = new Vector2((float)i / count,0);
                rect.anchorMax = new Vector2((float)i / count,1);
                rect.sizeDelta = new Vector2(1, -4);
                rect.anchoredPosition = Vector2.zero;
                var image = tick.gameObject.AddComponent<Image>();
                image.color = new Color(.1f,.1f,.25f,.7f); image.raycastTarget = false;
            }
        }
    }
}
