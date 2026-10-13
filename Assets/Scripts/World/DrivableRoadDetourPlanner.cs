using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    // FCG Way links can cross an empty stretch between two otherwise valid
    // road nodes. Find an alternative using *actual* CityVisual road colliders
    // instead of treating a graph edge as proof that a car can drive there.
    // Invoked only for long disconnected legs at city installation.
    internal static class DrivableRoadDetourPlanner
    {
        private const float GridStep = 10f;
        private const float SegmentCheckStep = 3.5f;
        private const float MaximumLeg = 82f;
        private const int MaximumVisitedCells = 2600;
        private const float SearchMargin = 375f;
        private static readonly Vector2Int[] Directions =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(-1, 1), new(1, -1), new(-1, -1)
        };

        internal static bool TryFindWaypoints(Vector3 start, Vector3 end,
            out List<Vector3> waypoints)
        {
            waypoints = null;
            if (!CityAssetRuntimeInstaller.IsRoadSurface(start) ||
                !CityAssetRuntimeInstaller.IsRoadSurface(end))
                return false;

            Dictionary<Vector2Int, Vector3> road =
                new Dictionary<Vector2Int, Vector3>();
            HashSet<Vector2Int> invalid = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, float> best =
                new Dictionary<Vector2Int, float>();
            Dictionary<Vector2Int, Vector2Int> previous =
                new Dictionary<Vector2Int, Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            SortedDictionary<float, Queue<Vector2Int>> open =
                new SortedDictionary<float, Queue<Vector2Int>>();

            Vector2Int origin = Vector2Int.zero;
            road[origin] = start;
            best[origin] = 0f;
            Push(open, FlatDistance(start, end), origin);

            bool GetRoad(Vector2Int cell, out Vector3 position)
            {
                if (road.TryGetValue(cell, out position))
                    return true;
                if (invalid.Contains(cell))
                    return false;

                float x = start.x + cell.x * GridStep;
                float z = start.z + cell.y * GridStep;
                if (Mathf.Abs(x - start.x) > SearchMargin ||
                    Mathf.Abs(z - start.z) > SearchMargin ||
                    !CityAssetRuntimeInstaller.TryGetRoadPoint(
                        x, z, out position))
                {
                    invalid.Add(cell);
                    return false;
                }
                road[cell] = position;
                return true;
            }

            int steps = 0;
            while (open.Count != 0 && steps++ < MaximumVisitedCells)
            {
                Vector2Int current = Pop(open);
                if (!visited.Add(current))
                    continue;

                Vector3 point = road[current];
                if (FlatDistance(point, end) <= GridStep * 1.45f &&
                    IsPassable(point, end))
                {
                    List<Vector3> path = new List<Vector3>();
                    Vector2Int node = current;
                    path.Add(end);
                    path.Add(point);
                    while (previous.TryGetValue(node, out Vector2Int from))
                    {
                        node = from;
                        path.Add(road[node]);
                    }
                    path.Reverse();
                    if (path.Count > 0)
                        path[0] = start;
                    waypoints = Simplify(path);
                    return waypoints.Count >= 1;
                }

                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = current + direction;
                    if (visited.Contains(next) ||
                        !GetRoad(next, out Vector3 nextPoint) ||
                        !IsPassable(point, nextPoint))
                        continue;

                    float candidate = best[current] +
                        FlatDistance(point, nextPoint);
                    if (best.TryGetValue(next, out float old) &&
                        candidate >= old)
                        continue;

                    best[next] = candidate;
                    previous[next] = current;
                    Push(open, candidate + FlatDistance(nextPoint, end), next);
                }
            }
            return false;
        }

        // Only a physical road hit can authorize an A* edge. Check intermediate
        // samples as well so an edge cannot jump across a missing road patch.
        private static bool IsPassable(Vector3 a, Vector3 b)
        {
            float length = FlatDistance(a, b);
            if (length > 100f)
                return false;
            int count = Mathf.Max(1,
                Mathf.CeilToInt(length / SegmentCheckStep));
            for (int i = 1; i < count; i++)
            {
                float t = i / (float)count;
                float x = Mathf.Lerp(a.x, b.x, t);
                float z = Mathf.Lerp(a.z, b.z, t);
                if (!CityAssetRuntimeInstaller.TryGetRoadPoint(
                    x, z, out Vector3 hit) ||
                    Mathf.Abs(hit.y - Mathf.Lerp(a.y, b.y, t)) > 3f)
                    return false;
            }
            return true;
        }

        private static List<Vector3> Simplify(List<Vector3> raw)
        {
            List<Vector3> result = new List<Vector3>();
            if (raw.Count <= 2)
                return result;

            // Keep a checkpoint at each bend and at most 82 metres apart.
            // The simplifier must never cut a corner across an empty block.
            int anchor = 0;
            for (int i = 1; i < raw.Count; i++)
            {
                bool tooFar = FlatDistance(raw[anchor], raw[i]) > MaximumLeg;
                bool obstructed = !tooFar && !IsPassable(raw[anchor], raw[i]);
                if (!tooFar && !obstructed)
                    continue;

                int keep = i - 1;
                if (keep <= anchor)
                    keep = i;
                if (keep < raw.Count - 1)
                    result.Add(raw[keep]);
                anchor = keep;
            }
            Vector3 last = raw[raw.Count - 1];
            if (result.Count > 0 &&
                FlatDistance(result[result.Count - 1], last) < 22f)
            {
                Vector3 before = result.Count > 1
                    ? result[result.Count - 2] : raw[0];
                if (IsPassable(before, last))
                    result.RemoveAt(result.Count - 1);
            }
            return result;
        }

        private static void Push(
            SortedDictionary<float, Queue<Vector2Int>> open,
            float priority, Vector2Int point)
        {
            if (!open.TryGetValue(priority, out Queue<Vector2Int> nodes))
            {
                nodes = new Queue<Vector2Int>();
                open.Add(priority, nodes);
            }
            nodes.Enqueue(point);
        }

        private static Vector2Int Pop(
            SortedDictionary<float, Queue<Vector2Int>> open)
        {
            float first = 0f;
            Queue<Vector2Int> nodes = null;
            foreach (KeyValuePair<float, Queue<Vector2Int>> pair in open)
            {
                first = pair.Key;
                nodes = pair.Value;
                break;
            }
            Vector2Int point = nodes.Dequeue();
            if (nodes.Count == 0)
                open.Remove(first);
            return point;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float x = b.x - a.x;
            float z = b.z - a.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}
