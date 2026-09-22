using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class CitySchematicMap
    {
        // The minimap is displayed at roughly 178 px in the HUD. A 256 px
        // source keeps it crisp while cutting CPU work and texture memory to
        // one quarter of the previous 512 px runtime-generated map.
        private const int TextureSize =
            256;

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

        private static readonly string[] RoadNameParts =
        {
            "road",
            "street",
            "highway",
            "avenue",
            "intersection",
            "crossroad",
            "sidewalk",
            "pavement",
            "asphalt",
            "parking"
        };

        private static readonly string[] BuildingNameParts =
        {
            "building",
            "house",
            "shop",
            "store",
            "office",
            "garage",
            "warehouse",
            "apartment",
            "hotel",
            "wall"
        };

        private static readonly string[] IgnoreNameParts =
        {
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
            "water"
        };

        private readonly List<MapShape> shapes =
            new();

        private readonly List<Vector3> roadPoints =
            new();

        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>>
            FieldCache =
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
                if (renderer == null)
                    continue;

                string hierarchyPath =
                    HierarchyName(
                        renderer.transform);

                if (ShouldIgnore(
                        hierarchyPath))
                {
                    continue;
                }

                Bounds bounds =
                    renderer.bounds;

                ShapeType type =
                    Classify(
                        bounds,
                        hierarchyPath);

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
                // Road renderer bounds are coarse rectangles and become very
                // obvious once the minimap is zoomed out. The authored FCG
                // traffic graph below provides the actual road network, so
                // only draw non-road world shapes here.
                if (shape.Type ==
                    ShapeType.Road)
                {
                    continue;
                }

                DrawShape(
                    pixels,
                    shape);
            }

            DrawFcgTrafficRoads(
                pixels,
                cityRoot);

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

        private void DrawFcgTrafficRoads(
            Color[] pixels,
            GameObject cityRoot)
        {
            if (cityRoot == null)
                return;

            // Traffic data lives under the authored FCG city. Restricting the
            // lookup to that hierarchy avoids scanning every MonoBehaviour in
            // the scene during HUD startup.
            MonoBehaviour[] behaviours =
                cityRoot.GetComponentsInChildren<MonoBehaviour>(
                    true);

            foreach (MonoBehaviour behaviour in
                     behaviours)
            {
                if (behaviour == null)
                    continue;

                FieldInfo waypointsField =
                    FindField(
                        behaviour.GetType(),
                        "waypoints");

                FieldInfo next0Field =
                    FindField(
                        behaviour.GetType(),
                        "nextWay0");

                FieldInfo next1Field =
                    FindField(
                        behaviour.GetType(),
                        "nextWay1");

                if (waypointsField == null ||
                    next0Field == null ||
                    next1Field == null)
                {
                    continue;
                }

                IEnumerable enumerable;

                try
                {
                    enumerable =
                        waypointsField.GetValue(
                            behaviour) as IEnumerable;
                }
                catch
                {
                    continue;
                }

                if (enumerable == null)
                    continue;

                roadPoints.Clear();

                foreach (object item in
                         enumerable)
                {
                    Transform transform =
                        item as Transform;

                    if (transform == null &&
                        item is GameObject gameObject)
                    {
                        transform =
                            gameObject.transform;
                    }

                    if (transform == null &&
                        item is Component component)
                    {
                        transform =
                            component.transform;
                    }

                    if (transform != null)
                    {
                        roadPoints.Add(
                            transform.position);
                    }
                }

                for (int i = 0;
                     i < roadPoints.Count - 1;
                     i++)
                {
                    DrawRoadSegment(
                        pixels,
                        roadPoints[i],
                        roadPoints[i + 1]);
                }
            }
        }

        private void DrawRoadSegment(
            Color[] pixels,
            Vector3 worldA,
            Vector3 worldB)
        {
            Vector2 uvA =
                WorldToUv(
                    worldA);

            Vector2 uvB =
                WorldToUv(
                    worldB);

            Vector2 pixelA =
                new(
                    uvA.x *
                    (TextureSize - 1),
                    uvA.y *
                    (TextureSize - 1));

            Vector2 pixelB =
                new(
                    uvB.x *
                    (TextureSize - 1),
                    uvB.y *
                    (TextureSize - 1));

            float length =
                Vector2.Distance(
                    pixelA,
                    pixelB);

            int steps =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        length));

            const int roadHalfWidth =
                4;

            for (int step = 0;
                 step <= steps;
                 step++)
            {
                Vector2 point =
                    Vector2.Lerp(
                        pixelA,
                        pixelB,
                        step /
                        (float)steps);

                int centerX =
                    Mathf.RoundToInt(
                        point.x);

                int centerY =
                    Mathf.RoundToInt(
                        point.y);

                for (int y =
                         -roadHalfWidth;
                     y <=
                         roadHalfWidth;
                     y++)
                {
                    int py =
                        centerY + y;

                    if (py < 0 ||
                        py >= TextureSize)
                    {
                        continue;
                    }

                    int row =
                        py *
                        TextureSize;

                    for (int x =
                             -roadHalfWidth;
                         x <=
                             roadHalfWidth;
                         x++)
                    {
                        int px =
                            centerX + x;

                        if (px < 0 ||
                            px >= TextureSize)
                        {
                            continue;
                        }

                        int radiusSquared =
                            x * x +
                            y * y;

                        int edgeRadius =
                            roadHalfWidth - 1;

                        pixels[row + px] =
                            radiusSquared >=
                                edgeRadius *
                                edgeRadius
                                ? RoadEdge
                                : Road;
                    }
                }
            }
        }

        private static FieldInfo FindField(
            Type type,
            string fieldName)
        {
            if (type == null ||
                string.IsNullOrEmpty(
                    fieldName))
            {
                return null;
            }

            if (!FieldCache.TryGetValue(
                    type,
                    out Dictionary<string, FieldInfo> typeCache))
            {
                typeCache =
                    new Dictionary<string, FieldInfo>(
                        StringComparer.Ordinal);

                FieldCache[
                    type] =
                    typeCache;
            }

            if (typeCache.TryGetValue(
                    fieldName,
                    out FieldInfo cached))
            {
                return
                    cached;
            }

            Type current =
                type;

            while (current != null)
            {
                FieldInfo field =
                    current.GetField(
                        fieldName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                if (field != null)
                {
                    typeCache[
                        fieldName] =
                        field;

                    return
                        field;
                }

                current =
                    current.BaseType;
            }

            typeCache[
                fieldName] =
                null;

            return
                null;
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
            Bounds bounds,
            string path)
        {
            if (ContainsAny(
                    path,
                    RoadNameParts))
            {
                return
                    ShapeType.Road;
            }

            if (ContainsAny(
                    path,
                    BuildingNameParts))
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
            string path)
        {
            return
                ContainsAny(
                    path,
                    IgnoreNameParts);
        }

        private static string HierarchyName(
            Transform item)
        {
            System.Text.StringBuilder builder =
                new();

            Transform current =
                item;

            // Classification only searches for name fragments, so hierarchy
            // order is irrelevant. Appending avoids repeated front-inserts
            // that shift the whole builder for every parent.
            while (current != null)
            {
                AppendNormalized(
                    builder,
                    current.name);

                builder.Append(
                    '/');

                current =
                    current.parent;
            }

            return
                builder.ToString();
        }

        private static bool ContainsAny(
            string value,
            string[] parts)
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

        private static void AppendNormalized(
            System.Text.StringBuilder builder,
            string value)
        {
            if (builder == null ||
                string.IsNullOrWhiteSpace(
                    value))
            {
                return;
            }

            foreach (char character in
                     value)
            {
                if (char.IsLetterOrDigit(
                        character))
                {
                    builder.Append(
                        char.ToLowerInvariant(
                            character));
                }
            }
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
