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
                position = default;
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
                    if (TrySimplify(path, out List<Vector3> simplified))
                    {
                        waypoints = simplified;
                        return true;
                    }

                    // The A* corridor is drivable, but an unavoidable
                    // sub-24m checkpoint may remain at a tight corner.
                    // Try another corridor instead of emitting an invalid
                    // series of markers that fails the gameplay audit.
                    continue;
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

        // One road validation contract for search, simplification, insertion
        // and Unity's marker audit. A* already uses validated endpoints; its
        // former interior-only test disagreed with the final endpoint test.
        private static bool IsPassable(Vector3 a, Vector3 b)
        {
            return CityAssetRuntimeInstaller.HasDrivableAsphaltBetween(a, b);
        }

        // Dynamic programming selects a chain of checkpoint anchors from the
        // fully validated A* polyline. It never rounds a corner across an
        // off-road shortcut and rejects <24m "micro-checkpoints" before
        // passing anything back to the city loader.
        private static bool TrySimplify(List<Vector3> raw,
            out List<Vector3> result)
        {
            result = null;
            if (raw == null || raw.Count < 2)
                return false;

            const float MinimumLeg = 24f;
            const float PreferredLeg = 65f;
            const int LookAhead = 18;
            int count = raw.Count;
            float[] cost = new float[count];
            int[] parent = new int[count];

            for (int i = 0; i < count; i++)
            {
                cost[i] = float.PositiveInfinity;
                parent[i] = -1;
            }
            cost[0] = 0f;

            for (int end = 1; end < count; end++)
            {
                int begin = Mathf.Max(0, end - LookAhead);
                for (int start = begin; start < end; start++)
                {
                    if (float.IsInfinity(cost[start]))
                        continue;

                    float length = FlatDistance(raw[start], raw[end]);
                    if (length < MinimumLeg || length > MaximumLeg)
                        continue;

                    if (!IsPassable(raw[start], raw[end]))
                        continue;

                    // Prefer fewer markers and normal 50-80m spacing.
                    // Each edge is tested against the real asphalt collider.
                    float nextCost = cost[start] + 1f +
                        Mathf.Abs(length - PreferredLeg) * 0.001f;
                    if (nextCost < cost[end])
                    {
                        cost[end] = nextCost;
                        parent[end] = start;
                    }
                }
            }

            if (parent[count - 1] < 0)
                return false;

            List<Vector3> reversed = new List<Vector3>();
            int current = count - 1;
            while (parent[current] > 0)
            {
                current = parent[current];
                reversed.Add(raw[current]);
            }
            reversed.Reverse();

            // Only intermediate points are returned; city installation adds
            // the existing start and finish exactly once.
            result = reversed;
            return true;
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
