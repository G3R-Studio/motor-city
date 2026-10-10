using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.Profiling;

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
                0.92f,
                0.93f,
                0.94f,
                1f);

        private static readonly Color Road =
            new(
                0.56f,
                0.61f,
                0.66f,
                1f);

        private static readonly Color RoadEdge =
            new(
                0.42f,
                0.47f,
                0.52f,
                1f);

        private static readonly Color Building =
            new(
                0.78f,
                0.80f,
                0.83f,
                1f);

        private static readonly Color BuildingEdge =
            new(
                0.67f,
                0.70f,
                0.74f,
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

        private static readonly ProfilerMarker BuildMapMarker =
            new("MotorCity.Minimap.Build");

        private readonly List<MapShape> shapes =
            new();

        private readonly List<Vector3> roadPoints =
            new();

public Texture2D Texture { get; private set; }

        public Bounds WorldBounds { get; private set; }

        public bool IsValid =>
            Texture != null &&
            WorldBounds.size.x > 1f &&
            WorldBounds.size.z > 1f;

        public bool Build()
        {
            using var mapSample = BuildMapMarker.Auto();

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

            // Keep the same normalized ancestor-name classification, but
            // cache its three flags per Transform instead of allocating a
            // full hierarchy-path string for every renderer.
            var hierarchyFlags = new Dictionary<Transform, NameFlags>(
                renderers.Length);
            var nameFlags = new Dictionary<string, NameFlags>(
                StringComparer.Ordinal);
            var nameBuilder = new System.Text.StringBuilder(96);
            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null)
                    continue;

                NameFlags flags =
                    GetHierarchyFlags(
                        renderer.transform,
                        hierarchyFlags,
                        nameFlags,
                        nameBuilder);

                if ((flags & NameFlags.Ignore) != NameFlags.None)
                    continue;

                Bounds bounds =
                    renderer.bounds;

                ShapeType type =
                    Classify(
                        bounds,
                        flags);

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

            // FCG waypoints are authored on FCGWaypointsContainer components.
            // Select only those components instead of materializing every
            // MonoBehaviour (including traffic cars and unrelated scripts).
            // Keep the old scan as a compatibility fallback for custom FCG
            // assemblies where the expected type cannot be resolved.
            Type waypointType = Type.GetType(
                "FCG.FCGWaypointsContainer, Assembly-CSharp",
                false);
            Component[] authoredBehaviours =
                waypointType != null
                    ? cityRoot.GetComponentsInChildren(waypointType, true)
                    : cityRoot.GetComponentsInChildren<MonoBehaviour>(true);

            ExpandBoundsWithFcgTraffic(
                authoredBehaviours,
                ref mapBounds,
                ref boundsInitialized);

            if (!boundsInitialized ||
                shapes.Count == 0)
            {
                return false;
            }

            Vector3 size =
                mapBounds.size;

            // Keep enough empty schematic texture around the authored road
            // network for the 260 m minimap window to stay centered on the
            // player even at the outermost playable streets.
            const float padding =
                300f;

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

            Color32[] pixels =
                new Color32[
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
                authoredBehaviours);

            Texture.SetPixels32(
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

        private void ExpandBoundsWithFcgTraffic(
            Component[] behaviours,
            ref Bounds mapBounds,
            ref bool boundsInitialized)
        {
            if (behaviours == null)
                return;

            foreach (Component component in
                     behaviours)
            {
                MonoBehaviour behaviour =
                    component as MonoBehaviour;
                if (behaviour == null)
                    continue;

                FieldInfo waypointsField =
                    AuthoredCityFieldCache.FindField(
                        behaviour.GetType(),
                        "waypoints");

                FieldInfo next0Field =
                    AuthoredCityFieldCache.FindField(
                        behaviour.GetType(),
                        "nextWay0");

                FieldInfo next1Field =
                    AuthoredCityFieldCache.FindField(
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

                foreach (object item in
                         enumerable)
                {
                    Transform waypoint =
                        item as Transform;

                    if (waypoint == null &&
                        item is GameObject gameObject)
                    {
                        waypoint =
                            gameObject.transform;
                    }

                    if (waypoint == null &&
                        item is Component component)
                    {
                        waypoint =
                            component.transform;
                    }

                    if (waypoint == null)
                        continue;

                    Vector3 position =
                        waypoint.position;

                    Bounds pointBounds =
                        new(
                            new Vector3(
                                position.x,
                                0f,
                                position.z),
                            new Vector3(
                                1f,
                                1f,
                                1f));

                    if (!boundsInitialized)
                    {
                        mapBounds =
                            pointBounds;

                        boundsInitialized =
                            true;
                    }
                    else
                    {
                        mapBounds.Encapsulate(
                            pointBounds);
                    }
                }
            }
        }

        private void DrawFcgTrafficRoads(
            Color32[] pixels,
            Component[] behaviours)
        {
            if (behaviours == null)
                return;

            foreach (Component component in
                     behaviours)
            {
                MonoBehaviour behaviour =
                    component as MonoBehaviour;
                if (behaviour == null)
                    continue;

                FieldInfo waypointsField =
                    AuthoredCityFieldCache.FindField(
                        behaviour.GetType(),
                        "waypoints");

                FieldInfo next0Field =
                    AuthoredCityFieldCache.FindField(
                        behaviour.GetType(),
                        "nextWay0");

                FieldInfo next1Field =
                    AuthoredCityFieldCache.FindField(
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
            Color32[] pixels,
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
                3;

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

        private Vector2 WorldToUv(
            Vector3 position)
        {
            Bounds bounds =
                WorldBounds;

            float width =
                Mathf.Max(
                    0.001f,
                    bounds.size.x);

            float height =
                Mathf.Max(
                    0.001f,
                    bounds.size.z);

            float u =
                (position.x -
                 bounds.min.x) /
                width;

            float v =
                (position.z -
                 bounds.min.z) /
                height;

            return
                new Vector2(
                    u,
                    v);
        }

        private void DrawShape(
            Color32[] pixels,
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
            NameFlags flags)
        {
            if ((flags & NameFlags.Road) != NameFlags.None)
            {
                return ShapeType.Road;
            }

            if ((flags & NameFlags.Building) != NameFlags.None)
            {
                return ShapeType.Building;
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

        private static NameFlags GetHierarchyFlags(
            Transform item,
            Dictionary<Transform, NameFlags> hierarchyCache,
            Dictionary<string, NameFlags> nameCache,
            System.Text.StringBuilder builder)
        {
            if (item == null)
                return NameFlags.None;

            if (hierarchyCache.TryGetValue(item, out NameFlags cached))
                return cached;

            // Name matches are independent of hierarchy order. Merge the
            // parent's cached flags with this node's normalized name. Each
            // Transform is evaluated once for the duration of Build().
            NameFlags flags = GetHierarchyFlags(
                item.parent,
                hierarchyCache,
                nameCache,
                builder);

            string name = item.name;
            if (!nameCache.TryGetValue(name, out NameFlags ownFlags))
            {
                builder.Clear();
                AppendNormalized(builder, name);
                string normalized = builder.ToString();

                ownFlags = NameFlags.None;
                if (ContainsAny(normalized, RoadNameParts))
                    ownFlags |= NameFlags.Road;
                if (ContainsAny(normalized, BuildingNameParts))
                    ownFlags |= NameFlags.Building;
                if (ContainsAny(normalized, IgnoreNameParts))
                    ownFlags |= NameFlags.Ignore;

                nameCache.Add(name, ownFlags);
            }

            flags |= ownFlags;
            hierarchyCache.Add(item, flags);
            return flags;
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

        [Flags]
        private enum NameFlags
        {
            None = 0,
            Road = 1,
            Building = 2,
            Ignore = 4
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
