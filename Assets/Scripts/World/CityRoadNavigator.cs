using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    public static class CityRoadNavigator
    {
        private const float MergeDistance =
            2.5f;

        private const float IntersectionDistance =
            16f;

        private static readonly List<Vector3> Nodes =
            new();

        private static readonly List<Edge> Edges =
            new();

        private static bool graphReady;
        private static ulong cachedSceneHandle =
            ulong.MaxValue;

        public static List<Vector3> BuildRoute(
            Vector3 start,
            Vector3 destination)
        {
            EnsureGraph();

            if (Nodes.Count == 0)
            {
                return new List<Vector3>
                {
                    start,
                    destination
                };
            }

            int startNode =
                NearestNode(
                    start,
                    Nodes);

            int endNode =
                NearestNode(
                    destination,
                    Nodes);

            List<int> nodePath =
                FindShortestPath(
                    startNode,
                    endNode,
                    Nodes,
                    Edges);

            List<Vector3> result =
                new()
                {
                    start
                };

            foreach (int index in nodePath)
            {
                Vector3 point =
                    Nodes[index];

                if (FlatDistance(
                        result[result.Count - 1],
                        point) >
                    1.5f)
                {
                    result.Add(
                        point);
                }
            }

            if (FlatDistance(
                    result[result.Count - 1],
                    destination) >
                1.5f)
            {
                result.Add(
                    destination);
            }

            return result;
        }

        private static void EnsureGraph()
        {
            ulong sceneHandle =
                UnityEngine.SceneManagement.SceneManager
                    .GetActiveScene()
                    .handle
                    .GetRawData();

            if (graphReady &&
                cachedSceneHandle ==
                sceneHandle)
            {
                return;
            }

            Nodes.Clear();
            Edges.Clear();

            cachedSceneHandle =
                sceneHandle;

            bool builtFromTraffic =
                BuildFromTrafficWays();

            if (!builtFromTraffic)
            {
                BuildFallbackGraph();
            }

            graphReady =
                true;
        }

        private static bool BuildFromTrafficWays()
        {
            Transform[] transforms =
                UnityEngine.Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Dictionary<int, List<WayPoint>> groups =
                new();

            foreach (Transform item in
                     transforms)
            {
                if (item == null ||
                    !TryParseWayName(
                        item.name,
                        out int group,
                        out int order))
                {
                    continue;
                }

                if (!groups.TryGetValue(
                        group,
                        out List<WayPoint> points))
                {
                    points =
                        new List<WayPoint>();

                    groups.Add(
                        group,
                        points);
                }

                points.Add(
                    new WayPoint(
                        group,
                        order,
                        item.position));
            }

            if (groups.Count == 0)
            {
                Debug.LogWarning(
                    "Motor City navigator: FCG Way points were not found. " +
                    "Using the fallback gameplay-road graph.");

                return false;
            }

            Dictionary<WayKey, int> nodeByWay =
                new();

            foreach (KeyValuePair<int, List<WayPoint>> pair in
                     groups)
            {
                List<WayPoint> points =
                    pair.Value;

                points.Sort(
                    (a, b) =>
                        a.Order.CompareTo(
                            b.Order));

                int previousNode =
                    -1;

                foreach (WayPoint point in
                         points)
                {
                    int node =
                        FindOrAddNode(
                            point.Position,
                            Nodes);

                    nodeByWay[
                        new WayKey(
                            point.Group,
                            point.Order)] =
                        node;

                    if (previousNode >= 0 &&
                        previousNode != node)
                    {
                        AddEdge(
                            previousNode,
                            node,
                            Nodes,
                            Edges);
                    }

                    previousNode =
                        node;
                }
            }

            ConnectTrafficIntersections(
                groups,
                nodeByWay);

            Debug.Log(
                "Motor City navigator: built from FCG traffic Ways. " +
                $"Groups={groups.Count}, Nodes={Nodes.Count}, Edges={Edges.Count}.");

            return
                Nodes.Count >= 2 &&
                Edges.Count >= 1;
        }

        private static void ConnectTrafficIntersections(
            Dictionary<int, List<WayPoint>> groups,
            Dictionary<WayKey, int> nodeByWay)
        {
            List<WayPoint> all =
                new();

            foreach (List<WayPoint> group in
                     groups.Values)
            {
                all.AddRange(
                    group);
            }

            float maximumSquared =
                IntersectionDistance *
                IntersectionDistance;

            for (int i = 0;
                 i < all.Count;
                 i++)
            {
                WayPoint a =
                    all[i];

                for (int j = i + 1;
                     j < all.Count;
                     j++)
                {
                    WayPoint b =
                        all[j];

                    if (a.Group ==
                        b.Group)
                    {
                        continue;
                    }

                    Vector3 delta =
                        a.Position -
                        b.Position;

                    delta.y =
                        0f;

                    if (delta.sqrMagnitude >
                        maximumSquared)
                    {
                        continue;
                    }

                    if (!nodeByWay.TryGetValue(
                            new WayKey(
                                a.Group,
                                a.Order),
                            out int aNode) ||
                        !nodeByWay.TryGetValue(
                            new WayKey(
                                b.Group,
                                b.Order),
                            out int bNode))
                    {
                        continue;
                    }

                    AddEdge(
                        aNode,
                        bNode,
                        Nodes,
                        Edges);
                }
            }
        }

        private static bool TryParseWayName(
            string name,
            out int group,
            out int order)
        {
            group =
                -1;

            order =
                -1;

            if (string.IsNullOrWhiteSpace(
                    name))
            {
                return false;
            }

            string trimmed =
                name.Trim();

            int wayIndex =
                trimmed.IndexOf(
                    "Way",
                    StringComparison.OrdinalIgnoreCase);

            int open =
                trimmed.IndexOf(
                    '(',
                    wayIndex >= 0
                        ? wayIndex
                        : 0);

            int close =
                open >= 0
                    ? trimmed.IndexOf(
                        ')',
                        open + 1)
                    : -1;

            int dash =
                close >= 0
                    ? trimmed.IndexOf(
                        '-',
                        close + 1)
                    : -1;

            if (wayIndex < 0 ||
                open < 0 ||
                close <= open + 1 ||
                dash < 0 ||
                dash >= trimmed.Length - 1)
            {
                return false;
            }

            string groupText =
                trimmed.Substring(
                        open + 1,
                        close - open - 1)
                    .Trim();

            string orderText =
                trimmed.Substring(
                        dash + 1)
                    .Trim();

            return
                int.TryParse(
                    groupText,
                    out group) &&
                int.TryParse(
                    orderText,
                    out order);
        }

        private static void BuildFallbackGraph()
        {
            AddRoute(
                CityAssetRuntimeInstaller.DeliveryRoute,
                Nodes,
                Edges);

            AddRoute(
                CityAssetRuntimeInstaller.SprintRoute,
                Nodes,
                Edges);

            AddRoute(
                CityAssetRuntimeInstaller.CircuitRoute,
                Nodes,
                Edges);

            ConnectNearestFallbackRoads(
                Nodes,
                Edges);
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

            foreach (Vector3 point in
                     route)
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

            nodes.Add(
                point);

            return
                nodes.Count - 1;
        }

        private static void ConnectNearestFallbackRoads(
            List<Vector3> nodes,
            List<Edge> edges)
        {
            for (int i = 0;
                 i < nodes.Count;
                 i++)
            {
                int nearest =
                    -1;

                float best =
                    175f;

                for (int j = 0;
                     j < nodes.Count;
                     j++)
                {
                    if (i == j)
                        continue;

                    float distance =
                        FlatDistance(
                            nodes[i],
                            nodes[j]);

                    if (distance >= best)
                        continue;

                    best =
                        distance;
                    nearest =
                        j;
                }

                if (nearest >= 0)
                {
                    AddEdge(
                        i,
                        nearest,
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

            foreach (Edge edge in
                     edges)
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

                foreach (Edge edge in
                         edges)
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
                path.Add(
                    cursor);

                if (cursor ==
                    start)
                {
                    break;
                }

                cursor =
                    previous[cursor];
            }

            if (path.Count == 0 ||
                path[path.Count - 1] !=
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
            a.y =
                0f;

            b.y =
                0f;

            return
                Vector3.Distance(
                    a,
                    b);
        }

        private readonly struct WayPoint
        {
            public readonly int Group;
            public readonly int Order;
            public readonly Vector3 Position;

            public WayPoint(
                int group,
                int order,
                Vector3 position)
            {
                Group =
                    group;

                Order =
                    order;

                Position =
                    position;
            }
        }

        private readonly struct WayKey :
            IEquatable<WayKey>
        {
            public readonly int Group;
            public readonly int Order;

            public WayKey(
                int group,
                int order)
            {
                Group =
                    group;

                Order =
                    order;
            }

            public bool Equals(
                WayKey other)
            {
                return
                    Group ==
                        other.Group &&
                    Order ==
                        other.Order;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is WayKey other &&
                    Equals(
                        other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return
                        Group * 397 ^
                        Order;
                }
            }
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
                A =
                    a;

                B =
                    b;

                Cost =
                    cost;
            }
        }
    }
}
