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
                "Checks the loaded scene for broken render data, road collider problems and small surface holes. " +
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
                    !HasColliderInHierarchy(
                        renderer.transform))
                {
                    AddObjectIssue(
                        IssueKind.RoadWithoutCollider,
                        renderer,
                        "Road-like renderer has no enabled collider in its local hierarchy.");
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
