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
                HorizontalWrapMode.Overflow;

            text.verticalOverflow =
                VerticalWrapMode.Truncate;

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

        private static Sprite GetModalButtonSprite(
            Texture2D texture)
        {
            if (texture == null)
                return null;

            if (modalButtonSprite != null &&
                modalButtonSpriteSource == texture)
            {
                return modalButtonSprite;
            }

            modalButtonSprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        texture.width,
                        texture.height),
                    new Vector2(
                        0.5f,
                        0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);

            modalButtonSprite.name =
                "Motor City Ville Modal Button";
            modalButtonSprite.hideFlags =
                HideFlags.DontSave;
            modalButtonSpriteSource =
                texture;

            return modalButtonSprite;
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
                label.verticalOverflow = VerticalWrapMode.Overflow;
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
                    if (current.name == "Garage Overlay" || current.name == "Touch Driving Controls")
                        { excluded = true; break; }
                if (excluded) continue;
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
                surface.color = new Color(.16f, .12f, .32f, .94f);
                surface.raycastTarget = true;
                button.targetGraphic = surface;
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
            rect.sizeDelta = new Vector2(size.x, Mathf.Max(size.y, fontSize * 1.45f));
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
                VerticalWrapMode.Truncate;

            // Keep one readable typography rule across the runtime HUD.
            // Dynamic text may shrink, but never all the way down to tiny
            // 8-9 px glyphs unless that size was requested explicitly.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize =
                RuntimeTextMinSize(
                    fontSize);
            text.resizeTextMaxSize =
                fontSize;

            Shadow shadow =
                go.AddComponent<Shadow>();

            shadow.effectColor =
                new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance =
                new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;

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
