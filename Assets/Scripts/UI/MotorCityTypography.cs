using UnityEngine;

namespace MotorCity.UI
{
    internal static class MotorCityVolumeSlider
    {
        private static Sprite circle;
        private static Sprite Circle()
        {
            if (circle != null) return circle;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Volume Handle Circle";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = new Vector2(x + .5f - size * .5f, y + .5f - size * .5f).magnitude;
                    pixels[y * size + x] = new Color32(255,255,255,(byte)Mathf.RoundToInt(255 * Mathf.Clamp01(size * .5f - 1f - distance)));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            circle = Sprite.Create(texture, new Rect(0,0,size,size), new Vector2(.5f,.5f));
            return circle;
        }

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
            var track = Part("Track", root.transform, new Vector2(0,.5f), new Vector2(1,.5f), new Vector2(11,-3), new Vector2(-11,3), new Color(.20f,.26f,.38f,1));
            var fill = Part("Fill", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.12f,.82f,1,1));
            var area = Part("Handle Area", root.transform, Vector2.zero, Vector2.one, new Vector2(11,0), new Vector2(-11,0), Color.clear);
            var handle = Part("Handle", area, new Vector2(0,.5f), new Vector2(0,.5f), new Vector2(-8,-10), new Vector2(8,10), new Color(.88f,.94f,1,1));
            handle.anchorMin = handle.anchorMax = new Vector2(0f,.5f);
            handle.sizeDelta = new Vector2(22f,22f);
            handle.anchoredPosition = Vector2.zero;
            var handleImage = handle.GetComponent<UnityEngine.UI.Image>();
            handleImage.sprite = Circle();
            handleImage.preserveAspect = true;
            // Slider drives both x anchors; keep y fixed at the centre.
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