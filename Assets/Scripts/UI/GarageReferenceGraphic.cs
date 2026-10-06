using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    // Vector UI artwork stays crisp at all canvas scales and requires no atlas.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GarageReferenceGraphic : MaskableGraphic
    {
        public enum Symbol { Surface, Credits, Crown, Star, Wrench, Engine, Brake,
            Shock, Paint, Rim, Neon, Speed, Acceleration, Gear, Stability,
            Steering, Drift, Mass, Up, Left, Right, NavigationLeft, NavigationRight,
            CitySurface, Padlock, OpenPadlock, MinimapRim, Keyboard, Check,
            Camera, Gift, Shop, MenuGrid, Pause }
        public Symbol symbol;
        private VertexHelper mesh;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); mesh = vh;
            if (symbol == Symbol.Surface) { Surface(); return; }
            if (symbol == Symbol.MinimapRim) { Ring(50,50,48,3); return; }
            if (symbol == Symbol.NavigationLeft || symbol == Symbol.NavigationRight || symbol == Symbol.CitySurface)
            {
                NavigationSurface(); return;
            }
            switch (symbol)
            {
                case Symbol.Check:
                    Polygon(new Vector2(8,47),new Vector2(22,61),new Vector2(41,40),
                        new Vector2(78,84),new Vector2(93,70),new Vector2(42,10)); break;
                case Symbol.Camera:
                    Box(10,22,90,76); Box(29,76,50,88); Ring(57,49,19,7);
                    Box(17,64,28,72); break;
                case Symbol.Gift:
                    Box(12,17,88,68); Box(8,68,92,82);
                    Line(50,17,50,82,7,Dark);
                    Ring(36,86,12,6); Ring(64,86,12,6); break;
                case Symbol.Shop:
                    Box(15,20,85,61); Line(24,61,31,82,6);
                    Line(31,82,88,82,6); Circle(32,11,7); Circle(72,11,7);
                    Line(24,49,84,49,5,Dark); break;
                case Symbol.MenuGrid:
                    Box(10,58,34,82); Box(38,58,62,82); Box(66,58,90,82);
                    Box(10,26,34,50); Box(38,26,62,50); Box(66,26,90,50); break;
                case Symbol.Pause:
                    Box(24,18,42,82); Box(58,18,76,82); break;
                case Symbol.Keyboard:
                    Line(8,25,92,25,5); Line(92,25,92,75,5);
                    Line(92,75,8,75,5); Line(8,75,8,25,5);
                    for(int row=0;row<2;row++)
                        for(int key=0;key<6;key++) Box(18+key*11,48+row*13,24+key*11,54+row*13);
                    Line(29,36,72,36,5); break;
                case Symbol.Padlock:
                case Symbol.OpenPadlock:
                    Line(25,55,25,88,6,GarageLockBlue); Line(25,88,73,88,6,GarageLockBlue);
                    Line(73,88,73,symbol==Symbol.OpenPadlock?69:55,6,GarageLockBlue);
                    Box(7,5,93,59); Line(50,18,50,44,6,Dark); Line(38,31,62,31,6,Dark);break;
                case Symbol.Credits:
                    Line(30,48,30,79,6); Line(30,79,70,79,6); Line(70,79,70,48,6);
                    Box(13,8,87,61); Line(50,20,50,50,5,Dark);
                    Line(60,44,42,44,5,Dark); Line(42,44,42,35,5,Dark);
                    Line(42,35,58,35,5,Dark); Line(58,35,58,26,5,Dark);
                    Line(58,26,40,26,5,Dark); break;
                case Symbol.Crown:
                    Polygon(new Vector2(6,75),new Vector2(27,47),new Vector2(50,94),new Vector2(72,47),new Vector2(96,76),new Vector2(81,13),new Vector2(20,13));
                    Circle(50,37,5,Dark); break;
                case Symbol.Star:
                    var star = new Vector2[10];
                    for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad; float r=i%2==0?47:21;star[i]=new Vector2(50+Mathf.Cos(a)*r,50+Mathf.Sin(a)*r);}
                    Polygon(star); break;
                case Symbol.Wrench:
                    Line(20,15,70,68,15); Circle(20,15,10);
                    Polygon(new Vector2(56,65),new Vector2(57,87),new Vector2(75,98),new Vector2(70,78),new Vector2(84,65),new Vector2(98,71),new Vector2(89,51),new Vector2(69,49));
                    Circle(20,15,4,Dark); break;
                case Symbol.Engine:
                    Box(22,27,80,70); Box(8,33,23,62); Box(80,34,93,63);
                    Box(33,70,68,80); Line(38,88,61,88,5);
                    Line(51,34,51,61,6,Dark); Line(39,48,63,48,6,Dark); break;
                case Symbol.Brake:
                    Circle(50,50,39); Circle(50,50,28,Dark); Circle(50,50,13);
                    for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;Circle(50+Mathf.Cos(a)*21,50+Mathf.Sin(a)*21,3);}
                    Line(63,94,84,83,10); Line(84,83,94,64,10); break;
                case Symbol.Shock:
                    Line(20,8,78,90,9); Circle(20,8,9); Circle(78,90,9);
                    for(int i=0;i<5;i++){float y=25+i*11;Line(18+i*8,y,52+i*8,y-13,6);}
                    break;
                case Symbol.Paint:
                    Polygon(new Vector2(40,83),new Vector2(59,99),new Vector2(95,65),new Vector2(78,46));
                    Line(27,65,58,37,12); Line(27,36,47,53,12);
                    Line(20,28,26,36,7); Line(12,16,17,24,5); Line(26,8,28,18,4); break;
                case Symbol.Rim:
                    Ring(50,50,43,9); Ring(50,50,33,3); Circle(50,50,9);
                    for(int i=0;i<5;i++){float a=(90+i*72)*Mathf.Deg2Rad;Line(50,50,50+Mathf.Cos(a)*32,50+Mathf.Sin(a)*32,8);}
                    break;
                case Symbol.Neon:
                    Color glow = new(color.r,color.g,color.b,.18f);
                    Line(9,64,63,80,17,glow); Line(34,44,89,61,17,glow); Line(13,33,50,44,17,glow); Line(44,14,85,26,17,glow);
                    Line(9,64,63,80,7); Line(34,44,89,61,7); Line(13,33,50,44,7); Line(44,14,85,26,7); break;
                case Symbol.Speed:
                    Arc(50,46,41,10,170,6); Line(50,46,72,76,6); Circle(50,46,6); break;
                case Symbol.Acceleration:
                    Polygon(new Vector2(8,26),new Vector2(47,46),new Vector2(61,64),new Vector2(66,88),new Vector2(89,84),new Vector2(96,20),new Vector2(73,18),new Vector2(66,40),new Vector2(58,19)); break;
                case Symbol.Gear:
                    Ring(50,50,30,14);
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(50+Mathf.Cos(a)*29,50+Mathf.Sin(a)*29,50+Mathf.Cos(a)*44,50+Mathf.Sin(a)*44,13);}
                    break;
                case Symbol.Stability:
                    Polygon(new Vector2(50,94),new Vector2(7,11),new Vector2(93,11));
                    Line(50,33,50,65,6,Dark); Line(38,49,62,49,6,Dark); break;
                case Symbol.Steering:
                    Ring(50,50,41,8); Line(10,47,90,47,6); Line(50,47,50,10,7);Circle(50,50,12);break;
                case Symbol.Drift:
                    Box(25,47,75,79); Line(25,79,32,94,5);Line(32,94,67,94,5);Line(67,94,75,79,5);
                    Line(27,35,12,22,5);Line(12,22,30,10,5);Line(66,35,85,21,5);Line(85,21,65,7,5);break;
                case Symbol.Mass:
                    Polygon(new Vector2(25,72),new Vector2(75,72),new Vector2(90,10),new Vector2(10,10)); Ring(50,83,12,5);break;
                case Symbol.Up:
                    Polygon(new Vector2(50,94),new Vector2(8,49),new Vector2(34,49),new Vector2(34,13),new Vector2(66,13),new Vector2(66,49),new Vector2(92,49));break;
                case Symbol.Left:
                    Polygon(new Vector2(80,87), new Vector2(35,50), new Vector2(80,13), new Vector2(68,3), new Vector2(11,50), new Vector2(68,97));break;
                case Symbol.Right:
                    Polygon(new Vector2(20,87), new Vector2(65,50), new Vector2(20,13), new Vector2(32,3), new Vector2(89,50), new Vector2(32,97));break;
            }
        }

        private static Color Dark => new(.05f,.045f,.14f,1f);
        private static Color GarageLockBlue => new(.28f,.78f,1f,1f);
        private Vector2 Point(Vector2 p)
        {
            Rect rect = rectTransform.rect;
            return rect.center + Vector2.Scale(p-new Vector2(50,50),new Vector2(rect.width/100f,rect.height/100f));
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
            // Ear clipping also handles the concave crown, star and wrench.
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
        private void Box(float x1,float y1,float x2,float y2) => Polygon(new Vector2(x1,y1),new Vector2(x2,y1),new Vector2(x2,y2),new Vector2(x1,y2));
        private void Line(float x1,float y1,float x2,float y2,float width) => Line(x1,y1,x2,y2,width,color);
        private void Line(float x1,float y1,float x2,float y2,float width,Color tint)
        {
            Vector2 a=new(x1,y1),b=new(x2,y2),d=(b-a).normalized;
            Vector2 n=new Vector2(-d.y,d.x)*width*.5f;
            Polygon(tint,a-n,b-n,b+n,a+n);
        }
        private void Circle(float x,float y,float radius) => Circle(x,y,radius,color);
        private void Circle(float x,float y,float radius,Color tint)
        {
            Vector2[] points = new Vector2[96];
            for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/points.Length;points[i]=new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius);}
            Polygon(tint,points);
        }
        private void Ring(float x,float y,float radius,float width)
        {
            const int segments = 96;
            int start = mesh.currentVertCount;
            var outer = new Vector2[segments];
            var inner = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                outer[i] = Point(new Vector2(x,y) + direction * (radius + width * .5f));
                Vector2 inside = Point(new Vector2(x,y) + direction * (radius - width * .5f));
                inner[segments-1-i] = inside;
                mesh.AddVert(outer[i], color, Vector2.zero);
                mesh.AddVert(inside, color, Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
            {
                int a = start+i*2, b = start+((i+1)%segments)*2;
                mesh.AddTriangle(a,b+1,b); mesh.AddTriangle(a,a+1,b+1);
            }
            AntialiasContour(outer,color);
            AntialiasContour(inner,color);
        }
        private void Arc(float x,float y,float radius,float from,float to,float width)
        {
            for(int i=0;i<32;i++)
            {
                float a=Mathf.Lerp(from,to,(float)i/32)*Mathf.Deg2Rad,b=Mathf.Lerp(from,to,(float)(i+1)/32)*Mathf.Deg2Rad;
                Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius,width);
            }
        }
        private void Surface()
        {
            Rect r=rectTransform.rect;
            var outline = new List<Vector2>();
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
                    outline.Add(p);
                }
            }
            int count=mesh.currentVertCount-center-1;
            for(int i=0;i<count;i++)mesh.AddTriangle(center,center+1+(i+1)%count,center+1+i);
            // The border shares the fill contour; no extra glow objects.
            int border = mesh.currentVertCount;
            var innerBorder = new List<Vector2>();
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 origin = corner switch {
                    0 => new Vector2(r.xMax-radius,r.yMax-radius),
                    1 => new Vector2(r.xMin+radius,r.yMax-radius),
                    2 => new Vector2(r.xMin+radius,r.yMin+radius),
                    _ => new Vector2(r.xMax-radius,r.yMin+radius) };
                for (int step = 0; step <= 8; step++)
                {
                    float angle = (corner*90+step*90f/8)*Mathf.Deg2Rad;
                    Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                    Color stroke = new(.65f,.58f,1f,.9f);
                    mesh.AddVert(origin + direction * radius, stroke, Vector2.zero);
                    mesh.AddVert(origin + direction * Mathf.Max(0, radius - 1.5f), stroke, Vector2.zero);
                    innerBorder.Add(origin + direction * Mathf.Max(0, radius - 1.5f));
                }
            }
            for (int i = 0; i < count; i++)
            {
                int a = border + i*2, b = border + ((i+1)%count)*2;
                mesh.AddTriangle(a, b+1, b);
                mesh.AddTriangle(a, a+1, b+1);
            }
            AntialiasContour(outline.ToArray(), new Color(.65f,.58f,1f,.9f));
            innerBorder.Reverse();
            AntialiasContour(innerBorder.ToArray(), new Color(.65f,.58f,1f,.9f));
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

                Line(a.x,a.y,b.x,b.y,1.5f,new Color(.65f,.58f,1f,.9f));
            }
        }
    }
}
