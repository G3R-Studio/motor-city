using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MotorCity.World
{
    public static class CityRoadNavigator
    {
        private const float MergeDistance =
            1.5f;

        private const float MaximumAuthoredLinkDistance =
            90f;

        private static readonly List<Vector3> Nodes =
            new();

        private static readonly List<Edge> Edges =
            new();

        private static readonly HashSet<ulong> EdgeKeys =
            new();

        private static readonly Dictionary<Vector2Int, List<int>> NodeBuckets =
            new();

        private static readonly Dictionary<int, List<Edge>> Adjacency =
            new();

        private static float[] distanceBuffer =
            Array.Empty<float>();

        private static int[] previousBuffer =
            Array.Empty<int>();

        private static bool[] visitedBuffer =
            Array.Empty<bool>();

        private static readonly Dictionary<MonoBehaviour, WayNetworkEntry>
            WayEntries =
                new();

        private static bool graphReady;

        private static ulong cachedSceneHandle =
            ulong.MaxValue;

        public static List<Vector3> BuildRoute(
            Vector3 start,
            Vector3 destination)
        {
            EnsureGraph();

            if (Nodes.Count == 0 ||
                Edges.Count == 0)
            {
                return new List<Vector3>();
            }

            RoadProjection startProjection =
                FindNearestRoadProjection(
                    start);

            RoadProjection endProjection =
                FindNearestRoadProjection(
                    destination);

            if (!startProjection.IsValid ||
                !endProjection.IsValid)
            {
                return new List<Vector3>();
            }

            RouteCandidate best =
                FindBestProjectedRoute(
                    startProjection,
                    endProjection);

            if (!best.IsValid)
            {
                return new List<Vector3>();
            }

            List<Vector3> result =
                new();

            AppendIfDistinct(
                result,
                startProjection.Position);

            foreach (int index in
                     best.NodePath)
            {
                AppendIfDistinct(
                    result,
                    Nodes[index]);
            }

            AppendIfDistinct(
                result,
                endProjection.Position);

            return result;
        }

        private static RouteCandidate FindBestProjectedRoute(
            RoadProjection start,
            RoadProjection end)
        {
            if (start.EdgeIndex ==
                end.EdgeIndex)
            {
                return new RouteCandidate(
                    new List<int>(),
                    FlatDistance(
                        start.Position,
                        end.Position));
            }

            RouteCandidate best =
                RouteCandidate.Invalid;

            int[] startNodes =
            {
                start.A,
                start.B
            };

            int[] endNodes =
            {
                end.A,
                end.B
            };

            foreach (int startNode in
                     startNodes)
            {
                foreach (int endNode in
                         endNodes)
                {
                    List<int> path =
                        FindShortestPath(
                            startNode,
                            endNode,
                            Nodes,
                            Edges);

                    if (path == null ||
                        path.Count == 0)
                    {
                        continue;
                    }

                    float cost =
                        FlatDistance(
                            start.Position,
                            Nodes[startNode]) +
                        PathCost(
                            path) +
                        FlatDistance(
                            Nodes[endNode],
                            end.Position);

                    if (!best.IsValid ||
                        cost < best.Cost)
                    {
                        best =
                            new RouteCandidate(
                                path,
                                cost);
                    }
                }
            }

            return best;
        }

        private static float PathCost(
            List<int> path)
        {
            float cost =
                0f;

            for (int i = 1;
                 i < path.Count;
                 i++)
            {
                cost +=
                    FlatDistance(
                        Nodes[path[i - 1]],
                        Nodes[path[i]]);
            }

            return cost;
        }

        private static RoadProjection FindNearestRoadProjection(
            Vector3 position)
        {
            RoadProjection best =
                RoadProjection.Invalid;

            float bestSquared =
                float.PositiveInfinity;

            for (int i = 0;
                 i < Edges.Count;
                 i++)
            {
                Edge edge =
                    Edges[i];

                if (!edge.IsRoadSegment)
                    continue;

                Vector3 projected =
                    ProjectToSegmentFlat(
                        position,
                        Nodes[edge.A],
                        Nodes[edge.B]);

                Vector3 delta =
                    projected -
                    position;

                delta.y =
                    0f;

                float squared =
                    delta.sqrMagnitude;

                if (squared >= bestSquared)
                    continue;

                bestSquared =
                    squared;

                best =
                    new RoadProjection(
                        i,
                        edge.A,
                        edge.B,
                        projected);
            }

            return best;
        }

        private static Vector3 ProjectToSegmentFlat(
            Vector3 point,
            Vector3 a,
            Vector3 b)
        {
            Vector2 p =
                new(
                    point.x,
                    point.z);

            Vector2 av =
                new(
                    a.x,
                    a.z);

            Vector2 bv =
                new(
                    b.x,
                    b.z);

            Vector2 ab =
                bv -
                av;

            float lengthSquared =
                ab.sqrMagnitude;

            float t =
                lengthSquared <= 0.0001f
                    ? 0f
                    : Mathf.Clamp01(
                        Vector2.Dot(
                            p - av,
                            ab) /
                        lengthSquared);

            return new Vector3(
                Mathf.Lerp(
                    a.x,
                    b.x,
                    t),
                Mathf.Lerp(
                    a.y,
                    b.y,
                    t),
                Mathf.Lerp(
                    a.z,
                    b.z,
                    t));
        }

        private static void AppendIfDistinct(
            List<Vector3> points,
            Vector3 point)
        {
            if (points.Count == 0 ||
                FlatDistance(
                    points[
                        points.Count - 1],
                    point) >
                1.25f)
            {
                points.Add(
                    point);
            }
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
            EdgeKeys.Clear();
            NodeBuckets.Clear();
            Adjacency.Clear();
            WayEntries.Clear();

            cachedSceneHandle =
                sceneHandle;

            BuildFromFcgAuthoredNetwork();

            graphReady =
                true;
        }

        private static bool BuildFromFcgAuthoredNetwork()
        {
            GameObject cityRoot =
                GameObject.Find(
                    "MotorCity_FCGCity") ??
                GameObject.Find(
                    "City-Maker");

            if (cityRoot == null)
            {
                Debug.LogWarning(
                    "Motor City navigator: authored city root was not found. " +
                    "Road navigation is disabled.");

                return false;
            }

            MonoBehaviour[] behaviours =
                cityRoot.GetComponentsInChildren<MonoBehaviour>(
                    true);

            foreach (MonoBehaviour behaviour in
                     behaviours)
            {
                if (behaviour == null ||
                    !LooksLikeFcgWayContainer(
                        behaviour))
                {
                    continue;
                }

                List<Transform> waypoints =
                    ReadTransformList(
                        behaviour,
                        "waypoints");

                if (waypoints.Count < 2)
                    continue;

                WayNetworkEntry entry =
                    new(
                        behaviour,
                        waypoints);

                WayEntries[
                    behaviour] =
                    entry;

                int previous =
                    -1;

                foreach (Transform waypoint in
                         waypoints)
                {
                    if (waypoint == null)
                        continue;

                    int current =
                        FindOrAddNode(
                            waypoint.position,
                            Nodes);

                    entry.NodeIndices.Add(
                        current);

                    if (previous >= 0 &&
                        previous != current)
                    {
                        AddEdge(
                            previous,
                            current,
                            Nodes,
                            Edges,
                            true);
                    }

                    previous =
                        current;
                }
            }

            if (WayEntries.Count == 0)
            {
                Debug.LogWarning(
                    "Motor City navigator: FCG authored Way containers were not found. " +
                    "Road navigation is disabled to avoid drawing an invalid route.");

                return false;
            }

            foreach (WayNetworkEntry entry in
                     WayEntries.Values)
            {
                ConnectAuthoredLinks(
                    entry,
                    "nextWay0",
                    true);

                ConnectAuthoredLinks(
                    entry,
                    "nextWay1",
                    false);
            }

            return
                Nodes.Count >= 2 &&
                Edges.Count >= 1;
        }

        private static bool LooksLikeFcgWayContainer(
            MonoBehaviour behaviour)
        {
            Type type =
                behaviour.GetType();

            return
                FindField(
                    type,
                    "waypoints") != null &&
                FindField(
                    type,
                    "nextWay0") != null &&
                FindField(
                    type,
                    "nextWay1") != null;
        }

        private static void ConnectAuthoredLinks(
            WayNetworkEntry source,
            string fieldName,
            bool sourceAtFirstWaypoint)
        {
            if (source == null ||
                source.NodeIndices.Count == 0)
            {
                return;
            }

            int sourceNode =
                sourceAtFirstWaypoint
                    ? source.NodeIndices[0]
                    : source.NodeIndices[
                        source.NodeIndices.Count - 1];

            IEnumerable linked =
                ReadEnumerable(
                    source.Component,
                    fieldName);

            if (linked == null)
                return;

            foreach (object item in
                     linked)
            {
                MonoBehaviour targetComponent =
                    item as MonoBehaviour;

                if (targetComponent == null &&
                    item is Component component)
                {
                    targetComponent =
                        component as MonoBehaviour;
                }

                if (targetComponent == null ||
                    !WayEntries.TryGetValue(
                        targetComponent,
                        out WayNetworkEntry target) ||
                    target.NodeIndices.Count == 0)
                {
                    continue;
                }

                int first =
                    target.NodeIndices[0];

                int last =
                    target.NodeIndices[
                        target.NodeIndices.Count - 1];

                float firstDistance =
                    FlatDistance(
                        Nodes[sourceNode],
                        Nodes[first]);

                float lastDistance =
                    FlatDistance(
                        Nodes[sourceNode],
                        Nodes[last]);

                int targetNode =
                    firstDistance <=
                        lastDistance
                        ? first
                        : last;

                float distance =
                    Mathf.Min(
                        firstDistance,
                        lastDistance);

                if (distance >
                    MaximumAuthoredLinkDistance)
                {
                    continue;
                }

                AddEdge(
                    sourceNode,
                    targetNode,
                    Nodes,
                    Edges,
                    false);
            }
        }

        private static List<Transform> ReadTransformList(
            MonoBehaviour behaviour,
            string fieldName)
        {
            List<Transform> result =
                new();

            IEnumerable enumerable =
                ReadEnumerable(
                    behaviour,
                    fieldName);

            if (enumerable == null)
                return result;

            foreach (object item in
                     enumerable)
            {
                switch (item)
                {
                    case Transform transform:
                        result.Add(
                            transform);
                        break;

                    case GameObject gameObject:
                        result.Add(
                            gameObject.transform);
                        break;

                    case Component component:
                        result.Add(
                            component.transform);
                        break;
                }
            }

            return result;
        }

        private static IEnumerable ReadEnumerable(
            MonoBehaviour behaviour,
            string fieldName)
        {
            if (behaviour == null)
                return null;

            FieldInfo field =
                FindField(
                    behaviour.GetType(),
                    fieldName);

            if (field == null)
                return null;

            object value;

            try
            {
                value =
                    field.GetValue(
                        behaviour);
            }
            catch
            {
                return null;
            }

            return
                value as IEnumerable;
        }

        private static FieldInfo FindField(
            Type type,
            string fieldName)
        {
            while (type != null)
            {
                FieldInfo field =
                    type.GetField(
                        fieldName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                if (field != null)
                    return field;

                type =
                    type.BaseType;
            }

            return null;
        }

        private static int FindOrAddNode(
            Vector3 point,
            List<Vector3> nodes)
        {
            Vector2Int cell =
                NodeCell(
                    point);

            int bestIndex =
                int.MaxValue;

            for (int y = -1;
                 y <= 1;
                 y++)
            {
                for (int x = -1;
                     x <= 1;
                     x++)
                {
                    Vector2Int neighbor =
                        new(
                            cell.x + x,
                            cell.y + y);

                    if (!NodeBuckets.TryGetValue(
                            neighbor,
                            out List<int> indices))
                    {
                        continue;
                    }

                    foreach (int index in
                             indices)
                    {
                        if (index >=
                            bestIndex)
                        {
                            continue;
                        }

                        if (FlatDistance(
                                point,
                                nodes[index]) <=
                            MergeDistance)
                        {
                            bestIndex =
                                index;
                        }
                    }
                }
            }

            if (bestIndex !=
                int.MaxValue)
            {
                return
                    bestIndex;
            }

            int newIndex =
                nodes.Count;

            nodes.Add(
                point);

            if (!NodeBuckets.TryGetValue(
                    cell,
                    out List<int> bucket))
            {
                bucket =
                    new List<int>();

                NodeBuckets[
                    cell] =
                    bucket;
            }

            bucket.Add(
                newIndex);

            return
                newIndex;
        }

        private static Vector2Int NodeCell(
            Vector3 point)
        {
            return
                new Vector2Int(
                    Mathf.FloorToInt(
                        point.x /
                        MergeDistance),
                    Mathf.FloorToInt(
                        point.z /
                        MergeDistance));
        }

        private static void AddEdge(
            int a,
            int b,
            List<Vector3> nodes,
            List<Edge> edges,
            bool roadSegment)
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

            ulong edgeKey =
                ((ulong)(uint)low << 32) |
                (uint)high;

            if (!EdgeKeys.Add(
                    edgeKey))
            {
                return;
            }

            Edge edge =
                new(
                    low,
                    high,
                    FlatDistance(
                        nodes[low],
                        nodes[high]),
                    roadSegment);

            edges.Add(
                edge);

            AddAdjacency(
                low,
                edge);

            AddAdjacency(
                high,
                edge);
        }

        private static void AddAdjacency(
            int node,
            Edge edge)
        {
            if (!Adjacency.TryGetValue(
                    node,
                    out List<Edge> connected))
            {
                connected =
                    new List<Edge>();

                Adjacency[
                    node] =
                    connected;
            }

            connected.Add(
                edge);
        }

        private static void EnsurePathBuffers(
            int count)
        {
            if (distanceBuffer.Length >=
                count)
            {
                return;
            }

            int capacity =
                Mathf.NextPowerOfTwo(
                    Mathf.Max(
                        16,
                        count));

            distanceBuffer =
                new float[capacity];

            previousBuffer =
                new int[capacity];

            visitedBuffer =
                new bool[capacity];
        }

        private static List<int> FindShortestPath(
            int start,
            int end,
            List<Vector3> nodes,
            List<Edge> edges)
        {
            if (start == end)
            {
                return new List<int>
                {
                    start
                };
            }

            int count =
                nodes.Count;

            EnsurePathBuffers(
                count);

            float[] distance =
                distanceBuffer;

            int[] previous =
                previousBuffer;

            bool[] visited =
                visitedBuffer;

            for (int i = 0;
                 i < count;
                 i++)
            {
                distance[i] =
                    float.PositiveInfinity;

                previous[i] =
                    -1;

                visited[i] =
                    false;
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
                        distance[i] <
                        best)
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

                if (!Adjacency.TryGetValue(
                        current,
                        out List<Edge> connected))
                {
                    continue;
                }

                foreach (Edge edge in
                         connected)
                {
                    int neighbor =
                        edge.A == current
                            ? edge.B
                            : edge.A;

                    if (visited[neighbor])
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

            if (previous[end] <
                0)
            {
                return new List<int>();
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

        private sealed class WayNetworkEntry
        {
            public readonly MonoBehaviour Component;

            public readonly List<Transform> Waypoints;

            public readonly List<int> NodeIndices =
                new();

            public WayNetworkEntry(
                MonoBehaviour component,
                List<Transform> waypoints)
            {
                Component =
                    component;

                Waypoints =
                    waypoints;
            }
        }

        private readonly struct RoadProjection
        {
            public static readonly RoadProjection Invalid =
                new(
                    -1,
                    -1,
                    -1,
                    Vector3.zero);

            public readonly int EdgeIndex;
            public readonly int A;
            public readonly int B;
            public readonly Vector3 Position;

            public bool IsValid =>
                EdgeIndex >= 0 &&
                A >= 0 &&
                B >= 0;

            public RoadProjection(
                int edgeIndex,
                int a,
                int b,
                Vector3 position)
            {
                EdgeIndex =
                    edgeIndex;

                A =
                    a;

                B =
                    b;

                Position =
                    position;
            }
        }

        private readonly struct RouteCandidate
        {
            public static readonly RouteCandidate Invalid =
                new(
                    null,
                    float.PositiveInfinity);

            public readonly List<int> NodePath;
            public readonly float Cost;

            public bool IsValid =>
                NodePath != null;

            public RouteCandidate(
                List<int> nodePath,
                float cost)
            {
                NodePath =
                    nodePath;

                Cost =
                    cost;
            }
        }

        private readonly struct Edge
        {
            public readonly int A;

            public readonly int B;

            public readonly float Cost;
            public readonly bool IsRoadSegment;

            public Edge(
                int a,
                int b,
                float cost,
                bool isRoadSegment)
            {
                A =
                    a;

                B =
                    b;

                Cost =
                    cost;

                IsRoadSegment =
                    isRoadSegment;
            }
        }
    }
}
