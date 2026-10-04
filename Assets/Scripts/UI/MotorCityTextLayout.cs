using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    /// <summary>Fixed typography in canvas units, independent of the current value or screen scale.</summary>
    internal static class MotorCityTextLayout
    {
        public static void Configure(Text text)
        {
            if (text == null) return;
            text.resizeTextForBestFit = false;
            text.alignByGeometry = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            // Reserve line-height headroom instead of enlarging the cell into its neighbours.
            float height = text.rectTransform.rect.height;
            if (height > 0f)
                text.fontSize = Mathf.Min(text.fontSize, Mathf.Max(1, Mathf.FloorToInt(height / 1.3f)));
        }
    }
}
