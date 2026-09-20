#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorSceneReporter
{
    private const string FcgRoot =
        "Assets/Fantastic City Generator/";

    private const string ReportFileName =
        "MotorCity_FCGSceneReport.txt";

    public static void ExportReport()
    {
        Scene scene =
            SceneManager.GetActiveScene();

        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Report",
                "Нет активной загруженной сцены.",
                "OK");
            return;
        }

        Renderer[] renderers =
            scene
                .GetRootGameObjects()
                .SelectMany(
                    root =>
                        root.GetComponentsInChildren<Renderer>(
                            true))
                .Where(IsFcgRenderer)
                .ToArray();

        if (renderers.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Report",
                "В активной сцене не найдено объектов Fantastic City Generator.",
                "OK");
            return;
        }

        var report =
            new StringBuilder(
                1024 * 256);

        report.AppendLine(
            "MOTOR CITY — GENERATED FCG CITY REPORT");
        report.AppendLine(
            new string(
                '=',
                78));
        report.AppendLine(
            "Scene: " +
            scene.path);
        report.AppendLine(
            "Generated UTC: " +
            DateTime.UtcNow.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture));
        report.AppendLine(
            "Unity: " +
            Application.unityVersion);
        report.AppendLine(
            "FCG renderers found: " +
            renderers.Length);

        Bounds cityBounds =
            renderers[0].bounds;

        foreach (Renderer renderer in
                 renderers.Skip(1))
        {
            cityBounds.Encapsulate(
                renderer.bounds);
        }

        report.AppendLine(
            "City renderer bounds: " +
            FormatBounds(
                cityBounds));
        report.AppendLine();

        foreach (Renderer renderer in
                 renderers
                     .OrderBy(
                         item =>
                             item.bounds.center.z)
                     .ThenBy(
                         item =>
                             item.bounds.center.x))
        {
            string materials =
                string.Join(
                    ", ",
                    renderer.sharedMaterials
                        .Where(
                            material =>
                                material != null)
                        .Select(
                            material =>
                            {
                                string shader =
                                    material.shader != null
                                        ? material.shader.name
                                        : "<missing>";

                                return
                                    material.name +
                                    " [" +
                                    shader +
                                    "]";
                            }));

            Collider[] colliders =
                renderer.GetComponents<Collider>();

            MeshFilter filter =
                renderer.GetComponent<MeshFilter>();

            report.Append("OBJECT ");
            report.Append(
                GetHierarchyPath(
                    renderer.transform));
            report.Append(" | pos=");
            report.Append(
                FormatVector(
                    renderer.transform.position));
            report.Append(" | bounds=");
            report.Append(
                FormatBounds(
                    renderer.bounds));

            if (filter != null &&
                filter.sharedMesh != null)
            {
                report.Append(" | mesh=");
                report.Append(
                    filter.sharedMesh.name);
                report.Append(" | vertices=");
                report.Append(
                    filter.sharedMesh.vertexCount);
            }

            if (!string.IsNullOrWhiteSpace(
                    materials))
            {
                report.Append(" | materials=");
                report.Append(
                    materials);
            }

            if (colliders.Length > 0)
            {
                report.Append(" | colliders=");
                report.Append(
                    string.Join(
                        ",",
                        colliders.Select(
                            collider =>
                                collider.GetType().Name +
                                (collider.isTrigger
                                    ? "(trigger)"
                                    : string.Empty))));
            }

            report.AppendLine();
        }

        string projectRoot =
            Directory.GetParent(
                    Application.dataPath)
                ?.FullName ??
            Application.dataPath;

        string path =
            Path.Combine(
                projectRoot,
                ReportFileName);

        File.WriteAllText(
            path,
            report.ToString(),
            new UTF8Encoding(false));

        EditorUtility.RevealInFinder(
            path);

        Debug.Log(
            "Motor City: generated FCG scene report at " +
            path);

        EditorUtility.DisplayDialog(
            "Motor City — FCG Report",
            "Готово. Загрузи в чат файл:\n\n" +
            path,
            "OK");
    }

    private static bool IsFcgRenderer(
        Renderer renderer)
    {
        if (renderer == null)
            return false;

        foreach (Material material in
                 renderer.sharedMaterials)
        {
            if (material == null)
                continue;

            if (IsFcgPath(
                    AssetDatabase.GetAssetPath(
                        material)))
                return true;

            if (material.shader != null &&
                IsFcgPath(
                    AssetDatabase.GetAssetPath(
                        material.shader)))
                return true;

            string[] properties;

            try
            {
                properties =
                    material.GetTexturePropertyNames();
            }
            catch
            {
                continue;
            }

            foreach (string property in
                     properties)
            {
                Texture texture;

                try
                {
                    texture =
                        material.GetTexture(
                            property);
                }
                catch
                {
                    continue;
                }

                if (texture != null &&
                    IsFcgPath(
                        AssetDatabase.GetAssetPath(
                            texture)))
                    return true;
            }
        }

        MeshFilter filter =
            renderer.GetComponent<MeshFilter>();

        return
            filter != null &&
            filter.sharedMesh != null &&
            IsFcgPath(
                AssetDatabase.GetAssetPath(
                    filter.sharedMesh));
    }

    private static bool IsFcgPath(
        string path)
    {
        return
            !string.IsNullOrWhiteSpace(
                path) &&
            path.Replace(
                    '\\',
                    '/')
                .StartsWith(
                    FcgRoot,
                    StringComparison.OrdinalIgnoreCase);
    }

    private static string GetHierarchyPath(
        Transform transform)
    {
        string path =
            transform.name;

        Transform current =
            transform.parent;

        while (current != null)
        {
            path =
                current.name +
                "/" +
                path;

            current =
                current.parent;
        }

        return path;
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
            FormatVector(
                bounds.center) +
            " size=" +
            FormatVector(
                bounds.size) +
            " min=" +
            FormatVector(
                bounds.min) +
            " max=" +
            FormatVector(
                bounds.max);
    }
}
#endif
