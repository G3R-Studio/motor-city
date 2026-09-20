#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorGeometryRecoveryDiagnostic
{
    private const string SourceReportPath =
        "MotorCity_FCGSceneReport.txt";

    private const string OutputReportPath =
        "MotorCity_FCGMissingGeometryReport.txt";

    public static void Diagnose()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Missing Geometry",
                "Останови Play Mode перед диагностикой.",
                "OK");
            return;
        }

        string absoluteSource =
            Path.GetFullPath(SourceReportPath);

        if (!File.Exists(absoluteSource))
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Missing Geometry",
                "В корне проекта не найден MotorCity_FCGSceneReport.txt.",
                "OK");
            return;
        }

        if (!FantasticCityGeneratorSceneSource.TryOpenSourceScene(
                out Scene scene,
                out bool openedTemporarily,
                out Scene previousActiveScene))
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Missing Geometry",
                "Не найдена сохранённая FCG-сцена в Assets/LocalGenerated.",
                "OK");
            return;
        }

        try
        {
            List<ExpectedRenderer> expected =
                ReadExpectedRenderers(absoluteSource);

            List<ActualRenderer> actual =
                ReadActualRenderers(scene);

            var actualCounts =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (ActualRenderer item in actual)
            {
                string key =
                    MakeKey(
                        item.Path,
                        item.MeshName);

                actualCounts.TryGetValue(
                    key,
                    out int count);

                actualCounts[key] =
                    count + 1;
            }

            var missing =
                new List<ExpectedRenderer>();

            foreach (ExpectedRenderer item in expected)
            {
                string key =
                    MakeKey(
                        item.Path,
                        item.MeshName);

                if (actualCounts.TryGetValue(
                        key,
                        out int count) &&
                    count > 0)
                {
                    actualCounts[key] =
                        count - 1;
                }
                else
                {
                    missing.Add(item);
                }
            }

            List<ActualRenderer> nullMeshes =
                actual
                    .Where(
                        item =>
                            item.HasMeshFilter &&
                            string.IsNullOrWhiteSpace(
                                item.MeshName))
                    .ToList();

            var builder =
                new StringBuilder();

            builder.AppendLine(
                "Motor City — FCG missing geometry diagnostic");

            builder.AppendLine(
                $"Scene: {scene.path}");

            builder.AppendLine(
                $"Expected renderers from historical report: {expected.Count}");

            builder.AppendLine(
                $"Current renderers: {actual.Count}");

            builder.AppendLine(
                $"Missing historical renderer entries: {missing.Count}");

            builder.AppendLine(
                $"Current MeshFilter renderers with missing mesh: {nullMeshes.Count}");

            builder.AppendLine();

            builder.AppendLine(
                "=== MISSING HISTORICAL RENDERERS ===");

            foreach (ExpectedRenderer item in missing)
            {
                builder.AppendLine(
                    $"{item.Path} | mesh={item.MeshName} | pos={item.Position}");
            }

            builder.AppendLine();

            builder.AppendLine(
                "=== CURRENT RENDERERS WITH NULL MESH ===");

            foreach (ActualRenderer item in nullMeshes)
            {
                builder.AppendLine(
                    $"{item.Path}");
            }

            File.WriteAllText(
                Path.GetFullPath(OutputReportPath),
                builder.ToString(),
                new UTF8Encoding(false));

            AssetDatabase.Refresh();

            Debug.Log(
                "Motor City: FCG missing geometry diagnostic complete. " +
                $"Expected={expected.Count}, current={actual.Count}, " +
                $"missing historical entries={missing.Count}, " +
                $"null meshes={nullMeshes.Count}. " +
                $"Report={OutputReportPath}");

            EditorUtility.DisplayDialog(
                "Motor City — FCG Missing Geometry",
                "Готово.\n\n" +
                $"Исторических Renderer'ов: {expected.Count}\n" +
                $"Сейчас Renderer'ов: {actual.Count}\n" +
                $"Пропавших записей: {missing.Count}\n" +
                $"Renderer'ов с отсутствующим Mesh: {nullMeshes.Count}\n\n" +
                $"Отчёт: {OutputReportPath}",
                "OK");
        }
        finally
        {
            FantasticCityGeneratorSceneSource.FinishSourceScene(
                scene,
                openedTemporarily,
                previousActiveScene,
                false);
        }
    }

    private static List<ExpectedRenderer> ReadExpectedRenderers(
        string path)
    {
        var result =
            new List<ExpectedRenderer>();

        foreach (string line in File.ReadLines(path))
        {
            if (!line.StartsWith(
                    "OBJECT ",
                    StringComparison.Ordinal))
                continue;

            int firstSeparator =
                line.IndexOf(
                    " | ",
                    StringComparison.Ordinal);

            if (firstSeparator <=
                "OBJECT ".Length)
                continue;

            string hierarchyPath =
                line.Substring(
                    "OBJECT ".Length,
                    firstSeparator -
                    "OBJECT ".Length);

            string mesh =
                ReadField(
                    line,
                    "mesh=");

            if (string.IsNullOrWhiteSpace(mesh))
                continue;

            result.Add(
                new ExpectedRenderer
                {
                    Path =
                        hierarchyPath,
                    MeshName =
                        mesh,
                    Position =
                        ReadField(
                            line,
                            "pos=")
                });
        }

        return result;
    }

    private static List<ActualRenderer> ReadActualRenderers(
        Scene scene)
    {
        var result =
            new List<ActualRenderer>();

        foreach (GameObject root in
                 scene.GetRootGameObjects())
        {
            foreach (Renderer renderer in
                     root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                MeshFilter filter =
                    renderer.GetComponent<MeshFilter>();

                SkinnedMeshRenderer skinned =
                    renderer as SkinnedMeshRenderer;

                Mesh mesh =
                    filter != null
                        ? filter.sharedMesh
                        : skinned != null
                            ? skinned.sharedMesh
                            : null;

                result.Add(
                    new ActualRenderer
                    {
                        Path =
                            GetHierarchyPath(
                                renderer.transform),
                        MeshName =
                            mesh != null
                                ? mesh.name
                                : string.Empty,
                        HasMeshFilter =
                            filter != null ||
                            skinned != null
                    });
            }
        }

        return result;
    }

    private static string ReadField(
        string line,
        string token)
    {
        string marker =
            " | " + token;

        int start =
            line.IndexOf(
                marker,
                StringComparison.Ordinal);

        if (start < 0)
            return string.Empty;

        start +=
            marker.Length;

        int end =
            line.IndexOf(
                " | ",
                start,
                StringComparison.Ordinal);

        return
            end >= 0
                ? line.Substring(
                    start,
                    end - start)
                : line.Substring(
                    start);
    }

    private static string MakeKey(
        string path,
        string mesh)
    {
        return
            path +
            "\n" +
            mesh;
    }

    private static string GetHierarchyPath(
        Transform item)
    {
        string path =
            item.name;

        Transform current =
            item.parent;

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

    private sealed class ExpectedRenderer
    {
        public string Path;
        public string MeshName;
        public string Position;
    }

    private sealed class ActualRenderer
    {
        public string Path;
        public string MeshName;
        public bool HasMeshFilter;
    }
}
#endif
