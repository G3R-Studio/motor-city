#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CityAssetScanner
{
    private const string ReportFileName =
        "MotorCity_CityAssetReport.txt";

    private const int MaxDetailedPrefabs = 800;
    private const int MaxObjectsPerRoot = 25000;

    private static readonly string[] RoadTokens =
    {
        "road", "street", "highway", "asphalt",
        "lane", "tarmac", "intersection", "junction"
    };

    private static readonly string[] SidewalkTokens =
    {
        "sidewalk", "sideway", "pavement", "footpath",
        "pedestrian", "curb", "kerb", "walkway"
    };

    private static readonly string[] BuildingTokens =
    {
        "building", "house", "shop", "store", "office",
        "tower", "factory", "garage", "warehouse", "apartment"
    };

    private static readonly string[] ParkingTokens =
    {
        "parking", "carpark", "car park", "lot"
    };

    [MenuItem("Motor City/Tools/Scan City Asset Folder")]
    public static void ScanCityAssetFolder()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City Asset Scanner",
                "Останови Play Mode перед сканированием ассета.",
                "OK");
            return;
        }

        string selectedFolder =
            EditorUtility.OpenFolderPanel(
                "Select imported city asset folder",
                Application.dataPath,
                string.Empty);

        if (string.IsNullOrWhiteSpace(selectedFolder))
            return;

        string assetRoot =
            ToAssetPath(selectedFolder);

        if (string.IsNullOrWhiteSpace(assetRoot) ||
            !AssetDatabase.IsValidFolder(assetRoot))
        {
            EditorUtility.DisplayDialog(
                "Motor City Asset Scanner",
                "Выбери папку, которая находится внутри Assets.",
                "OK");
            return;
        }

        try
        {
            string report =
                BuildReport(assetRoot);

            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                Application.dataPath;

            string reportPath =
                Path.Combine(
                    projectRoot,
                    ReportFileName);

            File.WriteAllText(
                reportPath,
                report,
                new UTF8Encoding(false));

            Debug.Log(
                "Motor City: city asset report generated at " +
                reportPath);

            EditorUtility.RevealInFinder(
                reportPath);

            EditorUtility.DisplayDialog(
                "Motor City Asset Scanner",
                "Готово. Создан файл:\n\n" +
                reportPath +
                "\n\nЗагрузи MotorCity_CityAssetReport.txt в чат.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: city asset scan failed. " +
                exception);

            EditorUtility.DisplayDialog(
                "Motor City Asset Scanner",
                "Сканирование завершилось ошибкой. " +
                "Посмотри Console / Editor.log.",
                "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static string BuildReport(
        string assetRoot)
    {
        var report =
            new StringBuilder(1024 * 128);

        string[] allAssetPaths =
            AssetDatabase.FindAssets(
                    string.Empty,
                    new[] { assetRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        string[] scenePaths =
            FindTypedAssets(
                "t:Scene",
                assetRoot);

        string[] prefabPaths =
            FindTypedAssets(
                "t:Prefab",
                assetRoot);

        string[] materialPaths =
            FindTypedAssets(
                "t:Material",
                assetRoot);

        string[] modelPaths =
            allAssetPaths
                .Where(
                    path =>
                    {
                        string extension =
                            Path.GetExtension(path)
                                .ToLowerInvariant();

                        return extension == ".fbx" ||
                               extension == ".obj";
                    })
                .ToArray();

        AppendHeader(
            report,
            assetRoot,
            allAssetPaths,
            scenePaths,
            prefabPaths,
            materialPaths,
            modelPaths);

        AppendAssetInventory(
            report,
            allAssetPaths);

        AppendMaterialInventory(
            report,
            materialPaths);

        AppendSceneInventory(
            report,
            scenePaths);

        AppendPrefabInventory(
            report,
            prefabPaths);

        AppendModelInventory(
            report,
            modelPaths);

        AppendFooter(report);

        return report.ToString();
    }

    private static void AppendHeader(
        StringBuilder report,
        string assetRoot,
        string[] allAssets,
        string[] scenes,
        string[] prefabs,
        string[] materials,
        string[] models)
    {
        report.AppendLine("MOTOR CITY — CITY ASSET REPORT");
        report.AppendLine(new string('=', 72));
        report.AppendLine(
            "Generated UTC: " +
            DateTime.UtcNow.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture));
        report.AppendLine(
            "Unity: " +
            Application.unityVersion);
        report.AppendLine(
            "Selected asset root: " +
            assetRoot);
        report.AppendLine(
            "Total indexed assets: " +
            allAssets.Length);
        report.AppendLine(
            "Scenes: " +
            scenes.Length);
        report.AppendLine(
            "Prefabs: " +
            prefabs.Length);
        report.AppendLine(
            "Materials: " +
            materials.Length);
        report.AppendLine(
            "FBX/OBJ models: " +
            models.Length);
        report.AppendLine();
        report.AppendLine(
            "Purpose: inspect city hierarchy, road/building candidates, " +
            "coordinates, renderer bounds, colliders and materials.");
        report.AppendLine();
    }

    private static void AppendAssetInventory(
        StringBuilder report,
        string[] paths)
    {
        report.AppendLine("ASSET INVENTORY");
        report.AppendLine(new string('-', 72));

        var extensionCounts =
            paths
                .GroupBy(
                    path =>
                    {
                        string extension =
                            Path.GetExtension(path);

                        return string.IsNullOrWhiteSpace(extension)
                            ? "<folder/other>"
                            : extension.ToLowerInvariant();
                    },
                    StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key);

        foreach (var group in extensionCounts)
        {
            report.AppendLine(
                group.Key +
                ": " +
                group.Count());
        }

        report.AppendLine();
        report.AppendLine("FILES:");

        foreach (string path in paths)
            report.AppendLine("  " + path);

        report.AppendLine();
    }

    private static void AppendMaterialInventory(
        StringBuilder report,
        string[] materialPaths)
    {
        report.AppendLine("MATERIALS");
        report.AppendLine(new string('-', 72));

        for (int i = 0;
             i < materialPaths.Length;
             i++)
        {
            EditorUtility.DisplayProgressBar(
                "Motor City Asset Scanner",
                "Reading materials...",
                Progress(
                    i,
                    materialPaths.Length));

            string path =
                materialPaths[i];

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (material == null)
                continue;

            report.Append("  ");
            report.Append(path);
            report.Append(" | shader=");
            report.Append(
                material.shader != null
                    ? material.shader.name
                    : "<null>");
            report.Append(" | queue=");
            report.Append(material.renderQueue);
            report.AppendLine();
        }

        report.AppendLine();
    }

    private static void AppendSceneInventory(
        StringBuilder report,
        string[] scenePaths)
    {
        report.AppendLine("SCENES");
        report.AppendLine(new string('-', 72));

        if (scenePaths.Length == 0)
        {
            report.AppendLine("  <none>");
            report.AppendLine();
            return;
        }

        for (int i = 0;
             i < scenePaths.Length;
             i++)
        {
            string scenePath =
                scenePaths[i];

            EditorUtility.DisplayProgressBar(
                "Motor City Asset Scanner",
                "Scanning scene " +
                Path.GetFileName(scenePath),
                Progress(
                    i,
                    scenePaths.Length));

            report.AppendLine();
            report.AppendLine(
                "SCENE: " +
                scenePath);

            Scene existing =
                SceneManager.GetSceneByPath(
                    scenePath);

            bool openedByScanner =
                !existing.IsValid() ||
                !existing.isLoaded;

            Scene scene =
                openedByScanner
                    ? EditorSceneManager.OpenScene(
                        scenePath,
                        OpenSceneMode.Additive)
                    : existing;

            try
            {
                GameObject[] roots =
                    scene.GetRootGameObjects();

                report.AppendLine(
                    "Root objects: " +
                    roots.Length);

                Bounds? sceneBounds =
                    CalculateRendererBounds(
                        roots);

                if (sceneBounds.HasValue)
                {
                    report.AppendLine(
                        "Renderer bounds: " +
                        FormatBounds(
                            sceneBounds.Value));
                }

                foreach (GameObject root in roots)
                {
                    AppendGameObjectTree(
                        report,
                        root,
                        "SCENE",
                        MaxObjectsPerRoot);
                }
            }
            finally
            {
                if (openedByScanner &&
                    scene.IsValid() &&
                    scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(
                        scene,
                        true);
                }
            }
        }

        report.AppendLine();
    }

    private static void AppendPrefabInventory(
        StringBuilder report,
        string[] prefabPaths)
    {
        report.AppendLine("PREFABS");
        report.AppendLine(new string('-', 72));
        report.AppendLine(
            "Prefab paths: " +
            prefabPaths.Length);

        foreach (string path in prefabPaths)
            report.AppendLine("  " + path);

        report.AppendLine();
        report.AppendLine("PREFAB DETAILS");

        int detailedCount =
            Mathf.Min(
                prefabPaths.Length,
                MaxDetailedPrefabs);

        for (int i = 0;
             i < detailedCount;
             i++)
        {
            string path =
                prefabPaths[i];

            EditorUtility.DisplayProgressBar(
                "Motor City Asset Scanner",
                "Scanning prefab " +
                Path.GetFileName(path),
                Progress(
                    i,
                    detailedCount));

            GameObject root =
                null;

            try
            {
                root =
                    PrefabUtility.LoadPrefabContents(
                        path);

                report.AppendLine();
                report.AppendLine(
                    "PREFAB: " +
                    path);

                AppendGameObjectTree(
                    report,
                    root,
                    "PREFAB",
                    MaxObjectsPerRoot);
            }
            catch (Exception exception)
            {
                report.AppendLine();
                report.AppendLine(
                    "PREFAB ERROR: " +
                    path +
                    " | " +
                    exception.Message);
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(
                        root);
            }
        }

        if (prefabPaths.Length >
            detailedCount)
        {
            report.AppendLine();
            report.AppendLine(
                "Prefab details truncated after " +
                detailedCount +
                " entries.");
        }

        report.AppendLine();
    }

    private static void AppendModelInventory(
        StringBuilder report,
        string[] modelPaths)
    {
        report.AppendLine("MODEL ASSETS");
        report.AppendLine(new string('-', 72));

        for (int i = 0;
             i < modelPaths.Length;
             i++)
        {
            string path =
                modelPaths[i];

            EditorUtility.DisplayProgressBar(
                "Motor City Asset Scanner",
                "Reading model " +
                Path.GetFileName(path),
                Progress(
                    i,
                    modelPaths.Length));

            GameObject model =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path);

            report.AppendLine();
            report.AppendLine(
                "MODEL: " +
                path);

            if (model == null)
            {
                report.AppendLine(
                    "  <could not load as GameObject>");
                continue;
            }

            MeshFilter[] meshFilters =
                model.GetComponentsInChildren<MeshFilter>(
                    true);

            SkinnedMeshRenderer[] skinned =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true);

            Renderer[] renderers =
                model.GetComponentsInChildren<Renderer>(
                    true);

            report.AppendLine(
                "  MeshFilters=" +
                meshFilters.Length +
                " SkinnedMeshes=" +
                skinned.Length +
                " Renderers=" +
                renderers.Length);

            foreach (MeshFilter filter in meshFilters)
            {
                if (filter == null ||
                    filter.sharedMesh == null)
                    continue;

                report.AppendLine(
                    "  MESH " +
                    GetHierarchyPath(
                        filter.transform) +
                    " | name=" +
                    filter.sharedMesh.name +
                    " | vertices=" +
                    filter.sharedMesh.vertexCount +
                    " | localBounds=" +
                    FormatBounds(
                        filter.sharedMesh.bounds));
            }
        }

        report.AppendLine();
    }

    private static void AppendGameObjectTree(
        StringBuilder report,
        GameObject root,
        string sourceType,
        int maxObjects)
    {
        if (root == null)
            return;

        int visited =
            0;

        var stack =
            new Stack<Tuple<Transform, int>>();

        stack.Push(
            Tuple.Create(
                root.transform,
                0));

        while (stack.Count > 0 &&
               visited < maxObjects)
        {
            Tuple<Transform, int> item =
                stack.Pop();

            Transform transform =
                item.Item1;

            int depth =
                item.Item2;

            visited++;

            AppendObjectLine(
                report,
                transform,
                sourceType,
                depth);

            for (int i = transform.childCount - 1;
                 i >= 0;
                 i--)
            {
                stack.Push(
                    Tuple.Create(
                        transform.GetChild(i),
                        depth + 1));
            }
        }

        if (stack.Count > 0)
        {
            report.AppendLine(
                "  ... hierarchy truncated after " +
                maxObjects +
                " objects");
        }
    }

    private static void AppendObjectLine(
        StringBuilder report,
        Transform transform,
        string sourceType,
        int depth)
    {
        GameObject gameObject =
            transform.gameObject;

        Renderer renderer =
            gameObject.GetComponent<Renderer>();

        Collider[] colliders =
            gameObject.GetComponents<Collider>();

        MeshFilter meshFilter =
            gameObject.GetComponent<MeshFilter>();

        string materials =
            renderer != null
                ? string.Join(
                    ",",
                    renderer.sharedMaterials
                        .Where(material => material != null)
                        .Select(material => material.name)
                        .Distinct())
                : string.Empty;

        string classification =
            Classify(
                gameObject.name +
                " " +
                GetHierarchyPath(transform) +
                " " +
                materials);

        bool interesting =
            classification != "OTHER" ||
            renderer != null ||
            colliders.Length > 0 ||
            depth <= 2;

        if (!interesting)
            return;

        report.Append(
            new string(
                ' ',
                Mathf.Min(
                    depth,
                    20) * 2));

        report.Append("- [");
        report.Append(classification);
        report.Append("] ");
        report.Append(gameObject.name);
        report.Append(" | path=");
        report.Append(GetHierarchyPath(transform));
        report.Append(" | active=");
        report.Append(gameObject.activeInHierarchy);
        report.Append(" | localPos=");
        report.Append(FormatVector(transform.localPosition));
        report.Append(" | worldPos=");
        report.Append(FormatVector(transform.position));
        report.Append(" | localRot=");
        report.Append(FormatVector(transform.localEulerAngles));
        report.Append(" | localScale=");
        report.Append(FormatVector(transform.localScale));

        if (renderer != null)
        {
            report.Append(" | renderer=");
            report.Append(
                renderer.GetType().Name);
            report.Append(" | worldBounds=");
            report.Append(
                FormatBounds(
                    renderer.bounds));

            if (!string.IsNullOrEmpty(materials))
            {
                report.Append(" | materials=");
                report.Append(materials);
            }
        }

        if (meshFilter != null &&
            meshFilter.sharedMesh != null)
        {
            report.Append(" | mesh=");
            report.Append(
                meshFilter.sharedMesh.name);
            report.Append(" | vertices=");
            report.Append(
                meshFilter.sharedMesh.vertexCount);
        }

        if (colliders.Length > 0)
        {
            report.Append(" | colliders=");

            for (int i = 0;
                 i < colliders.Length;
                 i++)
            {
                if (i > 0)
                    report.Append(",");

                Collider collider =
                    colliders[i];

                report.Append(
                    collider.GetType().Name);

                if (collider.isTrigger)
                    report.Append("(trigger)");

                if (collider is MeshCollider meshCollider)
                {
                    report.Append(
                        meshCollider.convex
                            ? "(convex)"
                            : "(nonconvex)");
                }
            }
        }

        string extraComponents =
            BuildExtraComponentList(
                gameObject);

        if (!string.IsNullOrWhiteSpace(
                extraComponents))
        {
            report.Append(" | components=");
            report.Append(extraComponents);
        }

        report.AppendLine();
    }

    private static string BuildExtraComponentList(
        GameObject gameObject)
    {
        Type[] ignored =
        {
            typeof(Transform),
            typeof(Renderer),
            typeof(MeshRenderer),
            typeof(SkinnedMeshRenderer),
            typeof(MeshFilter),
            typeof(Collider),
            typeof(BoxCollider),
            typeof(SphereCollider),
            typeof(CapsuleCollider),
            typeof(MeshCollider)
        };

        return string.Join(
            ",",
            gameObject
                .GetComponents<Component>()
                .Where(component => component != null)
                .Select(component => component.GetType())
                .Where(
                    type =>
                        !ignored.Any(
                            ignoredType =>
                                ignoredType.IsAssignableFrom(
                                    type)))
                .Select(type => type.Name)
                .Distinct());
    }

    private static string Classify(
        string value)
    {
        string lower =
            (value ?? string.Empty)
                .ToLowerInvariant();

        if (ContainsAny(
                lower,
                SidewalkTokens))
            return "SIDEWALK";

        if (ContainsAny(
                lower,
                ParkingTokens))
            return "PARKING";

        if (ContainsAny(
                lower,
                RoadTokens))
            return "ROAD";

        if (ContainsAny(
                lower,
                BuildingTokens))
            return "BUILDING";

        if (lower.Contains("bridge"))
            return "BRIDGE";

        if (lower.Contains("tunnel"))
            return "TUNNEL";

        if (lower.Contains("ground") ||
            lower.Contains("terrain"))
            return "GROUND";

        if (lower.Contains("tree") ||
            lower.Contains("vegetation") ||
            lower.Contains("grass"))
            return "VEGETATION";

        return "OTHER";
    }

    private static bool ContainsAny(
        string value,
        IEnumerable<string> tokens)
    {
        foreach (string token in tokens)
        {
            if (value.Contains(token))
                return true;
        }

        return false;
    }

    private static Bounds? CalculateRendererBounds(
        IEnumerable<GameObject> roots)
    {
        Renderer first =
            roots
                .Where(root => root != null)
                .SelectMany(
                    root =>
                        root.GetComponentsInChildren<Renderer>(
                            true))
                .FirstOrDefault(
                    renderer => renderer != null);

        if (first == null)
            return null;

        Bounds bounds =
            first.bounds;

        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;

            foreach (Renderer renderer in
                     root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    bounds.Encapsulate(
                        renderer.bounds);
            }
        }

        return bounds;
    }

    private static string[] FindTypedAssets(
        string filter,
        string assetRoot)
    {
        return AssetDatabase.FindAssets(
                filter,
                new[] { assetRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetHierarchyPath(
        Transform transform)
    {
        if (transform == null)
            return "<null>";

        var names =
            new List<string>();

        Transform current =
            transform;

        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();

        return string.Join(
            "/",
            names);
    }

    private static string ToAssetPath(
        string absoluteFolder)
    {
        string assetsPath =
            Path.GetFullPath(
                    Application.dataPath)
                .Replace('\\', '/')
                .TrimEnd('/');

        string selected =
            Path.GetFullPath(
                    absoluteFolder)
                .Replace('\\', '/')
                .TrimEnd('/');

        if (!selected.StartsWith(
                assetsPath,
                StringComparison.OrdinalIgnoreCase))
            return null;

        string suffix =
            selected.Substring(
                assetsPath.Length);

        return
            "Assets" +
            suffix;
    }

    private static float Progress(
        int index,
        int count)
    {
        if (count <= 0)
            return 1f;

        return Mathf.Clamp01(
            (index + 1f) /
            count);
    }

    private static string FormatVector(
        Vector3 value)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "({0:0.###},{1:0.###},{2:0.###})",
            value.x,
            value.y,
            value.z);
    }

    private static string FormatBounds(
        Bounds bounds)
    {
        return
            "center=" +
            FormatVector(bounds.center) +
            " size=" +
            FormatVector(bounds.size) +
            " min=" +
            FormatVector(bounds.min) +
            " max=" +
            FormatVector(bounds.max);
    }

    private static void AppendFooter(
        StringBuilder report)
    {
        report.AppendLine();
        report.AppendLine(new string('=', 72));
        report.AppendLine("END OF REPORT");
        report.AppendLine(
            "Upload this text file to ChatGPT; the original Asset Store " +
            "files do not need to be committed to GitHub.");
    }
}
#endif
