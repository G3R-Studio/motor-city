using System;
namespace UnityEngine
{
    public static class Mathf
    {
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int FloorToInt(float value) => (int)Math.Floor(value);
    }
    public struct Rect { public float height; }
    public class RectTransform { public Rect rect; }
}
namespace UnityEngine.UI
{
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public class Text
    {
        public bool resizeTextForBestFit = true;
        public bool alignByGeometry = true;
        public int fontSize;
        public string text;
        public HorizontalWrapMode horizontalOverflow;
        public VerticalWrapMode verticalOverflow;
        public UnityEngine.RectTransform rectTransform = new UnityEngine.RectTransform();
    }
}
public static class TextLayoutChecks
{
    public static void Run()
    {
        MotorCity.UI.MotorCityTextLayout.Configure(null);
        var text = new UnityEngine.UI.Text { fontSize = 24 };
        text.rectTransform.rect = new UnityEngine.Rect { height = 28 };
        MotorCity.UI.MotorCityTextLayout.Configure(text);
        if (text.fontSize != 21 || text.resizeTextForBestFit || text.alignByGeometry)
            throw new Exception("Tight labels must reserve line height and use a fixed font.");
        if (text.horizontalOverflow != UnityEngine.UI.HorizontalWrapMode.Wrap ||
            text.verticalOverflow != UnityEngine.UI.VerticalWrapMode.Truncate)
            throw new Exception("Text must stay within its allocated cell.");
        foreach (string value in new[] { "0", "999999", "A long changed value", "" })
        {
            text.text = value;
            MotorCity.UI.MotorCityTextLayout.Configure(text);
            if (text.fontSize != 21 || text.rectTransform.rect.height != 28)
                throw new Exception("Changing content must not move cells or resize fonts.");
        }
        text.fontSize = 24;
        text.rectTransform.rect = new UnityEngine.Rect { height = 88 };
        MotorCity.UI.MotorCityTextLayout.Configure(text);
        if (text.fontSize != 24) throw new Exception("Paragraph cells must retain their designed readable font.");
    }
}
