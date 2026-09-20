#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorMaterialSlotDiagnostic
{
    private const string SourceReportPath =
        "MotorCity_FCGSceneReport.txt";

    private const string OutputReportPath =
        "MotorCity_FCGMaterialSlotMismatchReport.txt";

    public static void Diagnose()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Material Slots",
                "Останови Play Mode перед диагностикой.",
                "OK");
            return;
        }

        string absoluteSource =
            Path.GetFullPath(
                SourceReportPath);

        if (!File.Exists(
                absoluteSource))
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Material Slots",
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
                "Motor City — FCG Material Slots",
                "Не найдена сохранённая FCG-сцена в Assets/LocalGenerated.",
                "OK");
            return;
        }

        try
        {
            List<Entry> expected =
                ReadExpected(
                    absoluteSource);

            List<Entry> actual =
                ReadActual(
                    scene,
                    out List<string> structuralProblems);

            Dictionary<string, int> expectedCounts =
                BuildCounts(
                    expected);

            Dictionary<string, int> actualCounts =
                BuildCounts(
                    actual);

            var missingExpected =
                new List<string>();

            foreach (KeyValuePair<string, int> pair in
                     expectedCounts)
            {
                actualCounts.TryGetValue(
                    pair.Key,
                    out int current);

                int missing =
                    pair.Value - current;

                for (int i = 0;
                     i < missing;
                     i++)
                {
                    missingExpected.Add(
                        pair.Key);
                }
            }

            var unexpectedActual =
                new List<string>();

            foreach (KeyValuePair<string, int> pair in
                     actualCounts)
            {
                expectedCounts.TryGetValue(
                    pair.Key,
                    out int historical);

                int extra =
                    pair.Value - historical;

                for (int i = 0;
                     i < extra;
                     i++)
                {
                    unexpectedActual.Add(
                        pair.Key);
                }
            }

            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                "Motor City — FCG material-slot mismatch diagnostic");

            builder.AppendLine(
                $"Scene: {scene.path}");

            builder.AppendLine(
                $"Historical renderer/material signatures: {expected.Count}");

            builder.AppendLine(
                $"Current renderer/material signatures: {actual.Count}");

            builder.AppendLine(
                $"Missing historical signatures: {missingExpected.Count}");

            builder.AppendLine(
                $"Unexpected current signatures: {unexpectedActual.Count}");

            builder.AppendLine(
                $"Structural material-slot problems: {structuralProblems.Count}");

            builder.AppendLine();

            builder.AppendLine(
                "=== STRUCTURAL MATERIAL-SLOT PROBLEMS ===");

            foreach (string item in
                     structuralProblems)
            {
                builder.AppendLine(
                    item);
            }

            builder.AppendLine();

            builder.AppendLine(
                "=== MISSING HISTORICAL SIGNATURES ===");

            foreach (string item in
                     missingExpected)
            {
                builder.AppendLine(
                    item.Replace(
                        "\n",
                        " | "));
            }

            builder.AppendLine();

            builder.AppendLine(
                "=== UNEXPECTED CURRENT SIGNATURES ===");

            foreach (string item in
                     unexpectedActual)
            {
                builder.AppendLine(
                    item.Replace(
                        "\n",
                        " | "));
            }

            File.WriteAllText(
                Path.GetFullPath(
                    OutputReportPath),
                builder.ToString(),
                new UTF8Encoding(false));

            AssetDatabase.Refresh();

            Debug.Log(
                "Motor City: FCG material-slot diagnostic complete. " +
                $"Expected={expected.Count}, current={actual.Count}, " +
                $"missing signatures={missingExpected.Count}, " +
                $"unexpected signatures={unexpectedActual.Count}, " +
                $"structural problems={structuralProblems.Count}. " +
                $"Report={OutputReportPath}");

            EditorUtility.DisplayDialog(
                "Motor City — FCG Material Slots",
                "Готово.\n\n" +
                $"Исторических записей: {expected.Count}\n" +
                $"Текущих записей: {actual.Count}\n" +
                $"Не совпало исторических material-наборов: {missingExpected.Count}\n" +
                $"Неожиданных текущих material-наборов: {unexpectedActual.Count}\n" +
                $"Проблем slot/subMesh/null: {structuralProblems.Count}\n\n" +
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

    private static List<Entry> ReadExpected(
        string path)
    {
        var result =
            new List<Entry>();

        foreach (string line in
                 File.ReadLines(
                     path))
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

            string materialText =
                ReadField(
                    line,
                    "materials=");

            if (string.IsNullOrWhiteSpace(
                    mesh))
                continue;

            result.Add(
                new Entry
                {
                    Path =
                        hierarchyPath,
                    MeshName =
                        mesh,
                    Materials =
                        ParseHistoricalMaterialNames(
                            materialText)
                });
        }

        return result;
    }

    private static List<Entry> ReadActual(
        Scene scene,
        out List<string> structuralProblems)
    {
        structuralProblems =
            new List<string>();

        var result =
            new List<Entry>();

        foreach (GameObject root in
                 scene.GetRootGameObjects())
        {
            foreach (Renderer renderer in
                     root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                Mesh mesh =
                    GetMesh(
                        renderer);

                if (mesh == null)
                    continue;

                Material[] materials =
                    renderer.sharedMaterials ??
                    Array.Empty<Material>();

                string path =
                    GetHierarchyPath(
                        renderer.transform);

                if (materials.Length <
                    mesh.subMeshCount)
                {
                    structuralProblems.Add(
                        $"{path} | mesh={mesh.name} | subMeshes={mesh.subMeshCount} | materialSlots={materials.Length}");
                }

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    if (materials[i] == null)
                    {
                        structuralProblems.Add(
                            $"{path} | mesh={mesh.name} | NULL material slot={i} | subMeshes={mesh.subMeshCount}");
                    }
                }

                result.Add(
                    new Entry
                    {
                        Path =
                            path,
                        MeshName =
                            mesh.name,
                        Materials =
                            materials
                                .Select(
                                    material =>
                                        material != null
                                            ? material.name
                                            : "<null>")
                                .ToArray()
                    });
            }
        }

        return result;
    }

    private static Mesh GetMesh(
        Renderer renderer)
    {
        MeshFilter filter =
            renderer.GetComponent<MeshFilter>();

        if (filter != null)
            return filter.sharedMesh;

        SkinnedMeshRenderer skinned =
            renderer as SkinnedMeshRenderer;

        return skinned != null
            ? skinned.sharedMesh
            : null;
    }

    private static Dictionary<string, int> BuildCounts(
        IEnumerable<Entry> entries)
    {
        var result =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        foreach (Entry entry in
                 entries)
        {
            string key =
                MakeKey(
                    entry);

            result.TryGetValue(
                key,
                out int count);

            result[key] =
                count + 1;
        }

        return result;
    }

    private static string MakeKey(
        Entry entry)
    {
        return
            entry.Path +
            "\nmesh=" +
            entry.MeshName +
            "\nmaterials=" +
            string.Join(
                " || ",
                entry.Materials ??
                Array.Empty<string>());
    }

    private static string[] ParseHistoricalMaterialNames(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
            return Array.Empty<string>();

        var result =
            new List<string>();

        int cursor =
            0;

        while (cursor < value.Length)
        {
            int bracket =
                value.IndexOf(
                    " [",
                    cursor,
                    StringComparison.Ordinal);

            if (bracket < 0)
                break;

            string name =
                value.Substring(
                    cursor,
                    bracket - cursor)
                    .Trim()
                    .TrimStart(',');

            if (!string.IsNullOrWhiteSpace(
                    name))
            {
                result.Add(
                    name);
            }

            int closing =
                value.IndexOf(
                    ']',
                    bracket + 2);

            if (closing < 0)
                break;

            cursor =
                closing + 1;

            while (cursor < value.Length &&
                   (value[cursor] == ',' ||
                    char.IsWhiteSpace(
                        value[cursor])))
            {
                cursor++;
            }
        }

        return result.ToArray();
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

    private sealed class Entry
    {
        public string Path;
        public string MeshName;
        public string[] Materials;
    }
}
#endif
