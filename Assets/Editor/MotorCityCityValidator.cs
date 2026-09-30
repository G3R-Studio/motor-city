using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MotorCity.EditorTools
{
    public sealed class MotorCityCityValidator : EditorWindow
    {
        private enum IssueKind
        {
            MissingMesh,
            MissingMaterial,
            BrokenShader,
            RoadWithoutCollider,
            ColliderWithoutMesh,
            SuspiciousRoadUv,
            SurfaceHole
        }

        [Serializable]
        private sealed class Issue
        {
            public IssueKind Kind;
            public string Message;
            public UnityEngine.Object Context;
            public Vector3 Position;
        }

        private static readonly List<Issue> Issues =
            new();

        private Vector2 scroll;
        private float gridSpacing = 3f;
        private float neighborHeightTolerance = 1.5f;
        private float markerSize = 1.8f;
        private bool showMarkers = true;

        [MenuItem("Tools/Motor City/City Validator")]
        private static void Open()
        {
            GetWindow<MotorCityCityValidator>(
                "City Validator");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui +=
                DrawSceneMarkers;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -=
                DrawSceneMarkers;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Motor City - City Validator",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Checks the loaded scene for broken render data, road collider problems, suspicious flipped road UVs and small surface holes. " +
                "Road UV detection looks for abrupt 180-degree UV-basis reversals between neighboring coplanar road triangles. " +
                "Surface-hole detection uses raycasts and is intended to find suspicious gaps, not every empty area of the map.",
                MessageType.Info);

            gridSpacing =
                EditorGUILayout.Slider(
                    "Hole scan spacing",
                    gridSpacing,
                    1.5f,
                    8f);

            neighborHeightTolerance =
                EditorGUILayout.Slider(
                    "Height tolerance",
                    neighborHeightTolerance,
                    0.25f,
                    5f);

            markerSize =
                EditorGUILayout.Slider(
                    "Marker size",
                    markerSize,
                    0.5f,
                    5f);

            showMarkers =
                EditorGUILayout.Toggle(
                    "Show Scene markers",
                    showMarkers);

            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        "Scan All",
                        GUILayout.Height(32f)))
                {
                    ScanAll();
                }

                if (GUILayout.Button(
                        "Visual / Mesh",
                        GUILayout.Height(32f)))
                {
                    ClearIssues();
                    ScanRenderers();
                    RepaintViews();
                }

                if (GUILayout.Button(
                        "Road UV",
                        GUILayout.Height(32f)))
                {
                    ClearIssues();
                    ScanRoadUvs();
                    RepaintViews();
                }

                if (GUILayout.Button(
                        "Surface Holes",
                        GUILayout.Height(32f)))
                {
                    ClearIssues();
                    ScanSurfaceHoles();
                    RepaintViews();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear"))
                {
                    ClearIssues();
                    RepaintViews();
                }

                if (GUILayout.Button("Frame All"))
                {
                    FrameAllIssues();
                }
            }

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField(
                "Problems: " + Issues.Count,
                Issues.Count == 0
                    ? EditorStyles.label
                    : EditorStyles.boldLabel);

            scroll =
                EditorGUILayout.BeginScrollView(
                    scroll);

            for (int i = 0;
                 i < Issues.Count;
                 i++)
            {
                DrawIssue(
                    i,
                    Issues[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawIssue(
            int index,
            Issue issue)
        {
            if (issue == null)
                return;

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    (index + 1) +
                    ". " +
                    issue.Kind,
                    EditorStyles.boldLabel);

                EditorGUILayout.LabelField(
                    issue.Message,
                    EditorStyles.wordWrappedLabel);

                EditorGUILayout.LabelField(
                    "Position: " +
                    FormatVector(
                        issue.Position),
                    EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(
                            "Go To",
                            GUILayout.Width(80f)))
                    {
                        FocusIssue(
                            issue);
                    }

                    if (issue.Context != null &&
                        GUILayout.Button(
                            "Select",
                            GUILayout.Width(80f)))
                    {
                        Selection.activeObject =
                            issue.Context;

                        EditorGUIUtility.PingObject(
                            issue.Context);
                    }
                }
            }
        }

        private void ScanAll()
        {
            ClearIssues();
            ScanRenderers();
            ScanRoadUvs();
            ScanSurfaceHoles();
            RepaintViews();

            Debug.Log(
                "Motor City City Validator: scan complete, " +
                Issues.Count +
                " issue(s) found.");
        }

        private void ScanRenderers()
        {
            Renderer[] renderers =
                Resources.FindObjectsOfTypeAll<Renderer>();

            Collider[] sceneColliders =
                Resources.FindObjectsOfTypeAll<Collider>();

            foreach (Renderer renderer in
                     renderers)
            {
                if (!IsSceneObject(
                        renderer))
                {
                    continue;
                }

                if (renderer is MeshRenderer)
                {
                    MeshFilter filter =
                        renderer.GetComponent<MeshFilter>();

                    if (filter == null ||
                        filter.sharedMesh == null)
                    {
                        AddObjectIssue(
                            IssueKind.MissingMesh,
                            renderer,
                            "Renderer has no valid MeshFilter/sharedMesh.");

                        continue;
                    }
                }

                if (renderer is SkinnedMeshRenderer skinned &&
                    skinned.sharedMesh == null)
                {
                    AddObjectIssue(
                        IssueKind.MissingMesh,
                        renderer,
                        "SkinnedMeshRenderer has no sharedMesh.");

                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                if (materials == null ||
                    materials.Length == 0)
                {
                    AddObjectIssue(
                        IssueKind.MissingMaterial,
                        renderer,
                        "Renderer has no materials.");

                    continue;
                }

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material material =
                        materials[i];

                    if (material == null)
                    {
                        AddObjectIssue(
                            IssueKind.MissingMaterial,
                            renderer,
                            "Material slot " +
                            i +
                            " is missing.");

                        continue;
                    }

                    Shader shader =
                        material.shader;

                    if (shader == null ||
                        shader.name ==
                            "Hidden/InternalErrorShader")
                    {
                        AddObjectIssue(
                            IssueKind.BrokenShader,
                            renderer,
                            "Material '" +
                            material.name +
                            "' has a missing/error shader.");
                    }
                }

                if (LooksLikeRoadRenderer(
                        renderer) &&
                    !IsColliderValidationExcluded(
                        renderer.transform) &&
                    !HasRoadCollider(
                        renderer,
                        sceneColliders))
                {
                    AddObjectIssue(
                        IssueKind.RoadWithoutCollider,
                        renderer,
                        "Road-like renderer has no enabled local or spatially matching road collider.");
                }
            }

            MeshCollider[] meshColliders =
                Resources.FindObjectsOfTypeAll<MeshCollider>();

            foreach (MeshCollider collider in
                     meshColliders)
            {
                if (!IsSceneObject(
                        collider) ||
                    !collider.enabled)
                {
                    continue;
                }

                if (IsColliderValidationExcluded(
                        collider.transform))
                {
                    continue;
                }

                if (collider.sharedMesh == null)
                {
                    AddObjectIssue(
                        IssueKind.ColliderWithoutMesh,
                        collider,
                        "Enabled MeshCollider has no sharedMesh.");
                }
            }
        }

        private void ScanSurfaceHoles()
        {
            if (!TryResolveScanBounds(
                    out Bounds bounds))
            {
                Debug.LogWarning(
                    "Motor City City Validator: could not resolve city bounds.");

                return;
            }

            Physics.SyncTransforms();

            float spacing =
                Mathf.Max(
                    1.5f,
                    gridSpacing);

            float rayTop =
                bounds.max.y +
                80f;

            float rayDistance =
                Mathf.Max(
                    160f,
                    bounds.size.y +
                    160f);

            int xSteps =
                Mathf.CeilToInt(
                    bounds.size.x /
                    spacing);

            int zSteps =
                Mathf.CeilToInt(
                    bounds.size.z /
                    spacing);

            int total =
                Mathf.Max(
                    1,
                    xSteps *
                    zSteps);

            int processed =
                0;

            try
            {
                for (int x = 0;
                     x <= xSteps;
                     x++)
                {
                    float worldX =
                        bounds.min.x +
                        x *
                        spacing;

                    for (int z = 0;
                         z <= zSteps;
                         z++)
                    {
                        processed++;

                        if (processed % 250 == 0 &&
                            EditorUtility.DisplayCancelableProgressBar(
                                "Motor City City Validator",
                                "Scanning surface holes...",
                                processed /
                                (float)total))
                        {
                            return;
                        }

                        float worldZ =
                            bounds.min.z +
                            z *
                            spacing;

                        Vector3 origin =
                            new(
                                worldX,
                                rayTop,
                                worldZ);

                        if (TryGroundHit(
                                origin,
                                rayDistance,
                                out _))
                        {
                            continue;
                        }

                        if (IsLikelySurfaceHole(
                                origin,
                                rayDistance,
                                spacing,
                                out float estimatedY))
                        {
                            Issues.Add(
                                new Issue
                                {
                                    Kind =
                                        IssueKind.SurfaceHole,
                                    Message =
                                        "No collider hit at this point, but nearby samples indicate a continuous surface. Inspect for a road/ground gap.",
                                    Position =
                                        new Vector3(
                                            worldX,
                                            estimatedY,
                                            worldZ)
                                });
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            MergeNearbySurfaceHoleMarkers(
                spacing *
                1.4f);
        }

        private bool IsLikelySurfaceHole(
            Vector3 origin,
            float rayDistance,
            float spacing,
            out float estimatedY)
        {
            Vector3[] offsets =
            {
                Vector3.left * spacing,
                Vector3.right * spacing,
                Vector3.forward * spacing,
                Vector3.back * spacing
            };

            float[] heights =
                new float[4];

            bool[] hits =
                new bool[4];

            int hitCount =
                0;

            float sum =
                0f;

            for (int i = 0;
                 i < offsets.Length;
                 i++)
            {
                if (!TryGroundHit(
                        origin + offsets[i],
                        rayDistance,
                        out RaycastHit hit))
                {
                    continue;
                }

                hits[i] =
                    true;

                heights[i] =
                    hit.point.y;

                hitCount++;

                sum +=
                    hit.point.y;
            }

            estimatedY =
                hitCount > 0
                    ? sum / hitCount
                    : origin.y;

            if (hitCount < 3)
                return false;

            bool oppositeSupport =
                (hits[0] && hits[1]) ||
                (hits[2] && hits[3]);

            if (!oppositeSupport)
                return false;

            float minY =
                float.PositiveInfinity;

            float maxY =
                float.NegativeInfinity;

            for (int i = 0;
                 i < heights.Length;
                 i++)
            {
                if (!hits[i])
                    continue;

                minY =
                    Mathf.Min(
                        minY,
                        heights[i]);

                maxY =
                    Mathf.Max(
                        maxY,
                        heights[i]);
            }

            return
                maxY - minY <=
                neighborHeightTolerance;
        }

        private static bool TryGroundHit(
            Vector3 origin,
            float distance,
            out RaycastHit hit)
        {
            RaycastHit[] hits =
                Physics.RaycastAll(
                    origin,
                    Vector3.down,
                    distance,
                    ~0,
                    QueryTriggerInteraction.Ignore);

            if (hits == null ||
                hits.Length == 0)
            {
                hit =
                    default;

                return false;
            }

            Array.Sort(
                hits,
                (a, b) =>
                    a.distance.CompareTo(
                        b.distance));

            for (int i = 0;
                 i < hits.Length;
                 i++)
            {
                Collider collider =
                    hits[i].collider;

                if (collider == null ||
                    !collider.enabled ||
                    collider.isTrigger)
                {
                    continue;
                }

                if (!IsSceneObject(
                        collider))
                {
                    continue;
                }

                hit =
                    hits[i];

                return true;
            }

            hit =
                default;

            return false;
        }

        private static bool TryResolveScanBounds(
            out Bounds bounds)
        {
            Renderer[] renderers =
                Resources.FindObjectsOfTypeAll<Renderer>();

            bool initialized =
                false;

            bounds =
                default;

            foreach (Renderer renderer in
                     renderers)
            {
                if (!IsSceneObject(
                        renderer) ||
                    !renderer.enabled)
                {
                    continue;
                }

                if (renderer.bounds.size.sqrMagnitude <=
                    0.0001f)
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds =
                        renderer.bounds;

                    initialized =
                        true;
                }
                else
                {
                    bounds.Encapsulate(
                        renderer.bounds);
                }
            }

            if (!initialized)
                return false;

            // Avoid huge accidental bounds from skyboxes, effects or helper
            // geometry. The playable city in this project is centered near
            // the origin, so cap only obviously invalid extents.
            bounds.size =
                new Vector3(
                    Mathf.Min(
                        bounds.size.x,
                        2200f),
                    Mathf.Min(
                        bounds.size.y,
                        500f),
                    Mathf.Min(
                        bounds.size.z,
                        2200f));

            return true;
        }

        private static bool LooksLikeRoadRenderer(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            string objectName =
                renderer.name
                    .ToLowerInvariant();

            if (objectName.Contains("road") ||
                objectName.Contains("street") ||
                objectName.Contains("asphalt"))
            {
                return true;
            }

            Material[] materials =
                renderer.sharedMaterials;

            for (int i = 0;
                 i < materials.Length;
                 i++)
            {
                Material material =
                    materials[i];

                if (material == null)
                    continue;

                string materialName =
                    material.name
                        .ToLowerInvariant();

                if (materialName.Contains("fcg_roads") ||
                    materialName.Contains("road") ||
                    materialName.Contains("asphalt"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsColliderValidationExcluded(
            Transform transform)
        {
            Transform current =
                transform;

            int depth =
                0;

            while (current != null &&
                   depth++ < 8)
            {
                string lower =
                    current.name
                        .ToLowerInvariant();

                if (lower.Contains("streetlight") ||
                    lower.Contains("parklamp") ||
                    lower.Contains("parkbench") ||
                    lower.Contains("trash"))
                {
                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static bool HasRoadCollider(
            Renderer renderer,
            Collider[] sceneColliders)
        {
            if (renderer == null)
                return false;

            if (HasColliderInHierarchy(
                    renderer.transform))
            {
                return true;
            }

            if (sceneColliders == null ||
                sceneColliders.Length == 0)
            {
                return false;
            }

            Bounds roadBounds =
                renderer.bounds;

            for (int i = 0;
                 i < sceneColliders.Length;
                 i++)
            {
                Collider collider =
                    sceneColliders[i];

                if (collider == null ||
                    !collider.enabled ||
                    collider.isTrigger ||
                    !IsSceneObject(
                        collider) ||
                    IsColliderValidationExcluded(
                        collider.transform) ||
                    !LooksLikeRoadCollider(
                        collider))
                {
                    continue;
                }

                Bounds colliderBounds =
                    collider.bounds;

                if (RoadBoundsOverlap(
                        roadBounds,
                        colliderBounds))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeRoadCollider(
            Collider collider)
        {
            if (collider == null)
                return false;

            Transform current =
                collider.transform;

            int depth =
                0;

            while (current != null &&
                   depth++ < 6)
            {
                string lower =
                    current.name
                        .ToLowerInvariant();

                if (lower.Contains("collider-road") ||
                    lower.Contains("collider road") ||
                    lower.Contains("road-collider") ||
                    lower.Contains("road collider"))
                {
                    return true;
                }

                current =
                    current.parent;
            }

            MeshCollider meshCollider =
                collider as MeshCollider;

            if (meshCollider != null &&
                meshCollider.sharedMesh != null)
            {
                string meshName =
                    meshCollider.sharedMesh.name
                        .ToLowerInvariant();

                if (meshName.Contains("road") ||
                    meshName.Contains("street") ||
                    meshName.Contains("asphalt"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RoadBoundsOverlap(
            Bounds road,
            Bounds collider)
        {
            float roadMinX =
                road.min.x;

            float roadMaxX =
                road.max.x;

            float roadMinZ =
                road.min.z;

            float roadMaxZ =
                road.max.z;

            float colliderMinX =
                collider.min.x;

            float colliderMaxX =
                collider.max.x;

            float colliderMinZ =
                collider.min.z;

            float colliderMaxZ =
                collider.max.z;

            float overlapX =
                Mathf.Min(
                    roadMaxX,
                    colliderMaxX) -
                Mathf.Max(
                    roadMinX,
                    colliderMinX);

            float overlapZ =
                Mathf.Min(
                    roadMaxZ,
                    colliderMaxZ) -
                Mathf.Max(
                    roadMinZ,
                    colliderMinZ);

            if (overlapX <= 0.02f ||
                overlapZ <= 0.02f)
            {
                return false;
            }

            float roadArea =
                Mathf.Max(
                    0.01f,
                    road.size.x *
                    road.size.z);

            float overlapArea =
                overlapX *
                overlapZ;

            float verticalGap =
                Mathf.Abs(
                    collider.bounds.center.y -
                    road.bounds.center.y);

            return
                overlapArea /
                roadArea >=
                    0.2f &&
                verticalGap <=
                    Mathf.Max(
                        2.5f,
                        road.extents.y +
                        collider.extents.y +
                        0.5f);
        }

        private sealed class RoadUvTriangle
        {
            public Vector3 A;
            public Vector3 B;
            public Vector3 C;
            public Vector3 Normal;
            public Vector3 UDirection;
            public Vector3 VDirection;
            public Vector3 Center;
        }

        private readonly struct RoadUvEdgeKey :
            IEquatable<RoadUvEdgeKey>
        {
            private readonly Vector3Int a;
            private readonly Vector3Int b;

            public RoadUvEdgeKey(
                Vector3 first,
                Vector3 second)
            {
                Vector3Int qFirst =
                    QuantizeRoadUvPoint(
                        first);

                Vector3Int qSecond =
                    QuantizeRoadUvPoint(
                        second);

                if (CompareVector3Int(
                        qFirst,
                        qSecond) <= 0)
                {
                    a = qFirst;
                    b = qSecond;
                }
                else
                {
                    a = qSecond;
                    b = qFirst;
                }
            }

            public bool Equals(
                RoadUvEdgeKey other)
            {
                return
                    a == other.a &&
                    b == other.b;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is RoadUvEdgeKey other &&
                    Equals(
                        other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return
                        (a.GetHashCode() * 397) ^
                        b.GetHashCode();
                }
            }
        }

        private static void ScanRoadUvs()
        {
            Renderer[] renderers =
                Resources.FindObjectsOfTypeAll<Renderer>();

            foreach (Renderer renderer in
                     renderers)
            {
                if (!IsSceneObject(
                        renderer) ||
                    !renderer.enabled ||
                    !LooksLikeRoadRenderer(
                        renderer) ||
                    IsColliderValidationExcluded(
                        renderer.transform))
                {
                    continue;
                }

                MeshFilter filter =
                    renderer.GetComponent<MeshFilter>();

                if (filter == null ||
                    filter.sharedMesh == null)
                {
                    continue;
                }

                ScanRoadRendererUvs(
                    renderer,
                    filter.sharedMesh);
            }
        }

        private static void ScanRoadRendererUvs(
            Renderer renderer,
            Mesh mesh)
        {
            Vector3[] vertices =
                mesh.vertices;

            Vector2[] uv =
                mesh.uv;

            if (vertices == null ||
                uv == null ||
                uv.Length != vertices.Length ||
                vertices.Length < 3)
            {
                return;
            }

            Material[] materials =
                renderer.sharedMaterials;

            Dictionary<RoadUvEdgeKey, RoadUvTriangle>
                edges =
                    new();

            HashSet<RoadUvEdgeKey>
                reported =
                    new();

            int issueCount =
                0;

            for (int subMesh = 0;
                 subMesh < mesh.subMeshCount;
                 subMesh++)
            {
                Material material =
                    subMesh < materials.Length
                        ? materials[subMesh]
                        : null;

                if (!LooksLikeRoadMaterial(
                        material))
                {
                    continue;
                }

                int[] triangles =
                    mesh.GetTriangles(
                        subMesh);

                for (int i = 0;
                     i + 2 < triangles.Length;
                     i += 3)
                {
                    int ia =
                        triangles[i];

                    int ib =
                        triangles[i + 1];

                    int ic =
                        triangles[i + 2];

                    if (ia < 0 ||
                        ib < 0 ||
                        ic < 0 ||
                        ia >= vertices.Length ||
                        ib >= vertices.Length ||
                        ic >= vertices.Length)
                    {
                        continue;
                    }

                    Vector3 localA =
                        vertices[ia];

                    Vector3 localB =
                        vertices[ib];

                    Vector3 localC =
                        vertices[ic];

                    Vector3 worldA =
                        renderer.transform.TransformPoint(
                            localA);

                    Vector3 worldB =
                        renderer.transform.TransformPoint(
                            localB);

                    Vector3 worldC =
                        renderer.transform.TransformPoint(
                            localC);

                    Vector3 e1 =
                        worldB -
                        worldA;

                    Vector3 e2 =
                        worldC -
                        worldA;

                    Vector3 normal =
                        Vector3.Cross(
                            e1,
                            e2);

                    float normalLength =
                        normal.magnitude;

                    if (normalLength <=
                        0.0001f)
                    {
                        continue;
                    }

                    normal /=
                        normalLength;

                    if (Vector3.Dot(
                            normal,
                            Vector3.up) <
                        0.7f)
                    {
                        continue;
                    }

                    Vector2 duv1 =
                        uv[ib] -
                        uv[ia];

                    Vector2 duv2 =
                        uv[ic] -
                        uv[ia];

                    float det =
                        duv1.x *
                        duv2.y -
                        duv2.x *
                        duv1.y;

                    if (Mathf.Abs(
                            det) <=
                        0.000001f)
                    {
                        continue;
                    }

                    Vector3 uDirection =
                        (e1 *
                         duv2.y -
                         e2 *
                         duv1.y) /
                        det;

                    Vector3 vDirection =
                        (e2 *
                         duv1.x -
                         e1 *
                         duv2.x) /
                        det;

                    uDirection =
                        Vector3.ProjectOnPlane(
                            uDirection,
                            normal);

                    vDirection =
                        Vector3.ProjectOnPlane(
                            vDirection,
                            normal);

                    if (uDirection.sqrMagnitude <=
                            0.0001f ||
                        vDirection.sqrMagnitude <=
                            0.0001f)
                    {
                        continue;
                    }

                    RoadUvTriangle triangle =
                        new()
                        {
                            A = worldA,
                            B = worldB,
                            C = worldC,
                            Normal = normal,
                            UDirection =
                                uDirection.normalized,
                            VDirection =
                                vDirection.normalized,
                            Center =
                                (worldA +
                                 worldB +
                                 worldC) /
                                3f
                        };

                    RoadUvEdgeKey[] triangleEdges =
                    {
                        new RoadUvEdgeKey(
                            worldA,
                            worldB),
                        new RoadUvEdgeKey(
                            worldB,
                            worldC),
                        new RoadUvEdgeKey(
                            worldC,
                            worldA)
                    };

                    for (int edgeIndex = 0;
                         edgeIndex < triangleEdges.Length;
                         edgeIndex++)
                    {
                        RoadUvEdgeKey edge =
                            triangleEdges[edgeIndex];

                        if (!edges.TryGetValue(
                                edge,
                                out RoadUvTriangle neighbor))
                        {
                            edges[edge] =
                                triangle;

                            continue;
                        }

                        if (reported.Contains(
                                edge) ||
                            Vector3.Dot(
                                triangle.Normal,
                                neighbor.Normal) <
                                0.94f)
                        {
                            continue;
                        }

                        float uDot =
                            Vector3.Dot(
                                triangle.UDirection,
                                neighbor.UDirection);

                        float vDot =
                            Vector3.Dot(
                                triangle.VDirection,
                                neighbor.VDirection);

                        if (uDot >
                                -0.75f ||
                            vDot >
                                -0.75f)
                        {
                            continue;
                        }

                        reported.Add(
                            edge);

                        Vector3 marker =
                            (triangle.Center +
                             neighbor.Center) *
                            0.5f;

                        AddPositionIssue(
                            IssueKind.SuspiciousRoadUv,
                            renderer,
                            marker,
                            "Neighboring coplanar road triangles reverse both UV axes by about 180 degrees. Inspect this spot for an upside-down road texture/UV island.");

                        issueCount++;

                        if (issueCount >= 64)
                        {
                            return;
                        }
                    }
                }
            }
        }

        private static bool LooksLikeRoadMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string lower =
                material.name
                    .ToLowerInvariant();

            return
                lower.Contains("fcg_roads") ||
                lower.Contains("road") ||
                lower.Contains("asphalt");
        }

        private static Vector3Int QuantizeRoadUvPoint(
            Vector3 value)
        {
            const float precision =
                1000f;

            return
                new Vector3Int(
                    Mathf.RoundToInt(
                        value.x *
                        precision),
                    Mathf.RoundToInt(
                        value.y *
                        precision),
                    Mathf.RoundToInt(
                        value.z *
                        precision));
        }

        private static int CompareVector3Int(
            Vector3Int a,
            Vector3Int b)
        {
            if (a.x != b.x)
                return
                    a.x.CompareTo(
                        b.x);

            if (a.y != b.y)
                return
                    a.y.CompareTo(
                        b.y);

            return
                a.z.CompareTo(
                    b.z);
        }

        private static bool HasColliderInHierarchy(
            Transform transform)
        {
            if (transform == null)
                return false;

            Collider local =
                transform.GetComponent<Collider>();

            if (local != null &&
                local.enabled)
            {
                return true;
            }

            Collider[] children =
                transform.GetComponentsInChildren<Collider>(
                    true);

            for (int i = 0;
                 i < children.Length;
                 i++)
            {
                if (children[i] != null &&
                    children[i].enabled)
                {
                    return true;
                }
            }

            Transform parent =
                transform.parent;

            int depth =
                0;

            while (parent != null &&
                   depth++ < 4)
            {
                Collider parentCollider =
                    parent.GetComponent<Collider>();

                if (parentCollider != null &&
                    parentCollider.enabled)
                {
                    return true;
                }

                parent =
                    parent.parent;
            }

            return false;
        }

        private static bool IsSceneObject(
            Component component)
        {
            if (component == null ||
                component.gameObject == null)
            {
                return false;
            }

            GameObject gameObject =
                component.gameObject;

            Scene scene =
                gameObject.scene;

            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                return false;
            }

            if ((gameObject.hideFlags &
                 HideFlags.HideAndDontSave) != 0)
            {
                return false;
            }

            return true;
        }

        private static void AddObjectIssue(
            IssueKind kind,
            Component component,
            string message)
        {
            Issues.Add(
                new Issue
                {
                    Kind =
                        kind,
                    Message =
                        message,
                    Context =
                        component.gameObject,
                    Position =
                        ResolveObjectPosition(
                            component)
                });
        }

        private static void AddPositionIssue(
            IssueKind kind,
            Component component,
            Vector3 position,
            string message)
        {
            Issues.Add(
                new Issue
                {
                    Kind =
                        kind,
                    Message =
                        message,
                    Context =
                        component != null
                            ? component.gameObject
                            : null,
                    Position =
                        position
                });
        }

        private static Vector3 ResolveObjectPosition(
            Component component)
        {
            if (component is Renderer renderer)
            {
                return
                    renderer.bounds.center;
            }

            if (component is Collider collider)
            {
                return
                    collider.bounds.center;
            }

            return
                component != null
                    ? component.transform.position
                    : Vector3.zero;
        }

        private static void MergeNearbySurfaceHoleMarkers(
            float radius)
        {
            for (int i = Issues.Count - 1;
                 i >= 0;
                 i--)
            {
                Issue current =
                    Issues[i];

                if (current.Kind !=
                    IssueKind.SurfaceHole)
                {
                    continue;
                }

                for (int j = i - 1;
                     j >= 0;
                     j--)
                {
                    Issue other =
                        Issues[j];

                    if (other.Kind !=
                        IssueKind.SurfaceHole)
                    {
                        continue;
                    }

                    Vector2 a =
                        new(
                            current.Position.x,
                            current.Position.z);

                    Vector2 b =
                        new(
                            other.Position.x,
                            other.Position.z);

                    if (Vector2.Distance(
                            a,
                            b) >
                        radius)
                    {
                        continue;
                    }

                    other.Position =
                        (other.Position +
                         current.Position) *
                        0.5f;

                    Issues.RemoveAt(
                        i);

                    break;
                }
            }
        }

        private static void ClearIssues()
        {
            Issues.Clear();
        }

        private static void FocusIssue(
            Issue issue)
        {
            if (issue == null)
                return;

            if (issue.Context != null)
            {
                Selection.activeObject =
                    issue.Context;
            }

            SceneView sceneView =
                SceneView.lastActiveSceneView;

            if (sceneView == null)
                return;

            sceneView.LookAt(
                issue.Position,
                sceneView.rotation,
                18f);

            sceneView.Repaint();
        }

        private void FrameAllIssues()
        {
            if (Issues.Count == 0)
                return;

            Bounds bounds =
                new(
                    Issues[0].Position,
                    Vector3.one);

            for (int i = 1;
                 i < Issues.Count;
                 i++)
            {
                bounds.Encapsulate(
                    Issues[i].Position);
            }

            SceneView sceneView =
                SceneView.lastActiveSceneView;

            if (sceneView == null)
                return;

            sceneView.Frame(
                bounds,
                false);

            sceneView.Repaint();
        }

        private void DrawSceneMarkers(
            SceneView sceneView)
        {
            if (!showMarkers ||
                Issues.Count == 0)
            {
                return;
            }

            Handles.zTest =
                UnityEngine.Rendering.CompareFunction.LessEqual;

            for (int i = 0;
                 i < Issues.Count;
                 i++)
            {
                Issue issue =
                    Issues[i];

                if (issue == null)
                    continue;

                Color color =
                    issue.Kind ==
                    IssueKind.SurfaceHole
                        ? new Color(
                            1f,
                            0.12f,
                            0.08f,
                            0.95f)
                        : new Color(
                            1f,
                            0.55f,
                            0.06f,
                            0.95f);

                Handles.color =
                    color;

                float size =
                    HandleUtility.GetHandleSize(
                        issue.Position) *
                    0.08f *
                    markerSize;

                Handles.SphereHandleCap(
                    0,
                    issue.Position,
                    Quaternion.identity,
                    size,
                    EventType.Repaint);

                Handles.DrawWireDisc(
                    issue.Position,
                    Vector3.up,
                    size *
                    1.7f);

                Handles.Label(
                    issue.Position +
                    Vector3.up *
                    size *
                    1.3f,
                    (i + 1) +
                    " " +
                    issue.Kind);
            }
        }

        private static void RepaintViews()
        {
            SceneView.RepaintAll();

            MotorCityCityValidator window =
                HasOpenInstances<MotorCityCityValidator>()
                    ? GetWindow<MotorCityCityValidator>()
                    : null;

            window?.Repaint();
        }

        private static string FormatVector(
            Vector3 value)
        {
            return
                value.x.ToString("0.0") +
                ", " +
                value.y.ToString("0.0") +
                ", " +
                value.z.ToString("0.0");
        }
    }
}
