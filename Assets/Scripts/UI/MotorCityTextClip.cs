using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    // Legacy Text can discard a whole line with VerticalWrapMode.Truncate at
    // fractional canvas scales. Generate the line first and clip its geometry.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class MotorCityTextClip : BaseMeshEffect
    {
        private readonly List<UIVertex> source = new();
        private readonly List<UIVertex> output = new();
        private readonly List<UIVertex> polygon = new(8);
        private readonly List<UIVertex> scratch = new(8);

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || mesh.currentVertCount == 0) return;
            Rect bounds = ((RectTransform)transform).rect;
            source.Clear(); output.Clear();
            mesh.GetUIVertexStream(source);
            for (int i = 0; i + 2 < source.Count; i += 3)
            {
                polygon.Clear();
                polygon.Add(source[i]); polygon.Add(source[i + 1]); polygon.Add(source[i + 2]);
                for (int edge = 0; edge < 4 && polygon.Count > 0; edge++)
                    ClipEdge(bounds, edge);
                for (int vertex = 1; vertex + 1 < polygon.Count; vertex++)
                {
                    output.Add(polygon[0]); output.Add(polygon[vertex]); output.Add(polygon[vertex + 1]);
                }
            }
            mesh.Clear();
            mesh.AddUIVertexTriangleStream(output);
        }

        private void ClipEdge(Rect bounds, int edge)
        {
            scratch.Clear();
            UIVertex previous = polygon[polygon.Count - 1];
            float previousDistance = Distance(previous.position, bounds, edge);
            foreach (UIVertex current in polygon)
            {
                float distance = Distance(current.position, bounds, edge);
                if ((previousDistance >= 0f) != (distance >= 0f))
                {
                    float t = previousDistance / (previousDistance - distance);
                    UIVertex intersection = previous;
                    intersection.position = previous.position + (current.position - previous.position) * t;
                    intersection.uv0 = previous.uv0 + (current.uv0 - previous.uv0) * t;
                    intersection.color = Color32.Lerp(previous.color, current.color, t);
                    scratch.Add(intersection);
                }
                if (distance >= 0f) scratch.Add(current);
                previous = current; previousDistance = distance;
            }
            polygon.Clear(); polygon.AddRange(scratch);
        }

        private static float Distance(Vector3 point, Rect bounds, int edge)
        {
            return edge switch
            {
                0 => point.x - bounds.xMin,
                1 => bounds.xMax - point.x,
                2 => point.y - bounds.yMin,
                _ => bounds.yMax - point.y
            };
        }
    }
}
