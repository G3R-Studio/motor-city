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

        private Texture2D ResolveVillePanelTexture(
            string panelName)
        {
            if (uiThemeAssets == null)
                return null;

            return panelName switch
            {
                "Character Card" =>
                    uiThemeAssets.characterPanel,

                "Activity Status" =>
                    uiThemeAssets.statusPanel,

                "Drift HUD" =>
                    uiThemeAssets.driftPanel,

                "Navigation Target Strip" =>
                    uiThemeAssets.targetPanel,

                _ =>
                    uiThemeAssets.rectanglePanel
            };
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

        private void ApplyModalPanelTexture(
            RectTransform panel)
        {
            if (panel == null)
                return;

            ClearPanelChrome(
                panel);

            Texture2D texture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.modalPanel;

            if (texture == null)
                return;

            CreatePanelEdgeGlow(
                panel,
                texture,
                ResolvePanelGlowColor(
                    panel.name),
                "Ville Modal Glow");

            GameObject backgroundObject =
                new(
                    "Ville Modal Background",
                    typeof(RectTransform),
                    typeof(Image));

            backgroundObject.transform.SetParent(
                panel,
                false);

            backgroundObject.transform.SetAsFirstSibling();

            RectTransform rect =
                backgroundObject.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;
            rect.anchorMax =
                Vector2.one;
            rect.offsetMin =
                Vector2.zero;
            rect.offsetMax =
                Vector2.zero;

            Image image =
                backgroundObject.GetComponent<Image>();

            image.sprite =
                GetSlicedPanelSprite(
                    texture);
            image.type =
                Image.Type.Sliced;
            image.fillCenter =
                true;
            image.pixelsPerUnitMultiplier =
                1f;
            image.color =
                Color.white;
            image.raycastTarget =
                false;
        }

        private void ApplyVillePanelTexture(
            RectTransform panel,
            float alpha)
        {
            if (panel == null)
                return;

            ClearPanelChrome(
                panel);

            Texture2D texture =
                ResolveVillePanelTexture(
                    panel.name);

            if (texture == null)
                return;

            CreatePanelEdgeGlow(
                panel,
                texture,
                ResolvePanelGlowColor(
                    panel.name),
                "Ville Panel Glow");

            GameObject backgroundObject =
                new(
                    "Ville Panel Background",
                    typeof(RectTransform),
                    typeof(Image));

            backgroundObject.transform.SetParent(
                panel,
                false);

            backgroundObject.transform.SetAsFirstSibling();

            RectTransform rect =
                backgroundObject.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;
            rect.anchorMax =
                Vector2.one;
            rect.offsetMin =
                Vector2.zero;
            rect.offsetMax =
                Vector2.zero;

            Image image =
                backgroundObject.GetComponent<Image>();

            image.sprite =
                GetSlicedPanelSprite(
                    texture);
            image.type =
                Image.Type.Sliced;
            image.fillCenter =
                true;
            image.pixelsPerUnitMultiplier =
                1f;
            image.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    Mathf.Clamp01(alpha));
            image.raycastTarget =
                false;
        }

        private static Sprite GetSlicedPanelSprite(
            Texture2D texture)
        {
            if (texture == null)
                return null;

            if (slicedPanelSprites.TryGetValue(
                    texture,
                    out Sprite cached) &&
                cached != null)
            {
                return cached;
            }

            float minDimension =
                Mathf.Min(
                    texture.width,
                    texture.height);

            float border =
                Mathf.Clamp(
                    minDimension * 0.16f,
                    12f,
                    64f);

            Sprite sprite =
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
                    0u,
                    SpriteMeshType.FullRect,
                    new Vector4(
                        border,
                        border,
                        border,
                        border));

            sprite.name =
                texture.name +
                " (Runtime 9-slice)";

            slicedPanelSprites[texture] =
                sprite;

            return sprite;
        }

        private static Color ResolvePanelGlowColor(
            string panelName)
        {
            if (string.IsNullOrWhiteSpace(
                    panelName))
            {
                return new Color(
                    0.34f,
                    0.53f,
                    1f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Garage",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    0.64f,
                    0.42f,
                    1f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Store",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    0.30f,
                    0.58f,
                    1f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Club",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    0.24f,
                    0.82f,
                    1f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Drift",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    1f,
                    0.46f,
                    0.14f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Result",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    0.50f,
                    0.62f,
                    1f,
                    1f);
            }

            if (panelName.IndexOf(
                    "Pause",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(
                    0.38f,
                    0.52f,
                    0.92f,
                    1f);
            }

            return new Color(
                0.34f,
                0.53f,
                1f,
                1f);
        }

        private static void CreatePanelEdgeGlow(
            RectTransform panel,
            Texture2D texture,
            Color color,
            string objectName)
        {
            if (panel == null ||
                texture == null)
            {
                return;
            }

            Vector2 size =
                panel.rect.size;

            bool compactPanel =
                size.y <= 72f ||
                size.x <= 340f;

            float outerExpansion =
                compactPanel
                    ? 8f
                    : 14f;

            float innerExpansion =
                compactPanel
                    ? 4f
                    : 7f;

            float outerAlpha =
                compactPanel
                    ? 0.028f
                    : 0.075f;

            float innerAlpha =
                compactPanel
                    ? 0.065f
                    : 0.16f;

            CreatePanelGlowLayer(
                panel,
                texture,
                color,
                objectName + " Outer",
                outerExpansion,
                outerAlpha);

            CreatePanelGlowLayer(
                panel,
                texture,
                color,
                objectName + " Inner",
                innerExpansion,
                innerAlpha);
        }

        private static void CreatePanelGlowLayer(
            RectTransform panel,
            Texture2D texture,
            Color color,
            string objectName,
            float expansion,
            float alpha)
        {
            GameObject glowObject =
                new(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image));

            glowObject.transform.SetParent(
                panel,
                false);

            glowObject.transform.SetAsFirstSibling();

            RectTransform rect =
                glowObject.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;
            rect.anchorMax =
                Vector2.one;
            rect.offsetMin =
                new Vector2(
                    -expansion,
                    -expansion);
            rect.offsetMax =
                new Vector2(
                    expansion,
                    expansion);

            Image image =
                glowObject.GetComponent<Image>();

            image.sprite =
                GetSlicedPanelSprite(
                    texture);
            image.type =
                Image.Type.Sliced;
            image.fillCenter =
                true;
            image.pixelsPerUnitMultiplier =
                1f;

            image.color =
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    alpha);

            image.raycastTarget =
                false;
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
            text.alignByGeometry = true;
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
