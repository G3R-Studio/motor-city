using UnityEngine;

namespace MotorCity.UI
{
    internal static class MotorCityVolumeSlider
    {
        public static UnityEngine.UI.Slider Create(Transform parent, float value, System.Action<float> changed)
        {
            var root = new GameObject("Volume Slider", typeof(RectTransform), typeof(UnityEngine.UI.Slider), typeof(UnityEngine.UI.LayoutElement), typeof(UnityEngine.UI.Image));
            root.transform.SetParent(parent, false);
            var layout = root.GetComponent<UnityEngine.UI.LayoutElement>();
            layout.minWidth = 64f;
            layout.preferredWidth = 180f;
            layout.flexibleWidth = 1f;
            layout.preferredHeight = 36f;
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);
            RectTransform Part(string name, Transform owner, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(owner, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = min; rect.anchorMax = max;
                rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.color = color; image.raycastTarget = false;
                return rect;
            }
            var track = Part("Track", root.transform, new Vector2(0,.5f), new Vector2(1,.5f), new Vector2(9,-3), new Vector2(-9,3), new Color(.20f,.26f,.38f,1));
            var fill = Part("Fill", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.12f,.82f,1,1));
            var area = Part("Handle Area", root.transform, Vector2.zero, Vector2.one, new Vector2(9,0), new Vector2(-9,0), Color.clear);
            var handle = Part("Handle", area, new Vector2(0,.5f), new Vector2(0,.5f), new Vector2(-8,-10), new Vector2(8,10), new Color(.88f,.94f,1,1));
            var slider = root.GetComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(Mathf.RoundToInt(value * 100));
            slider.onValueChanged.AddListener(v => changed(v * .01f));
            return slider;
        }
    }

    public static class MotorCityTypography
    {
        private static Font regular;
        private static Font bold;
        public static Font Regular => regular != null ? regular :
            regular = Resources.Load<Font>("MotorCity/Fonts/RobotoCondensed-Regular")
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font Bold => bold != null ? bold :
            bold = Resources.Load<Font>("MotorCity/Fonts/RobotoCondensed-Bold") ?? Regular;
    }
}