using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class CitySchematicMap
    {
        private const int TextureSize =
            512;

        private static readonly Color Background =
            new(
                0.86f,
                0.88f,
                0.87f,
                1f);

        private static readonly Color Road =
            new(
                0.96f,
                0.965f,
                0.96f,
                1f);

        private static readonly Color RoadEdge =
            new(
                0.72f,
                0.75f,
                0.74f,
                1f);

        private static readonly Color Building =
            new(
                0.73f,
                0.76f,
                0.75f,
                1f);

        private static readonly Color BuildingEdge =
            new(
                0.57f,
                0.61f,
                0.60f,
                1f);

        private readonly List<MapShape> shapes =
            new();

        public Texture2D Texture { get; private set; }

        public Bounds WorldBounds { get; private set; }

        public bool IsValid =>
            Texture != null &&
            WorldBounds.size.x > 1f &&
            WorldBounds.size.z > 1f;

        public bool Build()
        {
            GameObject cityRoot =
                GameObject.Find(
                    "MotorCity_FCGCity") ??
                GameObject.Find(
                    "City-Maker");

            if (cityRoot == null)
                return false;

            Renderer[] renderers =
                cityRoot.GetComponentsInChildren<Renderer>(
                    true);

            if (renderers == null ||
                renderers.Length == 0)
            {
                return false;
            }

            shapes.Clear();

            bool boundsInitialized =
                false;

            Bounds mapBounds =
                default;

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    ShouldIgnore(
                        renderer.transform))
                {
                    continue;
                }

                Bounds bounds =
                    renderer.bounds;

                ShapeType type =
                    Classify(
                        renderer,
                        bounds);

                if (type ==
                    ShapeType.Ignore)
                {
                    continue;
                }

                shapes.Add(
                    new MapShape
                    {
                        Bounds = bounds,
                        Type = type
                    });

                Bounds flat =
                    new(
                        new Vector3(
                            bounds.center.x,
                            0f,
                            bounds.center.z),
                        new Vector3(
                            Mathf.Max(
                                0.5f,
                                bounds.size.x),
                            1f,
                            Mathf.Max(
                                0.5f,
                                bounds.size.z)));

                if (!boundsInitialized)
                {
                    mapBounds =
                        flat;

                    boundsInitialized =
                        true;
                }
                else
                {
                    mapBounds.Encapsulate(
                        flat);
                }
            }

            if (!boundsInitialized ||
                shapes.Count == 0)
            {
                return false;
            }

            Vector3 size =
                mapBounds.size;

            const float padding =
                24f;

            size.x +=
                padding * 2f;

            size.z +=
                padding * 2f;

            mapBounds.size =
                size;

            WorldBounds =
                mapBounds;

            Texture =
                new Texture2D(
                    TextureSize,
                    TextureSize,
                    TextureFormat.RGBA32,
                    false,
                    true)
                {
                    name =
                        "MotorCity_SchematicMap",
                    filterMode =
                        FilterMode.Bilinear,
                    wrapMode =
                        TextureWrapMode.Clamp
                };

            Color[] pixels =
                new Color[
                    TextureSize *
                    TextureSize];

            for (int i = 0;
                 i < pixels.Length;
                 i++)
            {
                pixels[i] =
                    Background;
            }

            foreach (MapShape shape in
                     shapes)
            {
                DrawShape(
                    pixels,
                    shape);
            }

            Texture.SetPixels(
                pixels);

            Texture.Apply(
                false,
                true);

            return true;
        }

        public Rect UvWindow(
            Vector3 worldPosition,
            float worldRadius)
        {
            if (!IsValid)
            {
                return
                    new Rect(
                        0f,
                        0f,
                        1f,
                        1f);
            }

            Vector2 center =
                WorldToUv(
                    worldPosition);

            float width =
                Mathf.Clamp01(
                    worldRadius * 2f /
                    WorldBounds.size.x);

            float height =
                Mathf.Clamp01(
                    worldRadius * 2f /
                    WorldBounds.size.z);

            float x =
                Mathf.Clamp(
                    center.x -
                    width * 0.5f,
                    0f,
                    Mathf.Max(
                        0f,
                        1f - width));

            float y =
                Mathf.Clamp(
                    center.y -
                    height * 0.5f,
                    0f,
                    Mathf.Max(
                        0f,
                        1f - height));

            return
                new Rect(
                    x,
                    y,
                    width,
                    height);
        }

        private Vector2 WorldToUv(
            Vector3 position)
        {
            Bounds bounds =
                WorldBounds;

            float u =
                Mathf.InverseLerp(
                    bounds.min.x,
                    bounds.max.x,
                    position.x);

            float v =
                Mathf.InverseLerp(
                    bounds.min.z,
                    bounds.max.z,
                    position.z);

            return
                new Vector2(
                    u,
                    v);
        }

        private void DrawShape(
            Color[] pixels,
            MapShape shape)
        {
            Bounds bounds =
                shape.Bounds;

            Vector2 min =
                WorldToUv(
                    new Vector3(
                        bounds.min.x,
                        0f,
                        bounds.min.z));

            Vector2 max =
                WorldToUv(
                    new Vector3(
                        bounds.max.x,
                        0f,
                        bounds.max.z));

            int x0 =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        min.x *
                        (TextureSize - 1)),
                    0,
                    TextureSize - 1);

            int y0 =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        min.y *
                        (TextureSize - 1)),
                    0,
                    TextureSize - 1);

            int x1 =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        max.x *
                        (TextureSize - 1)),
                    0,
                    TextureSize - 1);

            int y1 =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        max.y *
                        (TextureSize - 1)),
                    0,
                    TextureSize - 1);

            if (x1 <= x0 ||
                y1 <= y0)
            {
                return;
            }

            Color fill =
                shape.Type ==
                ShapeType.Road
                    ? Road
                    : Building;

            Color edge =
                shape.Type ==
                ShapeType.Road
                    ? RoadEdge
                    : BuildingEdge;

            int border =
                shape.Type ==
                ShapeType.Road
                    ? 1
                    : 2;

            for (int y = y0;
                 y <= y1;
                 y++)
            {
                int row =
                    y *
                    TextureSize;

                for (int x = x0;
                     x <= x1;
                     x++)
                {
                    bool isEdge =
                        x - x0 < border ||
                        x1 - x < border ||
                        y - y0 < border ||
                        y1 - y < border;

                    pixels[row + x] =
                        isEdge
                            ? edge
                            : fill;
                }
            }
        }

        private static ShapeType Classify(
            Renderer renderer,
            Bounds bounds)
        {
            string path =
                HierarchyName(
                    renderer.transform);

            if (ContainsAny(
                    path,
                    "road",
                    "street",
                    "highway",
                    "avenue",
                    "intersection",
                    "crossroad",
                    "sidewalk",
                    "pavement",
                    "asphalt",
                    "parking"))
            {
                return
                    ShapeType.Road;
            }

            if (ContainsAny(
                    path,
                    "building",
                    "house",
                    "shop",
                    "store",
                    "office",
                    "garage",
                    "warehouse",
                    "apartment",
                    "hotel",
                    "wall"))
            {
                return
                    ShapeType.Building;
            }

            float horizontal =
                Mathf.Max(
                    bounds.size.x,
                    bounds.size.z);

            float vertical =
                bounds.size.y;

            float smallerHorizontal =
                Mathf.Min(
                    bounds.size.x,
                    bounds.size.z);

            if (horizontal >= 16f &&
                vertical <= 2.5f)
            {
                return
                    ShapeType.Road;
            }

            if (vertical >= 3.5f &&
                smallerHorizontal >= 2f &&
                horizontal >= 3f)
            {
                return
                    ShapeType.Building;
            }

            return
                ShapeType.Ignore;
        }

        private static bool ShouldIgnore(
            Transform item)
        {
            string path =
                HierarchyName(
                    item);

            return
                ContainsAny(
                    path,
                    "traffic",
                    "carcontainer",
                    "vehicle",
                    "wheel",
                    "smoke",
                    "particle",
                    "skid",
                    "trail",
                    "light",
                    "lamp",
                    "tree",
                    "bush",
                    "grass",
                    "background",
                    "sky",
                    "cloud",
                    "water");
        }

        private static string HierarchyName(
            Transform item)
        {
            System.Text.StringBuilder builder =
                new();

            Transform current =
                item;

            while (current != null)
            {
                builder.Insert(
                    0,
                    Normalize(
                        current.name) +
                    "/");

                current =
                    current.parent;
            }

            return
                builder.ToString();
        }

        private static bool ContainsAny(
            string value,
            params string[] parts)
        {
            foreach (string part in
                     parts)
            {
                if (value.Contains(
                        part))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return
                    string.Empty;
            }

            System.Text.StringBuilder builder =
                new(
                    value.Length);

            foreach (char character in
                     value.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(
                        character))
                {
                    builder.Append(
                        character);
                }
            }

            return
                builder.ToString();
        }

        private enum ShapeType
        {
            Ignore = 0,
            Road = 1,
            Building = 2
        }

        private struct MapShape
        {
            public Bounds Bounds;
            public ShapeType Type;
        }
    }
}
