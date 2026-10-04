using System;
namespace UnityEngine
{
    public static class Mathf
    {
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int FloorToInt(float value) => (int)Math.Floor(value);
    }
    public struct Rect { public float height; public float xMin, xMax, yMin, yMax; }
    public class Transform { }
    public class GameObject
    {
        public object component;
        public T AddComponent<T>() where T : new() { component = new T(); return (T)component; }
    }
    public class DisallowMultipleComponent : Attribute { }
    public class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z = 0) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public struct Color32 { public static Color32 Lerp(Color32 a, Color32 b, float t) => a; }
    public struct UIVertex { public Vector3 position, uv0; public Color32 color; }
    public class RectTransform : Transform { public Rect rect; }
}
namespace UnityEngine.UI
{
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public class BaseMeshEffect
    {
        public UnityEngine.Transform transform;
        public bool IsActive() => true;
        public virtual void ModifyMesh(VertexHelper mesh) { }
    }
    public class VertexHelper
    {
        public readonly System.Collections.Generic.List<UnityEngine.UIVertex> vertices = new();
        public int currentVertCount => vertices.Count;
        public void GetUIVertexStream(System.Collections.Generic.List<UnityEngine.UIVertex> result) => result.AddRange(vertices);
        public void Clear() => vertices.Clear();
        public void AddUIVertexTriangleStream(System.Collections.Generic.List<UnityEngine.UIVertex> input) => vertices.AddRange(input);
    }
    public class Text
    {
        public UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
        public T GetComponent<T>() where T : class => gameObject.component as T;
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
            text.verticalOverflow != UnityEngine.UI.VerticalWrapMode.Overflow)
            throw new Exception("Text must stay within its allocated cell.");
        object clip = text.gameObject.component;
        if (!(clip is MotorCity.UI.MotorCityTextClip)) throw new Exception("Text needs geometric bounds.");
        foreach (string value in new[] { "0", "999999", "A long changed value", "" })
        {
            text.text = value;
            MotorCity.UI.MotorCityTextLayout.Configure(text);
            if (text.fontSize != 21 || text.rectTransform.rect.height != 28 || text.gameObject.component != clip)
                throw new Exception("Changing content must not move cells or resize fonts.");
        }
        text.fontSize = 24;
        text.rectTransform.rect = new UnityEngine.Rect { height = 88 };
        MotorCity.UI.MotorCityTextLayout.Configure(text);
        if (text.fontSize != 24) throw new Exception("Paragraph cells must retain their designed readable font.");
        CheckMeshClip();
    }
    private static void CheckMeshClip()
    {
        var clip = new MotorCity.UI.MotorCityTextClip();
        clip.transform = new UnityEngine.RectTransform { rect = new UnityEngine.Rect { xMin=0, yMin=0, xMax=10, yMax=10 } };
        var mesh = new UnityEngine.UI.VertexHelper();
        foreach (var point in new[] { new UnityEngine.Vector3(-5, 5), new UnityEngine.Vector3(5, 15), new UnityEngine.Vector3(15, -5) })
            mesh.vertices.Add(new UnityEngine.UIVertex { position=point, uv0=point });
        clip.ModifyMesh(mesh);
        if (mesh.vertices.Count == 0 || mesh.vertices.Count % 3 != 0) throw new Exception("Visible glyph geometry must survive clipping.");
        foreach (var vertex in mesh.vertices)
        {
            var p = vertex.position;
            if (p.x < -0.0001 || p.x > 10.0001 || p.y < -0.0001 || p.y > 10.0001)
                throw new Exception("Clipped glyphs must stay inside their cell.");
            if (Math.Abs(p.x-vertex.uv0.x) > 0.0001 || Math.Abs(p.y-vertex.uv0.y) > 0.0001)
                throw new Exception("Clipping must interpolate glyph texture coordinates.");
        }
        mesh.Clear();
        foreach (var point in new[] { new UnityEngine.Vector3(20, 20), new UnityEngine.Vector3(20, 30), new UnityEngine.Vector3(30, 20) })
            mesh.vertices.Add(new UnityEngine.UIVertex { position=point });
        clip.ModifyMesh(mesh);
        if (mesh.vertices.Count != 0) throw new Exception("Outside geometry must not overlap neighbours.");
    }

}
