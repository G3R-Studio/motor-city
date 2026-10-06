using MotorCity.Input;
using MotorCity.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    /// <summary>
    /// Shared layout primitives for modal HUD windows.
    ///
    /// These helpers deliberately live in the PrototypeHud partial class so
    /// every runtime-created window reuses the same fonts, theme textures,
    /// localization tracking and button feedback instead of duplicating UI
    /// construction code and hard-coded coordinates.
    /// </summary>
    public sealed partial class PrototypeHud
    {
        private const float ModalHorizontalPadding = 45f;
        private const float ModalHeaderHeight = 70f;
        private const float ModalBottomPadding = 24f;
        private const float ModalRowHeight = 64f;
        private const float ModalRowSpacing = 14f;
        private const float ModalActionHeight = 50f;
        private const float ModalButtonSpacing = 14f;

        private readonly struct MotorCityModalWindow
        {
            public readonly RectTransform Panel;
            public readonly RectTransform Content;
            public readonly Text Title;

            public MotorCityModalWindow(
                RectTransform panel,
                RectTransform content,
                Text title)
            {
                Panel = panel;
                Content = content;
                Title = title;
            }
        }

        private readonly struct MotorCitySettingsRow
        {
            public readonly RectTransform Root;
            public readonly Text Label;
            public readonly Text Value;
            public readonly RectTransform Controls;

            public MotorCitySettingsRow(
                RectTransform root,
                Text label,
                Text value,
                RectTransform controls)
            {
                Root = root;
                Label = label;
                Value = value;
                Controls = controls;
            }
        }

        private MotorCityModalWindow CreateModalWindow(
            Transform parent,
            string objectName,
            string titleLocalizationKey,
            Vector2 size)
        {
            RectTransform panel =
                CreatePanel(
                    parent,
                    objectName,
                    Vector2.zero,
                    size,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Color.clear);

            ApplyModalPanelTexture(
                panel);

            Text title =
                CreateText(
                    panel,
                    objectName + " Title",
                    UiWindowTitleFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.zero,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    TextColor);

            RectTransform titleRect =
                title.rectTransform;

            titleRect.anchorMin =
                new Vector2(0f, 1f);

            titleRect.anchorMax =
                new Vector2(1f, 1f);

            titleRect.pivot =
                new Vector2(0.5f, 1f);

            titleRect.anchoredPosition =
                new Vector2(0f, -18f);

            titleRect.sizeDelta =
                new Vector2(
                    -ModalHorizontalPadding * 2f,
                    42f);

            title.text =
                MotorCityLocalization.Text(
                    titleLocalizationKey);

            touchLocalizedLabels.Add(
                new TouchLocalizedLabel(
                    title,
                    titleLocalizationKey));

            RectTransform content =
                GarageObject(
                    panel,
                    objectName + " Content");

            content.anchorMin =
                Vector2.zero;

            content.anchorMax =
                Vector2.one;

            content.offsetMin =
                new Vector2(
                    ModalHorizontalPadding,
                    ModalBottomPadding);

            content.offsetMax =
                new Vector2(
                    -ModalHorizontalPadding,
                    -ModalHeaderHeight);

            VerticalLayoutGroup layout =
                content.gameObject.AddComponent<
                    VerticalLayoutGroup>();

            layout.padding =
                new RectOffset();

            layout.spacing =
                ModalRowSpacing;

            layout.childAlignment =
                TextAnchor.UpperCenter;

            layout.childControlWidth =
                true;

            layout.childControlHeight =
                true;

            layout.childForceExpandWidth =
                true;

            layout.childForceExpandHeight =
                false;

            return
                new MotorCityModalWindow(
                    panel,
                    content,
                    title);
        }

        private MotorCitySettingsRow CreateSettingsRow(
            Transform parent,
            string objectName,
            string labelLocalizationKey)
        {
            RectTransform row =
                CreatePanel(
                    parent,
                    objectName,
                    Vector2.zero,
                    new Vector2(0f, ModalRowHeight),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(
                        PanelSoftColor.r,
                        PanelSoftColor.g,
                        PanelSoftColor.b,
                        0.72f));

            LayoutElement rowLayout =
                row.gameObject.AddComponent<
                    LayoutElement>();

            rowLayout.preferredHeight =
                ModalRowHeight;

            rowLayout.minHeight =
                ModalRowHeight;

            ApplyReferenceHudSurface(
                row,
                0.88f);

            Text label =
                CreateText(
                    row,
                    objectName + " Label",
                    UiSectionLabelFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    new Color(
                        0.46f,
                        0.82f,
                        1f,
                        1f));

            RectTransform labelRect =
                label.rectTransform;

            labelRect.anchorMin =
                new Vector2(0f, 0f);

            labelRect.anchorMax =
                new Vector2(0.30f, 1f);

            labelRect.offsetMin =
                new Vector2(18f, 8f);

            labelRect.offsetMax =
                new Vector2(-6f, -8f);

            label.text =
                MotorCityLocalization.Text(
                    labelLocalizationKey);

            touchLocalizedLabels.Add(
                new TouchLocalizedLabel(
                    label,
                    labelLocalizationKey));

            Text value =
                CreateText(
                    row,
                    objectName + " Value",
                    UiValueFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    TextColor);

            RectTransform valueRect =
                value.rectTransform;

            valueRect.anchorMin =
                new Vector2(0.30f, 0f);

            valueRect.anchorMax =
                new Vector2(0.53f, 1f);

            valueRect.offsetMin =
                new Vector2(2f, 8f);

            valueRect.offsetMax =
                new Vector2(-2f, -8f);

            RectTransform controls =
                GarageObject(
                    row,
                    objectName + " Controls");

            controls.anchorMin =
                new Vector2(0.53f, 0f);

            controls.anchorMax =
                new Vector2(1f, 1f);

            controls.offsetMin =
                new Vector2(4f, 8f);

            controls.offsetMax =
                new Vector2(-10f, -8f);

            HorizontalLayoutGroup controlsLayout =
                controls.gameObject.AddComponent<
                    HorizontalLayoutGroup>();

            controlsLayout.padding =
                new RectOffset();

            controlsLayout.spacing =
                6f;

            controlsLayout.childAlignment =
                TextAnchor.MiddleRight;

            controlsLayout.childControlWidth =
                true;

            controlsLayout.childControlHeight =
                true;

            controlsLayout.childForceExpandWidth =
                false;

            controlsLayout.childForceExpandHeight =
                true;

            return
                new MotorCitySettingsRow(
                    row,
                    label,
                    value,
                    controls);
        }

        private RectTransform CreateButtonRow(
            Transform parent,
            string objectName)
        {
            RectTransform row =
                GarageObject(
                    parent,
                    objectName);

            LayoutElement layoutElement =
                row.gameObject.AddComponent<
                    LayoutElement>();

            layoutElement.preferredHeight =
                ModalActionHeight;

            layoutElement.minHeight =
                ModalActionHeight;

            HorizontalLayoutGroup layout =
                row.gameObject.AddComponent<
                    HorizontalLayoutGroup>();

            layout.padding =
                new RectOffset();

            layout.spacing =
                ModalButtonSpacing;

            layout.childAlignment =
                TextAnchor.MiddleCenter;

            layout.childControlWidth =
                true;

            layout.childControlHeight =
                true;

            layout.childForceExpandWidth =
                true;

            layout.childForceExpandHeight =
                true;

            return row;
        }

        private Button CreatePrimaryButton(
            Transform parent,
            string objectName,
            string localizationKey,
            UnityEngine.Events.UnityAction action,
            float preferredWidth = -1f,
            bool compact = false,
            MotorCityInputAction? semanticAction = null)
        {
            GameObject buttonObject =
                new(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement));

            buttonObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                buttonObject.GetComponent<
                    RectTransform>();

            Image image =
                buttonObject.GetComponent<Image>();

            Sprite buttonSprite =
                GetModalButtonSprite(
                    uiThemeAssets == null
                        ? null
                        : uiThemeAssets.modalButton);

            if (buttonSprite != null)
            {
                image.sprite =
                    buttonSprite;

                image.type =
                    Image.Type.Simple;

                image.preserveAspect =
                    false;

                image.color =
                    Color.white;
            }
            else
            {
                image.color =
                    new Color(
                        0.075f,
                        0.07f,
                        0.12f,
                        0.96f);
            }

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                Color.white;

            colors.highlightedColor =
                new Color(
                    0.96f,
                    0.96f,
                    1f,
                    1f);

            colors.pressedColor =
                new Color(
                    0.82f,
                    0.80f,
                    0.92f,
                    1f);

            colors.selectedColor =
                colors.highlightedColor;

            button.colors =
                colors;

            buttonObject.AddComponent<
                UiButtonFeedback>();

            button.onClick.AddListener(
                action);

            LayoutElement layoutElement =
                buttonObject.GetComponent<
                    LayoutElement>();

            if (preferredWidth > 0f)
            {
                layoutElement.preferredWidth =
                    preferredWidth;

                layoutElement.minWidth =
                    preferredWidth;

                layoutElement.flexibleWidth =
                    0f;
            }
            else
            {
                layoutElement.flexibleWidth =
                    1f;
            }

            bool hasSemanticIcon =
                semanticAction.HasValue;

            if (hasSemanticIcon)
            {
                RectTransform iconRect =
                    GarageObject(
                        rect,
                        "Action Icon");

                iconRect.anchorMin =
                    iconRect.anchorMax =
                    iconRect.pivot =
                        new Vector2(
                            0f,
                            0.5f);

                iconRect.anchoredPosition =
                    new Vector2(
                        14f,
                        0f);

                iconRect.sizeDelta =
                    new Vector2(
                        24f,
                        24f);

                GarageReferenceGraphic icon =
                    iconRect.gameObject.AddComponent<
                        GarageReferenceGraphic>();

                if (!MotorCityButtonVisuals.TryForButton(
                        localizationKey + " " +
                        objectName,
                        semanticAction.Value,
                        out GarageReferenceGraphic.Symbol iconSymbol,
                        out Color iconColor))
                {
                    iconSymbol =
                        GarageReferenceGraphic.Symbol.Check;

                    iconColor =
                        TextColor;
                }

                icon.symbol =
                    iconSymbol;

                icon.color =
                    iconColor;

                icon.raycastTarget =
                    false;
            }

            Text label =
                CreateText(
                    rect,
                    "Label",
                    compact
                        ? UiAdjustButtonFontSize
                        : UiButtonFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    TextColor);

            RectTransform labelRect =
                label.rectTransform;

            labelRect.anchorMin =
                Vector2.zero;

            labelRect.anchorMax =
                Vector2.one;

            labelRect.offsetMin =
                new Vector2(
                    hasSemanticIcon
                        ? 40f
                        : 4f,
                    3f);

            labelRect.offsetMax =
                new Vector2(-4f, -3f);

            label.text =
                MotorCityLocalization.Text(
                    localizationKey);

            MakeButtonTextCrisp(
                label);

            touchLocalizedLabels.Add(
                new TouchLocalizedLabel(
                    label,
                    localizationKey));

            return button;
        }
    }
}
