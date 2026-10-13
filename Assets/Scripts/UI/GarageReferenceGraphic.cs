using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    // Shared UI surface and Tabler pictogram renderer. The surface mesh is
    // decorative; gameplay icon shapes are imported Tabler sprites only.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GarageReferenceGraphic : MaskableGraphic
    {
        public enum Symbol { Surface, Credits, Crown, Star, Wrench, Engine, Brake,
            Shock, Paint, Rim, Neon, Speed, Acceleration, Gear, Stability,
            Steering, Drift, Mass, Up, Left, Right, NavigationLeft, NavigationRight,
            CitySurface, Padlock, OpenPadlock, MinimapRim, Keyboard, Check,
            Camera, Gift, Shop, MenuGrid, Pause, Close }
        public Symbol symbol;
        private VertexHelper mesh;

        // Expose the semantic sprite mapping to Editor diagnostics.
        public string TablerIconName => symbol switch
            {
                Symbol.Credits => "coins",
                Symbol.Crown => "crown",
                Symbol.Star => "star",
                Symbol.Wrench => "tools",
                Symbol.Engine => "engine",
                Symbol.Brake => "wheel",
                Symbol.Shock => "car-suspension",
                Symbol.Paint => "palette",
                Symbol.Rim => "wheel",
                Symbol.Neon => "sparkles",
                Symbol.Speed => "gauge",
                Symbol.Acceleration => "brand-speedtest",
                Symbol.Gear => "manual-gearbox",
                Symbol.Stability => "car-4wd",
                Symbol.Steering => "steering-wheel",
                Symbol.Drift => "car-turbine",
                Symbol.Mass => "car-suv",
                Symbol.Up => "arrow-up",
                Symbol.Close => "x",
                Symbol.Left => "arrow-left",
                Symbol.Right => "arrow-right",
                Symbol.Padlock => "lock",
                Symbol.OpenPadlock => "lock-open",
                Symbol.Keyboard => "keyboard",
                Symbol.Check => "check",
                Symbol.Camera => "camera",
                Symbol.Gift => "gift",
                Symbol.Shop => "shopping-cart",
                Symbol.MenuGrid => "layout-grid",
                Symbol.Pause => "player-pause",
                // Decorative surfaces and the minimap ring have no icon.
                _ => null
            };

        private Sprite TablerSprite =>
            TablerIconName == null ? null : MotorCityIconLibrary.Get(TablerIconName);

        public override Texture mainTexture =>
            TablerSprite != null ? TablerSprite.texture : base.mainTexture;

        private void PopulateTablerMesh(VertexHelper vh, Sprite sprite)
        {
            Rect rect = rectTransform.rect;
            // Fit the sprite's ORIGINAL width/height in physical screen
            // space. RectTransform parents may stretch X and Y separately;
            // a local square is not necessarily a square on screen.
            const float inset = 0.86f;
            float maxWidth = Mathf.Abs(rect.width) * inset;
            float maxHeight = Mathf.Abs(rect.height) * inset;
            float worldX = rectTransform.TransformVector(Vector3.right).magnitude;
            float worldY = rectTransform.TransformVector(Vector3.up).magnitude;
            float nativeAspect = sprite.rect.width /
                Mathf.Max(1f, sprite.rect.height);
            float localAspect = nativeAspect *
                (worldY / Mathf.Max(0.0001f, worldX));
            float width = Mathf.Min(maxWidth, maxHeight * localAspect);
            float height = width / Mathf.Max(0.0001f, localAspect);
            Rect draw = new Rect(rect.center.x - width * 0.5f,
                rect.center.y - height * 0.5f, width, height);

            Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = new Vector3(draw.xMin, draw.yMin);
            vertex.uv0 = new Vector2(uv.x, uv.y);
            vh.AddVert(vertex);
            vertex.position = new Vector3(draw.xMin, draw.yMax);
            vertex.uv0 = new Vector2(uv.x, uv.w);
            vh.AddVert(vertex);
            vertex.position = new Vector3(draw.xMax, draw.yMax);
            vertex.uv0 = new Vector2(uv.z, uv.w);
            vh.AddVert(vertex);
            vertex.position = new Vector3(draw.xMax, draw.yMin);
            vertex.uv0 = new Vector2(uv.z, uv.y);
            vh.AddVert(vertex);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); mesh = vh;
            // Panels, navigation hit surfaces and the minimap border remain
            // geometry-based because these are UI decorations, not icons.
            if (symbol == Symbol.Surface) { Surface(); return; }
            if (symbol == Symbol.MinimapRim) { Ring(50, 50, 48, 3); return; }
            if (symbol == Symbol.NavigationLeft ||
                symbol == Symbol.NavigationRight || symbol == Symbol.CitySurface)
            {
                NavigationSurface();
                return;
            }

            // All semantic symbols are imported Tabler sprites. No legacy
            // procedurally drawn replacement glyphs can appear anymore.
            Sprite icon = TablerSprite;
            if (icon != null)
                PopulateTablerMesh(vh, icon);
        }

        private Vector2 Point(Vector2 p)
        {
            Rect rect = rectTransform.rect;
            Vector2 offset = p - new Vector2(50f, 50f);

            // Decorative surfaces fill their rectangles. The minimap
            // ring keeps a true circle even on a stretched parent.
            if (symbol == Symbol.Surface || symbol == Symbol.CitySurface ||
                symbol == Symbol.NavigationLeft || symbol == Symbol.NavigationRight)
            {
                return rect.center + Vector2.Scale(offset,
                    new Vector2(rect.width / 100f, rect.height / 100f));
            }

            float axisX = Mathf.Max(0.0001f,
                rectTransform.TransformVector(Vector3.right).magnitude);
            float axisY = Mathf.Max(0.0001f,
                rectTransform.TransformVector(Vector3.up).magnitude);
            float pixelSize = Mathf.Min(
                Mathf.Abs(rect.width) * axisX,
                Mathf.Abs(rect.height) * axisY);
            return rect.center + Vector2.Scale(offset,
                new Vector2(pixelSize / (100f * axisX),
                            pixelSize / (100f * axisY)));
        }
        private void Polygon(params Vector2[] points) => Polygon(color, points);
        private void Polygon(Color tint, params Vector2[] points)
        {
            int start = mesh.currentVertCount;
            foreach(Vector2 point in points) mesh.AddVert(Point(point),tint,Vector2.zero);
            var remaining = new List<int>();
            float area = 0;
            for(int i=0;i<points.Length;i++)
            {
                remaining.Add(i);
                Vector2 a=points[i],b=points[(i+1)%points.Length];
                area += a.x*b.y-b.x*a.y;
            }
            float orientation=area>=0?1:-1;
            // Ear clipping handles concave decorative surface polygons.
            while(remaining.Count>2)
            {
                bool clipped=false;
                for(int i=0;i<remaining.Count;i++)
                {
                    int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
                    if(Cross(points[a],points[b],points[c])*orientation<=.0001f)continue;
                    bool contains=false;
                    foreach(int p in remaining)
                    {
                        if(p==a||p==b||p==c)continue;
                        if(Cross(points[a],points[b],points[p])*orientation>=0 &&
                           Cross(points[b],points[c],points[p])*orientation>=0 &&
                           Cross(points[c],points[a],points[p])*orientation>=0){contains=true;break;}
                    }
                    if(contains)continue;
                    // Match Unity UI's clockwise triangle winding.
                    if (orientation > 0) mesh.AddTriangle(start+a,start+c,start+b);
                    else mesh.AddTriangle(start+a,start+b,start+c);
                    remaining.RemoveAt(i);clipped=true;break;
                }
                if(!clipped)break;
            }
            var contour = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++) contour[i] = Point(points[i]);
            AntialiasContour(contour, tint);
        }
        // Coverage fringe in canvas units, so the transition stays one pixel
        // wide at every window size. It is part of the same graphic mesh.
        private void AntialiasContour(Vector2[] points, Color tint)
        {
            float area = 0;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i+1)%points.Length];
                area += a.x*b.y - b.x*a.y;
            }
            float orientation = area >= 0 ? 1 : -1;
            float width = 1f / Mathf.Max(.01f, canvas != null ? canvas.scaleFactor : 1f);
            Color transparent = new(tint.r, tint.g, tint.b, 0);
            int start = mesh.currentVertCount;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p = points[i];
                Vector2 incoming = (p - points[(i+points.Length-1)%points.Length]).normalized;
                Vector2 outgoing = (points[(i+1)%points.Length] - p).normalized;
                Vector2 n1 = new Vector2(incoming.y, -incoming.x) * orientation;
                Vector2 n2 = new Vector2(outgoing.y, -outgoing.x) * orientation;
                Vector2 bisector = (n1+n2).normalized;
                float denominator = Mathf.Max(.25f, bisector.x*n2.x+bisector.y*n2.y);
                mesh.AddVert(p, tint, Vector2.zero);
                mesh.AddVert(p + bisector*(width/denominator), transparent, Vector2.zero);
            }
            for (int i = 0; i < points.Length; i++)
            {
                int a = start+i*2, b = start+((i+1)%points.Length)*2;
                if (orientation > 0)
                {
                    mesh.AddTriangle(a,b,b+1); mesh.AddTriangle(a,b+1,a+1);
                }
                else
                {
                    mesh.AddTriangle(a,b+1,b); mesh.AddTriangle(a,a+1,b+1);
                }
            }
        }
        private static float Cross(Vector2 a,Vector2 b,Vector2 c) => (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        private void Line(float x1, float y1, float x2, float y2,
            float width, Color tint)
        {
            Vector2 a = new(x1, y1), b = new(x2, y2);
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) *
                width * .5f;
            Polygon(tint, a - normal, b - normal, b + normal, a + normal);
        }

        // The minimap rim is another UI border, so it uses the same
        // cyan-to-violet glow rather than a solid lilac stroke.
        private void Ring(float x, float y, float radius, float width)
        {
            NeonCircleBand(x, y, radius + width * 0.5f + 5f,
                radius + width * 0.5f + 2f, 0f, 0.09f);
            NeonCircleBand(x, y, radius + width * 0.5f + 2f,
                radius + width * 0.5f, 0.09f, 0.54f);
            NeonCircleBand(x, y, radius + width * 0.5f,
                radius - width * 0.5f, 0.54f, 0.60f);
            NeonCircleBand(x, y, radius - width * 0.5f,
                radius - width * 0.5f - 2f, 0.60f, 0f);
        }

        private void NeonCircleBand(float x, float y,
            float outerRadius, float innerRadius,
            float outerAlpha, float innerAlpha)
        {
            const int segments = 96;
            int start = mesh.currentVertCount;
            Rect bounds = rectTransform.rect;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outer = Point(new Vector2(x, y) + dir * outerRadius);
                Vector2 inner = Point(new Vector2(x, y) + dir * innerRadius);
                mesh.AddVert(outer,
                    NeonAt(outer.x, bounds, outerAlpha), Vector2.zero);
                mesh.AddVert(inner,
                    NeonAt(inner.x, bounds, innerAlpha), Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
            {
                int a = start + i * 2;
                int b = start + (i + 1) % segments * 2;
                mesh.AddTriangle(a, b + 1, b);
                mesh.AddTriangle(a, a + 1, b + 1);
            }
        }

        // Unified Motor City neon light: left cyan, right violet.
        // The gradient is drawn in the same CanvasRenderer mesh, so every
        // panel and button shares one visual style without extra UI objects.
        private static readonly Color NeonCyan = new(0.06f, 0.85f, 1f, 1f);
        private static readonly Color NeonViolet = new(0.62f, 0.40f, 1f, 1f);
        private const int GlowSteps = 8;
        private const int GlowVertexCount = 4 * (GlowSteps + 1);

        private Color NeonAt(float x, Rect bounds, float opacity)
        {
            float t = Mathf.Clamp01((x - bounds.xMin) /
                Mathf.Max(0.001f, bounds.width));
            Color result = Color.Lerp(NeonCyan, NeonViolet, t);
            result.a = Mathf.Clamp01(opacity * color.a);
            return result;
        }

        private static Vector2 NeonCorner(Rect r, float radius,
            float expand, int corner, int step)
        {
            Vector2 origin = new(
                corner == 0 || corner == 3 ? r.xMax - radius : r.xMin + radius,
                corner == 0 || corner == 1 ? r.yMax - radius : r.yMin + radius);
            float angle = (corner * 90f +
                step * 90f / GlowSteps) * Mathf.Deg2Rad;
            return origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
                Mathf.Max(0f, radius + expand);
        }

        private void NeonBand(Rect r, float radius, float outer,
            float inner, float outerAlpha, float innerAlpha)
        {
            int first = mesh.currentVertCount;
            for (int corner = 0; corner < 4; corner++)
                for (int step = 0; step <= GlowSteps; step++)
                {
                    Vector2 a = NeonCorner(r, radius, outer, corner, step);
                    Vector2 b = NeonCorner(r, radius, inner, corner, step);
                    mesh.AddVert(a, NeonAt(a.x, r, outerAlpha), Vector2.zero);
                    mesh.AddVert(b, NeonAt(b.x, r, innerAlpha), Vector2.zero);
                }

            for (int i = 0; i < GlowVertexCount; i++)
            {
                int a = first + i * 2;
                int b = first + ((i + 1) % GlowVertexCount) * 2;
                mesh.AddTriangle(a, b + 1, b);
                mesh.AddTriangle(a, a + 1, b + 1);
            }
        }

        private void NeonEdge(Vector2 from, Vector2 to, Rect r,
            float thickness, float opacity)
        {
            Vector2 a = Point(from), b = Point(to);
            Vector2 d = b - a;
            if (d.sqrMagnitude <= 0.0001f)
                return;

            Vector2 normal = new Vector2(-d.y, d.x).normalized *
                (thickness * 0.5f);
            int first = mesh.currentVertCount;
            Color aColor = NeonAt(a.x, r, opacity);
            Color bColor = NeonAt(b.x, r, opacity);
            mesh.AddVert(a - normal, aColor, Vector2.zero);
            mesh.AddVert(b - normal, bColor, Vector2.zero);
            mesh.AddVert(b + normal, bColor, Vector2.zero);
            mesh.AddVert(a + normal, aColor, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private void Surface()
        {
            Rect r=rectTransform.rect;
            int center=mesh.currentVertCount;
            mesh.AddVert(r.center,new Color(color.r*.45f,color.g*.45f,color.b*.55f,color.a),Vector2.zero);
            float radius=Mathf.Min(12,Mathf.Min(r.width,r.height)*.2f);
            for(int corner=0;corner<4;corner++)
            {
                Vector2 origin = corner switch {
                    0 => new Vector2(r.xMax-radius,r.yMax-radius),
                    1 => new Vector2(r.xMin+radius,r.yMax-radius),
                    2 => new Vector2(r.xMin+radius,r.yMin+radius),
                    _ => new Vector2(r.xMax-radius,r.yMin+radius) };
                for(int step=0;step<=8;step++)
                {
                    float angle=(corner*90+step*90f/8)*Mathf.Deg2Rad;
                    Vector2 p=origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                    float t=Mathf.InverseLerp(r.yMin,r.yMax,p.y);
                    Color c=Color.Lerp(new Color(.035f,.035f,.09f,color.a),color,t);
                    mesh.AddVert(p,c,Vector2.zero);
                }
            }
            int count=mesh.currentVertCount-center-1;
            for(int i=0;i<count;i++)mesh.AddTriangle(center,center+1+(i+1)%count,center+1+i);
            // Soft layered glow, not the old 1.5px flat purple outline.
            // Faded outer bands give the neon a halo even without URP bloom.
            float spread = Mathf.Min(8f,
                Mathf.Min(r.width, r.height) * 0.13f);
            if (spread >= 1f)
            {
                NeonBand(r, radius, spread, spread * 0.62f, 0f, 0.075f);
                NeonBand(r, radius, spread * 0.62f,
                    spread * 0.26f, 0.075f, 0.19f);
            }
            NeonBand(r, radius, Mathf.Max(0.35f, spread * 0.26f),
                0.2f, 0.19f, 0.68f);
            NeonBand(r, radius, 0.2f, -1.1f, 0.68f, 0.23f);
            NeonBand(r, radius, -1.1f, -2.6f, 0.23f, 0f);
        }

        private void NavigationSurface()
        {
            Vector2[] points = symbol == Symbol.CitySurface
                ? new[] {new Vector2(1,2),new Vector2(99,2),new Vector2(99,98),new Vector2(5,98)}
                : new[] {new Vector2(98,2),new Vector2(52,2),new Vector2(2,50),new Vector2(52,98),new Vector2(98,98)};
            if(symbol == Symbol.NavigationRight)
                for(int i=0;i<points.Length;i++)points[i].x=100-points[i].x;
            Polygon(new Color(.045f,.035f,.13f,.96f),points);
            for(int i=0;i<points.Length;i++)
            {
                Vector2 a=points[i],b=points[(i+1)%points.Length];

                Rect bounds = rectTransform.rect;
                NeonEdge(a, b, bounds, 7f, 0.05f);
                NeonEdge(a, b, bounds, 3.5f, 0.16f);
                NeonEdge(a, b, bounds, 1.45f, 0.82f);
            }
        }
    }
}
