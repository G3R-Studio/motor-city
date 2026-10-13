using UnityEngine;

namespace MotorCity.UI
{
    // Four lightweight, reusable line-art icons. Generated in memory without
    // external fonts, asset imports, networking or WebGL-only dependencies.
    internal static class MotorCityLoadingIcons
    {
        private static readonly Sprite[] Cache = new Sprite[4];

        internal static Sprite Get(int icon)
        {
            icon = Mathf.Clamp(icon, 0, 3);
            if (Cache[icon] != null) return Cache[icon];

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Motor City Loading Icon Texture " + icon;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
            texture.SetPixels32(pixels);

            switch (icon)
            {
                case 0: // Checked racing flag with mast
                    Line(texture, 12, 7, 12, 55, 4);
                    Line(texture, 15, 49, 50, 45, 3);
                    Line(texture, 50, 45, 43, 19, 3);
                    Line(texture, 43, 19, 15, 24, 3);
                    for (int row = 0; row < 3; row++)
                        for (int col = 0; col < 3; col++)
                            if ((row + col) % 2 == 0)
                                Box(texture, 17 + col * 9, 25 + row * 7, 8, 6);
                    break;
                case 1: // Workshop wrench
                    Line(texture, 17, 13, 42, 41, 9);
                    Ring(texture, 15, 14, 9, 4);
                    Line(texture, 42, 41, 50, 50, 7);
                    Line(texture, 39, 52, 51, 39, 4);
                    break;
                case 2: // City map pin
                    Ring(texture, 32, 39, 18, 5);
                    Ring(texture, 32, 39, 6, 4);
                    Line(texture, 17, 30, 32, 5, 5);
                    Line(texture, 47, 30, 32, 5, 5);
                    break;
                default: // Lightbulb hint
                    Ring(texture, 32, 40, 17, 4);
                    Line(texture, 23, 28, 27, 19, 5);
                    Line(texture, 41, 28, 37, 19, 5);
                    Line(texture, 27, 19, 37, 19, 5);
                    Line(texture, 26, 12, 38, 12, 4);
                    Line(texture, 29, 7, 35, 7, 4);
                    break;
            }
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "Motor City Loading Icon " + icon;
            Cache[icon] = sprite;
            return sprite;
        }

        private static void Box(Texture2D texture, int x, int y, int w, int h)
        {
            for (int py = y; py < y + h; py++)
                for (int px = x; px < x + w; px++)
                    Plot(texture, px, py);
        }

        private static void Ring(Texture2D texture, int x, int y, int radius, int thick)
        {
            for (int py = y - radius - 1; py <= y + radius + 1; py++)
                for (int px = x - radius - 1; px <= x + radius + 1; px++)
                {
                    float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                    if (d >= radius - thick && d <= radius) Plot(texture, px, py);
                }
        }

        private static void Line(Texture2D texture, int x0, int y0, int x1, int y1, int thick)
        {
            Vector2 a = new Vector2(x0, y0);
            Vector2 b = new Vector2(x1, y1);
            Vector2 ab = b - a;
            float sq = ab.sqrMagnitude;
            int minX = Mathf.Min(x0, x1) - thick;
            int maxX = Mathf.Max(x0, x1) + thick;
            int minY = Mathf.Min(y0, y1) - thick;
            int maxY = Mathf.Max(y0, y1) + thick;
            for (int py = minY; py <= maxY; py++)
                for (int px = minX; px <= maxX; px++)
                {
                    Vector2 p = new Vector2(px, py);
                    float t = sq < 0.01f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / sq);
                    if ((p - (a + t * ab)).sqrMagnitude <= thick * thick * 0.25f)
                        Plot(texture, px, py);
                }
        }

        private static void Plot(Texture2D texture, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < texture.width && y < texture.height)
                texture.SetPixel(x, y, Color.white);
        }
    }
}
