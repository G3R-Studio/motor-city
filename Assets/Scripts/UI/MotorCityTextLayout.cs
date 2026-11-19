using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MotorCity.UI
{
    /// <summary>
    /// Common legacy-Text wrapping and bounds clipping. The layout owner,
    /// not this helper, chooses the nominal font size and rectangle.
    /// </summary>
    internal static class MotorCityTextLayout
    {
        public static void Configure(Text text)
        {
            if (text == null)
                return;

            int requestedFontSize = Mathf.Max(1, text.fontSize);
            // Keep caller-owned best-fit settings and explicit minimums.
            // A later general pass must not silently enlarge those minima.
            if (text.resizeTextForBestFit)
            {
                text.resizeTextMinSize = Mathf.Clamp(
                    text.resizeTextMinSize, 1, requestedFontSize);
                text.resizeTextMaxSize = Mathf.Max(
                    requestedFontSize, text.resizeTextMaxSize);
            }

            text.alignByGeometry = false;
            // Dynamic font atlases should interpolate glyph coverage rather than
            // sample nearest texels at fractional canvas scales.
            var atlas = text.font != null ? text.font.material.mainTexture : null;
            if (atlas != null) { atlas.filterMode = FilterMode.Bilinear; atlas.anisoLevel = 1; }

            text.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Truncate discards entire wrapped lines before TextClip has a
            // chance to clip them precisely, especially at fractional scale.
            // Generate the full text and let the mesh clip own the bounds.
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (text.GetComponent<MotorCityTextClip>() == null)
                text.gameObject.AddComponent<MotorCityTextClip>();
            if (Resources.Load<TMP_Settings>("TMP Settings") != null &&
                text.GetComponent<MotorCitySdfLabel>() == null)
                text.gameObject.AddComponent<MotorCitySdfLabel>();
        }
    }
    // Keep legacy Text as the layout/localization model while TMP owns
    // the visible glyphs. Existing Text references and layout stay valid.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    internal sealed class MotorCitySdfLabel : MonoBehaviour
    {
        private static readonly System.Collections.Generic.Dictionary<Font, TMP_FontAsset> fonts = new();
        private Text source;
        private TextMeshProUGUI label;
        private bool ready;
        public bool RendersGlyphs => ready && isActiveAndEnabled && label != null && label.enabled;

        private static TMP_FontAsset Resolve(Font font)
        {
            if (font == null || Resources.Load<TMP_Settings>("TMP Settings") == null) return null;
            if (fonts.TryGetValue(font, out var existing) && existing != null) return existing;
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            if (asset == null || asset.material == null || asset.material.shader == null ||
                !asset.material.shader.isSupported) return null;
            fonts[font] = asset;
            return asset;
        }

        private void LateUpdate()
        {
            if (source == null) source = GetComponent<Text>();
            if (source == null || source.font == null) return;
            if (!ready)
            {
                TMP_FontAsset asset;
                try { asset = Resolve(source.font); }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("SDF font unavailable: " + ex.Message);
                    enabled = false;
                    return;
                }
                if (asset == null) { enabled = false; return; }
                var child = new GameObject("SDF Glyphs", typeof(RectTransform));
                child.transform.SetParent(transform, false);
                var rect = (RectTransform)child.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                label = child.AddComponent<TextMeshProUGUI>();
                label.font = asset;
                label.raycastTarget = false;
                label.richText = source.supportRichText;
                label.overflowMode = TextOverflowModes.Masking;
                ready = true;
                source.SetVerticesDirty();
            }
            // Do not disable the source component: LayoutGroup and code rely
            // on its preferred dimensions. Cull only its rendered geometry.
            if (label.enabled != source.enabled)
            {
                label.enabled = source.enabled;
                source.SetVerticesDirty();
            }
            bool smallLabel = source.fontSize <= 24;
            var font = Resolve(smallLabel ? MotorCityTypography.Regular : source.font);
            if (font != null && label.font != font) label.font = font;
            if (label.text != source.text) label.text = source.text;
            if (label.color != source.color) label.color = source.color;
            float size = source.fontSize;
            if (!label.enableAutoSizing && label.fontSize != size) label.fontSize = size;
            label.characterSpacing = smallLabel ? 1.5f : 0f;
            label.fontStyle = (smallLabel ? FontStyle.Normal : source.fontStyle) switch
            {
                FontStyle.Bold => FontStyles.Bold,
                FontStyle.Italic => FontStyles.Italic,
                FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal
            };
            label.alignment = source.alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center
            };
            if (label.enableAutoSizing != source.resizeTextForBestFit)
                label.enableAutoSizing = source.resizeTextForBestFit;
            label.fontSizeMin = source.resizeTextMinSize;
            label.fontSizeMax = source.resizeTextMaxSize;
            label.textWrappingMode = source.horizontalOverflow == HorizontalWrapMode.Wrap
                ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.richText = source.supportRichText;
        }
        private void OnDisable()
        {
            if (source != null) source.SetVerticesDirty();
            if (label != null) label.enabled = false;
        }
    }
}
