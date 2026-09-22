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

        private static readonly Dictionary<MonoBehaviour, WayNetworkEntry>
            WayEntries =
                new();

        private static bool graphReady;

        private static ulong cachedSceneHandle =
            ulong.MaxValue;

        private static int cachedStartNode =
            -1;

        private static int cachedEndNode =
            -1;

        private static List<int> cachedNodePath =
            new();

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

            if (startNode !=
                    cachedStartNode ||
                endNode !=
                    cachedEndNode ||
                cachedNodePath == null ||
                cachedNodePath.Count == 0)
            {
                cachedStartNode =
                    startNode;

                cachedEndNode =
                    endNode;

                cachedNodePath =
                    FindShortestPath(
                        startNode,
                        endNode,
                        Nodes,
                        Edges);
            }

            List<Vector3> result =
                new()
                {
                    start
                };

            foreach (int index in
                     cachedNodePath)
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
            WayEntries.Clear();

            cachedSceneHandle =
                sceneHandle;

            cachedStartNode =
                -1;

            cachedEndNode =
                -1;

            cachedNodePath.Clear();

            bool builtFromTraffic =
                BuildFromFcgAuthoredNetwork();

            if (!builtFromTraffic)
            {
                BuildFallbackGraph();
            }

            graphReady =
                true;
        }

        private static bool BuildFromFcgAuthoredNetwork()
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

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
                            Edges);
                    }

                    previous =
                        current;
                }
            }

            if (WayEntries.Count == 0)
            {
                Debug.LogWarning(
                    "Motor City navigator: FCG authored Way containers were not found. " +
                    "Using the fallback gameplay-road graph.");

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

            Debug.Log(
                "Motor City navigator: built from authored FCG traffic graph. " +
                $"Ways={WayEntries.Count}, Nodes={Nodes.Count}, Edges={Edges.Count}.");

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
                    Edges);
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

            Debug.LogWarning(
                "Motor City navigator: using fallback gameplay-road graph.");
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
                if (edge.A ==
                        low &&
                    edge.B ==
                        high)
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

                if (distance >=
                    best)
                {
                    continue;
                }

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
            if (start == end)
            {
                return new List<int>
                {
                    start
                };
            }

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

            if (previous[end] <
                0)
            {
                return new List<int>
                {
                    start,
                    end
                };
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
