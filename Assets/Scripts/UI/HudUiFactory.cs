using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private Image CreateHudIcon(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Color color)
        {
            GameObject iconObject =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image));

            iconObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                iconObject.GetComponent<RectTransform>();

            rect.anchorMin =
                anchor;
            rect.anchorMax =
                anchor;
            rect.pivot =
                anchor;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                iconObject.GetComponent<Image>();

            image.sprite =
                sprite;

            image.preserveAspect =
                true;

            image.raycastTarget =
                false;

            image.color =
                color;

            image.enabled =
                sprite != null;

            return image;
        }

        private static void MakeButtonTextCrisp(
            Text text)
        {
            if (text == null)
                return;

            // Button labels are short and already sized for their controls.
            // Avoid Best Fit here: dynamic shrinking can land on fractional
            // glyph sizes and makes LegacyRuntime text look soft/pixelated.
            text.resizeTextForBestFit =
                false;

            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;

            text.verticalOverflow =
                VerticalWrapMode.Overflow;

            RectTransform rect =
                text.rectTransform;

            Vector2 position =
                rect.anchoredPosition;

            rect.anchoredPosition =
                new Vector2(
                    Mathf.Round(position.x),
                    Mathf.Round(position.y));

            Vector2 size =
                rect.sizeDelta;

            rect.sizeDelta =
                new Vector2(
                    Mathf.Round(size.x),
                    Mathf.Round(size.y));
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

            Outline outline =
                go.AddComponent<Outline>();

            outline.effectColor =
                new Color(
                    BlueAccent.r,
                    BlueAccent.g,
                    BlueAccent.b,
                    0.22f);

            outline.effectDistance =
                new Vector2(1f, -1f);

            outline.useGraphicAlpha = true;
            // The builder now owns legacy chrome suppression. No late
            // scene-wide visual pass is required on resolution changes.
            switch (name)
            {
                case "Player Card":
                case "Character Card":
                case "Activity Status":
                case "Speedometer":
                case "Minimap":
                case "Drift HUD":
                case "Activity Result":
                case "Navigator Menu":
                case "Pause Panel":
                case "Club Panel":
                    ClearPanelChrome(rect);
                    break;
            }
            return rect;
        }

        private void ClearPanelChrome(
            RectTransform panel)
        {
            if (panel == null)
                return;

            Image image =
                panel.GetComponent<Image>();

            if (image != null)
            {
                image.color =
                    Color.clear;
                image.raycastTarget =
                    false;
            }

            Outline outline =
                panel.GetComponent<Outline>();

            if (outline != null)
            {
                outline.enabled =
                    false;
            }
        }

        private static void ApplyReferenceHudSurface(RectTransform panel, float alpha = 0.94f)
        {
            if (panel == null) return;
            Image image = panel.GetComponent<Image>();
            if (image != null) { image.color = Color.clear; image.raycastTarget = false; }
            foreach (Outline outline in panel.GetComponents<Outline>()) outline.enabled = false;
            Transform existing = panel.Find("Reference HUD Surface");
            if (existing != null) return;
            RectTransform background = GarageObject(panel, "Reference HUD Surface");
            background.SetAsFirstSibling();
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = background.offsetMax = Vector2.zero;
            GarageReferenceGraphic graphic = background.gameObject.AddComponent<GarageReferenceGraphic>();
            graphic.symbol = GarageReferenceGraphic.Symbol.Surface;
            graphic.color = new Color(.16f, .12f, .32f, alpha);
            graphic.raycastTarget = false;
        }

        private void ApplyModalPanelTexture(RectTransform panel)
        {
            ApplyReferenceHudSurface(panel);
        }

        private void ApplyVillePanelTexture(RectTransform panel, float alpha)
        {
            ApplyReferenceHudSurface(panel, alpha);
        }

        private void ApplyDrivingHudReferenceStyle(Transform root)
        {
            foreach (Text label in root.GetComponentsInChildren<Text>(true))
            {
                bool garageLabel = false;
                for (Transform current = label.transform; current != null; current = current.parent)
                    if (current.name == "Garage Overlay") { garageLabel = true; break; }
                if (garageLabel) continue;
                label.fontSize = Mathf.Max(15, label.fontSize);
                label.resizeTextMinSize = 15;
                label.resizeTextMaxSize = label.fontSize;
                label.alignByGeometry = false;
                MotorCityTextLayout.Configure(label);
            }
            foreach (RectTransform panel in root.GetComponentsInChildren<RectTransform>(true))
                if (panel.name == "Season Panel" || panel.name == "Character Portrait Frame"
                    || panel.name == "Navigator Destination Card" || panel.name == "Navigator Icon Plate")
                    ApplyReferenceHudSurface(panel);
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                // Garage already builds its own vector controls. Steering/pedals
                // retain their special hold/drag hit areas and transparent artwork.
                bool excluded = false;
                for (Transform current = button.transform; current != null; current = current.parent)
                    if (current.name == "Garage Overlay" ||
                        (current.name == "Touch Driving Controls" && button.name != "Interact"))
                        { excluded = true; break; }
                // Vector targets are created by the button builder.
                if (excluded ||
                    button.targetGraphic is GarageReferenceGraphic)
                    continue;
                Image image = button.GetComponent<Image>();
                if (image == null) continue;
                image.color = Color.clear;
                image.raycastTarget = false;
                foreach (Outline outline in button.GetComponents<Outline>()) outline.enabled = false;
                // Unity allows only one Graphic per GameObject. Keep the
                // existing Image and put vector artwork on its own child.
                ApplyReferenceHudSurface(button.GetComponent<RectTransform>());
                GarageReferenceGraphic surface = button.transform
                    .Find("Reference HUD Surface").GetComponent<GarageReferenceGraphic>();
                surface.symbol = GarageReferenceGraphic.Symbol.Surface;
                surface.color = MotorCityUiSkin.Raised;
                surface.raycastTarget = true;
                button.targetGraphic = surface;
                if (button.name == "Interact")
                {
                    Transform oldIcon = button.transform.Find("Action Icon");
                    if (oldIcon != null) oldIcon.gameObject.SetActive(false);
                    RectTransform iconRect = GarageObject(button.transform, "Reference Action Icon");
                    iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, .5f);
                    iconRect.anchoredPosition = new Vector2(14f, 0f);
                    iconRect.sizeDelta = new Vector2(24f, 24f);
                    GarageReferenceGraphic icon = iconRect.gameObject.AddComponent<GarageReferenceGraphic>();
                    icon.symbol = GarageReferenceGraphic.Symbol.Check;
                    icon.color = GarageReferenceCyan;
                    icon.raycastTarget = false;
                    Text label = button.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.fontSize = 19;
                        label.resizeTextForBestFit = false;
                        label.rectTransform.anchoredPosition = new Vector2(18f, 0f);
                        label.rectTransform.sizeDelta = new Vector2(108f, 40f);
                    }
                }
            }
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

        private static int RuntimeTextMinSize(
            int fontSize)
        {
            if (fontSize <= 11)
            {
                return
                    Mathf.Max(
                        9,
                        fontSize - 2);
            }

            return
                Mathf.Max(
                    11,
                    Mathf.RoundToInt(
                        fontSize * 0.78f));
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

            fontSize = Mathf.Max(15, Mathf.RoundToInt(fontSize * 1.2f));
            bool useTrueBold =
                fontStyle == FontStyle.Bold &&
                boldFont != null;

            text.font =
                useTrueBold
                    ? boldFont
                    : font;
            text.fontSize = fontSize;
            text.fontStyle =
                useTrueBold
                    ? FontStyle.Normal
                    : fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.alignByGeometry = false;
            text.lineSpacing = 1f;

            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            text.verticalOverflow =
                VerticalWrapMode.Overflow;

            MotorCityTextLayout.Configure(text);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize =
                Mathf.Max(
                    10,
                    RuntimeTextMinSize(
                        fontSize));
            text.resizeTextMaxSize =
                fontSize;

            // The small enlarged minimap/action groups need glyphs rasterized
            // at the larger size, not just a stretched parent transform.
            // Counter-scale the text rect only; background art and button
            // hit targets keep their existing 1.15x layout.
            const float hudGroupScale = 1.15f;
            for (Transform owner = parent; owner != null; owner = owner.parent)
            {
                if ((owner.name == "Minimap" ||
                     owner.name == "HUD Utility Rail" ||
                     owner.name == "HUD Secondary Actions") &&
                    Mathf.Abs(owner.localScale.x - hudGroupScale) < 0.001f)
                {
                    rect.localScale = Vector3.one / hudGroupScale;
                    rect.sizeDelta *= hudGroupScale;
                    text.fontSize = Mathf.RoundToInt(text.fontSize * hudGroupScale);
                    text.resizeTextMinSize = Mathf.RoundToInt(
                        text.resizeTextMinSize * hudGroupScale);
                    text.resizeTextMaxSize = text.fontSize;
                    break;
                }
            }

            Shadow shadow =
                go.AddComponent<Shadow>();

            shadow.effectColor =
                new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance =
                new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;

            // Previously assigned by HudVisualPolish after the HUD was built.
            // Text and its shadow now have a single visual owner.
            float shadowAlpha = name switch
            {
                "Credits" => 1f,
                "Reputation" => 0.65f,
                "Active Objective" => 0.75f,
                "Character Source" => 0.55f,
                "Character Name" => 0.75f,
                "Character Mission Title" => 0.60f,
                "Character Line" => 0.70f,
                "Character Reward" => 0.70f,
                "Status Text" => 0.88f,
                "Speed" => 0.90f,
                "Minimap Target Label" => 0.86f,
                "Drift Score" => 0.85f,
                "Result Title" => 0.55f,
                "Result Headline" => 0.90f,
                "Result Details" => 0.65f,
                "Result Reward" => 0.90f,
                "Result Controls" => 0.55f,
                "Navigator Title" => 0.80f,
                "Navigator Index" => 0.50f,
                "Navigator Category" => 0.50f,
                "Navigator Selection" => 0.80f,
                "Navigator Description" => 0.50f,
                "Navigator Distance" => 0.50f,
                "Club Title" => 0.80f,
                "Club Name" => 0.80f,
                "Club Description" => 0.55f,
                "Club Weekly" => 0.75f,
                _ => 0.72f
            };
            Transform ancestor = parent;
            while (ancestor != null)
            {
                string group = ancestor.name;
                if (group == "Touch Utility Controls" ||
                    group == "Result Touch Controls" ||
                    group == "Navigator Touch Controls" ||
                    group == "Store Touch Controls" ||
                    group == "Club Touch Controls")
                {
                    shadowAlpha = 0.85f;
                    break;
                }
                ancestor = ancestor.parent;
            }
            shadow.effectColor = new Color(0f, 0f, 0f, shadowAlpha);
            shadow.effectDistance = new Vector2(1f, -2f);

            return text;
        }
        private sealed class SafeAreaRuntimeUpdater :
            MonoBehaviour
        {
            private RectTransform target;
            private Rect lastSafeArea;
            private Vector2Int lastScreen;

            public void Bind(
                RectTransform rect)
            {
                target =
                    rect;

                Refresh(
                    true);
            }

            private void Update()
            {
                Refresh(
                    false);
            }

            private void Refresh(
                bool force)
            {
                Rect currentSafeArea =
                    Screen.safeArea;

                Vector2Int currentScreen =
                    new(
                        Screen.width,
                        Screen.height);

                if (!force &&
                    currentSafeArea ==
                    lastSafeArea &&
                    currentScreen ==
                    lastScreen)
                {
                    return;
                }

                lastSafeArea =
                    currentSafeArea;

                lastScreen =
                    currentScreen;

                ApplySafeArea(
                    target);
            }
        }

    }
}
