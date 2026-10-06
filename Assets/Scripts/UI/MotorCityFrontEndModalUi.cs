using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class MotorCityFrontEndFlow
    {
        private const float FrontEndModalHorizontalPadding = 54f;
        private const float FrontEndModalRowSpacing = 16f;
        private const float FrontEndModalButtonSpacing = 14f;

        private readonly struct FrontEndModalWindow
        {
            public readonly RectTransform Panel;
            public readonly RectTransform Content;
            public readonly Text Title;

            public FrontEndModalWindow(
                RectTransform panel,
                RectTransform content,
                Text title)
            {
                Panel = panel;
                Content = content;
                Title = title;
            }
        }

        private FrontEndModalWindow CreateFrontEndModalWindow(
            Transform parent,
            string objectName,
            string title,
            Vector2 size,
            float headerHeight = 108f)
        {
            RectTransform panel =
                CreateFrontEndPanel(
                    parent,
                    objectName,
                    Vector2.zero,
                    size);

            Text titleText =
                CreateText(
                    panel,
                    title,
                    44,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.zero,
                    new Vector2(0.5f, 1f));

            titleText.name =
                objectName + " Title";

            titleText.color =
                new Color(
                    0.90f,
                    0.95f,
                    1f,
                    1f);

            RectTransform titleRect =
                titleText.rectTransform;

            titleRect.anchorMin =
                new Vector2(0f, 1f);

            titleRect.anchorMax =
                new Vector2(1f, 1f);

            titleRect.pivot =
                new Vector2(0.5f, 1f);

            titleRect.anchoredPosition =
                new Vector2(0f, -24f);

            titleRect.sizeDelta =
                new Vector2(
                    -FrontEndModalHorizontalPadding * 2f,
                    headerHeight - 34f);

            RectTransform content =
                new GameObject(
                    objectName + " Content",
                    typeof(RectTransform))
                    .GetComponent<RectTransform>();

            content.transform.SetParent(
                panel,
                false);

            content.anchorMin =
                Vector2.zero;

            content.anchorMax =
                Vector2.one;

            content.offsetMin =
                new Vector2(
                    FrontEndModalHorizontalPadding,
                    38f);

            content.offsetMax =
                new Vector2(
                    -FrontEndModalHorizontalPadding,
                    -headerHeight);

            VerticalLayoutGroup layout =
                content.gameObject.AddComponent<
                    VerticalLayoutGroup>();

            layout.padding =
                new RectOffset();

            layout.spacing =
                FrontEndModalRowSpacing;

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
                new FrontEndModalWindow(
                    panel,
                    content,
                    titleText);
        }

        private RectTransform CreateFrontEndActionRow(
            Transform parent,
            string objectName,
            float height,
            float spacing =
                FrontEndModalButtonSpacing)
        {
            GameObject rowObject =
                new(
                    objectName,
                    typeof(RectTransform),
                    typeof(LayoutElement));

            rowObject.transform.SetParent(
                parent,
                false);

            RectTransform row =
                rowObject.GetComponent<RectTransform>();

            LayoutElement element =
                rowObject.GetComponent<LayoutElement>();

            element.minHeight =
                height;

            element.preferredHeight =
                height;

            HorizontalLayoutGroup layout =
                rowObject.AddComponent<
                    HorizontalLayoutGroup>();

            layout.padding =
                new RectOffset();

            layout.spacing =
                spacing;

            layout.childAlignment =
                TextAnchor.MiddleCenter;

            layout.childControlWidth =
                true;

            layout.childControlHeight =
                true;

            layout.childForceExpandWidth =
                false;

            layout.childForceExpandHeight =
                true;

            return row;
        }

        private Button CreateFrontEndLayoutButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            float preferredWidth = -1f,
            float height = 58f)
        {
            Button button =
                CreateButton(
                    parent,
                    label,
                    Vector2.zero,
                    new Vector2(
                        preferredWidth > 0f
                            ? preferredWidth
                            : 320f,
                        height),
                    action,
                    new Vector2(0.5f, 0.5f));

            LayoutElement element =
                button.gameObject.AddComponent<
                    LayoutElement>();

            element.minHeight =
                height;

            element.preferredHeight =
                height;

            if (preferredWidth > 0f)
            {
                element.minWidth =
                    preferredWidth;

                element.preferredWidth =
                    preferredWidth;

                element.flexibleWidth =
                    0f;
            }
            else
            {
                element.flexibleWidth =
                    1f;
            }

            return button;
        }

        private Text CreateFrontEndLayoutText(
            Transform parent,
            string value,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            float height,
            Color color)
        {
            Text text =
                CreateText(
                    parent,
                    value,
                    fontSize,
                    style,
                    alignment,
                    Vector2.zero,
                    Vector2.zero,
                    new Vector2(0.5f, 0.5f));

            RectTransform rect =
                text.rectTransform;

            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                Vector2.one;

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                Vector2.zero;

            LayoutElement element =
                text.gameObject.AddComponent<
                    LayoutElement>();

            element.minHeight =
                height;

            element.preferredHeight =
                height;

            text.color =
                color;

            return text;
        }

        private void CreateFrontEndSettingsLayoutRow(
            Transform parent,
            string label,
            out Text valueText,
            UnityEngine.Events.UnityAction minusAction,
            UnityEngine.Events.UnityAction plusAction,
            UnityEngine.Events.UnityAction wideAction)
        {
            GameObject rowObject =
                new(
                    label + " Row",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));

            rowObject.transform.SetParent(
                parent,
                false);

            RectTransform row =
                rowObject.GetComponent<RectTransform>();

            LayoutElement rowLayout =
                rowObject.GetComponent<
                    LayoutElement>();

            rowLayout.minHeight =
                88f;

            rowLayout.preferredHeight =
                88f;

            Image background =
                rowObject.GetComponent<Image>();

            background.color =
                Color.clear;

            background.raycastTarget =
                false;

            FrontEndSurface(
                row);

            Text labelText =
                CreateText(
                    row,
                    label,
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero);

            labelText.name =
                "Label";

            labelText.color =
                new Color(
                    0.46f,
                    0.82f,
                    1f,
                    1f);

            RectTransform labelRect =
                labelText.rectTransform;

            labelRect.anchorMin =
                new Vector2(0f, 0f);

            labelRect.anchorMax =
                new Vector2(0.28f, 1f);

            labelRect.offsetMin =
                new Vector2(20f, 10f);

            labelRect.offsetMax =
                new Vector2(-8f, -10f);

            ConfigureFrontEndTextFit(
                labelText,
                14);

            valueText =
                CreateText(
                    row,
                    string.Empty,
                    24,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero);

            valueText.name =
                "Value";

            valueText.color =
                new Color(
                    0.95f,
                    0.96f,
                    1f,
                    1f);

            RectTransform valueRect =
                valueText.rectTransform;

            valueRect.anchorMin =
                new Vector2(0.28f, 0f);

            valueRect.anchorMax =
                new Vector2(0.50f, 1f);

            valueRect.offsetMin =
                new Vector2(4f, 10f);

            valueRect.offsetMax =
                new Vector2(-4f, -10f);

            ConfigureFrontEndTextFit(
                valueText,
                16);

            RectTransform controls =
                new GameObject(
                    "Controls",
                    typeof(RectTransform))
                    .GetComponent<RectTransform>();

            controls.transform.SetParent(
                row,
                false);

            controls.anchorMin =
                new Vector2(0.50f, 0f);

            controls.anchorMax =
                new Vector2(1f, 1f);

            controls.offsetMin =
                new Vector2(8f, 14f);

            controls.offsetMax =
                new Vector2(-16f, -14f);

            HorizontalLayoutGroup layout =
                controls.gameObject.AddComponent<
                    HorizontalLayoutGroup>();

            layout.padding =
                new RectOffset();

            layout.spacing =
                8f;

            layout.childAlignment =
                TextAnchor.MiddleRight;

            layout.childControlWidth =
                true;

            layout.childControlHeight =
                true;

            layout.childForceExpandWidth =
                false;

            layout.childForceExpandHeight =
                true;

            if (minusAction != null)
            {
                CreateFrontEndLayoutButton(
                    controls,
                    "−",
                    minusAction,
                    72f,
                    50f);
            }

            if (plusAction != null)
            {
                CreateFrontEndLayoutButton(
                    controls,
                    "+",
                    plusAction,
                    72f,
                    50f);
            }

            if (wideAction != null)
            {
                string actionLabel =
                    label ==
                    (IsRussian()
                        ? "ЯЗЫК"
                        : "LANGUAGE")
                        ? (IsRussian()
                            ? "СМЕНИТЬ"
                            : "CHANGE")
                        : (IsRussian()
                            ? "ВКЛ / ВЫКЛ"
                            : "ON / OFF");

                CreateFrontEndLayoutButton(
                    controls,
                    actionLabel,
                    wideAction,
                    210f,
                    50f);
            }
        }
    }
}
