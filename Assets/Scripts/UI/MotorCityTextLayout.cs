using UnityEngine;
using UnityEngine.UI;

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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Truncate discards entire wrapped lines before TextClip has a
            // chance to clip them precisely, especially at fractional scale.
            // Generate the full text and let the mesh clip own the bounds.
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (text.GetComponent<MotorCityTextClip>() == null)
                text.gameObject.AddComponent<MotorCityTextClip>();
        }
    }
}
