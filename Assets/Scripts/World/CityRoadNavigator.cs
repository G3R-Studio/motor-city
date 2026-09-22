using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    public static class CityRoadNavigator
    {
        private const float MergeDistance = 8f;
        private const float AxisTolerance = 24f;

        public static List<Vector3> BuildRoute(
            Vector3 start,
            Vector3 destination)
        {
            List<Vector3> nodes = new();
            List<Edge> edges = new();

            AddRoute(
                CityAssetRuntimeInstaller.DeliveryRoute,
                nodes,
                edges);

            AddRoute(
                CityAssetRuntimeInstaller.SprintRoute,
                nodes,
                edges);

            AddRoute(
                CityAssetRuntimeInstaller.CircuitRoute,
                nodes,
                edges);

            if (nodes.Count == 0)
            {
                return new List<Vector3>
                {
                    start,
                    destination
                };
            }

            ConnectAlignedRoads(
                nodes,
                edges);

            int startNode =
                NearestNode(
                    start,
                    nodes);

            int endNode =
                NearestNode(
                    destination,
                    nodes);

            List<int> nodePath =
                FindShortestPath(
                    startNode,
                    endNode,
                    nodes,
                    edges);

            List<Vector3> result =
                new();

            result.Add(start);

            foreach (int index in nodePath)
            {
                Vector3 point =
                    nodes[index];

                if (FlatDistance(
                        result[result.Count - 1],
                        point) >
                    2f)
                {
                    result.Add(point);
                }
            }

            if (FlatDistance(
                    result[result.Count - 1],
                    destination) >
                2f)
            {
                result.Add(destination);
            }

            return result;
        }

        private static void AddRoute(
            Vector3[] route,
            List<Vector3> nodes,
            List<Edge> edges)
        {
            if (route == null ||
                route.Length == 0)
            {
                return;
            }

            int previous =
                -1;

            foreach (Vector3 point in route)
            {
                int current =
                    FindOrAddNode(
                        point,
                        nodes);

                if (previous >= 0 &&
                    previous != current)
                {
                    AddEdge(
                        previous,
                        current,
                        nodes,
                        edges);
                }

                previous =
                    current;
            }
        }

        private static int FindOrAddNode(
            Vector3 point,
            List<Vector3> nodes)
        {
            for (int i = 0;
                 i < nodes.Count;
                 i++)
            {
                if (FlatDistance(
                        point,
                        nodes[i]) <=
                    MergeDistance)
                {
                    return i;
                }
            }

            nodes.Add(point);
            return nodes.Count - 1;
        }

        private static void ConnectAlignedRoads(
            List<Vector3> nodes,
            List<Edge> edges)
        {
            for (int i = 0;
                 i < nodes.Count;
                 i++)
            {
                int nearestX =
                    -1;
                int nearestZ =
                    -1;

                float bestX =
                    float.PositiveInfinity;
                float bestZ =
                    float.PositiveInfinity;

                for (int j = 0;
                     j < nodes.Count;
                     j++)
                {
                    if (i == j)
                        continue;

                    Vector3 a =
                        nodes[i];

                    Vector3 b =
                        nodes[j];

                    float dx =
                        Mathf.Abs(
                            a.x - b.x);

                    float dz =
                        Mathf.Abs(
                            a.z - b.z);

                    float distance =
                        FlatDistance(
                            a,
                            b);

                    if (dx <= AxisTolerance &&
                        dz > AxisTolerance &&
                        distance < bestX)
                    {
                        bestX =
                            distance;
                        nearestX =
                            j;
                    }

                    if (dz <= AxisTolerance &&
                        dx > AxisTolerance &&
                        distance < bestZ)
                    {
                        bestZ =
                            distance;
                        nearestZ =
                            j;
                    }
                }

                if (nearestX >= 0)
                {
                    AddEdge(
                        i,
                        nearestX,
                        nodes,
                        edges);
                }

                if (nearestZ >= 0)
                {
                    AddEdge(
                        i,
                        nearestZ,
                        nodes,
                        edges);
                }
            }
        }

        private static void AddEdge(
            int a,
            int b,
            List<Vector3> nodes,
            List<Edge> edges)
        {
            if (a == b)
                return;

            int low =
                Mathf.Min(
                    a,
                    b);

            int high =
                Mathf.Max(
                    a,
                    b);

            foreach (Edge edge in edges)
            {
                if (edge.A == low &&
                    edge.B == high)
                {
                    return;
                }
            }

            edges.Add(
                new Edge(
                    low,
                    high,
                    FlatDistance(
                        nodes[low],
                        nodes[high])));
        }

        private static int NearestNode(
            Vector3 position,
            List<Vector3> nodes)
        {
            int bestIndex =
                0;
            float best =
                float.PositiveInfinity;

            for (int i = 0;
                 i < nodes.Count;
                 i++)
            {
                float distance =
                    FlatDistance(
                        position,
                        nodes[i]);

                if (distance >= best)
                    continue;

                best =
                    distance;
                bestIndex =
                    i;
            }

            return bestIndex;
        }

        private static List<int> FindShortestPath(
            int start,
            int end,
            List<Vector3> nodes,
            List<Edge> edges)
        {
            int count =
                nodes.Count;

            float[] distance =
                new float[count];

            int[] previous =
                new int[count];

            bool[] visited =
                new bool[count];

            for (int i = 0;
                 i < count;
                 i++)
            {
                distance[i] =
                    float.PositiveInfinity;
                previous[i] =
                    -1;
            }

            distance[start] =
                0f;

            for (int step = 0;
                 step < count;
                 step++)
            {
                int current =
                    -1;

                float best =
                    float.PositiveInfinity;

                for (int i = 0;
                     i < count;
                     i++)
                {
                    if (!visited[i] &&
                        distance[i] < best)
                    {
                        best =
                            distance[i];
                        current =
                            i;
                    }
                }

                if (current < 0 ||
                    current == end)
                {
                    break;
                }

                visited[current] =
                    true;

                foreach (Edge edge in edges)
                {
                    int neighbor =
                        edge.A == current
                            ? edge.B
                            : edge.B == current
                                ? edge.A
                                : -1;

                    if (neighbor < 0 ||
                        visited[neighbor])
                    {
                        continue;
                    }

                    float candidate =
                        distance[current] +
                        edge.Cost;

                    if (candidate <
                        distance[neighbor])
                    {
                        distance[neighbor] =
                            candidate;
                        previous[neighbor] =
                            current;
                    }
                }
            }

            List<int> path =
                new();

            int cursor =
                end;

            while (cursor >= 0)
            {
                path.Add(cursor);

                if (cursor == start)
                    break;

                cursor =
                    previous[cursor];
            }

            if (path[path.Count - 1] !=
                start)
            {
                return new List<int>
                {
                    start,
                    end
                };
            }

            path.Reverse();
            return path;
        }

        private static float FlatDistance(
            Vector3 a,
            Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;

            return
                Vector3.Distance(
                    a,
                    b);
        }

        private readonly struct Edge
        {
            public readonly int A;
            public readonly int B;
            public readonly float Cost;

            public Edge(
                int a,
                int b,
                float cost)
            {
                A = a;
                B = b;
                Cost = cost;
            }
        }
    }
}
